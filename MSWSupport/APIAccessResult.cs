using System.Diagnostics.CodeAnalysis;

namespace MSWSupport
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class APIAccessResult
	{
		public enum EResult
		{
			Valid,
			UpForRenewal,
			Expired
		};

		public EResult status = EResult.Expired;
	};
}
