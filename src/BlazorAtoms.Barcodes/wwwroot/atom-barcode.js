// atom-barcode.js — optional interop for BlazorAtoms.Barcodes.
//
// Rendering itself stays pure C# / inline SVG. This module only loads when the consumer
// calls CopyAsync / SaveAsync / GetPngBytesAsync on AtomBarcode or AtomQrCode.
//
// Exports:
//   svgToPngBase64(svg, pixelWidth) -> Promise<string>   // base64 PNG payload
//   copyText(text)                  -> Promise<void>     // clipboard: text/plain
//   copyPngBase64(b64)              -> Promise<void>     // clipboard: image/png
//   saveText(text, mime, fileName)  -> Promise<void>     // download or Save-As
//   savePngBase64(b64, fileName)    -> Promise<void>     // download or Save-As
//   fetchToBase64(url)              -> Promise<string>   // fetch remote/data URI to base64
//   readImageFromClipboard()        -> Promise<{base64,mimeType}|null>
//                                                        // navigator.clipboard.read image item

function svgToBlob(svg) {
    return new Blob([svg], { type: 'image/svg+xml;charset=utf-8' });
}

function b64ToBytes(b64) {
    const bin = atob(b64);
    const out = new Uint8Array(bin.length);
    for (let i = 0; i < bin.length; i++) out[i] = bin.charCodeAt(i);
    return out;
}

function pngBlobFromBase64(b64) {
    return new Blob([b64ToBytes(b64)], { type: 'image/png' });
}

// Render an SVG string to a PNG data URL at the requested pixel width.
// Height scales to preserve the SVG's aspect ratio.
export function svgToPngBase64(svg, pixelWidth) {
    return new Promise((resolve, reject) => {
        try {
            // debugger;
            const svgBlob = svgToBlob(svg);
            const url = URL.createObjectURL(svgBlob);
            const img = new Image();
            img.onload = () => {
                try {
                    const targetW = pixelWidth && pixelWidth > 0 ? pixelWidth : (img.naturalWidth || img.width || 300);
                    const scale = targetW / (img.naturalWidth || img.width || targetW);
                    const targetH = Math.max(1, Math.round((img.naturalHeight || img.height || 1) * scale));
                    const canvas = document.createElement('canvas');
                    canvas.width = targetW;
                    canvas.height = targetH;
                    const ctx = canvas.getContext('2d');
                    ctx.drawImage(img, 0, 0, targetW, targetH);
                    URL.revokeObjectURL(url);
                    const dataUrl = canvas.toDataURL('image/png');
                    // Strip "data:image/png;base64," prefix.
                    const comma = dataUrl.indexOf(',');
                    resolve(comma >= 0 ? dataUrl.substring(comma + 1) : dataUrl);
                } catch (err) {
                    // debugger;
                    URL.revokeObjectURL(url);
                    reject(err);
                }
            };
            img.onerror = () => {
                // debugger;
                URL.revokeObjectURL(url);
                reject(new Error('Failed to load SVG for PNG conversion.'));
            };
            img.src = url;
        } catch (err) {
            // debugger;
            reject(err);
        }
    });
}

export async function copyText(text) {
    if (!navigator.clipboard || !navigator.clipboard.writeText)
        throw new Error('Clipboard text API unavailable (needs secure context).');
    await navigator.clipboard.writeText(text);
}

export async function copyPngBase64(b64) {
    if (!navigator.clipboard || !navigator.clipboard.write || typeof ClipboardItem === 'undefined')
        throw new Error('Clipboard image API unavailable (needs secure context + Chromium/WebKit).');
    const blob = pngBlobFromBase64(b64);
    await navigator.clipboard.write([new ClipboardItem({ 'image/png': blob })]);
}

// Rasterize an SVG string to a PNG Blob. Shared by svgToPngClipboard + svgToPngSave.
function rasterizeSvgToPngBlob(svg, pixelWidth) {
    return new Promise((resolve, reject) => {
        const svgBlob = svgToBlob(svg);
        const url = URL.createObjectURL(svgBlob);
        const img = new Image();
        img.onload = () => {
            try {
                const targetW = pixelWidth && pixelWidth > 0 ? pixelWidth : (img.naturalWidth || 300);
                const scale = targetW / (img.naturalWidth || targetW);
                const targetH = Math.max(1, Math.round((img.naturalHeight || 1) * scale));
                const canvas = document.createElement('canvas');
                canvas.width = targetW;
                canvas.height = targetH;
                canvas.getContext('2d').drawImage(img, 0, 0, targetW, targetH);
                URL.revokeObjectURL(url);
                canvas.toBlob((blob) => {
                    if (!blob) reject(new Error('canvas.toBlob returned null.'));
                    else resolve(blob);
                }, 'image/png');
            } catch (err) {
                URL.revokeObjectURL(url);
                reject(err);
            }
        };
        img.onerror = () => { URL.revokeObjectURL(url); reject(new Error('Failed to load SVG for PNG conversion.')); };
        img.src = url;
    });
}

