using Bookify.Web.Core.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace Bookify.Web.Controllers
{
	[Authorize(Roles = AppRoles.Reception)]
	public class RentalsController : Controller
	{
		private readonly ApplicationDbContext _context;
		private readonly IDataProtector _dataProtector;
		private readonly IMapper _mapper;

		public RentalsController(ApplicationDbContext context, IDataProtectionProvider dataProtector, IMapper mapper)
		{
			_context = context;
			_dataProtector = dataProtector.CreateProtector("SecureKey");
			_mapper = mapper;
		}

		public IActionResult Create(string sKey)
		{
			var subscriberId = int.Parse(_dataProtector.Unprotect(sKey));
			var subscriber = _context.Subscribers
				.Include(s => s.Subscriptions)
				.Include(s => s.Rentals)
				.ThenInclude(r => r.RentalCopies).SingleOrDefault(s => s.Id == subscriberId);

			if (subscriber is null)
				return NotFound();

			var (errorMessage, maxAllowedCopies) = ValidateSubscriber(subscriber);

			if (!string.IsNullOrEmpty(errorMessage))
				return View("NotAllowedRental", errorMessage);

			var viewModel = new RentalFormViewModel
			{
				SubscriberKey = sKey,
				MaxAllowedCopies = maxAllowedCopies
			};

			return View("Form",viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Create(RentalFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", model);

			var subscriberId = int.Parse(_dataProtector.Unprotect(model.SubscriberKey));
			var subscriber = _context.Subscribers
				.Include(s => s.Subscriptions)
				.Include(s => s.Rentals)
				.ThenInclude(r => r.RentalCopies).SingleOrDefault(s => s.Id == subscriberId);

			if (subscriber is null)
				return NotFound();

			var (errorMessage, maxAllowedCopies) = ValidateSubscriber(subscriber);

			if (!string.IsNullOrEmpty(errorMessage))
				return View("NotAllowedRental", errorMessage);


			var (rentalsError, copies) = ValidateCopies(model.SelectedCopies, subscriberId);

			if (!rentalsError.IsNullOrEmpty())
				return View("NotAllowedRental", rentalsError);

			Rental rental = new()
			{
				RentalCopies = copies,
				CreatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value
			};

			subscriber.Rentals.Add(rental);
			_context.SaveChanges();

			return RedirectToAction(nameof(Details), new {id = rental.Id});
		}

		public IActionResult Edit(int id)
		{
			var rental = _context.Rentals
				.Include(r => r.RentalCopies)
				.ThenInclude(c => c.BookCopy)
				.SingleOrDefault(r => r.Id == id);

			if (rental is null || rental.CreatedOn.Date != DateTime.Today)
				return NotFound();

			var subscriber = _context.Subscribers
				.Include(s => s.Subscriptions)
				.Include(s => s.Rentals)
				.ThenInclude(r => r.RentalCopies).SingleOrDefault(s => s.Id == rental.SubscriberId);

			var (errorMessage, maxAllowedCopies) = ValidateSubscriber(subscriber!,rental.Id);

			if (!string.IsNullOrEmpty(errorMessage))
				return View("NotAllowedRental", errorMessage);

			var currentCopiesIds = rental.RentalCopies.Select(c => c.BookCopyId).ToList();
			var currentCopies = _context.BookCopies
				.Where(c => currentCopiesIds.Contains(c.Id))
				.Include(c => c.Book);

			var viewModel = new RentalFormViewModel
			{
				SubscriberKey = _dataProtector.Protect(subscriber!.Id.ToString()),
				MaxAllowedCopies = maxAllowedCopies,
				CurrentCopies = _mapper.Map<IEnumerable<BookCopyViewModel>>(currentCopies)
			};

			return View("Form", viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Edit(RentalFormViewModel model)
		{
			if (!ModelState.IsValid)
				return View("Form", model);

			var rental = _context.Rentals
				.Include(r => r.RentalCopies)
				.SingleOrDefault(r => r.Id == model.Id);

			if (rental is null || rental.CreatedOn.Date != DateTime.Today)
				return NotFound();

			var subscriberId = int.Parse(_dataProtector.Unprotect(model.SubscriberKey));

			var subscriber = _context.Subscribers
				.Include(s => s.Subscriptions)
				.Include(s => s.Rentals)
				.ThenInclude(r => r.RentalCopies).SingleOrDefault(s => s.Id == subscriberId);

			var (errorMessage, maxAllowedCopies) = ValidateSubscriber(subscriber! , model.Id);

			if (!string.IsNullOrEmpty(errorMessage))
				return View("NotAllowedRental", errorMessage);

			var (rentalsError, copies) = ValidateCopies(model.SelectedCopies , subscriberId , rental.Id);

			if (!rentalsError.IsNullOrEmpty())
				return View("NotAllowedRental", rentalsError);
			

			rental.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
			rental.LastUpdatedOn = DateTime.Now;
			rental.RentalCopies = copies!;

			_context.SaveChanges();

			return RedirectToAction(nameof(Details), new { id = rental.Id });
		}

		public IActionResult Return(int id)
		{
			var rental = _context.Rentals
				.Include(r => r.RentalCopies)
				.ThenInclude(c => c.BookCopy)
				.ThenInclude(bc => bc!.Book)
				.SingleOrDefault(r => r.Id == id);

			if (rental is null || rental.CreatedOn.Date == DateTime.Today)
				return NotFound();

			var subscriber = _context.Subscribers
				.Include(s => s.Subscriptions)
				.SingleOrDefault(s => s.Id == rental.SubscriberId);

			var viewModel = new ReturnFormViewModel
			{
				Id = id,
				Copies = _mapper.Map<IList<RentalCopyViewModel>>(rental.RentalCopies.Where(c => !c.ReturnDate.HasValue)),
				SelectedCopies = rental.RentalCopies.Select(c => new ReturnCopyViewModel { Id = c.BookCopyId , IsReturned = c.ExtendedOn.HasValue ? false : null }).ToList(),
				AllowExtend = !subscriber!.IsBlackListed && subscriber.Subscriptions.Last().EndDate >= rental.StartDate.AddDays((int)RentalsConfigurations.MaxRentalDuration)
				&& rental.StartDate.AddDays((int)RentalsConfigurations.RentalDuration) >= DateTime.Today
			};
			return View(viewModel);
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult Return(ReturnFormViewModel model)
		{
			var rental = _context.Rentals
				.Include(r => r.RentalCopies)
				.ThenInclude(c => c.BookCopy)
				.ThenInclude(bc => bc!.Book)
				.SingleOrDefault(r => r.Id == model.Id);

			if (rental is null || rental.CreatedOn.Date == DateTime.Today)
				return NotFound();

			if(!ModelState.IsValid)
			{
				model.Copies = _mapper.Map<IList<RentalCopyViewModel>>(rental.RentalCopies.Where(c => !c.ReturnDate.HasValue));
				return View(model);
			}
			var subscriber = _context.Subscribers
				.Include(s => s.Subscriptions)
				.SingleOrDefault(s => s.Id == rental.SubscriberId);

			if(model.SelectedCopies.Any(c => c.IsReturned.HasValue && !c.IsReturned.Value))
			{
					string error = subscriber!.IsBlackListed ? Errors.RentalNotAllowedForBlacklisted
					   			: subscriber.Subscriptions.Last().EndDate < rental.StartDate.AddDays((int)RentalsConfigurations.MaxRentalDuration) ? Errors.RentalNotAllowedForInactive
					   			: rental.StartDate.AddDays((int)RentalsConfigurations.RentalDuration) < DateTime.Today ? Errors.RentalNotAllowed : string.Empty;

					if(!error.IsNullOrEmpty())
					{
						model.Copies = _mapper.Map<IList<RentalCopyViewModel>>(rental.RentalCopies.Where(c => !c.ReturnDate.HasValue));
						ModelState.AddModelError("", error);
						return View(model);
					}
			}

			var isUpdated = false;

			foreach(var copy in model.SelectedCopies)
			{
				if (!copy.IsReturned.HasValue) continue;

				var currentCopy = rental.RentalCopies.SingleOrDefault(c => c.BookCopyId == copy.Id);

				if (currentCopy is null) continue;

				if(copy.IsReturned.HasValue && copy.IsReturned.Value)
				{
					if (currentCopy.ReturnDate.HasValue) continue;

					currentCopy.ReturnDate = DateTime.Now;
					isUpdated = true;
				}

				if(copy.IsReturned.HasValue && !copy.IsReturned.Value)
				{
					if (currentCopy.ExtendedOn.HasValue) continue;

					currentCopy.ExtendedOn = DateTime.Now;
					currentCopy.EndDate = currentCopy.StartDate.AddDays((int)RentalsConfigurations.MaxRentalDuration);
					isUpdated = true;
				}


			}

			if(isUpdated)
			{
				rental.LastUpdatedOn = DateTime.Now;
				rental.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;
				rental.PenaltyPaid = true;

				_context.SaveChanges();
			}

			return RedirectToAction(nameof(Details), new { id = rental.Id });
		}

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult GetCopyDetails(SearchFormViewModel model)
		{
			if (!ModelState.IsValid)
				return BadRequest();

			var copy = _context.BookCopies
				.Include(c=>c.Book)
				.SingleOrDefault(c => c.SerialNumber.ToString() == model.Value && !c.IsDeleted && !c.Book!.IsDeleted);

			if (copy is null)
				return NotFound(Errors.InvalidSerialNumber);

			if (!copy.IsAvailableForRental || !copy.Book!.IsAvailableForRental)
				return BadRequest(Errors.NotAvailableRental);

			//check that copy is not in rental

			var isCopyInRental = _context.RentalCopies.Any(c => c.BookCopyId == copy.Id && !c.ReturnDate.HasValue);
			if(isCopyInRental)
				return BadRequest(Errors.CopyIsInRental);

			var viewModel = _mapper.Map<BookCopyViewModel>(copy);

			return PartialView("_CopyDetails",viewModel);
		}

		

		[HttpPost]
		[ValidateAntiForgeryToken]
		public IActionResult MarkAsDeleted(int id)
		{
			var rental = _context.Rentals.Find(id);

			if (rental is null || rental.CreatedOn != DateTime.Today)
				return NotFound();
			var copiesCount = _context.RentalCopies.Count(c => c.RentalId == id);


			rental.IsDeleted = true;
			rental.LastUpdatedOn = DateTime.Now;
			rental.LastUpdatedById = User.FindFirst(ClaimTypes.NameIdentifier)!.Value;

			_context.SaveChanges();


			return Ok(copiesCount);
		}

		public IActionResult Details(int id)
		{
			var rental = _context.Rentals
				.Include(r => r.RentalCopies)
				.ThenInclude(c => c.BookCopy)
				.ThenInclude(c => c!.Book)
				.Include(r => r.Subscriber)
				.SingleOrDefault(r => r.Id == id);

			if (rental is null)
				return NotFound();

			var viewModel = _mapper.Map<RentalViewModel>(rental);

			return View("rentalDetails",viewModel);
		}

		private (string errorMessage2, ICollection<RentalCopy> copies) ValidateCopies(IEnumerable<int> selectedSerials , int subscriberId , int? rentalId = null)
		{

			var selectedCopies = _context.BookCopies
				.Include(c => c.Book)
				.Include(c => c.Rentals)
				.Where(c => selectedSerials.Contains(c.SerialNumber))
				.ToList();


			var currentSubscriberRentals = _context.Rentals
				.Include(r => r.RentalCopies)
				.ThenInclude(c => c.BookCopy)
				.Where(r => r.SubscriberId == subscriberId &&(rentalId == null || r.Id != rentalId))
				.SelectMany(r => r.RentalCopies)
				.Where(r => !r.ReturnDate.HasValue)
				.Select(c => c.BookCopy!.BookId)
				.ToList();

			List<RentalCopy> copies = new();


			foreach (var copy in selectedCopies)
			{
				if (!copy.IsAvailableForRental || !copy.Book!.IsAvailableForRental)
					return (Errors.NotAvailableRental, copies);

				if (copy.Rentals.Any(c => !c.ReturnDate.HasValue && (rentalId == null || c.RentalId != rentalId)))
					return (Errors.CopyIsInRental, copies);

				if (currentSubscriberRentals.Contains(copy.BookId))
					return ($"This subscriber already has a copy for '{copy.Book.Title}'", copies);



				copies.Add(new RentalCopy { BookCopyId = copy.Id });
			}

			return (string.Empty, copies);
		}

		private (string errorMessage, int? maxAllowedCopies) ValidateSubscriber(Subscriber subscriber, int? rentalId = null)
		{
			if (subscriber.IsBlackListed)
				return (Errors.BlackListedSubscriber, maxAllowedCopies: null);

			if (subscriber.Subscriptions.Last().EndDate < DateTime.Today.AddDays((int)RentalsConfigurations.RentalDuration))
				return (Errors.InactiveSubscriber, maxAllowedCopies: null);

			var subscriberRentalsCopies = subscriber.Rentals
				.Where(r => rentalId == null || r.Id != rentalId)
				.SelectMany(r => r.RentalCopies)
				.Count(c => !c.ReturnDate.HasValue);

			var availableRentalsCount = (int)RentalsConfigurations.MaxAllowedCopies - subscriberRentalsCopies;

			if (availableRentalsCount.Equals(0))
				return (Errors.MaxCopiesReached, maxAllowedCopies: null);

			return (errorMessage: string.Empty, maxAllowedCopies: availableRentalsCount);
		}
	}
}
