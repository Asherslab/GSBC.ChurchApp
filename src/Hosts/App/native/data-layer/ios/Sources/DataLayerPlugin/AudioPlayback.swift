import AVFoundation
import DataLayerCore
import MediaPlayer
import UIKit

struct AudioItem: Equatable {
    var id: String
    var url: String
    var title: String
    var artist: String
    var album: String
    var artworkUrl: String?
    var duration: Double
    var link: String?

    init?(_ value: JSObjectLike?) {
        guard let value, let id = value["id"] as? String, let url = value["url"] as? String else { return nil }
        self.id = id
        self.url = url
        title = value["title"] as? String ?? ""
        artist = value["artist"] as? String ?? ""
        album = value["album"] as? String ?? ""
        artworkUrl = value["artworkUrl"] as? String
        duration = (value["duration"] as? NSNumber)?.doubleValue ?? 0
        link = value["link"] as? String
    }

    var json: [String: Any] {
        var result: [String: Any] = ["id": id, "url": url, "title": title, "artist": artist, "album": album, "duration": duration]
        if let artworkUrl { result["artworkUrl"] = artworkUrl }
        if let link { result["link"] = link }
        return result
    }
}

typealias JSObjectLike = [String: Any]

final class AudioPlayback: NSObject {
    static let shared = AudioPlayback()

    static let backSeconds = 15.0
    static let forwardSeconds = 30.0
    private static let saveEvery: TimeInterval = 5
    private static let currentKey = "gsbc.audio.current"

    var onState: (([String: Any]) -> Void)?
    let journal: PlaybackJournal

    private let player = AVPlayer()
    private var item: AudioItem?
    private var loaded = false
    private var restoredPosition = 0.0
    private var upNext: [AudioItem] = []
    private var rate: Float = 1
    private var sleepTimer: Timer?
    private var sleepAt: Date?
    private var sleepAtEnd = false
    private var ended = false
    private var problem: String?
    private var lastSaved = Date.distantPast
    private var timeObserver: Any?
    private var statusObservation: NSKeyValueObservation?
    private var artwork: MPMediaItemArtwork?
    private var artworkFor: String?

