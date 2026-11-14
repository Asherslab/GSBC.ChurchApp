using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace GSBC.ChurchApp.Architecture.Tests;

public sealed partial class PrivacyRules
{
    private const string InfoPlist = "src/Hosts/App/shell/ios/App/App/Info.plist";

    private static readonly (string Uses, Regex Found, string[] Keys)[] NativeNeeds =
    [
        ("the camera", new(@"\bAVCaptureDevice\b|\bAVCaptureSession\b|\bUIImagePickerController\b"), ["NSCameraUsageDescription"]),
        ("the microphone", new(@"\bAVAudioRecorder\b|requestRecordPermission|setCategory\(\.(playAndRecord|record)\b"), ["NSMicrophoneUsageDescription"]),
        ("the photo library", new(@"\bPHPhotoLibrary\b|\bPHAsset\b"), ["NSPhotoLibraryUsageDescription"]),
        ("saving to Photos", new("UIImageWriteToSavedPhotosAlbum|UISaveVideoAtPathToSavedPhotosAlbum"), ["NSPhotoLibraryAddUsageDescription"]),
        ("location", new(@"\bCLLocationManager\b"), ["NSLocationWhenInUseUsageDescription"]),
        ("contacts", new(@"\bCNContactStore\b"), ["NSContactsUsageDescription"]),
        ("the calendar", new(@"\bEKEventStore\b"), ["NSCalendarsFullAccessUsageDescription", "NSCalendarsWriteOnlyAccessUsageDescription"]),
        ("speech recognition", new(@"\bSFSpeechRecognizer\b"), ["NSSpeechRecognitionUsageDescription"]),
        ("Face ID", new(@"\bLAContext\b"), ["NSFaceIDUsageDescription"]),
    ];

    private static readonly string[] WebPickerNeeds = ["NSCameraUsageDescription", "NSMicrophoneUsageDescription"];

    [Fact]
    public void TheAppDescribesEveryPrivacyProtectedThingItCanReach()
    {
        HashSet<string> described = Described();
        List<string> missing = [];

        foreach (string path in WebFilePickers())
            missing.AddRange(WebPickerNeeds.Where(key => !described.Contains(key))
                .Select(key => $"{path} has a file picker, and iOS offers to take a photo or video from it: {key}"));

        foreach (string path in RepoFiles.Under("src/Hosts/App", ".swift").Where(path => !path.Contains($"{Path.DirectorySeparatorChar}Tests{Path.DirectorySeparatorChar}", StringComparison.Ordinal)))
        {
            string text = File.ReadAllText(path);
            foreach ((string uses, _, string[] keys) in NativeNeeds.Where(need => need.Found.IsMatch(text)))
                missing.AddRange(keys.Where(key => !described.Contains(key))
                    .Select(key => $"{Path.GetRelativePath(RepoRoot.Path, path)} uses {uses}: {key}"));
        }

        Assert.True(missing.Count == 0,
            $"iOS ends the app the moment it reaches something private without a usage description in {InfoPlist}. Add these:"
            + Environment.NewLine + string.Join(Environment.NewLine, missing.Distinct()));
    }

    private static IEnumerable<string> WebFilePickers() =>
        Projects.InTheApp()
            .Select(project => Path.GetRelativePath(RepoRoot.Path, Path.GetDirectoryName(project)!))
            .SelectMany(folder => RepoFiles.Under(folder, ".razor"))
            .Where(path => FilePicker().IsMatch(File.ReadAllText(path)))
            .Select(path => Path.GetRelativePath(RepoRoot.Path, path));

    private static HashSet<string> Described()
    {
        using XmlReader reader = XmlReader.Create(Path.Combine(RepoRoot.Path, InfoPlist), new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore });
        XElement root = XElement.Load(reader).Element("dict")!;
        return root.Elements("key")
            .Where(key => key.ElementsAfterSelf().FirstOrDefault() is { Name.LocalName: "string" } value && value.Value.Trim().Length > 0)
            .Select(key => key.Value)
            .ToHashSet();
    }

    [GeneratedRegex(@"<InputFile\b|type=""file""")]
    private static partial Regex FilePicker();
}
