using System.Text;
using Ardalis.GuardClauses;
using Microsoft.Extensions.Options;
using NetCord;
using ProgramowanieBot.Models.Options;

namespace ProgramowanieBot;

internal class TokenService : ITokenService
{
    public Token Token { get; }

    public Snowflake Id { get; }

    public TokenService(IOptions<AuthOptions> options)
    {
        Guard.Against.Null(options);
        Guard.Against.NullOrEmpty(options.Value.Token, message: "Token has to be provided!");

        Token = new(TokenType.Bot, options.Value.Token);

        Id = EvaluateId(options.Value.Token);
    }

    private Snowflake EvaluateId(string token)
    {
        var index = token.IndexOf('.');
        var partial = token[..index];

        var lastSegmentLength = partial.Length % 4;

        var totalWidth = partial.Length + (lastSegmentLength == 0 ? 0 : 4 - lastSegmentLength);
        partial = partial.PadRight(totalWidth, '=');

        var converted = Convert.FromBase64String(partial);
        return new Snowflake(Encoding.ASCII.GetString(converted));
    }
}