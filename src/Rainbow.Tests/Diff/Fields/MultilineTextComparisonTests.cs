using Xunit;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public class MultilineTextComparisonTests : FieldComparerTest
	{
		[Theory,
			InlineData("hello", "hello"),
			InlineData("hello\r\nthere", "hello\r\nthere"),
			InlineData("hello\nthere", "hello\r\nthere")]
		public void ReturnsTrue_WhenEqualStrings(string val1, string val2)
		{
			var comparison = new MultiLineTextComparison();

			RunComparer(comparison, val1, val2, true);
		}

		[Theory,
			InlineData("hello", "Hello"),
			InlineData("hello", "goodbye"),
			InlineData("hello\r\ngoodbye", "hello\nthere"),
			InlineData("hello\r\nthere\nsir", "hello\r\nthar\nyarr")]
		public void ReturnsFalse_WhenUnequalStrings(string val1, string val2)
		{
			var comparison = new MultiLineTextComparison();

			RunComparer(comparison, val1, val2, false);
		}

		[Theory,
			InlineData("memo", true),
			InlineData("Single-Line Text", false),
			InlineData("Rich text", true),
			InlineData("Multi-Line Text", true)]
		public void CanCompare_MultilineFieldTypes(string fieldType, bool expected)
		{
			var comparison = new MultiLineTextComparison();

			Assert.Equal(expected, comparison.CanCompare(new FakeFieldValue("foo", fieldType), new FakeFieldValue("foo", fieldType)));
		}
	}
}
