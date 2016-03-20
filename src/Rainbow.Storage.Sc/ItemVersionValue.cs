using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Rainbow.Model;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Rainbow.Storage.Sc
{
	[DebuggerDisplay("{Language} #{VersionNumber}")]
	public class ItemVersionValue : IItemVersion
	{
		private readonly Item _version;
		// ReSharper disable once RedundantDefaultMemberInitializer
		private bool _fieldsLoaded = false;

		public ItemVersionValue(Item version)
		{
			_version = version;
		}

		private List<IItemFieldValue> _fields;

		public virtual IEnumerable<IItemFieldValue> Fields
		{
			get
			{
				if (_fields == null)
				{
					EnsureFields();

					_fields = CreateFieldReader().ParseFields(_version, FieldReader.FieldReadType.Versioned);
				}

				return _fields;
			}
		}

		public CultureInfo Language => _version.Language.CultureInfo;

		public int VersionNumber => _version.Version.Number;

		protected virtual void EnsureFields()
		{
			if (!_fieldsLoaded)
			{
				_version.Fields.ReadAll();
				_fieldsLoaded = true;
			}
		}
		protected virtual IItemFieldValue CreateFieldValue(Field field, string value)
		{
			return new ItemFieldValue(field, value);
		}

		protected virtual FieldReader CreateFieldReader()
		{
			return ItemData.FieldReader;
		}
	}
}
