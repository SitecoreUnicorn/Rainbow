using System;
using System.Collections.Concurrent;
using System.IO;
using Sitecore.IO;

namespace Rainbow.Storage
{
	/// <summary>
	/// Implements a filesystem cache that invalidates entries when the file last write time changes
	/// Cache entries present for less than 1s are treated as always valid to reduce i/o
	/// This class is thread-safe and automatically takes out read and write locks on files
	/// </summary>
	/// <typeparam name="T">Type of item we're storing in the cache</typeparam>
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

		public T GetValue(string key, Func<FileInfo, T> populateFunction)
		{
			var cached = GetValue(key);
			if (cached != null) return cached;

			var file = new FileInfo(key);

			if (!file.Exists) return null;

			lock (FileUtil.GetFileLock(file.FullName))
			{
				var value = populateFunction(file);

				AddOrUpdate(file, value);

				return value;
			}
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
		public double HitRatio => _hits/(double)_accesses;
#endif
	}
}
