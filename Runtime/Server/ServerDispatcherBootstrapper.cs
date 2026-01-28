using System;
using System.Reflection;
using JetBrains.Annotations;
using Validosik.Core.Network.Events.Bucket;

namespace Validosik.Core.Network.Dispatcher.Server
{
    public static class ServerDispatcherBootstrapper<TDispatcher, TKind>
        where TDispatcher : IServerDispatcher
        where TKind : struct
    {
        public static void Initialize(
            TDispatcher dispatcher,
            Action<ushort, object> onParsed,
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

        private static void RegisterOneByReflection(TDispatcher dispatcher, Action<ushort, object> onParsed, Type dtoType)
        {
            // Call the generic method RegisterOne<TDto>() once per discovered DTO type.
            var mi = typeof(ServerDispatcherBootstrapper<TDispatcher, TKind>)
                .GetMethod(nameof(RegisterOne), BindingFlags.NonPublic | BindingFlags.Static);

            if (mi == null)
                throw new MissingMethodException($"Missing {nameof(RegisterOne)} method.");

            mi.MakeGenericMethod(dtoType).Invoke(null, new object[] { dispatcher, onParsed });
        }

        private delegate bool TryParseDelegate<TDto>(ReadOnlySpan<byte> span, out TDto dto)
            where TDto : struct;

        private static void RegisterOne<TDto>(TDispatcher dispatcher, Action<ushort, object> onParsed)
            where TDto : struct, IEventDto<TKind>
        {
            // Convert handles enum boxing and keeps the dispatcher contract (ushort kind).
            var kind = EventDtoReflection.GetKindAsUShort<TDto, TKind>();

            var tryFromBytes = EventDtoReflection.GetTryFromBytesOrThrow(typeof(TDto));
            var parser = (TryParseDelegate<TDto>)tryFromBytes.CreateDelegate(typeof(TryParseDelegate<TDto>));

            dispatcher.Register(kind, span =>
            {
                if (!parser(span, out var dto))
                    return false;

                onParsed(kind, dto);
                return true;
            });
        }
    }
}