using System.Linq;
using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IPTS.Controllers
{
    public class SeoController : Controller
    {
        [HttpGet]
        [Route("robots.txt")]
        [ResponseCache(Duration = 86400)]
        [AllowAnonymous]
        public IActionResult RobotsTxt()
        {
            var baseUrl = string.Concat(Request.Scheme, "://", Request.Host.Value.TrimEnd('/'));
            var sitemapUrl = string.Concat(baseUrl, Url.Action(nameof(SitemapXml), "Seo") ?? "/sitemap.xml");

            var builder = new StringBuilder();
            builder.AppendLine("User-agent: *");
            builder.AppendLine("Disallow: /Admin");
            builder.AppendLine("Disallow: /Account");
            builder.AppendLine("Disallow: /Patient");
            builder.AppendLine("Disallow: /Doctor");
            builder.AppendLine("Disallow: /Profile");
            builder.AppendLine("Disallow: /System");
            builder.AppendLine();
            builder.AppendLine("Allow: /");
            builder.AppendLine();
            builder.AppendLine($"Sitemap: {sitemapUrl}");

            return Content(builder.ToString(), "text/plain", Encoding.UTF8);
        }

        [HttpGet]
        [Route("sitemap.xml")]
        [ResponseCache(Duration = 86400)]
        [AllowAnonymous]
        public IActionResult SitemapXml()
        {
            var pages = new[]
            {
                new { Action = "Index", Controller = "Home", Priority = "1.0", Frequency = "daily" },
                new { Action = "About", Controller = "Home", Priority = "0.8", Frequency = "weekly" },
                new { Action = "Contact", Controller = "Home", Priority = "0.8", Frequency = "weekly" },
                new { Action = "Privacy", Controller = "Home", Priority = "0.6", Frequency = "monthly" },
                new { Action = "Treatments", Controller = "Home", Priority = "0.7", Frequency = "weekly" },
                new { Action = "OurProjects", Controller = "Home", Priority = "0.7", Frequency = "weekly" },
                new { Action = "Therapies", Controller = "Home", Priority = "0.7", Frequency = "weekly" }
            };

            var languages = new[] { "de-DE", "en-US" };
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urlset = new XElement(ns + "urlset",
                pages.SelectMany(page => languages.Select(lang =>
                    new XElement(ns + "url",
                        new XElement(ns + "loc", Url.Action(page.Action, page.Controller, new { culture = lang }, Request.Scheme, Request.Host.Value)),
                        new XElement(ns + "changefreq", page.Frequency),
                        new XElement(ns + "priority", page.Priority)
                    )
                ))
            );

            var document = new XDocument(new XDeclaration("1.0", "utf-8", "yes"), urlset);

            return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml", Encoding.UTF8);
        }
    }
}
