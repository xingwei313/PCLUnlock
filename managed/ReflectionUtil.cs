using System;
using System.Linq.Expressions;
using System.Reflection;

namespace PCLUnlock
{
    /// <summary>
    /// 反射访问 PCL 内部成员的工具方法。
    /// </summary>
    internal static class ReflectionUtil
    {
        public static object GetMemberValue(object instance, string name)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(instance, null);
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(instance) : null;
        }

        public static void SetMemberValue(object instance, string name, object value)
        {
            Type type = instance.GetType();
            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null && property.CanWrite)
            {
                property.SetValue(instance, value, null);
                return;
            }
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(instance, value);
        }

        public static object GetStaticMember(Type type, string name)
        {
            PropertyInfo property = type.GetProperty(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (property != null)
                return property.GetValue(null, null);
            FieldInfo field = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null ? field.GetValue(null) : null;
        }

        public static MethodInfo FindMethod(Type type, string name, int parameterCount, bool isStatic)
        {
            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | (isStatic ? BindingFlags.Static : BindingFlags.Instance);
            foreach (MethodInfo method in type.GetMethods(flags))
            {
                if (method.Name == name && method.GetParameters().Length == parameterCount)
                    return method;
            }
            return null;
        }

        public static Delegate CreateHandler(Type handlerType, object target, MethodInfo method)
        {
            ParameterInfo[] handlerParameters = handlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression[] parameters = new ParameterExpression[handlerParameters.Length];
            for (int i = 0; i < handlerParameters.Length; i++)
                parameters[i] = Expression.Parameter(handlerParameters[i].ParameterType, "p" + i);
            ParameterInfo[] methodParameters = method.GetParameters();
            Expression[] arguments = new Expression[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
            {
                if (i < parameters.Length)
                    arguments[i] = Expression.Convert(parameters[i], methodParameters[i].ParameterType);
                else
                    arguments[i] = Expression.Default(methodParameters[i].ParameterType);
            }
            Expression call = Expression.Call(Expression.Constant(target), method, arguments);
            return Expression.Lambda(handlerType, call, parameters).Compile();
        }

        public static Delegate CreateStaticHandler(Type handlerType, MethodInfo method)
        {
            ParameterInfo[] handlerParameters = handlerType.GetMethod("Invoke").GetParameters();
            ParameterExpression[] parameters = new ParameterExpression[handlerParameters.Length];
            for (int i = 0; i < handlerParameters.Length; i++)
                parameters[i] = Expression.Parameter(handlerParameters[i].ParameterType, "p" + i);
            ParameterInfo[] methodParameters = method.GetParameters();
            Expression[] arguments = new Expression[methodParameters.Length];
            for (int i = 0; i < methodParameters.Length; i++)
                arguments[i] = Expression.Convert(parameters[i], methodParameters[i].ParameterType);
            return Expression.Lambda(handlerType, Expression.Call(method, arguments), parameters).Compile();
        }
    }
}
