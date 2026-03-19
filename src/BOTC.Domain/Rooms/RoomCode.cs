using System.Text.RegularExpressions;

namespace BOTC.Domain.Rooms;

public readonly partial record struct RoomCode
{
    private const int RequiredLength = 6;

    public RoomCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Room code is required.", nameof(value));
        }

        var normalizedValue = value.Trim();

        if (normalizedValue.Length != RequiredLength)
        {
            throw new ArgumentException($"Room code must be exactly {RequiredLength} characters.", nameof(value));
        }

        if (!RoomCodePattern().IsMatch(normalizedValue))
        {
            throw new ArgumentException("Room code must contain only uppercase letters A-Z and digits 0-9.", nameof(value));
        }

        Value = normalizedValue;
    }

    public string Value { get; }

    public override string ToString() => Value;

    [GeneratedRegex("^[A-Z0-9]{6}$", RegexOptions.CultureInvariant)]
    private static partial Regex RoomCodePattern();
}

