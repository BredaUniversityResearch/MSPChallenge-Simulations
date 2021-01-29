using System.Collections.Generic;
using System.Drawing;
using EwEShell;
using MSWSupport;

namespace MEL
{
	public interface IApiConnector: ITokenReceiver
	{
		bool ShouldRasterizeLayers
		{
			get;
		}

		bool CheckAPIAccess();

		int GetCurrentGameMonth(int lastSimulatedMonth);
		void SetInitialFishingValues(List<cScalar> fishingValues);
		string GetMelConfigAsString();
		string[] GetUpdatedLayers();
		Fishing[] GetFishingValuesForMonth(int month);
		void SubmitKpi(string kpiName, int kpiMonth, double kpiValue, string kpiUnits);
		void NotifyTickDone();

		double[,] GetRasterLayerByName(string layerName);
		void SubmitRasterLayerData(string layerName, Bitmap rasterImage);

		APILayerGeometryData GetLayerData(string layerName, int layerType, bool constructionOnly);
		
		//Debug Only...
		double[,] GetRasterizedPressure(string name);
	}
}
