using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using MediatR;
using Moq;

namespace GitHubContributors.Tests
{
    public class GetContributorsTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly string _ownerId;
        private readonly string _repoId;

        public GetContributorsTests()
        {
            _ownerId = Guid.NewGuid().ToString();
            _repoId = Guid.NewGuid().ToString();
            
            _mockMediator = new Mock<IMediator>();
        }

        [Fact]
        public async Task Passes_Parameters_To_Mediator()
        {
            var client = CreateHttpClient();
            await client.GetAsync($"/api/v1/{_ownerId}/{_repoId}/contributors/");

            _mockMediator.Verify(t =>
                t.Send(It.Is<ContributorRequest>(cr => cr.Owner.Equals(_ownerId) && cr.Repo.Equals(_repoId)),
                    It.IsAny<CancellationToken>()));
        }

        [Fact]
        public async Task Returns_OkStatus_And_Results_When_Found()
        {
            var authors = new List<string> { "Author1", "Author2", "Author3" };
            var client = CreateHttpClient();

            _mockMediator.Setup(x => x.Send(It.IsAny<ContributorRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(authors);

            var result = await client.GetAsync($"/api/v1/{_ownerId}/{_repoId}/contributors/");

            result.StatusCode.Should().Be(HttpStatusCode.OK);
            var response = await result.Content.ReadFromJsonAsync<List<string>>();

            response.Should().BeEquivalentTo(authors);
        }

        [Fact]
        public async Task Returns_NotFoundStatus_When_NothingFound()
        {
            var client = CreateHttpClient();
            var result = await client.GetAsync($"/api/v1/{_ownerId}/{_repoId}/contributors/");

            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        private HttpClient CreateHttpClient()
        {
            var application = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder => builder
                    .ConfigureServices(services =>
                    {
                        services.AddScoped(_ => _mockMediator.Object);
                    }));

            return application.CreateClient();
        }
    }
}