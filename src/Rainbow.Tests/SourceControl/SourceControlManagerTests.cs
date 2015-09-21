using System;
using Xunit;

namespace Rainbow.Tests.SourceControl
{
	public class SourceControlManagerTests
	{
		private const string Filename = "edit-me.yml";

		[Fact]
		public void EditPreProcessing_Sucess_ReturnsTrue()
		{
			var scm = new TestableSourceControlManager(true);
			var result = scm.EditPreProcessing(Filename);
			Assert.True(result);
		}

		[Fact]
		public void EditPostProcessing_Sucess_ReturnsTrue()
		{
			var scm = new TestableSourceControlManager(true);
			var result = scm.EditPostProcessing(Filename);
			Assert.True(result);
		}

		[Fact]
		public void DeletePreProcessing_Sucess_ReturnsTrue()
		{
			var scm = new TestableSourceControlManager(true);
			var result = scm.DeletePreProcessing(Filename);
			Assert.True(result);
		}

		[Fact]
		public void EditPreProcessing_Failure_ThrowsException()
		{
			var scm = new TestableSourceControlManager(false);
			Assert.Throws<Exception>(() => scm.EditPreProcessing(Filename));
		}

		[Fact]
		public void EditPostProcessing_Failure_ThrowsException()
		{
			var scm = new TestableSourceControlManager(false);
			Assert.Throws<Exception>(() => scm.EditPostProcessing(Filename));
		}

		[Fact]
		public void DeletePreProcessing_Failure_ThrowsException()
		{
			var scm = new TestableSourceControlManager(false);
			Assert.Throws<Exception>(() => scm.DeletePreProcessing(Filename));
		}
	}
}