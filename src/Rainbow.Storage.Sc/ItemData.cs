using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Rainbow.Model;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Rainbow.Storage.Sc
{
	[DebuggerDisplay("{Name} ({DatabaseName}::{Id}) [DB ITEM]")]
	public class ItemData : IItemData
	{
		private readonly Func<IDisposable> _itemOperationContextFactory;
		private readonly Item _item;
		private readonly IDataStore _sourceDataStore;
		private Item[] _itemVersions;
		private Item[] _itemLanguages;
		// ReSharper disable once RedundantDefaultMemberInitializer
		private bool _fieldsLoaded = false;
		protected internal static FieldReader FieldReader = new FieldReader();

		public ItemData(Item item)
		{
			_item = item;
		}

		public ItemData(Item item, IDataStore sourceDataStore) : this(item)
		{
			_sourceDataStore = sourceDataStore;
		}

		/// <param name="item">The item to wrap</param>
		/// <param name="itemOperationContextFactory">Creates a disposable context for operations that may hit the DB. Used to disable caches and such as needed to preserve the item's context when getting children, versions, etc.</param>
		public ItemData(Item item, Func<IDisposable> itemOperationContextFactory) : this(item)
		{
			_itemOperationContextFactory = itemOperationContextFactory;
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

		public virtual Guid BranchId
		{
			get
			{
				if (SitecoreVersionResolver.IsVersionHigherOrEqual(SitecoreVersionResolver.SitecoreVersion101))
				{
					return _item.BranchId.Guid;
				}
				return Guid.Empty;
			}
		} 

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
		public virtual IEnumerable<IItemLanguage> UnversionedFields
		{
			get
			{
				if (_unversionedFields == null)
				{
					EnsureFields();

					var fieldReader = CreateFieldReader();

					// Get all item languages (note: not versions, because you can have unversioned fields without a version in a language)
					// then parse out the unversioned fields for each and project into a ProxyItemLanguage.
					_unversionedFields = GetAllLanguages()
						.Select(language =>
						{
							language.Fields.ReadAll();
							return (IItemLanguage)new ProxyItemLanguage(language.Language.CultureInfo) { Fields = fieldReader.ParseFields(language, FieldReader.FieldReadType.Unversioned) };
						})
						.Where(language => language.Fields.Any())
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
					// ReSharper disable once ForCanBeConvertedToForeach
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

			return ItemOperation(() => _item.GetChildren()).Select(child => new ItemData(child, _itemOperationContextFactory));
		}

		protected virtual void EnsureFields()
		{
			if (!_fieldsLoaded)
			{
				ItemOperation(() => _item.Fields.ReadAll());
				_fieldsLoaded = true;
			}
		}

		protected virtual Item[] GetVersions()
		{
			if (_itemVersions == null)
			{
				_itemVersions = ItemOperation(() => _item.Versions.GetVersions(true));

				// if we are on Sitecore 8.1.x we need to cull any language fallback'ed versions
				// but we don't want to break compatibility with earlier Sitecore versions so we do a runtime version
				// check prior to invoking the 8.1 API
				if (SitecoreVersionResolver.IsVersionHigherOrEqual(SitecoreVersionResolver.SitecoreVersion81))
				{
					_itemVersions = _itemVersions.Where(version => !version.IsFallback).ToArray();
				}
			}

			return _itemVersions;
		}

		protected virtual Item[] GetAllLanguages()
		{
			// ReSharper disable once ConvertIfStatementToNullCoalescingExpression
			if (_itemLanguages == null)
			{
				// get the item in all known system languages
				// this is different than getting all versions as unversioned fields may reside on items
				// who do not have a version in a given language. Unusual, but possible.
				_itemLanguages = ItemOperation(() => _item.Languages
					.Select(lang => _item.Database.GetItem(_item.ID, lang))
					.Where(item => item != null)
					.ToArray());
			}

			return _itemLanguages;
		}

		protected virtual IItemVersion CreateVersion(Item version)
		{
			return new ItemVersionValue(version);
		}

		protected virtual FieldReader CreateFieldReader()
		{
			return FieldReader;
		}

		protected virtual void ItemOperation(Action action)
		{
			if (_itemOperationContextFactory == null)
			{
				action();
				return;
			}

			using (_itemOperationContextFactory())
			{
				action();
			}
		}

		protected virtual TResult ItemOperation<TResult>(Func<TResult> action)
		{
			if (_itemOperationContextFactory == null) return action();

			using (_itemOperationContextFactory())
			{
				return action();
			}
		}
	}
}
