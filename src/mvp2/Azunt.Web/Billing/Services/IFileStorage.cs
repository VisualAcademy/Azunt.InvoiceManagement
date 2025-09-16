using System.Threading.Tasks;

namespace Azunt.Web.Billing.Services;
public interface IFileStorage {
    Task<string> SaveAsync(string fileName, byte[] bytes, string contentType = "application/pdf");
    string GetPublicUrl(string filePath);
}
