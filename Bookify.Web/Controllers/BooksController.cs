using Bookify.Web.Core.Models;
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

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Archive)]
	public class BooksController : Controller
	{
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly ApplicationDbContext _context;
		private readonly IMapper _mapper;
		private readonly Cloudinary _cloudinary;
		private List<string> _allowedExtensions = new() {".jpg" , ".jpeg", ".png" };
		private int _maxAllowedSize = 2076672;
		public BooksController(ApplicationDbContext context, IMapper mapper, IWebHostEnvironment webHostEnvironment, IOptions<CloudinarySettings> cloudinary)
		{
			_context = context;
			_mapper = mapper;
			_webHostEnvironment = webHostEnvironment;
			Account account = new()
			{
				Cloud = cloudinary.Value.Cloud,
				ApiKey = cloudinary.Value.ApiKey,
				ApiSecret = cloudinary.Value.ApiSecret,
			};



			_cloudinary = new Cloudinary(account);
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


			/*Save Image To images/books file*/

			var saveMode = SaveMode.File;

			var result = await SaveImageToFile(model , saveMode);

			if (!result.Success)
			{
				ModelState.AddModelError(nameof(model.Image), result.ErrorMessage);
				return View("Form", PopulateViewModel(model));
			}
			if (result.ImageUrl is null)
			{
				model.ImageUrl = $"/images/books/{result.ImageName}";
				model.ImageThumbnailUrl = $"/images/books/thumb/{result.ImageName}";
			}
			else
			{
				model.ImageUrl = result.ImageUrl;

			}
			/*Save Image To images/books file*/

			var book = _mapper.Map<Book>(model);
			if(saveMode == SaveMode.Cloud)
			{
				book.ImageThumbnailUrl = GetThumbnailUrl(result.ImageUrl);
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
				var oldImagePath = $"{_webHostEnvironment.WebRootPath}{book.ImageUrl}";
				var oldThumbPath = $"{_webHostEnvironment.WebRootPath}{book.ImageThumbnailUrl}";


				if (System.IO.File.Exists(oldImagePath))
					System.IO.File.Delete(oldImagePath);

				if (System.IO.File.Exists(oldThumbPath))
					System.IO.File.Delete(oldThumbPath);

				//await _cloudinary.DeleteResourcesAsync(book.ImagePublicId);
			}

			//Save new Image

			//handle if user did not add new image
			model.ImageUrl = book.ImageUrl;
			model.ImageThumbnailUrl = book.ImageThumbnailUrl;


			var saveMode = SaveMode.File;

			var result = await SaveImageToFile(model, saveMode);

			if (!result.Success)
			{
				ModelState.AddModelError(nameof(model.Image), result.ErrorMessage);
				return View("Form", PopulateViewModel(model));
			}
			if (result.ImageUrl is null && result.ImageName is not null)
			{
				model.ImageUrl = $"/images/books/{result.ImageName}";
				model.ImageThumbnailUrl = $"/images/books/thumb/{result.ImageName}";
			}
			else if(result.ImageUrl is not null)
			{
				model.ImageUrl = result.ImageUrl;
			}


			/*Save Image To images/books file*/



			book = _mapper.Map(model, book);
			if(saveMode == SaveMode.Cloud)
			{
				book.ImageThumbnailUrl = GetThumbnailUrl(result.ImageUrl);
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

		public enum SaveMode { File , Cloud};

		private async Task<(bool Success , string ErrorMessage , string? ImageName ,string? ImageUrl,string? ImagePublicId)> SaveImageToFile(BookFormViewModel model , SaveMode saveMode)
		{
			if (model.Image is null)
				return (true, string.Empty, null , null , null);


			string extension = Path.GetExtension(model.Image.FileName);

			if (!_allowedExtensions.Contains(extension))
				return (false, Errors.AllowedExtensions, null,null, null);


			if (model.Image.Length > _maxAllowedSize)
				return (false, Errors.MaxSize, null,null, null);

			string ImageName = $"{Guid.NewGuid()}{extension}";
			var ImageUrl = "";
			var ImagePublicId = "";
			switch (saveMode)
			{
				case SaveMode.File:
					{
						string path = Path.Combine(_webHostEnvironment.WebRootPath, "images", "books", ImageName);
						string thumbPath = Path.Combine(_webHostEnvironment.WebRootPath, "images", "books", "thumb", ImageName);
						using var stream = System.IO.File.Create(path);
						await model.Image.CopyToAsync(stream);
						stream.Dispose();

						using var image = Image.Load(model.Image.OpenReadStream());
						var ratio = (float)image.Width / 200;
						var height = image.Height / ratio;
						image.Mutate(i => i.Resize(width: 200, height: (int)height));
						image.Save(thumbPath);

						ImageUrl = null;
						ImagePublicId = null;
					}
					break;
				case SaveMode.Cloud:
					{
						using var stream = model.Image.OpenReadStream();

						var imageParams = new ImageUploadParams
						{
							File = new FileDescription(ImageName, stream),
							UseFilename = true
						};
						var result = await _cloudinary.UploadAsync(imageParams);

						ImageUrl = result.SecureUrl.ToString();

						ImagePublicId = result.PublicId;
					}
					break;
					
			}
			
			return (true, string.Empty, ImageName , ImageUrl , ImagePublicId);
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
