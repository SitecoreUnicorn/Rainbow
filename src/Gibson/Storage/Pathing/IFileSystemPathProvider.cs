using System.Collections.Generic;
using Gibson.Indexing;

namespace Gibson.Storage.Pathing
{
	public interface IFileSystemPathProvider
	{
		string IndexFileName { set; }
		string FileExtension { set; }

		string GetIndexStoragePath(string database, string rootPath);
		string GetStoragePath(IndexEntry indexData, string database, string rootPath);
		IEnumerable<string> GetAllStoredPaths(string rootPath, string database);
		IEnumerable<string> GetOrphans(string rootPath, string database);
		IEnumerable<string> GetAllStoredDatabaseNames(string rootPath);
	}
}