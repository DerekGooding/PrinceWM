using System.Text;

namespace PrinceWM.Helpers;

internal static class SnapStore
{
    private static readonly string _dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrinceWM", "snaps");

    private static string FileFor(string appKey)
    {
        var sb = new StringBuilder(appKey.Length);
        foreach (var c in appKey) sb.Append(char.IsLetterOrDigit(c) ? c : '_');
        var name = sb.ToString();
        if (name.Length > 96) name = name[..96];
        return Path.Combine(_dir, name + ".jpg");
    }

    public static string? PathIfExists(string appKey)
    {
        var p = FileFor(appKey);
        return File.Exists(p) ? p : null;
    }

    public static void Save(string appKey, Bitmap bmp)
    {
        try
        {
            Directory.CreateDirectory(_dir);
            bmp.Save(FileFor(appKey), System.Drawing.Imaging.ImageFormat.Jpeg);
        }
        catch (Exception ex) { Log.Ex("SnapStore.Save", ex); }
    }
}