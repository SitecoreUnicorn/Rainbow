using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rainbow.Storage
{
	public class DefaultFieldValueManipulator : IFieldValueManipulator
	{
		public DefaultFieldValueManipulator() {}

		public IFieldValueTransformer GetFieldValueTransformer(string fieldName)
		{
			return new DefaultFieldValueTransformer();
		}

		public IFieldValueTransformer[] GetFieldValueTransformers()
		{
			return new IFieldValueTransformer[] {new DefaultFieldValueTransformer()};
		}
	}
}
