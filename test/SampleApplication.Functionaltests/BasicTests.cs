using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using AngleSharp.Dom.Html;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using SampleApplication.Functionaltests.Helpers;
using SampleApplication.Services;
using Xunit;

namespace SampleApplication.Functionaltests
{
    public class Basictests : IClassFixture<WebApplicationFactory<Program>>
    {
        public Basictests(WebApplicationFactory<Program> factory)
        {
            Factory = factory;
        }

        public WebApplicationFactory<Program> Factory { get; }

        [Fact]
        public async Task CanRetrieveHomePageAsync()
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync("/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Theory]
        [InlineData("/")]
        [InlineData("/Index")]
        [InlineData("/About")]
        [InlineData("/Contact")]
        [InlineData("/Privacy")]
        public async Task CanGetApplicationEndpoints(string url)
        {
            // Arrange
            var client = Factory.CreateClient();

            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        [Fact]
        public async Task ManagingUsersRequiresAnAuthenticatedUser()
        {
            // Arrange
            var client = Factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost/"),
                AllowAutoRedirect = false
            });

            // Act
            var response = await client.GetAsync("/Identity/Account/Manage");

            // Assert
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.StartsWith("https://localhost/Identity/Account/Login", response.Headers.Location.ToString());
        }

        [Fact]
        public async Task CanGetAGithubUser()
        {
            // Arrange
            void ConfigureTestServices(IServiceCollection services) =>
                services.AddSingleton<IGithubClient>(new TestGithubClient());

            var client = Factory
                .WithWebHostBuilder(whb => whb.ConfigureTestServices(ConfigureTestServices))
                .CreateClient();

            // Act
            var profile = await client.GetAsync("/GithubProfile");
            Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
            var profileHtml = await HtmlHelpers.GetDocumentAsync(profile);

            var profileWithUserName = await client.SendAsync(
                (IHtmlFormElement)profileHtml.QuerySelector("#user-profile"),
                new Dictionary<string, string> { ["Input_UserName"] = "user" });

            // Assert
            Assert.Equal(HttpStatusCode.OK, profileWithUserName.StatusCode);
            var profileWithUserHtml = await HtmlHelpers.GetDocumentAsync(profileWithUserName);
            var userLogin = profileWithUserHtml.QuerySelector("#user-login");
            Assert.Equal("user", userLogin.TextContent);
        }

        public class TestGithubClient : IGithubClient
        {
            public Task<GithubUser> GetUserAsync(string userName)
            {
                if (userName == "user")
                {
                    return Task.FromResult(
                        new GithubUser
                        {
                            Login = "user",
                            Company = "Contoso Blockchain",
                            Name = "John Doe"
                        });
                }
                else
                {
                    return Task.FromResult<GithubUser>(null);
                }
            }
        }
    }
}
