using System;
using System.Collections.Generic;
using Rainbow.Model;

namespace Rainbow.Storage
{
	/// <summary>
	/// Represents a place you can store items in abstract. For example, this could be a Sitecore database, files on a filesystem, etc
	/// </summary>
	public interface IDataStore
	{
		/// <summary>
		/// Saves an item into the store
		/// </summary>
		void Save(IItemData item);

		/// <remarks>
		/// Note: for moved items, pass in the FINAL path to move to, not the path being moved from (if it's a move within the store, we'll know the old path by ID from current state)
		/// </remarks>
		void MoveOrRenameItem(IItemData itemWithFinalPath, string oldPath);

		/// <summary>
		/// Gets an item - or items, if multiple names match the path - by its path.
		/// If you have item ID available you should favor GetByPathAndId() over this,
		/// as it is faster and more accurate for most data stores.
		/// </summary>
		IEnumerable<IItemData> GetByPath(string path, string database);

		/// <summary>
		/// Gets an item by its path and ID. This is the preferred method to get a specific item from the data store, as it enables
		/// both path-indexed and ID-indexed data stores to perform optimally.
		/// </summary>
		/// <returns>The item or null if no item matches. Exception should be thrown if metadata is incomplete.</returns>
		IItemData GetByPathAndId(string path, Guid id, string database);

		/// <summary>
		/// Gets an item by its ID.
		/// If you have item path available you should favor GetByPathAndId() over this,
		/// as it enables a data store to choose its fastest path to resolution.
		/// 
		/// Note: with path-indexed data stores such as SFS this results in a whole-tree-scan to resolve the item by ID - which is not very fast.
		/// </summary>
		/// <returns>The item or null if that item does not exist in the store.</returns>
		IItemData GetById(Guid id, string database);

		/// <summary>
		/// Gets all items in the data store matching a template ID and returns their metadata
		/// Note: this may be a slow operation in some providers that may not be indexed by template ID
		/// </summary>
		IEnumerable<IItemMetadata> GetMetadataByTemplateId(Guid templateId, string database);

		IEnumerable<IItemData> GetChildren(IItemData parentItem);

		/// <summary>
		/// Loads all items in the data store
		/// </summary>
		void CheckConsistency(string database, bool fixErrors, Action<string> logMessageReceiver);

		/// <summary>
		/// Resets any kind of template field cache the provider may have
		/// </summary>
		void ResetTemplateEngine();

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		bool Remove(IItemData item);

		/// <summary>
		/// Delegate is fired when the data store data changes. This is an optional implementation, required only if you wish to enable data providers
		/// to clear Sitecore data caches when the data store is changed.
		/// </summary>
		void RegisterForChanges(Action<IItemMetadata, string> actionOnChange);

		/// <summary>
		/// Instructs the data store to remove all stored data (wipe clean)
		/// </summary>
		void Clear();
	}
}