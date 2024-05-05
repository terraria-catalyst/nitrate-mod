using System;
using System.Collections.Generic;
using System.Linq;

namespace ReLogic.Content.Readers;

internal static class Contracts
{
	public static void AssertValidReaderForAssetType<TReader, TActual, TExpected>()
	{
		AssertValidReaderForAssetType<TReader, TActual>(typeof(TExpected));
	}

	public static void AssertValidReaderForAssetType<TReader, TActual>(Type expected)
	{
		AssertValidReaderForAssetType<TReader, TActual>([expected]);
	}

	public static void AssertValidReaderForAssetType<TReader, TActual>(IEnumerable<Type> expectedTypes)
	{
		if (!expectedTypes.Contains(typeof(TActual)))
			throw AssetLoadException.FromInvalidReader<TReader, TActual>();
	}
}
