using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sitecore.Data;

namespace Gibson.Storage
{
	public class PathProvider
	{
		public string GetStoragePath(ID id, string rootPath)
		{
			var paths = new Stack<string>(3);
			paths.Push(id + ".gib");
			paths.Push(id.Guid.ToString("N").Substring(0, 2).ToUpperInvariant());
			paths.Push(rootPath);

			return Path.Combine(paths.ToArray());
		}

		public IEnumerable<string> GetAllStoredPaths(string rootPath)
		{
			return Directory.GetFiles(rootPath, "*.gib", SearchOption.AllDirectories);
		}

		public IEnumerable<string> GetOrphans(string rootPath)
		{
			var children = Directory.EnumerateDirectories(rootPath);

			return children.Where(child => !Directory.EnumerateFileSystemEntries(child).Any());
		}
	}
}
