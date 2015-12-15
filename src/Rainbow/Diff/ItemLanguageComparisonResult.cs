using System.Collections.Generic;
using Rainbow.Diff.Fields;
using Rainbow.Model;

namespace Rainbow.Diff
{
	public class ItemLanguageComparisonResult
	{
		public ItemLanguageComparisonResult(IItemLanguage language, FieldComparisonResult[] changedFields)
		{
			Language = language;
			ChangedFields = new List<FieldComparisonResult>(changedFields ?? new FieldComparisonResult[0]);
		}

		public IItemLanguage Language { get; }
		public IList<FieldComparisonResult> ChangedFields { get; private set; }
	}
}
