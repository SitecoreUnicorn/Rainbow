using System;
using System.Globalization;
using Rainbow.Diff.Fields;
using Rainbow.Model;

namespace Rainbow.Diff
{
	public class ItemVersionComparisonResult
	{
		public ItemVersionComparisonResult(ISerializableVersion sourceVersion, ISerializableVersion targetVersion, FieldComparisonResult[] changedFields)
		{
			SourceVersion = sourceVersion;
			TargetVersion = targetVersion;
			ChangedFields = changedFields;
			if (ChangedFields == null) ChangedFields = new FieldComparisonResult[] {};
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

		public ISerializableVersion SourceVersion { get; private set; }
		public ISerializableVersion TargetVersion { get; private set; }
		public FieldComparisonResult[] ChangedFields { get; private set; }
	}
}
