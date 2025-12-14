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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task<IDisposable> CreateScopeAsync(this SemaphoreSlim semaphore) 
            => await SemaphoreSlimGuard.CreateAsync(semaphore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IDisposable CreateScope(this SemaphoreSlim semaphore)
            => new SemaphoreSlimGuard(semaphore);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T EnterScope<T>(this SemaphoreSlim semaphore, Func<T> func)
        {
            using var scope = semaphore.CreateScope();
            return func();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static async Task EnterScopeAsync(this SemaphoreSlim semaphore, Func<Task> action)
        {
            using var scope = await semaphore.CreateScopeAsync();
            await action();
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