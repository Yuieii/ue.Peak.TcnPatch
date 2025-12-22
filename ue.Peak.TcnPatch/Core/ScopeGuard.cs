// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ue.Peak.TcnPatch.Core
{
    public abstract class ScopeGuard : IDisposable
    {
        protected virtual bool ShouldEndScope => true;

        protected abstract void EndScope();

        public void Dispose()
        {
            if (!ShouldEndScope) return;
            EndScope();
        }
    }

    public class DelegateScopeGuard : ScopeGuard
    {
        private readonly Action _endScope;

        private DelegateScopeGuard(Action endScope)
        {
            _endScope = endScope;
        }

        protected override void EndScope() => _endScope();
    }

    public class SemaphoreSlimGuard : ScopeGuard
    {
        private readonly SemaphoreSlim _semaphore;

        private SemaphoreSlimGuard(SemaphoreSlim semaphore)
        {
            _semaphore = semaphore;
        }

        public static SemaphoreSlimGuard Create(SemaphoreSlim semaphore)
        {
            semaphore.Wait();
            return new SemaphoreSlimGuard(semaphore);
        }

        public static async Task<SemaphoreSlimGuard> CreateAsync(SemaphoreSlim semaphore)
        {
            await semaphore.WaitAsync();
            return new SemaphoreSlimGuard(semaphore);
        }

        protected override void EndScope() => _semaphore.Release();
    }
}