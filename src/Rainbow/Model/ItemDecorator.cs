using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Sitecore.Diagnostics;

namespace Rainbow.Model
{
	/// <summary>
	/// Wraps any serializable item so you can add behaviours
	/// </summary>
	[ExcludeFromCodeCoverage]
	public abstract class ItemDecorator : IItemData
	{
		protected readonly IItemData InnerItem;

		protected ItemDecorator(IItemData innerItem)
		{
			Assert.ArgumentNotNull(innerItem, "item");

			InnerItem = innerItem;
		}

		public virtual Guid Id { get { return InnerItem.Id; } }
		public virtual string DatabaseName { get { return InnerItem.DatabaseName; } set { InnerItem.DatabaseName = value; } }
		public virtual Guid ParentId { get { return InnerItem.ParentId; } }
		public virtual string Path { get { return InnerItem.Path; } }
		public virtual string Name { get { return InnerItem.Name; } }
		public virtual Guid BranchId { get { return InnerItem.BranchId; } }
		public virtual Guid TemplateId { get { return InnerItem.TemplateId; } }
		public virtual IEnumerable<IItemFieldValue> SharedFields { get { return InnerItem.SharedFields; } }
		public virtual IEnumerable<IItemVersion> Versions { get { return InnerItem.Versions; }}
		public virtual string SerializedItemId { get { return InnerItem.SerializedItemId; } }
		public virtual IEnumerable<IItemData> GetChildren()
		{
			return InnerItem.GetChildren();
		}
	}
}
