using GSBC.ChurchApp.Platform.Contracts.Sync;
using ProtoBuf;

namespace GSBC.ChurchApp.Platform.Contracts.Tests;

public sealed class SyncWireFormatTests
{
    public const string WatchEventHex =
        "0a4e0a066576656e7473102a22340a026531100322030102032a0a323032362d31312d30383206089882c1d7063a0b08b0acc1d7061080e59a77420673756e646179220a0a02653010041801220028010a0b0a076e6f74696365731801";

    [Fact]
    public void AWatchEventEncodesToTheBytesTheNativeLayerDecodes()
    {
        WatchEvent watchEvent = new()
        {
            Feeds =
            [
                new FeedBatch
                {
                    Feed = "events",
                    Cursor = 42,
                    HasMore = true,
                    Changes =
                    [
                        new FeedChange
                        {
                            Id = "e1",
                            Version = 3,
                            Payload = [1, 2, 3],
                            SortKey = "2026-11-08",
                            StartsAt = new DateTime(2026, 11, 8, 9, 30, 0, DateTimeKind.Utc),
                            EndsAt = new DateTime(2026, 11, 8, 11, 0, 0, 250, DateTimeKind.Utc),
                            GroupKey = "sunday",
                        },
                        new FeedChange { Id = "e0", Version = 4, Deleted = true },
                    ],
                },
                new FeedBatch { Feed = "notices", Reset = true },
            ],
        };

        using MemoryStream bytes = new();
        Serializer.Serialize(bytes, watchEvent);

        Assert.Equal(WatchEventHex, Convert.ToHexStringLower(bytes.ToArray()));
    }

    [Fact]
    public void TheOriginTravelsAtTheNumbersTheNativeLayerUses()
    {
        Assert.Equal("0a0a0a086465762e6563686f1207756e6b6e6f776e", Hex(new WatchRequest { Cursors = [new FeedCursor { Feed = "dev.echo" }], Origin = "unknown" }));
        Assert.Equal("0a0a0a066576656e747318011a026f31", Hex(new PullReply { Feeds = [new FeedBatch { Feed = "events", Reset = true }], Origin = "o1" }));
    }

    private static string Hex<T>(T message)
    {
        using MemoryStream bytes = new();
        Serializer.Serialize(bytes, message);
        return Convert.ToHexStringLower(bytes.ToArray());
    }
}
