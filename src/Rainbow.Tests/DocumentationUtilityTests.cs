using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Rainbow.Tests
{
	public class DocumentationUtilityTests
	{
		[Test]
		public void GetFriendlyName_ReturnsExpectedValue_WhenItemIsDocumentable()
		{
			Assert.AreEqual("Test", DocumentationUtility.GetFriendlyName(new Documentable()));
		}

		[Test]
		public void GetFriendlyName_ReturnsExpectedValue_WhenItemIsNotDocumentable()
		{
			Assert.AreEqual(typeof(object).Name, DocumentationUtility.GetFriendlyName(new object()));
		}

		[Test]
		public void GetDescription_ReturnsExpectedValue_WhenItemIsDocumentable()
		{
			Assert.AreEqual("Description", DocumentationUtility.GetDescription(new Documentable()));
		}

		[Test]
		public void GetDescription_ReturnsExpectedValue_WhenItemIsNotDocumentable()
		{
			Assert.AreEqual(typeof(object).AssemblyQualifiedName, DocumentationUtility.GetDescription(new object()));
		}

		[Test]
		public void GetConfigurationDetails_ReturnsExpectedValue_WhenItemIsDocumentable()
		{
			var details = DocumentationUtility.GetConfigurationDetails(new Documentable());
			Assert.IsNotEmpty(details);
			Assert.AreEqual("Test", details[0].Key);
		}

		[Test]
		public void GetConfigurationDetails_ReturnsExpectedValue_WhenItemIsNotDocumentable()
		{
			var details = DocumentationUtility.GetConfigurationDetails(new object());

			Assert.IsNotNull(details);
			Assert.IsEmpty(details);
		}

		private class Documentable : IDocumentable
		{
			public string FriendlyName { get { return "Test"; } }
			public string Description { get { return "Description"; } }
			public KeyValuePair<string, string>[] GetConfigurationDetails()
			{
				return new[] { new KeyValuePair<string, string>("Test", "Value") };
			}
		}
	}
}
