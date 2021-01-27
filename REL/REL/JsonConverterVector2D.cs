using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace REL
{
	class JsonConverterVector2D: JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			Vector2D typedValue = (Vector2D)value;
			JArray array = new JArray(typedValue.x, typedValue.y);
			array.WriteTo(writer);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			JArray array = JArray.Load(reader);
			Vector2D result = new Vector2D(array[0].Value<double>(), array[1].Value<double>());
			return result;
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(Vector2D);
		}
	}
}
