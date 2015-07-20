using System;
using System.Runtime.Serialization;
using Rainbow.Model;

namespace Rainbow.Storage.Sc.Deserialization
{
	[Serializable]
	public class DeserializationException : Exception
	{
		public string SerializedItemId { get; private set; }

		public DeserializationException()
		{
		}

		public DeserializationException(string message) : base(message)
		{
		}

		public DeserializationException(string message, Exception inner) : base(message, inner)
		{
		}
		public DeserializationException(string message, IItemData item, Exception inner) : base(message, inner)
		{
			SerializedItemId = item.SerializedItemId;
		}

		protected DeserializationException(SerializationInfo info, StreamingContext context) : base(info, context)
		{
		}
	}
}
