namespace AzureKeyVaultEmulator.Shared.Utilities;

public static class DataExtensions
{
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
}
