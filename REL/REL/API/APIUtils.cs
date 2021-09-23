using Newtonsoft.Json;

namespace REL.API
{
	public static class APIUtils
	{
		public static JsonConverter[] AvailableConverters = {new JsonConverterVector2D()};

		public static void AddConverters(JsonSerializer a_serializer)
		{
			foreach (JsonConverter converter in AvailableConverters)
			{
				a_serializer.Converters.Add(converter);
			}
		}
	}
}
