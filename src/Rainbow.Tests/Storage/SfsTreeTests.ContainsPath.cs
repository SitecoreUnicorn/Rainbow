using NUnit.Framework;

namespace Rainbow.Tests.Storage
{
    partial class SfsTreeTests
    {
		[Test]
		public void ContainsPath_ReturnsTrue_WhenPathIsRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.IsTrue(testTree.ContainsPath("/sitecore/content"));
			}
		}

		[Test]
		public void ContainsPath_ReturnsTrue_WhenExpectedPath()
		{
			using (var testTree = new TestSfsTree("/sitecore/content"))
			{

				Assert.IsTrue(testTree.ContainsPath("/sitecore/content/hello"));
			}
		}

		[Test]
		public void ContainsPath_ReturnsFalse_WhenNotIncludedPath()
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.IsFalse(testTree.ContainsPath("/umbraco/aem"));
			}
		}

		[Test]
		public void ContainsPath_ReturnsTrue_WhenExpectedPath_ContainingInvalidFileSystemChars()
		{
			using (var testTree = new TestSfsTree("/sitecore/content"))
			{

				Assert.IsTrue(testTree.ContainsPath("/sitecore/content/$he\\llo%<html>"));
			}
		}
	}
}
