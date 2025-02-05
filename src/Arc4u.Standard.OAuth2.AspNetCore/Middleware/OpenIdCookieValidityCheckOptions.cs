﻿using Microsoft.AspNetCore.Authentication.Cookies;

namespace Arc4u.Standard.OAuth2.Middleware
{
    public class OpenIdCookieValidityCheckOptions
    {
        public ICookieManager CookieManager { get; set; }

        public string AuthenticationType { get; set; } = Constants.CookiesAuthenticationType;

        public string CookieName { get; set; }

        public IKeyValueSettings OpenIdSettings { get; set; } = null;

        public string RedirectAuthority { get; set; } = string.Empty;

    }
}
