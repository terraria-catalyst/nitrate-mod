using Terraria.Utilities;

namespace Terraria;

partial class Utils
{
	internal static bool NextBool(this FastRandom random)
	{
		return random.NextDouble() < 0.5D;
	}

	internal static bool NextBool(this FastRandom random, int a)
	{
		return random.Next(a) == 0;
	}

	internal static bool NextBool(this FastRandom random, int a, int b)
	{
		return random.Next(b) < a;
	}
}
