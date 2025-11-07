using Bookify.Web.Core.Models;
using Bookify.Web.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;


namespace Bookify.Web.Tasks
{
	public class HangfireTasks
	{
		private readonly ApplicationDbContext _context;
		private readonly IWebHostEnvironment _webHostEnvironment;
		private readonly IWhatsAppClient _whatsAppClient;
		private readonly IEmailBodyBuilder _emailBodyBuilder;
		private readonly IEmailSender _emailSender;
		public HangfireTasks(ApplicationDbContext context,
			IWebHostEnvironment webHostEnvironment,
			IWhatsAppClient whatsAppClient, IEmailBodyBuilder emailBodyBuilder, IEmailSender emailSender)
		{
			_context = context;
			_webHostEnvironment = webHostEnvironment;
			_whatsAppClient = whatsAppClient;
			_emailBodyBuilder = emailBodyBuilder;
			_emailSender = emailSender;
		}


		public async Task SubscriptionExpirationAlert()
		{

			var subscribers = _context.Subscribers.Include(s => s.Subscriptions)
				.Where(s => !s.IsBlackListed && s.Subscriptions.OrderByDescending(subscription => subscription.EndDate).First().EndDate == DateTime.Today.AddDays(5)).ToList();

			foreach (var subscriber in subscribers)
			{

				// Send welcome email


				var placeholders = new Dictionary<string, string>()
				{
					{"imageUrl","https://res.cloudinary.com/mustafabookify/image/upload/v1761142015/calender_eshlm4.png"},
					{"header",$"Hello {subscriber.FirstName},"},
					{"body",$"your subscription will be expired by {subscriber.Subscriptions.Last().EndDate.ToString("dd MMM, yyyy")} 😔"},
				};

				var body = _emailBodyBuilder.GetEmailBody(EmailTemplates.Notification, placeholders);

				await _emailSender.SendEmailAsync(
					subscriber.Email,
					"Bookify Subscription Renewal", body);

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
							new WhatsAppTextParameter{Text = subscriber.Subscriptions.Last().EndDate.ToString("dd MMM, yyyy")}
						}
					}
				};

					var mobileNumber = _webHostEnvironment.IsDevelopment() ? "966501642434" : subscriber.MobileNumber;

					await _whatsAppClient.SendMessage(mobileNumber, WhatsAppLanguageCode.English_US, WhatsAppTemplates.ExpirationAlert, components);
				}

			}
		}

		public async Task RentalsExpirationAlert()
		{
			var tomorrow = DateTime.Today.AddDays(1);

			var rentals = _context.Rentals
				.Include(r => r.Subscriber)
				.Include(r => r.RentalCopies)
				.ThenInclude(c => c.BookCopy)
				.ThenInclude(bc => bc!.Book)
				.Where(r => r.RentalCopies.Any(r => r.EndDate.Date == tomorrow))
				.ToList();

			foreach(var rental in rentals)
			{
				var expiredCopies = rental.RentalCopies.Where(c => c.EndDate.Date == tomorrow).ToList();

				var message = $"your rental for the bellow book(s) will be expired by {tomorrow.ToString("dd MMM, yyyy")} 💔";
				message += "<ul>";

				foreach(var copy in expiredCopies)
				{
					message += $"<li>{copy.BookCopy!.Book!.Title}</li>";
				}

				message += "</ul>";

				var placeholders = new Dictionary<string, string>()
				{
					{"imageUrl","https://res.cloudinary.com/mustafabookify/image/upload/v1761142015/calender_eshlm4.png"},
					{"header",$"Hello {rental.Subscriber!.FirstName},"},
					{"body",message},
				};

				var body = _emailBodyBuilder.GetEmailBody(EmailTemplates.Notification, placeholders);

				await _emailSender.SendEmailAsync(
					rental.Subscriber!.Email,
					"Bookify Rental Expiration 🛎️", body);

			}

			
		}


	}
}
