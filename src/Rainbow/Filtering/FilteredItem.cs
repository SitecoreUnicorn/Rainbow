using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Rainbow.Indexing;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Filtering
{
	/// <summary>
	/// Wraps any serializable item with a filter that returns only included fields
	/// </summary>
	public class FilteredItem : ISerializableItem
	{
		private readonly ISerializableItem _item;
		private readonly IFieldFilter _fieldFilter;

		public FilteredItem(ISerializableItem item, IFieldFilter fieldFilter)
		{
			Assert.ArgumentNotNull(item, "item");
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");

			_item = item;
			_fieldFilter = fieldFilter;
		}

		public Guid Id { get { return _item.Id; } }
		public string DatabaseName { get { return _item.DatabaseName; } set { _item.DatabaseName = value; } }
		public Guid ParentId { get { return _item.ParentId; } }
		public string Path { get { return _item.Path; } }
		public string Name { get { return _item.Name; } }
		public Guid BranchId { get { return _item.BranchId; } }
		public Guid TemplateId { get { return _item.TemplateId; } }
		public IEnumerable<ISerializableFieldValue> SharedFields
		{
			get
			{
				return _item.SharedFields.Where(field => _fieldFilter.Includes(field.FieldId));
			}
		}

		public IEnumerable<ISerializableVersion> Versions
		{
			get { return _item.Versions.Select(version => new FilteredVersion(version, _fieldFilter)); }
		}

		public string SerializedItemId { get { return _item.SerializedItemId; } }
		public void AddIndexData(IndexEntry indexEntry)
		{
			_item.AddIndexData(indexEntry);
		}

		protected class FilteredVersion : ISerializableVersion
		{
			private readonly ISerializableVersion _version;
			private readonly IFieldFilter _fieldFilter;

			public FilteredVersion(ISerializableVersion version, IFieldFilter fieldFilter)
			{
				_version = version;
				_fieldFilter = fieldFilter;
			}

			public IEnumerable<ISerializableFieldValue> Fields
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
