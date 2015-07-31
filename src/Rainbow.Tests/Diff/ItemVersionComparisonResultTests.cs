using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Rainbow.Diff;

namespace Rainbow.Tests.Diff
{
	public class ItemVersionComparisonResultTests
	{
		[Test]
		public void IVCR_ReturnsSourceVersion_WhenTargetVersionIsNull()
		{
			var source = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(source, null, null);

			Assert.AreEqual(comparison.VersionNumber, source.VersionNumber);
		}

		[Test]
		public void IVCR_ReturnsTargetVersion_WhenSourceVersionIsNull()
		{
			var target = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(null, target, null);

			Assert.AreEqual(comparison.VersionNumber, target.VersionNumber);
		}

		[Test]
		public void IVCR_ReturnsSourceLanguage_WhenTargetVersionIsNull()
		{
			var source = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(source, null, null);

			Assert.AreEqual(comparison.Language, source.Language);
		}

		[Test]
		public void IVCR_ReturnsTargetLanguage_WhenSourceVersionIsNull()
		{
			var target = new FakeItemVersion(2, "de");

			var comparison = new ItemVersionComparisonResult(null, target, null);

			Assert.AreEqual(comparison.Language, target.Language);
		}
	}
}