    private override init() {
        let directory = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0].appendingPathComponent("DataLayer")
        journal = PlaybackJournal(directory: directory)
        super.init()
        player.automaticallyWaitsToMinimizeStalling = true
        restore()
        observe()
        configureRemoteCommands()
    }

    func state() -> [String: Any] {
        var result: [String: Any] = [
            "playing": player.timeControlStatus != .paused,
            "buffering": player.timeControlStatus == .waitingToPlayAtSpecifiedRate,
            "position": position,
            "duration": duration,
            "rate": Double(rate),
            "sleepAtEnd": sleepAtEnd,
            "ended": ended,
        ]
        if let item { result["item"] = item.json }
        if let sleepAt { result["sleepAt"] = Int64(sleepAt.timeIntervalSince1970 * 1000) }
        if let problem { result["problem"] = problem }
        return result
    }

    func load(_ next: AudioItem, startAt: Double, play: Bool) {
        if loaded { save() }
        guard let url = URL(string: next.url) else {
            problem = "This sermon couldn’t be played."
            emit()
            return
        }
        item = next
        ended = false
        problem = nil
        loaded = true
        let playerItem = AVPlayerItem(url: url)
        statusObservation = playerItem.observe(\.status) { [weak self] observed, _ in
            DispatchQueue.main.async {
                guard let self, observed.status == .failed else { return }
                self.problem = "This sermon couldn’t be played. Check your connection and try again."
                self.emit()
            }
        }
        player.replaceCurrentItem(with: playerItem)
        if startAt > 0 { player.seek(to: CMTime(seconds: startAt, preferredTimescale: 600)) }
        persistCurrent(position: startAt)
        refreshArtwork()
        if play { self.play() } else { publishNowPlaying(); emit() }
    }

    func play() {
        guard let item else { return }
        if !loaded {
            load(item, startAt: restoredPosition, play: true)
            return
        }
        if ended {
            ended = false
            player.seek(to: .zero)
        }
        activateSession()
        player.playImmediately(atRate: rate)
        publishNowPlaying()
        emit()
    }

    func pause() {
        player.pause()
        save()
        publishNowPlaying()
        emit()
    }

    func seek(to seconds: Double) {
        let target = max(0, min(seconds, duration > 0 ? duration : seconds))
        if !loaded {
            restoredPosition = target
            persistCurrent(position: target)
            emit()
            return
        }
        player.seek(to: CMTime(seconds: target, preferredTimescale: 600)) { [weak self] _ in
            self?.save()
            self?.publishNowPlaying()
            self?.emit()
        }
    }

    func skip(by seconds: Double) { seek(to: position + seconds) }

    func setRate(_ value: Double) {
        rate = Float(min(max(value, 0.5), 3))
        if player.timeControlStatus != .paused { player.rate = rate }
        publishNowPlaying()
        emit()
    }

    func sleep(minutes: Int, endOfItem: Bool) {
        sleepTimer?.invalidate()
        sleepTimer = nil
        sleepAt = nil
        sleepAtEnd = endOfItem
        if minutes > 0 {
            sleepAt = Date().addingTimeInterval(TimeInterval(minutes * 60))
            sleepTimer = Timer.scheduledTimer(withTimeInterval: TimeInterval(minutes * 60), repeats: false) { [weak self] _ in
                self?.sleepAt = nil
                self?.pause()
            }
        }
        emit()
    }

    func setUpNext(_ items: [AudioItem]) {
        upNext = items
        MPRemoteCommandCenter.shared().nextTrackCommand.isEnabled = !items.isEmpty
    }

    func stop() {
        save()
        player.pause()
        player.replaceCurrentItem(with: nil)
        item = nil
        loaded = false
        upNext = []
        sleep(minutes: 0, endOfItem: false)
        UserDefaults.standard.removeObject(forKey: Self.currentKey)
        MPNowPlayingInfoCenter.default().nowPlayingInfo = nil
        try? AVAudioSession.sharedInstance().setActive(false, options: .notifyOthersOnDeactivation)
        emit()
    }

    func mark(_ id: String, _ mark: String, at: Int64) { journal.mark(id, mark, at: at) }

    func save() {
        guard let item, loaded else { return }
        journal.heard(item.id, position: position, duration: duration)
        persistCurrent(position: position)
        lastSaved = Date()
    }

    private var position: Double {
        guard loaded else { return restoredPosition }
        let seconds = player.currentTime().seconds
        return seconds.isFinite ? max(0, seconds) : 0
    }

    private var duration: Double {
        if let seconds = player.currentItem?.duration.seconds, seconds.isFinite, seconds > 0 { return seconds }
        return item?.duration ?? 0
    }

    private func emit() { onState?(state()) }

    private func activateSession() {
        let session = AVAudioSession.sharedInstance()
        try? session.setCategory(.playback, mode: .spokenAudio, policy: .longFormAudio)
        try? session.setActive(true)
    }

    private func observe() {
        timeObserver = player.addPeriodicTimeObserver(forInterval: CMTime(seconds: 1, preferredTimescale: 600), queue: .main) { [weak self] _ in
            guard let self, self.player.timeControlStatus == .playing else { return }
            if Date().timeIntervalSince(self.lastSaved) >= Self.saveEvery { self.save() }
            self.emit()
        }
        let center = NotificationCenter.default
        center.addObserver(self, selector: #selector(itemEnded(_:)), name: .AVPlayerItemDidPlayToEndTime, object: nil)
        center.addObserver(self, selector: #selector(interrupted(_:)), name: AVAudioSession.interruptionNotification, object: nil)
        center.addObserver(self, selector: #selector(routeChanged(_:)), name: AVAudioSession.routeChangeNotification, object: nil)
        center.addObserver(self, selector: #selector(leaving), name: UIApplication.didEnterBackgroundNotification, object: nil)
        center.addObserver(self, selector: #selector(leaving), name: UIApplication.willTerminateNotification, object: nil)
        player.addObserver(self, forKeyPath: "timeControlStatus", options: [.new], context: nil)
    }

    override func observeValue(forKeyPath keyPath: String?, of object: Any?, change: [NSKeyValueChangeKey: Any]?, context: UnsafeMutableRawPointer?) {
        DispatchQueue.main.async { [weak self] in
            self?.publishNowPlaying()
            self?.emit()
        }
    }

    @objc private func itemEnded(_ notification: Notification) {
        guard let ended = notification.object as? AVPlayerItem, ended == player.currentItem, let item else { return }
        journal.heard(item.id, position: duration, duration: duration)
        if sleepAtEnd {
            sleep(minutes: 0, endOfItem: false)
            finish()
            return
        }
        if !upNext.isEmpty {
            load(upNext.removeFirst(), startAt: 0, play: true)
            setUpNext(upNext)
            return
        }
        finish()
    }

    private func finish() {
        ended = true
        persistCurrent(position: duration)
        publishNowPlaying()
        emit()
    }

    @objc private func interrupted(_ notification: Notification) {
        guard let raw = notification.userInfo?[AVAudioSessionInterruptionTypeKey] as? UInt,
              let type = AVAudioSession.InterruptionType(rawValue: raw) else { return }
        switch type {
        case .began:
            save()
            emit()
        case .ended:
            let options = (notification.userInfo?[AVAudioSessionInterruptionOptionKey] as? UInt).map(AVAudioSession.InterruptionOptions.init) ?? []
            if options.contains(.shouldResume) { play() }
        @unknown default:
            break
        }
    }

    @objc private func routeChanged(_ notification: Notification) {
        guard let raw = notification.userInfo?[AVAudioSessionRouteChangeReasonKey] as? UInt,
              AVAudioSession.RouteChangeReason(rawValue: raw) == .oldDeviceUnavailable else { return }
        DispatchQueue.main.async { [weak self] in self?.pause() }
    }

    @objc private func leaving() { save() }

    private func configureRemoteCommands() {
        let commands = MPRemoteCommandCenter.shared()
        commands.playCommand.addTarget { [weak self] _ in self?.play(); return .success }
        commands.pauseCommand.addTarget { [weak self] _ in self?.pause(); return .success }
        commands.togglePlayPauseCommand.addTarget { [weak self] _ in
            guard let self else { return .commandFailed }
            self.player.timeControlStatus == .paused ? self.play() : self.pause()
            return .success
        }
        commands.skipBackwardCommand.preferredIntervals = [NSNumber(value: Self.backSeconds)]
        commands.skipBackwardCommand.addTarget { [weak self] _ in self?.skip(by: -Self.backSeconds); return .success }
        commands.skipForwardCommand.preferredIntervals = [NSNumber(value: Self.forwardSeconds)]
        commands.skipForwardCommand.addTarget { [weak self] _ in self?.skip(by: Self.forwardSeconds); return .success }
        commands.changePlaybackPositionCommand.addTarget { [weak self] event in
            guard let event = event as? MPChangePlaybackPositionCommandEvent else { return .commandFailed }
            self?.seek(to: event.positionTime)
            return .success
        }
        commands.nextTrackCommand.isEnabled = false
        commands.nextTrackCommand.addTarget { [weak self] _ in
            guard let self, !self.upNext.isEmpty else { return .noSuchContent }
            self.load(self.upNext.removeFirst(), startAt: 0, play: true)
            self.setUpNext(self.upNext)
            return .success
        }
        commands.previousTrackCommand.isEnabled = false
    }

    private func publishNowPlaying() {
        guard let item else { return }
        var info: [String: Any] = [
            MPMediaItemPropertyTitle: item.title,
            MPMediaItemPropertyArtist: item.artist,
            MPMediaItemPropertyAlbumTitle: item.album,
            MPMediaItemPropertyPlaybackDuration: duration,
            MPNowPlayingInfoPropertyElapsedPlaybackTime: position,
            MPNowPlayingInfoPropertyPlaybackRate: player.timeControlStatus == .paused ? 0.0 : Double(rate),
            MPNowPlayingInfoPropertyDefaultPlaybackRate: Double(rate),
            MPNowPlayingInfoPropertyMediaType: MPNowPlayingInfoMediaType.audio.rawValue,
        ]
        if let artwork, artworkFor == item.id { info[MPMediaItemPropertyArtwork] = artwork }
        MPNowPlayingInfoCenter.default().nowPlayingInfo = info
    }

    private func refreshArtwork() {
        guard let item, artworkFor != item.id else { return }
        artwork = nil
        artworkFor = nil
        guard let link = item.artworkUrl, let url = URL(string: link) else { return }
        let id = item.id
        URLSession.shared.dataTask(with: url) { [weak self] data, _, _ in
            guard let data, let image = UIImage(data: data) else { return }
            DispatchQueue.main.async {
                guard let self, self.item?.id == id else { return }
                self.artwork = MPMediaItemArtwork(boundsSize: image.size) { _ in image }
                self.artworkFor = id
                self.publishNowPlaying()
            }
        }.resume()
    }

    private func persistCurrent(position: Double) {
        guard let item, let data = try? JSONSerialization.data(withJSONObject: ["item": item.json, "position": position]) else { return }
        UserDefaults.standard.set(data, forKey: Self.currentKey)
    }

    private func restore() {
        guard let data = UserDefaults.standard.data(forKey: Self.currentKey),
              let stored = try? JSONSerialization.jsonObject(with: data) as? [String: Any],
              let item = AudioItem(stored["item"] as? [String: Any]) else { return }
        self.item = item
        restoredPosition = (stored["position"] as? NSNumber)?.doubleValue ?? journal.record(item.id)?.position ?? 0
    }
}
