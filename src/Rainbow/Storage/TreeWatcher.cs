using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace Rainbow.Storage
{
	public class TreeWatcher : IDisposable
	{
		private const int DebounceInMs = 1000;
		private readonly Action<string, WatcherChangeTypes> _actionOnChange;
		private readonly FileSystemWatcher _watcher;
		private readonly Timer _eventFlusher;
		private readonly ConcurrentQueue<Tuple<string, WatcherChangeTypes>> _actions = new ConcurrentQueue<Tuple<string, WatcherChangeTypes>>();

		public TreeWatcher(string rootPath, string extension, Action<string, WatcherChangeTypes> actionOnChange)
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

		public void Stop()
		{
			_watcher.EnableRaisingEvents = false;
		}

		public void Restart()
		{
			_watcher.EnableRaisingEvents = true;
		}

		private void OnFileChanged(object source, FileSystemEventArgs args)
		{
			OnFileChanged(args.FullPath, args.ChangeType);
		}

		private void OnFileChanged(string path, WatcherChangeTypes changeType)
		{
			_actions.Enqueue(Tuple.Create(path, changeType));
			_eventFlusher.Change(DebounceInMs, Timeout.Infinite);
		}

		private void FlushEvents(object ignored)
		{
			Tuple<string, WatcherChangeTypes> queueItem;
			HashSet<string> actionsTaken = new HashSet<string>();

			while (_actions.TryDequeue(out queueItem))
			{
				var key = queueItem.Item1 + (queueItem.Item2 == WatcherChangeTypes.Deleted ? "d" : "c");
				if (!actionsTaken.Contains(key))
				{
					_actionOnChange(queueItem.Item1, queueItem.Item2);
					actionsTaken.Add(key);
				}
			}
		}

		public void Dispose()
		{
			if(_watcher != null) _watcher.Dispose();
			if (_eventFlusher != null) _eventFlusher.Dispose();
		}
	}
}
