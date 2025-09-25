using System.Security.Claims;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Archive)]
	public class AuthorsController : Controller
	{
		private readonly ApplicationDbContext _context ;
		private readonly IMapper _mapper;

		public AuthorsController(ApplicationDbContext context, IMapper mapper = null)
		{
			_context = context;
			_mapper = mapper;
		}

		public IActionResult Index()
		{
			var authors = _context.Authors.AsNoTracking().ToList();
			var authorsViewModel = _mapper.Map<IEnumerable<AuthorViewModel>>(authors);
			return View(authorsViewModel);
		}

		[HttpGet]
		[AjaxOnly]
		public IActionResult Create()
		{
			return PartialView("_Form");
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(AuthorFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var author = _mapper.Map<Author>(model);
			author.CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			_context.Add(author);

			_context.SaveChanges();

			var authorViewModel = _mapper.Map<AuthorViewModel>(author);

			return PartialView("_AuthorRow", authorViewModel);
		}

		[HttpGet]
		[AjaxOnly]
		public IActionResult Edit(int id)
		{
			var author = _context.Authors.Find(id);
			if (author is null)
				return BadRequest();

			var authorFormViewModel = _mapper.Map<AuthorFormViewModel>(author);
			return PartialView("_Form", authorFormViewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(AuthorFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var author = _context.Authors.Find(model.Id);
			if (author is null)
				return BadRequest();

			author = _mapper.Map(model, author);
			author.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			author.LastUpdatedOn = DateTime.Now;

			_context.SaveChanges();

			var authorViewModel = _mapper.Map<AuthorViewModel>(author);

			return PartialView("_AuthorRow", authorViewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult ToggleStatus(int id)
		{
			var author = _context.Authors.Find(id);
			if (author is null)
				return NotFound();

			author.IsDeleted = !author.IsDeleted;
			author.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			author.LastUpdatedOn = DateTime.Now;

			_context.SaveChanges();

			return Ok(author.LastUpdatedOn.ToString());
		}

		public IActionResult AllowItem(AuthorFormViewModel model)
		{
			var author = _context.Authors.SingleOrDefault(c => c.Name == model.Name);
			var IsAllowed = author is null || author.Id.Equals(model.Id);
			return Json(IsAllowed);
		}

	}
}
