using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEL.Util
{
	interface IMathProvider
	{
	}

	abstract class MathProvider<NUMERIC_TYPE>: IMathProvider
		where NUMERIC_TYPE : struct
	{
		public abstract bool IsLess(NUMERIC_TYPE lhs, NUMERIC_TYPE rhs);

		public abstract float AsFloat(NUMERIC_TYPE input);
		public abstract NUMERIC_TYPE FromFloat(float input);

		public abstract NUMERIC_TYPE Add(NUMERIC_TYPE lhs, NUMERIC_TYPE rhs);
		public abstract NUMERIC_TYPE Substract(NUMERIC_TYPE lhs, NUMERIC_TYPE rhs);
		public abstract NUMERIC_TYPE Multiply(NUMERIC_TYPE lhs, NUMERIC_TYPE rhs);
		public abstract NUMERIC_TYPE Divide(NUMERIC_TYPE lhs, NUMERIC_TYPE rhs);
	}
}
