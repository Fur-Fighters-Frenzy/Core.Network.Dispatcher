using System;
using System.Reflection;
using JetBrains.Annotations;
using Validosik.Core.Network.Events.Bucket;

namespace Validosik.Core.Network.Dispatcher.Server
{
    public static class ServerDispatcherBootstrapper<TDispatcher, TKind, TMarker>
        where TDispatcher : IServerDispatcher
        where TKind : unmanaged, Enum
        where TMarker : class
    {
        public static void Initialize(
            TDispatcher dispatcher,
            IServerParsedSink<TKind, TMarker> sink,
            [CanBeNull] params Assembly[] assemblies)
        {
            var scanAssemblies = assemblies is { Length: > 0 }
                ? assemblies
                : AppDomain.CurrentDomain.GetAssemblies();

            foreach (var dtoType in EventDtoReflection.FindEventDtoStructs<TKind, TMarker>(scanAssemblies))
            {
                RegisterOneByReflection(dispatcher, sink, dtoType);
            }
        }

        private static void RegisterOneByReflection(TDispatcher dispatcher, IServerParsedSink<TKind, TMarker> sink, Type dtoType)
        {
            var mi = typeof(ServerDispatcherBootstrapper<TDispatcher, TKind, TMarker>)
                .GetMethod(nameof(RegisterOne), BindingFlags.NonPublic | BindingFlags.Static)
                ?? throw new MissingMethodException($"Missing {nameof(RegisterOne)} method.");

            mi.MakeGenericMethod(dtoType).Invoke(null, new object[] { dispatcher, sink });
        }

        private delegate bool TryParseDelegate<TDto>(ReadOnlySpan<byte> span, out TDto dto)
            where TDto : struct;

        private static void RegisterOne<TDto>(TDispatcher dispatcher, IServerParsedSink<TKind, TMarker> sink)
            where TDto : struct, IEventDto<TKind>, TMarker
        {
            var kind = default(TDto).Kind;
            var kindU16 = Convert.ToUInt16(kind);

            var tryFromBytes = EventDtoReflection.GetTryFromBytesOrThrow(typeof(TDto));
            var parser = (TryParseDelegate<TDto>)tryFromBytes.CreateDelegate(typeof(TryParseDelegate<TDto>));

            dispatcher.Register(kindU16, span =>
            {
                if (!parser(span, out var dto))
                    return false;

                sink.OnParsed(kind, in dto);
                return true;
            });
        }
    }
}