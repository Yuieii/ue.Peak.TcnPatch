// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace ue.Core
{
    public static class MutexExtensions
    {
        public static Task<IMutexGuard<T>> AcquireSharedAsync<T>(this IImmutableMutex<T> mutex)
            => Task.Run(mutex.AcquireShared);

        public static Task<IMutexGuard<T>> AcquireExclusiveAsync<T>(this IMutableMutex<T> mutex)
            => Task.Run(mutex.AcquireExclusive);

        public static Task<IUpgradableMutexGuard<TImmutable, TMutable>> AcquireUpgradableAsync<TImmutable, TMutable>(
            this IMutex<TImmutable, TMutable> mutex)
            => Task.Run(mutex.AcquireUpgradable);
    }

    public interface IMutex : IDisposable;

    public interface IImmutableMutex<out T> : IMutex
    {
        IMutexGuard<T> AcquireShared();
    }

    public interface IMutableMutex<out T> : IMutex
    { 
        IMutexGuard<T> AcquireExclusive();
    }

    public interface IMutex<out TImmutable, out TMutable> : IImmutableMutex<TImmutable>, IMutableMutex<TMutable>
    {
        IUpgradableMutexGuard<TImmutable, TMutable> AcquireUpgradable();
    }

    public interface IMutexGuard : IDisposable;

    public interface IMutexGuard<out T> : IMutexGuard
    {
        T Value { get; }
    }

    public interface IUpgradableMutexGuard<out TImmutable, out TMutable> : IMutexGuard<TImmutable>
    {
        IMutexGuard<TMutable> Upgrade();
    }

    public class Mutex<T> : Mutex<T, T, T>
    {
        public Mutex(T value) : base(value) {}
        public Mutex(Func<T> factory) : base(factory) {}
    }
    
    public class Mutex<TValue, TImmutable, TMutable> : ViewBasedMutex<SimpleMutabilityView<TValue, TImmutable, TMutable>, TImmutable, TMutable>
        where TValue : TImmutable, TMutable
    {
        public Mutex(TValue value) : base(new SimpleMutabilityView<TValue, TImmutable, TMutable>(value)) {}
        public Mutex(Func<TValue> factory) : base(() => new SimpleMutabilityView<TValue, TImmutable, TMutable>(factory())) {}
    }

    public interface IMutabilityView<out TImmutable, out TMutable>
    {
        TImmutable AsImmutableView();
        TMutable AsMutableView();
    }

    public class SimpleMutabilityView<T>(T value) : SimpleMutabilityView<T, T, T>(value);

    public class SimpleMutabilityView<TValue, TImmutable, TMutable>(TValue value) : IMutabilityView<TImmutable, TMutable>
        where TValue : TImmutable, TMutable
    {
        public TImmutable AsImmutableView() => value;
        public TMutable AsMutableView() => value;
    }

    public class ViewBasedMutex<TView, TImmutable, TMutable> : IMutex<TImmutable, TMutable>
        where TView : IMutabilityView<TImmutable, TMutable>
    {
        private readonly TView _value;
        private readonly ReaderWriterLockSlim _lock = new();

        public ViewBasedMutex(TView value)
        {
            _value = value;
        }

        public ViewBasedMutex(Func<TView> factory)
        {
            _value = factory();
        }

        public void Dispose()
        {
            _lock.Dispose();
        }
    
        public TView GetValueUnsafe() => _value;

        public ReadOnlyMutexGuard<TImmutable> AcquireShared()
        {
            _lock.EnterReadLock();
            return new ReadOnlyMutexGuard<TImmutable>(_lock, _value.AsImmutableView());
        }

        public ReadWriteMutexGuard<TMutable> AcquireExclusive()
        {
            _lock.EnterWriteLock();
            return new ReadWriteMutexGuard<TMutable>(_lock, _value.AsMutableView());
        }

        public UpgradableMutexGuard<TView, TImmutable, TMutable> AcquireUpgradable()
        {
            _lock.EnterUpgradeableReadLock();
            return new UpgradableMutexGuard<TView, TImmutable, TMutable>(_lock, _value);
        }
    
        // ---------------
        IMutexGuard<TImmutable> IImmutableMutex<TImmutable>.AcquireShared() => AcquireShared();
        IMutexGuard<TMutable> IMutableMutex<TMutable>.AcquireExclusive() => AcquireExclusive();
        IUpgradableMutexGuard<TImmutable, TMutable> IMutex<TImmutable, TMutable>.AcquireUpgradable() => AcquireUpgradable();
    }

    public sealed class UpgradableMutexGuard<TView, TImmutable, TMutable> : IUpgradableMutexGuard<TImmutable, TMutable>
        where TView : IMutabilityView<TImmutable, TMutable>
    {
        private readonly TView _value;
        private readonly ReaderWriterLockSlim _lock;
    
        public UpgradableMutexGuard(ReaderWriterLockSlim @lock, TView value)
        {
            _lock = @lock;
            _value = value;
        }
    
        public TImmutable Value => _value.AsImmutableView();

        public ReadWriteMutexGuard<TMutable> Upgrade() => new(_lock, _value.AsMutableView());

        public void Dispose()
        {
            _lock.ExitUpgradeableReadLock();
        }
    
        // ---------------
        IMutexGuard<TMutable> IUpgradableMutexGuard<TImmutable, TMutable>.Upgrade() => Upgrade();
    }

    public sealed class ReadWriteMutexGuard<TMutable> : IMutexGuard<TMutable>
    {
        private readonly TMutable _value;
        private readonly ReaderWriterLockSlim _lock;
    
        public ReadWriteMutexGuard(ReaderWriterLockSlim @lock, TMutable value)
        {
            _lock = @lock;
            _value = value;
        }
    
        public TMutable Value => _value;

        public void Dispose()
        {
            _lock.ExitWriteLock();
        }
    }

    public sealed class ReadOnlyMutexGuard<TImmutable> : IMutexGuard<TImmutable>
    {
        private readonly TImmutable _value;
        private readonly ReaderWriterLockSlim _lock;
    
        public ReadOnlyMutexGuard(ReaderWriterLockSlim @lock, TImmutable value)
        {
            _lock = @lock;
            _value = value;
        }
    
        public TImmutable Value => _value;
    
        public void Dispose()
        {
            _lock.ExitReadLock();
        }
    }
}