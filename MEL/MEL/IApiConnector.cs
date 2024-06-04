using System.Collections.Generic;
using System.Drawing;
using EwEMSPLink;
using Newtonsoft.Json.Linq;

namespace MEL
{
	public interface IApiConnector
	{
		bool ShouldRasterizeLayers
		{
			get;
		}

		void SetInitialFishingValues(List<cScalar> fishingValues);
		string? GetMelConfigAsString();
		string[] GetUpdatedLayers();
		Fishing[] GetFishingValuesForMonth(int month);
		void SubmitKpi(string kpiName, int kpiMonth, double kpiValue, string kpiUnits);
		void NotifyTickDone();

		double[,]? GetRasterLayerByName(string? layerName);
		void SubmitRasterLayerData(string layerName, Bitmap rasterImage);

		APILayerGeometryData? GetLayerData(
			string? layerName,
            int layerType,
            bool constructionOnly,
			JObject? policyFilters = null
		);

		//Debug Only...
		double[,]? GetRasterizedPressure(string name);
	}
}
