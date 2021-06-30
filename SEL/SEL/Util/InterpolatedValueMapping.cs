using System;
using System.Collections.Generic;

namespace SEL.Util
{
	/*
	 * Maps an input value to an output value using a sparsely populated mapping table.
	 * In other words: Takes inputs (0 -> 0, 1 -> 10, 2 -> 20) and maps any input value to an interpolated output value (0 -> 0, 1 -> 10, 1.5 -> 15)
	 */
	public class InterpolatedValueMapping<INPUT_TYPE, OUTPUT_TYPE>: IValueMapping<INPUT_TYPE, OUTPUT_TYPE>
		where INPUT_TYPE: struct
		where OUTPUT_TYPE: struct
	{
		private List<KeyValuePair<INPUT_TYPE, OUTPUT_TYPE>> m_values = new List<KeyValuePair<INPUT_TYPE, OUTPUT_TYPE>>();

		private MathProvider<INPUT_TYPE> m_inputMath = MathProviderFactory.CreateForNumericType<INPUT_TYPE>();
		private MathProvider<OUTPUT_TYPE> m_outputMath = MathProviderFactory.CreateForNumericType<OUTPUT_TYPE>();

		public void Add(INPUT_TYPE input, OUTPUT_TYPE output)
		{
			m_values.Add(new KeyValuePair<INPUT_TYPE, OUTPUT_TYPE>(input, output));
		}

		public OUTPUT_TYPE Map(INPUT_TYPE input)
		{
			bool mappedValue = false;
			OUTPUT_TYPE result = default(OUTPUT_TYPE);
			for (int i = 0; i < m_values.Count; ++i)
			{
				if (!IsLess(m_values[i].Key, input))
				{
					if (i == 0)
					{
						//First entry in our mapping table. Return the associated output.
						result = m_values[0].Value;
						mappedValue = true;
						break;
					}
					else
					{
						KeyValuePair<INPUT_TYPE, OUTPUT_TYPE> from = m_values[i - 1];
						KeyValuePair<INPUT_TYPE, OUTPUT_TYPE> to = m_values[i];
						result = Interpolate(from, to, input);
						mappedValue = true;
						break;
					}
				}
			}

			if (!mappedValue)
			{
				result = m_values[m_values.Count - 1].Value;
			}

			return result;
		}

		private bool IsLess(INPUT_TYPE lhs, INPUT_TYPE rhs)
		{
			//return lhs < rhs;
			return m_inputMath.IsLess(lhs, rhs);
		}

		private OUTPUT_TYPE Interpolate(KeyValuePair<INPUT_TYPE, OUTPUT_TYPE> from, KeyValuePair<INPUT_TYPE, OUTPUT_TYPE> to, INPUT_TYPE value)
		{
			float inputLerp = GetLerpValue(from.Key, to.Key, value);
			return Lerp(from.Value, to.Value, inputLerp);
		}

		private float GetLerpValue(INPUT_TYPE from, INPUT_TYPE to, INPUT_TYPE value)
		{
			//return (float)(value - from) / (float)(to - from);
			return m_inputMath.AsFloat(m_inputMath.Substract(value, from)) / m_inputMath.AsFloat(m_inputMath.Substract(to, from));
		}

		private OUTPUT_TYPE Lerp(OUTPUT_TYPE from, OUTPUT_TYPE to, float lerpValue)
		{
			//return (int)(from + (float)(lerpValue * (float)(to - from)));
			return m_outputMath.FromFloat(m_outputMath.AsFloat(from) + (lerpValue * m_outputMath.AsFloat(m_outputMath.Substract(to, from))));
		}
	}
}
