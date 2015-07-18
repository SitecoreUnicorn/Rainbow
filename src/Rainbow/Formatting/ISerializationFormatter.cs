using System.IO;
using Rainbow.Model;

namespace Rainbow.Formatting
{
	public interface ISerializationFormatter
	{
		IItemData ReadSerializedItem(Stream dataStream, string serializedItemId);
		void WriteSerializedItem(IItemData itemData, Stream outputStream);

		string FileExtension { get; }
	}
}
