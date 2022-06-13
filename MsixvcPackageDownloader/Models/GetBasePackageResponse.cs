namespace MsixvcPackageDownloader.Models;

public class GetBasePackageResponse
{
    public bool PackageFound { get; set; }
    public Guid ContentId { get; set; }
    public string VersionId { get; set; }
    public List<PackageFile> PackageFiles { get; set; }
    public string Version { get; set; }
    public PackageMetadata PackageMetadata { get; set; }
    public string HashOfHashes { get; set; }
}