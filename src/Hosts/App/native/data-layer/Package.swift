// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "GsbcDataLayer",
    platforms: [.iOS(.v15)],
    products: [
        .library(name: "GsbcDataLayer", targets: ["DataLayerPlugin"])
    ],
    dependencies: [
        .package(url: "https://github.com/ionic-team/capacitor-swift-pm.git", from: "8.0.0"),
        .package(url: "https://github.com/groue/GRDB.swift.git", from: "7.11.0")
    ],
    targets: [
        .target(
            name: "DataLayerCore",
            dependencies: [.product(name: "GRDB", package: "GRDB.swift")],
            path: "ios/Sources/DataLayerCore"),
        .target(
            name: "DataLayerPlugin",
            dependencies: [
                "DataLayerCore",
                .product(name: "Capacitor", package: "capacitor-swift-pm"),
                .product(name: "Cordova", package: "capacitor-swift-pm")
            ],
            path: "ios/Sources/DataLayerPlugin"),
        .testTarget(
            name: "DataLayerCoreTests",
            dependencies: ["DataLayerCore", .product(name: "GRDB", package: "GRDB.swift")],
            path: "ios/Tests/DataLayerCoreTests")
    ]
)
