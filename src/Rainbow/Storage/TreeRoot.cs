﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Rainbow.Storage
{
	[ExcludeFromCodeCoverage]
	[DebuggerDisplay("{Name} {DatabaseName}:{Path}")]
	public class TreeRoot
	{
		public IFieldValueManipulator FieldValueManipulator { get; set; }

		public TreeRoot(string name, string path, string databaseName)
		{
			Name = name?.Trim();
			Path = path;
			DatabaseName = databaseName;
		}

		public string Name { get; protected set; }
		public string Path { get; protected set; }
		public string DatabaseName { get; protected set; }
	}
}
