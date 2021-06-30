using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEL.Util
{
	class MathProviderInt : MathProvider<int>
	{
		public override int Add(int lhs, int rhs)
		{
			return lhs + rhs;
		}

		public override float AsFloat(int input)
		{
			return (float)input;
		}

		public override int Divide(int lhs, int rhs)
		{
			return lhs / rhs;
		}

		public override int FromFloat(float input)
		{
			return (int)input;
		}

		public override bool IsLess(int lhs, int rhs)
		{
			return lhs < rhs;
		}

		public override int Multiply(int lhs, int rhs)
		{
			return lhs * rhs;
		}

		public override int Substract(int lhs, int rhs)
		{
			return lhs - rhs;
		}
	}
}
