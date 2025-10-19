using Microsoft.AspNetCore.Hosting;
using System.Text.Encodings.Web;

namespace Bookify.Web.Services
{
	public class EmailBodyBuilder : IEmailBodyBuilder
	{
		private readonly IWebHostEnvironment _webHostEnvironment;

		public EmailBodyBuilder(IWebHostEnvironment webHostEnvironment)
		{
			_webHostEnvironment = webHostEnvironment;
		}

		public string GetEmailBody(string template, Dictionary<string, string> placeholders)
		{

			var filePath = $"{_webHostEnvironment.WebRootPath}/templates/{template}.html";

			StreamReader str = new(filePath);

			var templateBody = str.ReadToEnd();
			str.Close();

			//var imageUrl = $"{Url.Action("Index", "Home", null, Request.Protocol)}";

			foreach(var placeholder in placeholders)
				templateBody = templateBody.Replace($"[{placeholder.Key}]", placeholder.Value);


			return templateBody;
		}
	}
}
