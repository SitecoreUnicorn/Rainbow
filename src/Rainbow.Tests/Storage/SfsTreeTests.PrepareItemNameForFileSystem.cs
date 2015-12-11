using Xunit;

namespace Rainbow.Tests.Storage
{
	partial class SfsTreeTests
	{
		[Theory]
		[InlineData("hello", "hello")] // equal
		[InlineData("hello\\there", "hello_there")] // backslash
		[InlineData("hello/there", "hello_there")] // forward slash
		[InlineData("hello? is it you?", "hello_ is it you_")] // question mark
		[InlineData("<html>", "_html_")] // angle brackets
		[InlineData("hello | ", "hello __")] // pipes
		[InlineData("woo*", "woo_")] // wildcards
		[InlineData("yes \"sir\"", "yes _sir_")] // quotes
		[InlineData(" leading and trailing spaces ", "leading and trailing spaces_")] // should trim leading spaces, but transform trailing to underscore (ambuiguity issue, see comment in method)
		[InlineData("CON", "_CON")] // invalid FS names see https://msdn.microsoft.com/en-us/library/windows/desktop/aa365247(v=vs.85).aspx
		[InlineData("AUX", "_AUX")]
		[InlineData("COM1", "_COM1")]
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

		[Theory]
		[InlineData("hello", "hello", "$")] // equal
		[InlineData("$name", "_name", "$")] // $
		[InlineData("name$", "name_", "$")] // trailing $
		[InlineData("yo$$$cashmoney", "yo___cashmoney", "$")] // mid
		[InlineData("trolls like to troll", "____________________", "trolikes ")] // lulz
		public void PrepareItemNameForFileSystem_FiltersIllegalCharacters_AddedFromConfiguration(string input, string expectedOutput, string invalidChars)
		{
			using (var testTree = new TestSfsTree())
			{
				testTree.SetExtraInvalidNameChars(invalidChars.ToCharArray());

				Assert.Equal(expectedOutput, testTree.PrepareItemNameForFileSystemTest(input));
			}
		}
	}
}
