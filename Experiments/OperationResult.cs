using System;
using System.Collections.Generic;
using System.Linq;

namespace Experiments
{
    public abstract class OperationResult
    {
        public abstract void Match(Action onSuccess, Action<IEnumerable<string>> onFail);
        public abstract TOut Match<TOut>(Func<TOut> onSuccess, Func<IEnumerable<string>, TOut> onFail);

        internal class OkResult : OperationResult
        {
            public static OperationResult Instance = new OkResult();
            public override void Match(Action onSuccess, Action<IEnumerable<string>> onFail)
                => onSuccess();

            public override TOut Match<TOut>(Func<TOut> onSuccess, Func<IEnumerable<string>, TOut> onFail)
                => onSuccess();
        }

        public static OperationResult<T> Ok<T>(T value) => new OperationResult<T>.OkResult(value);

        public static OperationResult Ok() => OkResult.Instance;

        public static FailedOperationResult Fail(IEnumerable<string> errors) => new FailedOperationResult(errors);

        public static FailedOperationResult Fail(Exception ex) => new FailedOperationResult(new[] { ex.ToString() });

        public static FailedOperationResult Fail(string error) => new FailedOperationResult(new[] { error });

    }

    public class FailedOperationResult : OperationResult
    {
        static IEnumerable<string> emptyErrors = Enumerable.Empty<string>();

        public IEnumerable<string> Errors { get; }
        internal FailedOperationResult(IEnumerable<string> errors)
        {
            Errors = errors ?? emptyErrors;
        }
        public override void Match(Action onSuccess, Action<IEnumerable<string>> onFail)
            => onFail(Errors);

        public override TOut Match<TOut>(Func<TOut> onSuccess, Func<IEnumerable<string>, TOut> onFail)
            => onFail(Errors);
    }

    public abstract class OperationResult<T>
    {
        public abstract void Match(Action<T> onSuccess, Action<IEnumerable<string>> onFail);
        public abstract TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFail);

        internal class OkResult : OperationResult<T>
        {
            private readonly T _value;
            internal OkResult(T value)
            {
                _value = value;
            }
            public override void Match(Action<T> onSuccess, Action<IEnumerable<string>> onFail)
                => onSuccess(_value);

            public override TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFail)
                => onSuccess(_value);
        }

        internal class FailResult : OperationResult<T>
        {
            private readonly FailedOperationResult _nonGenericResult;
            internal FailResult(FailedOperationResult nonGenericResult)
            {
                _nonGenericResult = nonGenericResult;
            }
            public override void Match(Action<T> onSuccess, Action<IEnumerable<string>> onFail)
                => onFail(_nonGenericResult.Errors);

            public override TOut Match<TOut>(Func<T, TOut> onSuccess, Func<IEnumerable<string>, TOut> onFail)
                => onFail(_nonGenericResult.Errors);
        }

        public static implicit operator OperationResult<T>(FailedOperationResult failResult)
        {
            return new FailResult(failResult);
        }
    }
}
