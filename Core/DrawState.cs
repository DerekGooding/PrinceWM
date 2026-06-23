namespace PrinceWM.Core;

internal sealed class DrawState
{
    public bool Active;
    public DrawTool Tool;
    public int Color = 0xFFFFFF;
    public float Size = 4f;
    public IReadOnlyList<Stroke>? Strokes;
    public Stroke? InProgress;
    public bool HoverToggle;
    public int HoverButton = -1;
}
