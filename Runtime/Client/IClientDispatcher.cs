using System;
using Validosik.Core.Network.Types;

namespace Validosik.Core.Network.Dispatcher.Client
{
    public interface IClientDispatcher
    {
        public delegate bool TryFromBytesDelegate(ReadOnlySpan<byte> span, PlayerId sender);
        
        void Register(ushort kind, TryFromBytesDelegate handler);

        void Unregister(ushort kind);

        bool Dispatch(ushort kind, ReadOnlySpan<byte> blob, PlayerId sender);
    }
}