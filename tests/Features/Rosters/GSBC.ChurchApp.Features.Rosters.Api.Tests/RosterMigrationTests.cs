using GSBC.ChurchApp.Features.Rosters.Api.Data;
using GSBC.ChurchApp.Features.Rosters.Contracts;
using GSBC.ChurchApp.Platform.Data;
using GSBC.ChurchApp.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Features.Rosters.Api.Tests;

[Integration("The migration that turns a ministry's list of swap ways into one rule rewrites its settings in SQL")]
public sealed class RosterMigrationTests
{
    private static CancellationToken Cancel => TestContext.Current.CancellationToken;

    [Fact]
    public async Task ARosterThatHadALeaderConfirmingKeepsItAndNobodyChoosesIt()
    {
        await using TestDatabase database = await TestDatabase.CreateAsync();
        await using ServiceProvider services = new ServiceCollection()
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
                .AddInMemoryCollection([new($"ConnectionStrings:{Database.ConnectionStringName}", database.ConnectionString)])
                .Build())
            .AddLogging()
            .AddRostersData()
            .BuildServiceProvider();
        Guid both = Guid.CreateVersion7(), confirmed = Guid.CreateVersion7(), direct = Guid.CreateVersion7(), none = Guid.CreateVersion7();

        await using (AsyncServiceScope scope = services.CreateAsyncScope())
        {
            RostersDbContext db = scope.ServiceProvider.GetRequiredService<RostersDbContext>();
            await db.GetService<IMigrator>().MigrateAsync("20261110143021_Rosters", cancellationToken: Cancel);
            await db.Database.ExecuteSqlAsync($"""
                INSERT INTO rosters.settings (ministry_id, replies, swap_ways, who_sees, remind_days_before, nudge_after_days, updated_at) VALUES
                    ({both}, true, ARRAY['team', 'approve'], 'all', 2, 3, now()),
                    ({confirmed}, true, ARRAY['approve', 'direct'], 'all', 2, 3, now()),
                    ({direct}, true, ARRAY['direct'], 'all', 2, 3, now()),
                    ({none}, true, ARRAY[]::text[], 'all', 2, 3, now())
                """, Cancel);
            await db.Database.MigrateAsync(Cancel);
        }

        await using AsyncServiceScope after = services.CreateAsyncScope();
        Dictionary<Guid, (string, bool)> rules = await after.ServiceProvider.GetRequiredService<RostersDbContext>().Settings
            .ToDictionaryAsync(settings => settings.MinistryId, settings => (settings.OpenSwaps, settings.DirectSwaps), Cancel);
        Assert.Equal((OpenSwaps.Approve, false), rules[both]);
        Assert.Equal((OpenSwaps.Approve, true), rules[confirmed]);
        Assert.Equal((OpenSwaps.Off, true), rules[direct]);
        Assert.Equal((OpenSwaps.Off, false), rules[none]);
    }
}
