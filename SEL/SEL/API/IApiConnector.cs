using System.Collections.Generic;
using MSWSupport;

namespace SEL.API
{
	internal interface IApiConnector: ITokenReceiver
	{
		bool CheckApiAccess();

		APISELRegionSettings GetSELRegionSettings();
		APIHeatmapOutputSettings[] GetHeatmapOutputSettings();
		APIHeatmapSettings GetHeatmapSettings();
		int GetCurrentMonth();
		APIUpdateDescriptor GetUpdatePackage();

		APIAreaOutputConfiguration GetAreaOutputConfiguration();
		APIConfiguredIntensityRoute[] GetConfiguredRouteIntensities();
		APIEEZGeometryData[] GetEEZGeometryData();

		APIShippingLaneGeometry[] GetShippingLanes();
		APIShippingPortGeometry[] GetShippingPortGeometry();
		APIShippingPortIntensity[] GetShippingPortIntensity();
		APIShippingRestrictionGeometry[] GetRestrictionGeometry();
		APIShipType[] GetShipTypes();
		APIRestrictionTypeException[] GetRestrictionTypeExceptions();

		void SetUpdateFinished(int monthId);
		void UpdateRasterImage(string layerName, byte[] imageBuffer); //Image encoded as png.
		void SetKpiValues(string kpiName, int kpiValue, string kpiCategory, string kpiUnit, int country);
		void BatchPostKPI(IEnumerable<APIKPIResult> kpiResults);
		void BatchPostIssues(IEnumerable<APIShippingIssue> shippingIssues);
		void SetShippingIntensityValues(IEnumerable<KeyValuePair<int, int>> perGeometryIntensityValues); //Per geometryId (key) intensity values (value)
		void ReportErrorMessage(EErrorSeverity severity, string errorMessage, string stackTrace);
	}
}
