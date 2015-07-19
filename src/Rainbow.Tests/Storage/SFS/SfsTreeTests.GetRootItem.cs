using NUnit.Framework;

namespace Rainbow.Tests.Storage.SFS
{
	partial class SfsTreeTests
	{
		[Test]
		public void GetRootItem_ReturnsExpectedItem_WhenTreeIsAtRoot()
		{
			using (var testTree = new TestSfsTree())
			{
				CreateTestTree("/sitecore", testTree);

				var root = testTree.GetRootItem();

				Assert.IsNotNull(root);
				Assert.AreEqual(root.Name, "sitecore");
			}
		}

		[Test]
		public void GetRootItem_ReturnsExpectedItem_WhenTreeIsNested()
		{
			using (var testTree = new TestSfsTree("/sitecore/templates/User Defined"))
			{
				CreateTestTree("/sitecore/templates/User Defined", testTree);

				var root = testTree.GetRootItem();

				Assert.IsNotNull(root);
				Assert.AreEqual(root.Name, "User Defined");
			}
		}

		[Test]
		public void GetRootItem_ReturnsExpectedItem_WhenNameHasInvalidChars()
		{
			using (var testTree = new TestSfsTree("/<h\\tml>"))
			{
				CreateTestTree("/<h\\tml>", testTree);

				var root = testTree.GetRootItem();

				Assert.IsNotNull(root);
				Assert.AreEqual(root.Name, "<h\\tml>");
			}
		}
	}
}
