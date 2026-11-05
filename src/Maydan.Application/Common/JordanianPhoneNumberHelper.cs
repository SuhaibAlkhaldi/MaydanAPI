using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Maydan.Application.Common;

public static class JordanianPhoneNumberHelper
{
    private static readonly Regex JordanianMobileRegex = new(
        @"^(?:0|\+962|00962)?(7[789]\d{7})$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant
    );

    public static bool TryNormalize(
        string? input,
        out string normalized)
    {
        normalized = string.Empty;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var phone = input.Trim();

        var match = JordanianMobileRegex.Match(phone);

        if (!match.Success)
            return false;

        normalized = $"+962{match.Groups[1].Value}";

        return true;
    }
}