using Gibson.Model;

namespace Gibson.Deserialization
{
	public interface IDeserializer
	{
		ISerializableItem Deserialize(ISerializableItem serializedItem, bool ignoreMissingTemplateFields);
	}
}