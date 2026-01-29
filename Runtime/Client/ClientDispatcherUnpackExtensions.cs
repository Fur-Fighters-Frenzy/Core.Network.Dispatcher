using System;
using Validosik.Core.Network.Envelope;
using Validosik.Core.Network.Events;
using Validosik.Core.Network.Types;

namespace Validosik.Core.Network.Dispatcher.Client
{
    public static class ClientDispatcherUnpackExtensions
    {
        public static void Unpack<TKind, TCodec>(
            this IClientDispatcher dispatcher,
            in NetEnvelope env,
            PlayerId sender)
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
                dispatcher.Dispatch(Convert.ToUInt16(kind), blob, sender);
            }
        }
    }
}