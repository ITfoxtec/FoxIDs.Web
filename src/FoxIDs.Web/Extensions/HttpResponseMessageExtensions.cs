using FoxIDs.Web.Infrastructure.GitHubApi;
using ITfoxtec.Identity;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace FoxIDs.Web
{
    public static class HttpResponseMessageExtensions
    {
        /// <summary>
        /// Converts a HttpResponseMessage json string to an object.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<T> ToObjectAsync<T>(this HttpResponseMessage response)
        {
            var responseText = await response.Content.ReadAsStringAsync();
            if (responseText.IsNullOrWhiteSpace())
            {
                return default;
            }

            try
            {
                return responseText.ToObject<T>();
            }
            catch (JsonException ex)
            {
                throw new GitHubApiException($"Could not deserialize the response body string as '{typeof(T).Name} '.", response.StatusCode, responseText, ex);
            }
        }
    }
}
