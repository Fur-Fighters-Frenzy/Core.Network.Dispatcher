using Validosik.Core.Network.Events.Bucket;

namespace Validosik.Core.Network.Dispatcher.Server
{
    public interface IServerParsedSink<TKind>
        where TKind : unmanaged, System.Enum
    {
        void OnParsed<TDto>(TKind kind, in TDto dto)
            where TDto : struct, IEventDto<TKind>;
    }
}