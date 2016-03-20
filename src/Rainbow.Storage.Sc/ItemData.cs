using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rainbow.Model;
using Sitecore.Data.Items;

namespace Rainbow.Storage.Sc
{
	[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [DB ITEM]")]
	public class ItemData : IItemData
	{
		private readonly Item _item;
		private readonly IDataStore _sourceDataStore;
		private Item[] _itemVersions;
		// ReSharper disable once RedundantDefaultMemberInitializer
		private bool _fieldsLoaded = false;
		protected internal static FieldReader FieldReader = new FieldReader();
		private static readonly Version SitecoreVersion = Version.Parse(Sitecore.Configuration.About.GetVersionNumber(true));

		public ItemData(Item item)
		{
			_item = item;
		}

		public ItemData(Item item, IDataStore sourceDataStore) : this(item)
		{
			_sourceDataStore = sourceDataStore;
		}

		public virtual Guid Id => _item.ID.Guid;

		public virtual string DatabaseName
		{
			get { return _item.Database.Name; }
			set { }
		}

		public virtual Guid ParentId => _item.ParentID.Guid;

		private string _path;
		public virtual string Path
		{
			get
			{
				if (_path == null) return _path = _item.Paths.Path;
				return _path;
			}
		}

		public virtual string Name => _item.Name;

		public virtual Guid BranchId => _item.BranchId.Guid;

		public virtual Guid TemplateId => _item.TemplateID.Guid;

		private List<IItemFieldValue> _sharedFields;
		public virtual IEnumerable<IItemFieldValue> SharedFields
		{
			get
			{
				if (_sharedFields == null)
				{
					EnsureFields();

					_sharedFields = CreateFieldReader().ParseFields(_item, FieldReader.FieldReadType.Shared);
				}

				return _sharedFields;
			}
		}

		private IItemLanguage[] _unversionedFields;
		public IEnumerable<IItemLanguage> UnversionedFields
		{
			get
			{
				if (_unversionedFields == null)
				{
					EnsureFields();

					var fieldReader = CreateFieldReader();

					// Get all item versions, dedupe down to one per language only, then parse out the unversioned fields for each and project into a ProxyItemLanguage.
					_unversionedFields = GetVersions()
						.GroupBy(version => version.Language.Name)
						.Select(group => group.First())
						.Select(language => (IItemLanguage)new ProxyItemLanguage(language.Language.CultureInfo) { Fields = fieldReader.ParseFields(language, FieldReader.FieldReadType.Unversioned) })
						.ToArray();
				}

				return _unversionedFields;
			}
		}

		private List<IItemVersion> _versions;
		public virtual IEnumerable<IItemVersion> Versions
		{
			get
			{
				if (_versions == null)
				{
					var versionResults = new List<IItemVersion>();

					var versions = GetVersions();
					for (int i = 0; i < versions.Length; i++)
					{
						versionResults.Add(CreateVersion(versions[i]));
					}

					_versions = versionResults;
				}

				return _versions;
			}
		}

		public virtual string SerializedItemId => "(from Sitecore database)";

		public virtual IEnumerable<IItemData> GetChildren()
		{
			if (_sourceDataStore != null)
				return _sourceDataStore.GetChildren(this);

			return _item.GetChildren().Select(child => new ItemData(child));
		}

		protected virtual void EnsureFields()
		{
			if (!_fieldsLoaded)
			{
				_item.Fields.ReadAll();
				_fieldsLoaded = true;
			}
		}

		protected virtual Item[] GetVersions()
		{
			if (_itemVersions == null)
			{
				_itemVersions = _item.Versions.GetVersions(true);

				// if we are on Sitecore 8.1.x we need to cull any language fallback'ed versions
				// but we don't want to break compatibility with earlier Sitecore versions so we do a runtime version
				// check prior to invoking the 8.1 API
				if (SitecoreVersion.Major > 8 || (SitecoreVersion.Major == 8 && SitecoreVersion.Minor >= 1))
				{
					_itemVersions = _itemVersions.Where(version => !version.IsFallback).ToArray();
				}
			}

			return _itemVersions;
		}

		protected virtual IItemVersion CreateVersion(Item version)
		{
			return new ItemVersionValue(version);
		}

		protected virtual FieldReader CreateFieldReader()
		{
			return FieldReader;
		}
	}
}
