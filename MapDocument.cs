using System.Globalization;
using System.Text;

namespace AstoniaMapWorkbench;

internal sealed class MapTile
{
    public int X { get; init; }
    public int Y { get; init; }
    public uint GroundSprite { get; set; }
    public uint ForegroundSprite { get; set; }
    public string? Item { get; set; }
    public string? Character { get; set; }
    public HashSet<string> Flags { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> UnknownLines { get; } = [];

    public IEnumerable<ushort> SpriteComponents =>
    [
        (ushort)(GroundSprite & 0xffff), (ushort)(GroundSprite >> 16),
        (ushort)(ForegroundSprite & 0xffff), (ushort)(ForegroundSprite >> 16)
    ];

    public bool IsEmpty => GroundSprite == 0 && ForegroundSprite == 0 && Item is null && Character is null && Flags.Count == 0 && UnknownLines.Count == 0;
}

internal sealed class MapDocument
{
    internal static readonly HashSet<string> KnownFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "MF_MOVEBLOCK", "MF_SIGHTBLOCK", "MF_TMOVEBLOCK", "MF_TSIGHTBLOCK", "MF_INDOORS",
        "MF_RESTAREA", "MF_DOOR", "MF_SOUNDBLOCK", "MF_TSOUNDBLOCK", "MF_SHOUTBLOCK",
        "MF_CLAN", "MF_ARENA", "MF_PEACE", "MF_NEUTRAL", "MF_FIRETHRU", "MF_SLOWDEATH",
        "MF_NOLIGHT", "MF_NOMAGIC", "MF_UNDERWATER", "MF_NOREGEN"
    };

