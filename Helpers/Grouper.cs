namespace PrinceWM.Helpers;

internal static class Grouper
{
    public static List<WindowItem> Stack(List<WindowItem> items, List<IntPtr> mru,
        HashSet<IntPtr>? promoted = null)
    {
        int MruIndex(IntPtr h)
        {
            var i = mru.IndexOf(h);
            return i < 0 ? int.MaxValue : i;
        }

        var groups = new Dictionary<string, List<WindowItem>>(StringComparer.Ordinal);
        foreach (var it in items)
        {
            var key = promoted?.Contains(it.Hwnd) == true
                ? $"{it.AppKey}#{it.Hwnd}"
                : it.AppKey;
            if (!groups.TryGetValue(key, out var list)) { list = []; groups[key] = list; }
            list.Add(it);
        }

        var result = new List<WindowItem>(groups.Count);
        foreach (var (key, list) in groups)
        {
            list.Sort((a, b) => MruIndex(a.Hwnd).CompareTo(MruIndex(b.Hwnd)));
            var rep = list[0];
            rep.AppKey = key;
            rep.StackCount = list.Count;
            rep.StackHwnds = list.ConvertAll(x => x.Hwnd);
            result.Add(rep);
        }
        return result;
    }
}