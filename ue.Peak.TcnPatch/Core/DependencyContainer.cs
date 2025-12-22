// Copyright (c) 2025 Yuieii.

#nullable enable
using System;
using System.Collections.Concurrent;

namespace ue.Peak.TcnPatch.Core
{
    public class DependencyContainer
    {
        private readonly ConcurrentDictionary<Type, object> _dependencies = [];
        private readonly Option<DependencyContainer> _parent;

        public DependencyContainer(Option<DependencyContainer> parent = default)
        {
            _parent = parent;
        }

        public DependencyContainer(DependencyContainer? parent)
        {
            _parent = parent.ToOption();
        }

        public void Register<T>(T dependency)
            => Register(typeof(T), dependency!);

        public bool HasService<T>()
            => _dependencies.ContainsKey(typeof(T));

        public bool HasService(Type type)
            => _dependencies.ContainsKey(type);

        public void Register(Type type, object dependency)
        {
            if (dependency == null)
            {
                throw new ArgumentException("Dependency cannot be null.", nameof(dependency));
            }

            if (_parent
                    .Select(o => o.HasService(type))
                    .OrElse(false) || !_dependencies.TryAdd(type, dependency))
            {
                throw new InvalidOperationException($"The dependency of type {type.FullName} is already registered.");
            }
        }

        public Option<T> TryGet<T>()
            => TryGet(typeof(T)).Cast<T>();

        public Option<object> TryGet(Type type)
        {
            if (_dependencies.TryGetValue(type, out var result))
                return Option.Some(result);

            return _parent
                .Select(o => o.Get(type));
        }

        public T Get<T>()
            => TryGet<T>()
                .Expect(() => CreateNotRegisteredException(typeof(T)));

        public object Get(Type type)
            => TryGet(type)
                .Expect(() => CreateNotRegisteredException(type));

        private Exception CreateNotRegisteredException(Type type)
            => new ArgumentException($"The dependency of type {type.FullName} is not registered.");
    }
}