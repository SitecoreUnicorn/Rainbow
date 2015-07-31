using NUnit.Framework;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Test]
		public void ConvertGlobalVirtualPathToTreeVirtualPath_ReturnsExpectedValue_WhenTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.AreEqual("/sitecore/hello", testTree.ConvertGlobalPathToTreePathTest("/sitecore/hello"));
			}
		}

		[Test]
		public void ConvertGlobalVirtualPathToTreeVirtualPath_ReturnsExpectedValue_WhenTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/content/templates"))
			{
				Assert.AreEqual("/templates/User Defined", testTree.ConvertGlobalPathToTreePathTest("/sitecore/content/templates/User Defined"));
			}
		}
	}
}
