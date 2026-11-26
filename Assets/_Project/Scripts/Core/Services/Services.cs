using System;
using System.Collections.Generic;
using UnityEngine;

namespace NocturneCocoa.Core
{
    /// <summary>
    /// Small registry for the persistent systems created by the Bootstrap scene.
    ///
    /// Why this instead of a static Instance on every manager?
    /// - Only BootstrapLoader decides which systems exist and registers them, in a known order.
    /// - Systems never reach for each other through singletons; they get dependencies passed in.
    /// - Scene objects (player, UI) that need a system ask for it here once, usually in Start().
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Registry = new Dictionary<Type, object>();

        // Clears the registry when entering Play Mode with Domain Reload disabled.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Registry.Clear();
        }

        public static void Register<T>(T service) where T : class
        {
            if (service == null)
            {
                throw new ArgumentNullException(nameof(service));
            }

            Type type = typeof(T);
            object existing;
            if (Registry.TryGetValue(type, out existing) && !ReferenceEquals(existing, service))
            {
                Debug.LogWarning($"[Services] Replacing the registered {type.Name}.");
            }

            Registry[type] = service;
        }

        public static void Unregister<T>(T service) where T : class
        {
            object existing;
            if (Registry.TryGetValue(typeof(T), out existing) && ReferenceEquals(existing, service))
            {
                Registry.Remove(typeof(T));
            }
        }

        public static bool TryGet<T>(out T service) where T : class
        {
            service = null;
            object obj;
            if (!Registry.TryGetValue(typeof(T), out obj))
            {
                return false;
            }

            service = obj as T;

            // A destroyed Unity object still exists in C# but compares equal to null.
            UnityEngine.Object unityObject = obj as UnityEngine.Object;
            if (obj is UnityEngine.Object && unityObject == null)
            {
                Registry.Remove(typeof(T));
                service = null;
            }

            return service != null;
        }

        public static T Get<T>() where T : class
        {
            T service;
            if (TryGet(out service))
            {
                return service;
            }

            throw new InvalidOperationException(
                $"[Services] {typeof(T).Name} is not registered. Make sure Play Mode starts from the Bootstrap scene " +
                "(menu: Nocturne Cocoa > Always Start From Bootstrap).");
        }
    }
}
