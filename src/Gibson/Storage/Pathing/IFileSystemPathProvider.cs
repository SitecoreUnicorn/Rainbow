using System.Collections.Generic;
using Gibson.Indexing;

namespace Gibson.Storage.Pathing
{
	public interface IFileSystemPathProvider
	{
		string GetStoragePath(IndexEntry indexData, string rootPath);
		IEnumerable<string> GetAllStoredPaths(string rootPath);
		IEnumerable<string> GetOrphans(string rootPath);
	}
}