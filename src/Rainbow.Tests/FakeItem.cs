using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Rainbow.Model;

namespace Rainbow.Tests
{
	[ExcludeFromCodeCoverage]
	public class FakeItem : IItemData
	{
		private readonly IItemData[] _children;

		public FakeItem(Guid id = default(Guid), string databaseName = "master", Guid parentId = default(Guid), string path = "/sitecore/content/test item", string name = "test item", Guid branchId = default(Guid), Guid templateId = default(Guid), IEnumerable<IItemFieldValue> sharedFields = null, IEnumerable<IItemVersion> versions = null, string serializedItemId = "0xDEADBEEF", IEnumerable<IItemData> children = null)
		{
			ParentId = parentId;
			Path = path;
			Name = name;
			BranchId = branchId;
			TemplateId = templateId;
			SharedFields = sharedFields ?? new List<IItemFieldValue>();
			Versions = versions ?? new List<IItemVersion>();
			SerializedItemId = serializedItemId;
			Id = id;
			DatabaseName = databaseName;
			_children = children != null ? children.ToArray() : new IItemData[0];
		}

		public Guid Id { get; }
		public string DatabaseName { get; set; }
		public Guid ParentId { get; }
		public string Path { get; }
		public string Name { get; }
		public Guid BranchId { get; }
		public Guid TemplateId { get; }
		public IEnumerable<IItemFieldValue> SharedFields { get; }
		public IEnumerable<IItemVersion> Versions { get; }
		public string SerializedItemId { get; }
		public IEnumerable<IItemData> GetChildren()
		{
			return _children;
		}
	}
}
