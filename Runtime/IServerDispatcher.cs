using System;

namespace Validosik.Core.Network.Dispatcher
{
    public interface IServerDispatcher
    {
        public delegate bool TryFromBytesDelegate(ReadOnlySpan<byte> span);

        void Register(ushort kind, TryFromBytesDelegate handler);

        void Unregister(ushort kind);

        bool Dispatch(ushort kind, ReadOnlySpan<byte> blob);
    }
}