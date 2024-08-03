#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional
{
    using Outcome;

    using static ResultVoidIncomeSource;

    public readonly struct Result
    {
        private readonly ResultVoidIncomeSource _income;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private Result(Exception exception)
        {
            _income = Exception;
            _exception = exception;
        }

        [MethodImpl(AggressiveInlining)]
        private Result(Success _)
        {
            _income = Succeed;
            _exception = default!;
        }

        [Pure] public static Result Success { get; } = new (Expected.Success);
        [Pure] public static Result Error { get; } = new (Unexpected.Error);
        [Pure] public static Result Impossible { get; } = new (Unexpected.Impossible);

        [Pure] public bool IsSuccessful => _income == Succeed;
        [Pure] public bool IsFailure => IsSuccessful is false;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator Result (Exception exception)
        {
            return Equals(exception, null) is false
                ? new (exception)
                : new (new ArgumentNullException(nameof(exception)));
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator bool (Result income) => income.IsSuccessful;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator Result (bool income) => income ? Success : Error;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static Result FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Succeed => another,
                _ => Result<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result Combine(Result another)
        {
            return (_income, another._income) switch
            {
                (Exception, Exception) => new AggregateException(_exception, another._exception),
                (Exception, _) => _exception,
                (_, Exception) => another._exception,
                (Succeed, Succeed) => this,
                _ => Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Run(Action action)
        {
            if (_income == Succeed)
            {
                action.Invoke();
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result Run(Func<Result> action)
        {
            return _income switch
            {
                Exception => _exception,
                Succeed => action.Invoke(),
                _ => Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TAnother> Run<TAnother>(Func<Result<TAnother>> action)
        {
            return _income switch
            {
                Exception => _exception,
                Succeed => action.Invoke(),
                _ => Result<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>(Func<TMatch> success, Func<Exception, TMatch> error)
        {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Succeed => success.Invoke(),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match(Action success, Action<Exception> error)
        {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Succeed:
                    success.Invoke();
                    return;
                default:
                    error.Invoke(Unexpected.Impossible);
                    return;
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public override string ToString()
        {
            return _income switch
            {
                Exception => _exception.Message,
                Succeed => nameof(Success),
                _ => Unexpected.Impossible.Message
            };
        }
    }

    internal enum ResultVoidIncomeSource
    {
        Succeed = 1,
        Exception = 2
    }
}
