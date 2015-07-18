using System;
using System.Collections.Generic;
using Rainbow.Model;

namespace Rainbow.Storage
{
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

		IEnumerable<IItemData> GetByPath(string path, string database);

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
	}
}