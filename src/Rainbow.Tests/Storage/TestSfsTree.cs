using System;
using System.IO;
using System.Linq;
using Rainbow.Storage;
using Rainbow.Storage.Yaml;

namespace Rainbow.Tests.Storage
{
	internal class TestSfsTree : SerializationFileSystemTree
	{
		public TestSfsTree(string globalRootItemPath = "/sitecore", bool useDataCache = false)
			: base("Unit Testing", globalRootItemPath, "UnitTesting", Path.Combine(Path.GetTempPath(), Guid.NewGuid() + " rb"), new YamlSerializationFormatter(null, null), useDataCache)
		{
			MaxPathLengthForTests = base.MaxRelativePathLength;
			MaxFileNameLengthForTests = base.MaxItemNameLengthBeforeTruncation;
		}

		public TestSfsTree(string physicalRootPath, string globalRootItemPath, bool useDataCache = false)
			: base("Unit Testing", globalRootItemPath, "UnitTesting", physicalRootPath, new YamlSerializationFormatter(null, null), useDataCache)
		{
			MaxPathLengthForTests = base.MaxRelativePathLength;
			MaxFileNameLengthForTests = base.MaxItemNameLengthBeforeTruncation;
		}

		public string PrepareItemNameForFileSystemTest(string name)
		{
			return PrepareItemNameForFileSystem(name);
		}

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

		public void SetExtraInvalidNameChars(params char[] chars)
		{
			InvalidFileNameCharacters = Path.GetInvalidFileNameChars().Concat(chars).ToArray();
		}

		protected override int MaxRelativePathLength => MaxPathLengthForTests;
		protected override int MaxItemNameLengthBeforeTruncation => MaxFileNameLengthForTests;
	}
}
