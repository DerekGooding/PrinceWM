using System.Numerics;
using System.Text.Json;

namespace PrinceWM.Helpers;

internal static class SizeStore
{
    private static readonly string _dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrinceWM");

    private static readonly string _filePath = Path.Combine(_dir, "sizes.json");

    public static Dictionary<string, Vector2> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return [];
            var json = File.ReadAllText(_filePath);
            var raw = JsonSerializer.Deserialize<Dictionary<string, float[]>>(json);
            if (raw == null) return [];
            var dict = new Dictionary<string, Vector2>();
            foreach (var (k, v) in raw)
                if (v.Length == 2) dict[k] = new Vector2(v[0], v[1]);
            return dict;
        }
        catch (Exception ex)
        {
            Log.Ex("SizeStore.Load", ex);
            return [];
        }
    }

    public static void Save(Dictionary<string, Vector2> sizes)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            var raw = new Dictionary<string, float[]>();
            foreach (var (k, v) in sizes)
                raw[k] = [v.X, v.Y];
            File.WriteAllText(_filePath, JsonSerializer.Serialize(raw));
        }
        catch (Exception ex)
        {
            Log.Ex("SizeStore.Save", ex);
        }
    }
}