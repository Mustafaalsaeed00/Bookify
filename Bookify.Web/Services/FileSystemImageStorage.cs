using Bookify.Web.Core.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.HttpResults;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Bookify.Web.Services
{
	public class FileSystemImageStorage : IImageStorage
	{
		private readonly IWebHostEnvironment _webHostEnvironment;

		public FileSystemImageStorage(IWebHostEnvironment webHostEnvironment)
		{
			_webHostEnvironment = webHostEnvironment;
		}

		public async Task<(string? imageUrl, string? imagePublicId)> SaveImage(IFormFile image, string imageName, string folderPath, bool hasThumbnail)
		{
			string path = Path.Combine($"{_webHostEnvironment.WebRootPath}{folderPath}", imageName);
			string thumbPath = Path.Combine($"{_webHostEnvironment.WebRootPath}{folderPath}", "thumb", imageName);
			using var stream = File.Create(path);
			await image.CopyToAsync(stream);
			stream.Dispose();

			if(hasThumbnail)
			{
				using var uploadedImage = Image.Load(image.OpenReadStream());
				var ratio = (float)uploadedImage.Width / 200;
				var height = uploadedImage.Height / ratio;
				uploadedImage.Mutate(i => i.Resize(width: 200, height: (int)height));
				uploadedImage.Save(thumbPath);
			}

			return (null, null);
		}

		public async Task DeleteAsync(string imageUrl, bool? imageThumbnail)
		{
			var oldImagePath = $"{_webHostEnvironment.WebRootPath}{imageUrl}";
			var oldThumbPath = $"{_webHostEnvironment.WebRootPath}{imageThumbnail}";


			if (File.Exists(oldImagePath))
				File.Delete(oldImagePath);

			if (File.Exists(oldThumbPath))
				File.Delete(oldThumbPath);
		}

	}
}
