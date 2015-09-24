using System;
using System.Runtime.Serialization;

namespace Rainbow.Storage
{
	[Serializable]
	public class SfsReadException : Exception
	{
		public SfsReadException(string message, Exception inner) : base(message, inner)
		{
		}

		protected SfsReadException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
