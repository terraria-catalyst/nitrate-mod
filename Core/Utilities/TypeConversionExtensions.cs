using Microsoft.Xna.Framework;

namespace Zenith.Core.Utilities;

public static class TypeConversionExtensions
{
    public static Vector3 ToVector3(this Vector2 vector2) => new(vector2.X, vector2.Y, 0);
}