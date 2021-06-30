using System.Collections.Generic;

namespace SEL.Util
{
	/// <summary>
	/// Allows for mapping input values to output values. The output value will always be dependent on the lowest input value that does not exceed the requested value. 
	/// Example: 
	/// Add(10, 100)
	/// Add(20, 200)
	/// Add(30, 300)
	/// Map(10) -> 100
	/// Map(15) -> 100
	/// Map(21) -> 200
	/// </summary>
	public class SparseValueMapping: IValueMapping<int, int>
	{
		private SortedList<int, int> m_sparseValues = new SortedList<int, int>();

		public void Add(int inputValue, int outputValue)
		{
			if (m_sparseValues.ContainsKey(inputValue))
			{
				m_sparseValues[inputValue] = outputValue;
			}
			else
			{
				m_sparseValues.Add(inputValue, outputValue);
			}
		}

		public int Map(int input)
		{
			int result = m_sparseValues.Values[0];
			foreach(var kvp in m_sparseValues)
			{
				if (kvp.Key <= input)
				{
					result = kvp.Value;
				}
				else
				{
					break;
				}
			}
			return result;
		}
	}
}
