using Azure;

namespace Xunit;

public partial class Assert
{
    /// <summary>
    /// Attempts <paramref name="clientAction"/> expecting a <see cref="RequestFailedException"/> and Asserts ResponseCode == HttpStatusCode.BadRequest
    /// </summary>
    /// <typeparam name="TResult">The response object for <paramref name="clientAction"/></typeparam>
    /// <param name="clientAction">The client func to execute expecting a failure.</param>
    public static async Task ThrowsRequestFailedAsync<TResult>(Func<Task<TResult>> clientAction)
        where TResult : class
    {
        var exception = await ThrowsAsync<RequestFailedException>(clientAction);

        Equal((int)HttpStatusCode.BadRequest, exception?.Status);
    }
}
