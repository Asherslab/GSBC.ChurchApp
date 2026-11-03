import XCTest
@testable import DataLayerCore

final class CrashSummaryTests: XCTestCase {
    func testTheCrashedThreadsFramesAreReadInnermostFirst() throws {
        let tree = Data("""
            {"callStackPerThread": true, "callStacks": [
              {"threadAttributed": false, "callStackRootFrames": [{"binaryName": "libsystem_kernel.dylib", "offsetIntoBinaryTextSegment": 1}]},
              {"threadAttributed": true, "callStackRootFrames": [
                {"binaryName": "GSBC Dev", "offsetIntoBinaryTextSegment": 4242, "subFrames": [
                  {"binaryName": "DataLayerCore", "offsetIntoBinaryTextSegment": 77, "subFrames": [
                    {"binaryName": "UIKitCore", "offsetIntoBinaryTextSegment": 9}]}]}]}
            ]}
            """.utf8)

        let crash = CrashSummary.from(callStackTree: tree, exceptionType: 1, signal: 11, exceptionName: nil,
                                      crashedAt: Date(timeIntervalSince1970: 1_000), appVersion: "1.0", appBuild: "42")

        XCTAssertEqual(crash.type, "EXC_BAD_ACCESS (SIGSEGV)")
        XCTAssertEqual(crash.frames, ["GSBC Dev +4242", "DataLayerCore +77", "UIKitCore +9"])
        XCTAssertEqual(crash.crashedAt, 1_000_000)
    }
}
