using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Diff.Fields
{
	public class FieldComparisonResult
	{
		public FieldComparisonResult(IItemFieldValue sourceField, IItemFieldValue targetField)
		{
			Assert.ArgumentNotNull(sourceField, "sourceField");
			Assert.ArgumentNotNull(targetField, "targetField");

			SourceField = sourceField;
			TargetField = targetField;
		}

		public IItemFieldValue SourceField { get; private set; }
		public IItemFieldValue TargetField { get; private set; }
	}
}
