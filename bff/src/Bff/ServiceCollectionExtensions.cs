// Copyright (c) Duende Software. All rights reserved.
// See LICENSE in the project root for license information.

using Duende.AccessTokenManagement.OpenIdConnect;
using Duende.Bff.AccessTokenManagement;
using Duende.Bff.Configuration;
using Duende.Bff.DynamicFrontends;
using Duende.Bff.Endpoints;
using Duende.Bff.Endpoints.Internal;
using Duende.Bff.Internal;
using Duende.Bff.Otel;
using Duende.Bff.SessionManagement.Configuration;
using Duende.Bff.SessionManagement.Revocation;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Duende.Bff;

/// <summary>
/// Extension methods for the BFF DI services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Duende.BFF services to DI
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configureAction"></param>
    /// <returns></returns>
    public static BffBuilder AddBff(this IServiceCollection services, Action<BffOptions>? configureAction = null)
    {
        if (configureAction != null)
        {
            services.Configure(configureAction);
        }

        services.AddDistributedMemoryCache();
        // IMPORTANT: The BffConfigureOpenIdConnectOptions MUST be called before calling
        // AddOpenIdConnectAccessTokenManagement because both configure the same options
        // The AddOpenIdConnectAccessTokenManagement adds OR wraps the BackchannelHttpHandler
        // to add DPoP support. However, our code can also add a backchannel handler. 
        services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, BffConfigureOpenIdConnectOptions>();
        services.AddOpenIdConnectAccessTokenManagement();

        services.AddSingleton<IConfigureOptions<UserTokenManagementOptions>, ConfigureUserTokenManagementOptions>();

        services.AddTransient<IReturnUrlValidator, LocalUrlReturnUrlValidator>();
        services.TryAddSingleton<IAccessTokenRetriever, DefaultAccessTokenRetriever>();

        // management endpoints
        services.AddTransient<ILoginEndpoint, DefaultLoginEndpoint>();
#pragma warning disable CS0618 // Type or member is obsolete
        services.AddTransient<ISilentLoginEndpoint, DefaultSilentLoginEndpoint>();
#pragma warning restore CS0618 // Type or member is obsolete
        services.AddTransient<ISilentLoginCallbackEndpoint, DefaultSilentLoginCallbackEndpoint>();
        services.AddTransient<ILogoutEndpoint, DefaultLogoutEndpoint>();
        services.AddTransient<IUserEndpoint, DefaultUserEndpoint>();
        services.AddTransient<IBackchannelLogoutEndpoint, DefaultBackchannelLogoutEndpoint>();
        services.AddTransient<IDiagnosticsEndpoint, DefaultDiagnosticsEndpoint>();

        // session management
        services.TryAddTransient<ISessionRevocationService, NopSessionRevocationService>();

        // cookie configuration
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureSlidingExpirationCheck>();
        services.AddSingleton<IPostConfigureOptions<CookieAuthenticationOptions>, PostConfigureApplicationCookieRevokeRefreshToken>();
        services.AddSingleton<ActiveCookieAuthenticationScheme>();
        services.AddSingleton<ActiveOpenIdConnectAuthenticationScheme>();

        services.AddSingleton<IPostConfigureOptions<OpenIdConnectOptions>, PostConfigureOidcOptionsForSilentLogin>();

        services.AddSingleton<BffMetrics>();

        // wrap ASP.NET Core
        services.AddAuthentication();
        services.AddTransientDecorator<IAuthenticationService, BffAuthenticationService>();

        return new BffBuilder(services)
            .AddDynamicFrontends();
    }
}
