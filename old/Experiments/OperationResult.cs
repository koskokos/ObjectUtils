using System;

namespace Experiments
{

    public static class OperationResult
    {
        public static OkOperationResult<T> Ok<T>(T value) => new OkOperationResult<T>(value);

        public static OkOperationResult Ok() => OkOperationResult.Instance;

        public static FailedOperationResult<TFail> Fail<TFail>(TFail fail) => new FailedOperationResult<TFail>(fail);
    }

    public sealed class OkOperationResult
    {
        public static OkOperationResult Instance { get; } = new OkOperationResult();
        private OkOperationResult() { }
    }

    public sealed class OkOperationResult<TResult>
    {
        public TResult Value { get; }
        internal OkOperationResult(TResult value)
        {
            Value = value;
        }
    }

    public sealed class FailedOperationResult<TFail> : OperationResult<TFail>
    {
        public TFail Reason { get; }
        internal FailedOperationResult(TFail reason)
        {
            Reason = reason;
        }
        public override void Match(Action onSuccess, Action<TFail> onFail)
            => onFail(Reason);

        public override TOut Match<TOut>(Func<TOut> onSuccess, Func<TFail, TOut> onFail)
            => onFail(Reason);
    }

    public abstract class OperationResult<TFail> 
    {
        public abstract void Match(Action onSuccess, Action<TFail> onFail);
        public abstract TOut Match<TOut>(Func<TOut> onSuccess, Func<TFail, TOut> onFail);

        internal class OkResult : OperationResult<TFail>
        {
            public static OperationResult<TFail> Instance = new OkResult();
            public override void Match(Action onSuccess, Action<TFail> onFail)
                => onSuccess();

            public override TOut Match<TOut>(Func<TOut> onSuccess, Func<TFail, TOut> onFail)
                => onSuccess();
        }

        public static implicit operator OperationResult<TFail>(OkOperationResult okResult) 
            => OkResult.Instance;
    }

    public abstract class OperationResult<TResult, TFail>
    {
        public abstract void Match(Action<TResult> onSuccess, Action<TFail> onFail);
        public abstract TOut Match<TOut>(Func<TResult, TOut> onSuccess, Func<TFail, TOut> onFail);

        internal class OkResult : OperationResult<TResult, TFail>
        {
            private readonly OkOperationResult<TResult> _okResult;
            internal OkResult(OkOperationResult<TResult> okResult)
            {
                _okResult = okResult;
            }
            public override void Match(Action<TResult> onSuccess, Action<TFail> onFail)
                => onSuccess(_okResult.Value);

            public override TOut Match<TOut>(Func<TResult, TOut> onSuccess, Func<TFail, TOut> onFail)
                => onSuccess(_okResult.Value);
        }

        internal class FailResult : OperationResult<TResult, TFail>
        {
            private readonly FailedOperationResult<TFail> _nonGenericResult;
            internal FailResult(FailedOperationResult<TFail> nonGenericResult)
            {
                _nonGenericResult = nonGenericResult;
            }
            public override void Match(Action<TResult> onSuccess, Action<TFail> onFail)
                => onFail(_nonGenericResult.Reason);

            public override TOut Match<TOut>(Func<TResult, TOut> onSuccess, Func<TFail, TOut> onFail)
                => onFail(_nonGenericResult.Reason);
        }

        public static implicit operator OperationResult<TResult, TFail>(OkOperationResult<TResult> okResult)
            => new OkResult(okResult);

        public static implicit operator OperationResult<TResult, TFail>(FailedOperationResult<TFail> failResult)
            => new FailResult(failResult);
    }
}
