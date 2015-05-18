using System;
using System.Linq;
using Gibson;
using Gibson.Data;
using Gibson.Indexing;
using Gibson.Model;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Unicorn.Data;
using Unicorn.Predicates;
using Unicorn.Serialization;

namespace Unicorn.Gibson
{
	public class GibsonSerializationProvider : ISerializationProvider
	{
		private readonly SerializationStore _store;
		private readonly GibsonDeserializer _deserializer;
		private readonly string _logName;

		public GibsonSerializationProvider(GibsonDeserializer deserializer, IPredicate predicate, string rootPath = null, string logName = "GibsonItemSerialization")
		{
			// TODO: predicate is here so we can ignore moving descendants to ignored locations
			// TODO: gibson doesnt know about databases. seems like we should allow this to hold more than one gib store for multiple DBs and abstract the root path junk?
			// TODO: 'default' root path semantic not supported
			// TODO: unicorn reserialize does not delete existing gib folders
			// TODO: work out dependencies handling (in general, also registering serialization formatters?)
			// TODO: reconcile deserializer with main project and formatters in unicorn - incl logging TODOs
			// TODO: non-transactional provider for writing? with index flushing?
			// TODO: field predicate-like functionality - maybe in SerializableItem as a dependency you can inject? (note: revision and such are being serialized now)
			// TODO: of note: merging the json is nice except the lack of field names adds additional complexity that is not required
			// TODO: blob storage?
			// TODO: evaluate reducing verbosity of emitted JSON, with shorter identifiers?
			// TODO: evaluate reducing index verbosity with single letter prefixes?
			// TODO: formatting of security field values?
			// TODO: having field type on all the JSON field values is real verbose. maybe omit if it wasnt formatted?
			// TODO: extend unicorn data provider model to allow optional dp features like consistency checks, updating items with a field when deleting a t-field


			_store = new TransactionalFileSystemSerializationStore(rootPath, new PathProvider(), new JsonSerializationFormatter(), new LineOrientedIndexFormatter(), new Index());
			_deserializer = deserializer;
			_logName = logName;
		}

		public string LogName
		{
			get { return _logName; }
		}

		public ISerializedItem SerializeItem(ISourceItem item)
		{
			Assert.ArgumentNotNull(item, "item");

			var sitecoreSourceItem = item as SitecoreSourceItem;

			var sitecoreItem = sitecoreSourceItem != null ? sitecoreSourceItem.InnerItem : Factory.GetDatabase(item.DatabaseName).GetItem(item.Id);

			Assert.IsNotNull(sitecoreItem, "Item to serialize did not exist!");

			var serializable = new SerializableItem(sitecoreItem);

			_store.Save(serializable);

			return new GibsonSerializedItem(serializable, _deserializer, _store);
		}

		public void RenameSerializedItem(ISourceItem renamedItem, string oldName)
		{
			if (renamedItem == null || oldName == null) return;

			var typed = renamedItem as SitecoreSourceItem;

			if (typed == null) throw new ArgumentException("Renamed item must be a SitecoreSourceItem", "renamedItem");

			_store.Save(new SerializableItem(typed.InnerItem));
		}

		public void MoveSerializedItem(ISourceItem sourceItem, ISourceItem newParentItem)
		{
			Assert.ArgumentNotNull(sourceItem, "sourceItem");
			Assert.ArgumentNotNull(newParentItem, "newParentItem");

			var sitecoreSource = sourceItem as SitecoreSourceItem;
			var sitecoreParent = newParentItem as SitecoreSourceItem;

			if (sitecoreParent == null) throw new ArgumentException("newParentItem must be a SitecoreSourceItem", "newParentItem");
			if (sitecoreSource == null) throw new ArgumentException("sourceItem must be a SitecoreSourceItem", "sourceItem");

			// TODO: is sourceItem already moved or not?
		}

		public ISerializedReference GetReference(ISourceItem sourceItem)
		{
			var item = _store.GetById(sourceItem.Id.Guid);

			if (item == null) return null;

			return new GibsonSerializedItem(item, _deserializer, _store);
		}

		public ISerializedItem GetItemByPath(string database, string path)
		{
			var item = _store.GetByPath(path);

			if (item == null || !item.Any()) return null;

			return new GibsonSerializedItem(item.First(), _deserializer, _store);
		}
	}
}
