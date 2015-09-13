using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Fact]
		public void GetRootItem_ReturnsExpectedItem_WhenTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				var root = testTree.GetRootItem();

				Assert.NotNull(root);
				Assert.Equal(root.Name, "sitecore");
			}
		}

		[Fact]
		public void GetRootItem_ReturnsExpectedItem_WhenTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates/User Defined"))
			{
				CreateTestTree("/sitecore/templates/User Defined", testTree);

				var root = testTree.GetRootItem();

				Assert.NotNull(root);
				Assert.Equal(root.Name, "User Defined");
			}
		}

		[Fact]
		public void GetRootItem_ReturnsExpectedItem_WhenNameHasInvalidChars()
		{
			using (var testTree = new TestSfsTree("/<h\\tml>"))
			{
				CreateTestTree("/<h\\tml>", testTree);

				var root = testTree.GetRootItem();

				Assert.NotNull(root);
				Assert.Equal(root.Name, "<h\\tml>");
			}
		}
	}
}
