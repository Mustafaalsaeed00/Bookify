namespace Bookify.Web.Services
{
	public interface IImageService
	{
		Task<(bool isUploaded, string? errorMessage ,string? imageUrl, string? ImagePublicId)> UploadAsync(IFormFile image, string imageName, string folderPath, bool hasThumbnail);
		Task DeleteAsync(string? imageUrl, string? imageThumbnail = null);
	}
}
