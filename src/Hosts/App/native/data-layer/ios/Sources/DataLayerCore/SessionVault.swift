import Foundation

public protocol SessionVault: Sendable {
    func read() -> String?
    func write(_ secret: String?)
}
