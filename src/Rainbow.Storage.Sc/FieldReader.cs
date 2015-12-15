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

				if (!string.IsNullOrEmpty(value))
				{
					fieldResults.Add(CreateFieldValue(field, value));
					continue;
				}

				// if the value was null or empty, we could still have a "significant empty value" - e.g.
				// a checkbox that is explicitly set to FALSE, when the standard value is TRUE
				// so we grab the value, allowing standard values, and if the standard value is not null or empty
				// we set the field value to an empty string explicitly
				var standardValue = field.GetValue(true, false);

				if (standardValue == null) continue;

				// given: the field value without standard values is null or empty (if above)
				// we verify: if the field is NOT a standard value (in which case we know the value is blank), AND the standard value is not empty
				// if so: we know the field has a significant empty value (its local value is blank and the standard value is NOT blank)
				if (!field.ContainsStandardValue && !string.IsNullOrEmpty(field.GetStandardValue()))
					fieldResults.Add(CreateFieldValue(field, string.Empty));

				// we check for empty or null standard values as a number of Sitecore default items have
				// explicit blank values set for fields whose standard value is also blank. Which is pretty pointless
				// to store in serialization as it's essentially a tautology.
			}

			return fieldResults;
		}

		protected virtual IItemFieldValue CreateFieldValue(Field field, string value)
		{
			return new ItemData.ItemFieldValue(field, value);
		}

		public enum FieldReadType { Versioned, Unversioned, Shared }
	}
}
