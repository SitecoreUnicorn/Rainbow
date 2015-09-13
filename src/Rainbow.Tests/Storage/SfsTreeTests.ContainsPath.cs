using Xunit;

namespace Rainbow.Tests.Storage
{
    partial class SfsTreeTests
    {
		[Fact]
		public void ContainsPath_ReturnsTrue_WhenPathIsRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.True(testTree.ContainsPath("/sitecore/content"));
			}
		}

		[Fact]
		public void ContainsPath_ReturnsTrue_WhenExpectedPath()
		{
			using (var testTree = new TestSfsTree("/sitecore/content"))
			{

				Assert.True(testTree.ContainsPath("/sitecore/content/hello"));
			}
		}

		[Fact]
		public void ContainsPath_ReturnsFalse_WhenNotIncludedPath()
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.False(testTree.ContainsPath("/umbraco/aem"));
			}
		}

		[Fact]
		public void ContainsPath_ReturnsTrue_WhenExpectedPath_ContainingInvalidFileSystemChars()
		{
			using (var testTree = new TestSfsTree("/sitecore/content"))
			{

				Assert.True(testTree.ContainsPath("/sitecore/content/$he\\llo%<html>"));
			}
		}
	}
}
