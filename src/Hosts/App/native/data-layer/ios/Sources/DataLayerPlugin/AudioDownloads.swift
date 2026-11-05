import DataLayerCore
import Foundation

public final class AudioDownloads: NSObject, URLSessionDownloadDelegate, @unchecked Sendable {
    public static let shared = AudioDownloads()

    var onChanged: (() -> Void)?
    public var backgroundCompletion: (() -> Void)?
    let shelf: DownloadShelf
    private var lastNotified = Date.distantPast
    private lazy var session: URLSession = {
        let configuration = URLSessionConfiguration.background(withIdentifier: (Bundle.main.bundleIdentifier ?? "gsbc") + ".audio-downloads")
        configuration.sessionSendsLaunchEvents = true
        configuration.isDiscretionary = false
        return URLSession(configuration: configuration, delegate: self, delegateQueue: nil)
    }()

    private override init() {
        let files = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0].appendingPathComponent("Sermons")
        let index = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("Downloads")
        shelf = DownloadShelf(directory: files, indexDirectory: index)
        super.init()
        _ = session
    }

    public func reconnect() { _ = session }

    func start(id: String, url: String, title: String) {
        guard let link = URL(string: url) else { return }
        if shelf.entry(id)?.state == .done { return }
        shelf.started(id, url: url, title: title)
        let task = session.downloadTask(with: link)
        task.taskDescription = id
        task.resume()
        changed(force: true)
    }

    func remove(id: String) {
        session.getAllTasks { [weak self] tasks in
            tasks.filter { $0.taskDescription == id }.forEach { $0.cancel() }
            self?.shelf.remove(id)
            self?.changed(force: true)
        }
    }

    func list() -> [[String: Any]] {
        shelf.all.map { entry in
            ["id": entry.id, "title": entry.title, "bytes": entry.bytes, "expected": entry.expected, "state": entry.state.rawValue]
        }
    }

    public func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask, didWriteData bytesWritten: Int64,
                           totalBytesWritten: Int64, totalBytesExpectedToWrite: Int64) {
        guard let id = downloadTask.taskDescription else { return }
        shelf.progressed(id, written: totalBytesWritten, expected: totalBytesExpectedToWrite)
        changed(force: false)
    }

    public func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask, didFinishDownloadingTo location: URL) {
        guard let id = downloadTask.taskDescription else { return }
        let status = (downloadTask.response as? HTTPURLResponse)?.statusCode ?? 200
        guard (200..<300).contains(status) else {
            shelf.failed(id)
            return
        }
        let fileExtension = downloadTask.originalRequest?.url?.pathExtension.lowercased() == "m4a" ? "m4a" : "mp3"
        if (try? shelf.finished(id, movingFrom: location, extension: fileExtension)) == nil { shelf.failed(id) }
        changed(force: true)
    }

    public func urlSession(_ session: URLSession, task: URLSessionTask, didCompleteWithError error: Error?) {
        guard let error, let id = task.taskDescription, (error as NSError).code != NSURLErrorCancelled else { return }
        shelf.failed(id)
        changed(force: true)
    }

    public func urlSessionDidFinishEvents(forBackgroundURLSession session: URLSession) {
        DispatchQueue.main.async { [weak self] in
            self?.backgroundCompletion?()
            self?.backgroundCompletion = nil
        }
    }

    private func changed(force: Bool) {
        let now = Date()
        guard force || now.timeIntervalSince(lastNotified) >= 1 else { return }
        lastNotified = now
        DispatchQueue.main.async { [weak self] in self?.onChanged?() }
    }
}
