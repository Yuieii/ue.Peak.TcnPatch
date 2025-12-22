// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ue.Peak.TcnPatch.Core
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

    /// <summary>
    /// Provides mutex-protected access to a value without distinguishing between immutable and mutable views.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Mutex{T}"/> is a specialization of <see cref="Mutex{TValue,TImmutable,TMutable}"/>
    /// where the immutable and mutable views are identical to the underlying value type.
    /// </para>
    /// <para>
    /// This type represents the simplest form of a mutex-backed value and is intended for scenarios where no view-based
    /// access restriction are required.
    /// </para>
    /// </remarks>
    /// <typeparam name="T">The type of the value protected by the mutex.</typeparam>
    public class Mutex<T> : Mutex<T, T, T>
    {
        public Mutex(T value) : base(value)
        {
        }

        public Mutex(Func<T> factory) : base(factory)
        {
        }
    }

    public class Mutex<TValue, TImmutable, TMutable> :
        AccessViewMutex<OpaqueView<TValue, TImmutable, TMutable>, TImmutable, TMutable>
        where TValue : TImmutable, TMutable
    {
        public Mutex(TValue value) : base(new OpaqueView<TValue, TImmutable, TMutable>(value))
        {
        }

        public Mutex(Func<TValue> factory) : base(() => new OpaqueView<TValue, TImmutable, TMutable>(factory()))
        {
        }

        public IContainerMutexGuard<TValue> AcquireContainer()
        {
            _lock.EnterWriteLock();
            return new ContainerMutexGuard(_lock, _value);
        }

        private class ContainerMutexGuard : ScopeGuard, IContainerMutexGuard<TValue>
        {
            private readonly ReaderWriterLockSlim _lock;
            private readonly OpaqueView<TValue, TImmutable, TMutable> _view;

            public ContainerMutexGuard(ReaderWriterLockSlim @lock, OpaqueView<TValue, TImmutable, TMutable> view)
            {
                _lock = @lock;
                _view = view;
            }

            public TValue Value
            {
                get => _view.Value;
                set => _view.Value = value;
            }

            protected override void EndScope()
            {
                _lock.ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Represents a controlled access point to a value that exposes distinct immutable and mutable views.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Implementations of <see cref="IAccessView{TImmutable,TMutable}"/> are responsible for restricting how a value is
    /// accessed by external consumers.
    /// </para>
    /// <para>
    /// Consumers should only interact with the value through the provided immutable or mutable view types, as defined by
    /// the API designer. This interface is intended to be used as an API boundary to prevent consumers from accessing
    /// the underlying concrete type directly.
    /// </para>
    /// <para>
    /// This interface does not guarantee thread safety; synchronization concerns are left to the implementing type
    /// (e.g. a mutex-backed implementation).
    /// </para>
    /// </remarks>
    /// <typeparam name="TImmutable">The type representing a readonly or immutable view of the underlying value.</typeparam>
    /// <typeparam name="TMutable">The type representing a writable or mutable view of the underlying value.</typeparam>
    public interface IAccessView<out TImmutable, out TMutable>
    {
        /// <summary>
        /// Returns an immutable view of the underlying value.
        /// </summary>
        TImmutable AsImmutableView();

        /// <summary>
        /// Returns a mutable view of the underlying value.
        /// </summary>
        TMutable AsMutableView();
    }

    public class OpaqueView<T>(T value) : OpaqueView<T, T, T>(value);

    public class OpaqueView<TValue, TImmutable, TMutable>(TValue value) :
        IAccessView<TImmutable, TMutable>
        where TValue : TImmutable, TMutable
    {
        internal TValue Value { get; set; } = value;

        public TImmutable AsImmutableView() => Value;
        public TMutable AsMutableView() => Value;
    }

    /// <summary>
    /// Provides mutex-protected access to a value through distinct immutable and mutable view types.
    /// </summary>
    /// <typeparam name="TView">The type representing an access view of the underlying value.</typeparam>
    /// <typeparam name="TImmutable">The type representing an immutable view of the underlying value.</typeparam>
    /// <typeparam name="TMutable">The type representing a mutable view of the underlying value.</typeparam>
    public class AccessViewMutex<TView, TImmutable, TMutable> :
        IMutex<TImmutable, TMutable>,
        IAccessView<ReadOnlyMutexGuard<TImmutable>, ReadWriteMutexGuard<TMutable>>
        where TView : IAccessView<TImmutable, TMutable>
    {
        protected readonly TView _value;
        protected readonly ReaderWriterLockSlim _lock = new();

        public AccessViewMutex(TView value)
        {
            _value = value;
        }

        public AccessViewMutex(Func<TView> factory)
        {
            _value = factory();
        }

        public void Dispose()
        {
            _lock.Dispose();
        }

        public ReadOnlyMutexGuard<TImmutable> AcquireShared()
        {
            _lock.EnterReadLock();
            return new ReadOnlyMutexGuard<TImmutable>(_lock, _value.AsImmutableView());
        }

        public Result<ReadOnlyMutexGuard<TImmutable>, TryLockError> TryAcquireShared()
            => TryAcquireShared(TimeSpan.Zero);

        public Result<ReadOnlyMutexGuard<TImmutable>, TryLockError> TryAcquireShared(TimeSpan timeout)
        {
            try
            {
                return _lock.TryEnterReadLock(timeout)
                    ? Result.Success(new ReadOnlyMutexGuard<TImmutable>(_lock, _value.AsImmutableView()))
                    : Result.Error(TryLockError.WouldBlock);
            }
            catch (LockRecursionException)
            {
                return Result.Error(TryLockError.WouldBlock);
            }
        }

        public ReadWriteMutexGuard<TMutable> AcquireExclusive()
        {
            _lock.EnterWriteLock();
            return new ReadWriteMutexGuard<TMutable>(_lock, _value.AsMutableView());
        }

        public Result<ReadWriteMutexGuard<TMutable>, TryLockError> TryAcquireExclusive()
            => TryAcquireExclusive(TimeSpan.Zero);

        public Result<ReadWriteMutexGuard<TMutable>, TryLockError> TryAcquireExclusive(TimeSpan timeout)
        {
            try
            {
                return _lock.TryEnterWriteLock(timeout)
                    ? Result.Success(new ReadWriteMutexGuard<TMutable>(_lock, _value.AsMutableView()))
                    : Result.Error(TryLockError.WouldBlock);
            }
            catch (LockRecursionException)
            {
                return Result.Error(TryLockError.WouldBlock);
            }
        }

        public UpgradableMutexGuard<TView, TImmutable, TMutable> AcquireUpgradable()
        {
            _lock.EnterUpgradeableReadLock();
            return new UpgradableMutexGuard<TView, TImmutable, TMutable>(_lock, _value);
        }

        #region ---- GetValueUnsafe()

        /// <summary>
        /// Gets a direct reference to the underlying values, bypassing all view-based access restrictions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method exposes the concrete underlying value without enforcing any guarantees provided by
        /// <see cref="IAccessView{TImmutable,TMutable}"/>.
        /// </para>
        /// <para>
        /// The caller is fully responsible for ensuring correctness, including but not limited to thread safety,
        /// aliasing, and maintaining invariants expected by the surrounding API.
        /// </para>
        /// <para>
        /// Misuse of this method may result in undefined or hard-to-reason-about behavior. This method is intended for
        /// advanced and low-level scenarios only. 
        /// </para>
        /// </remarks>
        public TView GetValueUnsafe() => _value;

        #endregion

        #region ---- Implementations

        // ---------------
        // IMutex implementations

        IMutexGuard<TImmutable> IImmutableMutex<TImmutable>.AcquireShared() => AcquireShared();
        IMutexGuard<TMutable> IMutableMutex<TMutable>.AcquireExclusive() => AcquireExclusive();

        IUpgradableMutexGuard<TImmutable, TMutable> IMutex<TImmutable, TMutable>.AcquireUpgradable()
            => AcquireUpgradable();

        // ---------------
        // IAccessView implementations

        ReadOnlyMutexGuard<TImmutable> IAccessView<ReadOnlyMutexGuard<TImmutable>, ReadWriteMutexGuard<TMutable>>.
            AsImmutableView() => AcquireShared();

        ReadWriteMutexGuard<TMutable> IAccessView<ReadOnlyMutexGuard<TImmutable>, ReadWriteMutexGuard<TMutable>>.
            AsMutableView() => AcquireExclusive();

        #endregion
    }

    public enum TryLockError
    {
        WouldBlock
    }

    public sealed class UpgradableMutexGuard<TView, TImmutable, TMutable> :
        ScopeGuard,
        IUpgradableMutexGuard<TImmutable, TMutable>
        where TView : IAccessView<TImmutable, TMutable>
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

        protected override void EndScope()
        {
            _lock.ExitUpgradeableReadLock();
        }

        // ---------------
        IMutexGuard<TMutable> IUpgradableMutexGuard<TImmutable, TMutable>.Upgrade() => Upgrade();
    }
}