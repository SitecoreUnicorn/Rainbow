namespace Rainbow.Storage
{
	public interface IFieldValueManipulator
	{
		IFieldValueTransformer GetFieldValueTransformer(string fieldName);
		IFieldValueTransformer[] GetFieldValueTransformers();
		string[] GetFieldNamesInManipulator();
	}
}
