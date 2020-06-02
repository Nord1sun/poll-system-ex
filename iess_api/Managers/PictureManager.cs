using System.IO;
using iess_api.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;

namespace iess_api.Managers
{
    public class PictureManager:IPictureManager
    {
        public Stream GetCompactPictureVersion(Stream inputStream)
        {
            Stream outputStream ;
            var imageInfo = Image.Identify(inputStream);
            inputStream.Position = 0;
            if (imageInfo.Height > 600 || imageInfo.Width > 600)
            {
                outputStream = new MemoryStream();
                using (var image = Image.Load(inputStream, out IImageFormat format))
                {
                    image.Mutate(i => i.Resize(new ResizeOptions()
                    {
                        Mode = ResizeMode.Max,
                        Size = new Size(600, 600)
                    }));
                    image.SaveAsJpeg(outputStream);
                    outputStream.Position = 0;
                }
            }
            else
            {
                outputStream = inputStream;
            }
            return outputStream;
        }
    }
}
