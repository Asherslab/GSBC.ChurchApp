import CoreLocation
import Foundation

final class Compass: NSObject, CLLocationManagerDelegate, @unchecked Sendable {
    static let shared = Compass()

    var onHeading: ((_ heading: Double, _ accuracy: Double) -> Void)?
    private var manager: CLLocationManager?

    func start() {
        DispatchQueue.main.async {
            guard CLLocationManager.headingAvailable() else { return }
            let manager = self.manager ?? CLLocationManager()
            manager.delegate = self
            manager.headingFilter = 2
            self.manager = manager
            manager.startUpdatingHeading()
        }
    }

    func stop() {
        DispatchQueue.main.async { self.manager?.stopUpdatingHeading() }
    }

    func locationManager(_ manager: CLLocationManager, didUpdateHeading heading: CLHeading) {
        onHeading?(heading.trueHeading >= 0 ? heading.trueHeading : heading.magneticHeading, heading.headingAccuracy)
    }

    // A walker can't stop to wave the phone in a figure of eight; an unsure compass widens the cone instead.
    func locationManagerShouldDisplayHeadingCalibration(_ manager: CLLocationManager) -> Bool { false }
}
