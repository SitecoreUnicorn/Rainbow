using System.IO;
using Gibson.Model;

namespace Gibson.SerializationFormatting
{
	public interface ISerializationFormatter
	{
		ISerializableItem ReadSerializedItem(Stream dataStream, string serializedItemId);
		void WriteSerializedItem(ISerializableItem item, Stream outputStream);
	}
}
