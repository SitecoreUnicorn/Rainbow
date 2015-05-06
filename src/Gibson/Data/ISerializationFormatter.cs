using System.IO;
using Gibson.Model;

namespace Gibson.Data
{
	public interface ISerializationFormatter
	{
		ISerializableItem ReadSerializedItem(Stream dataStream);
		void WriteSerializedItem(ISerializableItem item, Stream outputStream);
	}
}
