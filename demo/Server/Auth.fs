namespace SignalRApp  

open System
open System.Security.Claims
open System.Security.Principal

[<AutoOpen>]
module Extensions =
    type ClaimsIdentity with
        member this.TryFindFirst (claimsIdentifier: string) =
            try 
                match this.FindFirst(claimsIdentifier) with
                | null -> None
                | claim -> Some claim
            with _ -> None

    [<RequireQualifiedAccess>]
    module Claim =
        let value (claim: Claim) = claim.Value

    [<RequireQualifiedAccess>]
    module DateTime =
        let asClaimValue (d: DateTime) = d.Ticks |> string

module Auth =
    open Giraffe
    open Microsoft.AspNetCore.Authorization
    open Microsoft.AspNetCore.Authentication.JwtBearer
    open Microsoft.IdentityModel.Tokens
    open System.IdentityModel.Tokens.Jwt

    type JwtIssuerOptions =
        { Audience: string
          Issuer: string
          NotBefore: DateTime
          RequiredHttpsMetadata: bool
          ValidFor: TimeSpan }
    
    [<NoComparison>]
    type JwtIssuer =
        { Audience: string
          Issuer: string
          NotBefore: DateTime
          RequiredHttpsMetadata: bool
          SigningCredentials: SigningCredentials
          ValidFor: TimeSpan }

        member this.Expiration (issuedAt: DateTime) = issuedAt |> fun d -> d.Add(this.ValidFor)

        member this.TokenValidationParameters =
            TokenValidationParameters()
            |> fun opts ->
                opts.ValidateIssuer <- true
                opts.ValidIssuer <- this.Issuer
                opts.ValidateAudience <- false
                opts.ValidAudience <- this.Audience
                opts.ValidateIssuerSigningKey <- true
                opts.IssuerSigningKey <- this.SigningCredentials.Key
                opts.RequireExpirationTime <- true
                opts.ValidateLifetime <- true
                opts.ClockSkew <- TimeSpan.Zero

                opts

    [<RequireQualifiedAccess>]
    module JwtIssuer =
        let create (options: JwtIssuerOptions) =
            let key = System.Text.Encoding.UTF8.GetBytes(Guid.NewGuid() |> string)

            { Audience = options.Audience
              Issuer = options.Issuer
              NotBefore = options.NotBefore
              RequiredHttpsMetadata = options.RequiredHttpsMetadata
              SigningCredentials = SigningCredentials(SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512)
              ValidFor = options.ValidFor }

    [<RequireQualifiedAccess>]
    module Token =
        [<RequireQualifiedAccess>]
        module ClaimsIdentifier =
            let [<Literal>] Rol = "rol"
            let [<Literal>] Id = "id"

        [<RequireQualifiedAccess>]
        module AppClaims =
            let [<Literal>] HubAccess = "hub_access" 

        let private generateJti () = 
            async { return {| token = Guid.NewGuid() |> string; issuedAt = DateTime.Now |} }
    
        let generateEncodedToken (options: JwtIssuer) (username: string) (identity: ClaimsIdentity) =
            async {
                let! jti = generateJti()

                let claims = [
                    Claim (JwtRegisteredClaimNames.Sub, username)
                    Claim (JwtRegisteredClaimNames.Jti, jti.token)
                    Claim (JwtRegisteredClaimNames.Iat, jti.issuedAt |> DateTime.asClaimValue, ClaimValueTypes.Integer64)
                    yield! [
                        identity.TryFindFirst ClaimsIdentifier.Rol
                        identity.TryFindFirst ClaimsIdentifier.Id
                    ] |> List.choose id
                ]

                return
                    JwtSecurityToken (
                        issuer = options.Issuer,
                        audience = options.Audience,
                        claims = claims,
                        notBefore = Nullable options.NotBefore,
                        expires = Nullable (options.Expiration jti.issuedAt),
                        signingCredentials = options.SigningCredentials
                    )
                    |> JwtSecurityTokenHandler().WriteToken
            }

        let generateClaimsIdentity (username: string) (id: string) =
            ClaimsIdentity (GenericIdentity(username, "Token"), [ 
                Claim (ClaimsIdentifier.Id, id)
                Claim (ClaimsIdentifier.Rol, AppClaims.HubAccess) 
            ])

    [<RequireQualifiedAccess>]
    module Jwt =
        let policy =
            let policy = AuthorizationPolicyBuilder()
                
            policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme)

            policy
                .RequireClaim(Token.ClaimsIdentifier.Rol, "hub_access")
                .Build()

        let authorize : HttpHandler =
            text "UNAUTHORIZED - JWT"
            |> RequestErrors.unauthorized JwtBearerDefaults.AuthenticationScheme "SignalRApp"
            |> authorizeByPolicy policy

        let authenticate : HttpHandler =
            requiresAuthentication <| challenge JwtBearerDefaults.AuthenticationScheme
