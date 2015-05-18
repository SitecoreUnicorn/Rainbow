using System;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace Gibson.Tests
{
	public class PathProviderTests
	{
		[Test]
		public void PathProvider_GetStoragePath_ReturnsExpectedPath()
		{
			var provider = new PathProvider();
			var id = new Guid("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}");

			var result = provider.GetStoragePath(id, @"c:\test\path with space");

			Assert.AreEqual(result, @"c:\test\path with space\0\0DE95AE4-41AB-4D01-9EB0-67441B7C2450.json", "Path was not expected value!");
		}

		[Test]
		public void PathProvider_GetAllStoredPaths_ReturnsExpectedItems_Recursively()
		{
			var temp = GetTemporaryDirectory();
			File.WriteAllText(Path.Combine(temp, "foo.json"), "gib!");
			File.WriteAllText(Path.Combine(temp, "foo.bar"), "not gib!");
			var subdirectory = Directory.CreateDirectory(Path.Combine(temp, "gib subdirectory"));
			File.WriteAllText(Path.Combine(subdirectory.FullName, "bar.json"), "gib!");

			var provider = new PathProvider();

			var result = provider.GetAllStoredPaths(temp).ToArray();

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
			File.WriteAllText(Path.Combine(temp, "foo.json"), "gib!");
			var subdirectory = Directory.CreateDirectory(Path.Combine(temp, "gib subdirectory"));
			File.WriteAllText(Path.Combine(subdirectory.FullName, "bar.json"), "gib!");
			Directory.CreateDirectory(Path.Combine(temp, "orphan"));

			var provider = new PathProvider();

			var result = provider.GetOrphans(temp).ToArray();

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
