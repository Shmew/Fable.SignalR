namespace Saturn

[<AutoOpen>]
module Extensions =
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.Extensions.DependencyInjection
    open SignalRApp

    type Saturn.Application.ApplicationBuilder with
        [<CustomOperation("config_auth")>]
        member this.UseAuth(state, issuer: Auth.JwtIssuer) =
            this.ServiceConfig(state, fun services -> 
                services
                    .AddSingleton<Auth.JwtIssuer>(issuer)
                    .AddAuthentication(fun opts ->
                        opts.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
                        opts.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme
                    )
                    .AddJwtBearer(fun opts ->
                        opts.SaveToken <- true
                        opts.Audience <- issuer.Audience
                        opts.TokenValidationParameters <- issuer.TokenValidationParameters
                        opts.RequireHttpsMetadata <- false
                    ).Services
                    .AddAuthorization(fun opts ->
                        opts.AddPolicy("SignalR Auth", Auth.Jwt.policy)
                    )
            )
