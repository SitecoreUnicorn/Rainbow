using System;
using System.Xml;
using NUnit.Framework;
using Rainbow.Filtering;

namespace Rainbow.Tests.Filtering
{
	public class ConfigurationFieldFilterTests
	{
		[Test]
		public void FieldFilter_IgnoresExpectedFields()
		{
			var configXml = new XmlDocument();
			configXml.LoadXml(@"<fieldFilter><exclude fieldID=""{B1E16562-F3F9-4DDD-84CA-6E099950ECC0}"" note=""'Last run' field on Schedule template (used to register tasks)"" /></fieldFilter>");

			var filter = new ConfigurationFieldFilter(configXml.DocumentElement);

			Assert.IsFalse(filter.Includes(new Guid("{B1E16562-F3F9-4DDD-84CA-6E099950ECC0}")));
		}

		[Test]
		public void FieldFilter_IncludesExpectedFields()
		{
			var configXml = new XmlDocument();
			configXml.LoadXml(@"<fieldFilter><exclude fieldID=""{B1E16562-F3F9-4DDD-84CA-6E099950ECC0}"" note=""'Last run' field on Schedule template (used to register tasks)"" /></fieldFilter>");

			var filter = new ConfigurationFieldFilter(configXml.DocumentElement);

			Assert.IsTrue(filter.Includes(Guid.NewGuid()));
		}
	}
}
