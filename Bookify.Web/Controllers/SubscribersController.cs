
using Bookify.Web.Core.Models;
using Bookify.Web.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Reception)]
	public class SubscribersController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IImageService _imageService;
		private readonly IMapper _mapper;
		public SubscribersController(ApplicationDbContext context, IImageService imageService, IMapper mapper)
		{
			_context = context;
			_imageService = imageService;
			_mapper = mapper;
		}

		public IActionResult Index()
		{
			return View();
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Search(SearchFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest(ModelState);

			var subscriber = _context.Subscribers
				.SingleOrDefault(s => s.MobileNumber == model.Value || s.NationalId == model.Value || s.Email == model.Value);

			var viewModel = _mapper.Map<SubscriberSearchResultViewModel>(subscriber);

			return PartialView("_Result", viewModel);
		}

		public IActionResult Create()
		{
			return View("Form", PopulateViewModel());
		}
		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Create(SubscriberFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			var subscriber = _mapper.Map<Subscriber>(model);

			var imageName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
			var imagePath = "/images/subscribers";
			var result = await _imageService.UploadAsync(model.Image, imageName, imagePath, hasThumbnail: true);

			if (!result.isUploaded)
			{
				ModelState.AddModelError("Image", result.errorMessage!);
				return View("Form", PopulateViewModel(model));
			}

			subscriber.ImageUrl = $"{imagePath}/{imageName}" ;
			subscriber.ImageThumbnailUrl = $"{imagePath}/thumb/{imageName}";
			subscriber.CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			subscriber.CreatedOn = DateTime.Now;

			_context.Add(subscriber);
			_context.SaveChanges();

			return RedirectToAction(nameof(Index) , new {subscriber.Id});
		}

		public IActionResult Edit(int id)
		{
			var subscriber = _context.Subscribers.Find(id);
			if (subscriber is null)
				return NotFound();

			var viewModel = _mapper.Map<SubscriberFormViewModel>(subscriber);

			return View("Form", PopulateViewModel(viewModel));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(SubscriberFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			var subscriber = _context.Subscribers.Find(model.Id);

			if (subscriber is null)
				return NotFound();

			if(model.Image is not null)
			{
				string imageName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image.FileName)}";
				string imagePath = $"/images/subscribers";

				var result = await _imageService.UploadAsync(model.Image, imageName , imagePath , hasThumbnail: true);

				if(!result.isUploaded)
				{
					ModelState.AddModelError("Image", result.errorMessage!);
					return View("Form", PopulateViewModel(model));
				}

				await _imageService.DeleteAsync(subscriber.ImageUrl, subscriber.ImageThumbnailUrl);

				model.ImageUrl = $"{imagePath}/{imageName}";
				model.ImageThumbnailUrl = $"{imagePath}/thumb/{imageName}";
			}
			else if (!string.IsNullOrEmpty(subscriber.ImageUrl))
			{
				model.ImageUrl = subscriber.ImageUrl;
				model.ImageThumbnailUrl = subscriber.ImageThumbnailUrl;
			}

			subscriber = _mapper.Map(model, subscriber);
			subscriber.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			subscriber.LastUpdatedOn = DateTime.Now;

			_context.SaveChanges();

			return RedirectToAction(nameof(Index), new { id = subscriber.Id });
		}

		public IActionResult Details(int id)
		{
			var subscriber = _context.Subscribers
				.Include(s => s.Governorate)
				.Include(s => s.Area)
				.SingleOrDefault(s=> s.Id == id);

			if (subscriber is null)
				return NotFound();

			var viewModel = _mapper.Map<SubscriberDetailsViewModel>(subscriber);

			return View(viewModel);
		}

		[AjaxOnly]
		public IActionResult GetAreas(int governorateId)
		{
			var areas = _context.Areas
				.Where(a => a.GovernorateId == governorateId);

			return Ok(_mapper.Map<IEnumerable<SelectListItem>>(areas));
		}

		public SubscriberFormViewModel PopulateViewModel(SubscriberFormViewModel model = null)
		{
			SubscriberFormViewModel viewModel = model is null ? new SubscriberFormViewModel() : model;

			var governorate = _context.Governorates
				.Where(g => !g.IsDeleted)
				.OrderBy(g => g.Name)
				.ToList();
			viewModel.Governorates = _mapper.Map<IEnumerable<SelectListItem>>(governorate);

			if (model?.GovernorateId > 0)
			{
				var areas = _context.Areas
				.Where(a => a.GovernorateId == model.GovernorateId && !a.IsDeleted)
				.OrderBy(g => g.Name)
				.ToList();
				viewModel.Areas = _mapper.Map<IEnumerable<SelectListItem>>(areas);
			}

			return viewModel;
		}

		public IActionResult AllowNationalId(SubscriberFormViewModel model)
		{
			var subscriber = _context.Subscribers.SingleOrDefault(s => s.NationalId == model.NationalId);
			var isAllowed = subscriber is null || subscriber.Id.Equals(model.Id); 
			return Json(isAllowed);
		}
		public IActionResult AllowMobileNumber(SubscriberFormViewModel model)
		{
			var subscriber = _context.Subscribers.SingleOrDefault(s => s.MobileNumber == model.MobileNumber);
			var isAllowed = subscriber is null || subscriber.Id.Equals(model.Id);
			return Json(isAllowed);
		}
		public IActionResult AllowEmail(SubscriberFormViewModel model)
		{
			var subscriber = _context.Subscribers.SingleOrDefault(s => s.Email.ToUpper() == model.Email.ToUpper());
			var isAllowed = subscriber is null || subscriber.Id.Equals(model.Id);
			return Json(isAllowed);
		}
	}
}
