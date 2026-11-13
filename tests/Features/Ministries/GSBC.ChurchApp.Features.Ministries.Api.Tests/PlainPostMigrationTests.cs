using GSBC.ChurchApp.Features.Ministries.Api.Data;
using GSBC.ChurchApp.Platform.Contracts.Content;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Ministries.Api.Tests;

[Integration("Posts written before rich text keep reading exactly as they were written once the migration turns them into the new format")]
public sealed class PlainPostMigrationTests : IAsyncLifetime
{
    private const string BeforeRichPosts = "20261112141000_HiddenMinistries";

    private TestDatabase _database = null!;
    private ServiceProvider _services = null!;

    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync()
    {
        _database = await TestDatabase.CreateAsync();
        _services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", _database.ConnectionString)])
                .Build())
            .AddMinistriesData()
            .BuildServiceProvider();
    }

    [Theory]
    [InlineData("Pick up by 8:30pm. *Not* 9.")]
    [InlineData("## Not a heading\n- not a list\n1. not numbered either\n> not a quote")]
    [InlineData("Bring [a plate](to share) and $5 for the kitty_fund \\ thanks!")]
    [InlineData("![not a picture](media:abc)\nSee you at 7 :)")]
    [InlineData("")]
    public async Task APlainPostReadsTheSameAfterTheMigration(string body)
    {
        await using AsyncServiceScope scope = _services.CreateAsyncScope();
        MinistriesDbContext db = scope.ServiceProvider.GetRequiredService<MinistriesDbContext>();
        IMigrator migrator = db.GetService<IMigrator>();
        await migrator.MigrateAsync(BeforeRichPosts, cancellationToken: Cancel);
        Guid id = Guid.CreateVersion7();
        await db.Database.ExecuteSqlAsync($"""
            INSERT INTO ministries.posts (id, ministry_id, title, body, audience, important, author_id, author_name, posted_at, no_class)
            VALUES ({id}, {Guid.CreateVersion7()}, 'Sunday', {body}, 'everyone', false, {Guid.Empty}, '', now(), false)
            """, Cancel);

        await migrator.MigrateAsync(cancellationToken: Cancel);

        PostRecord post = await db.Posts.AsNoTracking().SingleAsync(found => found.Id == id, Cancel);
        RichText content = MinistryViews.ContentOf(post);
        RichTextDocument document = RichTextParser.Parse(content.Markdown);
        Assert.All(document.Blocks, block => Assert.IsType<ParagraphBlock>(block));
        Assert.Empty(document.Problems);
        Assert.Equal(body.Replace('\n', ' '), PlainText.Of(content));
        Assert.Equal(PlainText.Of(PlainText.AsRichText(body)), PlainText.Of(content));
    }

    public async ValueTask DisposeAsync()
    {
        await _services.DisposeAsync();
        await _database.DisposeAsync();
    }
}
