using Ardalis.GuardClauses;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProgramowanieBot.Handlers;

namespace ProgramowanieBot;

internal class BotService : IHostedService
{
    private readonly ILogger _logger;
    private readonly IHandler[] _handlers;

    public BotService(ILogger<BotService> logger, ITokenService tokenService, IEnumerable<IHandler> handlers)
    {
        Guard.Against.Null(logger);
        Guard.Against.Null(tokenService);
        Guard.Against.NullOrEmpty(handlers);

        _logger = logger;
        _handlers = handlers.ToArray();
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting event handlers");
        foreach (var handler in _handlers)
        {
            await handler.StartAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping event handlers");
        foreach (var handler in _handlers)
        {
            await handler.StopAsync(cancellationToken);
        }
    }
}