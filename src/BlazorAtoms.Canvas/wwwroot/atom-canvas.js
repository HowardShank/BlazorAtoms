// atom-canvas.js — the drawing engine for BlazorAtoms.Canvas (AtomCanvas / AtomSignaturePad).
//
// The library is otherwise data-driven from C#; this module is lazily imported by the component
// (via IJSObjectReference) — no <script> tag, no DI, nothing for the consumer to wire up.
//
// Division of labor: C# owns the shape MODEL; JS owns the 60fps pointer GESTURE and the pixels.
// Invariant: render() is authoritative — it clears and fully redraws from the serialized model.
// C# mutates the model only at gesture commit (pointer-up), so render() never runs mid-gesture;
// that keeps freehand drawing smooth even over a Blazor Server circuit (one callback per gesture).

const STATE = new WeakMap();

// Context "state" names that are assignments (ctx.fillStyle = x), not method calls.
const SETTERS = new Set([
    "fillStyle", "strokeStyle", "lineWidth", "lineCap", "lineJoin", "font",
    "globalAlpha", "textAlign", "textBaseline", "miterLimit",
]);

function stateOf(el) { return el ? STATE.get(el) : null; }

export function init(el, dotNet, opts) {
    if (!el) return;
    dispose(el); // idempotent re-init
    const st = {
        dotNet,
        opts: opts || {},
        shapes: [],
        mode: (opts && opts.mode) || "static",
        drawing: false,
        current: null,   // in-progress freehand points
        drag: null,      // { id, dx, dy, lastX, lastY }
        images: new Map(),
        dpr: window.devicePixelRatio || 1,
        ctx: null,
        cssW: 0,
        cssH: 0,
    };
    STATE.set(el, st);
    applySize(el, st);

    const onDown = (e) => pointerDown(el, st, e);
    const onMove = (e) => pointerMove(el, st, e);
    const onUp = (e) => pointerUp(el, st, e);
    st.handlers = { onDown, onMove, onUp };
    el.addEventListener("pointerdown", onDown);
    el.addEventListener("pointermove", onMove);
    // Listen for up on the window so a stroke that ends off-canvas still commits.
    window.addEventListener("pointerup", onUp);

    redraw(el, st);
}

function applySize(el, st) {
    const w = (st.opts && st.opts.width) || el.clientWidth || 300;
    const h = (st.opts && st.opts.height) || el.clientHeight || 150;
    const dpr = st.dpr;
    st.cssW = w;
    st.cssH = h;
    el.width = Math.round(w * dpr);
    el.height = Math.round(h * dpr);
    el.style.width = w + "px";
    el.style.height = h + "px";
    const ctx = el.getContext("2d");
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0); // draw in CSS pixels; the backing store is hi-DPI
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    st.ctx = ctx;
}

export function render(el, shapesJson, opts) {
    const st = stateOf(el);
    if (!st) return;
    if (opts) {
        const sizeChanged = opts.width !== st.cssW || opts.height !== st.cssH;
        st.opts = opts;
        st.mode = opts.mode || st.mode;
        if (sizeChanged) applySize(el, st);
    }
    try { st.shapes = shapesJson ? JSON.parse(shapesJson) : []; }
    catch { st.shapes = []; }
    if (!st.drawing) redraw(el, st); // suppressed mid-gesture; C# won't change the model then anyway
}

export function setMode(el, mode) {
    const st = stateOf(el);
    if (st) st.mode = mode;
}

// ---- rendering ----

function clear(st) {
    st.ctx.clearRect(0, 0, st.cssW, st.cssH);
    if (st.opts && st.opts.background) {
        st.ctx.fillStyle = st.opts.background;
        st.ctx.fillRect(0, 0, st.cssW, st.cssH);
    }
}

function redraw(el, st) {
    if (!st.ctx) return;
    clear(st);
    for (const s of st.shapes) drawShape(el, st, s, 0, 0);
}

// Redraw with the currently-dragged shape offset by (drag.dx, drag.dy).
function redrawWithDrag(el, st) {
    if (!st.ctx) return;
    clear(st);
    for (const s of st.shapes) {
        const isDragged = st.drag && s.id === st.drag.id;
        drawShape(el, st, s, isDragged ? st.drag.dx : 0, isDragged ? st.drag.dy : 0);
    }
}

