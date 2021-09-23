using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEL.Util
{
	class MathProviderFactory
	{
		private static Dictionary<Type, IMathProvider> ms_mathProviders = new Dictionary<Type, IMathProvider>();

		static MathProviderFactory()
		{
			ms_mathProviders.Add(typeof(int), new MathProviderInt());
			ms_mathProviders.Add(typeof(float), new MathProviderFloat());
		}

		public static MathProvider<NUMERIC_TYPE> CreateForNumericType<NUMERIC_TYPE>()
			where NUMERIC_TYPE: struct
		{
			IMathProvider provider;
			if (!ms_mathProviders.TryGetValue(typeof(NUMERIC_TYPE), out provider))
			{
				throw new ArgumentException(string.Format("No math provider found for type {0}. Please specify one in this factory.", typeof(NUMERIC_TYPE).Name));
			}
			return (MathProvider<NUMERIC_TYPE>)provider;
		}
	}
}