    internal static readonly HashSet<string> RuntimeManagedFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "MF_TMOVEBLOCK", "MF_TSIGHTBLOCK", "MF_TSOUNDBLOCK", "MF_DOOR"
    };

    public string SourcePath { get; }
    public Dictionary<(int X, int Y), MapTile> Tiles { get; } = [];
    public List<string> GlobalUnknownLines { get; } = [];
    public bool CanEdit => GlobalUnknownLines.Count == 0 && Tiles.Values.All(tile => tile.UnknownLines.Count == 0);

    private MapDocument(string sourcePath) => SourcePath = sourcePath;

    public static MapDocument Load(string path)
    {
        var document = new MapDocument(path);
        MapTile? current = null;
        (int X, int Y)? copyFrom = null;
        var originX = 0;
        var originY = 0;
        var lineNumber = 0;

        foreach (var raw in File.ReadLines(path))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0) continue;
            // A rewrite must never silently remove comments or directives from
            // a newer map grammar. Such maps remain safely inspectable only.
            if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith('#'))
            {
                document.GlobalUnknownLines.Add($"line {lineNumber}: preserved comment requires inspection-only mode");
                continue;
            }
            var equals = line.IndexOf('=');
            if (equals < 1)
            {
                document.GlobalUnknownLines.Add($"line {lineNumber}: {raw}");
                continue;
            }

            var key = line[..equals].Trim();
            var value = line[(equals + 1)..].Trim().Trim('"');
            if (key.Equals("origin", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParsePoint(value, originX, originY, out originX, out originY))
                    document.GlobalUnknownLines.Add($"line {lineNumber}: invalid origin={value}");
                continue;
            }
            if (key.Equals("field", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParsePoint(value, originX, originY, out var x, out var y))
                {
                    document.GlobalUnknownLines.Add($"line {lineNumber}: invalid field={value}");
                    current = null;
                    continue;
                }
                current = document.GetOrCreate(x, y);
                current.Clear();
                continue;
            }

            if (key.Equals("from", StringComparison.OrdinalIgnoreCase))
            {
                if (!TryParsePoint(value, originX, originY, out var x, out var y))
                    document.GlobalUnknownLines.Add($"line {lineNumber}: invalid from={value}");
                else copyFrom = (x, y);
                continue;
            }

            if (key.Equals("to", StringComparison.OrdinalIgnoreCase))
            {
                if (current is null || copyFrom is null || !TryParsePoint(value, originX, originY, out var toX, out var toY))
                {
                    document.GlobalUnknownLines.Add($"line {lineNumber}: invalid to={value}");
                    continue;
                }
                document.CopyTileToRectangle(current, copyFrom.Value, toX, toY);
                continue;
            }

            if (current is null)
            {
                document.GlobalUnknownLines.Add($"line {lineNumber}: directive before field: {raw}");
                continue;
            }

            switch (key.ToLowerInvariant())
            {
                case "gsprite": current.GroundSprite = ParseSprite(value, lineNumber, document, raw); break;
                case "fsprite": current.ForegroundSprite = ParseSprite(value, lineNumber, document, raw); break;
                case "it": current.Item = value; break;
                case "ch": current.Character = value; break;
                case "flag":
                    current.Flags.Add(value);
                    if (!KnownFlags.Contains(value)) current.UnknownLines.Add($"line {lineNumber}: unsupported flag={value}");
                    break;
                default: current.UnknownLines.Add($"line {lineNumber}: {raw}"); break;
            }
        }
        return document;
    }

    public IReadOnlyList<string> Validate(ISet<string> items, ISet<string> characters, ServerMapProfile? profile, SpriteArchive? sprites)
    {
        var findings = new List<string>();
        findings.AddRange(GlobalUnknownLines);
        foreach (var tile in Tiles.Values.OrderBy(t => t.Y).ThenBy(t => t.X))
        {
            var position = $"({tile.X},{tile.Y})";
            foreach (var line in tile.UnknownLines) findings.Add($"{position}: {line}");
            foreach (var flag in tile.Flags)
            {
                if (profile is not null && !profile.IsKnownFlag(flag)) findings.Add($"{position}: '{flag}' is not accepted by profile {profile.Revision}.");
                if ((profile?.RuntimeManagedFlags.Contains(flag, StringComparer.OrdinalIgnoreCase) ?? RuntimeManagedFlags.Contains(flag))) findings.Add($"{position}: {flag} is runtime-managed; do not author it into a static .map file.");
            }
            foreach (var sprite in tile.SpriteComponents.Where(sprite => sprite != 0).Distinct())
            {
                if (sprites?.Get(sprite) is null) findings.Add($"{position}: sprite {sprite} was not found in current client ZIPs or compatible pak art.");
            }
            if (tile.Item is not null && items.Count != 0 && !items.Contains(tile.Item)) findings.Add($"{position}: item template '{tile.Item}' was not found.");
            if (tile.Character is not null && characters.Count != 0 && !characters.Contains(tile.Character)) findings.Add($"{position}: character template '{tile.Character}' was not found.");
        }
        return findings;
    }

    public void SaveWorkspaceCopy(string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination)) File.Copy(destination, destination + ".bak", true);
        var temporary = destination + ".tmp";
        // This first workbench slice has no mutation tools yet.  Copying the
        // original verbatim prevents an older UI/schema from deleting a new
        // server directive it does not understand.
        File.Copy(SourcePath, temporary, true);
        File.Move(temporary, destination, true);
    }

    public void SaveEditedWorkspaceMap(string destination)
    {
        if (!CanEdit) throw new InvalidOperationException("This map contains directives the workbench cannot preserve. It remains inspection-only.");
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        if (File.Exists(destination)) File.Copy(destination, destination + ".bak", true);
        var temporary = destination + ".tmp";
        using (var writer = new StreamWriter(temporary, false, new UTF8Encoding(false)))
        {
            foreach (var tile in Tiles.Values.Where(tile => !tile.IsEmpty).OrderBy(tile => tile.Y).ThenBy(tile => tile.X))
            {
                writer.WriteLine($"field=\"{tile.X},{tile.Y}\"");
                if (tile.GroundSprite != 0) writer.WriteLine($"gsprite={unchecked((int)tile.GroundSprite)}");
                if (tile.ForegroundSprite != 0) writer.WriteLine($"fsprite={unchecked((int)tile.ForegroundSprite)}");
                if (tile.Item is not null) writer.WriteLine($"it={tile.Item}");
                if (tile.Character is not null) writer.WriteLine($"ch={tile.Character}");
                foreach (var flag in tile.Flags.OrderBy(flag => flag)) writer.WriteLine($"flag={flag}");
                writer.WriteLine();
            }
        }
        File.Move(temporary, destination, true);
    }

    public MapTile GetTile(int x, int y) => GetOrCreate(x, y);

    private MapTile GetOrCreate(int x, int y)
    {
        if (!Tiles.TryGetValue((x, y), out var tile))
        {
            tile = new MapTile { X = x, Y = y };
            Tiles[(x, y)] = tile;
        }
        return tile;
    }

    private void CopyTileToRectangle(MapTile source, (int X, int Y) from, int toX, int toY)
    {
        var minX = Math.Min(from.X, toX);
        var maxX = Math.Max(from.X, toX);
        var minY = Math.Min(from.Y, toY);
        var maxY = Math.Max(from.Y, toY);
        for (var y = minY; y <= maxY; y++) for (var x = minX; x <= maxX; x++)
        {
            var target = GetOrCreate(x, y);
            target.CopyFrom(source);
        }
    }

    private static bool TryParsePoint(string value, int originX, int originY, out int x, out int y)
    {
        var parts = value.Split(',', 2, StringSplitOptions.TrimEntries);
        x = y = 0;
        if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawX) || !int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var rawY)) return false;
        x = rawX + originX;
        y = rawY + originY;
        return x is >= 0 and <= 255 && y is >= 0 and <= 255;
    }

    private static uint ParseSprite(string value, int lineNumber, MapDocument document, string raw)
    {
        if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var signedSprite)) return unchecked((uint)signedSprite);
        if (uint.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sprite)) return sprite;
        document.GlobalUnknownLines.Add($"line {lineNumber}: invalid sprite value: {raw}");
        return 0;
    }
}

internal static class MapTileExtensions
{
    public static void Clear(this MapTile tile)
    {
        tile.GroundSprite = 0;
        tile.ForegroundSprite = 0;
        tile.Item = null;
        tile.Character = null;
        tile.Flags.Clear();
        tile.UnknownLines.Clear();
    }

    public static void CopyFrom(this MapTile target, MapTile source)
    {
        target.GroundSprite = source.GroundSprite;
        target.ForegroundSprite = source.ForegroundSprite;
        target.Item = source.Item;
        target.Character = source.Character;
        target.Flags.Clear();
        foreach (var flag in source.Flags) target.Flags.Add(flag);
        target.UnknownLines.Clear();
        target.UnknownLines.AddRange(source.UnknownLines);
    }
}
