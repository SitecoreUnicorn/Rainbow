using System;
using System.Runtime.Serialization;

namespace Rainbow.Storage
{
	[Serializable]
	public class SfsDeleteException : Exception
	{
		public SfsDeleteException(string message, Exception inner) : base(message, inner)
		{
		}

		protected SfsDeleteException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
