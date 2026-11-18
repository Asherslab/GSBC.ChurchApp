import Capacitor
import Foundation

extension DataLayerPlugin {
    static let fileMethods: [CAPPluginMethod] = [
        "fileKeep", "keptFiles", "fileRemove", "fileRead",
    ].map { CAPPluginMethod(name: $0, returnType: CAPPluginReturnPromise) }

    func listenToKeptFiles() {
        KeptFiles.shared.onChanged = { [weak self] in
            self?.notifyListeners("keptFilesChanged", data: [:])
        }
    }

    @objc func fileKeep(_ call: CAPPluginCall) {
        guard let id = call.getString("id"), let url = call.getString("url") else { return call.reject("fileKeep needs an id and a url") }
        let title = call.getString("title").flatMap { $0.isEmpty ? nil : $0 } ?? id
        KeptFiles.shared.keep(id: id, url: url, title: title)
        call.resolve()
    }

    @objc func keptFiles(_ call: CAPPluginCall) {
        let json = (try? JSONSerialization.data(withJSONObject: KeptFiles.shared.list())).map { String(decoding: $0, as: UTF8.self) } ?? "[]"
        call.resolve(["json": json])
    }

    @objc func fileRemove(_ call: CAPPluginCall) {
        guard let id = call.getString("id") else { return call.reject("fileRemove needs an id") }
        KeptFiles.shared.remove(id: id)
        call.resolve()
    }

    @objc func fileRead(_ call: CAPPluginCall) {
        guard let id = call.getString("id") else { return call.reject("fileRead needs an id") }
        let offset = (call.getValue("offset") as? NSNumber)?.int64Value ?? 0
        let length = (call.getValue("length") as? NSNumber)?.int64Value ?? -1
        DispatchQueue.global(qos: .userInitiated).async {
            do {
                let data = try KeptFiles.shared.read(id: id, offset: offset, length: length)
                call.resolve(["base64": data.base64EncodedString()])
            } catch {
                call.reject("That file isn't kept on this phone.")
            }
        }
    }
}
