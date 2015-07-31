using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Rainbow
{
	[Serializable, ExcludeFromCodeCoverage]
	public class DataConsistencyException : Exception
	{
		public DataConsistencyException()
		{
		}

		public DataConsistencyException(string message) : base(message)
		{
		}

		public DataConsistencyException(string message, Exception inner) : base(message, inner)
		{
		}

		protected DataConsistencyException(
			SerializationInfo info,
			StreamingContext context) : base(info, context)
		{
		}
	}
}
