using Bookify.Web.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bookify.Web.Controllers
{
	public class BooksController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IMapper _mapper;
		public BooksController(ApplicationDbContext context, IMapper mapper)
		{
			_context = context;
			_mapper = mapper;
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
				return View("Form" , PopulateViewModel(model));

			var book = _mapper.Map<Book>(model);

			foreach (int categoryId in model.SelectedCategories)
			{
				book.Categories.Add(new BookCategory { CategoryId = categoryId });
			}
			
			_context.Add(book);
			_context.SaveChanges();

			return RedirectToAction(nameof(Index));
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
	}
}
