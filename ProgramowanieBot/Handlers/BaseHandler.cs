using Ardalis.GuardClauses;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

namespace ProgramowanieBot.Handlers
{
    internal abstract class BaseHandler : IHandler
    {
        public BaseHandler(ILogger logger, GatewayClient discordClient)
        {
            Guard.Against.Null(logger);
            Guard.Against.Null(discordClient);

            Logger = logger;
            Client = discordClient;
        }

        protected ILogger Logger { get; }
        protected GatewayClient Client { get; }

        protected abstract Task StartHandlerAsync(CancellationToken cancellationToken);

        protected abstract Task StopHandlerAsync(CancellationToken cancellationToken);

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Client.StartAsync();
            await StartHandlerAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await StopHandlerAsync(cancellationToken);
            await Client.CloseAsync();
        }
    }
}