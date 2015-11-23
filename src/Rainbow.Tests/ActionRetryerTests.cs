using System;
using FluentAssertions;
using Xunit;

namespace Rainbow.Tests
{
	public class ActionRetryerTests
	{
		[Fact]
		public void Perform_PerformsAction()
		{
			bool run = false;

			ActionRetryer.Perform(() => { run = true; });

			run.Should().BeTrue();
		}

		[Fact]
		public void Perform_Fails_IfAllRetriesFail()
		{
			Assert.Throws<Exception>(() =>
			{
				ActionRetryer.Perform(() => { throw new Exception("Derp."); });
			});
		}

		[Fact]
		public void Perform_RetrySucceeds()
		{
			bool first = false;
			bool run = false;

			ActionRetryer.Perform(() =>
			{
				if (first)
				{
					first = false;
					throw new Exception("Derp.");
				}

				run = true;
			});

			run.Should().BeTrue();
		}
	}
}
