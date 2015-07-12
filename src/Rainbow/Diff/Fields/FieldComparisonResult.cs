using Rainbow.Model;

namespace Rainbow.Diff.Fields
{
	public class FieldComparisonResult
	{
		public FieldComparisonResult(IItemFieldValue sourceField, IItemFieldValue targetField)
		{ 
			SourceField = sourceField;
			TargetField = targetField;
		}

		public IItemFieldValue SourceField { get; private set; }
		public IItemFieldValue TargetField { get; private set; }
	}
}