function applyStyle(st, s) {
    const ctx = st.ctx;
    ctx.globalAlpha = (s.opacity != null) ? s.opacity : 1;
    ctx.strokeStyle = s.stroke || st.opts.penColor || "#111827";
    ctx.lineWidth = (s.strokeWidth != null) ? s.strokeWidth : (st.opts.penWidth || 2);
}

function drawShape(el, st, s, ox, oy) {
    const ctx = st.ctx;
    ctx.save();
    ctx.translate(ox || 0, oy || 0);
    applyStyle(st, s);
    switch (s.kind) {
        case "line":
            ctx.beginPath();
            ctx.moveTo(s.x1, s.y1);
            ctx.lineTo(s.x2, s.y2);
            ctx.stroke();
            break;
        case "rect":
            roundRect(ctx, s.x, s.y, s.width, s.height, s.radius || 0);
            if (s.fill) { ctx.fillStyle = s.fill; ctx.fill(); }
            ctx.stroke();
            break;
        case "circle":
            ctx.beginPath();
            ctx.arc(s.cx, s.cy, Math.max(0, s.r), 0, Math.PI * 2);
            if (s.fill) { ctx.fillStyle = s.fill; ctx.fill(); }
            ctx.stroke();
            break;
        case "path":
            tracePath(ctx, s.points, s.smooth !== false, s.closed === true);
            if (s.closed && s.fill) { ctx.fillStyle = s.fill; ctx.fill(); }
            ctx.stroke();
            break;
        case "text":
            ctx.font = (s.fontSize || 16) + "px " + (s.fontFamily || "sans-serif");
            ctx.fillStyle = s.fill || s.stroke || st.opts.penColor || "#111827";
            ctx.fillText(s.text || "", s.x, s.y);
            break;
        case "image":
            drawImage(el, st, s);
            break;
    }
    ctx.restore();
}

function tracePath(ctx, pts, smooth, closed) {
    if (!pts || pts.length === 0) return;
    ctx.beginPath();
    ctx.moveTo(pts[0].x, pts[0].y);
    if (!smooth || pts.length < 3) {
        for (let i = 1; i < pts.length; i++) ctx.lineTo(pts[i].x, pts[i].y);
    } else {
        // Quadratic smoothing through segment midpoints — the classic freehand look.
        for (let i = 1; i < pts.length - 1; i++) {
            const mx = (pts[i].x + pts[i + 1].x) / 2;
            const my = (pts[i].y + pts[i + 1].y) / 2;
            ctx.quadraticCurveTo(pts[i].x, pts[i].y, mx, my);
        }
        const last = pts[pts.length - 1];
        ctx.lineTo(last.x, last.y);
    }
    if (closed) ctx.closePath();
}

function roundRect(ctx, x, y, w, h, r) {
    r = Math.min(r, Math.abs(w) / 2, Math.abs(h) / 2);
    ctx.beginPath();
    if (r <= 0) { ctx.rect(x, y, w, h); return; }
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
}

function drawImage(el, st, s) {
    let entry = st.images.get(s.src);
    if (!entry) {
        const img = new Image();
        entry = { img, loaded: false };
        img.onload = () => { entry.loaded = true; redraw(el, st); };
        img.onerror = () => { entry.loaded = false; };
        img.src = s.src;
        st.images.set(s.src, entry);
    }
    if (entry.loaded) st.ctx.drawImage(entry.img, s.x, s.y, s.width, s.height);
}

// ---- pointer gestures ----

function localPos(el, e) {
    const r = el.getBoundingClientRect();
    return { x: e.clientX - r.left, y: e.clientY - r.top };
}

function pointerDown(el, st, e) {
    if (st.opts && st.opts.disabled) return;
    if (st.mode === "draw") {
        st.drawing = true;
        st.current = [localPos(el, e)];
        if (el.setPointerCapture) { try { el.setPointerCapture(e.pointerId); } catch { } }
        if (st.dotNet) st.dotNet.invokeMethodAsync("NotifyDrawStart");
    } else if (st.mode === "select") {
        const hit = hitTest(st, localPos(el, e));
        if (hit) {
            st.drawing = true;
            st.drag = { id: hit.id, dx: 0, dy: 0, lastX: e.clientX, lastY: e.clientY };
        }
    } else if (st.mode === "static") {
        const hit = hitTest(st, localPos(el, e), /*ignoreDraggable*/ true);
        if (hit && st.dotNet) st.dotNet.invokeMethodAsync("OnShapeClicked", hit.id);
    }
}

