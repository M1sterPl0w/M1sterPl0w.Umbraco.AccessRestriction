using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Umbraco.Cms.Api.Common.Attributes;
using Umbraco.Cms.Web.Common.Authorization;
using Umbraco.Cms.Web.Common.Routing;

namespace M1sterPl0w.Umbraco.AccessRestriction.Controllers
{
    [ApiController]
    [BackOfficeRoute("m1sterpl0wumbracoaccessrestriction/api/v{version:apiVersion}")]
    [Authorize(Policy = AuthorizationPolicies.SectionAccessContent)]
    [MapToApi(Constants.ApiName)]
    public class M1sterPl0wUmbracoAccessRestrictionApiControllerBase : ControllerBase
    {
    }
}
