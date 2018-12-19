using System;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;

namespace DynamicExtensions.Tests
{
    public class AutoImplementerTests
    {
        readonly AutoImplementer ai = new AutoImplementer();

        #region testdata
        readonly Func<MethodInfo, (object, MethodInfo)> fakeResolver = mi => (null, null);

        public interface I1
        {
            void M1(object i);
        }

        #region StaticMethodsTestData
        public class StaticMethodHit : Exception
        {
            public object Param { get; set; }
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void StaticMethod(object i) => throw new StaticMethodHit { Param = i };
#pragma warning restore xUnit1013 // Public method should be marked as test

        static readonly Func<MethodInfo, (object, MethodInfo)> toStaticMethodResolver = mi => (null, typeof(AutoImplementerTests).GetMethod(nameof(StaticMethod)));
        #endregion

        #region InstanceMethodsTestData
        public class InstanceMethodHit : Exception
        {
            public object Target { get; set; }
            public object Param { get; set; }
        }

        public class InstanceImpl
        {
            public void Method(object param) => throw new InstanceMethodHit { Target = this, Param = param };
        }

        static Func<MethodInfo, (object, MethodInfo)> GetToInstanceMethodResolver(InstanceImpl target) =>
            mi => (target, typeof(InstanceImpl).GetMethod(nameof(InstanceImpl.Method)));

        #endregion

        #endregion

        [Fact]
        public void NotInterfaceParam_Fails()
        {
            Assert.Throws<ArgumentException>(() => ai.ImplementWith<object>(fakeResolver));
        }

        [Fact]
        public void NullResolver_Fails()
        {
            Assert.Throws<ArgumentNullException>(() => ai.ImplementWith<I1>(null));
        }

        [Fact]
        public void ImplementsOneMethod()
        {
            const int param = 236;

            var impl = ai.ImplementWith<I1>(toStaticMethodResolver);

            var hit = Assert.Throws<StaticMethodHit>(() => impl.M1(param));
            Assert.Equal(param, hit.Param);
        }
        
        [Fact]
        public void ImplementsInstanceMethod()
        {
            const int param = 873;
            var target = new InstanceImpl();

            var impl = ai.ImplementWith<I1>(GetToInstanceMethodResolver(target));

            var hit = Assert.Throws<InstanceMethodHit>(() => impl.M1(param));
            Assert.Equal(param, hit.Param);
            Assert.Equal(target, hit.Target);
        }

        #region ImplementsMethodWith2Parameters

        public class MethodHit2Params : Exception
        {
            public object P1 { get; set; }
            public object P2 { get; set; }
        }

        public interface I2
        {
            void Method(object a, object b);
        }

#pragma warning disable xUnit1013 // Public method should be marked as test
        public static void Method2Params(object a, object b) => throw new MethodHit2Params { P1 = a, P2 = b };
#pragma warning restore xUnit1013 // Public method should be marked as test

        [Fact]
        public void ImplementsMethodWith2Parameters()
        {
            const int p1 = 17, p2 = 29;
            var impl = ai.ImplementWith<I2>(mi => (null, typeof(AutoImplementerTests).GetMethod(nameof(Method2Params))));

            var hit = Assert.Throws<MethodHit2Params>(() => impl.Method(p1, p2));
            Assert.Equal(p1, hit.P1);
            Assert.Equal(p2, hit.P2);
        }
        #endregion

        #region ImplementsTwoDifferentlyResolvedMethods
        public interface I3
        {
            void Method1(object p1, object p2);
            void Method2(object p1);
        }

        [Fact]
        public void ImplementsTwoDifferentlyResolvedMethods()
        {
            var inst = new InstanceImpl();

            var impl = ai.ImplementWith<I3>(mi =>
            {
                if (mi == typeof(I3).GetMethod(nameof(I3.Method1)))
                    return (null, typeof(AutoImplementerTests).GetMethod(nameof(Method2Params)));
                else if (mi == typeof(I3).GetMethod(nameof(I3.Method2)))
                    return (inst, typeof(InstanceImpl).GetMethod(nameof(InstanceImpl.Method)));
                else
                    throw new Exception();
            });

            Assert.Throws<MethodHit2Params>(() => impl.Method1(1, 2));
            Assert.Throws<InstanceMethodHit>(() => impl.Method2(1));
        }
        #endregion

    }
}
