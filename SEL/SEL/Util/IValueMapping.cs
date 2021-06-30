namespace SEL.Util
{
	interface IValueMapper<in INPUT_TYPE, out OUTPUT_TYPE>
	{
		OUTPUT_TYPE Map(INPUT_TYPE input);
	}

	interface IValueMapping<INPUT_TYPE, OUTPUT_TYPE>: IValueMapper<INPUT_TYPE, OUTPUT_TYPE>
	{
		void Add(INPUT_TYPE input, OUTPUT_TYPE output);
	}
}
