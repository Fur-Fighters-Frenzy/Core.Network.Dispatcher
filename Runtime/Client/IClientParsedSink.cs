using Validosik.Core.Network.Events.Bucket;
using Validosik.Core.Network.Types;

namespace Validosik.Core.Network.Dispatcher.Client
{
    public interface IClientParsedSink<TKind, TMarker>
        where TKind : unmanaged, System.Enum
        where TMarker : class
    {
        void OnParsed<TDto>(TKind kind, PlayerId sender, in TDto dto)
            where TDto : struct, TMarker;
    }
}