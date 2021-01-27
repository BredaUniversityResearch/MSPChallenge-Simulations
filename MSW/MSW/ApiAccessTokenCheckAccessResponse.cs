using System.Diagnostics.CodeAnalysis;

namespace MSW
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	class ApiAccessTokenCheckAccessResponse
	{
		public enum EResponse
		{
			Valid,
			UpForRenewal,
			Expired
		};

		public EResponse status = EResponse.Expired;
	};
}