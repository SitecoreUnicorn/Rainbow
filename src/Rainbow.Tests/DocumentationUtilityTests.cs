using System.Collections.Generic;
using Xunit;

namespace Rainbow.Tests
{
	public class DocumentationUtilityTests
	{
		[Fact]
		public void GetFriendlyName_ReturnsExpectedValue_WhenItemIsDocumentable()
		{
			Assert.Equal("Test", DocumentationUtility.GetFriendlyName(new Documentable()));
		}

		[Fact]
		public void GetFriendlyName_ReturnsExpectedValue_WhenItemIsNotDocumentable()
		{
			Assert.Equal(typeof(object).Name, DocumentationUtility.GetFriendlyName(new object()));
		}

		[Fact]
		public void GetDescription_ReturnsExpectedValue_WhenItemIsDocumentable()
		{
			Assert.Equal("Description", DocumentationUtility.GetDescription(new Documentable()));
		}

		[Fact]
		public void GetDescription_ReturnsExpectedValue_WhenItemIsNotDocumentable()
		{
			Assert.Equal(typeof(object).AssemblyQualifiedName, DocumentationUtility.GetDescription(new object()));
		}

		[Fact]
		public void GetConfigurationDetails_ReturnsExpectedValue_WhenItemIsDocumentable()
		{
			var details = DocumentationUtility.GetConfigurationDetails(new Documentable());
			Assert.NotEmpty(details);
			Assert.Equal("Test", details[0].Key);
		}

		[Fact]
		public void GetConfigurationDetails_ReturnsExpectedValue_WhenItemIsNotDocumentable()
		{
			var details = DocumentationUtility.GetConfigurationDetails(new object());

			Assert.NotNull(details);
			Assert.Empty(details);
		}

		private class Documentable : IDocumentable
		{
			public string FriendlyName => "Test";
			public string Description => "Description";

			public KeyValuePair<string, string>[] GetConfigurationDetails()
			{
				return new[] { new KeyValuePair<string, string>("Test", "Value") };
			}
		}
	}
}
