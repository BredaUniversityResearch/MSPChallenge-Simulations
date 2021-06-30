using Newtonsoft.Json;

namespace SEL.API
{
	/// <summary>
	/// Class containing EEZ geometry data.
	/// </summary>
	public class APIEEZGeometryData: APIGeometryData
	{
		public int geometry_type;
		public int owning_country_id;
	}
}
