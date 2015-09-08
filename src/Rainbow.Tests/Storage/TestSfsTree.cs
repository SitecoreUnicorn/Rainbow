using System;
using System.IO;
using Rainbow.Storage;
using Rainbow.Storage.Yaml;

namespace Rainbow.Tests.Storage
{
	internal class TestSfsTree : SerializationFileSystemTree, IDisposable
	{
		public TestSfsTree(string globalRootItemPath = "/sitecore")
			: base("Unit Testing", globalRootItemPath, "UnitTesting", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()), new YamlSerializationFormatter(null, null), false)
		{
			GlobalRootItemPath = globalRootItemPath;
			MaxPathLengthForTests = base.MaxRelativePathLength;
			MaxFileNameLengthForTests = base.MaxItemNameLengthBeforeTruncation;
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

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			
			if (PhysicalRootPath != null && Directory.Exists(PhysicalRootPath))
				Directory.Delete(PhysicalRootPath, true);
		}

		public void ClearAllCaches()
		{
			ClearCaches();
		}

		public int MaxPathLengthForTests { get; set; }
		public int MaxFileNameLengthForTests { get; set; }

		protected override int MaxRelativePathLength { get { return MaxPathLengthForTests; } }
		protected override int MaxItemNameLengthBeforeTruncation { get { return MaxFileNameLengthForTests; } }
	}
}
