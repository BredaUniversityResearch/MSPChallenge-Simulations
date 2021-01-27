using System;

namespace SEL
{
	public class IntensityMapBleedMatrixCollection
	{
		private float[][,] m_bleedMatrices;

		public IntensityMapBleedMatrixCollection(int numberOfBleedMatrices)
		{
			m_bleedMatrices = new float[numberOfBleedMatrices][,];
			for (int i = 0; i < m_bleedMatrices.Length; ++i)
			{
				int dimension = 3 + (2 * i); //3, 5, 7, 9 etc.
				m_bleedMatrices[i] = CalculateBleedingKernel(dimension);
			}
		}

		public float[,] GetBleedingKernel(int bleedingKernelIndex)
		{
			int index = Math.Min(m_bleedMatrices.Length - 1, bleedingKernelIndex);
			return m_bleedMatrices[index];
		}

		private static float[,] CalculateBleedingKernel(int dimensions)
		{
			float[,] result = new float[dimensions, dimensions];

			int centerX = (dimensions - 1) / 2;
			int centerY = (dimensions - 1) / 2;
			float maxDistanceFromCentre = (float)(centerX * centerX) + (float)(centerY * centerY);

			for (int y = 0; y < dimensions; ++y)
			{
				for (int x = 0; x < dimensions; ++x)
				{
					int deltaX = x - centerX;
					int deltaY = y - centerY;

					if (deltaX == 0 && deltaY == 0)
					{
						result[x, y] = 0.0f;
						continue;
					}

					float distanceFromCenter = ((float)(deltaX * deltaX) + (float)(deltaY * deltaY));
					float unitDistanceFromCenter = distanceFromCenter / maxDistanceFromCentre;
					result[x, y] = 1.0f - ((float)Math.Pow(unitDistanceFromCenter, 0.35));
				}
			}

			//Now normalize the kernel...
			float summedValue = 0.0f;
			foreach (float value in result)
			{
				summedValue += value;
			}

			float normalizationMultiplier = 1.0f / summedValue;
			for (int y = 0; y < result.GetLength(1); ++y)
			{
				for (int x = 0; x < result.GetLength(0); ++x)
				{
					result[x, y] *= normalizationMultiplier;
				}
			}

			return result;
		}
	}
}
