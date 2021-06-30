using System.IO;
using Newtonsoft.Json;

namespace REL.API
{
	public class MarinAPIConnectorDebug : IMarinAPIConnector
	{
		public void SubmitInput(MarinAPIInput a_input)
		{
			JsonSerializer serializer = new JsonSerializer 
			{
				Formatting = Formatting.Indented
			};
			APIUtils.AddConverters(serializer);

			using (StreamWriter sw = new StreamWriter("Debug_MarinApiInput.json"))
			{
				using (JsonWriter writer = new JsonTextWriter(sw))
				{
					serializer.Serialize(writer, a_input);
				}
			}
		}

		public MarinAPIProcessResponse TryGetProcessResponse()
		{
			MarinAPIProcessResponse result = null;
			using (StreamReader sr = new StreamReader("Debug_MarinResponse.json"))
			{
				JsonSerializer serializer = new JsonSerializer();
				using (JsonReader reader = new JsonTextReader(sr))
				{
					result = serializer.Deserialize<MarinAPIProcessResponse>(reader);
				}
			}

			return result;
		}
	}
}