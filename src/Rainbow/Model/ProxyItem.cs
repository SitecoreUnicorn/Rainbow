using System;
using System.Collections.Generic;
using System.Linq;

namespace Rainbow.Model
{
	/// <summary>
	/// Fully evaluates an IItemData instance, performing any lazy loading
	/// Used so that writing items cannot cause any loops during writing
	/// </summary>
	public class ProxyItem : IItemData
	{
		public ProxyItem(IItemData itemToProxy)
		{
			ParentId = itemToProxy.ParentId;
			Path = itemToProxy.Path;
			Name = itemToProxy.Name;
			BranchId = itemToProxy.BranchId;
			TemplateId = itemToProxy.TemplateId;
			SharedFields = itemToProxy.SharedFields.Select(field => new ProxyFieldValue(field)).ToArray();
			Versions = itemToProxy.Versions.Select(version => new ProxyItemVersion(version)).ToArray();
			SerializedItemId = itemToProxy.SerializedItemId;
			Id = itemToProxy.Id;
			DatabaseName = itemToProxy.DatabaseName;
		}

		public Guid Id { get; set; }
		public string DatabaseName { get; set; }
		public Guid ParentId { get; set; }
		public string Path { get; set; }
		public string Name { get; set; }
		public Guid BranchId { get; set; }
		public Guid TemplateId { get; set; }
		public IEnumerable<IItemFieldValue> SharedFields { get; set; }
		public IEnumerable<IItemVersion> Versions { get; set; }
		public string SerializedItemId { get; set; }
		public IEnumerable<IItemData> GetChildren()
		{
			throw new NotImplementedException("Cannot get children of a proxied item");
		}
	}
}
