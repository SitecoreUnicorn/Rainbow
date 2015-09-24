using System;
using System.Runtime.Serialization;

namespace Rainbow.Storage
{
	[Serializable]
	public class SfsWriteException : Exception
	{
		public SfsWriteException(string message, Exception inner) : base(message, inner)
		{
		}

		protected SfsWriteException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
