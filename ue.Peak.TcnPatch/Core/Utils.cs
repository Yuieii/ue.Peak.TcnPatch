// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace ue.Peak.TcnPatch.Core
{
    /// <summary>
    /// Represents the type of computations which never resolve to any meaningful value at all.
    /// </summary>
    public enum Never;

    /// <summary>
    /// Represents the type which has exactly one value.
    /// </summary>
    public struct Unit
    {
        public static Unit Instance => new();
    }

    public static class Utils
    {
        public static Option<TValue> GetOptional<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key)
            => dict.TryGetValue(key, out var value)
                ? Option.Some(value)
                : Option<TValue>.None;

        private static Result<Unit, AggregateException> SafeInvokeInternal<T>(T? self, Action<T> invoke)
            where T : Delegate
        {
            if (self == null) return Result.Success(Unit.Instance);

            var exceptions = new Lazy<List<Exception>>(() => []);

            foreach (var action in self.GetInvocationList().Cast<T>())
            {
                try
                {
                    invoke(action);
                }
                catch (Exception ex)
                {
                    exceptions.Value.Add(ex);
                }
            }

            if (exceptions.IsValueCreated)
            {
                return Result.Error(new AggregateException(
                    "One or more exceptions occured while safe-invoking the delegate.",
                    exceptions.Value
                ));
            }

            return Result.Success(Unit.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit, AggregateException> SafeInvoke(this Action? self)
            => SafeInvokeInternal(self, a => a());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit, AggregateException> SafeInvoke<T>(this Action<T>? self, T arg)
            => SafeInvokeInternal(self, a => a(arg));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Result<Unit, AggregateException> SafeInvoke<T1, T2>(this Action<T1, T2>? self, T1 arg1, T2 arg2)
            => SafeInvokeInternal(self, a => a(arg1, arg2));

        extension(SemaphoreSlim semaphore)
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public async Task<IDisposable> CreateScopeAsync()
                => await SemaphoreSlimGuard.CreateAsync(semaphore);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public IDisposable CreateScope()
                => SemaphoreSlimGuard.Create(semaphore);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void EnterScope(Action action)
            {
                using var scope = semaphore.CreateScope();
                action();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T EnterScope<T>(Func<T> func)
            {
                using var scope = semaphore.CreateScope();
                return func();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public async Task EnterScopeAsync(Action action)
            {
                using var scope = await semaphore.CreateScopeAsync();
                action();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public async Task<T> EnterScopeAsync<T>(Func<T> func)
            {
                using var scope = await semaphore.CreateScopeAsync();
                return func();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public async Task EnterScopeAsync(Func<Task> action)
            {
                using var scope = await semaphore.CreateScopeAsync();
                await action();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public async Task<T> EnterScopeAsync<T>(Func<Task<T>> action)
            {
                using var scope = await semaphore.CreateScopeAsync();
                return await action();
            }
        }

        extension(Exception ex)
        {
            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public Never Rethrow()
                => Rethrow<Never>(ex);

            [DoesNotReturn]
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public T Rethrow<T>()
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return default;
            }
        }
    }
}