using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;

namespace RhythmTool
{
    /// <summary>
    /// Utility class for creating matching generic types.
    /// </summary>
    /// <typeparam name="T">The type to find.</typeparam>
    public static class Bindings<T>
    {
        delegate T ObjectActivator(params object[] args);

        private static Dictionary<Type, ObjectActivator> bindings = new Dictionary<Type, ObjectActivator>();

        /// <summary>
        /// Find the most concrete implementation of type T that matches args.
        /// </summary>
        /// <param name="args">Arguments that match the constructor of T.</param>
        /// <returns>An instance of the most concrete implementation.</returns>
        public static T GetBinding(params object[] args)
        {
            Type type = args[0].GetType();

            if(type.IsGenericType)
                type = type.GetGenericArguments()[0];

            if(type.BaseType.IsGenericType)
                type = type.BaseType.GetGenericArguments()[0];

            ObjectActivator activator = GetCachedActivator(type);

            return activator(args);
        }

        private static ObjectActivator GetCachedActivator(Type type)
        {
            ObjectActivator activator;

            if (bindings.TryGetValue(type, out activator))
                return activator;

            Type bindingType = GetBindingType(type);

            var ctor = GetConstructor(bindingType);

            activator = GetActivator(ctor);

            bindings.Add(type, activator);

            return activator;
        }

        private static ObjectActivator GetActivator(ConstructorInfo ctor)
        {
            ParameterInfo[] paramsInfo = ctor.GetParameters();

            ParameterExpression param = Expression.Parameter(typeof(object[]), "args");

            Expression[] argsExp = new Expression[paramsInfo.Length];

            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;

                Expression paramAccessorExp = Expression.ArrayIndex(param, index);

                Expression paramCastExp = Expression.Convert(paramAccessorExp, paramType);

                argsExp[i] = paramCastExp;
            }

            NewExpression newExp = Expression.New(ctor, argsExp);

            LambdaExpression lambda = Expression.Lambda(typeof(ObjectActivator), newExp, param);

            ObjectActivator compiled = (ObjectActivator)lambda.Compile();

            return compiled;
        }

        private static Type GetBindingType(Type featureType)
        {
            Type bindingType = typeof(T);

            foreach (Type type in bindingType.Assembly.GetTypes())
            {
                if (type.IsGenericType && bindingType.IsAssignableFrom(type))
                    bindingType = type;
            }

            if (bindingType.IsGenericTypeDefinition)
                bindingType = bindingType.MakeGenericType(featureType);

            foreach (Type type in bindingType.Assembly.GetTypes())
            {
                if (type.IsSubclassOf(bindingType) && !type.IsAbstract)
                    return type;
            }

            return bindingType;
        }

        private static void GetGenericBindingType(Type featureType)
        {

        }

        private static ConstructorInfo GetConstructor(Type type)
        {
            var ctors = type.GetConstructors();

            foreach (var ctor in ctors)
            {
                if (ctor.GetParameters().Length > 0)
                    return ctor;
            }

            return ctors[0];
        }
    }
}
