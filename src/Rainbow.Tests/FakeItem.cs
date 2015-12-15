using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Rainbow.Model;

namespace Rainbow.Tests
{
	[ExcludeFromCodeCoverage]
	public class FakeItem : ProxyItem
	{
		public FakeItem(
			Guid id = default(Guid), 
			string databaseName = "master", 
			Guid parentId = default(Guid), 
			string path = "/sitecore/content/test item", 
			string name = "test item", 
			Guid branchId = default(Guid), 
			Guid templateId = default(Guid), 
			IEnumerable<IItemFieldValue> sharedFields = null, 
			IEnumerable<IItemVersion> versions = null, 
			string serializedItemId = "0xDEADBEEF", 
			IEnumerable<IItemData> children = null,
			IEnumerable<IItemLanguage> unversionedFields = null) : base(name, id, parentId, templateId, path, databaseName)
		{
			BranchId = branchId;
			SharedFields = sharedFields ?? new List<IItemFieldValue>();
			Versions = versions ?? new List<IItemVersion>();
			SerializedItemId = serializedItemId;
			UnversionedFields = unversionedFields ?? new List<IItemLanguage>();

			SetProxyChildren(children?.ToArray() ?? new IItemData[0]);
		}
	}
}
