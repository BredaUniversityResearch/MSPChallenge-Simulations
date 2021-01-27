namespace REL.API
{
	interface IMarinAPIConnector
	{
		void SubmitInput(MarinAPIInput a_input);
		MarinAPIProcessResponse TryGetProcessResponse();
	}
}
