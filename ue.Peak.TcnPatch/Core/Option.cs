// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ue.Peak.TcnPatch.Core
{
    public static class Option
    {
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);
        public static NoneValuePlaceholder None => default;
        
        public static Option<T> ToOption<T>(this T? value) where T : class
            => value == null ? Option<T>.None : Option<T>.Some(value);

        public static Option<T> ToOption<T>(this T? value) where T : struct
            => value == null ? Option<T>.None : Option<T>.Some(value.Value);

        public struct NoneValuePlaceholder;
    }
    
    public readonly struct Option<T>
    {
        public bool IsSome { get; }
        
        internal T ValueUnsafe { get; }
        
        internal Option(T value)
        {
            IsSome = true;
            ValueUnsafe = value;
        }

        public static implicit operator Option<T>(Option.NoneValuePlaceholder _) => None;

        public static Option<T> Some(T value) => new(value);

        public static Option<T> None => default;

        public bool IsNone => !IsSome;
        
        public T Unwrap() => Expect("Cannot unwrap a None value.");
        
        public T Expect(string message) 
            => IsSome ? ValueUnsafe : throw new InvalidOperationException(message);

        public T Expect(Exception ex) 
            => IsSome ? ValueUnsafe : ex.Rethrow<T>();

        public Option<TResult> Select<TResult>(Func<T, TResult> selector) 
            => IsNone ? Option<TResult>.None : Option<TResult>.Some(selector(ValueUnsafe));

        public Option<TResult> SelectMany<TResult>(Func<T, Option<TResult>> selector)
            => IsNone ? Option<TResult>.None : selector(ValueUnsafe);
        
        public Option<TResult> SelectMany<TSelectMany, TResult>(
            Func<T, Option<TSelectMany>> selector, 
            Func<Option<T>, TSelectMany, TResult> resultSelector)
        {
            var val = ValueUnsafe;
            var self = this;
            return IsNone
                ? Option<TResult>.None
                : selector(val).Select(v => resultSelector(self, v));
        }

        public Option<T> IfSome(Action<T> action)
        {
            if (IsSome) action(ValueUnsafe);
            return this;
        }

        public Option<T> IfNone(Action action)
        {
            if (IsNone) action();
            return this;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        [SuppressMessage("ReSharper", "ParameterHidesMember")]
        public Option<T> Match(Action? None = null, Action<T>? Some = null)
        {
            if (IsNone) None?.Invoke();
            if (IsSome) Some?.Invoke(ValueUnsafe);
            return this;
        }

        public IEnumerable<T> AsEnumerable()
        {
            if (IsNone) yield break; 
            yield return ValueUnsafe;
        }

        public Option<T> Or(Option<T> other)
            => IsSome ? this : other;
        
        public Option<T> Where(Func<T, bool> predicate)
            => IsNone ? this : predicate(ValueUnsafe) ? this : None;
        
        public T OrElse(T other)
            => IsSome ? ValueUnsafe : other;

        public Option<T> OrGet(Func<Option<T>> other)
            => IsSome ? this : other();
        
        public T OrElseGet(Func<T> other)
            => IsSome ? ValueUnsafe : other();
    }
}