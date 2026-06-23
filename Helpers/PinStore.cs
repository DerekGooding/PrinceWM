using System.Text.Json;

namespace PrinceWM.Helpers;

internal static class PinStore
{
    private static readonly string _dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrinceWM");

    private static readonly string _filePath = Path.Combine(_dir, "pins.json");

    public static string ImagesDir => Path.Combine(_dir, "pins");

    public static List<Pin> Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return [];
            var list = JsonSerializer.Deserialize<List<Pin>>(File.ReadAllText(_filePath));
            if (list == null) return [];

            list.RemoveAll(p => p.Kind == PinKind.Image &&
                (string.IsNullOrEmpty(p.ImageFile) || !File.Exists(Path.Combine(ImagesDir, p.ImageFile))));
            return list;
        }
        catch (Exception ex)
        {
            Log.Ex("PinStore.Load", ex);
            return [];
        }
    }

    public static void Save(List<Pin> pins)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(pins));
        }
        catch (Exception ex) { Log.Ex("PinStore.Save", ex); }
    }

    public static string? SaveImage(Image img)
    {
        try
        {
            Directory.CreateDirectory(ImagesDir);
            var name = Guid.NewGuid().ToString("N") + ".png";
            img.Save(Path.Combine(ImagesDir, name), System.Drawing.Imaging.ImageFormat.Png);
            return name;
        }
        catch (Exception ex) { Log.Ex("PinStore.SaveImage", ex); return null; }
    }

    public static void DeleteImage(string? file)
    {
        if (string.IsNullOrEmpty(file)) return;
        try { File.Delete(Path.Combine(ImagesDir, file)); } catch { }
    }
}