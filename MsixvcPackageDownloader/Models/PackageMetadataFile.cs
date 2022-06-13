namespace MsixvcPackageDownloader.Models;

public class PackageMetadataFile : BasePackageFile
{
    public string Name { get; set; }
    public ulong Size { get; set; }
    public string License { get; set; }
}