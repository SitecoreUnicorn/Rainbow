using Rainbow.Model;

namespace Rainbow.Storage.Sc.Deserialization
{
	public interface IDeserializer
	{
		IItemData Deserialize(IItemData serializedItemData, IFieldValueManipulator fieldValueManipulator);
		IDataStore ParentDataStore { set; }
	}
}