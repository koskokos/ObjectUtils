using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicExtensions
{
    public class ObjectMerger : IObjectMerger
    {

        #region Caching
        private class TypeKey
        {
            readonly Type tOut;
            readonly Type[] types;

            public TypeKey(Type tOut, Type[] types)
            {
                this.tOut = tOut;
                this.types = types;
            }
            public override bool Equals(object obj)
            {
                if (typeof(TypeKey) != obj.GetType())
                {
                    return false;
                }
                var target = (TypeKey)obj;
                if (target.tOut != tOut)
                {
                    return false;
                }
                foreach (var t in types)
                {
                    if (!target.types.Contains(t)) return false;
                }
                return true;
            }
            public override int GetHashCode()
            {
                var res = tOut.GetHashCode();
                foreach (var item in types)
                {
                    res ^= item.GetHashCode();
                }
                return res;
            }
        }

        static readonly object sync = new object();
        static readonly Dictionary<TypeKey, Type> typesCache = new Dictionary<TypeKey, Type>();

        static Type GetCachedOrCreateType(Type[] types, Type tOut)
        {
            var typeKey = new TypeKey(tOut, types);
            Type tResult;
            if (!typesCache.TryGetValue(typeKey, out tResult))
            {
                lock (sync)
                {
                    if (!typesCache.TryGetValue(typeKey, out tResult))
                    {
                        tResult = CreateType(types, tOut);
                        typesCache.Add(typeKey, tResult);
                    }
                }
            }
            return tResult;
        }
        #endregion
        static ObjectMerger()
        {
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("ObjectMerger_" + Guid.NewGuid().ToString("N")),
                AssemblyBuilderAccess.Run);
            moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");
        }

        static readonly ModuleBuilder moduleBuilder;

        public static TypeBuilder CreateTypeBuilder(string typename)
        {
            return moduleBuilder.DefineType(typename,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout |
                TypeAttributes.Sealed,
                null);
        }

        private static void MapProperties(TypeBuilder typeBuilder, MethodBuilder[] newMethods, IEnumerable<PropertyInfo> props)
        {
            foreach (var prop in props)
            {
                var propBuilder = typeBuilder.DefineProperty(prop.Name, PropertyAttributes.None, prop.PropertyType, Type.EmptyTypes);
                if (prop.GetMethod != null)
                {
                    var method = newMethods
                        .Single(mi => mi.IsSpecialName && mi.Name == "get_" + prop.Name);

                    propBuilder.SetGetMethod(method);
                }
                if (prop.SetMethod != null)
                {
                    var method = newMethods
                        .Single(mi => mi.IsSpecialName && mi.Name == "set_" + prop.Name);

                    propBuilder.SetSetMethod(method);
                }
            }
        }

        private static IEnumerable<MethodBuilder> MapMethods(TypeBuilder typeBuilder, IEnumerable<MethodInfo> methods, FieldInfo target)
        {
            return methods.Select(method =>
            {
                var parameters = method.GetParameters();
                var attributes = method.Attributes & (~MethodAttributes.Abstract); // interfaces' methods are abstract, this should be removed
                var methodBuilder = typeBuilder.DefineMethod(method.Name, attributes, method.ReturnType, parameters.Select(p => p.ParameterType).ToArray());
                var methodGenerator = methodBuilder.GetILGenerator();
                methodGenerator.Emit(OpCodes.Ldarg_0);
                methodGenerator.Emit(OpCodes.Ldfld, target);
                for (byte i = 0; i < parameters.Length; i++)
                {
                    methodGenerator.Emit(OpCodes.Ldarg, i + 1); // i + 1 because arg_0 is "this" argument and actual arguments start from 1
                }
                methodGenerator.Emit(OpCodes.Call, method);
                methodGenerator.Emit(OpCodes.Ret);
                return methodBuilder;
            });
        }

        private static Type CreateType(Type[] types, Type tOut)
        {
            //var tOut = typeof(TOut);
            var name = $"{tOut.Name}_impl_{Guid.NewGuid().ToString("N")}";

            var typeBuilder = CreateTypeBuilder(name);
            // add interface implementation
            typeBuilder.AddInterfaceImplementation(tOut);
            // add fields to save objects
            var fieldAttrs = FieldAttributes.Private;

            var fields = types
                .Select((t, i) => typeBuilder.DefineField("obj" + i.ToString(), t, fieldAttrs))
                .ToArray();

            // add constructor to save objects
            var constrBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard | CallingConventions.HasThis, types);
            var constrGenerator = constrBuilder.GetILGenerator();
            for (int i = 0; i < fields.Length; i++)
            {
                constrGenerator.Emit(OpCodes.Ldarg_0);
                constrGenerator.Emit(OpCodes.Ldarg, i + 1); // i + 1 because arg_0 is "this" argument and actual arguments start from 1
                constrGenerator.Emit(OpCodes.Stfld, fields[i]);
            }
            constrGenerator.Emit(OpCodes.Ret);

            // add methods to map saved objects' methods
            var methods = types.Select(t => t.GetMethods());
            var methodsToFields = Enumerable.Zip(methods, fields, (m, f) => new { m, f });

            var newMethods = methodsToFields
                .SelectMany(item => MapMethods(typeBuilder, item.m, item.f)) // in MapMethods methods are actually created by typeBuilder
                .ToArray();

            // add properties to map to corresponding created methods
            var properties = types.Select(t => t.GetProperties());
            foreach (var prop in properties)
            {
                MapProperties(typeBuilder, newMethods, prop);
            }

            return typeBuilder.CreateTypeInfo().AsType();
        }

        public static TOut MergeInternal<TOut>(object[] objects, Type[] types)
        {
            var tResult = GetCachedOrCreateType(types, typeof(TOut));
            return (TOut)Activator.CreateInstance(tResult, objects);
        }

        TOut IObjectMerger.Merge<T1, T2, TOut>(T1 obj1, T2 obj2)
        {
            var t1 = typeof(T1);
            var t2 = typeof(T2);
            var tOut = typeof(TOut);

            if (!t1.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("Type T1 of argument obj1 must be an interface.");
            }

            if (!t2.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("Type T2 of argument obj2 must be an interface.");
            }

            if (!tOut.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("Type TOut of return value must be an interface.");
            }

            var outInterfaces = tOut.GetInterfaces();
            var minimalInterfaces = outInterfaces.Except
                        (outInterfaces.SelectMany(t => t.GetInterfaces()).Distinct()).ToList();


            if (minimalInterfaces.Count != 2 || !minimalInterfaces.Contains(t1) || !minimalInterfaces.Contains(t2))
            {
                throw new ArgumentException("T1 and T2 must be only direct ancestors of TOut");
            }

            return MergeInternal<TOut>(new object[] { obj1, obj2 }, new[] { t1, t2 });
        }

    }
}
