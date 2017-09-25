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

        public class TestImpl
        {
            private int incrementer = 2;
            public int IncInst(int i) => i + incrementer;
        }


        private Func<MethodInfo, (object, MethodInfo)> toInstanceResolver = mi => (new TestImpl(), typeof(TestImpl).GetMethod(nameof(TestImpl.IncInst)));

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

        [Fact]
        public void ImplementsInstanceMethod()
        {
            var impl = ai.ImplementWith<I1>(toInstanceResolver);

            Assert.Equal(new TestImpl().IncInst(5), impl.Inc(5));
        }
    }
}
