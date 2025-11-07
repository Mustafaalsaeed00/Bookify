using Hangfire.Annotations;
using Hangfire.Dashboard;

namespace Bookify.Web.Filters
{
	public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
	{
		private string _policyName;

		public HangfireAuthorizationFilter(string policyName)
		{
			_policyName = policyName;
		}

		public bool Authorize([NotNull] DashboardContext context)
		{
			var httpContext = context.GetHttpContext();
			var authServices = httpContext.RequestServices.GetRequiredService<IAuthorizationService>();

			var isAuthorize = authServices.AuthorizeAsync(httpContext.User, _policyName)
											.ConfigureAwait(false)
											.GetAwaiter()
											.GetResult()
											.Succeeded;
			return isAuthorize;
		}
	}
}
