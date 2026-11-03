using Grpc.Core;

namespace GSBC.ChurchApp.Platform.Client.Diagnostics;

public interface IClientDiagnostics
{
    void ExceptionThrown(Exception exception);

    void CallFailed(string method, StatusCode status);
}
