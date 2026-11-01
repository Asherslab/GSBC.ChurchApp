import Foundation

struct FeedChange {
    var id = ""
    var version: Int64 = 0
    var deleted = false
    var payload = Data()
    var sortKey: String?
    var startsAt: Int64?
    var endsAt: Int64?
    var groupKey: String?
}

struct FeedBatch {
    var feed = ""
    var cursor: Int64 = 0
    var reset = false
    var changes: [FeedChange] = []
    var hasMore = false
}

struct WatchEvent {
    var batches: [FeedBatch] = []
    var isHeartbeat = false
}

enum SyncMessages {
    static let pullMethod = "/gsbc.sync.Sync/Pull"
    static let watchMethod = "/gsbc.sync.Sync/Watch"

    static func cursorsRequest(_ cursors: [String: Int64]) -> Data {
        var request = ProtoWriter()
        for (feed, cursor) in cursors.sorted(by: { $0.key < $1.key }) {
            var entry = ProtoWriter()
            entry.string(1, feed)
            entry.int64(2, cursor)
            request.bytes(1, entry.data)
        }
        return request.data
    }

    static func pullReply(_ data: Data) throws -> [FeedBatch] {
        try watchEvent(data).batches
    }

    static func watchEvent(_ data: Data) throws -> WatchEvent {
        var reader = ProtoReader(data)
        var event = WatchEvent()
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            switch field {
            case 1: event.batches.append(try batch(try reader.bytes()))
            case 2: event.isHeartbeat = try reader.bool()
            default: try reader.skip(wireType: wireType)
            }
        }
        return event
    }

    private static func batch(_ data: Data) throws -> FeedBatch {
        var reader = ProtoReader(data)
        var batch = FeedBatch()
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            switch field {
            case 1: batch.feed = try reader.string()
            case 2: batch.cursor = try reader.int64()
            case 3: batch.reset = try reader.bool()
            case 4: batch.changes.append(try change(try reader.bytes()))
            case 5: batch.hasMore = try reader.bool()
            default: try reader.skip(wireType: wireType)
            }
        }
        return batch
    }

    private static func change(_ data: Data) throws -> FeedChange {
        var reader = ProtoReader(data)
        var change = FeedChange()
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            switch field {
            case 1: change.id = try reader.string()
            case 2: change.version = try reader.int64()
            case 3: change.deleted = try reader.bool()
            case 4: change.payload = try reader.bytes()
            case 5: change.sortKey = try reader.string()
            case 6: change.startsAt = try timestampMilliseconds(try reader.bytes())
            case 7: change.endsAt = try timestampMilliseconds(try reader.bytes())
            case 8: change.groupKey = try reader.string()
            default: try reader.skip(wireType: wireType)
            }
        }
        return change
    }

    private static func timestampMilliseconds(_ data: Data) throws -> Int64 {
        var reader = ProtoReader(data)
        var seconds: Int64 = 0
        var nanos: Int64 = 0
        while !reader.isAtEnd {
            let (field, wireType) = try reader.nextField()
            switch field {
            case 1: seconds = try reader.int64()
            case 2: nanos = try reader.int64()
            default: try reader.skip(wireType: wireType)
            }
        }
        return seconds * 1000 + nanos / 1_000_000
    }
}
