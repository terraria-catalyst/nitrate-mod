namespace Nitrate.Optimizations.Tiles;

public struct AnimatedPoint
{
    public int X { get; set; }

    public int Y { get; set; }

    public AnimatedPointType Type { get; set; }

    public AnimatedPoint(int x, int y, AnimatedPointType type)
    {
        X = x;
        Y = y;
        Type = type;
    }
}

public enum AnimatedPointType
{
    AnimatedTile,
}