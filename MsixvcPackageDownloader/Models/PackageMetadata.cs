namespace MsixvcPackageDownloader.Models;

public class PackageMetadata
{
    public List<string> CdnRoots { get; set; }
    public List<string> BackgroundCdnRootPaths { get; set; }
    public List<PackageMetadataFile> Files { get; set; }
    public ulong EstimatedTotalDownloadSize { get; set; }
}