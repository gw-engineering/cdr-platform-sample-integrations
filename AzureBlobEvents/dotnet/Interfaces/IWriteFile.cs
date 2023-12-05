using System.IO;
using System.Threading.Tasks;

namespace CdrAzureFunctions.Interfaces;

public interface IWriteFile
{
    Task WriteProtectedFile(string blobContainerName, string blobName, Stream protectedFile);
}