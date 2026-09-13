using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AstoniaMapWorkbench;

internal sealed record ServerMapProfile(
    string Name,
    string ServerRoot,
    string Revision,
    DateTimeOffset GeneratedUtc,
    List<string> Directives,
    List<string> AuthorableFlags,
    List<string> RuntimeManagedFlags,
    string ZoneRoot)
{
    public static readonly string[] RequiredDirectives = ["field", "origin", "from", "to", "gsprite", "fsprite", "flag", "ch", "it"];
    private static readonly HashSet<string> DynamicFlags = new(StringComparer.OrdinalIgnoreCase)
    {
        "MF_TMOVEBLOCK", "MF_TSIGHTBLOCK", "MF_TSOUNDBLOCK", "MF_DOOR"
    };

    public static ServerMapProfile Generate(string serverRoot)
    {
        var create = Path.Combine(serverRoot, "create.c");
        if (!File.Exists(create)) throw new FileNotFoundException("This folder does not contain Server 3 create.c.", create);
        var source = File.ReadAllText(create);
        var table = Regex.Match(source, @"static\s+char\s+\*MF_tab\[\]\s*=\s*\{(?<body>.*?)\};", RegexOptions.Singleline);
        if (!table.Success) throw new InvalidDataException("Could not find Server 3's MF_tab map-flag table in create.c.");
        var flags = Regex.Matches(table.Groups["body"].Value, "\"(?<flag>MF_[A-Z0-9_]+)\"")
            .Select(match => match.Groups["flag"].Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (flags.Count == 0) throw new InvalidDataException("Server 3's MF_tab did not contain map flags.");
        var root = Path.GetFullPath(serverRoot);
        return new ServerMapProfile(
            "Server 3 map profile",
            root,
            GetRevision(root),
            DateTimeOffset.UtcNow,
            RequiredDirectives.ToList(),
            flags.Where(flag => !DynamicFlags.Contains(flag)).ToList(),
            flags.Where(DynamicFlags.Contains).ToList(),
            Path.Combine(root, "zones"));
    }

    public static ServerMapProfile Load(string path) => JsonSerializer.Deserialize<ServerMapProfile>(File.ReadAllText(path))
        ?? throw new InvalidDataException("The profile is empty or invalid.");

    public void Save(string path) => File.WriteAllText(path, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));

    public bool IsKnownFlag(string flag) => AuthorableFlags.Contains(flag, StringComparer.OrdinalIgnoreCase) || RuntimeManagedFlags.Contains(flag, StringComparer.OrdinalIgnoreCase);

    private static string GetRevision(string serverRoot)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo("git", $"-C \"{serverRoot}\" rev-parse --short HEAD")
            { RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true });
            process?.WaitForExit(3000);
            var revision = process?.StandardOutput.ReadToEnd().Trim();
            return string.IsNullOrWhiteSpace(revision) ? "unversioned" : revision;
        }
        catch { return "unversioned"; }
    }
}
