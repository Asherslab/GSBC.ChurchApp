import Foundation
import GRDB
import os

public struct StoredRecord {
    public let id: String
    public let version: Int64
    public let payload: Data
    public let sortKey: String?
    public let startsAt: Int64?
    public let endsAt: Int64?
    public let groupKey: String?
}

public struct FeedInfo {
    public let feed: String
    public let rows: Int
    public let bytes: Int
    public let cursor: Int64
}

public struct FeedLimits: Sendable {
    public static let standard = FeedLimits(maximumRows: 5_000, maximumBytes: 20 * 1024 * 1024)

    public let maximumRows: Int
    public let maximumBytes: Int

    public init(maximumRows: Int, maximumBytes: Int) {
        self.maximumRows = maximumRows
        self.maximumBytes = maximumBytes
    }
}

public enum FeedOrder: String {
    case sortKey
    case startsAt
}

public final class FeedStore: Sendable {
    private static let reclaimAtFreePages = 64

    private static let log = Logger(subsystem: "au.com.baptist.app", category: "FeedStore")

    public let database: DatabaseQueue
    public let fileURL: URL
    private let limits: FeedLimits

    public init(directory: URL, limits: FeedLimits = .standard) throws {
        self.limits = limits
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        var excluded = directory
        var values = URLResourceValues()
        values.isExcludedFromBackup = true
        try? excluded.setResourceValues(values)

        fileURL = directory.appendingPathComponent("store.sqlite")
        var configuration = Configuration()
        configuration.prepareDatabase { db in
            try db.execute(sql: "PRAGMA auto_vacuum = INCREMENTAL")
        }
        database = try DatabaseQueue(path: fileURL.path, configuration: configuration)
        try Self.migrator.migrate(database)
    }

    public func copy(to url: URL) throws {
        let copy = try DatabaseQueue(path: url.path)
        try database.backup(to: copy)
        try copy.close()
    }

    private static var migrator: DatabaseMigrator {
        var migrator = DatabaseMigrator()
        migrator.registerMigration("v1") { db in
            try db.execute(sql: """
                CREATE TABLE records (
                    feed TEXT NOT NULL,
                    id TEXT NOT NULL,
                    version INTEGER NOT NULL,
                    payload BLOB NOT NULL,
                    sort_key TEXT,
                    starts_at INTEGER,
                    ends_at INTEGER,
                    group_key TEXT,
                    PRIMARY KEY (feed, id)
                ) WITHOUT ROWID;
                CREATE INDEX records_by_sort_key ON records (feed, sort_key);
                CREATE INDEX records_by_starts_at ON records (feed, starts_at);
                CREATE INDEX records_by_group ON records (feed, group_key, sort_key);
                CREATE TABLE cursors (
                    feed TEXT PRIMARY KEY NOT NULL,
                    cursor INTEGER NOT NULL
                );
                """)
        }
        migrator.registerMigration("v2-outbox") { db in
            try db.execute(sql: """
                CREATE TABLE outbox (
                    id TEXT PRIMARY KEY NOT NULL,
                    method TEXT NOT NULL,
                    request BLOB NOT NULL,
                    created_at INTEGER NOT NULL,
                    attempts INTEGER NOT NULL DEFAULT 0,
                    next_attempt_at INTEGER NOT NULL,
                    last_error TEXT,
                    failed INTEGER NOT NULL DEFAULT 0
                );
                CREATE INDEX outbox_due ON outbox (failed, next_attempt_at);
                """)
        }
        migrator.registerMigration("v3-stored-cursor") { db in
            try db.execute(sql: """
                ALTER TABLE records ADD COLUMN stored_cursor INTEGER NOT NULL DEFAULT 0;
                CREATE INDEX records_by_stored_cursor ON records (feed, stored_cursor);
                """)
        }
        migrator.registerMigration("v4-quiet-outbox") { db in
            try db.execute(sql: "ALTER TABLE outbox ADD COLUMN quiet INTEGER NOT NULL DEFAULT 0")
        }
        migrator.registerMigration("v5-origin") { db in
            try db.execute(sql: "CREATE TABLE origin (only INTEGER PRIMARY KEY CHECK (only = 1), origin TEXT NOT NULL)")
        }
        migrator.registerMigration("v6-uploads") { db in
            try db.execute(sql: """
                ALTER TABLE outbox ADD COLUMN kind TEXT NOT NULL DEFAULT 'call';
                CREATE TABLE uploads (
                    id TEXT PRIMARY KEY NOT NULL,
                    file TEXT NOT NULL,
                    file_name TEXT NOT NULL,
                    content_type TEXT NOT NULL,
                    bytes INTEGER NOT NULL,
                    find_method TEXT NOT NULL,
                    find_request BLOB NOT NULL,
                    begin_method TEXT NOT NULL,
                    begin_request BLOB NOT NULL,
                    complete_method TEXT NOT NULL,
                    complete_request BLOB NOT NULL,
                    stage TEXT NOT NULL,
                    upload_id TEXT,
                    put_url TEXT,
                    put_content_type TEXT,
                    put_expires_at INTEGER,
                    media_id TEXT
                );
                CREATE INDEX uploads_by_file ON uploads (file);
                CREATE TABLE outbox_waits (
                    entry_id TEXT NOT NULL,
                    upload_id TEXT NOT NULL,
                    path TEXT NOT NULL,
                    done INTEGER NOT NULL DEFAULT 0,
                    PRIMARY KEY (entry_id, upload_id, path)
                );
                CREATE INDEX outbox_waits_by_upload ON outbox_waits (upload_id);
                """)
        }
        return migrator
    }

