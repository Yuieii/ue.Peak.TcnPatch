// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Threading;

namespace ue.Peak.TcnPatch.Core
{
    public static class MutexGuardExtensions
    {
        extension<T>(IMutexGuard<T> self)
        {
            public IMutexGuard<TOut> Select<TOut>(Func<T, TOut> transform)
                => new MappedMutexGuard<T, TOut>(self, transform);
        }
    }

    public interface IMutexGuard : IDisposable;

    public interface IMutexGuard<out T> : IMutexGuard
    {
        T Value { get; }
    }

    public interface IContainerMutexGuard<T> : IMutexGuard<T>
    {
        public new T Value { get; set; }

        T IMutexGuard<T>.Value => Value;
    }

    public interface IUpgradableMutexGuard<out TImmutable, out TMutable> : IMutexGuard<TImmutable>
    {
        IMutexGuard<TMutable> Upgrade();
    }

    public sealed class MappedMutexGuard<T, TOut> : ScopeGuard, IMutexGuard<TOut>
    {
        private readonly IMutexGuard<T> _inner;
        private readonly Func<T, TOut> _transform;

        public MappedMutexGuard(IMutexGuard<T> inner, Func<T, TOut> transform)
        {
            _inner = inner;
            _transform = transform;
        }

        public TOut Value => _transform(_inner.Value);

        protected override void EndScope()
        {
            _inner.Dispose();
        }
    }

    public sealed class ReadWriteMutexGuard<TMutable> : ScopeGuard, IMutexGuard<TMutable>
    {
        private readonly TMutable _value;
        private readonly ReaderWriterLockSlim _lock;

        public ReadWriteMutexGuard(ReaderWriterLockSlim @lock, TMutable value)
        {
            _lock = @lock;
            _value = value;
        }

        public TMutable Value => _value;

        protected override void EndScope()
        {
            _lock.ExitWriteLock();
        }
    }

    public sealed class ReadOnlyMutexGuard<TImmutable> : ScopeGuard, IMutexGuard<TImmutable>
    {
        private readonly TImmutable _value;
        private readonly ReaderWriterLockSlim _lock;

        public ReadOnlyMutexGuard(ReaderWriterLockSlim @lock, TImmutable value)
        {
            _lock = @lock;
            _value = value;
        }

        public TImmutable Value => _value;

        protected override void EndScope()
        {
            _lock.ExitReadLock();
        }
    }
}