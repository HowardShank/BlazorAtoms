const active = new Map();

function resolveCanvas(canvasOrId) {
    if (typeof canvasOrId === "string") {
        return document.getElementById(canvasOrId);
    }
    return canvasOrId instanceof HTMLElement ? canvasOrId : null;
}

function updateLayout(state) {
    const canvas = state.canvas;
    const fontSize = Math.max(1, parseFontSize(readStyle(canvas, "--mr-font-size", "16px")));
    // Size canvas from its own rendered box, not the parent — CSS like `width: 50%` shrinks
    // the canvas below its parent, and sizing from the parent would produce a permanent width
    // mismatch that fires updateLayout every frame (which clears the canvas → no trail).
    // Floor to integers so the per-frame mismatch check against clientWidth (also integer) is
    // stable — fractional rect values would otherwise re-trigger layout every frame and wipe
    // the trail.
    const width = Math.max(1, Math.floor(canvas.clientWidth || canvas.getBoundingClientRect().width));
    const height = Math.max(1, Math.floor(canvas.clientHeight || canvas.getBoundingClientRect().height));

    canvas.width = width;
    canvas.height = height;

    const columns = Math.floor(canvas.width / fontSize) || 1;
    const oldDrops = state.drops;
    const drops = Array(columns).fill(0).map((_, i) =>
        i < oldDrops.length ? oldDrops[i] : Math.floor(Math.random() * (canvas.height / fontSize)));

    state.fontSize = fontSize;
    state.columns = columns;
    state.drops = drops;
    return { columns, drops, fontSize };
}

function createState(canvasOrId) {
    const canvas = resolveCanvas(canvasOrId);
    if (!canvas) {
        return null;
    }

    const ctx = canvas.getContext("2d", { alpha: false });
    const letters = "アァイィウヴエェオカガキギクグケゲコゴサザシジスズセゼソゾタダチヂッツヅテデトドナニヌネノハバパヒビピフブプヘベペホボポマミムメモヤユヨラリルレロワヲンABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    const chars = letters.split("");

    const state = {
        canvasId: canvasOrId,
        canvas,
        ctx,
        chars,
        fontSize: 16,
        columns: 0,
        drops: [],
        running: false,
        rafId: 0,
        resizeHandler: () => updateLayout(state),
    };

    updateLayout(state);
    canvas.addEventListener("resize", state.resizeHandler);
    window.addEventListener("resize", state.resizeHandler);

    return state;
}

function draw(state) {
    if (!state.running) return;

    if (!state.canvas.isConnected) {
        dispose(state.canvasId);
        return;
    }

    const { ctx, canvas, chars } = state;

    // Read latest CSS variables so control changes are picked up every frame.
    const color = readStyle(canvas, "--mr-color", "#0F0");
    const backgroundColor = readStyle(canvas, "--mr-bg", color);
    const glow = canvas.closest("[data-glow='true']") !== null;
    const fontFamily = readStyle(canvas, "--mr-font", "monospace");

    // Recalculate columns/drops whenever the font size or container size changes.
    if (canvas.width !== canvas.clientWidth || canvas.height !== canvas.clientHeight) {
        updateLayout(state);
    }
    const fontSize = Math.max(1, parseFontSize(readStyle(canvas, "--mr-font-size", "16px")));
    if (fontSize !== state.fontSize) {
        updateLayout(state);
    }

    const { columns, drops } = state;

    ctx.fillStyle = "rgba(0, 0, 0, 0.05)";
    ctx.fillRect(0, 0, canvas.width, canvas.height);

    ctx.fillStyle = color;
    ctx.font = `${fontSize}px ${fontFamily}`;

    if (glow) {
        ctx.shadowColor = backgroundColor;
        ctx.shadowBlur = 8;
    } else {
        ctx.shadowColor = "transparent";
        ctx.shadowBlur = 0;
    }

    const speed = Math.max(0, parseFloat(readStyle(canvas, "--mr-speed", "1")) || 1);

    for (let i = 0; i < columns; i++) {
        const text = chars[Math.floor(Math.random() * chars.length)];
        ctx.fillText(text, i * fontSize, drops[i] * fontSize);

        if (drops[i] * fontSize > canvas.height && Math.random() > 0.975) {
            drops[i] = 0;
        }
        drops[i] += speed;
    }

    state.rafId = requestAnimationFrame(() => draw(state));
}

function parseFontSize(value) {
    if (!value) return 16;
    const parsed = parseFloat(value);
    return Number.isFinite(parsed) && parsed > 0 ? parsed : 16;
}

function getKey(canvasOrId) {
    if (typeof canvasOrId === "string") return canvasOrId;
    return canvasOrId?.id;
}

function readStyle(canvas, name, fallback) {
    return window.getComputedStyle(canvas).getPropertyValue(name).trim() || fallback;
}

export function start(canvasOrId) {
    const key = getKey(canvasOrId);
    if (!key) return;

    if (active.has(key)) {
        stop(key);
    }

    const state = createState(canvasOrId);
    if (!state) return;

    active.set(key, state);
    state.running = true;
    state.rafId = requestAnimationFrame(() => draw(state));
}

export function stop(canvasOrId) {
    const key = getKey(canvasOrId);
    if (!key) return;

    const state = active.get(key);
    if (!state) return;

    state.running = false;
    if (state.rafId) {
        cancelAnimationFrame(state.rafId);
        state.rafId = 0;
    }
}

export function dispose(canvasOrId) {
    const key = getKey(canvasOrId);
    if (!key) return;

    const state = active.get(key);
    if (!state) return;

    stop(key);

    state.canvas?.removeEventListener("resize", state.resizeHandler);
    window.removeEventListener("resize", state.resizeHandler);

    active.delete(key);
}
