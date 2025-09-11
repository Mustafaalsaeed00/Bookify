namespace Bookify.Web.Core.ViewModels
{
	public class BookCopyFormViewModel
	{
		public int Id { get; set; }
		public int BookId { get; set; }

		[Range(1,1000 , ErrorMessage = Errors.InvalidRange)]
		[Display(Name = "Edition Number")]
		public int EditionNumber { get; set; }

		[Display(Name = "is available for rental?")]
		public bool IsAvailableForRental { get; set; }
		public bool ShowRentalInput { get; set; }
	}
}
