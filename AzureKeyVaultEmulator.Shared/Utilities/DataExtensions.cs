using System.Text.RegularExpressions;

namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class DataExtensions
{
    private static readonly Regex _base64Normalise = new(@"^[\w/\:.-]+;base64,", RegexOptions.Compiled);

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a <see cref="DateTimeOffset"/> and returns the epoch time.
    /// </summary>
    /// <param name="dt">The <see cref="DateTime"/> to convert.</param>
    /// <param name="timeZone">Optional timezone to calculate the UTC offset from.</param>
    /// <returns>The epoch time, offset by <paramref name="dt"/>.</returns>
    public static long ToUnixTimeSeconds(this DateTime dt, string timeZone = "")
    {
        if (string.IsNullOrEmpty(timeZone))
            return new DateTimeOffset(dt).ToUnixTimeSeconds();

        var dto = new DateTimeOffset(dt, TimeZoneInfo.FindSystemTimeZoneById(timeZone).GetUtcOffset(dt));

        return dto.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Formats the <paramref name="guid"/> in lowercase with no hyphens ("-").
    /// </summary>
    /// <param name="guid">The guid to format.</param>
    /// <returns>A formatted <see cref="Guid"/></returns>
    public static string Neat(this Guid guid)
    {
        return guid.ToString("n");
    }

    /// <summary>
    /// Normalises Base64, replacing - with + and _ with /.
    /// </summary>
    /// <param name="bytes">The bytes to normalise</param>
    /// <returns>Normalised <paramref name="bytes"/> which will convert to Base64</returns>
    public static byte[] NormaliseForBase64(this byte[] bytes)
    {
        var str = Convert.ToBase64String(bytes);

        var normalised = _base64Normalise.Replace(str, string.Empty);

        return Convert.FromBase64String(normalised);
    }
}
