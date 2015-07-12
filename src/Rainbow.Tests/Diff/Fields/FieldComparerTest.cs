using NUnit.Framework;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public abstract class FieldComparerTest
	{
		protected void RunComparer(IFieldComparer comparer, string val1, string val2, bool expectedResult)
		{
			var field1 = new FakeFieldValue(val1);
			var field2 = new FakeFieldValue(val2);

			Assert.AreEqual(expectedResult, comparer.AreEqual(field1, field2));
		}
	}
}
