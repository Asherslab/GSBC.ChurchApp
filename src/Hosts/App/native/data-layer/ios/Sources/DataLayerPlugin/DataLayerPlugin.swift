import Capacitor

@objc(DataLayerPlugin)
public class DataLayerPlugin: CAPPlugin, CAPBridgedPlugin {
    public let identifier = "DataLayerPlugin"
    public let jsName = "DataLayer"
    public let pluginMethods: [CAPPluginMethod] = [
        CAPPluginMethod(name: "unary", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "readPage", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "startSync", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "stopSync", returnType: CAPPluginReturnPromise),
        CAPPluginMethod(name: "enqueue", returnType: CAPPluginReturnPromise)
    ]

    private static let unavailable = 14

    private lazy var transport: GrpcWebTransport? = {
        let info = Bundle.main.infoDictionary
        guard let scheme = info?["GsbcGatewayScheme"] as? String, !scheme.isEmpty,
              let host = info?["GsbcGateway"] as? String, !host.isEmpty,
              let url = URL(string: "\(scheme)://\(host)") else { return nil }
        return GrpcWebTransport(gateway: url)
    }()

    @objc func unary(_ call: CAPPluginCall) {
        guard let method = call.getString("method"),
              let request = Data(base64Encoded: call.getString("request") ?? "") else {
            call.reject("unary needs a method and a base64 request")
            return
        }
        guard let transport else {
            resolve(call, GrpcResult(status: Self.unavailable, message: "This build has no gateway configured.", body: Data()))
            return
        }

        Task {
            do {
                resolve(call, try await transport.unary(method: method, message: request))
            } catch {
                resolve(call, GrpcResult(status: Self.unavailable, message: error.localizedDescription, body: Data()))
            }
        }
    }

    @objc func readPage(_ call: CAPPluginCall) { notBuiltYet(call) }
    @objc func startSync(_ call: CAPPluginCall) { notBuiltYet(call) }
    @objc func stopSync(_ call: CAPPluginCall) { notBuiltYet(call) }
    @objc func enqueue(_ call: CAPPluginCall) { notBuiltYet(call) }

    private func resolve(_ call: CAPPluginCall, _ result: GrpcResult) {
        call.resolve([
            "status": result.status,
            "message": result.message,
            "response": result.body.base64EncodedString()
        ])
    }

    private func notBuiltYet(_ call: CAPPluginCall) {
        call.unimplemented("The native data layer is the next milestone (SPEC §13).")
    }
}
