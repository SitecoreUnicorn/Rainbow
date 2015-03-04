using System.IO;
using System.Linq;
using Gibson.Storage;
using NUnit.Framework;
using Sitecore.Data;

namespace Gibson.Tests.Storage
{
	public class PathProviderTests
	{
		[Test]
		public void PathProvider_GetStoragePath_ReturnsExpectedPath()
		{
			var provider = new PathProvider();
			var id = new ID("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}");

			var result = provider.GetStoragePath(id, @"c:\test\path with space");

			Assert.AreEqual(result, @"c:\test\path with space\0D\{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}.gib", "Path was not expected value!");
		}

		[Test]
		public void PathProvider_GetAllStoredPaths_ReturnsExpectedItems_Recursively()
		{
			var temp = GetTemporaryDirectory();
			File.WriteAllText(Path.Combine(temp, "foo.gib"), "gib!");
			File.WriteAllText(Path.Combine(temp, "foo.bar"), "not gib!");
			var subdirectory = Directory.CreateDirectory(Path.Combine(temp, "gib subdirectory"));
			File.WriteAllText(Path.Combine(subdirectory.FullName, "bar.gib"), "gib!");

			var provider = new PathProvider();

			var result = provider.GetAllStoredPaths(temp).ToArray();

			try
			{
				Assert.AreEqual(result.Length, 2, "More than the 2 expected items found!");
				Assert.That(result.All(x => x.EndsWith(".gib")), "Found non gib files!");
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
			File.WriteAllText(Path.Combine(temp, "foo.gib"), "gib!");
			var subdirectory = Directory.CreateDirectory(Path.Combine(temp, "gib subdirectory"));
			File.WriteAllText(Path.Combine(subdirectory.FullName, "bar.gib"), "gib!");
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
