using MSWSupport;

namespace REL.API
{
	interface IMSPAPIConnector: ITokenReceiver
	{
		MSPAPIDate GetDateForSimulatedMonth(int a_simulatedMonth);
		MSPAPIGeometry[] GetGeometry();
		MSPAPIRELConfig GetConfiguration();

		void UpdateRaster(string a_layerName, Bounds2D a_rasterBounds, byte[] a_rasterImageData);
	}
}