// One-shot: rasterize SVG → PNG Blob → clipboard, entirely in the browser. Chromium enforces
// "transient user activation" on navigator.clipboard.write() — passing a Promise to ClipboardItem
// keeps the synchronous write() call inside the click activation window while the blob is built
// asynchronously in the background. Documented pattern from MDN + web.dev.
export async function svgToPngClipboard(svg, pixelWidth) {
    if (!navigator.clipboard || !navigator.clipboard.write || typeof ClipboardItem === 'undefined')
        throw new Error('Clipboard image API unavailable (needs secure context + Chromium/WebKit).');
    const pngPromise = rasterizeSvgToPngBlob(svg, pixelWidth);
    await navigator.clipboard.write([new ClipboardItem({ 'image/png': pngPromise })]);
}

async function saveBlob(blob, fileName) {
    // Prefer the File System Access API when available — user picks folder + filename.
    if (window.showSaveFilePicker) {
        try {
            const ext = fileName && fileName.lastIndexOf('.') >= 0 ? fileName.substring(fileName.lastIndexOf('.')) : '';
            const desc = blob.type === 'image/png' ? 'PNG image' : (blob.type.startsWith('image/svg') ? 'SVG image' : 'File');
            const handle = await window.showSaveFilePicker({
                suggestedName: fileName || 'file',
                types: ext ? [{ description: desc, accept: { [blob.type || 'application/octet-stream']: [ext] } }] : undefined,
            });
            const writable = await handle.createWritable();
            await writable.write(blob);
            await writable.close();
            return;
        } catch (err) {
            // User cancelled the picker — treat as a no-op, not an error.
            if (err && (err.name === 'AbortError' || err.code === 20)) return;
            // Any other picker failure falls through to the anchor fallback.
        }
    }
    // Fallback: anchor with download attribute (browser Downloads folder).
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName || 'file';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    setTimeout(() => URL.revokeObjectURL(url), 0);
}

export async function saveText(text, mime, fileName) {
    await saveBlob(new Blob([text], { type: mime || 'text/plain;charset=utf-8' }), fileName);
}

export async function savePngBase64(b64, fileName) {
    await saveBlob(pngBlobFromBase64(b64), fileName);
}

// One-shot: rasterize SVG → PNG Blob → save. Same motivation as svgToPngClipboard — bypass the
// SignalR message-size limit by never returning the PNG bytes across the wire.
export async function svgToPngSave(svg, pixelWidth, fileName) {
    const blob = await rasterizeSvgToPngBlob(svg, pixelWidth);
    await saveBlob(blob, fileName);
}

// ---- fetch helper ------------------------------------------------------------------

// Fetch a URL (http(s) or data:) and return base64. Used by AtomQrCodeImage's Copy/Save PNG path.
export async function fetchToBase64(url) {
    if (!url) return '';
    const resp = await fetch(url);
    if (!resp.ok) throw new Error(`Fetch failed: ${resp.status}`);
    const buf = await resp.arrayBuffer();
    const bytes = new Uint8Array(buf);
    let bin = '';
    for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
    return btoa(bin);
}

// ---- clipboard image read ----------------------------------------------------------

async function blobToBase64(blob) {
    const buf = await blob.arrayBuffer();
    const bytes = new Uint8Array(buf);
    let bin = '';
    for (let i = 0; i < bytes.length; i++) bin += String.fromCharCode(bytes[i]);
    return btoa(bin);
}

// Read the first image on the system clipboard. Requires secure context + user permission.
export async function readImageFromClipboard() {
    if (!navigator.clipboard || !navigator.clipboard.read)
        throw new Error('Clipboard read API unavailable (needs secure context + Chromium/WebKit).');
    const items = await navigator.clipboard.read();
    for (const item of items) {
        const imageType = item.types.find(t => t.startsWith('image/'));
        if (!imageType) continue;
        const blob = await item.getType(imageType);
        return { base64: await blobToBase64(blob), mimeType: imageType };
    }
    return null;
}

