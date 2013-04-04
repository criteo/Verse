﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using Verse.Dynamics;
using Verse.Events;

namespace Verse.Models
{
	abstract class	AbstractEncoder<T> : IEncoder<T>
	{
		#region Events
		
		public event StreamErrorEvent	OnStreamError;

		public event TypeErrorEvent		OnTypeError;
		
		#endregion

		#region Methods / Abstract

		public abstract bool		Encode (Stream stream, T instance);

		public abstract IEncoder<U>	HasField<U> (string name, EncoderValueGetter<T, U> getter);

		public abstract IEncoder<U>	HasItems<U> (EncoderArrayGetter<T, U> getter);

		public abstract IEncoder<U>	HasPairs<U> (EncoderMapGetter<T, U> getter);

		protected abstract bool		TryLink ();

		#endregion

		#region Methods / Public

		public void		Link ()
		{
			Type[]		arguments;
			Type		container;
			TypeFilter	filter;
			Type		inner;

			if (this.TryLink ())
				return;

			container = typeof (T);

			// Check whether type has items or pairs
			filter = new TypeFilter ((type, criteria) => type.IsGenericType && type.GetGenericTypeDefinition () == typeof (IEnumerable<>));

			#warning Search in type itself (FindInterface won't find anything if type is IEnumerable<T>)
			foreach (Type contract in container.FindInterfaces (filter, null))
			{
				arguments = contract.GetGenericArguments ();

				if (arguments.Length == 1)
				{
					inner = arguments[0];

					if (inner.IsGenericType && inner.GetGenericTypeDefinition () == typeof (KeyValuePair<,>))
					{
						arguments = inner.GetGenericArguments ();
	
						if (arguments.Length == 2 && arguments[0] == typeof (string))
						{
							inner = arguments[1];

							AbstractEncoder<T>.LinkInvoke (inner, MethodResolver
								.Resolve<Func<IEncoder<T>, EncoderMapGetter<T, object>, IEncoder<object>>> ((encoder, getter) => encoder.HasPairs (getter))
								.MakeGenericMethod (inner)
								.Invoke (this, new object[] {AbstractEncoder<T>.MakeMapGetter (container, inner)}));

							return;
						}
					}

					AbstractEncoder<T>.LinkInvoke (inner, MethodResolver
						.Resolve<Func<IEncoder<T>, EncoderArrayGetter<T, object>, IEncoder<object>>> ((encoder, getter) => encoder.HasItems (getter))
						.MakeGenericMethod (inner)
						.Invoke (this, new object[] {AbstractEncoder<T>.MakeArrayGetter (container, inner)}));

					return;
				}
			}

			// Browse public readable and writable properties
			foreach (PropertyInfo property in container.GetProperties (BindingFlags.Instance | BindingFlags.Public))
			{
				if (property.GetGetMethod () == null || property.GetSetMethod () == null || (property.Attributes & PropertyAttributes.SpecialName) == PropertyAttributes.SpecialName)
					continue;

				AbstractEncoder<T>.LinkInvoke (property.PropertyType, MethodResolver
					.Resolve<Func<IEncoder<T>, string, EncoderValueGetter<T, object>, IEncoder<object>>> ((encoder, name, getter) => encoder.HasField (name, getter))
					.MakeGenericMethod (property.PropertyType)
					.Invoke (this, new object[] {property.Name, AbstractEncoder<T>.MakeValueGetter (property)}));
			}

			// Browse public fields
			foreach (FieldInfo field in container.GetFields (BindingFlags.Instance | BindingFlags.Public))
			{
				if ((field.Attributes & FieldAttributes.SpecialName) == FieldAttributes.SpecialName)
					continue;

				AbstractEncoder<T>.LinkInvoke (field.FieldType, MethodResolver
					.Resolve<Func<IEncoder<T>, string, EncoderValueGetter<T, object>, IEncoder<object>>> ((encoder, name, getter) => encoder.HasField (name, getter))
					.MakeGenericMethod (field.FieldType)
					.Invoke (this, new object[] {field.Name, AbstractEncoder<T>.MakeValueGetter (field)}));
			}
		}

		#endregion

		#region Methods / Protected

		protected void	EventStreamError (long position, string message)
		{
			StreamErrorEvent	error;

			error = this.OnStreamError;

			if (error != null)
				error (position, message);
		}

		protected void	EventTypeError (Type type, string value)
		{
			TypeErrorEvent	error;

			error = this.OnTypeError;

			if (error != null)
				error (type, value);
		}

		#endregion

		#region Methods / Private

		private static void	LinkInvoke (Type type, object encoder)
		{
			typeof (IEncoder<>)
				.MakeGenericType (type)
				.GetMethod ("Link", BindingFlags.Instance | BindingFlags.Public)
				.Invoke (encoder, null);
		}

		private static object	MakeArrayGetter (Type container, Type inner)
		{
        	ILGenerator		generator;
			DynamicMethod	method;

			method = new DynamicMethod (string.Empty, typeof (IEnumerable<>).MakeGenericType (inner), new Type[] {container}, container.Module, true);

			generator = method.GetILGenerator ();
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ret);

			return method.CreateDelegate (typeof (EncoderArrayGetter<,>).MakeGenericType (container, inner));
		}

		private static object	MakeMapGetter (Type container, Type inner)
		{
        	ILGenerator		generator;
			DynamicMethod	method;

			method = new DynamicMethod (string.Empty, typeof (IEnumerable<>).MakeGenericType (typeof (KeyValuePair<,>).MakeGenericType (typeof (string), inner)), new Type[] {container}, container.Module, true);

			generator = method.GetILGenerator ();
			generator.Emit (OpCodes.Ldarg_0);
			generator.Emit (OpCodes.Ret);

			return method.CreateDelegate (typeof (EncoderMapGetter<,>).MakeGenericType (container, inner));
		}

		private static object	MakeValueGetter (FieldInfo field)
		{
        	ILGenerator		generator;
			DynamicMethod	method;

			method = new DynamicMethod (string.Empty, field.FieldType, new Type[] {field.DeclaringType}, field.Module, true);

			generator = method.GetILGenerator ();

			if (field.DeclaringType.IsValueType)
				generator.Emit (OpCodes.Ldarga_S, 0);
			else
				generator.Emit (OpCodes.Ldarg_0);

			generator.Emit (OpCodes.Ldfld, field);
			generator.Emit (OpCodes.Ret);

			return method.CreateDelegate (typeof (EncoderValueGetter<,>).MakeGenericType (field.DeclaringType, field.FieldType));
		}

		private static object	MakeValueGetter (PropertyInfo property)
		{
        	ILGenerator		generator;
			DynamicMethod	method;

			method = new DynamicMethod (string.Empty, property.PropertyType, new Type[] {property.DeclaringType}, property.Module, true);

			generator = method.GetILGenerator ();

			if (property.DeclaringType.IsValueType)
				generator.Emit (OpCodes.Ldarga_S, 0);
			else
				generator.Emit (OpCodes.Ldarg_0);

			generator.Emit (OpCodes.Call, property.GetGetMethod ());
			generator.Emit (OpCodes.Ret);

			return method.CreateDelegate (typeof (EncoderValueGetter<,>).MakeGenericType (property.DeclaringType, property.PropertyType));
		}

		#endregion
	}
}
