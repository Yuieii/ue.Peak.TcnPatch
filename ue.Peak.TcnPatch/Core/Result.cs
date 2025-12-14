// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace ue.Core
{
    public static class Result
    {
        public static PartialWithSuccess<T> Success<T>(T value) => new(value);

        public static PartialWithError<T> Error<T>(T error) => new(error);
        
        public class PartialWithSuccess<T>
        {
            private readonly T _value;

            internal PartialWithSuccess(T value)
            {
                _value = value;
            }

            public Result<T, TError> FulfillErrorType<TError>()
                => Result<T, TError>.Success(_value);
        }

        public class PartialWithError<T>
        {
            private readonly T _error;
            
            internal PartialWithError(T error)
            {
                _error = error;
            }

            public Result<TSuccess, T> FulfillSuccessType<TSuccess>()
                => Result<TSuccess, T>.Error(_error);
        }

        public static T UnwrapOrThrow<T, TError>(this Result<T, TError> result) where TError : Exception
        {
            var opt = result.Value;
            return opt.IsSome ? opt.Unwrap() : result.UnwrapError().Rethrow<T>();
        }

        public static T Branch<T>(this Result<T, T> result) 
            => result.IsSuccess ? result.Unwrap() : result.UnwrapError();

        extension<T>(Result<T, Never> self)
        {
            public T FastUnwrap() => self.ValueUnsafe;
        }
        
        extension<T>(Result<Never, T> self)
        {
            public T FastUnwrapError() => self.ErrUnsafe;
        }
    }
    
    public abstract class Result<T, TError>
    {
        internal Result() {}

        public bool IsSuccess => this is SuccessBranch;

        public bool IsError => this is ErrorBranch;

        public static implicit operator Result<T, TError>(Result.PartialWithSuccess<T> partial)
            => partial.FulfillErrorType<TError>();

        public static implicit operator Result<T, TError>(Result.PartialWithError<TError> partial)
            => partial.FulfillSuccessType<T>();

        internal T ValueUnsafe => ((SuccessBranch) this).Value;
        
        internal TError ErrUnsafe => ((ErrorBranch) this).Error;
        
        public Option<T> Value 
            => this is SuccessBranch s ? Option<T>.Some(s.Value) : Option<T>.None;

        public Option<TError> Err
            => this is ErrorBranch e ? Option<TError>.Some(e.Error) : Option<TError>.None;

        public T Unwrap() 
            => Value.Expect("Cannot unwrap a success value from an Error branch.");

        public TError UnwrapError() 
            => Err.Expect("Cannot unwrap an error value from a Success branch.");

        public abstract Result<TResult, TError> Select<TResult>(Func<T, TResult> func);
        
        public abstract Result<TResult, TError> SelectMany<TResult>(Func<T, Result<TResult, TError>> func);

        public Result<TResult, TError> SelectMany<TMiddle, TResult>(
            Func<T, Result<TMiddle, TError>> func,
            Func<Result<T, TError>, TMiddle, TResult> resultSelector)
        {
            return IsError 
                ? Result.Error(ErrUnsafe) 
                : func(ValueUnsafe).Select(m => resultSelector(this, m));
        }

        public abstract Result<T, TErrorResult> SelectError<TErrorResult>(Func<TError, TErrorResult> func);

        public abstract Result<T, TError> IfSuccess(Action<T> action);
        
        public abstract Result<T, TError> IfError(Action<TError> action);
        
        public bool TryUnwrap([MaybeNullWhen(false)] out T value)
        {
            if (IsSuccess)
            {
                value = ValueUnsafe;
                return true;
            }
            
            value = default;
            return false;
        }

        public bool TryUnwrapError([MaybeNullWhen(false)] out TError error)
        {
            if (IsError)
            {
                error = ErrUnsafe;
                return true;
            }

            error = default;
            return false;
        }
        
        // =========
        
        public static Result<T, TError> Success(T value) => new SuccessBranch(value);

        public static Result<T, TError> Error(TError error) => new ErrorBranch(error);

        private class SuccessBranch : Result<T, TError>
        {
            public new T Value { get; }

            internal SuccessBranch(T value)
            {
                Value = value;
            }
            
            public override Result<TResult, TError> Select<TResult>(Func<T, TResult> func)
                => Result.Success(func(Value));

            public override Result<TResult, TError> SelectMany<TResult>(Func<T, Result<TResult, TError>> func)
                => func(Value);

            public override Result<T, TErrorResult> SelectError<TErrorResult>(Func<TError, TErrorResult> func)
                => Result.Success(Value);

            public override Result<T, TError> IfSuccess(Action<T> action)
            {
                action(Value);
                return this;
            }

            public override Result<T, TError> IfError(Action<TError> action) => this;
        }
        
        private class ErrorBranch : Result<T, TError>
        {
            // ReSharper disable once MemberHidesStaticFromOuterClass
            public new TError Error { get; }

            internal ErrorBranch(TError error)
            {
                Error = error;
            }

            public override Result<TResult, TError> Select<TResult>(Func<T, TResult> func)
                => Result.Error(Error);

            public override Result<TResult, TError> SelectMany<TResult>(Func<T, Result<TResult, TError>> func)
                => Result.Error(Error);

            public override Result<T, TErrorResult> SelectError<TErrorResult>(Func<TError, TErrorResult> func)
                => Result.Error(func(Error));

            public override Result<T, TError> IfSuccess(Action<T> action) => this;

            public override Result<T, TError> IfError(Action<TError> action)
            {
                action(Error);
                return this;
            }
        }
    }
}