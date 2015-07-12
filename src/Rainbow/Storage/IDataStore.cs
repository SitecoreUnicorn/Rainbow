using System;
using System.Collections.Generic;
using Rainbow.Model;

namespace Rainbow.Storage
{
	public interface IDataStore
	{
		IEnumerable<string> GetDatabaseNames(); 

		/// <summary>
		/// Saves an item into the store
		/// </summary>
		void Save(IItemData item);

		/// <summary>
		/// Loads an item from the store by ID
		/// </summary>
		/// <returns>The stored item, or null if it does not exist in the store</returns>
		IItemData GetById(Guid itemId, string database);

		IEnumerable<IItemData> GetByPath(string path, string database);
		IEnumerable<IItemData> GetByTemplate(Guid templateId, string database);
		IEnumerable<IItemData> GetChildren(Guid parentId, string database);
		IEnumerable<IItemData> GetDescendants(Guid parentId, string database);

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
		bool Remove(Guid itemId, string database);
	}
}