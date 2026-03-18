namespace GTEK.FSM.Backend.Infrastructure.Configuration;

/// <summary>
/// Placeholder configuration for storage providers.
/// No storage provider implementation is activated in Phase 0.7.5.
/// </summary>
public class StorageOptions
{
    public string DefaultProvider { get; set; } = "Local";

    public LocalStorageOptions Local { get; set; } = new();

    public BlobStorageOptions Blob { get; set; } = new();

    public S3StorageOptions S3 { get; set; } = new();
}

public class LocalStorageOptions
{
    public string RootPath { get; set; } = "./storage";
}

public class BlobStorageOptions
{
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = string.Empty;
}

public class S3StorageOptions
{
    public string ServiceUrl { get; set; } = string.Empty;

    public string BucketName { get; set; } = string.Empty;

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;
}
