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

    public ValueTask<ServiceList> ServicesAsync() => _library.ListServicesAsync(new Done());

    public ValueTask<SourceStatus> StatusAsync() => _library.StatusAsync(new Done());

    public ValueTask<CopyProgress> CopyingAsync() => _library.CopyingAsync(new Done());

    public ValueTask<SourceStatus> ChooseSourceAsync(string source) => _library.ChooseSourceAsync(new SourceChoice { Source = source });

    public ValueTask<SourceStatus> CheckNowAsync() => _library.CheckNowAsync(new Done());

    public ValueTask<AudioUploadTicket> BeginUploadAsync(AudioUploadRequest request) => _library.BeginAudioUploadAsync(request);

    public ValueTask<SermonDetail> AddUploadAsync(SermonUpload upload) => _library.AddUploadAsync(upload);
}
