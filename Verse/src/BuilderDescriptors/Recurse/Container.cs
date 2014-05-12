using System;
using System.Collections.Generic;

namespace Verse.BuilderDescriptors.Recurse
{
	class Container<T, C, V>
	{
		public Dictionary<string, Follow<T, C, V>>	fields = new Dictionary<string, Follow<T, C, V>> ();

		public Follow<T, C, V>						items = null;

		public Func<T, V>							value = null;
	}
}