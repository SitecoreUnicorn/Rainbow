using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Theory]
		[InlineData("hello.yml", "hello.yml")]
		[InlineData("hello\\there.yml", "hello_there.yml")]
		[InlineData("hello/there.yml", "hello_there.yml")]
		[InlineData("$name", "_name")]
		[InlineData("hello%d", "hello_d")]
		[InlineData("hello? is it you?", "hello_ is it you_")]
		[InlineData("<html>", "_html_")]
		[InlineData("hello | %", "hello _ _")]
		[InlineData("woo*", "woo_")]
		[InlineData("yes \"sir\"", "yes _sir_")]
		public void PrepareItemNameForFileSystem_FiltersIllegalCharacters(string input, string expectedOutput)
		{
			using (var testTree = new TestSfsTree())
			{
				Assert.Equal(expectedOutput, testTree.PrepareItemNameForFileSystemTest(input));
			}
		}

		[Theory]
		[InlineData("hello", "hello")]
		[InlineData("hello hello", "hello hell")]
		public void PrepareItemNameForFileSystem_TruncatesLongFileNames(string input, string expectedOutput)
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.MaxFileNameLengthForTests = 10;
				Assert.Equal(expectedOutput, testTree.PrepareItemNameForFileSystemTest(input));
			}
		}
	}
}
