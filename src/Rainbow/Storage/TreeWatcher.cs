using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Sitecore.Diagnostics;

namespace Rainbow.Storage
{
	/// <summary>
	/// Provides real time updates when a SFS tree is changed on the filesystem.
	/// Updates are debounced (e.g. 3 rapid writes result in one change logged)
	/// </summary>
	public class TreeWatcher : IDisposable
	{
		private const int DebounceInMs = 1000;
		private readonly Action<string, TreeWatcherChangeType> _actionOnChange;
		private readonly FileSystemWatcher _watcher;
		private readonly Timer _eventFlusher;
		private readonly ConcurrentQueue<TreeChange> _actions = new ConcurrentQueue<TreeChange>();
		private readonly ConcurrentDictionary<string, bool> _knownUpdates = new ConcurrentDictionary<string, bool>();
		private bool _enableEvents = true;

		public TreeWatcher(string rootPath, string extension, Action<string, TreeWatcherChangeType> actionOnChange)
		{
			_actionOnChange = actionOnChange;
			_watcher = new FileSystemWatcher(rootPath, "*" + extension);
			_watcher.IncludeSubdirectories = true;
			_watcher.Changed += OnFileChanged;
			_watcher.Created += OnFileChanged;
			_watcher.Deleted += OnFileChanged;
			_watcher.Renamed += OnFileChanged;
			_watcher.EnableRaisingEvents = true;

			_eventFlusher = new Timer(FlushEvents);
		}

		public void PushKnownUpdate(string path, TreeWatcherChangeType changeType)
		{
			_knownUpdates.TryAdd(path + changeType, true);
		}

		private void OnFileChanged(object source, FileSystemEventArgs args)
		{
			if (!_enableEvents) return;

			OnFileChanged(args.FullPath, args.ChangeType);
		}

		private void OnFileChanged(string path, WatcherChangeTypes changeType)
		{
			if (!_enableEvents) return;

			_actions.Enqueue(new TreeChange { Path = path, Type = SimplifyType(changeType) });
			_eventFlusher.Change(DebounceInMs, Timeout.Infinite);
		}

		private void FlushEvents(object ignored)
		{
			TreeChange queueItem;
			HashSet<string> actionsTaken = new HashSet<string>();

			while (_actions.TryDequeue(out queueItem))
			{
				var key = queueItem.Path + queueItem.Type;
				if (!actionsTaken.Contains(key))
				{
					actionsTaken.Add(key);

					// a known update is a file we knew about the change to, so we signal the watcher to ignore it
					// because SFS did it, so all the caches are good
					if (_knownUpdates.ContainsKey(key))
					{
						bool val;
						_knownUpdates.TryRemove(key, out val);
						continue;
					}

					_actionOnChange(queueItem.Path, queueItem.Type);
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (_watcher != null)
				{
					_watcher.EnableRaisingEvents = false;
					_watcher.Changed -= OnFileChanged;
					_watcher.Created -= OnFileChanged;
					_watcher.Deleted -= OnFileChanged;
					_watcher.Renamed -= OnFileChanged;
					_watcher.Dispose();
				}
				_eventFlusher?.Dispose();
			}
		}

		protected TreeWatcherChangeType SimplifyType(WatcherChangeTypes watcherChange)
		{
			if (watcherChange == WatcherChangeTypes.Deleted) return TreeWatcherChangeType.Delete;

			return TreeWatcherChangeType.ChangeOrAdd;
		}

		protected class TreeChange
		{
			public TreeWatcherChangeType Type { get; set; }
			public string Path { get; set; }
		}

		public enum TreeWatcherChangeType
		{
			ChangeOrAdd,
			Delete
		}
	}
}
