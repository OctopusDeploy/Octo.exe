using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Octopus.Client.Extensibility;
using Octopus.Client.Extensibility.Attributes;

namespace Octopus.Client.Model
{
    public class WebPortalConfigResource : IResource
    {
        public WebPortalConfigResource()
        {
            Id = "webportal";
        }

        public string Id { get; set; }

        public LinkCollection Links { get; set; }

        //Not implemented yet
        //public string PublicBaseUrl { get; set; }

        public WebPortalSecurityResource Security { get; set; } = new WebPortalSecurityResource();
    }

    public class WebPortalSecurityResource
    {
        [Writeable]
        public string CorsWhitelist { get; set; }

        [Writeable]
        public string ReferrerPolicy { get; set; }

        [Writeable]
        public bool ContentSecurityPolicyEnabled { get; set; }

        [Writeable]
        public bool HttpStrictTransportSecurityEnabled { get; set; }

        [Writeable]
        [Required]
        public long HttpStrictTransportSecurityMaxAge { get; set; }

        [Writeable]
        [Required]
        public SameSiteMode CookieSameSiteMode { get; set; }

        public XOptionsResource XOptions { get; set; } = new XOptionsResource();
    }

    public enum SameSiteMode
    {
        Lax,
        Strict,
        None,
    }

    public class XOptionsResource
    {
        public const string XFrameAllowFromDescription = "(Deprecated) A uri to provide in the X-Frame-Option http header in conjunction with the ALLOW-FROM value.";
        public const string XFrameOptionsDescription = "Provide in the X-Frame-Option http header a directive such as sameorigin or deny.";


        [Writeable]
        public string XFrameOptionAllowFrom { get; set; }

        [Writeable]
        public string XFrameOptions { get; set; }
    }
}
