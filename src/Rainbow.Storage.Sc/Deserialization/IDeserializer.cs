using Rainbow.Model;

namespace Rainbow.Storage.Sc.Deserialization
{
	public interface IDeserializer
	{
		ISerializableItem Deserialize(ISerializableItem serializedItem, bool ignoreMissingTemplateFields);
	}
}