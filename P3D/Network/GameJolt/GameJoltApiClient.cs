using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Networking.GameJolt
{
    public static class GameJoltApiClient
    {
        public static async Task<(bool ok, string body, string err)> LoginAsync(
            Uri uri, TimeSpan httpTimeout, CancellationToken ct)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(httpTimeout);

            try
            {
                using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = true });
                http.Timeout = Timeout.InfiniteTimeSpan; // use CTS for timeout

                using var resp = await http.GetAsync(uri, cts.Token).ConfigureAwait(false);
                var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);

                if (!resp.IsSuccessStatusCode || string.IsNullOrWhiteSpace(body))
                    return (false, body, $"HTTP {(int)resp.StatusCode}");

                return (true, body, null);
            }
            catch (OperationCanceledException) when (cts.IsCancellationRequested)
            {
                return (false, null, $"timeout after {httpTimeout.TotalSeconds:F1}s");
            }
            catch (Exception ex)
            {
                return (false, null, ex.Message);
            }
        }
    }
}