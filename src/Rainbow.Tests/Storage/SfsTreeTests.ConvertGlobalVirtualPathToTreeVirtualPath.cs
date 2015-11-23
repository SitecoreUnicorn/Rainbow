using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Fact]
		public void ConvertGlobalVirtualPathToTreeVirtualPath_ReturnsExpectedValue_WhenTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.Equal("/sitecore/hello", testTree.ConvertGlobalVirtualPathToTreeVirtualPath("/sitecore/hello"));
			}
		}

		[Fact]
		public void ConvertGlobalVirtualPathToTreeVirtualPath_ReturnsExpectedValue_WhenTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/content/templates"))
			{
				Assert.Equal("/templates/User Defined", testTree.ConvertGlobalVirtualPathToTreeVirtualPath("/sitecore/content/templates/User Defined"));
			}
		}
	}
}
