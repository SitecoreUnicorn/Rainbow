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

		public virtual Guid Id => InnerItem.Id;
		public virtual string DatabaseName { get { return InnerItem.DatabaseName; } set { InnerItem.DatabaseName = value; } }
		public virtual Guid ParentId => InnerItem.ParentId;
		public virtual string Path => InnerItem.Path;
		public virtual string Name => InnerItem.Name;
		public virtual Guid BranchId => InnerItem.BranchId;
		public virtual Guid TemplateId => InnerItem.TemplateId;
		public virtual IEnumerable<IItemFieldValue> SharedFields => InnerItem.SharedFields;
		public virtual IEnumerable<IItemLanguage> UnversionedFields => InnerItem.UnversionedFields;
		public virtual IEnumerable<IItemVersion> Versions => InnerItem.Versions;
		public virtual string SerializedItemId => InnerItem.SerializedItemId;

		public virtual IEnumerable<IItemData> GetChildren()
		{
			return InnerItem.GetChildren();
		}
	}
}
