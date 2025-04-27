using Azure;
using AzureKeyVaultEmulator.Shared.Utilities;

namespace AzureKeyVaultEmulator.IntegrationTests.SetupHelper
{
    public static class RequestSetup
    {
        /// <summary>
        /// Executes <paramref name="execution"/> between <paramref name="lower"/> and <paramref name="upper"/> times.
        /// </summary>
        /// <typeparam name="T">The the underlying KeyVault type to execute a request for.</typeparam>
        /// <param name="lower">The lower bound for execution count.</param>
        /// <param name="upper">The upper limit for execution count.</param>
        /// <param name="execution">The func to execute.</param>
        /// <returns>The underlying name of the <typeparamref name="T"/> response type.</returns>
        public static async Task<int> CreateMultiple<T>(
            int lower, int upper,
            Func<int, Task<Response<T>>> execution)
        {
            var executionCount = Random.Shared.Next(lower, upper);

            var tasks = Enumerable
                .Range(0, executionCount)
                .Select(i => execution(i));

            await Task.WhenAll(tasks);

            return executionCount;
        }

        public static async Task<int> CreateMultiple<T>(
    int lower, int upper,
    Func<int, Task<T>> execution)
        {
            var executionCount = Random.Shared.Next(lower, upper);

            var tasks = Enumerable
                .Range(0, executionCount)
                .Select(i => execution(i));

            await Task.WhenAll(tasks);

            return executionCount;
        }

        public static string CreateRandomData(int size = 512)
        {
            var bytes = CreateRandomBytes(size);

            return EncodingUtils.Base64UrlEncode(bytes);
        }

        public static byte[] CreateRandomBytes(int size = 512)
        {
            byte[] bytes = new byte[size];

            Random.Shared.NextBytes(bytes);

            return bytes;
        }
    }
}
