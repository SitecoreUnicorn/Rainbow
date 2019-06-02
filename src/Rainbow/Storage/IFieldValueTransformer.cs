namespace Rainbow.Storage
{
	public interface IFieldValueTransformer
	{
		string FieldName { get; }
		bool ShouldDeployFieldValue(string existingValue, string proposedValue);
		string GetFieldValue(string existingValue, string proposedValue);
		string Description { get; }
	}
}
