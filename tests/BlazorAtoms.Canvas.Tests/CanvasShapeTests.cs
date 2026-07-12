using System.Text.Json;

namespace BlazorAtoms.Canvas.Tests;

// The shape model is the C# <-> JS contract. These prove the polymorphic discriminator serializes,
// round-trips back to the right concrete types, that Translate is a faithful immutable move, and that
// the pure-C# SVG export emits the expected elements.
public class CanvasShapeTests
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Serializes_with_kind_discriminator_and_round_trips()
    {
        var shapes = new List<CanvasShape>
        {
            new CanvasLine(0, 0, 10, 10),
            new CanvasRect(1, 2, 3, 4, Radius: 2) { Fill = "#fff" },
            new CanvasCircle(5, 5, 3),
            new CanvasPath(new List<CanvasPoint> { new(0, 0), new(1, 1) }),
            new CanvasText(2, 2, "hi"),
            new CanvasImage(0, 0, 4, 4, "data:image/png;base64,AA"),
        };

        var json = JsonSerializer.Serialize(shapes, Web);
        Assert.Contains("\"kind\":\"line\"", json);
        Assert.Contains("\"kind\":\"path\"", json);

        var back = JsonSerializer.Deserialize<List<CanvasShape>>(json, Web)!;
        Assert.IsType<CanvasLine>(back[0]);
        Assert.IsType<CanvasRect>(back[1]);
        Assert.IsType<CanvasCircle>(back[2]);
        Assert.IsType<CanvasPath>(back[3]);
        Assert.IsType<CanvasText>(back[4]);
        Assert.IsType<CanvasImage>(back[5]);
    }

    [Fact]
    public void Path_points_round_trip_through_json()
    {
        var path = new CanvasPath(new List<CanvasPoint> { new(1, 2), new(3, 4, 0.5) });
        var back = (CanvasPath)JsonSerializer.Deserialize<CanvasShape>(JsonSerializer.Serialize<CanvasShape>(path, Web), Web)!;
        Assert.Equal(2, back.Points.Count);
        Assert.Equal(3, back.Points[1].X);
        Assert.Equal(0.5, back.Points[1].P);
    }

    [Fact]
    public void Translate_preserves_id_and_moves_geometry()
    {
        var r = new CanvasRect(10, 10, 5, 5) { Fill = "#abc" };
        var moved = Assert.IsType<CanvasRect>(r.Translate(3, 4));
        Assert.Equal(r.Id, moved.Id);
        Assert.Equal("#abc", moved.Fill);
        Assert.Equal(13, moved.X);
        Assert.Equal(14, moved.Y);
    }

    [Fact]
    public void Translate_moves_every_path_point()
    {
        var p = new CanvasPath(new List<CanvasPoint> { new(0, 0), new(2, 2) });
        var moved = Assert.IsType<CanvasPath>(p.Translate(1, 1));
        Assert.Equal(1, moved.Points[0].X);
        Assert.Equal(3, moved.Points[1].Y);
    }

    [Fact]
    public void Visible_defaults_true_and_survives_translate()
    {
        var r = new CanvasRect(0, 0, 5, 5);
        Assert.True(r.Visible);

        var hidden = r with { Visible = false };
        Assert.False(((CanvasRect)hidden.Translate(1, 1)).Visible);
    }

    [Fact]
    public void ToSvg_emits_shapes_and_background()
    {
        var svg = CanvasSvg.ToSvg(
            new List<CanvasShape> { new CanvasRect(0, 0, 10, 10) { Fill = "#abc" }, new CanvasCircle(5, 5, 2) },
            20, 20, "#fff", "#000", 1);

        Assert.StartsWith("<svg", svg);
        Assert.Contains("<rect", svg);
        Assert.Contains("<circle", svg);
        Assert.Contains("#abc", svg);
        Assert.EndsWith("</svg>", svg);
    }
}
