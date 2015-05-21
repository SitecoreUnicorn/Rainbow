using System;
using System.Collections.Generic;
using Gibson.Model;

namespace Gibson.Storage
{
	public interface ISerializationStore
	{
		/// <summary>
		/// Saves an item into the store
		/// </summary>
		void Save(ISerializableItem item);

		/// <summary>
		/// Loads an item from the store by ID
		/// </summary>
		/// <returns>The stored item, or null if it does not exist in the store</returns>
		ISerializableItem GetById(Guid itemId, string database);

		IEnumerable<ISerializableItem> GetByPath(string path);
		IEnumerable<ISerializableItem> GetByTemplate(Guid templateId);
		IEnumerable<ISerializableItem> GetChildren(Guid parentId);
		IEnumerable<ISerializableItem> GetDescendants(Guid parentId);

		/// <summary>
		/// Loads all items in the data store
		/// </summary>
		void CheckConsistency(bool fixErrors, Action<string> logMessageReceiver);

		/// <summary>
		/// Removes an item from the store
		/// </summary>
		/// <returns>True if the item existed in the store and was removed, false if it did not exist and the store is unchanged.</returns>
		bool Remove(Guid itemId);
	}
}