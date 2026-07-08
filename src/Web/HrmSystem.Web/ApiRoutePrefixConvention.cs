using Microsoft.AspNetCore.Mvc.ApplicationModels;
// ─────────────────────────────────────────────────────────────
// Auto-prefix REST controllers with /api so we don't repeat
// [Route("api/...")] on every API controller. Match is anchored on
// the namespace SEGMENT `Api.Controllers` to avoid false positives.
// ─────────────────────────────────────────────────────────────

namespace HrmSystem.Web;

public sealed class ApiRoutePrefixConvention(string prefix) : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix = new(new Microsoft.AspNetCore.Mvc.RouteAttribute(prefix));

    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            string ns = controller.ControllerType.Namespace ?? string.Empty;
            string[] segments = ns.Split('.');

            // Match `.Api.Controllers` as adjacent segments, not as a substring.
            bool hit = false;
            for (int i = 0; i < segments.Length - 1; i++)
            {
                if (segments[i] == "Api" && segments[i + 1] == "Controllers")
                {
                    hit = true;
                    break;
                }
            }
            if (!hit)
            {
                continue;
            }

            foreach (SelectorModel selector in controller.Selectors)
            {
                selector.AttributeRouteModel = selector.AttributeRouteModel is null
                    ? _routePrefix
                    : AttributeRouteModel.CombineAttributeRouteModel(_routePrefix, selector.AttributeRouteModel);
            }
        }
    }
}
