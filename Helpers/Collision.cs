using System.Numerics;

namespace PrinceWM.Helpers;

internal static class Collision
{
    public static bool Overlaps(Vector2 ap, Vector2 asz, Vector2 bp, Vector2 bsz, float gap)
        => ap.X < bp.X + bsz.X + gap && ap.X + asz.X + gap > bp.X &&
           ap.Y < bp.Y + bsz.Y + gap && ap.Y + asz.Y + gap > bp.Y;

    private static bool OverlapsAny(Vector2 pos, Vector2 size, List<(Vector2 pos, Vector2 size)> obstacles, float gap)
    {
        foreach (var (op, os) in obstacles)
            if (Overlaps(pos, size, op, os, gap)) return true;
        return false;
    }

    public static Vector2 Resolve(Vector2 pos, Vector2 size, List<(Vector2 pos, Vector2 size)> obstacles, float gap)
    {
        if (!OverlapsAny(pos, size, obstacles, gap)) return pos;

        var step = MathF.Max(24f, MathF.Min(size.X, size.Y) * 0.35f);
        for (var ring = 1; ring <= 80; ring++)
        {
            var rad = ring * step;
            Vector2 best = default;
            var bestDist = float.MaxValue;

            for (var a = 0; a < 24; a++)
            {
                var ang = a / 24f * MathF.Tau;
                var cand = pos + (new Vector2(MathF.Cos(ang), MathF.Sin(ang)) * rad);
                if (OverlapsAny(cand, size, obstacles, gap)) continue;
                var d = Vector2.DistanceSquared(cand, pos);
                if (d < bestDist) { bestDist = d; best = cand; }
            }
            if (bestDist < float.MaxValue) return best;
        }
        return pos;
    }
}