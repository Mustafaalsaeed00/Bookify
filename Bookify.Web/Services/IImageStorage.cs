namespace Bookify.Web.Services
{
	public interface IImageStorage
	{
		Task<(string? imageUrl, string imagePublicId)> SaveImage(IFormFile image, string imageName, string folderPath, bool hasThumbnail);
		Task DeleteAsync(string imageUrl, string? imageThumbnail);
	}
}