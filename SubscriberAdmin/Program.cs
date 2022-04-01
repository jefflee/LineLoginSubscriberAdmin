using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<JwtHelpers>();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // 當驗證失敗時，回應標頭會包含 WWW-Authenticate 標頭，這裡會顯示失敗的詳細錯誤原因
        options.IncludeErrorDetails = true; // 預設值為 true，有時會特別關閉

        options.TokenValidationParameters = new TokenValidationParameters
        {
            // 透過這項宣告，就可以從 "sub" 取值並設定給 User.Identity.Name
            NameClaimType = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
            // 透過這項宣告，就可以從 "roles" 取值，並可讓 [Authorize] 判斷角色
            RoleClaimType = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",

            // 一般我們都會驗證 Issuer
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration.GetValue<string>("JwtSettings:Issuer"),

            // 通常不太需要驗證 Audience
            ValidateAudience = false,
            //ValidAudience = "JwtAuthDemo", // 不驗證就不需要填寫

            // 一般我們都會驗證 Token 的有效期間
            ValidateLifetime = true,

            // 如果 Token 中包含 key 才需要驗證，一般都只有簽章而已
            ValidateIssuerSigningKey = false,

            // "1234567890123456" 應該從 IConfiguration 取得
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("JwtSettings:SignKey")))
        };
    });

builder.Services.AddAuthorization();

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

builder.Services.AddDbContext<SubscriberContext>(options => options.UseInMemoryDatabase("subs"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// LINE Login
app.MapGet("/signin", LINELoginHandler.SignIn).WithName(nameof(LINELoginHandler.SignIn)).AllowAnonymous();
app.MapGet("/callback", LINELoginHandler.CallbackUri).WithName(nameof(LINELoginHandler.CallbackUri)).AllowAnonymous();

app.MapGet("/my", async (ClaimsPrincipal user, SubscriberContext db) =>
{
    var profile = await db.Subscribers.FirstOrDefaultAsync(p => p.Id.ToString() == user.Identity.Name);
    if (profile is null)
    {
        return Results.NotFound();
    }
    else
    {
        return Results.Ok(profile);
    }
}).WithName("GetSubscribers").RequireAuthorization();

app.MapGet("/subs", async (SubscriberContext db) => await db.Subscribers.ToListAsync())
    .WithName("GetAllSubscribers");

// app.MapGet("/subs/{id}", async (SubscriberContext db, int id) =>
// {
//     var sub = await db.Subscribers.FindAsync(id);
//     if (sub is null) return Results.NotFound();
//     return Results.Ok(sub);
// }).WithName("GetSubscriberById");

// app.MapPost("/subs", async (SubscriberContext db, Subscriber sub) =>
// {
//     await db.Subscribers.AddAsync(sub);
//     await db.SaveChangesAsync();
//     return Results.Created($"/subs/{sub.Id}", sub);
// }).WithName("AddSubscriber");

// app.MapPut("/subs/{id}", async (SubscriberContext db, Subscriber updatesub, int id) =>
// {
//     var sub = await db.Subscribers.FindAsync(id);
//     if (sub is null) return Results.NotFound();
//     sub.Username = updatesub.Username;
//     sub.AccessToken = updatesub.AccessToken;
//     await db.SaveChangesAsync();
//     return Results.NoContent();
// });

// app.MapDelete("/subs/{id}", async (SubscriberContext db, int id) =>
// {
//     var sub = await db.Subscribers.FindAsync(id);
//     if (sub is null)
//     {
//         return Results.NotFound();
//     }
//     db.Subscribers.Remove(sub);
//     await db.SaveChangesAsync();
//     return Results.Ok();
// }).WithName("DeleteSubscriber");

app.Run();
