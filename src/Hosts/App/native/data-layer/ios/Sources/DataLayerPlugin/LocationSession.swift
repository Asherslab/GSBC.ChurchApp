import CoreLocation
import DataLayerCore
import Foundation

final class LocationSession: NSObject, CLLocationManagerDelegate {
    static let shared = LocationSession()

    var onFix: ((Int64) -> Void)?
    let journal: LocationJournal
    private let manager = CLLocationManager()
    private let defaults = UserDefaults.standard
    private let runningKey = "gsbc.location.running"
    private let purposeKey = "gsbc.location.purpose"
    private var waitingForAnswer: [(String) -> Void] = []
    private var lastNotified = Date.distantPast
    private var notifyScheduled = false

    private override init() {
        let directory = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("Location")
        journal = LocationJournal(directory: directory)
        super.init()
        manager.delegate = self
        manager.activityType = .fitness
        manager.desiredAccuracy = kCLLocationAccuracyBest
        manager.distanceFilter = 3
        manager.pausesLocationUpdatesAutomatically = false
    }

    var running: Bool { defaults.bool(forKey: runningKey) }

    func resumeIfRunning() {
        guard running else { return }
        servicesEnabled { [weak self] enabled in
            guard let self else { return }
            if enabled && self.authorized { self.begin() } else { self.end() }
        }
    }

    func start(purpose: String, answer: @escaping (String) -> Void) {
        servicesEnabled { [weak self] enabled in
            guard let self else { return }
            guard enabled else { return answer("off") }
            switch self.manager.authorizationStatus {
            case .notDetermined:
                self.waitingForAnswer.append { state in
                    if state == "running" { self.defaults.set(purpose, forKey: self.purposeKey) }
                    answer(state)
                }
                if self.waitingForAnswer.count == 1 { self.manager.requestWhenInUseAuthorization() }
            case .denied, .restricted:
                answer("denied")
            default:
                self.defaults.set(purpose, forKey: self.purposeKey)
                self.begin()
                answer("running")
            }
        }
    }

    func stop() { end() }

    func state(_ answer: @escaping ([String: Any]) -> Void) {
        servicesEnabled { [weak self] enabled in
            guard let self else { return }
            answer(["running": self.running, "authorization": enabled ? self.authorization : "off"])
        }
    }

    func locationManagerDidChangeAuthorization(_ manager: CLLocationManager) {
        let status = manager.authorizationStatus
        if status == .notDetermined { return }
        if !authorized && running { end() }
        let waiting = waitingForAnswer
        waitingForAnswer = []
        guard !waiting.isEmpty else { return }
        if authorized { begin() }
        waiting.forEach { $0(authorized ? "running" : "denied") }
    }

    func locationManager(_ manager: CLLocationManager, didUpdateLocations locations: [CLLocation]) {
        guard running else { return }
        var latest: Int64?
        for location in locations where location.horizontalAccuracy >= 0 {
            let fix = journal.append(
                at: Int64((location.timestamp.timeIntervalSince1970 * 1000).rounded()),
                lat: location.coordinate.latitude,
                lon: location.coordinate.longitude,
                accuracy: location.horizontalAccuracy,
                heading: location.course >= 0 ? location.course : -1,
                speed: location.speed >= 0 ? location.speed : -1)
            latest = fix.seq
        }
        if latest != nil { notify() }
    }

    func locationManager(_ manager: CLLocationManager, didFailWithError error: Error) {
        if (error as? CLError)?.code == .denied { end() }
    }

    private var authorized: Bool {
        let status = manager.authorizationStatus
        return status == .authorizedWhenInUse || status == .authorizedAlways
    }

    private var authorization: String {
        switch manager.authorizationStatus {
        case .notDetermined: return "notDetermined"
        case .authorizedAlways: return "always"
        case .authorizedWhenInUse: return "whenInUse"
        default: return "denied"
        }
    }

    private func begin() {
        defaults.set(true, forKey: runningKey)
        manager.allowsBackgroundLocationUpdates = true
        manager.showsBackgroundLocationIndicator = true
        manager.startUpdatingLocation()
    }

    private func end() {
        defaults.set(false, forKey: runningKey)
        manager.stopUpdatingLocation()
        manager.allowsBackgroundLocationUpdates = false
    }

    private func notify() {
        let wait = 1 - Date().timeIntervalSince(lastNotified)
        guard wait > 0 else { return send() }
        guard !notifyScheduled else { return }
        notifyScheduled = true
        DispatchQueue.main.asyncAfter(deadline: .now() + wait) { [weak self] in
            self?.notifyScheduled = false
            self?.send()
        }
    }

    private func send() {
        lastNotified = Date()
        onFix?(journal.latest)
    }

    private func servicesEnabled(_ then: @escaping (Bool) -> Void) {
        DispatchQueue.global(qos: .userInitiated).async {
            let enabled = CLLocationManager.locationServicesEnabled()
            DispatchQueue.main.async { then(enabled) }
        }
    }
}
