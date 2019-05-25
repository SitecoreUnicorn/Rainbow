namespace Rainbow.Storage
{
	public interface IFieldValueTransformer
	{
		bool ShouldDeployFieldValue(string existingValue, string proposedValue);
		string GetFieldValue(string existingValue, string proposedValue);
	}
}
