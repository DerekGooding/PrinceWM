namespace PrinceWM.Model;

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