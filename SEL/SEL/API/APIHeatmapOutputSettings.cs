using SEL.Util;
using System.Runtime.Serialization;
using System;

namespace SEL.API
{
	/// <summary>
	/// Settings relevant to creating our heatmap.
	/// </summary>
	public class APIHeatmapOutputSettings: IValueMapper<int, float>
	{
		public class HeatmapMapping
		{
			public int input; //Amount of ship calls that go over a certain raster grid.
			public float output; //Normalised output corresponding to the input.
		}

		public string layer_name;
		public bool output_for_mel;
		public EHeatmapType heatmap_type;
		public int[] ship_type_ids;
		public double[][] raster_bounds; //Only here for validation purposes to ensure the layer bounds are the same as the one we got as an input parameter for all layers.

		public HeatmapMapping[] heatmap_range;
		private InterpolatedValueMapping<int, float> m_heatmapRangeValueMap = new InterpolatedValueMapping<int, float>();

		[OnDeserialized]
		public void OnDeserlialise(StreamingContext context)
		{
			int lastInputValue = 0;
			foreach(HeatmapMapping mapping in heatmap_range)
			{
				if (mapping.input < lastInputValue)
					throw new Exception("Heatmap mapping values not ordered by input value. Please make sure the config file defines these values from low to high (0 -> 1) for the input range.");
				lastInputValue = mapping.input;
				m_heatmapRangeValueMap.Add(mapping.input, mapping.output);
			}
		}

		public float Map(int shipCalls)
		{
			return m_heatmapRangeValueMap.Map(shipCalls);
		}

		public int GetMaxMappedIntensity()
		{
			return heatmap_range[heatmap_range.Length - 1].input;
		}
	}
}
