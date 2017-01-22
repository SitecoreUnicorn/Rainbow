using System.Collections.Generic;
using System.Linq;
using Rainbow.Model;
using Sitecore.Diagnostics;

namespace Rainbow.Filtering
{
	/// <summary>
	/// Wraps any serializable item with a filter that returns only included fields
	/// </summary>
	public class FilteredItem : ItemDecorator
	{
		private readonly IFieldFilter _fieldFilter;

		public FilteredItem(IItemData innerItem, IFieldFilter fieldFilter) : base(innerItem)
		{
			Assert.ArgumentNotNull(fieldFilter, "fieldFilter");

			_fieldFilter = fieldFilter;
		}

		public override IEnumerable<IItemFieldValue> SharedFields
		{
			get
			{
				return InnerItem.SharedFields.Where(field => _fieldFilter.Includes(field.FieldId));
			}
		}

		public override IEnumerable<IItemVersion> Versions
		{
			get { return InnerItem.Versions.Select(version => new FilteredVersion(version, _fieldFilter)); }
		}

		protected class FilteredVersion : ProxyItemVersion
		{
			private readonly IItemVersion _innerVersion;
			private readonly IFieldFilter _fieldFilter;

			public FilteredVersion(IItemVersion innerVersion, IFieldFilter fieldFilter) : base(innerVersion)
			{
				_innerVersion = innerVersion;
				_fieldFilter = fieldFilter;
			}

			public override IEnumerable<IItemFieldValue> Fields
			{
				get { return _innerVersion.Fields.Where(field => _fieldFilter.Includes(field.FieldId)); }
			}
		}
	}
}
