using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicExtensions
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

        public static Type InheritBoth(Type t1, Type t2)
        {
            if (!t1.IsInterface || !t2.IsInterface)
            {
                throw new ArgumentException($"Both types {t1} and {t2} must be interface types");
            }

            var tRes = mb.DefineType($"IBoth_{t1.Name}_{t2.Name}_{NewGuid()}", TypeAttributes.Public | TypeAttributes.Interface);

            tRes.AddInterfaceImplementation(t1);
            tRes.AddInterfaceImplementation(t2);

            return tRes.CreateType();
        }
    }
}
