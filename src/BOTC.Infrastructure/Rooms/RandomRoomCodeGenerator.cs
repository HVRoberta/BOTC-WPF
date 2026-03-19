using System.Security.Cryptography;
using System.Text;
using BOTC.Application.Abstractions.Services;

namespace BOTC.Infrastructure.Rooms;

public sealed class RandomRoomCodeGenerator : IRoomCodeGenerator
{
    private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    private const int CodeLength = 6;

    public string Generate()
    {
        var result = new StringBuilder(CodeLength);

        for (var i = 0; i < CodeLength; i++)
        {
            result.Append(AllowedCharacters[RandomNumberGenerator.GetInt32(AllowedCharacters.Length)]);
        }

        return result.ToString();
    }
}

