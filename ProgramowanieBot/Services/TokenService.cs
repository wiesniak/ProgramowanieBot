using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using NetCord;
using ProgramowanieBot.Models.Options;

namespace ProgramowanieBot;

internal class TokenService : ITokenService
{
    public Token Token { get; }

    public TokenService(IOptions<AuthOptions> options)
    {
        Guard.Against.Null(options);
        Guard.Against.NullOrEmpty(options.Value.Token, message: "Token has to be provided!");

        Token = new(TokenType.Bot, options.Value.Token);
    }
}