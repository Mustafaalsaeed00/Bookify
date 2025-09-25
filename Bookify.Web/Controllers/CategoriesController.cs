using Bookify.Web.Core.Models;
using System.Security.Claims;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Archive)]
	public class CategoriesController(ApplicationDbContext context, IMapper mapper) : Controller
	{
		private readonly ApplicationDbContext _context = context;
		private readonly IMapper _mapper = mapper;

		public IActionResult Index()
		{
			var categories = _context.Categories.AsNoTracking().ToList();
			var categoryViewModel = _mapper.Map<IEnumerable<CategoryViewModel>>(categories);

			return View(categoryViewModel);
		}

		[HttpGet]
		[AjaxOnly]
		public IActionResult Create()
		{
			return PartialView("_Form");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(CategoryFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var category = _mapper.Map<Category>(model);
			category.CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			_context.Add(category);

			_context.SaveChanges();

			var categoryViewModel = _mapper.Map<CategoryViewModel>(category);

			return PartialView("_CategoryRow", categoryViewModel);
		}

		[HttpGet]
		[AjaxOnly]
		public IActionResult Edit(int id)
		{
			var category = _context.Categories.Find(id);
			if (category is null)
				return BadRequest();

			var categoryFormViewModel = _mapper.Map<CategoryFormViewModel>(category);
			return PartialView("_Form", categoryFormViewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(CategoryFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var category = _context.Categories.Find(model.Id);
			if (category is null)
				return BadRequest();

			category = _mapper.Map(model, category);
			category.LastUpdatedOn = DateTime.Now;
			category.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.SaveChanges();

			var categoryViewModel = _mapper.Map<CategoryViewModel>(category);

			return PartialView("_CategoryRow", categoryViewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ToggleStatus(int id)
		{
			var category = _context.Categories.Find(id);
			if (category is null)
				return NotFound();

			category.IsDeleted = !category.IsDeleted;
			category.LastUpdatedOn = DateTime.Now;
			category.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.SaveChanges();

			return Ok(category.LastUpdatedOn.ToString());
		}

		public IActionResult AllowItem(CategoryFormViewModel model)
		{
			var category = _context.Categories.SingleOrDefault(c => c.Name == model.Name);
			var IsAllowed = category is null || category.Id.Equals(model.Id);
			return Json(IsAllowed);
		}
	}
}
