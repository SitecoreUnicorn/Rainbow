using System;
using System.Globalization;
using Rainbow.Diff.Fields;
using Rainbow.Model;

namespace Rainbow.Diff
{
	public class ItemVersionComparisonResult
	{
		public ItemVersionComparisonResult(IItemVersion sourceVersion, IItemVersion targetVersion, FieldComparisonResult[] changedFields)
		{
			SourceVersion = sourceVersion;
			TargetVersion = targetVersion;
			ChangedFields = changedFields ?? new FieldComparisonResult[] {};
		}

		public int VersionNumber
		{
			get
			{
				if (SourceVersion != null) return SourceVersion.VersionNumber;
				if (TargetVersion != null) return TargetVersion.VersionNumber;
				throw new Exception("Source and target versions both null. Should never occur.");
			}
		}

		public CultureInfo Language
		{
			get
			{
				if (SourceVersion != null) return SourceVersion.Language;
				if (TargetVersion != null) return TargetVersion.Language;
				throw new Exception("Source and target versions both null. Should never occur.");
			}
		}

		public IItemVersion SourceVersion { get; private set; }
		public IItemVersion TargetVersion { get; private set; }
		public FieldComparisonResult[] ChangedFields { get; private set; }
	}
}
