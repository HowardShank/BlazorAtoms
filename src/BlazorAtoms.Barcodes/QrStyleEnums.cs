namespace BlazorAtoms.Barcodes;

/// <summary>Data-module shape for <see cref="AtomQrCode"/>.</summary>
public enum ModuleShape { Square, Rounded, Dot, Ellipse, Diamond, Star, Pill, Blob }

/// <summary>Finder-eye outer frame shape. Restricted to decoder-friendly variants: Rhombus,
/// ConcaveStar and Dotted broke the 1:1:3:1:1 finder-pattern ratio that ISO-18004 scanners
/// (including ZXing.Net) require, so they were removed.</summary>
public enum EyeFrame { Square, Circle, Rounded }

/// <summary>Finder-eye pupil shape. Restricted to shapes whose fill is contiguous through the
/// pupil's centre horizontal / vertical / diagonal lines — that's what preserves the 1:1:3:1:1
/// finder-pattern ratio ISO-18004 scanners require. Star was removed because its arm gaps
/// interrupt the centre scanline even at large sizes.</summary>
public enum EyePupil { Square, Circle, Rounded, Rhombus }

/// <summary>Which corners of the eye frame get <c>EyeFrameRadius</c> applied.</summary>
[System.Flags]
public enum EyeCorner
{
    None = 0,
    TopLeft = 1,
    TopRight = 2,
    BottomRight = 4,
    BottomLeft = 8,
    All = TopLeft | TopRight | BottomRight | BottomLeft,
}

/// <summary>Foreground fill style.</summary>
public enum FillStyle { Solid, LinearGradient, RadialGradient }

/// <summary>Center-logo backing shape.</summary>
public enum LogoShape { Square, Rounded, Circle }

/// <summary>Outer decorative frame shape wrapping the QR.</summary>
public enum FrameShape { None, Square, Rounded, Circle, DottedCircle, DoubleCircle, Blob, Torn }

/// <summary>Text banner position (paired with <see cref="FrameShape"/>).</summary>
public enum FrameBanner { None, Bottom, BottomPointer, Top, BottomPill, Inline }
