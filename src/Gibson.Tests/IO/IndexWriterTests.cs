using System.IO;
using System.Text;
using Gibson.IO;
using NUnit.Framework;
using Sitecore.Data;

namespace Gibson.Tests.IO
{
	public class IndexWriterTests
	{
		[Test]
		public void IndexWriter_WritesOneItem()
		{
			var result = WriteFile(GetTestEntry1());

			Assert.AreEqual(GetTestItem1(), result);
		}

		[Test]
		public void IndexWriter_WritesTwoItems()
		{
			var result = WriteFile(GetTestEntry1(), GetTestEntry2());

			Assert.AreEqual(GetTestItem1() + GetTestItem2(), result);
		}

		[Test]
		public void IndexWriter_WritesTwoItems_InPathOrder()
		{
			var result = WriteFile(GetTestEntry2(), GetTestEntry1());

			Assert.AreEqual(GetTestItem1() + GetTestItem2(), result);
		}

		private string WriteFile(params IndexEntry[] entries)
		{
			using (var ms = new MemoryStream())
			{
				using (var sw = new StreamWriter(ms, Encoding.UTF8, 128, true))
				{
					new IndexWriter().WriteGibs(entries, sw);
					sw.Flush();
				}

				ms.Position = 0;

				using (var sr = new StreamReader(ms))
				{
					return sr.ReadToEnd();
				}
			}
		}

		private IndexEntry GetTestEntry1()
		{
			return new IndexEntry
			{
				Id = new ID("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}"),
				ParentId = new ID("{11111111-1111-1111-1111-111111111111}"),
				TemplateId = new ID("{E3E2D58C-DF95-4230-ADC9-279924CECE84}"),
				Path = "/sitecore/content folder"
			};
		}

		private string GetTestItem1()
		{
			return @"-- Item --
PATH /sitecore/content folder
ID {0DE95AE4-41AB-4D01-9EB0-67441B7C2450}
TEMPLATE {E3E2D58C-DF95-4230-ADC9-279924CECE84}
ANCESTOR {11111111-1111-1111-1111-111111111111}
";
		}

		private IndexEntry GetTestEntry2()
		{
			return new IndexEntry
			{
				Id = new ID("{1CE3B36C-9B0C-4EB5-A996-BFCB4EAA5287}"),
				ParentId = new ID("{EB2E4FFD-2761-4653-B052-26A64D385227}"),
				TemplateId = new ID("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}"),
				Path = "/sitecore/layout/Placeholder Settings"
			};
		}

		private string GetTestItem2()
		{
			return @"-- Item --
PATH /sitecore/layout/Placeholder Settings
ID {1CE3B36C-9B0C-4EB5-A996-BFCB4EAA5287}
TEMPLATE {A87A00B1-E6DB-45AB-8B54-636FEC3B5523}
ANCESTOR {EB2E4FFD-2761-4653-B052-26A64D385227}
";
		}
	}
}
