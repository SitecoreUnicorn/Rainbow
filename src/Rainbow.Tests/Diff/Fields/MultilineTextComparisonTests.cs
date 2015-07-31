using NUnit.Framework;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public class MultilineTextComparisonTests : FieldComparerTest
	{
		[Test,
			TestCase("hello", "hello"),
			TestCase("hello\r\nthere", "hello\r\nthere"),
			TestCase("hello\nthere", "hello\r\nthere")]
		public void MultilineComparison_ReturnsTrue_WhenEqualStrings(string val1, string val2)
		{
			var comparison = new MultiLineTextComparison();

			RunComparer(comparison, val1, val2, true);
		}

		[Test,
			TestCase("hello", "Hello"),
			TestCase("hello", "goodbye"),
			TestCase("hello\r\ngoodbye", "hello\nthere"),
			TestCase("hello\r\nthere\nsir", "hello\r\nthar\nyarr")]
		public void MultilineComparison_ReturnsFalse_WhenUnequalStrings(string val1, string val2)
		{
			var comparison = new MultiLineTextComparison();

			RunComparer(comparison, val1, val2, false);
		}

		[Test,
			TestCase("memo", true),
			TestCase("Single-Line Text", false),
			TestCase("Rich text", true),
			TestCase("Multi-Line Text", true)]
		public void MultilineComparison_CanCompare_MultilineFieldTypes(string fieldType, bool expected)
		{
			var comparison = new MultiLineTextComparison();

			Assert.AreEqual(expected, comparison.CanCompare(new FakeFieldValue("foo", fieldType), new FakeFieldValue("foo", fieldType)));
		}
	}
}
