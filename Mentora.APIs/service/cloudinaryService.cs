using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace Mentora.APIs.service
{
    public class cloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public cloudinaryService(IOptions<cloudinarySettings> options)
        {
            if (options?.Value == null)
            {
                throw new ArgumentNullException(nameof(options), "Cloudinary settings are not configured");
            }

            var settings = options.Value;
            var account = new Account(
                settings.Name ?? throw new ArgumentNullException(nameof(settings.Name)),
                settings.APIKey ?? throw new ArgumentNullException(nameof(settings.APIKey)),
                settings.APISecret ?? throw new ArgumentNullException(nameof(settings.APISecret))
            );
            _cloudinary = new Cloudinary(account);
        }

        public async Task<String> upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is null or empty");
            }

            await using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams()
            {
                File = new FileDescription(file.Name, stream),
                Folder = "userImages/pic",
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult == null)
            {
                throw new InvalidOperationException("Upload failed: No result returned from Cloudinary");
            }

            if (uploadResult.Error != null)
            {
                throw new InvalidOperationException($"Upload failed: {uploadResult.Error.Message}");
            }

            if (uploadResult.Url == null)
            {
                throw new InvalidOperationException("Upload completed but no URL was returned");
            }

            return uploadResult.Url.ToString();
        }
    }
}
