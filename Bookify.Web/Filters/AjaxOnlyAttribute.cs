using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Bookify.Web.Filters
{
	public class AjaxOnlyAttribute : ActionMethodSelectorAttribute
	{
		public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
		{
			var Request = routeContext.HttpContext.Request;
			var IsAjax = Request.Headers["x-requested-with"] == "XMLHttpRequest";
			return IsAjax;
		}
	}
}
