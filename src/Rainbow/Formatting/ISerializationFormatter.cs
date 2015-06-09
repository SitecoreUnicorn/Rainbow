using System.IO;
using Rainbow.Model;

namespace Rainbow.Formatting
{
	public interface ISerializationFormatter
	{
		ISerializableItem ReadSerializedItem(Stream dataStream, string serializedItemId);
		void WriteSerializedItem(ISerializableItem item, Stream outputStream);
	}
}
