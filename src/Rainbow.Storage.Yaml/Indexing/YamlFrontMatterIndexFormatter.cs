using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Rainbow.Indexing;
using Rainbow.Storage.Pathing;
using Rainbow.Storage.Yaml.Formatting;

namespace Rainbow.Storage.Yaml.Indexing
{
	public class YamlFrontMatterIndexFormatter : IIndexFormatter
	{
		private readonly IFileSystemPathProvider _pathProvider;
		private readonly string _rootPath;
		private readonly string _databaseName;

		public YamlFrontMatterIndexFormatter(IFileSystemPathProvider pathProvider, string rootPath, string databaseName)
		{
			_pathProvider = pathProvider;
			_rootPath = rootPath;
			_databaseName = databaseName;
		}

		public ReadOnlyCollection<IndexEntry> ReadIndex(Stream sourceStream)
		{
			return _pathProvider.GetAllStoredPaths(_rootPath, _databaseName)
				.AsParallel()
				.Select(x =>
				{
					using (var stream = File.OpenRead(x))
					{
						return ReadFrontMatter(stream);
					}
				})
				.ToList()
				.AsReadOnly();
		}

		public void WriteIndex(IEnumerable<IndexEntry> entries, Stream outputStream)
		{
			// a front matter index stores its entries with the data
			throw new NotImplementedException();
		}

		/*
		 * Front matter is a fixed length data index set.
		 * Format (GUID is 36 chars):
		 * ID $guid\r\n
		 * PID $guid\r\n
		 * TID $guid\r\n
		 * PATH $path\r\n
		 * # begin regular matter
		 */
		protected virtual IndexEntry ReadFrontMatter(Stream inputStream)
		{
			var entry = new IndexEntry();
			using (var reader = new YamlReader(inputStream, 300, true))
			{
				entry.Id = reader.ReadExpectedGuidMap("ID");
				entry.ParentId = reader.ReadExpectedGuidMap("Parent");
				entry.TemplateId = reader.ReadExpectedGuidMap("Template");
				entry.Path = reader.ReadExpectedMap("Path");
			}

			return entry;
		}
	}
}