    public func cursors(for feeds: [String]) throws -> [String: Int64] {
        try database.read { db in
            var cursors = Dictionary(uniqueKeysWithValues: feeds.map { ($0, Int64(0)) })
            for row in try Row.fetchAll(db, sql: "SELECT feed, cursor FROM cursors") {
                let feed: String = row["feed"]
                if cursors[feed] != nil { cursors[feed] = row["cursor"] }
            }
            return cursors
        }
    }

    public func origin() throws -> String? {
        try database.read { db in try String.fetchOne(db, sql: "SELECT origin FROM origin") }
    }

    public func apply(_ batches: [FeedBatch], origin: String = "") throws -> Set<String> {
        let newOrigin = try origin.isEmpty || origin == self.origin() ? nil : origin
        guard !batches.isEmpty || newOrigin != nil else { return [] }
        var changed = Set<String>()
        try database.write { db in
            if let newOrigin {
                try db.execute(sql: "INSERT INTO origin (only, origin) VALUES (1, ?) ON CONFLICT (only) DO UPDATE SET origin = excluded.origin",
                               arguments: [newOrigin])
            }
            for batch in batches {
                if batch.reset {
                    try db.execute(sql: "DELETE FROM records WHERE feed = ?", arguments: [batch.feed])
                    changed.insert(batch.feed)
                }
                for change in batch.changes {
                    if change.deleted {
                        try db.execute(sql: "DELETE FROM records WHERE feed = ? AND id = ?", arguments: [batch.feed, change.id])
                    } else {
                        try db.execute(sql: """
                            INSERT INTO records (feed, id, version, payload, sort_key, starts_at, ends_at, group_key, stored_cursor)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
                            ON CONFLICT (feed, id) DO UPDATE SET
                                version = excluded.version, payload = excluded.payload, sort_key = excluded.sort_key,
                                starts_at = excluded.starts_at, ends_at = excluded.ends_at, group_key = excluded.group_key,
                                stored_cursor = excluded.stored_cursor
                            WHERE excluded.version > records.version
                            """, arguments: [batch.feed, change.id, change.version, change.payload,
                                             change.sortKey, change.startsAt, change.endsAt, change.groupKey, batch.cursor])
                    }
                    changed.insert(batch.feed)
                }
                try db.execute(sql: """
                    INSERT INTO cursors (feed, cursor) VALUES (?, ?)
                    ON CONFLICT (feed) DO UPDATE SET cursor = excluded.cursor
                    """, arguments: [batch.feed, batch.cursor])
            }
            for feed in changed {
                try enforceLimits(db, feed: feed)
            }
        }
        try reclaimSpaceIfWorthIt()
        return changed
    }

