using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SEL.Util
{
	class MathProviderFloat : MathProvider<float>
	{
		public override float Add(float lhs, float rhs)
		{
			return lhs + rhs;
		}

		public override float AsFloat(float input)
		{
			return input;
		}

		public override float Divide(float lhs, float rhs)
		{
			return lhs / rhs;
		}

		public override float FromFloat(float input)
		{
			return input;
		}

		public override bool IsLess(float lhs, float rhs)
		{
			return lhs < rhs;
		}

		public override float Multiply(float lhs, float rhs)
		{
			return lhs * rhs;
		}

		public override float Substract(float lhs, float rhs)
		{
			return lhs - rhs;
		}
	}
}
