import Foundation
import GRDB

struct StoredRecord {
    let id: String
    let version: Int64
    let payload: Data
    let sortKey: String?
    let startsAt: Int64?
    let endsAt: Int64?
    let groupKey: String?
}

struct FeedInfo {
    let feed: String
    let rows: Int
    let bytes: Int
    let cursor: Int64
}

enum FeedOrder: String {
    case sortKey
    case startsAt
}

final class FeedStore: Sendable {
    private static let vacuumAfterDeletes = 200

    let database: DatabaseQueue
    let fileURL: URL

    init(directory: URL) throws {
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
        return migrator
    }

    func cursors(for feeds: [String]) throws -> [String: Int64] {
        try database.read { db in
            var cursors = Dictionary(uniqueKeysWithValues: feeds.map { ($0, Int64(0)) })
            for row in try Row.fetchAll(db, sql: "SELECT feed, cursor FROM cursors") {
                let feed: String = row["feed"]
                if cursors[feed] != nil { cursors[feed] = row["cursor"] }
            }
            return cursors
        }
    }

    func apply(_ batches: [FeedBatch]) throws -> Set<String> {
        var changed = Set<String>()
        var deletes = 0
        try database.write { db in
            for batch in batches {
                if batch.reset {
                    try db.execute(sql: "DELETE FROM records WHERE feed = ?", arguments: [batch.feed])
                    deletes += db.changesCount
                    changed.insert(batch.feed)
                }
                for change in batch.changes {
                    if change.deleted {
                        try db.execute(sql: "DELETE FROM records WHERE feed = ? AND id = ?", arguments: [batch.feed, change.id])
                        deletes += db.changesCount
                    } else {
                        try db.execute(sql: """
                            INSERT INTO records (feed, id, version, payload, sort_key, starts_at, ends_at, group_key)
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?)
                            ON CONFLICT (feed, id) DO UPDATE SET
                                version = excluded.version, payload = excluded.payload, sort_key = excluded.sort_key,
                                starts_at = excluded.starts_at, ends_at = excluded.ends_at, group_key = excluded.group_key
                            WHERE excluded.version > records.version
                            """, arguments: [batch.feed, change.id, change.version, change.payload,
                                             change.sortKey, change.startsAt, change.endsAt, change.groupKey])
                    }
                    changed.insert(batch.feed)
                }
                try db.execute(sql: """
                    INSERT INTO cursors (feed, cursor) VALUES (?, ?)
                    ON CONFLICT (feed) DO UPDATE SET cursor = excluded.cursor
                    """, arguments: [batch.feed, batch.cursor])
            }
        }
        if deletes >= Self.vacuumAfterDeletes { try reclaimSpace() }
        return changed
    }

    func keepOnly(feeds: [String]) throws {
        let deleted = try database.write { db -> Int in
            let placeholders = databaseQuestionMarks(count: feeds.count)
            let clause = feeds.isEmpty ? "" : "WHERE feed NOT IN (\(placeholders))"
            try db.execute(sql: "DELETE FROM records \(clause)", arguments: StatementArguments(feeds))
            let count = db.changesCount
            try db.execute(sql: "DELETE FROM cursors \(clause)", arguments: StatementArguments(feeds))
            return count
        }
        if deleted > 0 { try reclaimSpace() }
    }

    func page(feed: String, order: FeedOrder, descending: Bool, limit: Int, offset: Int, groupKey: String?) throws -> [StoredRecord] {
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

    func info() throws -> [FeedInfo] {
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

    var fileBytes: Int {
        (try? fileURL.resourceValues(forKeys: [.fileSizeKey]).fileSize) ?? 0
    }

    private func reclaimSpace() throws {
        try database.writeWithoutTransaction { db in
            try db.execute(sql: "PRAGMA incremental_vacuum")
        }
    }
}
