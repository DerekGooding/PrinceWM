namespace PrinceWM.Model;

internal sealed class Workspace
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Workspace";
    public int Tint { get; set; } = Theme.Rgb(135, 139, 146);
    public List<string> Members { get; set; } = new();
    public List<WsLink> Links { get; set; } = new();
    public Dictionary<string, GhostInfo> Info { get; set; } = new();

    public bool Has(string appKey) => Members.Contains(appKey);

    public void AddMember(string appKey)
    {
        if (!string.IsNullOrEmpty(appKey) && !Members.Contains(appKey)) Members.Add(appKey);
    }

    public void Connect(string a, string b)
    {
        AddMember(a);
        AddMember(b);
        if (a != b && !Links.Exists(l => (l.A == a && l.B == b) || (l.A == b && l.B == a)))
            Links.Add(new WsLink { A = a, B = b });
    }

    public void RemoveMember(string appKey)
    {
        Members.Remove(appKey);
        Links.RemoveAll(l => l.A == appKey || l.B == appKey);
        Info.Remove(appKey);
    }
}