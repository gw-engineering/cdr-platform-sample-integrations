using System.IO;
using System.Text;
using System.Threading.Tasks;
using Flurl.Http;

namespace CdrAzureFunctions.Responses;

public class ProtectResponse
{
    private IFlurlResponse _protectRequestResponse;
    private string _name;
    public bool IsProtected { get; private set; }

    public IFlurlResponse ProtectRequestResponse
    {
        get => _protectRequestResponse;
        set
        {
            IsProtected = value.StatusCode == 201;
            _protectRequestResponse = value;
        }
    }

    public string BlobContainerName { get; set; }

    public string Name
    {
        get => IsProtected ? _name : $"{_name}.txt";
        set => _name = value;
    }

    public async Task<Stream> GetResponseStream()
    {
        return IsProtected
            ? await _protectRequestResponse.GetStreamAsync()
            : new MemoryStream(Encoding.UTF8.GetBytes(
                $"File could not be protected StatusCode:{_protectRequestResponse.StatusCode} ResponseMessage:{_protectRequestResponse.ResponseMessage.Content}"));
    }
}