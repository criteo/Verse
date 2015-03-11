using System;
using System.Collections.Generic;
using System.Globalization;
using Verse.BuilderDescriptors.Recurse;

namespace Verse.Schemas.JSON
{
	class ValueEncoder : IEncoder<Value>
	{
		#region Attributes

		private readonly Dictionary<Type, object> converters = new Dictionary<Type, object>
		{
			{typeof (bool),		new Converter<bool, Value> (Value.FromBoolean)},
			{typeof (char),		new Converter<char, Value> ((v) => Value.FromString (new string (v, 1)))},
			{typeof (decimal),	new Converter<decimal, Value> ((v) => Value.FromNumber ((double)v))},
			{typeof (float),	new Converter<float, Value> ((v) => Value.FromNumber (v))},
			{typeof (double),	new Converter<double, Value> (Value.FromNumber)},
			{typeof (sbyte),	new Converter<sbyte, Value> ((v) => Value.FromNumber (v))},
			{typeof (byte),		new Converter<byte, Value> ((v) => Value.FromNumber (v))},
			{typeof (short),	new Converter<short, Value> ((v) => Value.FromNumber (v))},
			{typeof (ushort),	new Converter<ushort, Value> ((v) => Value.FromNumber (v))},
			{typeof (int),		new Converter<int, Value> ((v) => Value.FromNumber (v))},
			{typeof (uint),		new Converter<uint, Value> ((v) => Value.FromNumber (v))},
			{typeof (long),		new Converter<long, Value> ((v) => Value.FromNumber (v))},
			{typeof (ulong),	new Converter<ulong, Value> ((v) => Value.FromNumber (v))},
			{typeof (string),	new Converter<string, Value> (Value.FromString)},
			{typeof (Value),	new Converter<Value, Value> ((v) => v)}
		};

		#endregion

		#region Methods

		public Converter<T, Value> Get<T> ()
		{
			object	box;

			if (!this.converters.TryGetValue (typeof (T), out box))
				throw new InvalidCastException (string.Format (CultureInfo.InvariantCulture, "no available converter from type '{0}', JSON value", typeof (T)));

			return (Converter<T, Value>)box;
		}

		public void Set<T> (Converter<T, Value> converter)
		{
			if (converter == null)
				throw new ArgumentNullException ("converter");

			this.converters[typeof (T)] = converter;
		}

		#endregion
	}
}
