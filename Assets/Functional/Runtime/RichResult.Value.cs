#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional
{
    using Outcome;

    using static RichResultValueIncomeSource;

    public readonly struct RichResult<TValue>
    {
        private readonly RichResultValueIncomeSource _income;
        private readonly TValue _value;
        private readonly Exception _exception;
        private readonly Expected.Failure _failure;

        [MethodImpl(AggressiveInlining)]
        private RichResult(TValue value)
        {
            _income = Value;
            _value = value;
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private RichResult(Expected.Failure failure)
        {
            _income = Failed;
            _value = default!;
            _failure = failure;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private RichResult(Exception exception)
        {
            _income = Exception;
            _value = default!;
            _failure = default!;
            _exception = exception;
        }

        [Pure] public static RichResult<TValue> Failure { get; } = new (Expected.Failed);
        [Pure] public static RichResult<TValue> Error { get; } = new (Unexpected.Error);
        [Pure] public static RichResult<TValue> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult<TValue> (TValue value) => new (value);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult<TValue> (Expected.Failure failure) => new (failure);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult<TValue> (Exception exception) => new (exception);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult<TValue> FromValue(TValue value) => value;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult<TValue> FromFailure(Expected.Failure failure) => failure;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult<TValue> FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TValue, TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure,
                Value => (_value, another),
                _ => RichResult<TValue, TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TValue, TAnother> Combine<TAnother>(RichResult<TAnother> another)
        {
            return (_income, another._income) switch
            {
                (Exception, Exception) => new AggregateException(_exception, another._exception),
                (Exception, _) => _exception,
                (_, Exception) => another._exception,
                (Failed, Failed) => _failure.Combine(another._failure),
                (Failed, _) => _failure,
                (_, Failed) => another._failure,
                (Value, Value) => (_value, another._value),
                _ => RichResult<TValue, TAnother>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action<TValue> success,
            Action<Expected.Failure> failure,
            Action<Exception> error
        ) {
            switch (_income)
            {
                case Exception:
                    success.Invoke(_value);
                    return;
                case Failed:
                    failure.Invoke(_failure);
                    return;
                case Value:
                    error.Invoke(_exception);
                    return;
                default:
                    error.Invoke(Unexpected.Impossible);
                    return;
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>
        (
            Func<TValue, TMatch> success,
            Func<Expected.Failure, TMatch> failure,
            Func<Exception, TMatch> error
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Value => success.Invoke(_value),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TValue Match
        (
            Func<TValue, TValue> success,
            Func<Expected.Failure, TValue> failure,
            Func<Exception, TValue> error
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Value => success.Invoke(_value),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }
    }

    public readonly struct RichResult<TFirst, TSecond>
    {
        private readonly RichResultValueIncomeSource _income;
        private readonly (TFirst First, TSecond Second) _value;
        private readonly Exception _exception;
        private readonly Expected.Failure _failure;

        [MethodImpl(AggressiveInlining)]
        private RichResult(TFirst first, TSecond second)
        {
            _income = Value;
            _value = (first, second);
            _failure = default!;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private RichResult(Expected.Failure failure)
        {
            _income = Failed;
            _value = default!;
            _failure = failure;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private RichResult(Exception exception)
        {
            _income = Exception;
            _value = default!;
            _failure = default!;
            _exception = exception;
        }

        [Pure] public static RichResult<TFirst, TSecond> Failure { get; } = new (Expected.Failed);
        [Pure] public static RichResult<TFirst, TSecond> Error { get; } = new (Unexpected.Error);
        [Pure] public static RichResult<TFirst, TSecond> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult<TFirst, TSecond> ((TFirst First, TSecond Second) income) => new (income.First, income.Second);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult<TFirst, TSecond> (Expected.Failure expected) => new (expected);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator RichResult<TFirst, TSecond> (Exception unexpected) => new (unexpected);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult<TFirst, TSecond> FromValue(TFirst first, TSecond second) => (first, second);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult<TFirst, TSecond> FromFailure(Expected.Failure failure) => failure;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static RichResult<TFirst, TSecond> FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TAnother> Run<TAnother>(Func<TFirst, TSecond, RichResult<TAnother>> func)
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure,
                Value => func.Invoke(_value.First, _value.Second),
                _ => RichResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TAnother, TAnotherOne> Run<TAnother, TAnotherOne>(Func<TFirst, TSecond, RichResult<TAnother, TAnotherOne>> func)
        {
            return _income switch
            {
                Exception => _exception,
                Failed => _failure,
                Value => func.Invoke(_value.First, _value.Second),
                _ => RichResult<TAnother, TAnotherOne>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match
        (
            Action<TFirst, TSecond> success,
            Action<Expected.Failure> failure,
            Action<Exception> error
        ) {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Failed:
                    failure.Invoke(_failure);
                    return;
                case Value:
                    success.Invoke(_value.First, _value.Second);
                    return;
                default:
                    error.Invoke(Unexpected.Impossible);
                    return;
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>
        (
            Func<TFirst, TSecond, TMatch> success,
            Func<Expected.Failure, TMatch> failure,
            Func<Exception, TMatch> error
        ) {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Failed => failure.Invoke(_failure),
                Value => success.Invoke(_value.First, _value.Second),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }
    }

    internal enum RichResultValueIncomeSource
    {
        Value = 1,
        Failed = 2,
        Exception = 3
    }
}
