using System.Threading;
using System.Threading.Tasks;
using AeroScape.Server.Core.Session;

namespace AeroScape.Server.Core.Handlers;

public interface IMessageHandler<in TMessage> where TMessage : class
{
    Task HandleAsync(PlayerSession session, TMessage message, CancellationToken cancellationToken);
}