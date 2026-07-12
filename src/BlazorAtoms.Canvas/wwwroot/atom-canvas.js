// atom-canvas.js — the drawing engine for BlazorAtoms.Canvas (AtomCanvas / AtomSignaturePad / AtomCanvasStudio).
//
// The library is otherwise data-driven from C#; this module is lazily imported by the component
// (via IJSObjectReference) — no <script> tag, no DI, nothing for the consumer to wire up.
//
// Division of labor: C# owns the shape MODEL + the view (pan/scale); JS owns the 60fps pointer GESTURE
// and the pixels. Invariant: render() is authoritative — it clears and fully redraws from the serialized
// model. C# mutates model/view only at gesture commit (pointerup), so render() never runs mid-gesture.
//
// Coordinates: shapes live in WORLD space. The view transform maps world -> screen:
//   ctx.setTransform(dpr*scale, 0, 0, dpr*scale, dpr*panX, dpr*panY)   (panX/panY are CSS px)
// Pointer -> world: worldX = (clientX - rect.left - panX) / scale. Hit-test, freehand capture, and
// click-to-place all work in world space so they stay correct under zoom/pan.

const STATE = new WeakMap();

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
        selectedId: (opts && opts.selectedId) || null,
        scale: (opts && opts.scale) || 1,
        panX: (opts && opts.panX) || 0,
        panY: (opts && opts.panY) || 0,
        drawing: false,
        current: null,   // in-progress freehand points (world)
        drag: null,      // { id, dx, dy, lastX, lastY } (dx/dy world)
        pan: null,       // { lastX, lastY } (screen)
        click: null,     // { moved, lastX, lastY, world }
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
        st.selectedId = opts.selectedId != null ? opts.selectedId : null;
        st.scale = opts.scale || 1;
        if (!st.drawing) { st.panX = opts.panX || 0; st.panY = opts.panY || 0; } // don't clobber an active pan
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

function applyView(st) {
    const dpr = st.dpr, s = st.scale || 1;
    st.ctx.setTransform(dpr * s, 0, 0, dpr * s, dpr * st.panX, dpr * st.panY);
}

// Clear the whole device buffer in screen space, then paint the (screen-fixed) background.
function clear(st) {
    const ctx = st.ctx, dpr = st.dpr;
    ctx.save();
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    ctx.clearRect(0, 0, st.cssW, st.cssH);
    if (st.opts && st.opts.background) {
        ctx.fillStyle = st.opts.background;
        ctx.fillRect(0, 0, st.cssW, st.cssH);
    }
    ctx.restore();
}

function redraw(el, st) {
    if (!st.ctx) return;
    clear(st);
    applyView(st);
    for (const s of st.shapes) {
        if (s.visible === false) continue;
        drawShape(el, st, s, 0, 0);
    }
    drawSelection(st);
}

function redrawWithDrag(el, st) {
    if (!st.ctx) return;
    clear(st);
    applyView(st);
    for (const s of st.shapes) {
        if (s.visible === false) continue;
        const dragged = st.drag && s.id === st.drag.id;
        drawShape(el, st, s, dragged ? st.drag.dx : 0, dragged ? st.drag.dy : 0);
    }
    drawSelection(st);
}

