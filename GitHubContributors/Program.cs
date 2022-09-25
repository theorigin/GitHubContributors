using System.Collections.Immutable;
using MediatR;
using Octokit;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddAWSLambdaHosting(LambdaEventSource.HttpApi);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMediatR(Assembly.GetExecutingAssembly());
builder.Services.AddScoped<IGitHubClient>(_ => new GitHubClient(new ProductHeaderValue("GitHubContributors")));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.MapGet("/api/v1/{owner}/{repo}/contributors", async (string owner, string repo, IMediator mediator) =>
    {
        var result = await mediator.Send(new ContributorRequest(owner, repo));

        return result != null 
            ? Results.Ok(result) 
            : Results.NotFound();
    }
).WithName("GetContributors");

app.Run();

public record ContributorRequest(string Owner, string Repo, int RequiredCount = 30) : IRequest<IReadOnlyList<string>?>;

public class ContributorRequestHandler : IRequestHandler<ContributorRequest, IReadOnlyList<string>?>
{
    private readonly IGitHubClient _gitHubClient;

    public ContributorRequestHandler(IGitHubClient gitHubClient)
    {
        _gitHubClient = gitHubClient;
    }

    public async Task<IReadOnlyList<string>?> Handle(ContributorRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var mostRecentCommits = await _gitHubClient.Repository.Commit.GetAll(request.Owner, request.Repo,
                new ApiOptions { PageCount = 1, PageSize = request.RequiredCount });

            return mostRecentCommits
                .Select(x => x.Commit.Author.Name)
                .ToImmutableList();
        }
        catch (Exception ex) when (ex is NotFoundException or ApiException)
        {
            return null;
        }
    }
}

public partial class Program { }