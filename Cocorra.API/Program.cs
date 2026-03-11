using Cocorra.API.Hubs;
using Cocorra.BLL.Services.AdminService;
using Cocorra.BLL.Services.Auth;
using Cocorra.BLL.Services.AuthServices;
using Cocorra.BLL.Services.FriendService;
using Cocorra.BLL.Services.RolesService;
using Cocorra.BLL.Services.RoomService;
using Cocorra.BLL.Services.Upload;
using Cocorra.DAL.Data;
using Cocorra.DAL.Models;
using Cocorra.DAL.Repository.FriendRepository;
using Cocorra.DAL.Repository.GenericRepository;
using Cocorra.DAL.Repository.NotificationRepository;
using Cocorra.DAL.Repository.RoomRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection("JWTSetting");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Cocorra API",
        Version = "v1",
        Description = "API for Cocorra Application"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Add JWT Here"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", builder =>
        builder
            .SetIsOriginAllowed(origin => true) // 👈 الحل السحري: اسمح لأي حد (للتطوير فقط)
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()); // دي ضرورية عشان SignalR
});

#region AddScopedServices
builder.Services.AddSignalR();
builder.Services.AddScoped<IUploadVoice, UploadVoice>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IRoomService, RoomService>();
builder.Services.AddScoped<IAuthServices, AuthServices>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IRolesService, RolesService>();
builder.Services.AddScoped<IFriendRepository, FriendRepository>();
builder.Services.AddScoped<IFriendService, FriendService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped(typeof(IGenericRepositoryAsync<>), typeof(GenericRepositoryAsync<>));
#endregion

#region AddDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b =>
    {
        b.MigrationsAssembly("Cocorra.DAL");
        b.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});
#endregion

#region AddIdentity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(op =>
{
    op.User.RequireUniqueEmail = true;
    op.Password.RequireLowercase = false;
    op.Password.RequireUppercase = false;
    op.Password.RequireNonAlphanumeric = false;
    op.Password.RequireDigit = false;
    op.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();
#endregion

#region Authentication & JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.SaveToken = true;
    options.RequireHttpsMetadata = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["ValidIssuer"],
        ValidAudience = jwtSettings["ValidAudience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["securityKey"]!))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];

            // لو فيه توكن في الرابط + الرابط رايح للـ Hub بتاعنا
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) &&
                path.StartsWithSegments("/chatHub"))
            {
                // خد التوكن من الرابط وحطه في الكونتكست عشان السيرفر يشوفه
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});
#endregion

var app = builder.Build();


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors("CorsPolicy");
app.UseAuthentication(); // لازم الأول
app.UseAuthorization();  // لازم بعده
app.MapHub<RoomHub>("/roomHub"); 
app.MapControllers();

app.Run();