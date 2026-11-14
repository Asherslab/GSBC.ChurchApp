import DataLayerCore
import Foundation

final class KeptFiles: NSObject, URLSessionDownloadDelegate, @unchecked Sendable {
    static let shared = KeptFiles()

    var onChanged: (() -> Void)?
    let shelf: DownloadShelf
    private let lock = NSLock()
    private var tasks: [String: URLSessionDownloadTask] = [:]
    private var lastNotified = Date.distantPast
    private lazy var session = URLSession(configuration: .default, delegate: self, delegateQueue: nil)

    private override init() {
        let support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
        shelf = DownloadShelf(directory: support.appendingPathComponent("Kept"), indexDirectory: support.appendingPathComponent("KeptIndex"))
        super.init()
        shelf.all.filter { $0.state == .downloading }.forEach { shelf.failed($0.id) }
    }

    func keep(id: String, url: String, title: String) {
        guard let link = URL(string: url) else { return }
        if let entry = shelf.entry(id), entry.url == url, entry.state != .failed { return }
        let task = session.downloadTask(with: link)
        task.taskDescription = id
        lock.withLock {
            tasks[id]?.cancel()
            tasks[id] = task
        }
        shelf.replacing(id, url: url, title: title)
        task.resume()
        changed(force: true)
    }

    func remove(id: String) {
        lock.withLock { tasks.removeValue(forKey: id) }?.cancel()
        shelf.remove(id)
        changed(force: true)
    }

    func list() -> [[String: Any]] {
        shelf.all.map { entry in
            ["id": entry.id, "url": entry.url, "title": entry.title, "bytes": entry.bytes, "expected": entry.expected, "state": entry.state.rawValue]
        }
    }

    func read(id: String, offset: Int64, length: Int64) throws -> Data {
        try shelf.read(id, offset: offset, length: length)
    }

    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask, didWriteData bytesWritten: Int64,
                    totalBytesWritten: Int64, totalBytesExpectedToWrite: Int64) {
        guard let id = current(downloadTask) else { return }
        shelf.progressed(id, written: totalBytesWritten, expected: totalBytesExpectedToWrite)
        changed(force: false)
    }

    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask, didFinishDownloadingTo location: URL) {
        guard let id = current(downloadTask) else { return }
        let status = (downloadTask.response as? HTTPURLResponse)?.statusCode ?? 200
        if !(200..<300).contains(status) || (try? shelf.finished(id, movingFrom: location, extension: Self.fileExtension(downloadTask))) == nil {
            shelf.failed(id)
        }
        lock.withLock { if tasks[id] === downloadTask { tasks[id] = nil } }
        changed(force: true)
    }

    func urlSession(_ session: URLSession, task: URLSessionTask, didCompleteWithError error: Error?) {
        guard let error, (error as NSError).code != NSURLErrorCancelled, let id = current(task) else { return }
        lock.withLock { tasks[id] = nil }
        shelf.failed(id)
        changed(force: true)
    }

    private func current(_ task: URLSessionTask) -> String? {
        guard let id = task.taskDescription else { return nil }
        return lock.withLock { tasks[id] === task } ? id : nil
    }

    private static func fileExtension(_ task: URLSessionTask) -> String {
        let found = (task.originalRequest?.url?.pathExtension ?? "").lowercased().filter { $0.isLetter || $0.isNumber }
        return found.isEmpty ? "bin" : String(found.prefix(12))
    }

    private func changed(force: Bool) {
        let now = Date()
        let due = lock.withLock { () -> Bool in
            guard force || now.timeIntervalSince(lastNotified) >= 1 else { return false }
            lastNotified = now
            return true
        }
        guard due else { return }
        DispatchQueue.main.async { [weak self] in self?.onChanged?() }
    }
}
