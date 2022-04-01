using System.Text.Json;
using JWT;
using JWT.Builder;
using JWT.Serializers;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using WebApplication1.Models;

public class LINELoginHandler
{
    public static IResult SignIn(JwtHelpers jwt, IConfiguration config, [FromServices] IHttpContextAccessor httpContextAccessor)
    {
        var currentUrl = httpContextAccessor.HttpContext.Request.GetEncodedUrl();
        var authority = new Uri(currentUrl).GetLeftPart(UriPartial.Authority);

        var RedirectUri = config["LINELogin:redirect_uri"];
        if (Uri.IsWellFormedUriString(RedirectUri, UriKind.Relative))
        {
            RedirectUri = authority + RedirectUri;
        }

        var qb = new QueryBuilder();
        qb.Add("response_type", "code");
        qb.Add("client_id", config["LINELogin:client_id"]);
        qb.Add("scope", config["LINELogin:scope"]);
        qb.Add("redirect_uri", RedirectUri);
        qb.Add("state", "111");

        var authUrl = "https://access.line.me/oauth2/v2.1/authorize" + qb.ToQueryString().Value;

        return Results.Redirect(authUrl);
    }

    public static async Task<IResult> CallbackUri(
        JwtHelpers jwt, SubscriberContext db, IHttpClientFactory httpClientFactory, IConfiguration config,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        string code, string state)
    {
        var currentUrl = httpContextAccessor.HttpContext.Request.GetEncodedUrl();
        var authority = new Uri(currentUrl).GetLeftPart(UriPartial.Authority);

        var RedirectUri = config["LINELogin:redirect_uri"];
        if (Uri.IsWellFormedUriString(RedirectUri, UriKind.Relative))
        {
            RedirectUri = authority + RedirectUri;
        }

        var http = httpClientFactory.CreateClient();

        var content = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type",    "authorization_code"),
            new KeyValuePair<string, string>("code",          code),
            new KeyValuePair<string, string>("client_id",     config["LINELogin:client_id"]),
            new KeyValuePair<string, string>("client_secret", config["LINELogin:client_secret"]),
            new KeyValuePair<string, string>("redirect_uri",  RedirectUri),
        });

        var response = await http.PostAsync("https://api.line.me/oauth2/v2.1/token", content);
        var jsonString = await response.Content.ReadAsStringAsync();

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var result = JsonSerializer.Deserialize<GetTokenResult>(jsonString);

            // http.DefaultRequestHeaders.Add("Authorization", "Bearer " + result.AccessToken);
            // var profile = await http.GetFromJsonAsync<LINELoginProfile>("https://api.line.me/v2/profile");

            IJsonSerializer serializer = new JsonNetSerializer();
            IBase64UrlEncoder urlEncoder = new JwtBase64UrlEncoder();
            IJwtDecoder decoder = new JwtDecoder(serializer, urlEncoder);

            var payload = decoder.DecodeToObject<JwtPayload>(result.IdToken);

            // Save to DB
            var sub = new Subscriber()
            {
                LINELoginAccessToken = result.AccessToken,
                LINELoginIDToken = result.IdToken,
                Username = payload.Name,
                Email = payload.Email,
                Photo = payload.Picture
            };
            db.Subscribers.Add(sub);
            db.SaveChanges();

            return Results.Ok(new
            {
                token = jwt.GenerateToken(sub.Id.ToString())
            });
        }
        else
        {
            var result = JsonSerializer.Deserialize<GetTokenError>(jsonString);

            return Results.BadRequest(result);
        }
    }
}