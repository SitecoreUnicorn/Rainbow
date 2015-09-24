using System;
using System.Runtime.Serialization;

namespace Rainbow.Storage.Yaml
{
	[Serializable]
	public class YamlFormatException : Exception
	{
		public YamlFormatException(string message, Exception inner) : base(message, inner)
		{
		}

		protected YamlFormatException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
