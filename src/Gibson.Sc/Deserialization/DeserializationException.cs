using System;
using System.Runtime.Serialization;

namespace Gibson.Sc.Deserialization
{
	[Serializable]
	public class DeserializationException : Exception
	{
		public DeserializationException()
		{
		}

		public DeserializationException(string message) : base(message)
		{
		}

		public DeserializationException(string message, Exception inner) : base(message, inner)
		{
		}

		protected DeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
