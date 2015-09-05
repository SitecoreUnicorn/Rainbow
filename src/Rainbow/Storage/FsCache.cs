using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Sitecore.IO;

namespace Rainbow.Storage
{
	/// <summary>
	/// Implements a filesystem cache that invalidates entries when the file last write time changes
	/// Cache entries present for less than 1s are treated as always valid to reduce i/o
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FsCache<T> where T : class
	{
		private readonly ConcurrentDictionary<string, FsCacheEntry<T>> _fsCache = new ConcurrentDictionary<string, FsCacheEntry<T>>(StringComparer.OrdinalIgnoreCase);

		public FsCache(bool enabled)
		{
			Enabled = enabled;
		}

		public bool Enabled { get; set; }

		public void AddOrUpdate(string key, T value)
		{
			if (!Enabled) return;

			var file = new FileInfo(key);

			AddOrUpdate(file, value);
		}

		/// <summary>
		///     Gets a value from the cache. Returns null if the value doesn't exist.
		/// </summary>
		/// <typeparam name="T">Type expected to return.</typeparam>
		/// <param name="key">The cache key to retrieve</param>
		/// <param name="populateFunction">Delegate to invoke if the cached item doesn't exist that generates the item value</param>
		public T GetValue(string key, Func<FileInfo, T> populateFunction)
		{
			var cached = GetValue(key);
			if (cached != null) return cached;

			var file = new FileInfo(key);

			if (!file.Exists) return null;

			var value = populateFunction(file);

			AddOrUpdate(file, value);

			return value;
		}

		public virtual T GetValue(string key, bool validate = true)
		{
			if (!Enabled) return null;

#if DEBUG
			_accesses++;
#endif
			FsCacheEntry<T> existing;
			if (!_fsCache.TryGetValue(key, out existing)) return null;

			if (!validate) return existing.Entry;

			// if entry is less than 1sec old, return it
			if ((DateTime.Now - existing.Added).TotalMilliseconds < 1000)
			{
#if DEBUG
				_hits++;
#endif
				return existing.Entry;
			}

			// entry file does not exist or last mod is changed, invalidate entry
			var file = new FileInfo(key);
			if (!file.Exists || file.LastWriteTime != existing.LastModified) return null;

#if DEBUG
			_hits++;
#endif
			return existing.Entry;
		}

		public bool Remove(string key)
		{
			FsCacheEntry<T> entry;
			return _fsCache.TryRemove(key, out entry);
		}

		public void Clear()
		{
			_fsCache.Clear();
		}

		protected virtual void AddOrUpdate(FileInfo file, T value)
		{
			if (!Enabled) return;

			if (!file.Exists) return;

			var entry = new FsCacheEntry<T>
			{
				Added = DateTime.Now,
				LastModified = file.LastWriteTime,
				Entry = value
			};

			_fsCache[file.FullName] = entry;
		}

		protected class FsCacheEntry<TEntry>
		{
			public TEntry Entry { get; set; }
			public DateTime Added { get; set; }
			public DateTime LastModified { get; set; }
		}

#if DEBUG
		private long _hits;
		private long _accesses;

		[Obsolete("This property only exists when compiled for debug, do not use in actual code.")]
		public double HitRatio
		{
			get { return _hits/(double)_accesses; }
		}
#endif
	}
}
