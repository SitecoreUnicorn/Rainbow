using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Diagnostics;

namespace Rainbow.Model
{
	/// <summary>
	/// Overrides the path and parent ID of an item - and its children - to something else
	/// This allows you to execute a move or rename of an item without needing to worry about the consistency of the paths of the child items
	/// </summary>
	public class PathRebasingProxyItem : ItemDecorator
	{
		private readonly string _newParentPath;
		private readonly bool _parentPathIsLiteral = false;

		/// <summary>
		/// Creates a rebasing path based on the path and parent ID of an existing item. Use this to rebase the paths of all children,
		/// while leaving the root item unchanged.
		/// </summary>
		public PathRebasingProxyItem(IItemData innerItem) : base(innerItem)
		{
			Assert.ArgumentNotNull(innerItem, "innerItem");

			ParentId = innerItem.ParentId;
			_newParentPath = innerItem.Path;
			_parentPathIsLiteral = true;
		}

		/// <summary>
		/// Creates a rebasing based on an item, new PARENT path, and new parent ID. Use this to rebase an individual item
		/// that may not yet have the right parent and path.
		/// </summary>
		public PathRebasingProxyItem(IItemData innerItem, string newParentPath, Guid newParentId) : base(innerItem)
		{
			Assert.ArgumentNotNull(innerItem, "innerItem");
			Assert.ArgumentNotNull(newParentPath, "newParentPath");

			_newParentPath = newParentPath;
			ParentId = newParentId;
		}

		public override Guid ParentId { get; }

		public override string Path
		{
			get
			{
				if(_parentPathIsLiteral) return _newParentPath;

				return _newParentPath + "/" + Name;
			}
		}

		public override IEnumerable<IItemData> GetChildren()
		{
			return base.GetChildren().Select(child => new PathRebasingProxyItem(child, Path, Id));
		}
	}
}
