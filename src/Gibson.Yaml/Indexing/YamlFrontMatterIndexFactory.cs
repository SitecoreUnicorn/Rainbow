using Gibson.Indexing;
using Gibson.Storage.Pathing;

namespace Gibson.Yaml.Indexing
{
	public class YamlFrontMatterIndexFactory : IIndexFactory
	{
		private readonly string _rootPath;
		private readonly IFileSystemPathProvider _pathProvider;

		public YamlFrontMatterIndexFactory(string rootPath, IFileSystemPathProvider pathProvider)
		{
			_rootPath = rootPath;
			_pathProvider = pathProvider;
		}

		public IIndex CreateIndex(string databaseName)
		{
			var formatter = new YamlFrontMatterIndexFormatter(_pathProvider, _rootPath, databaseName);

			var entries = formatter.ReadIndex(null);

			return new Index(entries);
		}
	}
}
