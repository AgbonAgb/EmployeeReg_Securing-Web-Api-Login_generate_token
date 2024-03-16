using API.MiddlewareExt;
using ApplicationCore.Services.Interface;
using ApplicationCore.Services.Repository;
using ApplicationCore.Services.Utilities;
using ApplicationCore.Utilities;
using Infrastructure.Accounts;
using Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

var conn = builder.Configuration.GetConnectionString("DefaultConn");
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(conn));

builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
builder.Services.AddDocSwagger();

//Automapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IEmployeeBasicInfoRepository, EmployeeBasicInfoRepository>();
builder.Services.AddScoped<IAuthenRepository, AuthenRepository>();
/// <summary>
/// IAuthenRepository
/// </summary>

builder.Services.AddIdentity<UserReg, UserRoles>(otp =>
{
    otp.SignIn.RequireConfirmedAccount = false;
    //otp.SignIn.RequireConfirmedEmail = true;
    otp.Password.RequireNonAlphanumeric = false;
    otp.Password.RequiredLength = 6;
})
    .AddRoleManager<RoleManager<UserRoles>>()
    .AddUserManager<UserManager<UserReg>>()
    .AddSignInManager<SignInManager<UserReg>>()//this is to allow sign-n
    .AddDefaultTokenProviders()
    .AddEntityFrameworkStores<AppDbContext>();

//
var expkey = builder.Configuration.GetSection("SecretKey");
builder.Services.Configure<SecreteKeys>(expkey);
var appsett = expkey.Get<SecreteKeys>();

var key = Encoding.ASCII.GetBytes(appsett.Secret);

builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;//set to true if using https

    x.SaveToken = true;

    x.TokenValidationParameters = new TokenValidationParameters

    {
        ValidateIssuerSigningKey = true,

        IssuerSigningKey = new SymmetricSecurityKey(key),

        ValidateIssuer = false,

        ValidateAudience = false

    };
});
//
var app = builder.Build();
//if(app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
//}
// Configure the HTTP request pipeline.

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
