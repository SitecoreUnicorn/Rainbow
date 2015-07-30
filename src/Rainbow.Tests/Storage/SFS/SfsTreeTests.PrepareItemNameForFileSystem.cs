using NUnit.Framework;

namespace Rainbow.Tests.Storage.SFS
{
	partial class SfsTreeTests
	{
		[Test]
		[TestCase("hello.yml", "hello.yml")]
		[TestCase("hello\\there.yml", "hello_there.yml")]
		[TestCase("hello/there.yml", "hello_there.yml")]
		[TestCase("$name", "_name")]
		[TestCase("hello%d", "hello_d")]
		[TestCase("hello? is it you?", "hello_ is it you_")]
		[TestCase("<html>", "_html_")]
		[TestCase("hello | %", "hello _ _")]
		[TestCase("woo*", "woo_")]
		[TestCase("yes \"sir\"", "yes _sir_")]
		public void PrepareItemNameForFileSystem_FiltersIllegalCharacters(string input, string expectedOutput)
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.AreEqual(expectedOutput, testTree.PrepareItemNameForFileSystemTest(input));
			}
		}

		[Test]
		[TestCase("hello", "hello")]
		[TestCase("hello hello", "hello hell")]
		public void PrepareItemNameForFileSystem_TruncatesLongFileNames(string input, string expectedOutput)
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.MaxFileNameLengthForTests = 10;
				Assert.AreEqual(expectedOutput, testTree.PrepareItemNameForFileSystemTest(input));
			}
		}
	}
}
