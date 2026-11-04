using GSBC.ChurchApp.Features.Sermons.Contracts;

namespace GSBC.ChurchApp.Features.Sermons.Admin.Components;

public sealed record DeskView(SermonList Sermons, SourceStatus Status, IReadOnlyList<SeriesDetail> Series, IReadOnlyList<SpeakerDetail> Speakers);
