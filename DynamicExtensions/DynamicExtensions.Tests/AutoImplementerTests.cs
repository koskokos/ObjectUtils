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
        
        public static int Inc(int i) => i + 1;

        private static Func<MethodInfo, (object, MethodInfo)> intToIntResolver = mi => (null, typeof(AutoImplementerTests).GetMethod(nameof(Inc)));

        public interface I1
        {
            int Inc(int i);
        }

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
            var impl = ai.ImplementWith<I1>(intToIntResolver);

            Assert.Equal(Inc(5), impl.Inc(5));
        }

        #region ImplementsInstanceMethod
        public class TestImpl
        {
            private int incrementer = 2;
            public int Inc(int i) => i + incrementer;
        }

        [Fact]
        public void ImplementsInstanceMethod()
        {
            var target = new TestImpl();

            var impl = ai.ImplementWith<I1>(mi => (target, typeof(TestImpl).GetMethod(nameof(TestImpl.Inc))));

            Assert.Equal(target.Inc(5), impl.Inc(5));
        }
        #endregion

        #region ImplementsMethodWith2Parameters
        public interface I2
        {
            int Add(int a, int b);
        }

        public static int Add(int a, int b) => a + b;

        [Fact]
        public void ImplementsMethodWith2Parameters()
        {
            var impl = ai.ImplementWith<I2>(mi => (null, typeof(AutoImplementerTests).GetMethod(nameof(Add))));

            Assert.Equal(Add(2, 3), impl.Add(2, 3));
        }
        #endregion

        #region ImplementsTwoDifferentlyResolvedMethods
        public interface I3
        {
            int Add(int a, int b);
            int Inc(int a);
        }

        public class IncInst
        {
            readonly int val = 7;
            public int Inc(int a) => a + val;
        }

        [Fact]
        public void ImplementsTwoDifferentlyResolvedMethods()
        {
            var incInst = new IncInst();
            var impl = ai.ImplementWith<I3>(mi =>
            {
                if (mi == typeof(I3).GetMethod(nameof(I3.Add)))
                    return (null, typeof(AutoImplementerTests).GetMethod(nameof(Add)));
                else if (mi == typeof(I3).GetMethod(nameof(I3.Inc)))
                    return (incInst, typeof(IncInst).GetMethod(nameof(IncInst.Inc)));
                else
                    throw new Exception();
            });

            Assert.Equal(impl.Inc(5), incInst.Inc(5));
            Assert.Equal(impl.Add(4, 5), Add(4, 5));
        }
        #endregion

    }
}
