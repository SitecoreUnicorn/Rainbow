using Xunit;
using Rainbow.Diff;

namespace Rainbow.Tests.Diff
{
	public class ItemVersionComparisonResultTests
	{
		[Fact]
		public void IVCR_ReturnsSourceVersion_WhenTargetVersionIsNull()
		{
			var source = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(source, null, null);

			Assert.Equal(comparison.VersionNumber, source.VersionNumber);
		}

		[Fact]
		public void IVCR_ReturnsTargetVersion_WhenSourceVersionIsNull()
		{
			var target = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(null, target, null);

			Assert.Equal(comparison.VersionNumber, target.VersionNumber);
		}

		[Fact]
		public void IVCR_ReturnsSourceLanguage_WhenTargetVersionIsNull()
		{
			var source = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(source, null, null);

			Assert.Equal(comparison.Language, source.Language);
		}

		[Fact]
		public void IVCR_ReturnsTargetLanguage_WhenSourceVersionIsNull()
		{
			var target = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(null, target, null);

			Assert.Equal(comparison.Language, target.Language);
		}
	}
}
