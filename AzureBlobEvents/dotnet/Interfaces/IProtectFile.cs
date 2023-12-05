using System;
using System.Threading.Tasks;
using CdrAzureFunctions.Responses;

namespace CdrAzureFunctions.Interfaces;

public interface IProtectFile
{
    Task<ProtectResponse> ProtectFile(Uri fileUri);
}