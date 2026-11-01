namespace GSBC.ChurchApp.Platform.Data;

public sealed record MigrationTarget(Type ContextType, int Order);
