using Edulytics.Web.Middleware;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Edulytics.Web.TagHelpers;

[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper
    : TagHelper
{
    [ViewContext]
    [HtmlAttributeNotBound]
    public ViewContext ViewContext
        { get; set; } = default!;

    public override void Process(
        TagHelperContext context,
        TagHelperOutput output)
    {
        if (ViewContext.HttpContext.Items
                .TryGetValue(
                    SecurityHeadersMiddleware
                        .CspNonceItemKey,
                    out var value) &&
            value is string nonce &&
            !string.IsNullOrWhiteSpace(
                nonce))
        {
            output.Attributes.SetAttribute(
                "nonce",
                nonce);
        }
    }
}