function pointerMove(el, st, e) {
    if (!st.drawing) return;
    if (st.mode === "draw" && st.current) {
        st.current.push(localPos(el, e));
        redraw(el, st);
        strokeCurrent(st);
    } else if (st.mode === "select" && st.drag) {
        st.drag.dx += (e.clientX - st.drag.lastX);
        st.drag.dy += (e.clientY - st.drag.lastY);
        st.drag.lastX = e.clientX;
        st.drag.lastY = e.clientY;
        redrawWithDrag(el, st);
    }
}

function pointerUp(el, st, e) {
    if (!st.drawing) return;
    if (st.mode === "draw" && st.current) {
        const pts = st.current.map(p => ({ x: p.x, y: p.y }));
        st.current = null;
        st.drawing = false;
        if (st.dotNet && pts.length) st.dotNet.invokeMethodAsync("OnStrokeCommitted", pts);
    } else if (st.mode === "select" && st.drag) {
        const d = st.drag;
        st.drag = null;
        st.drawing = false;
        if (st.dotNet && (d.dx || d.dy)) st.dotNet.invokeMethodAsync("OnShapeMoved", d.id, d.dx, d.dy);
    }
}

function strokeCurrent(st) {
    const ctx = st.ctx;
    ctx.save();
    ctx.strokeStyle = st.opts.penColor || "#111827";
    ctx.lineWidth = st.opts.penWidth || 2;
    tracePath(ctx, st.current, st.opts.smoothing !== false, false);
    ctx.stroke();
    ctx.restore();
}

// Top-most shape whose bounding box contains p. Honors `draggable` unless ignoreDraggable.
function hitTest(st, p, ignoreDraggable) {
    for (let i = st.shapes.length - 1; i >= 0; i--) {
        const s = st.shapes[i];
        if (!ignoreDraggable && s.draggable === false) continue;
        if (pointInBounds(s, p)) return s;
    }
    return null;
}

function pointInBounds(s, p) {
    const b = bounds(s);
    if (!b) return false;
    const pad = 6; // a little slop so thin strokes are grabbable
    return p.x >= b.x - pad && p.x <= b.x + b.w + pad &&
           p.y >= b.y - pad && p.y <= b.y + b.h + pad;
}

function bounds(s) {
    switch (s.kind) {
        case "line":
            return { x: Math.min(s.x1, s.x2), y: Math.min(s.y1, s.y2), w: Math.abs(s.x2 - s.x1), h: Math.abs(s.y2 - s.y1) };
        case "rect":
        case "image":
            return { x: s.x, y: s.y, w: s.width, h: s.height };
        case "circle":
            return { x: s.cx - s.r, y: s.cy - s.r, w: s.r * 2, h: s.r * 2 };
        case "text":
            return { x: s.x, y: s.y - (s.fontSize || 16), w: (s.text || "").length * (s.fontSize || 16) * 0.6, h: (s.fontSize || 16) * 1.2 };
        case "path": {
            const pts = s.points || [];
            if (!pts.length) return null;
            let minX = Infinity, minY = Infinity, maxX = -Infinity, maxY = -Infinity;
            for (const q of pts) { minX = Math.min(minX, q.x); minY = Math.min(minY, q.y); maxX = Math.max(maxX, q.x); maxY = Math.max(maxY, q.y); }
            return { x: minX, y: minY, w: maxX - minX, h: maxY - minY };
        }
    }
    return null;
}

// ---- imperative escape hatch + export ----

export function runCommands(el, batch) {
    const st = stateOf(el);
    if (!st || !st.ctx || !batch) return;
    const ctx = st.ctx;
    for (const row of batch) {
        const name = row[0];
        const args = row.slice(1);
        if (SETTERS.has(name)) {
            ctx[name] = args[0];
        } else if (typeof ctx[name] === "function") {
            ctx[name].apply(ctx, args);
        }
    }
}

export function clearCanvas(el) {
    const st = stateOf(el);
    if (st) clear(st);
}

export function toDataUrl(el, type, quality) {
    if (!el) return "";
    try { return el.toDataURL(type || "image/png", quality); }
    catch { return ""; }
}

export function dispose(el) {
    const st = stateOf(el);
    if (!st) return;
    if (st.handlers) {
        el.removeEventListener("pointerdown", st.handlers.onDown);
        el.removeEventListener("pointermove", st.handlers.onMove);
        window.removeEventListener("pointerup", st.handlers.onUp);
    }
    STATE.delete(el);
}
