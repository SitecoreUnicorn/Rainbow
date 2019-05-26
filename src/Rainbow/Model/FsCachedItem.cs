using System;
using System.Collections.Generic;
using Rainbow.Storage;
using Sitecore.Diagnostics;

namespace Rainbow.Model
{
	/// <summary>
	/// Represents an item that has been stored in a data cache
	/// This class is used to 'restore' the ability to call GetChildren() on a data cached item
	/// after it has been retrieved from cache.
	/// 
	/// Without this ability, data cached items are unable to be moved or renamed because their children are unresolvable.
	/// </summary>
	public class FsCachedItem : ItemDecorator
	{
		private readonly Func<IEnumerable<IItemData>> _childrenFactory;

		public FsCachedItem(IItemData innerItem, Func<IEnumerable<IItemData>> childrenFactory) : base(innerItem)
		{
			_childrenFactory = childrenFactory;
		}

		public override IEnumerable<IItemData> GetChildren()
		{
			return _childrenFactory();
		}
	}
}
