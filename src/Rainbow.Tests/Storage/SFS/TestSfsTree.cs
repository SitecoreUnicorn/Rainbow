using System;
using System.IO;
using Rainbow.Storage;
using Rainbow.Storage.Yaml;

namespace Rainbow.Tests.Storage.SFS
{
	internal class TestSfsTree : SerializationFileSystemTree, IDisposable
	{
		public TestSfsTree(string globalRootItemPath = "/sitecore")
			: base("Unit Testing", globalRootItemPath, "UnitTesting", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), new YamlSerializationFormatter(null, null))
		{
			GlobalRootItemPath = globalRootItemPath;
			MaxPathLengthForTests = base.MaxRelativePathLength;
		}

		public string ConvertGlobalPathToTreePathTest(string globalPath)
		{
			return ConvertGlobalVirtualPathToTreeVirtualPath(globalPath);
		}

		public string PrepareItemNameForFileSystemTest(string name)
		{
			return PrepareItemNameForFileSystem(name);
		}

		public string GlobalRootItemPath { get; private set; }
		public string PhysicalRootPathTest { get { return PhysicalRootPath; } }

		public void Dispose()
		{
			if (PhysicalRootPath != null && Directory.Exists(PhysicalRootPath))
				Directory.Delete(PhysicalRootPath, true);
		}

		public int MaxPathLengthForTests { get; set; }

		protected override int MaxRelativePathLength { get { return MaxPathLengthForTests; } }
	}
}
