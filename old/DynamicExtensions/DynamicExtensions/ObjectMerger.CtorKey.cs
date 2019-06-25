using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace DynamicExtensions
{
    public partial class ObjectMerger
    {
        static readonly object sync = new object();

        #region Constructors creation and caching
        internal class CtorKey
        {
            readonly Type tOut;
            readonly Type[] types;

            public CtorKey(Type tOut, Type[] types)
            {
                this.tOut = tOut;
                this.types = types;
            }

            // TODO: rewrite Equals and GetHashCode + UnitTests, now it doesn't work properly
            public override bool Equals(object obj)
            {
                if (typeof(CtorKey) != obj.GetType())
                {
                    return false;
                }
                var target = (CtorKey)obj;
                if (target.tOut != tOut || target.types.Length != types.Length)
                {
                    return false;
                }
                for (int i = 0; i < types.Length; i++)
                {
                    if (target.types[i] != types[i])
                    {
                        return false;
                    }
                }
                return true;
            }
            public override int GetHashCode()
            {
                var res = (uint)tOut.GetHashCode();
                for (int i = 0; i < types.Length; i++)
                {
                    var hc = (uint)types[i].GetHashCode();
                    res ^= (hc << i) | (hc >> (32 - i));
                }
                return (int)res;
            }
        }

        internal static Func<object[], TOut> GetCachedOrCreateCtor<TOut>(IDictionary<CtorKey, object> cache, Type[] types, Func<Type[], Func<object[], TOut>> creatorDelegate) where TOut : class
        {
            var tOut = typeof(TOut);

            var ctorKey = new CtorKey(tOut, types);
            if (!cache.TryGetValue(ctorKey, out object ctorResult))
            {
                lock (sync)
                {
                    if (!cache.TryGetValue(ctorKey, out ctorResult))
                    {
                        ctorResult = creatorDelegate(types);
                        cache.Add(ctorKey, ctorResult);
                    }
                }
            }

            return (Func<object[], TOut>)ctorResult;
        }

        private static Func<object[], TOut> CreateConstructor<TOut>(Type source, params Type[] ctrArgs)
        {
            var constructorInfo = source.GetConstructor(ctrArgs);
            if (constructorInfo == null)
            {
                return null;
            }
            var argsArray = Expression.Parameter(typeof(object[]));
            var paramsExpression = new Expression[ctrArgs.Length];
            for (var i = 0; i < ctrArgs.Length; i++)
            {
                var argType = ctrArgs[i];
                paramsExpression[i] =
                    Expression.Convert(Expression.ArrayIndex(argsArray, Expression.Constant(i)), argType);
            }
            Expression returnExpression = Expression.New(constructorInfo, paramsExpression);

            returnExpression = Expression.Convert(returnExpression, typeof(TOut));
            return (Func<object[], TOut>)Expression.Lambda(returnExpression, argsArray).Compile();
        }

        #endregion
    }
}
