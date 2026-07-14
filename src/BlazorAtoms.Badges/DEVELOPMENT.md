# BlazorAtoms.Badges — Development Notes

Internal implementation notes for maintainers working on this library's source. Not needed to
consume the package — see `README.md` for usage.

## Overlay positioning mechanism

When `AtomBadge` is given `ChildContent`, it wraps it in a `position:relative` host
(`span.atom-badge-host`) and places the badge absolutely within it, keyed off the `data-placement`
attribute (mirrored from the `Placement` param). Inline mode (no `ChildContent`) skips the host
wrapper and the badge renders in normal flow.
