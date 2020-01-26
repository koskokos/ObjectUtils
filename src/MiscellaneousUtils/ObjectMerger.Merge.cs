using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MiscellaneousUtils
{
    public partial class ObjectMerger
    {

        #region Types creation and caching
        
        private static Type CreateMergeType(Type[] types, Type tOut)
        {
            //var tOut = typeof(TOut);
            var name = $"{tOut.Name}_impl_{Guid.NewGuid().ToString("N")}";

            var typeBuilder = moduleBuilder.CreateTypeBuilder(name);
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
            var methodsToFields = Enumerable.Zip(fields, types, (f, t) => new { f, t })
                .SelectMany(item =>
                {
                    var ti = item.t.GetTypeInfo();
                    return Enumerable.Repeat(ti.DeclaredMethods, 1)
                        .Union(ti.ImplementedInterfaces.Select(i => i.GetTypeInfo().DeclaredMethods))
                        .Select(m => new { m, item.f });
                });

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
                methodGenerator.Emit(OpCodes.Callvirt, method);
                methodGenerator.Emit(OpCodes.Ret);
                return methodBuilder;
            });
        }
        #endregion

#region Constructors creation and caching

        private static void ValidateMergeCtorCreation(Type tOut, Type[] types)
        {
            var typesCount = types.Length;

            for (int i = 1; i <= typesCount; i++)
            {
                var t = types[i - 1];
                if (!t.GetTypeInfo().IsInterface)
                {
                    throw new ArgumentException($"Type T{i} of argument obj{i} must be an interface.");
                }
            }

            if (!tOut.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("Type TOut of return value must be an interface.");
            }

            var outInterfaces = tOut.GetTypeInfo().GetInterfaces();
            var minimalInterfaces = outInterfaces.Except
                        (outInterfaces.SelectMany(t => t.GetInterfaces()).Distinct()).ToList();


            if (minimalInterfaces.Count != typesCount || types.Any(t => !minimalInterfaces.Contains(t)))
            {
                throw new ArgumentException($"{string.Join(" and ", types.Select((t, i) => "T" + (i + 1).ToString()))} must be only direct ancestors of TOut");
            }
        }

        static readonly IDictionary<CtorKey, object> mergeCtorsCache = new ConcurrentDictionary<CtorKey, object>();

        internal static Func<object[], TOut> MakeMergeCtor<TOut>(Type[] types)
        {
            var tOut = typeof(TOut);
            ValidateMergeCtorCreation(tOut, types);
            var tResult = CreateMergeType(types, tOut);
            return CreateConstructor<TOut>(tResult, types);
        }

        TOut IObjectMerger.Merge<T1, T2, TOut>(T1 obj1, T2 obj2)
        {
            var t1 = typeof(T1);
            var t2 = typeof(T2);
            var ctor = GetCachedOrCreateCtor(mergeCtorsCache, new[] { typeof(T1), typeof(T2) }, MakeMergeCtor<TOut>);
            return ctor(new object[] { obj1, obj2 });
        }

        #endregion
        
    }
}
