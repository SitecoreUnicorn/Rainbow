using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Gibson.IO;
using NUnit.Framework;
using Sitecore.Data;

namespace Gibson.Tests.IO
{
	public class IndexReaderTests
	{
		[Test]
		public void IndexReader_ReadsOneItem()
		{
			var result = ReadFile(GetTestItem1());

			Assert.AreEqual(1, result.Count());
		}

		[Test]
		public void IndexReader_ReadsTwoItems()
		{
			var result = ReadFile(GetTestItem1() + "\r\n" + GetTestItem2());

			Assert.AreEqual(2, result.Count());
		}

		[Test]
		public void IndexReader_ReadsPath()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().Path == "/sitecore/content folder");
		}

		[Test]
		public void IndexReader_ReadsId()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().Id.Equals(new ID("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}")));
		}

		[Test]
		public void IndexReader_ReadsTemplateId()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().TemplateId.Equals(new ID("{E3E2D58C-DF95-4230-ADC9-279924CECE84}")));
		}

		[Test]
		public void IndexReader_ReadsParentId()
		{
			var result = ReadFile(GetTestItem1());

			Assert.That(result.First().ParentId.Equals(new ID("{11111111-1111-1111-1111-111111111111}")));
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

				using (var sr = new StreamReader(ms))
				{
					return new IndexReader().ReadGibs(sr);
				}
			}
		}

		private string GetTestItem1()
		{
			return @"-- Item --
PATH /sitecore/content folder
ID {0DE95AE4-41AB-4D01-9EB0-67441B7C2450}
TEMPLATE {E3E2D58C-DF95-4230-ADC9-279924CECE84}
ANCESTOR {11111111-1111-1111-1111-111111111111}";
		}

		private string GetTestItem2()
		{
			return @"-- Item --
PATH /sitecore/layout/Placeholder Settings
ID {1CE3B36C-9B0C-4EB5-A996-BFCB4EAA5287}
TEMPLATE {A87A00B1-E6DB-45AB-8B54-636FEC3B5523}
ANCESTOR {EB2E4FFD-2761-4653-B052-26A64D385227}";
		}
	}
}
