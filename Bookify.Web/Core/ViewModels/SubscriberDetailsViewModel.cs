namespace Bookify.Web.Core.ViewModels
{
	public class SubscriberDetailsViewModel
	{
		public int Id { get; set; }
		public string FullName { get; set; } = null!;

		public DateTime DateOfBirth { get; set; }

		public string NationalId { get; set; } = null!;

		public string MobileNumber { get; set; } = null!;

		public bool HasWhatsApp { get; set; }

		public string Email { get; set; } = null!;

		public string ImageUrl { get; set; } = null!;
		public string ImageThumbnailUrl { get; set; } = null!;

		public string? Area { get; set; }

		public string? Governorate { get; set; }

		public string Address { get; set; } = null!;

		public bool IsBlackListed { get; set; }
		public DateTime CreatedOn { get; set; }
	}
}
