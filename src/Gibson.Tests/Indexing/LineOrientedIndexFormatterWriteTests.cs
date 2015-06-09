using System;
using System.IO;
using Gibson.Indexing;
using NUnit.Framework;

namespace Gibson.Tests.Indexing
{
	public class LineOrientedIndexFormatterWriteTests
	{
		[Test]
		public void IndexFormatter_WritesOneItem()
		{
			var result = WriteFile(GetTestEntry1());

			Assert.AreEqual(GetTestItem1(), result);
		}

		[Test]
		public void IndexFormatter_WritesTwoItems()
		{
			var result = WriteFile(GetTestEntry1(), GetTestEntry2());

			Assert.AreEqual(GetTestItem1() + GetTestItem2(), result);
		}

		[Test]
		public void IndexFormatter_WritesTwoItems_InPathOrder()
		{
			var result = WriteFile(GetTestEntry2(), GetTestEntry1());

			Assert.AreEqual(GetTestItem1() + GetTestItem2(), result);
		}

		private string WriteFile(params IndexEntry[] entries)
		{
			using (var ms = new MemoryStream())
			{
				new LineOrientedIndexFormatter().WriteIndex(entries, ms);

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
				Id = new Guid("{0DE95AE4-41AB-4D01-9EB0-67441B7C2450}"),
				ParentId = new Guid("{11111111-1111-1111-1111-111111111111}"),
				TemplateId = new Guid("{E3E2D58C-DF95-4230-ADC9-279924CECE84}"),
				Path = "/sitecore/content folder"
			};
		}

		private string GetTestItem1()
		{
			return @"-- Item --
0DE95AE4-41AB-4D01-9EB0-67441B7C2450
/sitecore/content folder
TPL E3E2D58C-DF95-4230-ADC9-279924CECE84
PID 11111111-1111-1111-1111-111111111111
";
		}

		private IndexEntry GetTestEntry2()
		{
			return new IndexEntry
			{
				Id = new Guid("{1CE3B36C-9B0C-4EB5-A996-BFCB4EAA5287}"),
				ParentId = new Guid("{EB2E4FFD-2761-4653-B052-26A64D385227}"),
				TemplateId = new Guid("{A87A00B1-E6DB-45AB-8B54-636FEC3B5523}"),
				Path = "/sitecore/layout/Placeholder Settings"
			};
		}

		private string GetTestItem2()
		{
			return @"-- Item --
1CE3B36C-9B0C-4EB5-A996-BFCB4EAA5287
/sitecore/layout/Placeholder Settings
TPL A87A00B1-E6DB-45AB-8B54-636FEC3B5523
PID EB2E4FFD-2761-4653-B052-26A64D385227
";
		}
	}
}
