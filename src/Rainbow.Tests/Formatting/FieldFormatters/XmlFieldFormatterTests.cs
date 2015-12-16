using FluentAssertions;
using Xunit;
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
		
		[Fact]
		public void XmlFieldFormatter_FormatsValues_AsExpected()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Format(new FakeFieldValue(SourceValueExpectation));

			Assert.Equal(result, FormattedValueExpectation);
		}

		[Fact]
		public void XmlFieldFormatter_ReturnsValues_AsExpected()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Unformat(FormattedValueExpectation);

			Assert.Equal(result, SourceValueExpectation);
		}

		[Fact]
		public void XmlFieldFormatter_HandlesNullValue_AsNull()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Unformat(null);

			Assert.Equal(result, null);
		}

		[Fact]
		public void XmlFieldFormatter_HandlesEmptyValue_AsRawValue()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Unformat(string.Empty);

			Assert.Equal(result, string.Empty);
		}

		[Fact]
		public void XmlFieldFormatter_HandlesInvalidValue_AsRawValue()
		{
			var formatter = new XmlFieldFormatter();

			var result = formatter.Unformat("lol not valid XML m8");

			result.Should().Be("lol not valid XML m8");
		}

		[Theory,
			InlineData("Layout", true),
			InlineData("Multilist", false)]
		public void XmlFieldFormatter_Formats_XmlFieldTypes(string fieldType, bool expected)
		{
			var formatter = new XmlFieldFormatter();

			Assert.Equal(expected, formatter.CanFormat(new FakeFieldValue("foo", fieldType)));
		}
	}
}
