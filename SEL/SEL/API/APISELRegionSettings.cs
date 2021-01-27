namespace SEL.API
{
	/// <summary>
	/// Holds region-specific SEL config data.
	/// </summary>
	class APISELRegionSettings
	{
		public class MaintenanceDestinationSettings
		{
			public float construction_intensity_multiplier = 0;
			public float base_intensity_per_square_km = 0;

			public float point_construction_intensity = 0;
			public float point_intensity = 0;
		};

		public double shipping_lane_point_merge_distance = 1000;
		public double shipping_lane_subdivide_distance = 10000;
		public double shipping_lane_implicit_distance_limit = 30000;
		public double port_lane_max_merge_distance = 1000;
		public MaintenanceDestinationSettings maintenance_destinations = null;
		public double restriction_point_size = 5000;
	}
}
