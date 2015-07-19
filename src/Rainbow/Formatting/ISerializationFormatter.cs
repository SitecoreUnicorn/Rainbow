using System.IO;
using Rainbow.Model;
using Rainbow.Storage;

namespace Rainbow.Formatting
{
	public interface ISerializationFormatter
	{
		IItemData ReadSerializedItem(Stream dataStream, string serializedItemId);
		void WriteSerializedItem(IItemData itemData, Stream outputStream);
		string FileExtension { get; }
		IDataStore ParentDataStore { set; }
	}
}
