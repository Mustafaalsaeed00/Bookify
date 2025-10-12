
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using static Bookify.Web.Controllers.BooksController;

namespace Bookify.Web.Services
{
	public class ImageService : IImageService
	{
		private readonly IImageStorage _imageStorage;
		private List<string> _allowedExtensions = new() { ".jpg", ".jpeg", ".png" };
		private int _maxAllowedSize = 2076672;

		public ImageService(IImageStorage imageStorage)
		{
			_imageStorage = imageStorage;
		}

		public async Task<(bool isUploaded, string? errorMessage , string? imageUrl , string? ImagePublicId)> UploadAsync(IFormFile image, string imageName, string folderPath, bool hasThumbnail)
		{
			string extension = Path.GetExtension(image.FileName);
			if (!_allowedExtensions.Contains(extension))
				return (false, Errors.AllowedExtensions , null , null);


			if (image.Length > _maxAllowedSize)
				return (false, Errors.MaxSize , null , null);

			
			(var ImageUrl, var ImagePublicId) = await _imageStorage.SaveImage(image, imageName, folderPath, hasThumbnail);

			return (true, null , ImageUrl , ImagePublicId);
		}

		public async Task DeleteAsync(string imageUrl, string? imageThumbnail = null)
		{
			await _imageStorage.DeleteAsync(imageUrl, imageThumbnail);
		}

	}
}
