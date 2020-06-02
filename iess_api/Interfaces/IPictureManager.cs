using System.IO;

namespace iess_api.Interfaces
{
    public interface IPictureManager
    {
        Stream GetCompactPictureVersion(Stream inputStream);
    }
}
