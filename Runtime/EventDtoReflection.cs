using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Validosik.Core.Network.Events.Bucket;

namespace Validosik.Core.Network.Dispatcher
{
    internal static class EventDtoReflection
    {
        internal static IEnumerable<Type> FindEventDtoStructs<TKind, TMarker>(IEnumerable<Assembly> assemblies)
            where TKind : unmanaged, Enum
            where TMarker : class
        {
            foreach (var asm in assemblies)
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray()!;
                }

                foreach (var t in types)
                {
                    if (t == null) continue;

                    if (!t.IsValueType || t.IsEnum) continue;
                    if (!ImplementsEventDtoOfKind<TKind>(t)) continue;
                    if (!typeof(TMarker).IsAssignableFrom(t)) continue;

                    yield return t;
                }
            }
        }

        internal static bool ImplementsEventDtoOfKind<TKind>(Type t)
            where TKind : unmanaged, Enum
        {
            foreach (var i in t.GetInterfaces())
            {
                if (!i.IsGenericType) continue;
                if (i.GetGenericTypeDefinition() != typeof(IEventDto<>)) continue;

                if (i.GetGenericArguments()[0] == typeof(TKind))
                    return true;
            }
            return false;
        }

        internal static MethodInfo GetTryFromBytesOrThrow(Type dtoType)
        {
            var mi = dtoType.GetMethod(
                "TryFromBytes",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(ReadOnlySpan<byte>), dtoType.MakeByRefType() },
                modifiers: null);

            if (mi == null || mi.ReturnType != typeof(bool))
            {
                throw new MissingMethodException(
                    $"{dtoType.FullName} must have: public static bool TryFromBytes(ReadOnlySpan<byte> span, out {dtoType.Name} dto)");
            }

            return mi;
        }
    }
}