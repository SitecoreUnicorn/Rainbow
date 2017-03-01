using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;

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
		private Func<IEnumerable<IItemData>> _proxyChildrenFactory;
		 
		public ProxyItem() : this("Unnamed", Guid.NewGuid(), Guid.Empty, Guid.Empty, "[orphan]", "master")
		{
		}

		public ProxyItem(IItemData itemToProxy)
		{
			ParentId = itemToProxy.ParentId;
			Path = itemToProxy.Path;
			Name = itemToProxy.Name;
			BranchId = itemToProxy.BranchId;
			TemplateId = itemToProxy.TemplateId;
			SharedFields = itemToProxy.SharedFields.Select(field => new ProxyFieldValue(field)).ToArray();
			UnversionedFields = itemToProxy.UnversionedFields.Select(language => new ProxyItemLanguage(language)).ToArray();
			Versions = itemToProxy.Versions.Select(version => new ProxyItemVersion(version)).ToArray();
			SerializedItemId = itemToProxy.SerializedItemId;
			Id = itemToProxy.Id;
			DatabaseName = itemToProxy.DatabaseName;
		}

		/// <param name="itemToProxy">The item to evaluate into a proxy</param>
		/// <param name="proxyChildren">If true, the method to get children of the proxy item will be kept as a factory for the proxy item's children. Be careful with this as it can have undesirable memory effects as well as as cache issues</param>
		public ProxyItem(IItemData itemToProxy, bool proxyChildren) : this(itemToProxy)
		{
			if(proxyChildren) SetProxyChildren(itemToProxy.GetChildren);
		}

		public ProxyItem(string name, Guid id, Guid parentId, Guid templateId, string path, string databaseName)
		{
			Assert.ArgumentNotNullOrEmpty(name, "name");
			Assert.ArgumentNotNullOrEmpty(path, "path");
			Assert.ArgumentNotNullOrEmpty(databaseName, "databaseName");

			Name = name;
			Id = id;
			ParentId = parentId;
			TemplateId = templateId;
			Path = path;
			DatabaseName = databaseName;
			SharedFields = Enumerable.Empty<IItemFieldValue>();
			Versions = Enumerable.Empty<IItemVersion>();
			UnversionedFields = Enumerable.Empty<IItemLanguage>();
		}

		public ProxyItem(string name, Guid id, Guid parentId, Guid templateId, string path, string databaseName, Func<IEnumerable<IItemData>> childrenFactory) : this(name, id, parentId, templateId, path, databaseName)
		{
			SetProxyChildren(childrenFactory);
		}

		public virtual Guid Id { get; set; }
		public virtual string DatabaseName { get; set; }
		public virtual Guid ParentId { get; set; }
		public virtual string Path { get; set; }
		public virtual string Name { get; set; }
		public virtual Guid BranchId { get; set; }
		public virtual Guid TemplateId { get; set; }
		public virtual IEnumerable<IItemFieldValue> SharedFields { get; set; }
		public IEnumerable<IItemLanguage> UnversionedFields { get; set; }
		public virtual IEnumerable<IItemVersion> Versions { get; set; }
		public virtual string SerializedItemId { get; set; }

		public virtual void SetProxyChildren(IEnumerable<IItemData> children)
		{
			SetProxyChildren(() => children);
		}

		public virtual void SetProxyChildren(Func<IEnumerable<IItemData>> childrenFactory)
		{
			_proxyChildrenFactory = childrenFactory;
		}

		public virtual IEnumerable<IItemData> GetChildren()
		{
			if(_proxyChildrenFactory == null)
				throw new NotImplementedException("Cannot get children of a proxied item that does not have them explicitly set with SetProxyChildren()");

			return _proxyChildrenFactory();
		}
	}
}
