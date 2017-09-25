using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace DynamicExtensions
{
    using System.Collections.Generic;
    using static OpCodes;
    public class AutoImplementer
    {
        static readonly ModuleBuilder moduleBuilder;

        static AutoImplementer()
        {
            moduleBuilder = General.GetModuleBuilder();
        }

        public T ImplementWith<T>(Func<MethodInfo, (object target, MethodInfo method)> implementationResolver)
        {
            if (implementationResolver == null) throw new ArgumentNullException("implementationResolver cannot be null");

            var t = typeof(T);

            if (!t.IsInterface) throw new ArgumentException("T must be an interface");

            var tb = moduleBuilder.CreateTypeBuilder($"_autoImpl_{t.Name}_{General.NewGuid()}");

            tb.AddInterfaceImplementation(t);

            var constructorParams = new List<object>();
            var fields = new List<FieldInfo>();

            foreach (var m in t.GetMethods())
            {
                var attributes = m.Attributes & (~MethodAttributes.Abstract); // interfaces' methods are abstract, this should be removed

                var (target, targetMethod) = implementationResolver(m);

                var parameters = m.GetParameters();

                var mg = tb.DefineMethod(m.Name, attributes, m.ReturnType, parameters.Select(p => p.ParameterType).ToArray())
                    .GetILGenerator();

                if (target != null) {
                    var targetField = tb.DefineField($"target_{m.Name}", target.GetType(), FieldAttributes.Private);
                    mg.Emit(Ldarg_0);
                    mg.Emit(Ldfld, targetField);

                    constructorParams.Add(target);
                    fields.Add(targetField);
                }
                
                for (int i = 0; i < parameters.Length; i++)
                {
                    mg.Emit(Ldarg, i + 1);
                }


                mg.Emit(Call, targetMethod);
                mg.Emit(Ret);
            }

            var constructorParamTypes = constructorParams.Select(target => target.GetType()).ToArray();

            var cg = tb.DefineConstructor(
                    MethodAttributes.Public,
                    CallingConventions.Standard | CallingConventions.HasThis,
                    constructorParamTypes)
                .GetILGenerator();

            for (int i = 0; i < fields.Count; i++)
            {
                cg.Emit(Ldarg_0);
                cg.Emit(Ldarg, i + 1);
                cg.Emit(Stfld, fields[i]);
            }

            cg.Emit(Ret);

            var tImpl = tb.CreateType();

            var c = tImpl.GetConstructor(constructorParamTypes);

            return (T)c.Invoke(constructorParams.ToArray());
        }
    }
}
