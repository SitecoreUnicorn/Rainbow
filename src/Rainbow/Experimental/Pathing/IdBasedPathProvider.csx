using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Rainbow.Indexing;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Rainbow.Storage.Pathing
{
	public class IdBasedPathProvider : IFileSystemPathProvider
	{
		public string IndexFileName { get; set; }

		private string _fileExtension;
		public string FileExtension
		{
			get
			{
				return _fileExtension;
			}
			set
			{
				if(!Regex.IsMatch(value, "^\\.[a-zA-Z0-9]+$")) throw new InvalidOperationException("The file extension must start with a dot and be alphanumeric (e.g. '.json' but not 'json' or '.$5@#'");

				_fileExtension = value;
			}
		}

		public string GetIndexStoragePath(string database, string rootPath)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			return Path.Combine(rootPath, database, IndexFileName);
		}

		public string GetStoragePath(IndexEntry indexData, string database, string rootPath)
		{
			Assert.ArgumentNotNull(indexData, "indexData");
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			var paths = new Stack<string>(4);
			paths.Push(indexData.Id.ToString("D").ToUpperInvariant() + FileExtension);
			paths.Push(indexData.Id.ToString("N").Substring(0, 1).ToUpperInvariant());
			paths.Push(database);
			paths.Push(rootPath);

			return Path.Combine(paths.ToArray());
		}

		public string GetDatabaseNameFromPath(string physicalPath, string rootPath)
		{
			Assert.ArgumentNotNullOrEmpty(physicalPath, "physicalPath");
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			if(!physicalPath.StartsWith(rootPath)) throw new InvalidOperationException("Physical path was not under the root path.");

			var relativePath = physicalPath.Substring(rootPath.Length).TrimStart(Path.DirectorySeparatorChar);

			var indexOfSlash = relativePath.IndexOf(Path.DirectorySeparatorChar);

			string result = relativePath;

			if (indexOfSlash > 0) result = relativePath.Substring(0, indexOfSlash);

			if(string.IsNullOrEmpty(result)) throw new InvalidOperationException("Path {0} did not result in a usable database name.".FormatWith(physicalPath));

			return result;
		}

		public IndexEntry FindItemByPhysicalPath(string physicalPath, string rootPath, IIndex index)
		{
			Assert.ArgumentNotNullOrEmpty(physicalPath, "physicalPath");
			Assert.ArgumentNotNull(index, "index");

			var itemName = Path.GetFileNameWithoutExtension(physicalPath);

			Guid id;

			if (!Guid.TryParse(itemName, out id)) return null;

			return index.GetById(id);
		}

		public IEnumerable<string> GetAllStoredPaths(string rootPath, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			var dbPath = GetDatabasePath(database, rootPath);

			if (!Directory.Exists(dbPath)) return Enumerable.Empty<string>();

			return Directory.GetFiles(dbPath, "*" + FileExtension, SearchOption.AllDirectories);
		}

		public IEnumerable<string> GetOrphans(string rootPath, string database)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			var dbPath = GetDatabasePath(database, rootPath);

			if (!Directory.Exists(dbPath)) return Enumerable.Empty<string>();

			var children = Directory.EnumerateDirectories(dbPath);

			return children.Where(child => !Directory.EnumerateFileSystemEntries(child).Any());
		}

		public IEnumerable<string> GetAllStoredDatabaseNames(string rootPath)
		{
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			return Directory.GetDirectories(rootPath).Select(Path.GetDirectoryName);
		}

		protected string GetDatabasePath(string database, string rootPath)
		{
			Assert.ArgumentNotNullOrEmpty(database, "database");
			Assert.ArgumentNotNullOrEmpty(rootPath, "rootPath");

			return Path.Combine(rootPath, database);
		}
	}
}
