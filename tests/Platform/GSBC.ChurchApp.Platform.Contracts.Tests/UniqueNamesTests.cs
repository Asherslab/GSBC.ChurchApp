using GSBC.ChurchApp.Platform.Contracts.Configuration;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class UniqueNamesTests
{
    private static readonly Surface Saved = HubSurfaces.Saved.Surface;

    [Fact]
    public void ANewNameIsOfferedByNumberingTheTakenOne()
    {
        Assert.Equal("Ministries 3", UniqueNames.Free(" Ministries ", ["ministries", "Ministries 2", "Youth"]));
        Assert.Equal("Camp", UniqueNames.Free("Camp", ["Ministries"]));
    }

    [Fact]
    public void OnlyANameThisChangeAddsOrRenamesCanClashSoOldDuplicatesStillEdit()
    {
        StoredPlacement[] library = [Named("a", "Ministries"), Named("b", "ministries "), Named("c", "Youth")];

        Assert.Empty(UniqueNames.Clashes(Saved, library, ["c"]));
        Assert.Equal(["Ministries"], UniqueNames.Clashes(Saved, library, ["a"]));
        Assert.Empty(UniqueNames.Clashes(HubSurfaces.Icon.Surface, library, ["a"]));
    }

    private static StoredPlacement Named(string id, string name) =>
        new(id, Saved.Key, 0, false, "", null, null, new Dictionary<string, string> { [HubSurfaces.Saved.Name.Key] = name }, 1);
}
