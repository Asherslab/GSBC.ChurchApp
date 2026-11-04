using GSBC.ChurchApp.Platform.Contracts.Tiers;
using ProtoBuf.Grpc;
using ProtoBuf.Grpc.Configuration;

namespace GSBC.ChurchApp.Features.Sermons.Contracts;

[Service("gsbc.sermons.Library")]
public interface ISermonLibraryService
{
    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SermonList> ListAsync(SermonQuery request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SermonDetail> CorrectAsync(SermonCorrection request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SeriesList> ListSeriesAsync(Done request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SeriesDetail> SaveSeriesAsync(SeriesDetail request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SpeakerList> ListSpeakersAsync(Done request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SpeakerDetail> SaveSpeakerAsync(SpeakerDetail request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SourceStatus> StatusAsync(Done request, CallContext context = default);

    [Requires(SermonsPermissions.SourceNode)]
    ValueTask<SourceStatus> ChooseSourceAsync(SourceChoice request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<AudioUploadTicket> BeginAudioUploadAsync(AudioUploadRequest request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SermonDetail> AddUploadAsync(SermonUpload request, CallContext context = default);

    [Requires(SermonsPermissions.EditNode)]
    ValueTask<SourceStatus> CheckNowAsync(Done request, CallContext context = default);
}
