using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;

namespace M1sterPl0w.Umbraco.AccessRestriction.Middleware
{
    /// <summary>Resolves content URLs via Umbraco's published content cache and URL provider.</summary>
    public class UmbracoContentUrlResolver : IContentUrlResolver
    {
        private readonly IUmbracoContextFactory _umbracoContextFactory;
        private readonly IPublishedUrlProvider _publishedUrlProvider;

        public UmbracoContentUrlResolver(
            IUmbracoContextFactory umbracoContextFactory,
            IPublishedUrlProvider publishedUrlProvider)
        {
            _umbracoContextFactory = umbracoContextFactory;
            _publishedUrlProvider = publishedUrlProvider;
        }

        public string? GetUrl(Guid contentKey)
        {
            try
            {
                using var ctx = _umbracoContextFactory.EnsureUmbracoContext();
                var content = ctx.UmbracoContext.Content?.GetById(contentKey);
                if (content is null)
                    return null;

                var url = _publishedUrlProvider.GetUrl(content);
                return string.IsNullOrWhiteSpace(url) || url == "#" ? null : url;
            }
            catch
            {
                return null;
            }
        }
    }
}
