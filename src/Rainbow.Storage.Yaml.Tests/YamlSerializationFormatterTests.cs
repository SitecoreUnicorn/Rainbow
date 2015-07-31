using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using NUnit.Framework;
using Rainbow.Tests;

namespace Rainbow.Storage.Yaml.Tests
{
	public class YamlSerializationFormatterTests
	{
		[Test]
		public void YamlFormatter_ReadsMetadata_AsExpected()
		{
			var formatter = new YamlSerializationFormatter(null, null);

			var metadata = @"---
ID: a4f985d9-98b3-4b52-aaaf-4344f6e747c6
Parent: 001dd393-96c5-490b-924a-b0f25cd9efd8
Template: 007a464d-5b09-4d0e-8481-cb6a604a5948
Path: /sitecore/content/test
";

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(metadata)))
			{
				var item = formatter.ReadSerializedItemMetadata(ms, "unittest.yml");
				Assert.AreEqual(new Guid("a4f985d9-98b3-4b52-aaaf-4344f6e747c6"), item.Id);
				Assert.AreEqual(new Guid("001dd393-96c5-490b-924a-b0f25cd9efd8"), item.ParentId);
				Assert.AreEqual("unittest.yml", item.SerializedItemId);
				Assert.AreEqual("/sitecore/content/test", item.Path);
			}
		}

		[Test]
		public void YamlFormatter_ReadsItem_AsExpected()
		{
			var formatter = new YamlSerializationFormatter(null, null);

			var yml = @"---
ID: a4f985d9-98b3-4b52-aaaf-4344f6e747c6
Parent: 001dd393-96c5-490b-924a-b0f25cd9efd8
Template: 007a464d-5b09-4d0e-8481-cb6a604a5948
Path: /sitecore/content/test
SharedFields:
- ID: 549fa670-79ab-4810-9450-aba0c06a2b87
  # Text Shared
  Value: SHARED
Languages:
- Language: en
  Versions:
  - Version: 1
    Fields:
    - ID: 25bed78c-4957-4165-998a-ca1b52f67497
      # __Created
      Value: 20140918T062658:635466184182719253
";

			using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(yml)))
			{
				var item = formatter.ReadSerializedItem(ms, "unittest.yml");
				Assert.AreEqual("unittest.yml", item.SerializedItemId);
				Assert.AreEqual(new Guid("a4f985d9-98b3-4b52-aaaf-4344f6e747c6"), item.Id);
				Assert.AreEqual(new Guid("001dd393-96c5-490b-924a-b0f25cd9efd8"), item.ParentId);
				Assert.AreEqual("/sitecore/content/test", item.Path);

				var shared = item.SharedFields.ToArray();

				Assert.AreEqual(1, shared.Length);
				Assert.AreEqual(new Guid("549fa670-79ab-4810-9450-aba0c06a2b87"), shared[0].FieldId);
				Assert.AreEqual("SHARED", shared[0].Value);

				var versions = item.Versions.ToArray();

				Assert.AreEqual(1, versions.Length);
				Assert.AreEqual(1, versions[0].VersionNumber);
				Assert.AreEqual(new CultureInfo("en"), versions[0].Language);

				var versionedFields = versions[0].Fields.ToArray();

				Assert.AreEqual(1, versionedFields.Length);
				Assert.AreEqual(new Guid("25bed78c-4957-4165-998a-ca1b52f67497"), versionedFields[0].FieldId);
				Assert.AreEqual("20140918T062658:635466184182719253", versionedFields[0].Value);
			}
		}

		[Test]
		public void YamlFormatter_WritesItem_AsExpected()
		{
			var formatter = new YamlSerializationFormatter(null, null);

			var item = new FakeItem(
				id: new Guid("a4f985d9-98b3-4b52-aaaf-4344f6e747c6"),
				parentId: new Guid("001dd393-96c5-490b-924a-b0f25cd9efd8"),
				templateId: new Guid("007a464d-5b09-4d0e-8481-cb6a604a5948"),
				path: "/sitecore/content/test",
				sharedFields: new[]
				{
					new FakeFieldValue("SHARED", string.Empty, new Guid("549fa670-79ab-4810-9450-aba0c06a2b87"), "Text Shared")
				},
				name: "test",
				branchId: new Guid("25bed78c-4957-4165-998a-ca1b52f67497"),
				versions: new[]
				{
					new FakeItemVersion(1, "en", new FakeFieldValue("20140918T062658:635466184182719253", string.Empty, new Guid("25bed78c-4957-4165-998a-ca1b52f67497"), "__Created")),
				});

			var expectedYml = @"---
ID: a4f985d9-98b3-4b52-aaaf-4344f6e747c6
Parent: 001dd393-96c5-490b-924a-b0f25cd9efd8
Template: 007a464d-5b09-4d0e-8481-cb6a604a5948
Path: /sitecore/content/test
BranchID: 25bed78c-4957-4165-998a-ca1b52f67497
SharedFields:
- ID: 549fa670-79ab-4810-9450-aba0c06a2b87
  # Text Shared
  Value: SHARED
Languages:
- Language: en
  Versions:
  - Version: 1
    Fields:
    - ID: 25bed78c-4957-4165-998a-ca1b52f67497
      # __Created
      Value: 20140918T062658:635466184182719253
";

			using (var ms = new MemoryStream())
			{
				formatter.WriteSerializedItem(item, ms);

				ms.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(ms))
				{
					var yml = reader.ReadToEnd();

					Assert.AreEqual(expectedYml, yml);
				}
			}

		}

		[Test]
		public void YamlFormatter_WritesItem_WithFieldFormatter_AsExpected()
		{
			var xmlConfigNode = @"<serializationFormatter>
						<fieldFormatter type=""Rainbow.Formatting.FieldFormatters.MultilistFormatter, Rainbow"" />
					</serializationFormatter>";

			var configDoc = new XmlDocument();
			configDoc.LoadXml(xmlConfigNode);

			var formatter = new YamlSerializationFormatter(configDoc.DocumentElement, null);

			var item = new FakeItem(
				id: new Guid("a4f985d9-98b3-4b52-aaaf-4344f6e747c6"),
				parentId: new Guid("001dd393-96c5-490b-924a-b0f25cd9efd8"),
				templateId: new Guid("007a464d-5b09-4d0e-8481-cb6a604a5948"),
				path: "/sitecore/content/test",
				sharedFields: new[]
				{
					new FakeFieldValue("{35633C96-8494-4C05-A62A-954E2B401A4D}|{1486633B-3A5D-40D6-99D5-920FDFA63617}", "Multilist", new Guid("549fa670-79ab-4810-9450-aba0c06a2b87"), "Multilist Field")
				},
				name: "test");

			var expectedYml = @"---
ID: a4f985d9-98b3-4b52-aaaf-4344f6e747c6
Parent: 001dd393-96c5-490b-924a-b0f25cd9efd8
Template: 007a464d-5b09-4d0e-8481-cb6a604a5948
Path: /sitecore/content/test
SharedFields:
- ID: 549fa670-79ab-4810-9450-aba0c06a2b87
  # Multilist Field
  Type: Multilist
  Value: |
    {35633C96-8494-4C05-A62A-954E2B401A4D}
    {1486633B-3A5D-40D6-99D5-920FDFA63617}
";

			using (var ms = new MemoryStream())
			{
				formatter.WriteSerializedItem(item, ms);

				ms.Seek(0, SeekOrigin.Begin);

				using (var reader = new StreamReader(ms))
				{
					var yml = reader.ReadToEnd();

					Assert.AreEqual(expectedYml, yml);
				}
			}
		}
	}
}
