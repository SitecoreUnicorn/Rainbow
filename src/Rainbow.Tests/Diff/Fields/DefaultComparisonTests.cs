using NUnit.Framework;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public class DefaultComparisonTests : FieldComparerTest
	{
		[Test, TestCase("Hello", "Hello"), TestCase("hello", "hello"), TestCase("123", "123")]
		public void DefaultComparison_ReturnsTrue_WhenEqualStrings(string val1, string val2)
		{
			var comparison = new DefaultComparison();

			RunComparer(comparison, val1, val2, true);
		}

		[Test, TestCase("Hello", "hello"), TestCase("hello", "goodbye"), TestCase("123", "1234")]
		public void DefaultComparison_ReturnsFalse_WhenUnequalStrings(string val1, string val2)
		{
			var comparison = new DefaultComparison();

			RunComparer(comparison, val1, val2, false);
		}
	}
}
