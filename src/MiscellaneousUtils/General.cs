using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace MiscellaneousUtils
{
    public static class General
    {
        internal static string NewGuid() => Guid.NewGuid().ToString("N");

        static readonly ModuleBuilder mb = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DynamicExtensions_" + NewGuid()),
                AssemblyBuilderAccess.Run).DefineDynamicModule("MainModule");

        internal static ModuleBuilder GetModuleBuilder() => mb;

        internal static TypeBuilder CreateTypeBuilder(this ModuleBuilder moduleBuilder, string typename) 
            => moduleBuilder.DefineType(typename,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout |
                TypeAttributes.Sealed,
                null);

        static readonly Dictionary<(Type, Type), Type> _createdTypes = new Dictionary<(Type, Type), Type>();

        public static Type InheritBoth(Type t1, Type t2)
        {
            lock (_createdTypes)
            {
                if (_createdTypes.TryGetValue((t1, t2), out Type storedType))
                    return storedType;

                if (!t1.IsInterface || !t2.IsInterface)
                {
                    throw new ArgumentException($"Both types {t1} and {t2} must be interface types");
                }

                var tRes = mb.DefineType($"Mixin_{t1.Name}_{t2.Name}", TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);

                tRes.AddInterfaceImplementation(t1);
                tRes.AddInterfaceImplementation(t2);
                var newType = tRes.CreateType();
                _createdTypes.Add((t1, t2), newType);
                return newType;
            }
        }
    }
}
