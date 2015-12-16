using System;
using FluentAssertions;
using Xunit;
using Rainbow.Diff.Fields;

namespace Rainbow.Tests.Diff.Fields
{
	public class XmlComparisonTests : FieldComparerTest
	{
		[Theory,
			InlineData("<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ><d id=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\" l=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\"></d></r>",
			"<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ><d id=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\" l=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\"></d></r>"),
			InlineData("<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ><d id=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\" l=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\"></d></r>",
			@"<r		 
xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" >   
			<d    id=""{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}""   l=""{0DAC6578-BC11-4B41-960A-E95F21A78D1F}"">  </d>
</r>")]
		public void ReturnsTrue_WhenEqualStrings(string val1, string val2)
		{
			var comparison = new XmlComparison();

			RunComparer(comparison, val1, val2, true);
		}

		[Theory,
			InlineData("<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ><d id=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\" l=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\"></d></r>",
			"<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"><d id=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\" l=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\"></d></r>"),
			InlineData("<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ><d l=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\" id=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\"></d></r>",
			"<r xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ><d l=\"{FE5D7FDF-89C0-4D99-9AA3-B5FBD009C9F3}\"  id=\"{0DAC6578-BC11-4B41-960A-E95F21A78D1F}\" ></d></r>")]
		public void ReturnsFalse_WhenUnequalStrings(string val1, string val2)
		{
			var comparison = new XmlComparison();

			RunComparer(comparison, val1, val2, false);
		}

		[Theory,
			InlineData("Layout", true),
			InlineData("Rules", true),
			InlineData("Tracking", true),
			InlineData("Single-Line Text", false)]
		public void XmlComparison_CanCompare_XmlFieldTypes(string fieldType, bool expected)
		{
			var comparison = new XmlComparison();

			comparison.CanCompare(new FakeFieldValue("foo", fieldType), new FakeFieldValue("foo", fieldType)).Should().Be(expected);
		}

		[Fact]
		public void Throws_WhenPassedInvalidXmlValues()
		{
			var comparison = new XmlComparison();

			Assert.Throws<InvalidOperationException>(() => RunComparer(comparison, "NOT XML", "NOT XML", false));
		}
	}
}
