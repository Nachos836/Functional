#nullable enable

using System;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

using static System.Runtime.CompilerServices.MethodImplOptions;

namespace Functional
{
    using Outcome;

    using static ResultValueIncomeSource;

    public readonly struct Result<TValue>
    {
        private static bool IsValueType { get; } = typeof(TValue).IsValueType;

        private readonly ResultValueIncomeSource _income;
        private readonly TValue _value;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private Result(TValue value)
        {
            _income = Value;
            _value = value;
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private Result(Exception exception)
        {
            _income = Exception;
            _value = default!;
            _exception = exception;
        }

        [Pure] public static Result<TValue> Error { get; } = new (Unexpected.Error);
        [Pure] public static Result<TValue> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator Result<TValue> (TValue value)
        {
            if (IsValueType) return new (value);

            return Equals(value, null) is false
                ? new (value)
                : new (new ArgumentNullException(nameof(value)));
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator Result<TValue> (Exception exception)
        {
            return Equals(exception, null) is false
                ? new (exception)
                : new (new ArgumentNullException(nameof(exception)));
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static Result<TValue> FromValue(TValue value) => value;
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static Result<TValue> FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TValue, TAnother> Attach<TAnother>(TAnother another)
        {
            return _income switch
            {
                Exception => _exception,
                Value => (_value, another),
                _ => Result<TValue, TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TValue, TAnother> Attach<TAnother>(Func<TValue, Result<TAnother>> merge)
        {
            return _income switch
            {
                Exception => _exception,
                Value => Combine(merge.Invoke(_value)),
                _ => Result<TValue, TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TValue, TAnother> Combine<TAnother>(Result<TAnother> another)
        {
            return (_income, another._income) switch
            {
                (Exception, Exception) => new AggregateException(_exception, another._exception),
                (Exception, _) => _exception,
                (_, Exception) => another._exception,
                (Value, Value) => (_value, another._value),
                _ => Result<TValue, TAnother>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Run(Action<TValue?> action)
        {
            if (_income == Value)
            {
                action.Invoke(_value);
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result Run(Func<TValue?, Result> action)
        {
            return _income switch
            {
                Exception => _exception,
                Value => action.Invoke(_value),
                _ => Result.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TAnother> Run<TAnother>(Func<TValue?, Result<TAnother>> action)
        {
            return _income switch
            {
                Exception => _exception,
                Value => action.Invoke(_value),
                _ => Result<TAnother>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match(Action<TValue?> success, Action<Exception> error)
        {
            switch (_income)
            {
                case Exception:
                    error.Invoke(_exception);
                    return;
                case Value:
                    success.Invoke(_value);
                    return;
                default:
                    error.Invoke(Unexpected.Impossible);
                    return;
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TMatch Match<TMatch>(Func<TValue, TMatch> success, Func<Exception, TMatch> error)
        {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Value => success.Invoke(_value),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public TValue Match(Func<TValue, TValue> success, Func<Exception, TValue> error)
        {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Value => success.Invoke(_value),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public override string ToString()
        {
            return _income switch
            {
                Exception => _exception.Message,
                Value => _value!.ToString(),
                _ => Unexpected.Impossible.Message
            };
        }
    }

    public readonly struct Result<TFirst, TSecond>
    {
        private readonly ResultValueIncomeSource _income;
        private readonly (TFirst First, TSecond Second) _value;
        private readonly Exception _exception;

        [MethodImpl(AggressiveInlining)]
        private Result(TFirst first, TSecond second)
        {
            _income = Value;
            _value = (first, second);
            _exception = default!;
        }

        [MethodImpl(AggressiveInlining)]
        private Result(Exception exception)
        {
            _income = Exception;
            _value = default;
            _exception = exception;
        }

        [Pure] public static Result<TFirst, TSecond> Error { get; } = new (Unexpected.Error);
        [Pure] public static Result<TFirst, TSecond> Impossible { get; } = new (Unexpected.Impossible);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator Result<TFirst, TSecond> ((TFirst First, TSecond Second) income) => new (income.First, income.Second);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static implicit operator Result<TFirst, TSecond> (Exception exception) => new (exception);

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static (TFirst?, TSecond?) FromValue(TFirst first, TSecond second) => (first, second);
        [Pure]
        [MethodImpl(AggressiveInlining)]
        public static Result<TFirst, TSecond> FromException(Exception exception) => exception;

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TFirst> Reduce(Func<TFirst, TSecond, TFirst> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Value => reducer.Invoke(_value.First, _value.Second),
                _ => Result<TFirst>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TSecond> Reduce(Func<TFirst, TSecond, TSecond> reducer)
        {
            return _income switch
            {
                Exception => _exception,
                Value => reducer.Invoke(_value.First, _value.Second),
                _ => Result<TSecond>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Run(Action<TFirst, TSecond> action)
        {
            if (_income == Value)
            {
                action.Invoke(_value.First, _value.Second);
            }
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TAnother> Run<TAnother>(Func<TFirst, TSecond, Result<TAnother>> action)
        {
            return _income switch
            {
                Exception => _exception,
                Value => action.Invoke(_value.First, _value.Second),
                _ => Result<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TAnother> Run<TAnother>(Func<TFirst, TSecond, RichResult<TAnother>> action)
        {
            return _income switch
            {
                Exception => _exception,
                Value => action.Invoke(_value.First, _value.Second),
                _ => RichResult<TAnother>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public Result<TAnother, TAnotherOne> Run<TAnother, TAnotherOne>(Func<TFirst, TSecond, Result<TAnother, TAnotherOne>> action)
        {
            return _income switch
            {
                Exception => _exception,
                Value => action.Invoke(_value.First, _value.Second),
                _ => Result<TAnother, TAnotherOne>.Impossible
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public RichResult<TAnother, TAnotherOne> Run<TAnother, TAnotherOne>(Func<TFirst, TSecond, RichResult<TAnother, TAnotherOne>> action)
        {
            return _income switch
            {
                Exception => _exception,
                Value => action.Invoke(_value.First, _value.Second),
                _ => RichResult<TAnother, TAnotherOne>.Impossible
            };
        }

        [MethodImpl(AggressiveInlining)]
        public void Match(Action<TFirst, TSecond> success, Action<Exception> error)
        {
            switch (_income)
            {
                case Exception:
                    success.Invoke(_value.First, _value.Second);
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
        public TMatch Match<TMatch>(Func<TFirst, TSecond, TMatch> success, Func<Exception, TMatch> error)
        {
            return _income switch
            {
                Exception => error.Invoke(_exception),
                Value => success.Invoke(_value.First, _value.Second),
                _ => error.Invoke(Unexpected.Impossible)
            };
        }

        [Pure]
        [MethodImpl(AggressiveInlining)]
        public override string ToString()
        {
            return _income switch
            {
                Exception => _exception.Message,
                Value => $"{ _value.First } { _value.Second }",
                _ => Unexpected.Impossible.Message
            };
        }
    }

    internal enum ResultValueIncomeSource
    {
        Value = 1,
        Exception = 2
    }
}
