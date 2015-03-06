using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Verse.Tools
{
	static class Resolver
	{
		#region Methods

		public static MethodInfo Method<T> (Expression<T> lambda, Type[] callerParameters, Type[] methodParameters)
		{
			MethodCallExpression expression;
			MethodInfo method;
			Type type;

			expression = lambda.Body as MethodCallExpression;

			if (expression == null)
				throw new ArgumentException ("can't get method information from expression", "lambda");

			method = expression.Method;

			// Change method generic parameters if requested
			if (methodParameters != null && method.IsGenericMethod)
			{
				method = method.GetGenericMethodDefinition ();

				if (methodParameters.Length > 0)
					method = method.MakeGenericMethod (methodParameters);
			}

			// Change target generic parameters if requested
			if (callerParameters != null && method.DeclaringType.IsGenericType)
			{
				type = method.DeclaringType.GetGenericTypeDefinition ();

				if (callerParameters.Length > 0)
					type = type.MakeGenericType (callerParameters);

				method = Array.Find (type.GetMethods (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static), (m) => m.MetadataToken == method.MetadataToken);
			}

			return method;
		}

		#endregion
	}
}