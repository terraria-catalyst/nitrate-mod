using System;
using System.IO;
using System.Threading.Tasks;

namespace ReLogic.Content.Readers;

/// <summary>
///		Handles the reading of an asset from a stream.
/// </summary>
public interface IAssetReader
{
	/// <summary>
	///		Reads an asset of type <typeparamref name="T"/> from a <paramref name="stream"/>.
	/// </summary>
	/// <typeparam name="T">The asset type.</typeparam>
	/// <param name="stream">The asset file stream.</param>
	/// <param name="mainThreadCtx">The main thread context.</param>
	/// <returns>A value task containing the loaded asset.</returns>
	ValueTask<T> FromStream<T>(Stream stream, MainThreadCreationContext mainThreadCtx) where T : class => ValueTask.FromResult(FromStream<T>(stream));

	/// <summary>
	///		A synchronous alternative to <see cref="FromStream{T}(Stream, MainThreadCreationContext)"/>.
	/// </summary>
	/// <typeparam name="T">The asset type.</typeparam>
	/// <param name="stream">The asset file stream.</param>
	/// <returns>The loaded asset.</returns>
	protected T FromStream<T>(Stream stream) where T : class => throw new NotImplementedException();
}
