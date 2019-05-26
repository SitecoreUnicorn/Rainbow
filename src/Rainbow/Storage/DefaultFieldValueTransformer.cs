namespace Rainbow.Storage
{
	// The Rainbow default Field Value Transformer behaves exactly like Rainbow under normal operations

	public class DefaultFieldValueTransformer : IFieldValueTransformer
	{
		public bool ShouldDeployFieldValue(string existingValue, string proposedValue)
		{
			return !existingValue.Equals(proposedValue);
		}

		public string GetFieldValue(string existingValue, string proposedValue)
		{
			return proposedValue;
		}
	}
}