    private func enforceLimits(_ db: Database, feed: String) throws {
        let rows = try Int.fetchOne(db, sql: "SELECT COUNT(*) FROM records WHERE feed = ?", arguments: [feed]) ?? 0
        let bytes = try Int.fetchOne(db, sql: "SELECT COALESCE(SUM(LENGTH(payload)), 0) FROM records WHERE feed = ?", arguments: [feed]) ?? 0
        guard rows > limits.maximumRows || bytes > limits.maximumBytes else { return }

        Self.log.warning("Feed \(feed, privacy: .public) is over its limits (\(rows) rows, \(bytes) bytes); dropping the oldest. Its server-side window is too wide.")
        var remainingRows = rows
        var remainingBytes = bytes
        for row in try Row.fetchAll(db, sql: "SELECT id, LENGTH(payload) AS size FROM records WHERE feed = ? ORDER BY stored_cursor, id", arguments: [feed]) {
            guard remainingRows > limits.maximumRows || remainingBytes > limits.maximumBytes else { break }
            let id: String = row["id"]
            let size: Int = row["size"]
            try db.execute(sql: "DELETE FROM records WHERE feed = ? AND id = ?", arguments: [feed, id])
            remainingRows -= 1
            remainingBytes -= size
        }
    }

    public func register(feeds: [String]) throws {
        try database.write { db in
            let placeholders = databaseQuestionMarks(count: feeds.count)
            let clause = feeds.isEmpty ? "" : "WHERE feed NOT IN (\(placeholders))"
            try db.execute(sql: "DELETE FROM records \(clause)", arguments: StatementArguments(feeds))
            try db.execute(sql: "DELETE FROM cursors \(clause)", arguments: StatementArguments(feeds))
            for feed in feeds {
                try db.execute(sql: "INSERT INTO cursors (feed, cursor) VALUES (?, 0) ON CONFLICT (feed) DO NOTHING", arguments: [feed])
            }
        }
        try reclaimSpaceIfWorthIt()
    }

    public func clear(feeds: [String]) throws -> Set<String> {
        guard !feeds.isEmpty else { return [] }
        let placeholders = databaseQuestionMarks(count: feeds.count)
        try database.write { db in
            try db.execute(sql: "DELETE FROM records WHERE feed IN (\(placeholders))", arguments: StatementArguments(feeds))
            try db.execute(sql: "UPDATE cursors SET cursor = 0 WHERE feed IN (\(placeholders))", arguments: StatementArguments(feeds))
        }
        try reclaimSpaceIfWorthIt()
        return Set(feeds)
    }

    public func registeredFeeds() throws -> [String] {
        try database.read { db in try String.fetchAll(db, sql: "SELECT feed FROM cursors ORDER BY feed") }
    }

    public func page(feed: String, order: FeedOrder, descending: Bool, limit: Int, offset: Int, groupKey: String?) throws -> [StoredRecord] {
        let column = order == .sortKey ? "sort_key" : "starts_at"
        let direction = descending ? "DESC" : "ASC"
        let groupFilter = groupKey == nil ? "" : "AND group_key = ?"
        var arguments: StatementArguments = [feed]
        if let groupKey { arguments += [groupKey] }
        arguments += [limit, offset]
        return try database.read { db in
            try Row.fetchAll(db, sql: """
                SELECT id, version, payload, sort_key, starts_at, ends_at, group_key FROM records
                WHERE feed = ? \(groupFilter)
                ORDER BY \(column) \(direction), id
                LIMIT ? OFFSET ?
                """, arguments: arguments).map { row in
                StoredRecord(id: row["id"], version: row["version"], payload: row["payload"],
                             sortKey: row["sort_key"], startsAt: row["starts_at"], endsAt: row["ends_at"], groupKey: row["group_key"])
            }
        }
    }

    public func info() throws -> [FeedInfo] {
        try database.read { db in
            try Row.fetchAll(db, sql: """
                SELECT c.feed, c.cursor, COUNT(r.id) AS rows, COALESCE(SUM(LENGTH(r.payload)), 0) AS bytes
                FROM cursors c LEFT JOIN records r ON r.feed = c.feed
                GROUP BY c.feed ORDER BY c.feed
                """).map { row in
                FeedInfo(feed: row["feed"], rows: row["rows"], bytes: row["bytes"], cursor: row["cursor"])
            }
        }
    }

    public var fileBytes: Int {
        ((try? FileManager.default.attributesOfItem(atPath: fileURL.path))?[.size] as? NSNumber)?.intValue ?? 0
    }

    private func reclaimSpaceIfWorthIt() throws {
        try database.writeWithoutTransaction { db in
            let freePages = try Int.fetchOne(db, sql: "PRAGMA freelist_count") ?? 0
            if freePages >= Self.reclaimAtFreePages {
                try db.execute(sql: "PRAGMA incremental_vacuum")
            }
        }
    }
}
