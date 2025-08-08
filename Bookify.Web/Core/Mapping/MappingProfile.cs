using Mapster;

namespace Bookify.Web.Core.Mapping
{
	public class MappingProfile : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			//for Mapping Costomization
			//config.NewConfig<Category, CategoryViewModel>().Map(dest => dest.CategoryName , src=>src.Name);

			config.NewConfig<Category, CategoryViewModel>();
			config.NewConfig<CategoryFormViewModel, Category>();
		}
	}
}
