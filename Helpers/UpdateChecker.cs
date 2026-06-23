using System.Reflection;
using System.Text.Json;

namespace PrinceWM.Helpers;

internal static class UpdateChecker
{
    private const string Repo = "Ravestars/PrinceWM";

    private static readonly string _dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrinceWM");

    private static readonly string _optOutPath = Path.Combine(_dir, "updates-disabled");

    public static string ReleasesUrl => $"https://github.com/{Repo}/releases/latest";

    public static bool OptedOut => File.Exists(_optOutPath);

    public static void DisableForever()
    {
        try
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_optOutPath, "1");
        }
        catch (Exception ex) { Log.Ex("UpdateChecker.DisableForever", ex); }
    }

    public static Version Current
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v ?? new Version(0, 0, 0, 0);
        }
    }

    public static async Task<string?> GetNewerTagAsync()
    {
        if (OptedOut) return null;
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("PrinceWM");
            var json = await http.GetStringAsync($"https://api.github.com/repos/{Repo}/releases/latest");

            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("tag_name", out var tagEl)) return null;
            var tag = tagEl.GetString();
            if (string.IsNullOrWhiteSpace(tag)) return null;

            var num = tag.TrimStart('v', 'V');
            if (!Version.TryParse(num, out var latest)) return null;

            var cur = Current;
            var curNorm = new Version(cur.Major, Math.Max(0, cur.Minor), Math.Max(0, cur.Build));
            var latNorm = new Version(latest.Major, Math.Max(0, latest.Minor), Math.Max(0, latest.Build));
            return latNorm > curNorm ? tag : null;
        }
        catch
        {
            return null;
        }
    }
}