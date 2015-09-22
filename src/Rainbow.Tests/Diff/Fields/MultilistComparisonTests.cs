using Xunit;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public class MultilistComparisonTests : FieldComparerTest
	{
		[Theory,
			InlineData("hello", "hello"),
			InlineData("{1172F251-DAD4-4EFB-A329-0C63500E4F1E}", "{1172F251-DAD4-4EFB-A329-0C63500E4F1E}"),
			InlineData("{1172F251-DAD4-4EFB-A329-0C63500E4F1E}|{83798D75-DF25-4C28-9327-E8BAC2B75292}", "{1172F251-DAD4-4EFB-A329-0C63500E4F1E}|{83798D75-DF25-4C28-9327-E8BAC2B75292}"),
			InlineData("|{1172F251-DAD4-4EFB-A329-0C63500E4F1E}|{83798D75-DF25-4C28-9327-E8BAC2B75292}|", "{1172F251-DAD4-4EFB-A329-0C63500E4F1E}|{83798D75-DF25-4C28-9327-E8BAC2B75292}")]
		public void ShouldReturnTrue_WhenEqualStrings(string val1, string val2)
		{
			var comparison = new MultilistComparison();

			RunComparer(comparison, val1, val2, true);
		}

		[Theory,
			InlineData("hello", "Hello"),
			InlineData("hello", "goodbye"),
			InlineData("hello\r\ngoodbye", "hello\nthere"),
			InlineData("hello\r\nthere\nsir", "hello\r\nthar\nyarr")]
		public void ShouldReturnFalse_WhenUnequalStrings(string val1, string val2)
		{
			var comparison = new MultilistComparison();

			RunComparer(comparison, val1, val2, false);
		}

		[Theory,
			InlineData("TreelistEx", true),
			InlineData("Treelist", true),
			InlineData("Multilist", true),
			InlineData("Multi-Line Text", false)]
		public void CanCompare_MultilistFieldTypes(string fieldType, bool expected)
		{
			var comparison = new MultilistComparison();

			Assert.Equal(expected, comparison.CanCompare(new FakeFieldValue("foo", fieldType), new FakeFieldValue("foo", fieldType)));
		}
	}
}
