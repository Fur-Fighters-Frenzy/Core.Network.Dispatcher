using System;
using System.Reflection;
using JetBrains.Annotations;
using Validosik.Core.Network.Events.Bucket;
using Validosik.Core.Network.Types;

namespace Validosik.Core.Network.Dispatcher.Client
{
    public static class ClientDispatcherBootstrapper<TDispatcher, TKind>
        where TDispatcher : IClientDispatcher
        where TKind : unmanaged, Enum
    {
        public static void Initialize(
            TDispatcher dispatcher,
            IClientParsedSink<TKind> sink,
            [CanBeNull] params Assembly[] assemblies)
        {
            var scanAssemblies = assemblies is { Length: > 0 }
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies();

            foreach (var dtoType in EventDtoReflection.FindEventDtoStructs<TKind>(scanAssemblies))
            {
                RegisterOneByReflection(dispatcher, sink, dtoType);
            }
        }

        private static void RegisterOneByReflection(TDispatcher dispatcher, IClientParsedSink<TKind> sink, Type dtoType)
        {
            var mi = typeof(ClientDispatcherBootstrapper<TDispatcher, TKind>)
                .GetMethod(nameof(RegisterOne), BindingFlags.NonPublic | BindingFlags.Static);

            if (mi == null)
                throw new MissingMethodException($"Missing {nameof(RegisterOne)} method.");

            mi.MakeGenericMethod(dtoType).Invoke(null, new object[] { dispatcher, sink });
        }

        private delegate bool TryParseDelegate<TDto>(ReadOnlySpan<byte> span, out TDto dto)
            where TDto : struct;

        private static void RegisterOne<TDto>(TDispatcher dispatcher, IClientParsedSink<TKind> sink)
            where TDto : struct, IEventDto<TKind>
        {
            // No per-packet reflection: all work below happens once at bootstrap time.
            var kind = default(TDto).Kind;
            var kindU16 = Convert.ToUInt16(kind);

            var tryFromBytes = EventDtoReflection.GetTryFromBytesOrThrow(typeof(TDto));
            var parser = (TryParseDelegate<TDto>)tryFromBytes.CreateDelegate(typeof(TryParseDelegate<TDto>));

            dispatcher.Register(kindU16, (span, sender) =>
            {
                if (!parser(span, out var dto))
                    return false;

                sink.OnParsed(kind, sender, in dto);
                return true;
            });
        }
    }
}