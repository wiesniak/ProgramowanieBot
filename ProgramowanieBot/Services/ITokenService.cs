﻿using NetCord;

namespace ProgramowanieBot
{
    internal interface ITokenService
    {
        Token Token { get; }
        Snowflake Id { get; }
    }
}