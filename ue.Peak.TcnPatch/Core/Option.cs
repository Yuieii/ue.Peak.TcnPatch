// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ue.Peak.TcnPatch.Core
{
    /// <summary>
    /// Represents an optional value.
    /// Every <c>Option</c> is either <c>Some</c> and contains a value, or <see cref="Option{T}.None">None</see>, and does not.
    /// </summary>
    public interface IOption
    {
        /// <summary>
        /// Returns <c>true</c> if the option is a <c>Some</c> value.
        /// </summary>
        bool IsSome { get; }

        /// <summary>
        /// Returns <c>true</c> if the option is a <see cref="Option{T}.None">None</see> value.
        /// </summary>
        bool IsNone { get; }

        IOption<T> Cast<T>();
    }

    /// <inheritdoc cref="IOption" />
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public interface IOption<out T> : IOption
    {
        /// <summary>
        /// Returns the contained <c>Some</c> value.
        /// </summary>
        T Unwrap();

        /// <summary>
        /// Returns the contained <c>Some</c> value, throwing an exception with the provided message if the value is a
        /// <see cref="Option{T}.None">None</see>.
        /// </summary>
        T Expect(string message);

        /// <summary>
        /// Returns the contained <c>Some</c> value, throwing a provided exception if the value is a
        /// <see cref="Option{T}.None">None</see>.
        /// </summary>
        T Expect(Exception ex);

        /// <summary>
        /// Returns the contained <c>Some</c> value, throwing a provided exception if the value is a
        /// <see cref="Option{T}.None">None</see>.
        /// </summary>
        T Expect(Func<Exception> ex);

        IOption<TResult> Select<TResult>(Func<T, TResult> selector);

        IOption<TResult> SelectMany<TResult>(Func<T, IOption<TResult>> selector);

        IOption<T> Where(Func<T, bool> predicate);
    }

    public static class Option
    {
        /// <summary>
        /// Creates a <c>Some</c> value.
        /// </summary>
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);

        /// <summary>
        /// Returns a placeholder for creating a <see cref="Option{T}.None">None</see> value.
        /// </summary>
        public static NoneValuePlaceholder None => default;

        public static Option<T> ToOption<T>(this T? value) where T : class
            => value == null ? Option<T>.None : Option<T>.Some(value);

        public static Option<T> ToOption<T>(this T? value) where T : struct
            => value == null ? Option<T>.None : Option<T>.Some(value.Value);

        public static IOption<T> Cast<T>(this IOption self)
            => (IOption<T>) self;

        public static TOption Or<TOption, TValue>(this TOption self, TOption other) where TOption : IOption<TValue>
            => self.IsSome ? self : other;

        public static Option<T> GetOptional<T>(this IReadOnlyList<T> self, int index)
        {
            if (index < 0 || index >= self.Count)
                return None;

            return Some(self[index]);
        }

        extension<T>(IOption<T> self)
        {
            public T OrElse(T other)
                => self.IsSome ? self.Unwrap() : other;

            public T OrElseGet(Func<T> other)
                => self.IsSome ? self.Unwrap() : other();

            public Option<T> SafeUnbox()
            {
                if (self is Option<T> result)
                    return result;

                return self
                    .Select(Option<T>.Some)
                    .OrElse(Option<T>.None);
            }
        }

        extension<TFirst, TSecond>(IOption<(TFirst, TSecond)> self)
        {
            public (Option<TFirst> First, Option<TSecond> Second) Unzip()
                => self
                    .Select(t => (Some(t.Item1), Some(t.Item2)))
                    .OrElse((None, None));
        }

        public struct NoneValuePlaceholder
        {
            public Option<T> FulfillType<T>()
                => Option<T>.None;

            public IOption FulfillType(Type type)
            {
                var t = typeof(Option<>).MakeGenericType(type);
                return (IOption) Activator.CreateInstance(t);
            }
        }
    }

    /// <inheritdoc cref="IOption{T}" />
    public readonly struct Option<T> : IOption<T>
    {
        public bool IsSome { get; }

        private T ValueUnsafe { get; }

        private Option(T value)
        {
            IsSome = true;
            ValueUnsafe = value;
        }

        public static implicit operator Option<T>(Option.NoneValuePlaceholder _) => None;

        /// <summary>
        /// Represents some value of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value of type <typeparamref name="T"/>.</param>
        public static Option<T> Some(T value) => new(value);

        /// <summary>
        /// Represents no value.
        /// </summary>
        public static Option<T> None => default;

        public bool IsNone => !IsSome;

        public T Unwrap()
            => Expect("Cannot unwrap a None value.");

        public T Expect(string message)
            => IsSome ? ValueUnsafe : throw new InvalidOperationException(message);

        public T Expect(Exception ex)
            => IsSome ? ValueUnsafe : ex.Rethrow<T>();

        public T Expect(Func<Exception> ex)
            => IsSome ? ValueUnsafe : ex().Rethrow<T>();

        public Option<TResult> Select<TResult>(Func<T, TResult> selector)
            => IsSome
                ? Option<TResult>.Some(selector(ValueUnsafe))
                : Option<TResult>.None;

        IOption<TResult> IOption<T>.Select<TResult>(Func<T, TResult> selector)
            => Select(selector);

        public Option<TResult> SelectMany<TResult>(Func<T, Option<TResult>> selector)
            => IsSome
                ? selector(ValueUnsafe)
                : Option<TResult>.None;

        IOption<TResult> IOption<T>.SelectMany<TResult>(Func<T, IOption<TResult>> selector)
            => SelectMany<TResult>(t => selector(t)
                .Select(Option.Some)
                .OrElse(Option.None));

        public Option<TResult> SelectMany<TSelectMany, TResult>(
            Func<T, Option<TSelectMany>> selector,
            Func<Option<T>, TSelectMany, TResult> resultSelector)
        {
            var val = ValueUnsafe;
            var self = this;
            return IsSome
                ? selector(val).Select(v => resultSelector(self, v))
                : Option<TResult>.None;
        }

        public Option<T> IfSome(Action<T> action)
        {
            if (IsSome) action(ValueUnsafe);
            return this;
        }

        public Option<T> IfNone(Action action)
        {
            if (!IsSome) action();
            return this;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public Option<T> Match(Action? None = null, Action<T>? Some = null)
        {
            if (IsSome) Some?.Invoke(ValueUnsafe);
            else None?.Invoke();
            return this;
        }

        public IEnumerable<T> AsEnumerable()
        {
            if (IsSome)
            {
                yield return ValueUnsafe;
            }
        }

        public Result<T, TError> ToSuccessOr<TError>(TError error)
            => Select<Result<T, TError>>(x => Result.Success(x))
                .OrElseGet(() => Result.Error(error));

        public Option<T> Or(Option<T> other)
            => IsSome ? this : other;

        public Option<T> Where(Func<T, bool> predicate)
            => !IsSome ? this : predicate(ValueUnsafe) ? this : None;

        IOption<T> IOption<T>.Where(Func<T, bool> predicate)
            => Where(predicate);

        public T OrElse(T other)
            => IsSome ? ValueUnsafe : other;

        public Option<T> OrGet(Func<Option<T>> other)
            => IsSome ? this : other();

        public T OrElseGet(Func<T> other)
            => IsSome ? ValueUnsafe : other();

        public Option<TOut> CastOrThrow<TOut>()
            => IsSome
                ? Option.Some((TOut) (object) ValueUnsafe!)
                : Option<TOut>.None;

        public Option<TOut> Cast<TOut>()
        {
            if (!IsSome) return Option<TOut>.None;

            return ValueUnsafe is TOut result
                ? Option.Some(result)
                : Option.None;
        }

        IOption<TOut> IOption.Cast<TOut>()
            => Cast<TOut>();

        /// <summary>
        /// Returns <c>Some</c> if exactly one of this option or <paramref name="other"/> is <c>Some</c>, otherwise
        /// returns <see cref="None"/>.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public Option<T> Xor(Option<T> other)
        {
            if (IsSome == other.IsSome)
                return Option.None;

            return IsSome ? this : other;
        }

        public Option<(T, TOther)> Zip<TOther>(Option<TOther> other)
        {
            if (!IsSome || !other.IsSome) return Option.None;
            return Option.Some((ValueUnsafe, other.ValueUnsafe));
        }

        public Option<TZipped> Zip<TOther, TZipped>(Option<TOther> other, Func<T, TOther, TZipped> zip)
        {
            if (!IsSome || !other.IsSome) return Option.None;
            return Option.Some(zip(ValueUnsafe, other.ValueUnsafe));
        }
    }
}