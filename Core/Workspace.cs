namespace PrinceWM.Core;

internal sealed class WsLink
{
    public string A { get; set; } = "";
    public string B { get; set; } = "";
}

internal sealed class GhostInfo
{
    public string Title { get; set; } = "";
    public string Exe { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public float W { get; set; }
    public float H { get; set; }
}

internal sealed class GhostTile
{
    public string AppKey = "";
    public string Title = "";
    public string Exe = "";
    public int Tint;
    public System.Numerics.Vector2 Pos;
    public System.Numerics.Vector2 Size;
    public System.Numerics.Vector2 Center => Pos + Size * 0.5f;
    public bool Contains(System.Numerics.Vector2 p) =>
        p.X >= Pos.X && p.Y >= Pos.Y && p.X <= Pos.X + Size.X && p.Y <= Pos.Y + Size.Y;
}

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
