import Capacitor
import Foundation

extension DataLayerPlugin {
    static let audioMethods: [CAPPluginMethod] = [
        "audioState", "audioLoad", "audioPlay", "audioPause", "audioSeek", "audioSkip", "audioRate",
        "audioUpNext", "audioStop", "audioMark", "audioJournal",
        "audioDownload", "audioDownloads", "audioRemoveDownload",
    ].map { CAPPluginMethod(name: $0, returnType: CAPPluginReturnPromise) }

    func listenToAudio() {
        AudioPlayback.shared.onState = { [weak self] state in
            self?.notifyListeners("audioState", data: ["state": state])
        }
        AudioDownloads.shared.onChanged = { [weak self] in
            self?.notifyListeners("downloadsChanged", data: [:])
        }
    }

    @objc func audioState(_ call: CAPPluginCall) {
        onMain(call) { $0.resolve(["state": AudioPlayback.shared.state()]) }
    }

    @objc func audioLoad(_ call: CAPPluginCall) {
        guard let item = AudioItem(call.getObject("item")) else { return call.reject("audioLoad needs an item with an id and a url") }
        onMain(call) {
            AudioPlayback.shared.load(item, startAt: $0.getDouble("startAt") ?? 0, play: $0.getBool("play") ?? true)
            $0.resolve()
        }
    }

    @objc func audioPlay(_ call: CAPPluginCall) { onMain(call) { AudioPlayback.shared.play(); $0.resolve() } }

    @objc func audioPause(_ call: CAPPluginCall) { onMain(call) { AudioPlayback.shared.pause(); $0.resolve() } }

    @objc func audioSeek(_ call: CAPPluginCall) {
        onMain(call) { AudioPlayback.shared.seek(to: $0.getDouble("to") ?? 0); $0.resolve() }
    }

    @objc func audioSkip(_ call: CAPPluginCall) {
        onMain(call) { AudioPlayback.shared.skip(by: $0.getDouble("by") ?? 0); $0.resolve() }
    }

    @objc func audioRate(_ call: CAPPluginCall) {
        onMain(call) { AudioPlayback.shared.setRate($0.getDouble("rate") ?? 1); $0.resolve() }
    }

    @objc func audioUpNext(_ call: CAPPluginCall) {
        let items = (call.getArray("items") ?? []).compactMap { AudioItem($0 as? [String: Any]) }
        onMain(call) { AudioPlayback.shared.setUpNext(items); $0.resolve() }
    }

    @objc func audioStop(_ call: CAPPluginCall) { onMain(call) { AudioPlayback.shared.stop(); $0.resolve() } }

    @objc func audioMark(_ call: CAPPluginCall) {
        guard let id = call.getString("id"), let mark = call.getString("mark") else { return call.reject("audioMark needs an id and a mark") }
        let at = (call.getValue("at") as? NSNumber)?.int64Value ?? Int64(Date().timeIntervalSince1970 * 1000)
        AudioPlayback.shared.mark(id, mark, at: at)
        call.resolve()
    }

    @objc func audioJournal(_ call: CAPPluginCall) {
        let since = (call.getValue("since") as? NSNumber)?.int64Value ?? 0
        let journal = AudioPlayback.shared.journal.since(since)
        let records = journal.records.map { record -> [String: Any] in
            ["id": record.id, "position": record.position, "duration": record.duration, "heardAt": record.heardAt, "marks": record.marks, "seq": record.seq]
        }
        let json = (try? JSONSerialization.data(withJSONObject: ["records": records, "latestSeq": journal.latest]))
            .map { String(decoding: $0, as: UTF8.self) } ?? "{\"records\":[],\"latestSeq\":\(since)}"
        call.resolve(["json": json])
    }

    @objc func audioDownload(_ call: CAPPluginCall) {
        guard let id = call.getString("id"), let url = call.getString("url") else { return call.reject("audioDownload needs an id and a url") }
        AudioDownloads.shared.start(id: id, url: url, title: call.getString("title") ?? "")
        call.resolve()
    }

    @objc func audioDownloads(_ call: CAPPluginCall) {
        let json = (try? JSONSerialization.data(withJSONObject: AudioDownloads.shared.list())).map { String(decoding: $0, as: UTF8.self) } ?? "[]"
        call.resolve(["json": json])
    }

    @objc func audioRemoveDownload(_ call: CAPPluginCall) {
        guard let id = call.getString("id") else { return call.reject("audioRemoveDownload needs an id") }
        AudioDownloads.shared.remove(id: id)
        call.resolve()
    }

    private func onMain(_ call: CAPPluginCall, _ work: @escaping (CAPPluginCall) -> Void) {
        DispatchQueue.main.async { work(call) }
    }
}
