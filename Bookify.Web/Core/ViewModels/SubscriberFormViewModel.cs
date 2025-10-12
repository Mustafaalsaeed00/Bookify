using Microsoft.AspNetCore.Mvc.Rendering;
using UoN.ExpressiveAnnotations.NetCore.Attributes;

namespace Bookify.Web.Core.ViewModels
{
	public class SubscriberFormViewModel
	{
		public int Id { get; set; }

		[MaxLength(100)]
		[Display(Name = "First Name")]
		[RegularExpression(RegexPatterns.DenySpecialCharacters , ErrorMessage = Errors.DenySpecialCharacters)]
		public string FirstName { get; set; } = null!;

		[MaxLength(100)]
		[Display(Name = "Last Name")]
		[RegularExpression(RegexPatterns.DenySpecialCharacters, ErrorMessage = Errors.DenySpecialCharacters)]
		public string LastName { get; set; } = null!;

		[Display(Name = "Date Of Birth")]
		[AssertThat("DateOfBirth <= Today()", ErrorMessage = Errors.NotAllowFutureDate)]
		public DateTime DateOfBirth { get; set; } = DateTime.Now;

		[MaxLength(14,ErrorMessage = Errors.MaxLength)]
		[RegularExpression(RegexPatterns.NationalId_Egy , ErrorMessage = Errors.InvalidNationalId)]
		[Display(Name = "National ID")]
		[Remote("AllowNationalId" , null! ,AdditionalFields = "Id" , ErrorMessage = Errors.Duplicated)]
		public string NationalId { get; set; } = null!;

		[MaxLength(15)]
		[RegularExpression(RegexPatterns.MobileNumber ,ErrorMessage = Errors.InvalidMobileNumber)]
		[Display(Name = "Mobile Number")]
		[Remote("AllowMobileNumber", null!, AdditionalFields = "Id", ErrorMessage = Errors.Duplicated)]
		public string MobileNumber { get; set; } = null!;

		[Display(Name = "Has WhatsApp")]
		public bool HasWhatsApp { get; set; }

		[MaxLength(150 , ErrorMessage = Errors.MaxLength)]
		[Remote("AllowEmail", null!, AdditionalFields = "Id", ErrorMessage = Errors.Duplicated)]
		[EmailAddress]
		public string Email { get; set; } = null!;

		[RequiredIf("Id == 0", ErrorMessage = Errors.RequiredField)]
		public IFormFile? Image { get; set; }

		public string? ImageUrl { get; set; }

		public string? ImageThumbnailUrl { get; set; }

		public int AreaId { get; set; }

		public IEnumerable<SelectListItem>? Areas { get; set; }

		public int GovernorateId { get; set; }

		public IEnumerable<SelectListItem>? Governorates { get; set; }

		[MaxLength(500, ErrorMessage = Errors.MaxLength)]
		public string Address { get; set; } = null!;

		public bool IsBlackListed { get; set; }
	}
}
