using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nancy;
using Nancy.Cookies;
using NUnit.Framework;
using Octopus.Client.Model;

namespace Octopus.Client.Tests.Integration.OctopusClient
{
    public class AntiforgeryTokenTests : HttpIntegrationTestBase
    {
        private static readonly string AuthCookieValue = "54321";
        private static readonly string AntiforgeryCookieValue = "12345";

        public AntiforgeryTokenTests() 
            : base(UrlPathPrefixBehaviour.UseClassNameAsUrlPathPrefix)
        {
            Get(TestRootPath, p =>
            {
                var antiforgeryHeaderValue = Request.Headers[ApiConstants.AntiforgeryTokenHttpHeaderName]?.FirstOrDefault();

                return Response.AsJson(new TestDto {AntiforgeryTokenValue = antiforgeryHeaderValue})
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithCookie(new NancyCookie(
                        $"{ApiConstants.AuthenticationCookiePrefix}_{InstallationId}",
                        AuthCookieValue,
                        httpOnly: true,
                        secure: Request.Url.IsSecure,
                        expires: DateTime.UtcNow.AddDays(1)))
                    .WithCookie(new NancyCookie(
                        $"{ApiConstants.AntiforgeryTokenCookiePrefix}_{InstallationId}",
                        AntiforgeryCookieValue,
                        httpOnly: false,
                        secure: Request.Url.IsSecure,
                        expires: DateTime.UtcNow.AddDays(1)));
            });
        }

        [Test]
        public async Task AsyncClient_ShouldCopyAntiforgeryCookieToHeader()
        {
            // Simulate getting the auth and antiforgery cookies
            var firstResponse = await AsyncClient.Get<TestDto>(TestRootPath);
            firstResponse.AntiforgeryTokenValue.Should()
                .BeNullOrWhiteSpace("The antiforgery cookie hasn't been sent, so we shouln't copy anyhthing to the header yet.");

            // Prove we copy the antiforgery cookie value to the header if it exists
            var secondResponse = await AsyncClient.Get<TestDto>(TestRootPath);
            secondResponse.AntiforgeryTokenValue.Should()
                .Be(AntiforgeryCookieValue, $"The antiforgery cookie should have been copied to the {ApiConstants.AntiforgeryTokenHttpHeaderName} header.");
        }

#if SYNC_CLIENT
        [Test]
        public void SyncClient_ShouldCopyAntiforgeryCookieToHeader()
        {
            var client = new Client.OctopusClient(new OctopusServerEndpoint(HostBaseUri + TestRootPath));
            // Force the root document to load
            var rootDocument = client.RootDocument;
            
            // Simulate getting the auth and antiforgery cookies
            var firstResponse = client.Get<TestDto>(TestRootPath);
            firstResponse.AntiforgeryTokenValue.Should()
                .BeNullOrWhiteSpace("The antiforgery cookie hasn't been sent, so we shouln't copy anyhthing to the header yet.");

            // Prove we copy the antiforgery cookie value to the header if it exists
            var secondResponse = client.Get<TestDto>(TestRootPath);
            secondResponse.AntiforgeryTokenValue.Should()
                .Be(AntiforgeryCookieValue, $"The antiforgery cookie should have been copied to the {ApiConstants.AntiforgeryTokenHttpHeaderName} header.");
        }
#endif

        public class TestDto
        {
            public string AntiforgeryTokenValue { get; set; }
        }
    }
}