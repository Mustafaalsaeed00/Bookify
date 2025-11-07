
using Bookify.Web.Core.Models;
using Bookify.Web.Services;
using Hangfire;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using WhatsAppCloudApi;
using WhatsAppCloudApi.Services;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Reception)]
	public class SubscribersController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly IMapper _mapper;
		private readonly IDataProtector _dataProtector;
		private readonly IWhatsAppClient _whatsAppClient;
		private readonly IImageService _imageService;
		private readonly IEmailBodyBuilder _emailBodyBuilder;
		private readonly IEmailSender _emailSender;
		public SubscribersController(ApplicationDbContext context,
			IWebHostEnvironment webHostEnvironment,
			IMapper mapper, IDataProtectionProvider dataProtector,
			IWhatsAppClient whatsAppClient, IImageService imageService, IEmailBodyBuilder emailBodyBuilder, IEmailSender emailSender)
		{
			_context = context;
			_webHostEnvironment = webHostEnvironment;
			_mapper = mapper;
			_dataProtector = dataProtector.CreateProtector("SecureKey");
			_whatsAppClient = whatsAppClient;
			_imageService = imageService;
			_emailBodyBuilder = emailBodyBuilder;
			_emailSender = emailSender;
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
			if(subscriber is not null)
				viewModel.Key = _dataProtector.Protect(subscriber.Id.ToString());

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

			var imageName = $"{Guid.NewGuid()}{Path.GetExtension(model.Image!.FileName)}";
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

			Subscription subscription = new()
			{
				CreatedById = subscriber.CreatedById,
				CreatedOn = subscriber.CreatedOn,
				StartDate = DateTime.Today,
				EndDate = DateTime.Today.AddYears(1),
			};

			subscriber.Subscriptions.Add(subscription);

			_context.Subscribers.Add(subscriber);
			_context.SaveChanges();


		
			// Send welcome email

			
			var placeholders = new Dictionary<string, string>()
				{
					{"imageUrl","https://res.cloudinary.com/mustafabookify/image/upload/v1759580827/icon-positive-vote-2_jccgi4.png"},
					{"header",$"Welcome {subscriber.FirstName}"},
					{"body","thanks for joining Bookify 🤩"},
				};

			var body = _emailBodyBuilder.GetEmailBody(EmailTemplates.Notification, placeholders);


			BackgroundJob.Enqueue(() => _emailSender.SendEmailAsync(
				subscriber.Email,
				"welcome to Bookify", body));


			//Send welcome message using whatsApp

			if (model.HasWhatsApp)
			{
				var components = new List<WhatsAppComponent>()
				{
					new WhatsAppComponent
					{
						Type = "body",
						Parameters = new List<object>()
						{
							new WhatsAppTextParameter
							{
								Text = subscriber.FirstName
							}
						}
					}
				};

				var mobileNumber = _webHostEnvironment.IsDevelopment() ? "966501642434" : subscriber.MobileNumber;

				BackgroundJob.Enqueue(() => _whatsAppClient.SendMessage(mobileNumber, WhatsAppLanguageCode.English_US, WhatsAppTemplates.NewSubscriberNotice, components));
			}

			string subscriberId = _dataProtector.Protect(subscriber.Id.ToString());

			return RedirectToAction(nameof(Details) , new {id= subscriberId });
		}

		public IActionResult Edit(string id)
		{
			int subsciberId = int.Parse(_dataProtector.Unprotect(id));

			var subscriber = _context.Subscribers.Find(subsciberId);
			if (subscriber is null)
				return NotFound();

			var viewModel = _mapper.Map<SubscriberFormViewModel>(subscriber);
			viewModel.Key = id;

			return View("Form", PopulateViewModel(viewModel));
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public async Task<IActionResult> Edit(SubscriberFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", PopulateViewModel(model));

			int subscriberId = int.Parse(_dataProtector.Unprotect(model.Key!));

			var subscriber = _context.Subscribers.Find(subscriberId);

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

			string subscriberKey = _dataProtector.Protect(subscriber.Id.ToString());

			return RedirectToAction(nameof(Details), new { id = subscriberKey});
		}

		public IActionResult Details(string id)
		{
			int subscriberId = int.Parse(_dataProtector.Unprotect(id));

			var subscriber = _context.Subscribers
				.Include(s => s.Governorate)
				.Include(s => s.Area)
				.Include(s => s.Subscriptions)
				.Include(s => s.Rentals)
				.ThenInclude(r => r.RentalCopies)
				.SingleOrDefault(s=> s.Id == subscriberId);

			if (subscriber is null)
				return NotFound();

			var viewModel = _mapper.Map<SubscriberViewModel>(subscriber);
			viewModel.Key = id;

			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult RenewSubscription(string sKey)
		{
			var subscriberId = int.Parse(_dataProtector.Unprotect(sKey));

			var subscriber = _context.Subscribers.Include(s => s.Subscriptions).SingleOrDefault(s => s.Id == subscriberId);

			if (subscriber is null)
				return NotFound();

			if (subscriber.IsBlackListed)
				return BadRequest();

			var lastSubscription = subscriber.Subscriptions.Last();
			var startDate = DateTime.Today > lastSubscription.EndDate ? DateTime.Today : lastSubscription.EndDate.AddDays(1) ;
			Subscription newSubscription = new()
			{
				CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value,
				CreatedOn = DateTime.Now,
				StartDate = startDate,
				EndDate = startDate.AddYears(1)
			};

			subscriber.Subscriptions.Add(newSubscription);
			_context.SaveChanges();

			// Send welcome email


			var placeholders = new Dictionary<string, string>()
				{
					{"imageUrl","https://res.cloudinary.com/mustafabookify/image/upload/v1759580827/icon-positive-vote-2_jccgi4.png"},
					{"header",$"Hello {subscriber.FirstName},"},
					{"body",$"your subscription has been renewed through {newSubscription.EndDate.ToString("dd MMM, yyyy")} 🥳🥳"},
				};

			var body = _emailBodyBuilder.GetEmailBody(EmailTemplates.Notification, placeholders);

			BackgroundJob.Enqueue(() => _emailSender.SendEmailAsync(
				subscriber.Email,
				"Bookify Subscription Renewal", body));
			
			//Send welcome message using whatsApp

			if (subscriber.HasWhatsApp)
			{
				var components = new List<WhatsAppComponent>()
				{
					new WhatsAppComponent
					{
						Type = "body",
						Parameters = new List<object>()
						{
							new WhatsAppTextParameter{Text = subscriber.FirstName},
							new WhatsAppTextParameter{Text = newSubscription.EndDate.ToString("dd MMM, yyyy")}
						}
					}
				};

				var mobileNumber = _webHostEnvironment.IsDevelopment() ? "966501642434" : subscriber.MobileNumber;

				BackgroundJob.Enqueue(() => _whatsAppClient.SendMessage(mobileNumber, WhatsAppLanguageCode.English_US, WhatsAppTemplates.SubscriptionRenew, components));
			}

			var viewModel = _mapper.Map<SubscriptionViewModel>(newSubscription);

			return PartialView("_SubscriptionRow", viewModel);
		}

		[AjaxOnly]
		public IActionResult GetAreas(int governorateId)
		{
			var areas = _context.Areas
				.Where(a => a.GovernorateId == governorateId);

			return Ok(_mapper.Map<IEnumerable<SelectListItem>>(areas));
		}

		public IActionResult AllowNationalId(SubscriberFormViewModel model)
		{
			int subscriberId = 0;
			if (!string.IsNullOrEmpty(model.Key))
				subscriberId = int.Parse(_dataProtector.Unprotect(model.Key));
			var subscriber = _context.Subscribers.SingleOrDefault(s => s.NationalId == model.NationalId);
			var isAllowed = subscriber is null || subscriber.Id.Equals(subscriberId); 
			return Json(isAllowed);
		}
		public IActionResult AllowMobileNumber(SubscriberFormViewModel model)
		{
			int subscriberId = 0;
			if (!string.IsNullOrEmpty(model.Key))
				subscriberId = int.Parse(_dataProtector.Unprotect(model.Key));
			var subscriber = _context.Subscribers.SingleOrDefault(s => s.MobileNumber == model.MobileNumber);
			var isAllowed = subscriber is null || subscriber.Id.Equals(subscriberId);
			return Json(isAllowed);
		}
		public IActionResult AllowEmail(SubscriberFormViewModel model)
		{
			int subscriberId = 0;
			if (!string.IsNullOrEmpty(model.Key))
				subscriberId = int.Parse(_dataProtector.Unprotect(model.Key));
			var subscriber = _context.Subscribers.SingleOrDefault(s => s.Email.ToUpper() == model.Email.ToUpper());
			var isAllowed = subscriber is null || subscriber.Id.Equals(subscriberId);
			return Json(isAllowed);
		}


		private SubscriberFormViewModel PopulateViewModel(SubscriberFormViewModel model = null)
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

	}
}
