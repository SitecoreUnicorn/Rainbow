using System;
using System.Linq;
using Gibson.Indexing;
using NUnit.Framework;

namespace Gibson.Tests.Indexing
{
	public class IndexTests
	{
		[Test]
		public void Index_RetrievesEntry_ById()
		{
			var id = Guid.NewGuid();
			const string path = "/test/item";
			var index = new Index(new[] { new IndexEntry { Id = id, Path = path, ParentId = Guid.Empty } });

			var result = index.GetById(id);

			Assert.IsNotNull(result);
			Assert.AreEqual(result.Path, path);
		}

		[Test]
		public void Index_RetrievesEntry_ByPath()
		{
			const string path = "/test/item";
			var index = new Index(new[] { new IndexEntry { Id = Guid.NewGuid(), Path = path, ParentId = Guid.Empty } });

			var result = index.GetByPath(path);

			Assert.IsNotEmpty(result);
			Assert.AreEqual(result.Count, 1);
			Assert.AreEqual(result.First().Path, path);
		}

		[Test]
		public void Index_RetrievesMultipleEntry_ByPath()
		{
			const string path = "/test/item";
			var index = new Index(new[] { new IndexEntry { Id = Guid.NewGuid(), Path = path, ParentId = Guid.Empty }, new IndexEntry { Id = Guid.NewGuid(), Path = path, ParentId = Guid.Empty } });

			var result = index.GetByPath(path);

			Assert.IsNotEmpty(result);
			Assert.AreEqual(result.Count, 2);
			Assert.IsTrue(result.All(x => x.Path.Equals(path)), "Results contained non matching paths!");
		}

		[Test]
		public void Index_RetrievesEntry_ByTemplateId()
		{
			var tid = Guid.NewGuid();
			const string path = "/test/item";
			var index = new Index(new[] { new IndexEntry { TemplateId = tid, Path = path, ParentId = Guid.Empty, Id = Guid.NewGuid() } });

			var result = index.GetByTemplate(tid);

			Assert.IsNotNull(result);
			Assert.AreEqual(result.Count, 1);
			Assert.AreEqual(result.First().TemplateId, tid);
		}

		[Test]
		public void Index_RetrievesChildren_ByParentId()
		{
			var parentId = Guid.NewGuid();
			var index = new Index(new[] { new IndexEntry { Id = Guid.NewGuid(), Path = string.Empty, ParentId = parentId }, new IndexEntry { Id = Guid.NewGuid(), Path = string.Empty, ParentId = parentId } });

			var result = index.GetChildren(parentId);

			Assert.IsNotNull(result);
			Assert.AreEqual(result.Count, 2);
			Assert.IsTrue(result.All(x => x.ParentId.Equals(parentId)), "Results contained non matching parent IDs!");
		}

		[Test]
		public void Index_RetrievesDescendants_ByParentId()
		{
			var parentId = Guid.NewGuid();
			var childId = Guid.NewGuid();
			var index = new Index(new[] { new IndexEntry { Id = childId, Path = string.Empty, ParentId = parentId }, new IndexEntry { Id = Guid.NewGuid(), Path = string.Empty, ParentId = childId } });

			var result = index.GetDescendants(parentId);

			Assert.IsNotNull(result);
			Assert.AreEqual(result.Count(), 2);
		}

		[Test]
		public void Index_UpdatesEntry_WhenRenamed()
		{
			var testEntry = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry", ParentId = Guid.Empty };
			var testEntryChild = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry/child", ParentId = testEntry.Id };

			var index = new Index(new[] { testEntry, testEntryChild });

			var editedEntry = testEntry.Clone();

			editedEntry.Path = "/test/entry result";

			index.Update(editedEntry);

			// verify rename is in index
			Assert.IsNotEmpty(index.GetByPath(editedEntry.Path));
			Assert.IsEmpty(index.GetByPath(testEntry.Path));

			// verify child was moved to new parent path
			Assert.IsNotEmpty(index.GetByPath("/test/entry result/child"));
			Assert.IsEmpty(index.GetByPath(testEntryChild.Path));
		}

		[Test]
		public void Index_UpdatesEntry_WhenMoved()
		{
			var testEntry = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry", ParentId = Guid.Empty };
			var testEntryChild = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry/child", ParentId = testEntry.Id };
			var testEntryGrandchild = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry/child/grandchild", ParentId = testEntryChild.Id };

			var index = new Index(new[] { testEntry, testEntryChild, testEntryGrandchild });

			var editedEntry = testEntry.Clone();

			editedEntry.ParentId = Guid.NewGuid();
			editedEntry.Path = "/elsewhere/item";

			index.Update(editedEntry);

			// verify move is in index
			Assert.IsNotEmpty(index.GetByPath(editedEntry.Path));
			Assert.IsEmpty(index.GetByPath(testEntry.Path));
			Assert.IsEmpty(index.GetChildren(Guid.Empty)); // original parent ID
			Assert.AreEqual(index.GetChildren(editedEntry.ParentId).Count, 1);

			// verify child was moved to new parent path
			Assert.IsNotEmpty(index.GetByPath("/elsewhere/item/child"));
			Assert.IsEmpty(index.GetByPath(testEntryChild.Path));

			// verify grandchild moved to new parent path
			Assert.IsNotEmpty(index.GetByPath("/elsewhere/item/child/grandchild"));
			Assert.IsEmpty(index.GetByPath(testEntryGrandchild.Path));
		}

		[Test]
		public void Index_UpdatesEntry_WhenTemplateChanged()
		{
			var testEntry = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry", TemplateId = Guid.Empty };

			var index = new Index(new[] { testEntry });

			var editedEntry = testEntry.Clone();

			editedEntry.TemplateId = Guid.NewGuid();

			index.Update(editedEntry);

			// verify new template is in the index
			Assert.IsNotEmpty(index.GetByTemplate(editedEntry.TemplateId));
			Assert.IsEmpty(index.GetByTemplate(testEntry.TemplateId));
		}

		[Test]
		public void Index_RemovesEntry()
		{
			var testEntry = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry", TemplateId = Guid.Empty };
			var testEntryChild = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry/child", ParentId = testEntry.Id };
			var testEntryGrandchild = new IndexEntry { Id = Guid.NewGuid(), Path = "/test/entry/child/grandchild", ParentId = testEntryChild.Id };

			var index = new Index(new[] { testEntry, testEntryChild, testEntryGrandchild });

			index.Remove(testEntryChild.Id);

			// verify the removed item is not in any indexes
			Assert.IsNull(index.GetById(testEntryChild.Id));
			Assert.IsEmpty(index.GetByPath(testEntryChild.Path));
			Assert.IsFalse(index.GetByTemplate(testEntryChild.TemplateId).Any(x => x.Id.Equals(testEntryChild.Id)));

			// verify it's not still a child of the parent
			Assert.IsEmpty(index.GetChildren(testEntry.Id));

			// verify the item's children are removed too
			Assert.IsEmpty(index.GetChildren(testEntryChild.Id));

			// make sure child item is also not in indexes
			Assert.IsNull(index.GetById(testEntryGrandchild.Id));
			Assert.IsEmpty(index.GetByPath(testEntryGrandchild.Path));
		}
	}
}
