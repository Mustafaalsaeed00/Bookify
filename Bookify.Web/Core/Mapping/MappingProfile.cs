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

		}
	}
}
