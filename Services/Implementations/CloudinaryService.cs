using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration configuration)
        {
            var cloudinarySettings = configuration.GetSection("Cloudinary");
            var cloudName = cloudinarySettings["CloudName"];
            var apiKey = cloudinarySettings["ApiKey"];
            var apiSecret = cloudinarySettings["ApiSecret"];

            var account = new Account(
                cloudName,
                apiKey,
                apiSecret);

            _cloudinary = new Cloudinary(account);
        }

        public async Task<string> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return string.Empty;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "quickmarket"
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }

        public async Task<bool> DeleteImageAsync(string publicId)
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            // Extract public_id from URL if necessary
            if (publicId.Contains("cloudinary.com"))
            {
                try {
                    // Parse URL to extract folder and filename
                    // Example: https://res.cloudinary.com/yourcloudname/image/upload/v1234567890/quickmarket/image123.jpg
                    var uri = new Uri(publicId);
                    var pathSegments = uri.AbsolutePath.Split('/');
                    
                    // Find the upload part index
                    int uploadIndex = Array.IndexOf(pathSegments, "upload");
                    
                    if (uploadIndex >= 0 && pathSegments.Length > uploadIndex + 2)
                    {
                        // The version segment (v1234567890) is after upload
                        // Then we have folder(s) and filename
                        var pathAfterVersion = string.Join("/", pathSegments.Skip(uploadIndex + 2));
                        
                        // Remove file extension
                        var lastDotIndex = pathAfterVersion.LastIndexOf('.');
                        if (lastDotIndex > 0)
                        {
                            pathAfterVersion = pathAfterVersion.Substring(0, lastDotIndex);
                        }
                        
                        publicId = pathAfterVersion;
                    }
                }
                catch {
                    // Fall back to simple extraction if URI parsing fails
                    var segments = publicId.Split('/');
                    var filenameWithExtension = segments[^1];
                    var filename = filenameWithExtension.Substring(0, filenameWithExtension.LastIndexOf('.'));
                    publicId = $"quickmarket/{filename}";
                }
            }

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            return result.Result == "ok";
        }
    }
}
