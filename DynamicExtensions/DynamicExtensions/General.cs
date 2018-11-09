using System;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicExtensions
{
    static class General
    {
        public static string NewGuid() => Guid.NewGuid().ToString("N");

        static readonly ModuleBuilder mb = AssemblyBuilder.DefineDynamicAssembly(
                new AssemblyName("DynamicExtensions_" + NewGuid()),
                AssemblyBuilderAccess.Run).DefineDynamicModule("MainModule");

        public static ModuleBuilder GetModuleBuilder() => mb;

        public static TypeBuilder CreateTypeBuilder(this ModuleBuilder moduleBuilder, string typename) 
            => moduleBuilder.DefineType(typename,
                TypeAttributes.Public |
                TypeAttributes.Class |
                TypeAttributes.AutoClass |
                TypeAttributes.AnsiClass |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.AutoLayout |
                TypeAttributes.Sealed,
                null);
    }
}
