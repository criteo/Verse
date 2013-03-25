﻿using System;
using System.Collections.Generic;

namespace Verse
{
	public delegate void DecoderMapSetter<T, U> (ref T container, IList<KeyValuePair<string, U>> map);
}