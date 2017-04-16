using Xunit;
using System;

namespace DynamicExtensions.Tests
{
    public class ObjectMergerTests
    {
        IObjectMerger iOm = new ObjectMerger();

        #region Interfaces&Classes

        public interface IEmpty { }
        public interface IEmptyResult : IEmpty { }
        public class Empty : IEmpty { }

        public interface IWithGetter
        {
            int Prop { get; }
        }
        public interface IWithGetterResult : IWithGetter { }
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
        public interface IWithSetterResult : IWithSetter { }
        public class WithSetter : IWithSetter
        {
            public int Prop { get; set; }
        }

        public interface IWithMethod
        {
            int GetValue(int value);
        }
        public interface IWithMethodResult : IWithMethod { }
        public class WithMethod : IWithMethod
        {
            readonly Func<int, int> getValueBody;
            public WithMethod(Func<int, int> getValueBody)
            {
                this.getValueBody = getValueBody;
            }
            public int GetValue(int value) => getValueBody(value);
        }

        public interface IFirst
        {
            int Prop1 { get; set; }
        }
        public interface ISecond : IWithMethod
        {
            int Prop2 { get; set; }
        }
        public interface IAggregated : IFirst, ISecond { }

        public class First : IFirst
        {
            public int Prop1 { get; set; }
        }

        public class Second : ISecond
        {
            public int Prop2 { get; set; }
            public int GetValue(int value) => value;
        }

        #endregion

        #region Merge ArgumentCheck Tests
        [Fact]
        public void Merge_CallWithClassTypeArgument_BadTIn_ThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => ObjectMerger.GetCachedOrCreateCtor<IEmptyResult>(new[] { typeof(Empty) }));
            Assert.Contains("obj1", e.Message);
        }

        [Fact]
        public void Merge_CallWithClassTypeArgument_BadTOut_ThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => ObjectMerger.GetCachedOrCreateCtor<Empty>(new[] { typeof(IEmpty) }));
            Assert.Contains("TOut", e.Message);
        }

        [Fact]
        public void Merge_CallWithClassTypeArgument_TOutDoesntInheritTIn_ThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => ObjectMerger.GetCachedOrCreateCtor<IEmpty>(new[] { typeof(IEmptyResult) }));
            Assert.Matches(@"T1.*TOut", e.Message);
        }

        [Fact]
        public void Merge_CallWithClassTypeArgument_TOutInheritsNotOnlyTInThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => ObjectMerger.GetCachedOrCreateCtor<IAggregated>(new[] { typeof(IFirst) }));
            Assert.Matches(@"T1.*TOut", e.Message);
        }
        #endregion

        #region Merge Tests
        [Fact]
        public void Merge_CallWith2Objects_ResultShouldContainAllPropertiesAndMethods()
        {
            var val1 = 12;
            var val2 = 23;
            var val3 = 34;

            var obj1 = new First { Prop1 = val1 };
            var obj2 = new Second { Prop2 = val2 };

            var res = iOm.Merge<IFirst, ISecond, IAggregated>(obj1, obj2);

            Assert.Equal(val1, res.Prop1);
            Assert.Equal(val2, res.Prop2);
            Assert.Equal(val3, res.GetValue(val3));
        }
        #endregion

        #region MergeInternal Tests
        [Fact]
        public void MergeInternal_CallWithEmptyInterface_ShouldReturnNewObject()
        {
            var res = ObjectMerger.GetCachedOrCreateCtor<IEmptyResult>(new[] { typeof(IEmpty) })(new[] { new Empty() });
            Assert.NotNull(res);
        }

        [Fact]
        public void MergeInternal_CallWithInterfaceWithMethod_ShouldReturnNewObjectWithMethod()
        {
            var val = 234;
            Func<int, int> func = v => v * 2;
            var expected = func(val);

            var srcObj = new WithMethod(func);
            var resObj = ObjectMerger.GetCachedOrCreateCtor<IWithMethodResult>( new[] { typeof(IWithMethod) })(new object[] { srcObj });
            var actual = resObj.GetValue(val);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MergeInternal_CallWithInterfaceWithGetProperty_ShouldReturnNewObjectWithGetter()
        {
            var val = 123;
            var obj = new WithGetter(val);
            var res = ObjectMerger.GetCachedOrCreateCtor<IWithGetterResult>( new[] { typeof(IWithGetter) })(new object[] { obj });

            Assert.Equal(val, res.Prop);
        }

        [Fact]
        public void MergeInternal_CallWithInterfaceWithSetProperty_ShouldReturnNewObjectWithSetter()
        {
            var val = 123;
            var obj = new WithSetter();

            var res = ObjectMerger.GetCachedOrCreateCtor<IWithSetterResult>( new[] { typeof(IWithSetter) })(new object[] { obj });
            res.Prop = val;

            Assert.Equal(val, obj.Prop);
        }

        [Fact]
        public void MergeInternal_CallTwice_ResultsShouldHaveSameType()
        {
            var obj = new Empty();

            var res1 = ObjectMerger.GetCachedOrCreateCtor<IEmptyResult>(new[] { typeof(IEmpty) })(new[] { obj });
            var res2 = ObjectMerger.GetCachedOrCreateCtor<IEmptyResult>(new[] { typeof(IEmpty) })(new[] { obj });

            var t1 = res1.GetType();
            var t2 = res2.GetType();
            Assert.True(res1.GetType() == res2.GetType(), $"Types are not the same:\n{t1}\n{t2}");
        }
        #endregion

        #region TypeCreationTests
        // types with same name cannot be created so we need separate type name per test
        private string GetNewTypeName() => "NewType_" + Guid.NewGuid().ToString("N");

        [Fact]
        public void BuildType_ConstructedTypeHasProperNameAndIsClass()
        {
            var name = GetNewTypeName();
            var type = ObjectMerger.CreateTypeBuilder(name).CreateTypeInfo();

            Assert.Equal(name, type.Name);
            Assert.True(type.IsClass, "Constructed type is not a class");
        }

        [Fact]
        public void BuildType_TypesShouldBeInOneAssembly_FailsToCreate2TypesWithSameName()
        {
            var name = GetNewTypeName();

            ObjectMerger.CreateTypeBuilder(name);

            Assert.Throws<ArgumentException>(delegate { ObjectMerger.CreateTypeBuilder(name);  });
        }

        [Fact]
        public void BuildType_TypesShouldBeInOneAssembly_ConstructedTypesAreInOneAssembly()
        {
            var type1 = ObjectMerger.CreateTypeBuilder(GetNewTypeName()).CreateTypeInfo();
            var type2 = ObjectMerger.CreateTypeBuilder(GetNewTypeName()).CreateTypeInfo();

            Assert.Equal(type1.Assembly.FullName, type2.Assembly.FullName);
        }
        #endregion
    }
}