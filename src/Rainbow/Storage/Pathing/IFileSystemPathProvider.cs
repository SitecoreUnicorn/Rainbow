using System.Collections.Generic;
using Rainbow.Indexing;

namespace Rainbow.Storage.Pathing
{
	public interface IFileSystemPathProvider
	{
		string IndexFileName { set; }
		string FileExtension { set; }

		string GetIndexStoragePath(string database, string rootPath);
		string GetStoragePath(IndexEntry indexData, string database, string rootPath);
		string GetDatabaseNameFromPath(string physicalPath, string rootPath);
		IndexEntry FindItemByPhysicalPath(string physicalPath, string rootPath, IIndex index);
		IEnumerable<string> GetAllStoredPaths(string rootPath, string database);
		IEnumerable<string> GetOrphans(string rootPath, string database);
		IEnumerable<string> GetAllStoredDatabaseNames(string rootPath);
	}
}