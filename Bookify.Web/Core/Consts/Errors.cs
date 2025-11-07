namespace Bookify.Web.Core.Consts
{
	public static class Errors
	{
		public const string RequiredField = "Required field";
		public const string MaxLength = "Length cannot be more than {1} characters";
		public const string MaxMinLength = "The {0} must be at least {2} and at max {1} characters long.";
		public const string Duplicated = "Another record with the same {0} is already exists!";
		public const string DuplicatedBook = "Book with the same title is already exists with the same author!";
		public const string AllowedExtensions = "Only .jpg, .jpeg , .png files are allowed!";
		public const string MaxSize = "File cannot be more than 2 MB";
		public const string NotAllowFutureDate = "Date cannot be in the future!";
		public const string InvalidRange = "{0} should be between {1} and {2}!";
		public const string PasswordConfirmationNotMatch = "The password and confirmation password do not match.";
		public const string WeakPassword = "Passwords must contain an uppercase character, lowercase character, a digit, and a non-alphanumeric character. Passwords must be at least 8 characters long.";
		public const string InvalidUsername = "Username can only contain letters or digits.";
		public const string OnlyEnglishLetters = "Only English letters are allowed.";
		public const string OnlyArabicLetters = "Only Arabic letters are allowed.";
		public const string OnlyNumbersAndLetters = "Only Arabic/English letters or digits are allowed..";
		public const string DenySpecialCharacters = "Special characters are not allowed..";
		public const string InvalidMobileNumber = "Invalid mobile number.";
		public const string InvalidSerialNumber = "Invalid serial number.";
		public const string InvalidNationalId = "Invalid National ID.";
		public const string NotAvailableRental = "This Book/Copy is not available for rental.";
		public const string BlackListedSubscriber = "This Subscriber is blacklisted.";
		public const string InactiveSubscriber = "This subscriber is Inactive.";
		public const string MaxCopiesReached = "This subscriber has reached the max number of rentals.";
		public const string CopyIsInRental = "This copy is already rentaled.";
		public const string RentalNotAllowedForBlacklisted = "Rental cannot be extended for blacklisted subscriber .";
		public const string RentalNotAllowedForInactive = "Rental cannot be extended for this subscriber before renewal .";
		public const string RentalNotAllowed = "Rental cannot be extended .";
		public const string PenaltyShouldBePaid = "penalty should be paid .";
		public const string NoRentals = "this copy has no rentals.";
	}
}
