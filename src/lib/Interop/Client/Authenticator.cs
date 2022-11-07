using System;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using NodaTime;
using STak.TakEngine;
using STak.TakHub.Interop;

namespace STak.TakHub.Client
{
    public class Authenticator
    {
        private readonly string       m_login;
        private readonly string       m_password;
        private readonly Uri          m_takHubUri;
        private          JwtTokenInfo m_tokenInfo;

        public string Login => m_login;


        public Authenticator(Uri takHubUri, string login, string password)
        {
            m_login     = login;
            m_password  = password;
            m_takHubUri = takHubUri;
            m_tokenInfo = new JwtTokenInfo();
        }


        public async Task<string> RegisterUser(string email = null)
        {
            Exception exception = null;

            string firstName = "Dummy";
            string lastName = "Dummy";
            email ??= "Dummy@Dummy.com";

            var address = CombineUris(m_takHubUri, "api/accounts/register");
            var request = new HttpRequestMessage(HttpMethod.Post, address);
            string contentStr = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                ["firstName"] = firstName,
                ["lastName" ] = lastName,
                ["email"    ] = email,
                ["userName" ] = m_login,
                ["password" ] = m_password
            });
            request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            var client = new HttpClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);

            try { response.EnsureSuccessStatusCode(); } catch (Exception ex) { exception = ex; }

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());
            string errorMessage = null;

            if (exception != null)
            {
                errorMessage = String.Empty;
                foreach (string value in payload.PropertyValues().Values<string>())
                {
                    errorMessage += value + "\r\n";
                }
                if (errorMessage == String.Empty)
                {
                    errorMessage = "An unknown error occurred.";
                }
            }

            return errorMessage;
        }


        public async Task<JwtTokenInfo> Authenticate(bool force = false)
        {
            return await GetJavaWebToken(force);
        }


        private async Task<JwtTokenInfo> GetJavaWebToken(bool force)
        {
            if (force || m_tokenInfo.ExpireInstant < SystemClock.Instance.GetCurrentInstant())
            {
                m_tokenInfo = await GetTokenAsync();
            }

            return m_tokenInfo;
        }


        private async Task<JwtTokenInfo> GetTokenAsync()
        {
            var address = CombineUris(m_takHubUri, "api/auth/login");
            var request = new HttpRequestMessage(HttpMethod.Post, address);
            string contentStr = JsonConvert.SerializeObject(new Dictionary<string, string>
            {
                ["userName"] = m_login,
                ["password"] = m_password
            });
            request.Content = new StringContent(contentStr, Encoding.UTF8, "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            var client = new HttpClient();
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseContentRead);
            response.EnsureSuccessStatusCode();

            var payload = JObject.Parse(await response.Content.ReadAsStringAsync());

            string token           = (string) payload["accessToken"]["token"].ToObject(typeof(string));
            string tokenExpireTime = (string) payload["accessToken"]["expiresIn"].ToObject(typeof(string));
            string refreshToken    = (string) payload["refreshToken"].ToObject(typeof(string));

            return new JwtTokenInfo(token, refreshToken, GetExpirationTimeInstant(Int32.Parse(tokenExpireTime)));
        }


        private static Instant GetExpirationTimeInstant(int secondsUntilExpiration)
        {
            var duration = Duration.FromSeconds(secondsUntilExpiration);
            return SystemClock.Instance.GetCurrentInstant() + duration;
        }


        private static Uri CombineUris(Uri baseUri, string child)
        {
            string baseStr = baseUri.ToString();
            if (! baseStr.EndsWith("/"))
            {
                baseStr += "/";
            }
            while (child.StartsWith("/"))
            {
                child = child.Substring(1);
            }
            return new Uri(baseStr + child);
        }
    }
}
