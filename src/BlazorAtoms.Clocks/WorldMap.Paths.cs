using System.Globalization;
using System.Text;

namespace BlazorAtoms.Clocks;

/// <summary>
/// The bundled world artwork and default city set for <see cref="AtomTimeZoneMap"/>. The continents
/// are a deliberately low-poly, public-domain-style outline stored as raw longitude/latitude vertex
/// arrays and projected once into the map's <c>0 0 360 180</c> equirectangular viewBox. Keeping the
/// source as lon/lat (rather than a hand-tuned path string) makes the geometry auditable and matches
/// the projection the component uses for bands, pins and the terminator. No external asset, no raster.
/// </summary>
internal static class WorldMap
{
    // Equirectangular projection into the 360×180 viewBox — identical to AtomTimeZoneMap.Project.
    private static double X(double lon) => lon + 180;
    private static double Y(double lat) => 90 - lat;

    // Rough continent outlines as (lon, lat) rings. Low vertex count on purpose — this reads as a
    // world map at a glance without shipping kilobytes of coastline detail.
    private static readonly double[][,] Continents =
    {
        // North America
        new double[,] { {-158,66},{-140,70},{-95,72},{-82,64},{-64,60},{-56,52},{-66,48},{-70,42},{-76,35},
                 {-81,26},{-97,18},{-105,20},{-110,29},{-122,37},{-125,48},{-140,60},{-165,60},{-165,66} },
        // Greenland
        new double[,] { {-46,60},{-30,64},{-18,70},{-20,80},{-40,83},{-56,78},{-52,68} },
        // South America
        new double[,] { {-79,9},{-70,11},{-60,6},{-50,0},{-35,-6},{-38,-15},{-48,-25},{-58,-34},{-66,-45},
                 {-74,-52},{-72,-42},{-70,-30},{-76,-16},{-81,-4} },
        // Africa
        new double[,] { {-16,20},{-6,30},{10,34},{11,37},{24,32},{35,31},{43,12},{51,12},{42,-2},{40,-16},
                 {32,-27},{20,-35},{18,-34},{12,-16},{8,4},{-8,5},{-16,12} },
        // Europe
        new double[,] { {-10,43},{-9,37},{0,39},{4,43},{13,45},{18,40},{28,41},{40,47},{42,52},{30,60},
                 {24,65},{10,63},{5,58},{-2,50},{-8,49} },
        // Asia
        new double[,] { {28,41},{45,40},{48,30},{57,25},{67,24},{78,8},{80,13},{90,22},{97,16},{106,10},
                 {110,20},{105,30},{122,31},{122,40},{130,43},{142,45},{155,60},{170,66},{180,68},
                 {160,72},{120,74},{90,76},{66,70},{48,68},{38,60} },
        // Australia
        new double[,] { {114,-22},{122,-18},{130,-12},{137,-11},{143,-11},{146,-18},{150,-25},{153,-32},
                 {150,-38},{140,-38},{129,-32},{120,-34},{114,-30} },
    };

    /// <summary>The projected continents as a single SVG path <c>d</c> string (many closed rings).</summary>
    internal static readonly string LandPath = BuildLandPath();

    private static string BuildLandPath()
    {
        var sb = new StringBuilder();
        foreach (var ring in Continents)
        {
            for (var i = 0; i < ring.GetLength(0); i++)
            {
                var cmd = i == 0 ? 'M' : 'L';
                sb.Append(cmd).Append(N(X(ring[i, 0]))).Append(' ').Append(N(Y(ring[i, 1]))).Append(' ');
            }
            sb.Append("Z ");
        }
        return sb.ToString().TrimEnd();
    }

    private static string N(double v) =>
        Math.Round(v, 1).ToString(CultureInfo.InvariantCulture);

    /// <summary>A spread of major cities used when the caller doesn't supply their own.</summary>
    internal static readonly IReadOnlyList<MapCity> DefaultCities = new[]
    {
        new MapCity("Honolulu", -157.9, 21.3, "Pacific/Honolulu"),
        new MapCity("Los Angeles", -118.2, 34.1, "America/Los_Angeles"),
        new MapCity("New York", -74.0, 40.7, "America/New_York"),
        new MapCity("São Paulo", -46.6, -23.6, "America/Sao_Paulo"),
        new MapCity("London", -0.1, 51.5, "Europe/London"),
        new MapCity("Paris", 2.35, 48.9, "Europe/Paris"),
        new MapCity("Johannesburg", 28.0, -26.2, "Africa/Johannesburg"),
        new MapCity("Moscow", 37.6, 55.8, "Europe/Moscow"),
        new MapCity("Dubai", 55.3, 25.2, "Asia/Dubai"),
        new MapCity("Mumbai", 72.9, 19.1, "Asia/Kolkata"),
        new MapCity("Shanghai", 121.5, 31.2, "Asia/Shanghai"),
        new MapCity("Tokyo", 139.7, 35.7, "Asia/Tokyo"),
        new MapCity("Sydney", 151.2, -33.9, "Australia/Sydney"),
    };
}
