using System;
using System.IO;
using Rainbow.Storage.Yaml.OutputModel;
using Xunit;

namespace Rainbow.Storage.Yaml.Tests
{
	public class YamlFormatTests
	{
		[Fact]
		public void YamlFormatter_EmitsRootFormats()
		{
			var testItem = CreateBaseTestItem();

			ExecuteYamlWriter(writer =>
			{
				testItem.WriteYaml(writer);
			}, BaseTestExpected);
		}

		[Fact]
		public void YamlFormatter_EmitsSharedFields()
		{
			var testItem = CreateBaseTestItem();
			DecorateSharedFieldsTestData(testItem);

			ExecuteYamlWriter(writer =>
			{
				testItem.WriteYaml(writer);
			}, SharedFieldsExpected);
		}

		[Fact]
		public void YamlFormatter_EmitsVersions()
		{
			var testItem = CreateBaseTestItem();
			DecorateVersionsTestData(testItem);

			ExecuteYamlWriter(writer =>
			{
				testItem.WriteYaml(writer);
			}, VersionsExpected);
		}

		private void ExecuteYamlWriter(Action<YamlWriter> actions, string expectedOutput)
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
					Assert.Equal(expectedOutput, result);
				}
			}
		}

		private YamlItem CreateBaseTestItem()
		{
			var testItem = new YamlItem
			{
				Id = new Guid("a4f985d9-98b3-4b52-aaaf-4344f6e747c6"),
				DatabaseName = "master",
				ParentId = new Guid("001dd393-96c5-490b-924a-b0f25cd9efd8"),
				TemplateId = new Guid("007a464d-5b09-4d0e-8481-cb6a604a5948"),
				Path = "/sitecore/content/test"
			};

			return testItem;
		}
		
		const string BaseTestExpected = @"---
ID: ""a4f985d9-98b3-4b52-aaaf-4344f6e747c6""
Parent: ""001dd393-96c5-490b-924a-b0f25cd9efd8""
Template: ""007a464d-5b09-4d0e-8481-cb6a604a5948""
Path: /sitecore/content/test
DB: master
";

		private void DecorateSharedFieldsTestData(YamlItem item)
		{
			var field = new YamlFieldValue
			{
				Id = new Guid("9a5a2ce9-9ae3-4a21-92f0-dba3cb7ac2bf"),
				Value = "Hello world."
			};

			var field2 = new YamlFieldValue
			{
				Id = new Guid("badd9cf9-53e0-4d0c-bcc0-2d784c282f6a"),
				NameHint = "Test Field",
				Value = @"Lorem thine ipsum
<p>forsooth thy sit amet</p>
<div class=""simian"">Chimpanzee.</div>"
			};

			item.SharedFields.Add(field);
			item.SharedFields.Add(field2);
		}

		private const string SharedFieldsExpected = BaseTestExpected + @"SharedFields:
- ID: ""9a5a2ce9-9ae3-4a21-92f0-dba3cb7ac2bf""
  Value: Hello world.
- ID: ""badd9cf9-53e0-4d0c-bcc0-2d784c282f6a""
  Hint: Test Field
  Value: |
    Lorem thine ipsum
    <p>forsooth thy sit amet</p>
    <div class=""simian"">Chimpanzee.</div>
";

		private void DecorateVersionsTestData(YamlItem item)
		{
			var field = new YamlFieldValue
			{
				Id = new Guid("9a5a2ce9-9ae3-4a21-92f0-dba3cb7ac2bf"),
				Value = "Hello \"silly\" world."
			};

			var field2 = new YamlFieldValue
			{
				Id = new Guid("badd9cf9-53e0-4d0c-bcc0-2d784c282f6a"),
				NameHint = "Test Field",
				Value = @"Lorem thine ipsum
<p>forsooth thy sit amet</p>
<div class=""simian"">Chimpanzee.</div>"
			};

			var testVersion1 = new YamlVersion();
			testVersion1.VersionNumber = 1;
			testVersion1.Fields.Add(field);
			testVersion1.Fields.Add(field2);

			var testVersion2 = new YamlVersion();
			testVersion2.VersionNumber = 2;
			testVersion2.Fields.Add(field);

			var testLanguage = new YamlLanguage();
			testLanguage.Language = "en-US";
			testLanguage.Versions.Add(testVersion1);

			var testLanguage2 = new YamlLanguage();
			testLanguage2.Language = "da-DK";
			testLanguage2.Versions.Add(testVersion1);
			testLanguage2.Versions.Add(testVersion2);

			item.Languages.Add(testLanguage);
			item.Languages.Add(testLanguage2);
		}

		private const string VersionsExpected = BaseTestExpected + @"Languages:
- Language: ""da-DK""
  Versions:
  - Version: 1
    Fields:
    - ID: ""9a5a2ce9-9ae3-4a21-92f0-dba3cb7ac2bf""
      Value: |
        Hello ""silly"" world.
    - ID: ""badd9cf9-53e0-4d0c-bcc0-2d784c282f6a""
      Hint: Test Field
      Value: |
        Lorem thine ipsum
        <p>forsooth thy sit amet</p>
        <div class=""simian"">Chimpanzee.</div>
  - Version: 2
    Fields:
    - ID: ""9a5a2ce9-9ae3-4a21-92f0-dba3cb7ac2bf""
      Value: |
        Hello ""silly"" world.
- Language: ""en-US""
  Versions:
  - Version: 1
    Fields:
    - ID: ""9a5a2ce9-9ae3-4a21-92f0-dba3cb7ac2bf""
      Value: |
        Hello ""silly"" world.
    - ID: ""badd9cf9-53e0-4d0c-bcc0-2d784c282f6a""
      Hint: Test Field
      Value: |
        Lorem thine ipsum
        <p>forsooth thy sit amet</p>
        <div class=""simian"">Chimpanzee.</div>
";
	}
}
