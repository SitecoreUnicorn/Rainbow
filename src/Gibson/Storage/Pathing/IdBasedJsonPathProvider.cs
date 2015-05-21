using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gibson.Indexing;

namespace Gibson.Storage.Pathing
{
	public class IdBasedJsonPathProvider : IFileSystemPathProvider
	{
		public string GetStoragePath(IndexEntry indexData, string rootPath)
		{
			var paths = new Stack<string>(3);
			paths.Push(indexData.Id.ToString("D").ToUpperInvariant() + ".json");
			paths.Push(indexData.Id.ToString("N").Substring(0, 1).ToUpperInvariant());
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
