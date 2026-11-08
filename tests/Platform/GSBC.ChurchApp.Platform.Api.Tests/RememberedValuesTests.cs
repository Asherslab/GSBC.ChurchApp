using GSBC.ChurchApp.Platform.Client.Access;
using GSBC.ChurchApp.Platform.Client.Sessions;
using GSBC.ChurchApp.Platform.Client.Signals;
using GSBC.ChurchApp.Platform.Permissions;
using Microsoft.Extensions.DependencyInjection;

namespace GSBC.ChurchApp.Platform.Api.Tests;

public sealed class RememberedValuesTests
{
    private const string Key = "events.diary";

    private readonly SwitchableAccess _access = new();
    private readonly SessionSignal _session = new();
    private readonly RememberedValues _remembered;

    public RememberedValuesTests()
    {
        ClientSignals signals = new(new ServiceCollection().BuildServiceProvider(), _access, _session);
        _remembered = new RememberedValues(signals);
        _remembered.Keep(Key, "the last person's calendar");
    }

    [Fact]
    public async Task WhatAScreenLastShowedIsForgottenWhenSomeoneSignsInOrOut()
    {
        await _session.RaiseAsync();

        Assert.False(_remembered.TryGet(Key, out string _));
    }

    [Fact]
    public void WhatAScreenLastShowedIsForgottenWhenAccessChanges()
    {
        _access.Change();

        Assert.False(_remembered.TryGet(Key, out string _));
    }

    private sealed class SwitchableAccess : IAccess
    {
        public event Action? Changed;

        public bool SignedIn => false;

        public bool Can(Permission permission, string? objectId = null) => false;

        public bool CanSomewhere(Permission permission) => false;

        public void Change() => Changed?.Invoke();
    }
}
