﻿using Xunit;
using System;
using System.Collections.Generic;
using static DynamicExtensions.ObjectMerger;
using System.Collections.Concurrent;

namespace DynamicExtensions.Tests
{
    public class ObjectMergerTests
    {
        readonly IObjectMerger iOm = new ObjectMerger();

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
            var e = Assert.Throws<ArgumentException>(() => MakeMergeCtor<IEmptyResult>(new[] { typeof(Empty) }));
            Assert.Contains("obj1", e.Message);
        }

        [Fact]
        public void Merge_CallWithClassTypeArgument_BadTOut_ThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => MakeMergeCtor<Empty>(new[] { typeof(IEmpty) }));
            Assert.Contains("TOut", e.Message);
        }

        [Fact]
        public void Merge_CallWithClassTypeArgument_TOutDoesntInheritTIn_ThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => MakeMergeCtor<IEmpty>(new[] { typeof(IEmptyResult) }));
            Assert.Matches(@"T1.*TOut", e.Message);
        }

        [Fact]
        public void Merge_CallWithClassTypeArgument_TOutInheritsNotOnlyTInThrowArgumentException()
        {
            var e = Assert.Throws<ArgumentException>(() => MakeMergeCtor<IAggregated>(new[] { typeof(IFirst) }));
            Assert.Matches(@"T1.*TOut", e.Message);
        }
        #endregion
        

        #region joinByIdTests

        public interface IWithId { int Id { get; } }
        public interface I1WithId : IWithId { int Prop1 { get; } }
        public interface I2WithId : IWithId { int Prop2 { get; } }
        public interface IAggregateWithId : I1WithId, I2WithId { }

        class WithId1 : I1WithId
        {
            public int Prop1 { get; set; }
            public int Id { get; set; }
        }

        class WithId2 : I2WithId
        {
            public int Prop2 { get; set; }
            public int Id { get; set; }
        }

        [Fact]
        public void JoinById_CorrectPropsAndId()
        {
            var val1 = 12;
            var val2 = 23;
            var valId = 123;

            var obj1 = new WithId1 { Id = valId, Prop1 = val1 };
            var obj2 = new WithId2 { Id = valId, Prop2 = val2 };

            var res = iOm.JoinById<IWithId, I1WithId, I2WithId, IAggregateWithId>(obj1, obj2);

            Assert.Equal(val1, res.Prop1);
            Assert.Equal(val2, res.Prop2);
            Assert.Equal(valId, res.Id);
        }

        [Fact]
        public void JoinByDifferentIds_Fail()
        {
            var val1 = 12;
            var val2 = 23;

            var obj1 = new WithId1 { Id = val1, Prop1 = val1 };
            var obj2 = new WithId2 { Id = val2, Prop2 = val2 };

            Assert.Throws<InvalidOperationException>(() => iOm.JoinById<IWithId, I1WithId, I2WithId, IAggregateWithId>(obj1, obj2));
            
        }
        #endregion

        #region Merge Tests
        [Fact]
        public void MergeInternal_CallWithEmptyInterface_ShouldReturnNewObject()
        {
            var res = MakeMergeCtor<IEmptyResult>(new[] { typeof(IEmpty) })(new[] { new Empty() });
            Assert.NotNull(res);
        }

        [Fact]
        public void MergeInternal_CallWithInterfaceWithMethod_ShouldReturnNewObjectWithMethod()
        {
            var val = 234;
            int func(int v) => v * 2;
            var expected = func(val);

            var srcObj = new WithMethod(func);
            var resObj = MakeMergeCtor<IWithMethodResult>(new[] { typeof(IWithMethod) })(new object[] { srcObj });
            var actual = resObj.GetValue(val);

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MergeInternal_CallWithInterfaceWithGetProperty_ShouldReturnNewObjectWithGetter()
        {
            var val = 123;
            var obj = new WithGetter(val);
            var res = MakeMergeCtor<IWithGetterResult>(new[] { typeof(IWithGetter) })(new object[] { obj });

            Assert.Equal(val, res.Prop);
        }

        [Fact]
        public void MergeInternal_CallWithInterfaceWithSetProperty_ShouldReturnNewObjectWithSetter()
        {
            var val = 123;
            var obj = new WithSetter();

            var res = MakeMergeCtor<IWithSetterResult>(new[] { typeof(IWithSetter) })(new object[] { obj });
            res.Prop = val;

            Assert.Equal(val, obj.Prop);
        }

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

        #region TypeCreationTests
        // types with same name cannot be created so we need separate type name per test
        private string GetNewTypeName() => "NewType_" + Guid.NewGuid().ToString("N");

        [Fact]
        public void BuildType_ConstructedTypeHasProperNameAndIsClass()
        {
            var name = GetNewTypeName();
            var type = General.GetModuleBuilder().CreateTypeBuilder(name).CreateTypeInfo();

            Assert.Equal(name, type.Name);
            Assert.True(type.IsClass, "Constructed type is not a class");
        }

        [Fact]
        public void BuildType_TypesShouldBeInOneAssembly_FailsToCreate2TypesWithSameName()
        {
            var name = GetNewTypeName();

            General.GetModuleBuilder().CreateTypeBuilder(name);

            Assert.Throws<ArgumentException>(delegate { General.GetModuleBuilder().CreateTypeBuilder(name); });
        }

        [Fact]
        public void BuildType_TypesShouldBeInOneAssembly_ConstructedTypesAreInOneAssembly()
        {
            var type1 = General.GetModuleBuilder().CreateTypeBuilder(GetNewTypeName()).CreateTypeInfo();
            var type2 = General.GetModuleBuilder().CreateTypeBuilder(GetNewTypeName()).CreateTypeInfo();

            Assert.Equal(type1.Assembly.FullName, type2.Assembly.FullName);
        }
        #endregion


        #region GetCachedOrCreateType

        static Func<object[], TOut> MakeFakeCtor<TOut>(Type[] types) => objects =>
        {
            var kek = types.Length;
            return default;
        };

        [Fact]
        public void GetCachedOrCreateType_CallTwice_ResultsShouldHaveSameConstructor()
        {
            var mergeCtorCache = new ConcurrentDictionary<CtorKey, object>();

            var res1 = GetCachedOrCreateCtor(mergeCtorCache, new[] { typeof(IEmpty) }, MakeFakeCtor<IAggregated>);
            var res2 = GetCachedOrCreateCtor(mergeCtorCache, new[] { typeof(IEmpty) }, MakeFakeCtor<IAggregated>);

            Assert.True(res1 == res2, $"Constructors are not the same:\n{res1}\n{res2}");
        }

        [Fact]
        public void GetCachedOrCreateType_MustReturnTheSameTypeForSameTypesList()
        {
            var mergeCtorCache = new ConcurrentDictionary<CtorKey, object>();

            var res1 = GetCachedOrCreateCtor(mergeCtorCache, new[] { typeof(IFirst), typeof(ISecond) }, MakeFakeCtor<IAggregated>);
            var res2 = GetCachedOrCreateCtor(mergeCtorCache, new[] { typeof(IFirst), typeof(ISecond) }, MakeFakeCtor<IAggregated>);

            Assert.True(res1 == res2, $"Types are not the same:\n{res1}\n{res2}");
        }

        [Fact]
        public void GetCachedOrCreateType_MustReturnDifferentTypesForDifferentOrder()
        {
            var mergeCtorCache = new ConcurrentDictionary<CtorKey, object>();

            var res1 = GetCachedOrCreateCtor(mergeCtorCache, new[] { typeof(ISecond), typeof(IFirst) }, MakeFakeCtor<IAggregated>);
            var res2 = GetCachedOrCreateCtor(mergeCtorCache, new[] { typeof(IFirst), typeof(ISecond) }, MakeFakeCtor<IAggregated>);

            Assert.True(res1 != res2, $"Types are the same:\n{res1}\n{res2}");
        }
        #endregion

    }
}