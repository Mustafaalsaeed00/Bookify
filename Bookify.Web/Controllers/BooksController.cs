using Bookify.Web.Core.Consts;
using Bookify.Web.Core.Models;
using Bookify.Web.Services;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.Options;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Security.Claims;
using System.Threading.Tasks;
using static System.Reflection.Metadata.BlobBuilder;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Archive)]
	public class BooksController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IMapper _mapper;
		private readonly IImageService _imageService;

		public BooksController(ApplicationDbContext context, IMapper mapper, IImageService imageService)
		{
			_context = context;
			_mapper = mapper;
			_imageService = imageService;
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpPost]
		public IActionResult GetBooks()
		{
			var skip = int.Parse(Request.Form["start"]!);
			var pageSize = int.Parse(Request.Form["length"]!);

			var searchValue = Request.Form["search[value]"];

			var sortColumnIndex = Request.Form["order[0][column]"];
			var sortColumn = Request.Form[$"columns[{sortColumnIndex}][name]"];
			var sortColumnDirection = Request.Form["order[0][dir]"];

			IQueryable<Book> books = _context.Books
				.Include(b=>b.Author)
				.Include(b=> b.Categories)
				.ThenInclude(c=>c.Category);

			if(!string.IsNullOrEmpty(searchValue))
				books = books.Where(b => b.Title.Contains(searchValue!) || b.Author!.Name.Contains(searchValue!));

			books = books.OrderBy($"{sortColumn} {sortColumnDirection}");

			var data = books.Skip(skip).Take(pageSize).ToList();

			var mappedData = _mapper.Map<IEnumerable<BookViewModel>>(data);

			var recordsTotal = books.Count();

			var jsonData = new { recordsFiltered = recordsTotal, recordsTotal = recordsTotal, data = mappedData };

			return Ok(jsonData);
		}

		public IActionResult Details(int id)
		{
			var book = _context.Books
				.Include(b=> b.Author)
				.Include(b=> b.Copies)
				.Include(b=> b.Categories)
				.ThenInclude(c=> c.Category)
				.SingleOrDefault(b => b.Id == id);

			if (book is null)
				return NotFound();

			var viewModel = _mapper.Map<BookViewModel>(book);
				
			return View(viewModel);
		}

		public IActionResult Create()
		{
			return View("Form" , PopulateViewModel());
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(BookFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			(bool isUploaded, string? errorMessage, string? imageUrl, string? ImagePublicId) result = new();
			/*Save Image To images/books file*/
			if (model.Image is not null)
			{
				string extension = Path.GetExtension(model.Image.FileName);
				string ImageName = $"{Guid.NewGuid()}{extension}";

				result = await _imageService.UploadAsync(model.Image , ImageName , "/images/books" , true);

				if (!result.isUploaded)
				{
					ModelState.AddModelError(nameof(model.Image), result.errorMessage!);
					return View("Form", PopulateViewModel(model));
				}

				if(result.imageUrl is null && result.ImagePublicId is null)
				{
					model.ImageUrl = $"/images/books/{ImageName}";
					model.ImageThumbnailUrl = $"/images/books/thumb/{ImageName}";
				}
				else
				{
					model.ImageUrl = result.imageUrl;
					model.ImageThumbnailUrl = GetThumbnailUrl(result.imageUrl!);
				}
			}


		
			/*Save Image To images/books file*/

			var book = _mapper.Map<Book>(model);

			if(result.ImagePublicId is not null)
			{
				book.ImagePublicId = result.ImagePublicId;
			}

			foreach (int categoryId in model.SelectedCategories)
			{
				book.Categories.Add(new BookCategory { CategoryId = categoryId });
			}

			book.CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.Add(book);
			_context.SaveChanges();

			return RedirectToAction(nameof(Details) , new { id = book.Id});
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
		public async Task<IActionResult> Edit(BookFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			var book = _context.Books
				.Include(b => b.Categories)
				.Include(b => b.Copies)
				.FirstOrDefault(b => b.Id == model.Id);

			if (book is null)
				return NotFound();

			/*Save Image To images/books file*/

			//Delete Old image from file
			if((!string.IsNullOrEmpty(book.ImageUrl)) && model.Image is not null)
			{
				await _imageService.DeleteAsync(book.ImageUrl, imageThumbnail: true);
			}

			//Save new Image

			//handle if user did not add new image

			(bool isUploaded, string? errorMessage, string? imageUrl, string? ImagePublicId) result = new();

			string extension = Path.GetExtension(model.Image.FileName);
			string ImageName = $"{Guid.NewGuid()}{extension}";

			result = await _imageService.UploadAsync(model.Image, ImageName, "/images/books", true);

			if (!result.isUploaded)
			{
				ModelState.AddModelError(nameof(model.Image), result.errorMessage!);
				return View("Form", PopulateViewModel(model));
			}

			if (result.imageUrl is null && result.ImagePublicId is null)
			{
				model.ImageUrl = $"/images/books/{ImageName}";
				model.ImageThumbnailUrl = $"/images/books/thumb/{ImageName}";
			}
			else
			{
				model.ImageUrl = result.imageUrl;
				model.ImageThumbnailUrl = GetThumbnailUrl(result.imageUrl!);
			}
			/*Save Image To images/books file*/



			book = _mapper.Map(model, book);
			if (result.ImagePublicId is not null)
			{
				book.ImagePublicId = result.ImagePublicId;
			}

			foreach (int categoryId in model.SelectedCategories)
			{
				book.Categories.Add(new BookCategory { CategoryId = categoryId });
			}

			book.LastUpdatedOn = DateTime.Now;
			book.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			if(!book.IsAvailableForRental)
			{
				foreach (var copy in book.Copies)
					copy.IsAvailableForRental = false;
			}
			_context.SaveChanges();

			return RedirectToAction(nameof(Details), new { id = book.Id });
		}


		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ToggleStatus(int id)
		{
			var book = _context.Books.Find(id);
			if (book is null)
				return NotFound();

			book.IsDeleted = !book.IsDeleted;
			book.LastUpdatedOn = DateTime.Now;
			book.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.SaveChanges();

			return Ok();
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
		private string GetThumbnailUrl(string url)
		{
			var separator = "image/upload/";
			var urlParts = url.Split(separator);
			var thumnailUrl = $"{urlParts[0]}{separator}c_thumb,w_200,g_face/{urlParts[1]}";
			return thumnailUrl;
		}
	}
}
