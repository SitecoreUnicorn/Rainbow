using System;
using System.IO;
using System.Linq;
using Gibson.Indexing;
using Gibson.Storage.Pathing;
using NUnit.Framework;

namespace Gibson.Tests.Storage.Pathing
{
	public class IdBasedPathProviderTests
	{
		[Test]
		public void PathProvider_RespectsFileExtension_WhenGettingStoragePath()
		{
			var provider = new IdBasedPathProvider();
			provider.FileExtension = ".lol";

			Assert.AreEqual(".lol", Path.GetExtension(provider.GetStoragePath(new IndexEntry{ Id = new Guid()}, "master", GetTemporaryDirectory())));
		}

		[Test]
		public void PathProvider_GetStoragePath_ReturnsExpectedPath()
		{
			var provider = new IdBasedPathProvider();
			provider.FileExtension = ".json";

			var entry = new IndexEntry
			{
				Id = new Guid("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}")
			};

			var result = provider.GetStoragePath(entry, "master", @"c:\test\path with space");

			Assert.AreEqual(@"c:\test\path with space\master\0\0DE95AE4-41AB-4D01-9EB0-67441B7C2450.json", result, "Path was not expected value!");
		}

		[Test]
		public void PathProvider_GetIndexStoragePath_ReturnsExpectedPath()
		{
			var provider = new IdBasedPathProvider();
			provider.IndexFileName = "teraflop.lol";

			var result = provider.GetIndexStoragePath("master", @"c:\test\path with space");

			Assert.AreEqual(@"c:\test\path with space\master\teraflop.lol", result, "Path was not expected value!");
		}

		[Test]
		public void PathProvider_GetAllStoredPaths_ReturnsExpectedItems_Recursively()
		{
			var temp = GetTemporaryDirectory();
			var masterPath = Path.Combine(temp, "master");
			Directory.CreateDirectory(masterPath);

			File.WriteAllText(Path.Combine(masterPath, "foo.json"), "gib!");
			File.WriteAllText(Path.Combine(masterPath, "foo.bar"), "not gib!");
			var subdirectory = Directory.CreateDirectory(Path.Combine(masterPath, "gib subdirectory"));
			File.WriteAllText(Path.Combine(subdirectory.FullName, "bar.json"), "gib!");

			var provider = new IdBasedPathProvider();
			provider.FileExtension = ".json";

			var result = provider.GetAllStoredPaths(temp, "master").ToArray();

			try
			{
				Assert.AreEqual(result.Length, 2, "More than the 2 expected items found!");
				Assert.That(result.All(x => x.EndsWith(".json")), "Found non json files!");
			}
			finally
			{
				Directory.Delete(temp, true);
			}
		}

		[Test]
		public void PathProvider_GetOrphans_FindsEmptyFolder()
		{
			var temp = GetTemporaryDirectory();
			var masterPath = Path.Combine(temp, "master");
			Directory.CreateDirectory(masterPath);

			File.WriteAllText(Path.Combine(masterPath, "foo.json"), "gib!");
			var subdirectory = Directory.CreateDirectory(Path.Combine(masterPath, "gib subdirectory"));
			File.WriteAllText(Path.Combine(subdirectory.FullName, "bar.json"), "gib!");
			Directory.CreateDirectory(Path.Combine(masterPath, "orphan"));

			var provider = new IdBasedPathProvider();

			var result = provider.GetOrphans(temp, "master").ToArray();

			try
			{
				Assert.AreEqual(result.Length, 1, "More than the 1 expected orphan directory found!");
				Assert.That(result.All(x => x.EndsWith("orphan")), "Result was not the expected orphan directory!");
			}
			finally
			{
				Directory.Delete(temp, true);
			}
		}

		private string GetTemporaryDirectory()
		{
			string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
			Directory.CreateDirectory(tempDirectory);
			return tempDirectory;
		}
	}
}
