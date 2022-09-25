using System.Net;
using FluentAssertions;
using Moq;
using Octokit;
using Xunit;

namespace GitHubContributors.Tests
{
    public class ContributorRequestHandlerTests
    {
        private readonly Mock<IGitHubClient> _mockGitHubClient;

        public ContributorRequestHandlerTests()
        {
            _mockGitHubClient = new Mock<IGitHubClient>();
        }

        [Fact]
        public async Task Passes_Correct_Parameters()
        {
            var ownerId = Guid.NewGuid().ToString();
            var repoId = Guid.NewGuid().ToString();
            var pageSize = 99;

            _mockGitHubClient.Setup(x => x.Repository.Commit.GetAll(ownerId, repoId, It.IsAny<ApiOptions>()))
                .ReturnsAsync(CreateGitHubCommits(3));

            var sut = new ContributorRequestHandler(_mockGitHubClient.Object);
            var _ = await sut.Handle(new ContributorRequest(ownerId, repoId, pageSize), CancellationToken.None);

            _mockGitHubClient.Verify(t =>
                t.Repository.Commit.GetAll(It.Is<string>(x => x == ownerId),
                    It.Is<string>(x => x == repoId),
                    It.Is<ApiOptions>(x => x.PageSize.Equals(pageSize) && x.PageCount.Equals(1))));
        }

        [Fact]
        public async Task Returns_Authors()
        {
            _mockGitHubClient
                .Setup(x => x.Repository.Commit.GetAll(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiOptions>()))
                .ReturnsAsync(CreateGitHubCommits(3));

            var sut = new ContributorRequestHandler(_mockGitHubClient.Object);
            var result = await sut.Handle(new ContributorRequest("owner", "repo"), CancellationToken.None);

            result.Should().BeEquivalentTo(new List<string> { "author 1", "author 2", "author 3" });
        }

        [Fact]
        public async Task Returns_Null_When_NotFound()
        {
            _mockGitHubClient
                .Setup(x => x.Repository.Commit.GetAll(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiOptions>()))
                .Throws(new NotFoundException("Not Found", HttpStatusCode.NotFound) );

            var sut = new ContributorRequestHandler(_mockGitHubClient.Object);

            var result = await sut.Handle(new ContributorRequest("owner", "repo"), CancellationToken.None);
            result.Should().BeNull();
        }

        [Fact]
        public async Task Returns_Null_When_ApiException()
        {
            _mockGitHubClient
                .Setup(x => x.Repository.Commit.GetAll(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<ApiOptions>()))
                .Throws(new ApiException("Not Found", HttpStatusCode.NotFound));

            var sut = new ContributorRequestHandler(_mockGitHubClient.Object);

            var result = await sut.Handle(new ContributorRequest("owner", "repo"), CancellationToken.None);
            result.Should().BeNull();
        }
                private static List<GitHubCommit> CreateGitHubCommits(int numberRequired)
        {
            var x = new List<GitHubCommit>();
            for (var i = 1; i <= numberRequired; i++)
            {
                var commit = new Commit(null, null, null, null, null, null, null, null,
                    new Committer($"author {i}", $"test_{i}@test.com", DateTimeOffset.UtcNow), null, null, new List<GitReference>(), 0, null);

                x.Add(new GitHubCommit(null, null, null, null, null, null, null, null, null, commit, null, null, null, null,
                    null));
            }

            return x;
        }
    }
}