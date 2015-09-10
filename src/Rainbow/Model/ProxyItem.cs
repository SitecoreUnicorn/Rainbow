using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable DoNotCallOverridableMethodsInConstructor

namespace Rainbow.Model
{
	/// <summary>
	/// Fully evaluates an IItemData instance, performing any lazy loading
	/// Used so that writing items cannot cause any loops during writing
	/// Also used for testing, as you can change values in the ProxyItem after creation.
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

		public virtual Guid Id { get; set; }
		public virtual string DatabaseName { get; set; }
		public virtual Guid ParentId { get; set; }
		public virtual string Path { get; set; }
		public virtual string Name { get; set; }
		public virtual Guid BranchId { get; set; }
		public virtual Guid TemplateId { get; set; }
		public virtual IEnumerable<IItemFieldValue> SharedFields { get; set; }
		public virtual IEnumerable<IItemVersion> Versions { get; set; }
		public virtual string SerializedItemId { get; set; }
		public virtual IEnumerable<IItemData> GetChildren()
		{
			throw new NotImplementedException("Cannot get children of a proxied item");
		}
	}
}
