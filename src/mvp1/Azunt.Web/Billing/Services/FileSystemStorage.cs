using System.IO;
using System.Threading.Tasks;

namespace Azunt.Web.Billing.Services;
public class FileSystemStorage : IFileStorage {
    private readonly string _root;
    private readonly string _baseUrl;
    public FileSystemStorage(string root, string baseUrl) { _root = root; _baseUrl = baseUrl; }
    public async Task<string> SaveAsync(string fileName, byte[] bytes, string contentType = "application/pdf") {
        var path = Path.Combine(_root, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        return path;
    }
    public string GetPublicUrl(string filePath) {
        var name = Path.GetFileName(filePath);
        return $"{_baseUrl}/{name}";
    }
}
