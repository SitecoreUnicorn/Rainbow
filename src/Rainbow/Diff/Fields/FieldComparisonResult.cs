using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Diff.Fields
{
	public class FieldComparisonResult
	{
		public FieldComparisonResult(ISerializableFieldValue sourceField, ISerializableFieldValue targetField)
		{
			Assert.ArgumentNotNull(sourceField, "sourceField");
			Assert.ArgumentNotNull(targetField, "targetField");

			SourceField = sourceField;
			TargetField = targetField;
		}

		public ISerializableFieldValue SourceField { get; private set; }
		public ISerializableFieldValue TargetField { get; private set; }
	}
}
