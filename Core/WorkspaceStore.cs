using System.Text.Json;

namespace PrinceWM.Core;

internal static class WorkspaceStore
{
    private static readonly string Dir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PrinceWM", "workspaces");

    public static List<Workspace> Load()
    {
        var list = new List<Workspace>();
        try
        {
            if (!Directory.Exists(Dir)) return list;
            foreach (var file in Directory.GetFiles(Dir, "*.json"))
            {
                try
                {
                    var ws = JsonSerializer.Deserialize<Workspace>(File.ReadAllText(file));
                    if (ws != null && ws.Members.Count >= 1) list.Add(ws);
                }
                catch (Exception ex) { Log.Ex("WorkspaceStore.Load.file", ex); }
            }
        }
        catch (Exception ex) { Log.Ex("WorkspaceStore.Load", ex); }
        return list;
    }

    public static void SaveAll(List<Workspace> workspaces)
    {
        try
        {
            Directory.CreateDirectory(Dir);
            var keep = new HashSet<string>();
            foreach (var ws in workspaces)
            {
                keep.Add(ws.Id);
                File.WriteAllText(Path.Combine(Dir, ws.Id + ".json"), JsonSerializer.Serialize(ws));
            }
            foreach (var file in Directory.GetFiles(Dir, "*.json"))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                if (!keep.Contains(id)) { try { File.Delete(file); } catch { } }
            }
        }
        catch (Exception ex) { Log.Ex("WorkspaceStore.SaveAll", ex); }
    }
}
