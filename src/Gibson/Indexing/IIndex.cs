using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gibson.Indexing
{
	public interface IIndex
	{
		void Update(IndexEntry indexEntry);
	}
}
