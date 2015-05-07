using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Gibson.Storage
{
	public class PathProvider
	{
		public string GetStoragePath(Guid id, string rootPath)
		{
			var paths = new Stack<string>(3);
			paths.Push(id.ToString("D").ToUpperInvariant() + ".json");
			paths.Push(id.ToString("N").Substring(0, 2).ToUpperInvariant());
			paths.Push(rootPath);

			return Path.Combine(paths.ToArray());
		}

		public IEnumerable<string> GetAllStoredPaths(string rootPath)
		{
			return Directory.GetFiles(rootPath, "*.json", SearchOption.AllDirectories);
		}

		public IEnumerable<string> GetOrphans(string rootPath)
		{
			var children = Directory.EnumerateDirectories(rootPath);

			return children.Where(child => !Directory.EnumerateFileSystemEntries(child).Any());
		}
	}
}
