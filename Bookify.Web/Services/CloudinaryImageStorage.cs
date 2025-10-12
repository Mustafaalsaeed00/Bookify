
using Bookify.Web.Core.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CloudinaryDotNet.Core;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;

namespace Bookify.Web.Services
{
	public class CloudinaryImageStorage : IImageStorage
	{
		private readonly Cloudinary _cloudinary;

		public CloudinaryImageStorage(IOptions<CloudinarySettings> cloudinary)
		{
			Account account = new()
			{
				Cloud = cloudinary.Value.Cloud,
				ApiKey = cloudinary.Value.ApiKey,
				ApiSecret = cloudinary.Value.ApiSecret,
			};

			_cloudinary = new Cloudinary(account);
		}

		public async Task<(string? imageUrl, string imagePublicId)> SaveImage(IFormFile image, string imageName, string? folderPath, bool hasThumbnail)
		{
			using var stream = image.OpenReadStream();

			var ImageUrl = "";
			var ImagePublicId = "";

			var imageParams = new ImageUploadParams
			{
				File = new FileDescription(imageName, stream),
				UseFilename = true
			};
			var result = await _cloudinary.UploadAsync(imageParams);

			ImageUrl = result.SecureUrl.ToString();

			ImagePublicId = result.PublicId;

			return (ImageUrl, ImagePublicId);
		}

		public async Task DeleteAsync(string imageUrl, string? imageThumbnail)
		{
			var publicId = ExtractPublicId(imageUrl);
			 await _cloudinary.DeleteResourcesAsync(publicId);
		}

		string ExtractPublicId(string imageUrl)
		{
			var cleanUrl = imageUrl.Split('?')[0];
			// 2. هات الجزء اللي بعد "upload/"
			var parts = cleanUrl.Split(new[] { "/upload/" }, StringSplitOptions.None);
			if (parts.Length < 2)
				throw new ArgumentException("Invalid Cloudinary URL format.");

			var segments = parts[1].Split('/').ToList();

			if (segments[0].StartsWith('v'))
				segments.RemoveAt(0);

			// Handle case when image is inside nested folders
			var idWithExtension = string.Join('/', segments);

			//var idWithExtension = parts[1].SkipWhile(chr => chr != '/').Skip(1).ToArray();

			// 3. شيل الامتداد
			string publicId = Path.ChangeExtension(string.Concat(idWithExtension), null);
			
			return publicId;
		}
	}
}
