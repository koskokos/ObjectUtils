using Chameleon.Common.Merge;
using NUnit.Framework;
using System;

namespace ChameleonLogic.UnitTests.Merge
{

    [TestFixture]
    public class ObjectMergerTests
    {
        IObjectMerger iOm = new ObjectMerger();

        #region Interfaces&Classes
        public interface IBase { }
        public interface I1 : IBase
        {
            int FirstProp { get; }
            int Method(string param);
        }
        public interface I2
        {
            int SecondProp { get; }
        }
        public interface I3 { }
        public interface IAggregatedBad : I1, I2, I3 { }
        public interface IAggregated : I1, I2 { }
        public class First : I1
        {
            readonly Func<string, int> methodBody;
            public First(Func<string, int> methodBody = null)
            {
                this.methodBody = methodBody;
            }
            public int FirstProp { get; set; }

            public int Method(string param) => methodBody(param);
        }
        public class Second : I2
        {
            public int SecondProp { get; set; }
        }
        public class Empty : IBase { }
        public class Aggregated1 : First, IAggregated
        {
            public int SecondProp { get; }
        }
        public class Aggregated2 : Second, IAggregated
        {
            public int FirstProp { get; }

            public int Method(string param) => 0;
        }
        public interface IWithGetter
        {
            int Prop { get; }
        }
        public class WithGetter : IWithGetter
        {
            public WithGetter(int val)
            {
                Prop = val;
            }
            public int Prop { get; }
        }
        public interface IWithSetter
        {
            int Prop { set; }
        }
        public class WithSetter : IWithSetter
        {
            public int Prop { get; set; }
        }
        public interface IWithMethod
        {
            int GetValue(int value);
        }
        public class WithMethod : IWithMethod
        {
            readonly Func<int, int> getValueBody;
            public WithMethod(Func<int, int> getValueBody)
            {
                this.getValueBody = getValueBody;
            }
            public int GetValue(int value) => getValueBody(value);
        }
        #endregion

        #region Merge ArgumentCheck Tests
        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "obj1")]
        public void Merge_CallWithClassTypeArgument_BadT1_ThrowArgumentException()
        {
            iOm.Merge<First, I2, Aggregated1>(new First(), new Second());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "obj2")]
        public void Merge_CallWithClassTypeArgument_BadT2_ThrowArgumentException()
        {
            iOm.Merge<I1, Second, Aggregated2>(new First(), new Second());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Contains, ExpectedMessage = "TOut")]
        public void Merge_CallWithClassTypeArgument_BadTOut_ThrowArgumentException()
        {
            iOm.Merge<I1, I2, Aggregated2>(new First(), new Second());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Regex, ExpectedMessage = @"T1.*T2.*TOut")]
        public void Merge_CallWithClassTypeArgument_TOutDoesntInheritT1andT2_ThrowArgumentException()
        {
            iOm.Merge<IBase, I2, IAggregated>(new First(), new Second());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException), MatchType = MessageMatch.Regex, ExpectedMessage = @"T1.*T2.*TOut")]
        public void Merge_CallWithClassTypeArgument_TOutInheritsMoreThanT1andT2_ThrowArgumentException()
        {
            iOm.Merge<I1, I2, IAggregatedBad>(new First(), new Second());
        }
        #endregion

        #region Merge Tests
        [Test]
        public void Merge_CallWith2Objects_ResultShouldContainAllPropertiesAndMethods()
        {
            var val1 = 12;
            var val2 = 23;
            var strVal = "15";
            Func<string, int> func = int.Parse;

            var obj1 = new First(func) { FirstProp = val1 };
            var obj2 = new Second { SecondProp = val2 };

            var res = iOm.Merge<I1, I2, IAggregated>(obj1, obj2);

            Assert.AreEqual(val1, res.FirstProp);
            Assert.AreEqual(val2, res.SecondProp);
            Assert.AreEqual(func(strVal), res.Method(strVal));
        }
        #endregion

        #region MergeInternal Tests
        [Test]
        public void MergeInternal_CallWithEmptyInterface_ShouldReturnNewObject()
        {
            var res = ObjectMerger.MergeInternal<IBase>(new[] { new Empty() }, new[] { typeof(IBase) });
            Assert.IsNotNull(res);
        }

        [Test]
        public void MergeInternal_CallWithInterfaceWithMethod_ShouldReturnNewObjectWithMethod()
        {
            var val = 234;
            Func<int, int> func = v => v * 2;
            var expected = func(val);

            var srcObj = new WithMethod(func);
            var resObj = ObjectMerger.MergeInternal<IWithMethod>(new object[] { srcObj }, new[] { typeof(IWithMethod) });
            var actual = resObj.GetValue(val);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void MergeInternal_CallWithInterfaceWithGetProperty_ShouldReturnNewObjectWithGetter()
        {
            var val = 123;
            var obj = new WithGetter(val);
            var res = ObjectMerger.MergeInternal<IWithGetter>(new object[] { obj }, new[] { typeof(IWithGetter) });

            Assert.AreEqual(val, res.Prop);
        }

        [Test]
        public void MergeInternal_CallWithInterfaceWithSetProperty_ShouldReturnNewObjectWithSetter()
        {
            var val = 123;
            var obj = new WithSetter();

            var res = ObjectMerger.MergeInternal<IWithSetter>(new object[] { obj }, new[] { typeof(IWithSetter) });
            res.Prop = val;

            Assert.AreEqual(val, obj.Prop);
        }

        [Test]
        public void MergeInternal_CallTwice_ResultsShouldHaveSameType()
        {
            var obj = new Empty();

            var res1 = ObjectMerger.MergeInternal<IBase>(new[] { obj }, new[] { typeof(IBase) });
            var res2 = ObjectMerger.MergeInternal<IBase>(new[] { obj }, new[] { typeof(IBase) });

            var t1 = res1.GetType();
            var t2 = res2.GetType();
            Assert.IsTrue(res1.GetType() == res2.GetType(), $"Types are not the same:\n{t1}\n{t2}");
        }
        #endregion

        #region TypeCreationTests
        // types with same name cannot be created so we need separate type name per test
        private string GetNewTypeName() => "NewType_" + Guid.NewGuid().ToString("N");

        [Test]
        public void BuildType_ConstructedTypeHasProperNameAndIsClass()
        {
            var name = GetNewTypeName();
            var type = ObjectMerger.CreateTypeBuilder(name).CreateType();

            Assert.AreEqual(name, type.Name, "Constructed type has improper name");
            Assert.IsTrue(type.IsClass, "Constructed type is not a class");
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void BuildType_TypesShouldBeInOneAssembly_FailsToCreate2TypesWithSameName()
        {
            var name = GetNewTypeName();

            ObjectMerger.CreateTypeBuilder(name);
            ObjectMerger.CreateTypeBuilder(name);
        }

        [Test]
        public void BuildType_TypesShouldBeInOneAssembly_ConstructedTypesAreInOneAssembly()
        {
            var type1 = ObjectMerger.CreateTypeBuilder(GetNewTypeName()).CreateType();
            var type2 = ObjectMerger.CreateTypeBuilder(GetNewTypeName()).CreateType();

            Assert.AreEqual(type1.Assembly.FullName, type2.Assembly.FullName);
        }
        #endregion
    }
}