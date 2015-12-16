using Xunit;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public class DefaultComparisonTests : FieldComparerTest
	{
		[Theory, 
			InlineData("Hello", "Hello"),
			InlineData("hello", "hello"),
			InlineData("123", "123")]
		public void ReturnsTrue_WhenEqualStrings(string val1, string val2)
		{
			var comparison = new DefaultComparison();

			RunComparer(comparison, val1, val2, true);
		}

		[Theory, 
			InlineData("Hello", "hello"), 
			InlineData("hello", "goodbye"), 
			InlineData("123", "1234")]
		public void ReturnsFalse_WhenUnequalStrings(string val1, string val2)
		{
			var comparison = new DefaultComparison();

			RunComparer(comparison, val1, val2, false);
		}
	}
}
