using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibson.Indexing;
using NUnit.Framework;

namespace Gibson.Tests.Indexing
{
	public class LineOrientedIndexFormatterReadTests
	{
		[Test]
		public void IndexFormatter_ReadsOneItem()
		{
			var result = ReadFile(GetTestItem1());

			Assert.AreEqual(1, result.Count());
		}

		[Test]
		public void IndexFormatter_ReadsTwoItems()
		{
			var result = ReadFile(GetTestItem1() + "\r\n" + GetTestItem2());

			Assert.AreEqual(2, result.Count());
		}

		[Test]
		public void IndexFormatter_ReadsPath()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().Path == "/sitecore/content folder");
		}

		[Test]
		public void IndexFormatter_ReadsId()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().Id.Equals(new Guid("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}")));
		}

		[Test]
		public void IndexFormatter_ReadsTemplateId()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().TemplateId.Equals(new Guid("{E3E2D58C-DF95-4230-ADC9-279924CECE84}")));
		}

		[Test]
		public void IndexFormatter_ReadsParentId()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().ParentId.Equals(new Guid("{11111111-1111-1111-1111-111111111111}")));
		}

		private IEnumerable<IndexEntry> ReadFile(string content)
		{
			using (var ms = new MemoryStream())
			{
				using (var sw = new StreamWriter(ms, Encoding.UTF8, 128, true))
				{
					sw.Write(content);
					sw.Flush();
				}

				ms.Position = 0;

				return new LineOrientedIndexFormatter().ReadIndex(ms);
			}
		}

		private string GetTestItem1()
		{
			return @"-- Item --
0DE95AE4-41AB-4D01-9EB0-67441B7C2450
/sitecore/content folder
TPL {E3E2D58C-DF95-4230-ADC9-279924CECE84}
PID {11111111-1111-1111-1111-111111111111}";
		}

		private string GetTestItem2()
		{
			return @"-- Item --
1CE3B36C-9B0C-4EB5-A996-BFCB4EAA5287
/sitecore/layout/Placeholder Settings
TPL {A87A00B1-E6DB-45AB-8B54-636FEC3B5523}
PID {EB2E4FFD-2761-4653-B052-26A64D385227}";
		}
	}
}
