using Gibson.Model;

namespace Gibson.Sc.Deserialization
{
	public interface IDeserializer
	{
		ISerializableItem Deserialize(ISerializableItem serializedItem, bool ignoreMissingTemplateFields);
	}
}