using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Filtering
{
	/// <summary>
	/// Wraps any serializable item with a filter that returns only included fields
	/// </summary>
	public class FilteredItem : IItemData
	{
		private readonly IItemData _itemData;
		private readonly IFieldFilter _fieldFilter;

		public FilteredItem(IItemData itemData, IFieldFilter fieldFilter)
		{
			Assert.ArgumentNotNull(itemData, "item");
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");

			_itemData = itemData;
			_fieldFilter = fieldFilter;
		}

		public Guid Id { get { return _itemData.Id; } }
		public string DatabaseName { get { return _itemData.DatabaseName; } set { _itemData.DatabaseName = value; } }
		public Guid ParentId { get { return _itemData.ParentId; } }
		public string Path { get { return _itemData.Path; } }
		public string Name { get { return _itemData.Name; } }
		public Guid BranchId { get { return _itemData.BranchId; } }
		public Guid TemplateId { get { return _itemData.TemplateId; } }
		public IEnumerable<IItemFieldValue> SharedFields
		{
			get
			{
				return _itemData.SharedFields.Where(field => _fieldFilter.Includes(field.FieldId));
			}
		}

		public IEnumerable<IItemVersion> Versions
		{
			get { return _itemData.Versions.Select(version => new FilteredVersion(version, _fieldFilter)); }
		}

		public string SerializedItemId { get { return _itemData.SerializedItemId; } }
		public IEnumerable<IItemData> GetChildren()
		{
			return _itemData.GetChildren();
		}

		protected class FilteredVersion : IItemVersion
		{
			private readonly IItemVersion _version;
			private readonly IFieldFilter _fieldFilter;

			public FilteredVersion(IItemVersion version, IFieldFilter fieldFilter)
			{
				_version = version;
				_fieldFilter = fieldFilter;
			}

			public IEnumerable<IItemFieldValue> Fields
			{
				get
				{
					return _version.Fields.Where(field => _fieldFilter.Includes(field.FieldId));
				}
			}
			public CultureInfo Language { get { return _version.Language; } }
			public int VersionNumber { get { return _version.VersionNumber; } }
		}
	}
}
