# BlazorAtoms.Avatars — Development Notes

Internal implementation notes for maintainers of this library. Not needed to consume the package —
see `README.md` for usage.

## Rendering approach

- The silhouette is authored as inline SVG directly in `AtomAvatar.razor` (not via `MarkupString`),
  because Blazor's scoped-CSS mechanism only rewrites markup it actually parses at compile time —
  content injected as `MarkupString`/raw HTML at runtime doesn't get the scope attribute and would
  fall outside the component's `.razor.css` rules.
- `AtomAvatar` handles three mutually exclusive render branches: image (`Src` set), initials
  (`HasInitials`, used internally by `AtomInitialsAvatar`), and the built-in silhouette (fallback).

## Shape crop implementation

- `Circle`/`Square`/`Rounded`/`Squircle` crop via CSS `border-radius` + `overflow: hidden` on the
  container.
- `Hexagon` crops via `clip-path` instead, because a `border-radius` can't produce hexagonal
  corners.
- This split is *why* the border ring behaves differently on `Hexagon` (see README Notes): a CSS
  `border` is a rectangle drawn before the clip-path is applied, so only the segments of the
  rectangle that fall on hexagon's flat edges remain visible — the pointed corners cut the border
  away. The `border-radius` shapes don't have this problem since the border and the radius are the
  same box.
- Image cropping uses `object-fit: cover` on the `<img>` combined with the container's crop
  (border-radius/clip-path per above).

## Gradient fills

- Silhouette fill picks a solid `FigureColor` or, when both `FigureGradientFrom`/`To` are set,
  renders a per-instance SVG `<linearGradient>` (`FigureGradientAngle` becomes a
  `gradientTransform: rotate(...)` about the center).
- The gradient's `id` is generated per-component-instance in code-behind:
  ```csharp
  private readonly string _id = "av" + Guid.NewGuid().ToString("N")[..8];
  private string FigId => "fig-" + _id;
  ```
  This is necessary because SVG `<linearGradient>` IDs are global to the DOM — without a unique ID
  per instance, multiple gradient avatars on the same page would collide and all reference whichever
  `<linearGradient>` happened to be defined first.
- Background gradient (`BackgroundGradientFrom`/`To`/`Angle`) is a plain CSS `linear-gradient()` and
  doesn't need this treatment — CSS custom properties/gradients are scoped to the element they're
  applied to, unlike SVG IDs.

## AtomAvatarGroup child styling

- `AtomAvatarGroup` renders either generated `AtomInitialsAvatar`s (from `Names`) or arbitrary
  `ChildContent`. Either way, the overlap margin and separating ring are applied via `::deep`
  selectors in `AtomAvatarGroup.razor.css` (using the `--avg-overlap`/`--avg-ring*` custom
  properties), because the children carry a *different* component's CSS scope attribute — a plain
  scoped selector in `AtomAvatarGroup.razor.css` would never match them.
