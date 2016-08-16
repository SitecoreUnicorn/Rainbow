using System.Collections.Generic;
using Rainbow.Model;
using Sitecore.Data.Items;
using Sitecore.Data.Managers;
using Sitecore.StringExtensions;
using Sitecore.Diagnostics;
using Sitecore.Data.Fields;

namespace Rainbow.Storage.Sc
{
	/// <summary>
	/// Defines how to read fields (shared and versioned) from a Sitecore item.
	/// </summary>
	public class FieldReader
	{
		/// <summary>
		/// Defines how to read fields (shared and versioned) from a Sitecore item.
		/// </summary>
		/// <param name="item">The item whose fields you wish to parse</param>
		/// <param name="fieldType">The class of field to parse</param>
		public virtual List<IItemFieldValue> ParseFields(Item item, FieldReadType fieldType)
		{
			var template = TemplateManager.GetTemplate(item);

			if (template == null)
			{
				Log.Warn("Unable to read {2} fields for {0} because template {1} did not exist.".FormatWith(item.ID, item.TemplateID, fieldType.ToString()), this);
				return new List<IItemFieldValue>();
			}

			// avoiding LINQ as this is a very tight loop operation

			var fieldResults = new List<IItemFieldValue>();

			for (int i = 0; i < item.Fields.Count; i++)
			{
				var field = item.Fields[i];

				// fields that don't match the sharing setting, or fields not on the template
				if (fieldType == FieldReadType.Shared && !field.Shared) continue;
				if (fieldType == FieldReadType.Unversioned && (!field.Unversioned || field.Shared)) continue; // extra check is for 'shared + unversioned' fields, which evaluate to shared
				if (fieldType == FieldReadType.Versioned && (field.Shared || field.Unversioned)) continue;
				if (template.GetField(field.ID) == null) continue;

				var value = field.GetValue(allowDefaultValue: false, allowStandardValue: false);

				// @cassidydotdk: 07-June-2016: If there is a value (and it's not coming from std values or elsewhere), it is significant
				if (value != null)
					fieldResults.Add(CreateFieldValue(field, value));
			}

			return fieldResults;
		}

		protected virtual IItemFieldValue CreateFieldValue(Field field, string value)
		{
			return new ItemFieldValue(field, value);
		}

		public enum FieldReadType { Versioned, Unversioned, Shared }
	}
}
