// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ue.Peak.TcnPatch.Core
{
    public interface IResult
    {
        /// <summary>
        /// Returns <c>true</c> if the result is <c>Success</c>.
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Returns <c>true</c> if the result is <c>Error</c>.
        /// </summary>
        bool IsError { get; }
    }

    public interface IResultSuccess<T> : IResult
    {
        /// <summary>
        /// Converts from <see cref="Result{T, TError}">Result&lt;T, TError&gt;</see> to <see cref="Option{T}">Option&lt;T&gt;</see>.
        /// </summary>
        Option<T> Value { get; }

        /// <summary>
        /// Returns the contained <c>Success</c> value.
        /// </summary>
        T Unwrap();

        /// <summary>
        /// Maps a <see cref="Result{T,TError}">Result&lt;T, TError&gt;</see> to
        /// <see cref="Result{TResult,TError}">Result&lt;TResult, TError&gt;</see> by applying a function to a contained
        /// <c>Success</c> value, leaving an <c>Error</c> value untouched.
        /// </summary>
        IResultSuccess<TResult> Select<TResult>(Func<T, TResult> transform);
    }

    public interface IResultError<T> : IResult
    {
        /// <summary>
        /// Converts from <see cref="Result{T, TError}">Result&lt;T, TError&gt;</see> to <see cref="Option{TError}">Option&lt;TError&gt;</see>.
        /// </summary>
        Option<T> Error { get; }

        /// <summary>
        /// Returns the contained <c>Error</c> value.
        /// </summary>
        T UnwrapError();

        /// <summary>
        /// Maps a <see cref="Result{T,TError}">Result&lt;T, TError&gt;</see> to
        /// <see cref="Result{T,TResult}">Result&lt;T, TResult&gt;</see> by applying a function to a contained
        /// <c>Error</c> value, leaving a <c>Success</c> value untouched.
        /// </summary>
        IResultError<TResult> SelectError<TResult>(Func<T, TResult> transform);
    }

    public interface IResult<T, TError> : IResultSuccess<T>, IResultError<TError>
    {
        /// <inheritdoc cref="IResultSuccess{T}.Select" />
        new IResult<TResult, TError> Select<TResult>(Func<T, TResult> transform);

        /// <inheritdoc cref="IResultError{TError}.SelectError" />
        new IResult<T, TResult> SelectError<TResult>(Func<TError, TResult> transform);

        IResultSuccess<TResult> IResultSuccess<T>.Select<TResult>(Func<T, TResult> transform)
            => Select(transform);

        IResultError<TResult> IResultError<TError>.SelectError<TResult>(Func<TError, TResult> transform)
            => SelectError(transform);
    }

    public static class Result
    {
        public static PartialWithSuccess<T> Success<T>(T value) => new(value);

        public static PartialWithError<T> Error<T>(T error) => new(error);
        
        public static Result<T, Exception> Catch<T>(Func<T> func)
        {
            try
            {
                return Success(func());
            }
            catch (Exception ex)
            {
                return Error(ex);
            }
        }

        public static T Branch<T>(this IResult<T, T> self)
            => self.IsSuccess ? self.Unwrap() : self.UnwrapError();

        public static Result<T, TError> SafeUnbox<T, TError, TResult>(this TResult self)
            where TResult : IResult<T, TError>
        {
            if (self is Result<T, TError> result)
                return result;

            return self
                .Select(val => Success(val).FulfillErrorType<TError>())
                .SelectError(val => Error(val).FulfillSuccessType<T>())
                .Branch();
        }

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

        public static T FastUnwrap<T>(this Result<T, Never> self) => self.ValueUnsafe;

        public static T FastUnwrapError<T>(this Result<Never, T> self) => self.ErrUnsafe;

        public static Result<T, TError> Flatten<T, TError>(this Result<Result<T, TError>, TError> self)
            => self.SelectMany(s => s);

        public static Result<IEnumerable<TItem>, TError> Process<TItem, TError>(
            this IEnumerable<Result<TItem, TError>> enumerable)
        {
            var result = Enumerable.Empty<TItem>();

            foreach (var res in enumerable)
            {
                if (res.IsError)
                    return Error(res.UnwrapError());

                result = result.Append(res.Unwrap());
            }

            return Success(result);
        }
    }

    public abstract class Result<T, TError> :
        IResult<T, TError>,
        IEquatable<Result<T, TError>>
    {
        internal Result()
        {
        }

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

        /// <inheritdoc cref="IResultError{T}.Error" />
        public Option<TError> Err
            => this is ErrorBranch e ? Option<TError>.Some(e.Error) : Option<TError>.None;

        Option<TError> IResultError<TError>.Error => Err;

        public T Unwrap()
            => Value.Expect("Cannot unwrap a success value from an Error branch.");

        public TError UnwrapError()
            => Err.Expect("Cannot unwrap an error value from a Success branch.");

        /// <inheritdoc cref="IResult{T,TError}.Select" />
        public abstract Result<TResult, TError> Select<TResult>(Func<T, TResult> func);

        IResult<TResult, TError> IResult<T, TError>.Select<TResult>(Func<T, TResult> transform)
            => Select(transform);

        public abstract Result<TResult, TError> SelectMany<TResult>(Func<T, Result<TResult, TError>> func);

        public Result<TResult, TError> SelectMany<TMiddle, TResult>(
            Func<T, Result<TMiddle, TError>> func,
            Func<Result<T, TError>, TMiddle, TResult> resultSelector)
        {
            return IsError
                ? Result.Error(ErrUnsafe)
                : func(ValueUnsafe).Select(m => resultSelector(this, m));
        }

        /// <inheritdoc cref="IResult{T,TError}.SelectError" />
        public abstract Result<T, TErrorResult> SelectError<TErrorResult>(Func<TError, TErrorResult> func);

        IResult<T, TResult> IResult<T, TError>.SelectError<TResult>(Func<TError, TResult> transform)
            => SelectError(transform);

        public abstract Result<T, TError> IfSuccess(Action<T> action);

        public abstract Result<T, TError> IfError(Action<TError> action);

        public IEnumerable<T> AsEnumerable()
            => Value.AsEnumerable();

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

        public bool Equals(Result<T, TError>? other)
        {
            if (ReferenceEquals(other, null)) return false;
            if (IsSuccess != other.IsSuccess) return false;

            return IsSuccess
                ? ValueUnsafe!.Equals(other.ValueUnsafe)
                : ErrUnsafe!.Equals(other.ErrUnsafe);
        }

        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, obj) || obj is Result<T, TError> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return IsSuccess
                ? HashCode.Combine(true, ValueUnsafe!)
                : HashCode.Combine(false, ErrUnsafe!);
        }

        public static bool operator ==(Result<T, TError>? left, Result<T, TError>? right)
            => Equals(left, right);

        public static bool operator !=(Result<T, TError>? left, Result<T, TError>? right)
            => !Equals(left, right);
    }
}