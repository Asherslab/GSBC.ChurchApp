#:project ../../src/Features/ReleaseNotes/GSBC.ChurchApp.Features.ReleaseNotes.Generator/GSBC.ChurchApp.Features.ReleaseNotes.Generator.csproj
#:property PublishAot=false

using GSBC.ChurchApp.Features.ReleaseNotes.Generator;

try
{
    IEnumerable<Note> notes = args.Select(path =>
        NoteFile.Parse(Path.GetFileName(Path.GetDirectoryName(Path.GetFullPath(path)))!, Path.GetFileName(path), File.ReadAllText(path)));
    Console.Write(WhatToTest.Render(notes.ToList()));
}
catch (NoteException problem)
{
    Console.Error.WriteLine(problem.Message);
    return 1;
}
return 0;
