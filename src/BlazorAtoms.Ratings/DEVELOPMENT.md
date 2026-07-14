# BlazorAtoms.Ratings — development notes

Internal implementation notes for maintainers of `AtomRating`. This is not packed into the NuGet
package; see `README.md` for consumer-facing usage docs.

## How the fractional fill works

Each icon is two stacked copies of the same SVG glyph: an empty one underneath and a full-color one
on top, clipped by a wrapper whose width is the fraction of the value in that position. No SVG clip
ids, no masks, no JS — just `overflow: hidden` on a percentage-width box. That makes `4.3` render as
four full icons and one 30%-filled icon.
