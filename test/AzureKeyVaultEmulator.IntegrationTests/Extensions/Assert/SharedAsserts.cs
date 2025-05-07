using Azure;

namespace Xunit;

public partial class Assert
{
    /// <summary>
    /// Attempts <paramref name="clientAction"/> expecting a <see cref="RequestFailedException"/> and Asserts ResponseCode == HttpStatusCode.BadRequest
    /// </summary>
    /// <typeparam name="TResult">The response object for <paramref name="clientAction"/></typeparam>
    /// <param name="clientAction">The client func to execute expecting a failure.</param>
    /// <param name="expectedStatusCode">Denotes the expected status code for the request, typically NotFound.</param>
    public static async Task RequestFailsAsync<TResult>(
        Func<Task<TResult>> clientAction,
        HttpStatusCode expectedStatusCode = HttpStatusCode.NotFound)
        where TResult : class
    {
        var exception = await ThrowsAsync<RequestFailedException>(clientAction);

        Equal((int)expectedStatusCode, exception?.Status);
    }
}
