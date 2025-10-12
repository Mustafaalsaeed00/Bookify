using Mapster;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Bookify.Web.Core.Mapping
{
	public class MappingProfile : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			//for Mapping Costomization
			//config.NewConfig<Category, CategoryViewModel>().Map(dest => dest.CategoryName , src=>src.Name);
			//Categories
			config.NewConfig<Category, CategoryViewModel>();
			config.NewConfig<CategoryFormViewModel, Category>();
			config.NewConfig<Category, SelectListItem>()
				.Map(dest => dest.Value, src => src.Id)
				.Map(dest => dest.Text, src => src.Name);

			//Authors
			config.NewConfig<Author, AuthorViewModel>();
			config.NewConfig<AuthorFormViewModel, Author>();
			config.NewConfig<Author, SelectListItem>()
				.Map(dest => dest.Value, src => src.Id)
				.Map(dest => dest.Text, src => src.Name);

			//Books
			config.NewConfig<BookFormViewModel, Book>();
			config.NewConfig<Book, BookViewModel>()
				.Map(dest => dest.Author, src => src.Author!.Name)
				.Map(dest => dest.Categories, src => src.Categories.Select(c => c.Category!.Name).ToList());

			//BookCopies
			config.NewConfig<BookCopy, BookCopyViewModel>()
				.Map(dest => dest.BookTitle, src => src.Book!.Title);
			config.NewConfig<BookCopyFormViewModel, BookCopy>();

			//Users
			config.NewConfig<ApplicationUser , UserViewModel>()
			.Map(dest => dest.Username, src => src.UserName);
			config.NewConfig<UserFormViewModel , ApplicationUser>()
				.Map(dest => dest.NormalizedEmail, src => src.Email.ToUpper())
				.Map(dest => dest.NormalizedUserName, src => src.UserName.ToUpper());

			//Governorates
			config.NewConfig<Governorate, SelectListItem>()
				.Map(dest => dest.Text, src => src.Name)
				.Map(dest => dest.Value, src => src.Id);

			//Areas
			config.NewConfig<Area, SelectListItem>()
				.Map(dest => dest.Text, src => src.Name)
				.Map(dest => dest.Value, src => src.Id);

			//Subscribers
			config.NewConfig<Subscriber, SubscriberFormViewModel>();
			config.NewConfig<Subscriber, SubscriberSearchResultViewModel>()
				.Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
			config.NewConfig<Subscriber, SubscriberDetailsViewModel>()
				.Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
				.Map(dest => dest.Governorate, src => src.Governorate!.Name)
				.Map(dest => dest.Area, src => src.Area!.Name);


		}
	}
}
