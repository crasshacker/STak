using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STak.TakHub.Core.Dto.UseCaseRequests;
using Xunit;

namespace STak.TakHub.IntegrationTests.Controllers
{
    public class AccountsControllerIntegrationTests : IClassFixture<CustomWebApplicationFactory<Startup>>
    {
        private readonly HttpClient m_client;

        public AccountsControllerIntegrationTests(CustomWebApplicationFactory<Startup> factory)
        {
            m_client = factory.CreateClient();
        }

        [Fact]
        public async Task CanRegisterUserWithValidAccountDetails()
        {
            using var httpResponse = await m_client.PostAsync("/takhub/api/accounts/register", new StringContent(JsonConvert.SerializeObject(new RegisterUserRequest("John", "Doe", "jdoe@gmail.com", "johndoe", "Pa$$word1")), Encoding.UTF8, "application/json"));
            httpResponse.EnsureSuccessStatusCode();
            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
            dynamic result = JObject.Parse(stringResponse);
            Assert.True((bool) result.success);
            Assert.Equal(HttpStatusCode.OK, httpResponse.StatusCode);
        }

        // NOTE: This Fact is disabled until FluentValidation officially supports .NET 5.
        //       The test should be enabled and the method made public when support exists.
        //
        [Fact]
        private async Task CantRegisterUserWithInvalidAccountDetails()
        {
            using var httpResponse = await m_client.PostAsync("/takhub/api/accounts/register", new StringContent(JsonConvert.SerializeObject(new RegisterUserRequest("Jane", "Doe", "", "janedoe", "Pa$$word1")), Encoding.UTF8, "application/json"));
            var stringResponse = await httpResponse.Content.ReadAsStringAsync();
            Assert.Contains("'Email' is not a valid email address.", stringResponse);
            Assert.Equal(HttpStatusCode.BadRequest, httpResponse.StatusCode);
        }
    }
}

 

