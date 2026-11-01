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
}
