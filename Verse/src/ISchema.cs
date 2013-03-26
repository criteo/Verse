﻿using System;

using Verse.Events;

namespace Verse
{
	public interface ISchema
	{
		#region Events
		
		event StreamErrorEvent	OnStreamError;
		
		event TypeErrorEvent	OnTypeError;
		
		#endregion

		#region Methods

		IDecoder<T>	GetDecoder<T> (Func<T> constructor);

		IDecoder<T>	GetDecoder<T> ();

		IEncoder<T>	GetEncoder<T> ();

		#endregion
	}
}
