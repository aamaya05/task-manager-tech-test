using System.Text.RegularExpressions;
using TaskManager.Domain.Exceptions;

namespace TaskManager.Domain.ValueObjects;

public record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !EmailRegex.IsMatch(value))
        {
            throw new InvalidEmailException(value);
        }

        Value = value.ToLowerInvariant();
    }
}
