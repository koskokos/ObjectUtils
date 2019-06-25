using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace MiscellaneousUtils
{
    public partial class ObjectMerger : IObjectMerger
    {
        static ObjectMerger()
        {
            moduleBuilder = General.GetModuleBuilder();
        }

        static readonly ModuleBuilder moduleBuilder;


        #region JoinById

        static readonly IDictionary<CtorKey, object> joinByIdCtorsCache = new ConcurrentDictionary<CtorKey, object>();

        private static Type CreateJoinByIdType(Type[] types, Type tId, Type tOut)
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
            var methodsSet = new HashSet<string>();

            var methodsToFields = Enumerable.Zip(fields, types, (field, type) => new { field, type })
                .SelectMany(item =>
                {
                    var ti = item.type.GetTypeInfo();
                    return Enumerable.Repeat(ti.DeclaredMethods, 1)
                        .Concat(ti.ImplementedInterfaces.Select(i => i.GetTypeInfo().DeclaredMethods))
                        .Select((IEnumerable<MethodInfo> methodInfo) => (methods: methodInfo.Where(m => methodsSet.Add(m.Name)), item.field));
                });

            var newMethods = methodsToFields
                .SelectMany(item => MapMethods(typeBuilder, item.methods, item.field)) // in MapMethods methods are actually created by typeBuilder
                .ToArray();

            // add properties to map to corresponding created methods
            var properties = types.Select(t => t.GetProperties());
            foreach (var prop in properties)
            {
                MapProperties(typeBuilder, newMethods, prop);
            }

            return typeBuilder.CreateTypeInfo().AsType();
        }


        internal static Func<object[], TOut> MakeJoinByIdCtor<TOut>(Type[] types)
        {
            var tOut = typeof(TOut);
            var tId = types[0];
            types = types.Skip(1).ToArray();

            ValidateJoinByIdCtorCreation(tOut, tId, types);
            var tResult = CreateJoinByIdType(types, tId, tOut);

            return CreateConstructor<TOut>(tResult, types);
        }

        private static void ValidateJoinByIdCtorCreation(Type tOut, Type tId, Type[] types)
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

            if (!tId.GetTypeInfo().IsInterface)
            {
                throw new ArgumentException("Type TId of return value must be an interface.");
            }

            var outInterfaces = tOut.GetTypeInfo().GetInterfaces();
            var minimalInterfaces = outInterfaces.Except
                        (outInterfaces.SelectMany(t => t.GetInterfaces()).Distinct()).ToList();


            if (minimalInterfaces.Count != typesCount || types.Any(t => !minimalInterfaces.Contains(t)))
            {
                throw new ArgumentException($"{string.Join(" and ", types.Select((t, i) => "T" + (i + 1).ToString()))} must be only direct ancestors of TOut");
            }
        }

        TOut IObjectMerger.JoinById<TId, T1, T2, TOut>(T1 obj1, T2 obj2)
        {
            var t1 = typeof(T1);
            var t2 = typeof(T2);
            var tId = typeof(TId);

            foreach (var prop in tId.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!Equals(prop.GetValue(obj1), prop.GetValue(obj2)))
                {
                    throw new InvalidOperationException("obj1 ids part are not equal obj2");
                }
            }

            var ctor = GetCachedOrCreateCtor(joinByIdCtorsCache, new[] { tId, t1, t2 }, MakeJoinByIdCtor<TOut>);
            return ctor(new object[] { obj1, obj2 });
        }

        #endregion
    }
}
