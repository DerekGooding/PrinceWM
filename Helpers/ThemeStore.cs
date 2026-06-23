using System.Text.Json;

namespace PrinceWM.Helpers;

internal static class ThemeStore
{
    private static readonly string _dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrinceWM");

    private static readonly string _filePath = Path.Combine(_dir, "theme.json");

    public static Theme Load()
    {
        try
        {
            if (File.Exists(_filePath))
                return JsonSerializer.Deserialize<Theme>(File.ReadAllText(_filePath)) ?? new Theme();
        }
        catch (Exception ex)
        {
            Log.Ex("ThemeStore.Load", ex);
        }
        return new Theme();
    }

    public static void Save(Theme theme)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(theme));
        }
        catch (Exception ex)
        {
            Log.Ex("ThemeStore.Save", ex);
        }
    }
}