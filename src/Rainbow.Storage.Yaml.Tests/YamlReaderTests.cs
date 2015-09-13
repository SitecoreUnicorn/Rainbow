using System;
using System.IO;
using System.Text;
using Xunit;

namespace Rainbow.Storage.Yaml.Tests
{
	public class YamlReaderTests
	{
		[Fact]
		public void YamlReader_ReadsYamlHeading()
		{
			ExecuteYamlReader("---", reader => { });
		}

		[Fact]
		public void YamlReader_ReadsValuelessMap_AtRoot()
		{
			ExecuteYamlReader("---\r\nHello:\r\n", reader =>
			{
				reader.ReadExpectedMap("Hello");
			});
		}

		[Fact]
		public void YamlReader_ReadsMap_AtRoot()
		{
			ExecuteYamlReader("---\r\nHello: Whirled Peas\r\n", reader =>
			{
				var value = reader.ReadExpectedMap("Hello");
				Assert.Equal("Whirled Peas", value);
			});
		}

		[Fact]
		public void YamlReader_PreservesMapValueWhitespace_AtRoot()
		{
			ExecuteYamlReader("---\r\nHello:  hi  \r\n", reader =>
			{
				var value = reader.ReadExpectedMap("Hello");
				Assert.Equal(" hi  ", value);
			});
		}

		[Fact]
		public void YamlReader_ReadsMap_WithEscaping_AtRoot()
		{
			ExecuteYamlReader("---\r\nHello: \"Whirled: \\\"Peas\\\"\"\r\n", reader =>
			{
				var value = reader.ReadExpectedMap("Hello");
				Assert.Equal("Whirled: \"Peas\"", value);
			});
		}

		[Fact]
		public void YamlReader_ReadsMultilineMap_AtRoot()
		{
			ExecuteYamlReader("---\r\nHello: |\r\n  <div>\r\n    <p>Hello!</p>\r\n  </div>\r\nFake: Value", reader =>
			{
				var value = reader.ReadExpectedMap("Hello");
				Assert.Equal("<div>\r\n  <p>Hello!</p>\r\n</div>", value);
			});
		}

		[Fact]
		public void YamlReader_ReadsMap_Indented()
		{
			ExecuteYamlReader("---\r\nHello: World\r\n  Indented: Value\r\n", reader =>
			{
				reader.ReadMap();

				var value = reader.ReadExpectedMap("Indented");
				Assert.Equal("Value", value);
			});
		}

		[Fact]
		public void YamlReader_SkipsComment()
		{
			// contains a comment, a blank line, and an indented comment for testing - should skip all of them
			ExecuteYamlReader("---\r\n# This is a comment\r\nHello: World\r\n\r\n  # Indented comment\r\n  Indented: Value\r\n", reader =>
			{
				Assert.Equal("World", reader.ReadExpectedMap("Hello"));

				Assert.Equal("Value", reader.ReadExpectedMap("Indented"));
			});
		}

		[Fact]
		public void YamlReader_ReadsStartListItem_AsMap()
		{
			ExecuteYamlReader("---\r\nHello:\r\n- Pie: Cake\r\n", reader =>
			{
				reader.ReadMap();

				var value = reader.ReadExpectedMap("Pie");
				Assert.Equal("Cake", value);
			});
		}

		[Fact]
		public void YamlReader_ReadsMultilineValue_WithLinuxEndlines()
		{
			ExecuteYamlReader("---\nHello: |\n  <div>\n    <p>Hello!</p>\n  </div>\nFake: Value", reader =>
			{
				var value = reader.ReadExpectedMap("Hello");
				Assert.Equal("<div>\r\n  <p>Hello!</p>\r\n</div>", value); // note: endlines are converted
			});
		}

		[Fact]
		public void YamlReader_PeekMap_DoesNotAdvancePointer()
		{
			ExecuteYamlReader("---\r\nHello: There\r\nPie: Cake\r\n", reader =>
			{
				var peek = reader.PeekMap();

				Assert.Equal("There", peek.Value.Value);

				var read = reader.ReadMap();

				Assert.Equal("There", read.Value.Value);

				var value = reader.ReadExpectedMap("Pie");
				Assert.Equal("Cake", value);
			});
		}

		[Fact]
		public void YamlReader_PeekMap_PeeksSameValue_WhenCalledMultipleTimes()
		{
			ExecuteYamlReader("---\r\nHello: There\r\nPie: Cake\r\n", reader =>
			{
				var peek = reader.PeekMap();

				Assert.Equal("There", peek.Value.Value);

				peek = reader.PeekMap();

				Assert.Equal("There", peek.Value.Value);

				var read = reader.ReadMap();

				Assert.Equal("There", read.Value.Value);
			});
		}

		[Fact]
		public void YamlReader_ReadExpectedGuid_ParsesValidGuidValue()
		{
			ExecuteYamlReader("---\r\nHello: 1d2261da-389e-4f0a-abc1-8cb7bbfbfe28\r\n", reader =>
			{
				var value = reader.ReadExpectedGuidMap("Hello");
				Assert.Equal(new Guid("1d2261da-389e-4f0a-abc1-8cb7bbfbfe28"), value);
			});
		}

		private void ExecuteYamlReader(string yaml, Action<YamlReader> readAssertions)
		{
			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(yaml)))
			{
				using (var writer = new YamlReader(ms, 4096, false))
				{
					readAssertions(writer);
				}
			}
		}
	}
}
