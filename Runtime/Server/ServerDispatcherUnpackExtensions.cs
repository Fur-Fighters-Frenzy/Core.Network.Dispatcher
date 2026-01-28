using System;
using Validosik.Core.Network.Envelope;
using Validosik.Core.Network.Events;

namespace Validosik.Core.Network.Dispatcher.Server
{
    public static class ServerDispatcherUnpackExtensions
    {
        public static void Unpack<TKind, TCodec>(
            this IServerDispatcher dispatcher,
            in NetEnvelope env)
            where TKind : unmanaged, Enum
            where TCodec : struct, IKindCodec<TKind>
        {
            // Ensure enum underlying type matches dispatcher contract (ushort).
            if (Enum.GetUnderlyingType(typeof(TKind)) != typeof(ushort))
                throw new InvalidOperationException(
                    $"TKind underlying type must be ushort, but was {Enum.GetUnderlyingType(typeof(TKind)).Name}.");

            var reader = new EventsReader<TKind, TCodec>(env.Payload.Span);

            while (reader.TryRead(out var kind, out var blob))
            {
                dispatcher.Dispatch(Convert.ToUInt16(kind), blob);
            }
        }
    }
}