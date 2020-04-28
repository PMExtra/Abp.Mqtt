using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Abp.Mqtt.Rpc.Resolving
{
    public class RpcMethodResolver
    {
        public ImmutableDictionary<string, MethodInfo> Methods { get; private set; }

        public void AddService<T>() where T : IRpcService
        {
            AddServices(typeof(T));
        }

        public void AddServices(params Type[] types)
        {
            var methodDictionary = Methods == null
                ? new Dictionary<string, MethodInfo>()
                : new Dictionary<string, MethodInfo>(Methods);

            foreach (var type in types)
            {
                if (!type.IsAssignableFrom(typeof(IRpcService))) throw new InvalidCastException($"Cannot assign type '{type.FullName}' as '{typeof(IRpcService).FullName}'.");

                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(method => method.DeclaringType != typeof(object));

                foreach (var methodInfo in methods)
                {
                    var attribute = methodInfo.GetCustomAttribute<RpcMethodAttribute>();
                    var name = attribute.Name ?? methodInfo.Name;
                    if (!methodDictionary.TryAdd(name, methodInfo))
                    {
                        var exists = methodDictionary[name];
                        Debug.Assert(exists.DeclaringType != null);
                        throw new ArgumentException(
                            $"Method '{name}' in '{type.FullName}' has already registered with type '{exists.DeclaringType.FullName}'. Methods cannot be registered with duplicate name.");
                    }
                }
            }

            Methods = methodDictionary.ToImmutableDictionary();
        }

        public void AddServices(Assembly fromAssembly)
        {
            AddServices(fromAssembly.GetExportedTypes().Where(type => type.IsAssignableFrom(typeof(IRpcService))).ToArray());
        }
    }
}
