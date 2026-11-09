using Grpc.Core;
using GSBC.ChurchApp.Features.Sermons.Contracts;
using ProtoBuf.Grpc.Client;

namespace GSBC.ChurchApp.Features.Sermons.Client;

public sealed class SermonsAdminClient(ChannelBase channel)
{
    private readonly ISermonLibraryService _library = channel.CreateGrpcService<ISermonLibraryService>();

    public ValueTask<SermonList> ListAsync(SermonQuery query) => _library.ListAsync(query);

    public ValueTask<SermonDetail> CorrectAsync(SermonCorrection correction) => _library.CorrectAsync(correction);

    public ValueTask<SeriesList> SeriesAsync() => _library.ListSeriesAsync(new Done());

    public ValueTask<SeriesDetail> SaveSeriesAsync(SeriesDetail series) => _library.SaveSeriesAsync(series);

    public ValueTask<SpeakerList> SpeakersAsync() => _library.ListSpeakersAsync(new Done());

    public ValueTask<SpeakerDetail> SaveSpeakerAsync(SpeakerDetail speaker) => _library.SaveSpeakerAsync(speaker);

    public ValueTask<Done> DeleteSpeakerAsync(string id, string? moveTo) => _library.DeleteSpeakerAsync(new DeleteRequest { Id = id, MoveTo = moveTo });

    public ValueTask<SeriesDeletion> SeriesDeletionAsync(string id) => _library.SeriesDeletionAsync(new DeleteRequest { Id = id });

    public ValueTask<Done> DeleteSeriesAsync(string id, string? moveTo) => _library.DeleteSeriesAsync(new DeleteRequest { Id = id, MoveTo = moveTo });

    public ValueTask<Done> DeleteSermonAsync(string id) => _library.DeleteSermonAsync(new DeleteRequest { Id = id });

    public ValueTask<ServiceList> ServicesAsync() => _library.ListServicesAsync(new Done());

    public ValueTask<SourceStatus> StatusAsync() => _library.StatusAsync(new Done());

    public ValueTask<CopyProgress> CopyingAsync() => _library.CopyingAsync(new Done());

    public ValueTask<SourceStatus> ChooseSourceAsync(string source) => _library.ChooseSourceAsync(new SourceChoice { Source = source });

    public ValueTask<SourceStatus> CheckNowAsync() => _library.CheckNowAsync(new Done());

    public ValueTask<AudioUploadTicket> BeginUploadAsync(AudioUploadRequest request) => _library.BeginAudioUploadAsync(request);

    public ValueTask<SermonDetail> AddUploadAsync(SermonUpload upload) => _library.AddUploadAsync(upload);
}
