using System;
using System.IO;
using NUnit.Framework;

namespace Rainbow.Storage.Yaml.Tests
{
	public class YamlWriterTests
	{
		[Test]
		public void YamlWriter_WritesYamlHeading()
		{
			ExecuteYamlWriter(writer => { }, "---\r\n", "YAML header was missing!");
		}

		[Test]
		public void YamlWriter_WritesKeylessMap_AtRoot()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.WriteMap("Hello");
			}, "---\r\nHello:\r\n", "Written keyless map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMap_WithoutEscaping_AtRoot()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.WriteMap("Hello", "World");
			}, "---\r\nHello: World\r\n", "Written unescaped map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMap_WithEscaping_AtRoot()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.WriteMap("Hello", "What a nice \"world\" this is");
			}, "---\r\nHello: \"What a nice \\\"world\\\" this is\"\r\n", "Written map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMultilineMap_AtRoot()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.WriteMap("Hello", "World\r\nit's me!");
			}, "---\r\nHello: |\r\n  World\r\n  it's me!\r\n", "Written multiline map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMultilineMap_WithLinuxEndLines_AtRoot()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.WriteMap("Hello", "World\nit's me!");
			}, "---\r\nHello: |\r\n  World\r\n  it's me!\r\n", "Written multiline map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMap_Indented()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.IncreaseIndent();
				writer.WriteMap("Hello", "World");
			}, "---\r\n  Hello: World\r\n", "Written indented map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesComment_Indented()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.IncreaseIndent();
				writer.WriteComment("Hello");
			}, "---\r\n  # Hello\r\n", "Written comment was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesStartListItem()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.IncreaseIndent();
				writer.WriteBeginListItem("Hello", "World");
			}, "---\r\n- Hello: World\r\n", "Written indented list map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMultilineMap_AsListItem()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.IncreaseIndent();
				writer.WriteBeginListItem("Hello", "World\r\nit's me!");
			}, "---\r\n- Hello: |\r\n    World\r\n    it's me!\r\n", "Written multiline list map was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesMultipleItemList()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.IncreaseIndent();
				writer.WriteBeginListItem("Hello", "World\r\nit's me!");
				writer.WriteMap("Goodbye", "World");
				writer.WriteMap("Just", "Kidding");
			}, "---\r\n- Hello: |\r\n    World\r\n    it's me!\r\n  Goodbye: World\r\n  Just: Kidding\r\n", "Written multiple item list was not in expected format!");
		}

		[Test]
		public void YamlWriter_WritesIndentedLists()
		{
			ExecuteYamlWriter(writer =>
			{
				writer.IncreaseIndent();
				writer.WriteBeginListItem("Hello", "World");
				writer.WriteMap("Goodbye", "World");
				writer.IncreaseIndent();
				writer.WriteBeginListItem("Just", "Kidding");
			}, "---\r\n- Hello: World\r\n  Goodbye: World\r\n  - Just: Kidding\r\n", "Written nested list was not in expected format!");
		}

		private void ExecuteYamlWriter(Action<YamlWriter> actions, string expectedOutput, string errorMessage)
		{
			using (var ms = new MemoryStream())
			{
				using (var writer = new YamlWriter(ms, 4096, true))
				{
					actions(writer);
				}

				ms.Position = 0;

				using (var sr = new StreamReader(ms))
				{
					string result = sr.ReadToEnd();
					Assert.AreEqual(expectedOutput, result, errorMessage);
				}
			}
		}
	}
}
