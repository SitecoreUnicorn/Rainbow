using Sitecore.Data;

namespace Gibson
{
	public class SerializationStore
	{
		private readonly string _rootPath;
		private readonly string _databaseName;

		public SerializationStore(string rootPath, string databaseName)
		{
			_rootPath = rootPath;
			_databaseName = databaseName;
		}

		public void SerializeItem(object item)
		{
			// takes a source item and adds it to the store
			// PIPELINE?
		}

		public void SerializeTree(object root)
		{
			// takes a source item and recursively adds it to the store
		}

		public object Deserialize(object serialized)
		{
			// deserializes a serialized item into sitecore and returns it
			// note: set last mod date to now, last mod user to sitecore\unicorn, and revisions to a new random guid
			// PIPELINE?
			return null;
		}

		public object GetItemByPath(string path)
		{
			// gets a serialized item by path
			//Get by Path: simple - index entry

			// TODO; handling of multiple items with identical path?
			return null;
		}

		public object GetItemById(ID id)
		{
			// gets a serialized item by ID
			//Get By ID: simple - by physical path
			return null;
		}

		public object[] GetChildren(ID parent)
		{
			// gets serialized children of a serialized item
			//Get children - index query by parentid

			return null;
		}

		public void MoveItem(ID item, ID newParent)
		{
			// PIPELINE

			// moves a serialized item, given a source item and a source new parent
			//Move: write new paths to all descendants in index, and each file thereof
			// if item is a template field, find all items of that template AND the old template in index and re-serialize (todo: cross database?)
		}

		public void RenameItem(ID itemId, string newName, string oldName)
		{
			// PIPELINE

			// renames a serialized item
			//Rename: write new name to item file, update path in index, update children paths in index
			// if item is a template field, find all items of that template in index and re-serialize (todo: cross database?)
		}

		public void DeleteItem(ID id)
		{
			// PIPELINE

			// deletes a serialized item (and any children)
			//Delete w/children: get all descendants by path in index, delete each file entry + cleanup empty hash bucket folders
			// if item is a template field, find all items of that template in index and re-serialize (todo: cross database?)
		}

		public object[] CheckIntegrity()
		{
			// PIPELINE
			// FSCK: check that all IDs in index have targets, verify that all files have index entries, check paths/IDs/TIDs match in files, check parent IDs in serialized

			return null;
		}

		protected virtual object[] GetItemsByTemplateId(ID templateId)
		{
			return null;
		}
	}
}
