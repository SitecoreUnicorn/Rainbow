using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Rainbow.Model;
using Sitecore.StringExtensions;

namespace Rainbow.Storage.Sc.Deserialization
{
	[Serializable]
	[ExcludeFromCodeCoverage]
	public class TemplateMissingFieldException : Exception
	{
		public string TemplateName { get; }
		public string ItemIdentifier { get; private set; }
		public IItemFieldValue[] Fields { get; private set; }

		public TemplateMissingFieldException(string templateName, string itemIdentifier, IItemFieldValue field) : this(templateName, itemIdentifier, new[] { field })
		{
		}

		public TemplateMissingFieldException(string templateName, string itemIdentifier, IEnumerable<IItemFieldValue> fields)
		{
			TemplateName = templateName;
			ItemIdentifier = itemIdentifier;
			Fields = fields.ToArray();
		}

		protected TemplateMissingFieldException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}

		public override string Message
		{
			get { return "The field{0} {1} {2} not present in Sitecore on the {3} template.".FormatWith(Fields.Length == 1 ? string.Empty : "s", string.Join(", ", Fields.Select(f => $"{f.FieldId} (likely {f.NameHint})")), Fields.Length == 1 ? "is" : "are", TemplateName); }
		}

		public static TemplateMissingFieldException Merge(IEnumerable<TemplateMissingFieldException> exceptions)
		{
			var exo = exceptions.ToArray();

			var result = exo.First();

			result.Fields = exo.SelectMany(ex => ex.Fields).ToArray();

			return result;
		}
	}
}
