// Copyright (c) 2025 Yuieii.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ue.Peak.TcnPatch
{
    public enum Never;

    public struct Unit
    {
        public static Unit Instance => new();
    }

    public static class Utils
    {
        extension<TKey, TValue>(IDictionary<TKey, TValue> dict)
        {
            public Option<TValue> GetOptional(TKey key) 
                => dict.TryGetValue(key, out var value) 
                    ? Option.Some(value) 
                    : Option<TValue>.None;
        }

        extension<T>(T obj) where T: Object
        {
            public Option<T> ToOptionUnity()
            {
                return obj == null 
                    ? Option.None 
                    : Option.Some(obj);
            }
        }
        
        extension(SemaphoreSlim semaphore)
        {
            public async Task<IDisposable> CreateScopeAsync() 
                => await SemaphoreSlimGuard.CreateAsync(semaphore);
        
            public IDisposable CreateScope()
                => new SemaphoreSlimGuard(semaphore);

            public void EnterScope(Action action)
            {
                using var scope = semaphore.CreateScope();
                action();
            }

            public T EnterScope<T>(Func<T> func)
            {
                using var scope = semaphore.CreateScope();
                return func();
            }

            public async Task EnterScopeAsync(Action action)
            {
                using var scope = await semaphore.CreateScopeAsync();
                action();
            }

            public async Task<T> EnterScopeAsync<T>(Func<T> func)
            {
                using var scope = await semaphore.CreateScopeAsync();
                return func();
            }
        
            public async Task EnterScopeAsync(Func<Task> action)
            {
                using var scope = await semaphore.CreateScopeAsync();
                await action();
            }
        
            public async Task<T> EnterScopeAsync<T>(Func<Task<T>> action)
            {
                using var scope = await semaphore.CreateScopeAsync();
                return await action();
            }
        }

        extension(Exception ex)
        {
            [DoesNotReturn]
            public void Rethrow() 
                => ExceptionDispatchInfo.Capture(ex).Throw();

            [DoesNotReturn]
            public T Rethrow<T>()
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
                return default;
            }
        }

        private class SemaphoreSlimGuard : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;

            public SemaphoreSlimGuard(SemaphoreSlim semaphore, bool wait = true)
            {
                _semaphore = semaphore;
                if (wait) semaphore.Wait();
            }

            public static async Task<SemaphoreSlimGuard> CreateAsync(SemaphoreSlim semaphore)
            {
                await semaphore.WaitAsync();
                return new SemaphoreSlimGuard(semaphore, false);
            }

            public void Dispose()
            {
                _semaphore.Release();
            }
        }
    
        public static Task WaitForFramesAsync(int frames)
        {
            var tcs = new TaskCompletionSource<bool>();
            Plugin.Instance.StartCoroutine(DelayFramesCoroutine(tcs, frames, () => true));
            return tcs.Task;
        }

        public static async Task WaitForNextFrameAsync() => await WaitForFramesAsync(1);

        private static IEnumerator DelayFramesCoroutine<T>(TaskCompletionSource<T> source, int frames, Func<T> resultGetter)
        {
            for (var i = 0; i < frames; i++)
            {
                yield return new WaitForEndOfFrame();
            }
        
            source.SetResult(resultGetter());
        }
    }

    public enum ReturnFlow
    {
        /// <summary>
        /// The handler should continue the further operations.
        /// </summary>
        Continue,
        
        /// <summary>
        /// The handler should immediately return, not performing all further unnecessary operations.
        /// </summary>
        Break,
    }
}