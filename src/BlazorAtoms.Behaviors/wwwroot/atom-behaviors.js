// Backs AtomBrowserSupport.SupportsCssAsync — a thin wrapper around the native feature-detection
// API so the check can run from C#. No state, no DOM: pure passthrough.
export function supportsCss(property, value) {
    return CSS.supports(property, value);
}

// Re-exported for TransitionState's JS-fallback path (see build/SharedJs.props).
export { nextFrame } from './shared/next-frame.js';
