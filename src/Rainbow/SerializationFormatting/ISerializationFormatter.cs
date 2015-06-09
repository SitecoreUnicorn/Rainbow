using System.IO;
using Rainbow.Model;

namespace Rainbow.SerializationFormatting
{
	public interface ISerializationFormatter
	{
		ISerializableItem ReadSerializedItem(Stream dataStream, string serializedItemId);
		void WriteSerializedItem(ISerializableItem item, Stream outputStream);
	}
}
