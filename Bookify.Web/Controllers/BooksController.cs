using Bookify.Web.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bookify.Web.Controllers
{
	public class BooksController : Controller
	{
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly ApplicationDbContext _context;
		private readonly IMapper _mapper;
		private List<string> _allowedExtensions = new() {".jpg" , ".jpeg", ".png" };
		private int _maxAllowedSize = 2076672;
		public BooksController(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment webHostEnvironment)
		{
			_context = context;
			_mapper = mapper;
			_webHostEnvironment = webHostEnvironment;
		}

		public IActionResult Index()
		{
			return View();
		}

		public IActionResult Create()
		{
			return View("Form" , PopulateViewModel());
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(BookFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			var book = _mapper.Map<Book>(model);

			/*Save Image To images/books file*/

			var result = SaveImageToFile(model);

			if (!result.Success)
			{
				ModelState.AddModelError(nameof(model.Image), result.ErrorMessage);
				return View("Form", PopulateViewModel(model));
			}
			if(result.ImageName is not null)
				book.ImageUrl = result.ImageName;	
			/*Save Image To images/books file*/


			foreach (int categoryId in model.SelectedCategories)
			{
				book.Categories.Add(new BookCategory { CategoryId = categoryId });
			}
			
			_context.Add(book);
			_context.SaveChanges();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult Edit(int id)
		{
			var book = _context.Books.Include(b=>b.Categories).FirstOrDefault(b=>b.Id == id);

			if (book is null)
				return NotFound();

			var model = _mapper.Map<BookFormViewModel>(book!);
			var viewModel = PopulateViewModel(model);

			viewModel.SelectedCategories = book!.Categories.Select(x => x.CategoryId).ToList();
			return View("Form", viewModel);
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(BookFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			var book = _context.Books.Include(b => b.Categories).FirstOrDefault(b => b.Id == model.Id);

			if (book is null)
				return NotFound();

			/*Save Image To images/books file*/

			//Delete Old image from file
			if((!string.IsNullOrEmpty(book.ImageUrl)) && model.Image is not null)
			{
				var oldImagePath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "books", book.ImageUrl);
				if (System.IO.File.Exists(oldImagePath))
					System.IO.File.Delete(oldImagePath);
			}

			//Save new Image

			//handle if user did not add new image
			model.ImageUrl = book.ImageUrl;


			var result = SaveImageToFile(model);

			if (!result.Success)
			{
				ModelState.AddModelError(nameof(model.Image), result.ErrorMessage);
				return View("Form", PopulateViewModel(model));
			}
			if (result.ImageName is not null)
				model.ImageUrl = result.ImageName;


			/*Save Image To images/books file*/


		
			book = _mapper.Map(model, book);

			foreach (int categoryId in model.SelectedCategories)
			{
				book.Categories.Add(new BookCategory { CategoryId = categoryId });
			}

			book.LastUpdatedOn = DateTime.Now;
			_context.SaveChanges();

			return RedirectToAction(nameof(Index));
		}

		public IActionResult AllowItem(BookFormViewModel model)
		{
			var book = _context.Books.SingleOrDefault(b => b.Title == model.Title && b.AuthorId == model.AuthorId);
			var IsAllowed = book is null || book.Id.Equals(model.Id);
			return Json(IsAllowed);
		}


		private BookFormViewModel PopulateViewModel(BookFormViewModel? model = null)
		{
			BookFormViewModel viewModel = model is null ? new BookFormViewModel() : model;

	
			var authors = _context.Authors.Where(a => !a.IsDeleted).OrderBy(a => a.Name).ToList();
			var categories = _context.Categories.Where(c => !c.IsDeleted).OrderBy(c => c.Name).ToList();

			viewModel.Authors = _mapper.Map<IEnumerable<SelectListItem>>(authors);
			viewModel.Categories = _mapper.Map<IEnumerable<SelectListItem>>(categories);
		
			return viewModel;
		}

		private (bool Success , string ErrorMessage , string? ImageName) SaveImageToFile(BookFormViewModel model)
		{
			if (model.Image is null)
				return (true, string.Empty, null);


			string extension = Path.GetExtension(model.Image.FileName);

			if (!_allowedExtensions.Contains(extension))
				return (false, Errors.AllowedExtensions, null);


			if (model.Image.Length > _maxAllowedSize)
				return (false, Errors.MaxSize, null);

			string ImageName = $"{Guid.NewGuid()}{extension}";
			string path = Path.Combine(_webHostEnvironment.WebRootPath,"images" , "books", ImageName);

			using (var stream = System.IO.File.Create(path))
			{
				model.Image.CopyTo(stream);
			}

			return (true, string.Empty, ImageName);
		}
	}
}
