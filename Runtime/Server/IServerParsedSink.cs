using Validosik.Core.Network.Events.Bucket;

namespace Validosik.Core.Network.Dispatcher.Server
{
    public interface IServerParsedSink<TKind, TMarker>
        where TKind : unmanaged, System.Enum
        where TMarker : class
    {
        void OnParsed<TDto>(TKind kind, in TDto dto)
            where TDto : struct, TMarker;
    }
}