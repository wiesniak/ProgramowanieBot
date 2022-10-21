namespace ProgramowanieBot.Handlers
{
    internal interface IHandler
    {
        Task StartAsync(CancellationToken cancellationToken);

        Task StopAsync(CancellationToken cancellationToken);
    }
}