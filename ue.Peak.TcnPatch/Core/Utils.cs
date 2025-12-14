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

namespace ue.Core
{
    public enum Never;

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
                } catch (Exception ex)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<IDisposable> CreateScopeAsync(this SemaphoreSlim semaphore) 
            => await SemaphoreSlimGuard.CreateAsync(semaphore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable CreateScope(this SemaphoreSlim semaphore)
            => new SemaphoreSlimGuard(semaphore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnterScope(this SemaphoreSlim semaphore, Action action)
        {
            using var scope = semaphore.CreateScope();
            action();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T EnterScope<T>(this SemaphoreSlim semaphore, Func<T> func)
        {
            using var scope = semaphore.CreateScope();
            return func();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task EnterScopeAsync(this SemaphoreSlim semaphore, Action action)
        {
            using var scope = await semaphore.CreateScopeAsync();
            action();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> EnterScopeAsync<T>(this SemaphoreSlim semaphore, Func<T> func)
        {
            using var scope = await semaphore.CreateScopeAsync();
            return func();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task EnterScopeAsync(this SemaphoreSlim semaphore, Func<Task> action)
        {
            using var scope = await semaphore.CreateScopeAsync();
            await action();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<T> EnterScopeAsync<T>(this SemaphoreSlim semaphore, Func<Task<T>> action)
        {
            using var scope = await semaphore.CreateScopeAsync();
            return await action();
        }

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Never Rethrow(this Exception ex) 
            => Rethrow<Never>(ex);

        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Rethrow<T>(this Exception ex)
        {
            ExceptionDispatchInfo.Capture(ex).Throw();
            return default;
        }

        private class SemaphoreSlimGuard : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreSlimGuard(SemaphoreSlim semaphore, bool wait = true)
            {
                _semaphore = semaphore;
                if (wait) semaphore.Wait();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static async Task<SemaphoreSlimGuard> CreateAsync(SemaphoreSlim semaphore)
            {
                await semaphore.WaitAsync();
                return new SemaphoreSlimGuard(semaphore, false);
            }

            public void Dispose()
                => _semaphore.Release();
        }
    }
}