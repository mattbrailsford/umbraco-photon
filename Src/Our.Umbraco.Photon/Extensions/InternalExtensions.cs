using System;
using System.Linq;
using System.Reflection;

namespace Our.Umbraco.Photon.Extensions
{
	internal static class InternalExtensions
	{
		public static T GetProperty<T>(this object obj, string propertyName)
		{
			var objType = obj.GetType();
			var accessorProp = objType.FindProperty(typeof(T), propertyName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

			return accessorProp == null
				? default(T)
				: (T)accessorProp.GetValue(obj);
		}

		public static T ExecuteSingletonMethod<T>(this Type objType, string singletonAccessor,
			string methodName, params object[] args)
		{
			var accessorProp = objType.FindProperty(objType, singletonAccessor,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
			if (accessorProp == null)
				return default(T);

			var instance = accessorProp.GetValue(objType);
			if (instance == null)
				return default(T);

			return instance.ExecuteMethod<T>(methodName, args);
		}

		public static T ExecuteMethod<T>(this object obj, string methodName, params object[] args)
		{
			var objType = obj.GetType(); 
			var returnType = typeof(T);
			var paramTypes = args.Select(x => x.GetType()).ToArray();

			var method = objType.FindMethod(returnType, methodName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
				paramTypes);

			if (method == null)
				throw new ApplicationException("No method with name '" + methodName + "' found with the right return type / method signature.");

			return (T)method.Invoke(obj, args);
		}

		//public static void ExecuteMethod(this object obj, string methodName, params object[] args)
		//{
		//	var objType = obj.GetType();
		//	var returnType = typeof(void);
		//	var paramTypes = args.Select(x => x.GetType()).ToArray();

		//	var method = objType.FindMethod(returnType, methodName,
		//		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
		//		paramTypes);

		//	if (method == null)
		//		throw new ApplicationException("No method with name '" + methodName + "' found with the right return type / method signature.");

		//	method.Invoke(obj, args);
		//}

		public static T ExecuteMethod<T>(this Type objType, string methodName, params object[] args)
		{
			var returnType = typeof(T);
			var paramTypes = args.Select(x => x.GetType()).ToArray();

			var method = objType.FindMethod(returnType, methodName,
				BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
				paramTypes);

			if (method == null)
				throw new ApplicationException("No method with name '" + methodName + "' found with the right return type / method signature.");

			return (T)method.Invoke(null, args);
		}

		//public static void ExecuteMethod(this Type objType, string methodName, params object[] args)
		//{
		//	var returnType = typeof(void);
		//	var paramTypes = args.Select(x => x.GetType()).ToArray();

		//	var method = objType.FindMethod(returnType, methodName,
		//		BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
		//		paramTypes);

		//	if (method == null)
		//		throw new ApplicationException("No method with name '" + methodName + "' found with the right return type / method signature.");

		//	method.Invoke(null, args);
		//}

		private static PropertyInfo FindProperty(this Type type, Type returnType, string propertyName, BindingFlags flags)
		{
			return type.GetProperties(flags)
				.FirstOrDefault(x =>
				{
					if (x.Name != propertyName) return false;
					if (x.PropertyType != returnType && !x.PropertyType.IsAssignableFrom(returnType)) return false;

					return true;
				});
		}

		private static MethodInfo FindMethod(this Type type, Type returnType, string methodName, BindingFlags flags, Type[] paramTypes)
		{
			return type.GetMethods(flags)
				.FirstOrDefault(x =>
				{
					if (x.Name != methodName) return false;
					if (x.ReturnType != returnType && !x.ReturnType.IsAssignableFrom(returnType)) return false;

					var parameters = x.GetParameters();
					if (paramTypes.Length == 0) return parameters.Length == 0;

					for (int i = 0; i < paramTypes.Length; i++)
					{
						if (parameters[i].ParameterType != paramTypes[i] &&
							!parameters[i].ParameterType.IsAssignableFrom(paramTypes[i]))
							return false;
					}

					return true;
				});
		}
	}
}
