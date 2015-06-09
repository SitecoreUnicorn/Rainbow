using NUnit.Framework;
using Rainbow.Formatting.FieldFormatters;

namespace Rainbow.Tests.Formatting.FieldFormatters
{
	public class XmlFieldFormatterTests
	{
		private const string SourceValueExpectation = "<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><d id=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\" l=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\"><p uid=\"{C4C0DC9E-3C95-45D2-BB02-EAA406B1C9A9}\" key=\"main\" md=\"/sitecore/layout/Placeholder Settings/Sites/Public/Home/main\" /><r id=\"{EF37A63D-B9E1-4335-B4B1-762E4764EF5A}\" ph=\"wrapper\" uid=\"{6D364A99-1F9D-44F1-A849-8D8310DB24AE}\" /></d></r>";
		private const string FormattedValueExpectation = @"<r xmlns:xsd=""http://www.w3.org/2001/XMLSchema"">
  <d id=""{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}"" l=""{0DAC6578-BC11-4B41-960A-E95F21A78D1F}"">
    <p uid=""{C4C0DC9E-3C95-45D2-BB02-EAA406B1C9A9}"" key=""main"" md=""/sitecore/layout/Placeholder Settings/Sites/Public/Home/main"" />
    <r id=""{EF37A63D-B9E1-4335-B4B1-762E4764EF5A}"" ph=""wrapper"" uid=""{6D364A99-1F9D-44F1-A849-8D8310DB24AE}"" />
  </d>
</r>";
		
		[Test]
		public void MultilistFormatter_FormatsValues_AsExpected()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Format(new FakeSerializableFieldValue(SourceValueExpectation));

			Assert.AreEqual(result, FormattedValueExpectation);
		}

		[Test]
		public void MultilistFormatter_ReturnsValues_AsExpected()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Unformat(FormattedValueExpectation);

			Assert.AreEqual(result, SourceValueExpectation);
		}
	}
}
