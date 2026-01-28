using System;
using System.Reflection;
using JetBrains.Annotations;
using Validosik.Core.Network.Events.Bucket;
using Validosik.Core.Network.Types;

namespace Validosik.Core.Network.Dispatcher.Client
{
    public static class ClientDispatcherBootstrapper<TDispatcher, TKind>
        where TDispatcher : IClientDispatcher
        where TKind : struct
    {
        public static void Initialize(
            TDispatcher dispatcher,
            Action<ushort, PlayerId, object> onParsed,
            [CanBeNull] params Assembly[] assemblies)
        {
            var scanAssemblies = assemblies is { Length: > 0 }
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies();

            foreach (var dtoType in EventDtoReflection.FindEventDtoStructs<TKind>(scanAssemblies))
            {
                RegisterOneByReflection(dispatcher, onParsed, dtoType);
            }
        }

        private static void RegisterOneByReflection(TDispatcher dispatcher, Action<ushort, PlayerId, object> onParsed, Type dtoType)
        {
            var mi = typeof(ClientDispatcherBootstrapper<TDispatcher, TKind>)
                .GetMethod(nameof(RegisterOne), BindingFlags.NonPublic | BindingFlags.Static);

            if (mi == null)
                throw new MissingMethodException($"Missing {nameof(RegisterOne)} method.");

            mi.MakeGenericMethod(dtoType).Invoke(null, new object[] { dispatcher, onParsed });
        }

        private delegate bool TryParseDelegate<TDto>(ReadOnlySpan<byte> span, out TDto dto)
            where TDto : struct;

        private static void RegisterOne<TDto>(TDispatcher dispatcher, Action<ushort, PlayerId, object> onParsed)
            where TDto : struct, IEventDto<TKind>
        {
            // Enforce ushort-based enum contract at runtime.
            var kind = EventDtoReflection.GetKindAsUShort<TDto, TKind>();

            var tryFromBytes = EventDtoReflection.GetTryFromBytesOrThrow(typeof(TDto));
            var parser = (TryParseDelegate<TDto>)tryFromBytes.CreateDelegate(typeof(TryParseDelegate<TDto>));

            dispatcher.Register(kind, (span, sender) =>
            {
                if (!parser(span, out var dto))
                    return false;

                onParsed(kind, sender, dto);
                return true;
            });
        }
    }
}