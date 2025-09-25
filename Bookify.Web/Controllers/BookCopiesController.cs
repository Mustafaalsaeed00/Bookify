using Bookify.Web.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Archive)]
	public class BookCopiesController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IMapper _mapper;
		public BookCopiesController(ApplicationDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
		}

		public IActionResult Index()
		{
			return View();
		}
		[AjaxOnly]
		public IActionResult Create(int bookId)
		{
			var book = _context.Books.Find(bookId);
			if (book is null)
				return NotFound();

			var viewModel = new BookCopyFormViewModel { BookId = bookId , ShowRentalInput = book!.IsAvailableForRental};
			return PartialView("Form" , viewModel);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(BookCopyFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var book = _context.Books.Find(model.BookId);
			if (book is null)
				return NotFound();

			var bookCopy = _mapper.Map<BookCopy>(model);
			bookCopy.IsAvailableForRental = book.IsAvailableForRental && model.IsAvailableForRental;
			bookCopy.CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.Add(bookCopy);
			_context.SaveChanges();

			var viewModel = _mapper.Map<BookCopyViewModel>(bookCopy);

			return PartialView("_BookCopyRow" , viewModel);
		}

		[AjaxOnly]
		public IActionResult Edit(int id)
		{
			var bookCopy = _context.BookCopies.Include(c=>c.Book).SingleOrDefault(c=> c.Id == id);
			if (bookCopy is null)
				return NotFound();

			var viewModel = _mapper.Map<BookCopyFormViewModel>(bookCopy);
			viewModel.ShowRentalInput = bookCopy.Book!.IsAvailableForRental;

			return PartialView("Form", viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(BookCopyFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var bookCopy = _context.BookCopies.Include(c => c.Book).SingleOrDefault(c => c.Id == model.Id);
			if (bookCopy is null)
				return NotFound();

			bookCopy.EditionNumber = model.EditionNumber;
			bookCopy.IsAvailableForRental = model.IsAvailableForRental && bookCopy.Book!.IsAvailableForRental;
			bookCopy.LastUpdatedOn = DateTime.Now;
			bookCopy.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.SaveChanges();

			var viewModel = _mapper.Map<BookCopyViewModel>(bookCopy);

			return PartialView("_BookCopyRow", viewModel);
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ToggleStatus(int id)
		{
			var bookCopy = _context.BookCopies.Find(id);
			if (bookCopy is null)
				return NotFound();

			bookCopy.IsDeleted = !bookCopy.IsDeleted;
			bookCopy.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			bookCopy.LastUpdatedOn = DateTime.Now;
			_context.SaveChanges();
			return Ok();
		}
	}
}
