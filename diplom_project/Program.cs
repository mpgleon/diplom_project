using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using diplom_project.Services;
using Microsoft.OpenApi.Models;
using diplom_project.Controllers;
using Microsoft.Extensions.FileProviders;
using Swashbuckle.AspNetCore.Filters;

namespace diplom_project
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            
            builder.Services.AddControllersWithViews();

            // Настройка сервиса
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IRatingService, RatingService>();
            // Настройка JWT
            builder.Services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = builder.Configuration["Jwt:Issuer"],
                        ValidAudience = builder.Configuration["Jwt:Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
                    };
                });
            builder.Services.AddAuthorization();

            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    In = ParameterLocation.Header,
                    Description = "Please enter JWT with Bearer into field",
                    Name = "Authorization",
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
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
                        Array.Empty<string>()
                    }
                });
            });

            

            // Настройка Entity Framework
            builder.Services.AddDbContext<AppDbContext>(options => 
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5, // Максимальное количество попыток
                        maxRetryDelay: TimeSpan.FromSeconds(10), // Задержка между попытками
                        errorNumbersToAdd: null // Дополнительные коды ошибок (можно оставить null)
                    )
                ));

            builder.Services.AddSignalR();

            var app = builder.Build();
            app.UseCors("AllowAll");
            app.Use(async (context, next) =>
            {
                if (context.Request.Method == "OPTIONS")
                {
                    context.Response.StatusCode = StatusCodes.Status204NoContent;
                    context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
                    context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
                    context.Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Authorization");
                    return;
                }
                await next();
            });
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }
            
            app.MapHub<ChatHub>("/chatHub");
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V2");
                c.RoutePrefix = string.Empty; // Делает Swagger доступным по корневому URL (localhost:port)
            });

            app.MapGet("/", async context =>
            {
                context.Response.Redirect("/registration.html"); //------------------
                await Task.CompletedTask;
            });
            app.UseStaticFiles();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Path.Combine(builder.Environment.WebRootPath, "uploads", "avatars")),
                RequestPath = "/uploads/avatars"
            });
            app.UseRouting();
            app.UseHttpsRedirection(); //12312312312321
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();

            app.Run();
        }
    }

}
