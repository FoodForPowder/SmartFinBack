using System.Text;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartFin.Classes;
using SmartFin.DbContexts;
using SmartFin.Entities;
using SmartFin.Services;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<SmartFinDbContext>(
    options => options.UseNpgsql(builder.Configuration["ConnectionStrings:DefaultConnection"])
    .UseLazyLoadingProxies()
    );
builder.Services.AddIdentity<User, IdentityRole<int>>()
    .AddEntityFrameworkStores<SmartFinDbContext>()
    .AddDefaultTokenProviders();



builder.Services.Configure<IdentityOptions>(options =>
{
    // Password settings.
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.Password.RequiredUniqueChars = 1;
});

builder.Services.AddControllers();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters()
                    {
                        ValidateIssuer = false,
                        ValidateIssuerSigningKey = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        ValidAudience = builder.Configuration["token:audience"],
                        ValidIssuer = builder.Configuration["token:issuer"],
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["token:key"])),

                        ClockSkew = TimeSpan.FromMinutes(120),
                    };
                });

builder.Services.AddScoped<GoalService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<BankStatementParserFactory>();
// Регистрируем фоновую службу
builder.Services.AddHostedService<GoalNotificationBackgroundService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
// NOTE: Auth ALWAYS must be before authorization
app.UseAuthorization();

app.MapControllers();

app.Run();