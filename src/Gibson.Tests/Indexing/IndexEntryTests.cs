using Gibson.Indexing;
using NUnit.Framework;

namespace Gibson.Tests.Indexing
{
	public class IndexEntryTests
	{
		[Test]
		public void IndexEntry_ReturnsExpectedName_WithSinglePath()
		{
			var entry = new IndexEntry();
			entry.Path = "/sitecore";

			Assert.AreEqual(entry.Name, "sitecore");
		}

		[Test]
		public void IndexEntry_ReturnsExpectedName_WithPath()
		{
			var entry = new IndexEntry();
			entry.Path = "/sitecore/content/Home";

			Assert.AreEqual(entry.Name, "Home");
		}

		[Test]
		public void IndexEntry_ReturnsExpectedName_WithNaughtyPath()
		{
			var entry = new IndexEntry();
			entry.Path = "/sitecore/content/";

			Assert.AreEqual(entry.Name, "content");
		}

		[Test]
		public void IndexEntry_ReturnsExpectedParentPath_WithSinglePath()
		{
			var entry = new IndexEntry();
			entry.Path = "/sitecore";

			Assert.AreEqual(entry.ParentPath, null);
		}

		[Test]
		public void IndexEntry_ReturnsExpectedParentPath_WithPath()
		{
			var entry = new IndexEntry();
			entry.Path = "/sitecore/content/Home";

			Assert.AreEqual(entry.ParentPath, "/sitecore/content");
		}

		[Test]
		public void IndexEntry_ReturnsExpectedParentPath_WithNaughtyPath()
		{
			var entry = new IndexEntry();
			entry.Path = "/sitecore/content/Home/";

			Assert.AreEqual(entry.ParentPath, "/sitecore/content");
		}
	}
}