function drawSelection(st) {
    if (!st.selectedId) return;
    const sh = st.shapes.find(x => x.id === st.selectedId);
    if (!sh || sh.visible === false) return;
    const b = bounds(sh);
    if (!b) return;
    const inv = 1 / (st.scale || 1), pad = 4 * inv;
    const ctx = st.ctx;
    ctx.save();
    ctx.strokeStyle = "#3b82f6";
    ctx.lineWidth = 1.5 * inv;
    ctx.setLineDash([6 * inv, 4 * inv]);
    ctx.strokeRect(b.x - pad, b.y - pad, b.w + 2 * pad, b.h + 2 * pad);
    ctx.restore();
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

function localPos(el, st, e) {
    const r = el.getBoundingClientRect(), s = st.scale || 1;
    return { x: (e.clientX - r.left - st.panX) / s, y: (e.clientY - r.top - st.panY) / s };
}

function capture(el, e) { if (el.setPointerCapture) { try { el.setPointerCapture(e.pointerId); } catch { } } }

function pointerDown(el, st, e) {
    if (st.opts && st.opts.disabled) return;
    const m = st.mode;
    if (m === "draw") {
        st.drawing = true;
        st.current = [localPos(el, st, e)];
        capture(el, e);
        if (st.dotNet) st.dotNet.invokeMethodAsync("NotifyDrawStart");
    } else if (m === "select") {
        const hit = hitTest(st, localPos(el, st, e));
        st.selectedId = hit ? hit.id : null;
        if (st.dotNet) st.dotNet.invokeMethodAsync("NotifyShapeSelected", st.selectedId);
        if (hit) {
            st.drawing = true;
            st.drag = { id: hit.id, dx: 0, dy: 0, lastX: e.clientX, lastY: e.clientY };
            capture(el, e);
        }
        redraw(el, st); // reflect selection immediately
    } else if (m === "pan") {
        st.drawing = true;
        st.pan = { lastX: e.clientX, lastY: e.clientY };
        capture(el, e);
    } else if (m === "static") {
        st.drawing = true;
        st.click = { moved: false, lastX: e.clientX, lastY: e.clientY, world: localPos(el, st, e) };
    }
}

function pointerMove(el, st, e) {
    if (!st.drawing) return;
    if (st.mode === "draw" && st.current) {
        st.current.push(localPos(el, st, e));
        redraw(el, st);
        strokeCurrent(st);
    } else if (st.mode === "select" && st.drag) {
        const s = st.scale || 1;
        st.drag.dx += (e.clientX - st.drag.lastX) / s;
        st.drag.dy += (e.clientY - st.drag.lastY) / s;
        st.drag.lastX = e.clientX;
        st.drag.lastY = e.clientY;
        redrawWithDrag(el, st);
    } else if (st.mode === "pan" && st.pan) {
        st.panX += (e.clientX - st.pan.lastX);
        st.panY += (e.clientY - st.pan.lastY);
        st.pan.lastX = e.clientX;
        st.pan.lastY = e.clientY;
        redraw(el, st);
    } else if (st.mode === "static" && st.click) {
        if (Math.abs(e.clientX - st.click.lastX) > 3 || Math.abs(e.clientY - st.click.lastY) > 3) st.click.moved = true;
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
    } else if (st.mode === "pan" && st.pan) {
        st.pan = null;
        st.drawing = false;
        if (st.dotNet) st.dotNet.invokeMethodAsync("NotifyViewChanged", st.panX, st.panY, st.scale);
    } else if (st.mode === "static" && st.click) {
        const c = st.click;
        st.click = null;
        st.drawing = false;
        if (!c.moved && st.dotNet) {
            const hit = hitTest(st, c.world, true);
            if (hit) st.dotNet.invokeMethodAsync("OnShapeClicked", hit.id);
            else st.dotNet.invokeMethodAsync("NotifyCanvasClick", c.world.x, c.world.y);
        }
    } else {
        st.drawing = false;
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

function hitTest(st, p, ignoreDraggable) {
    for (let i = st.shapes.length - 1; i >= 0; i--) {
        const s = st.shapes[i];
        if (s.visible === false) continue;
        if (!ignoreDraggable && s.draggable === false) continue;
        if (pointInBounds(s, p)) return s;
    }
    return null;
}

function pointInBounds(s, p) {
    const b = bounds(s);
    if (!b) return false;
    const pad = 6;
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
        if (SETTERS.has(name)) ctx[name] = args[0];
        else if (typeof ctx[name] === "function") ctx[name].apply(ctx, args);
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
