using System;
using System.Linq;
using FluentAssertions;
using Rainbow.Model;
using Xunit;

namespace Rainbow.Tests.Model
{
	public class PathRebasingItemDataTests
	{
		[Fact]
		public void ShouldRebasePath()
		{
			var root = new FakeItem();
			var pid = Guid.NewGuid();

			var rebase = new PathRebasingProxyItem(root, "/sitecore/new", pid);

			rebase.Path.Should().Be("/sitecore/new/test item");
			rebase.ParentId.Should().Be(pid);
		}

		[Fact]
		public void ShouldRebasePathOfChildren()
		{
			var root = new FakeItem(children: new[] { new FakeItem() }, id: Guid.NewGuid());

			var rebase = new PathRebasingProxyItem(root, "/sitecore/new", Guid.NewGuid());

			rebase.GetChildren().First().Path.Should().Be("/sitecore/new/test item/test item");
			rebase.GetChildren().First().ParentId.Should().Be(root.Id);
		}
	}
}
