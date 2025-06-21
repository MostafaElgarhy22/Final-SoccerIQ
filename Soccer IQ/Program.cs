using Microsoft.EntityFrameworkCore;
using Soccer_IQ.Data;
using Soccer_IQ.Models;
using Soccer_IQ.Repository.IRepository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Soccer_IQ.Repository;
using Microsoft.AspNetCore.Identity;
using Soccer_IQ.Services;
using System.Text.Json.Serialization;

namespace Soccer_IQ
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ✅ Allow any origin (CORS fully open)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            // ✅ JWT Authentication setup
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                        )
                    };
                });

            // ✅ Services
            builder.Services.AddScoped<CsvSeeder>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddScoped<PlayerStatLinker>();
            builder.Services.AddScoped<PlayerSeeder>();

            builder.Services.AddDbContext<AppDbContext>(
                options => options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddScoped<IRepository<Player>, PlayerRepository>();
            builder.Services.AddScoped<IRepository<Club>, ClubRepository>();
            builder.Services.AddScoped<IRepository<PLayerStat>, PlayerStatRepository>();
            builder.Services.AddScoped<IRepository<LeagueStanding>, LeagueStandingRepository>();
            builder.Services.AddScoped<IRepository<ApplicationUser>, ApplicationUserRepository>();
            builder.Services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<AppDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddHttpClient<PredictionService>(c =>
            {
                c.BaseAddress = new Uri("https://web-production-4057.up.railway.app/");
            });

            builder.Services.AddHttpClient();
            builder.Services.AddScoped<StandingsSyncService>();

            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
                });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            // ✅ Apply open CORS
            app.UseCors("AllowAll");

            // ✅ Apply authentication & authorization
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            using (var scope = app.Services.CreateScope())
            {
                var linker = scope.ServiceProvider.GetRequiredService<PlayerStatLinker>();
                int updated = linker.FillPlayerIdsOffset4();
                Console.WriteLine($"✔️ PlayerId filled for {updated} PlayerStats.");

                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
                if (!await roleManager.RoleExistsAsync("User"))
                {
                    await roleManager.CreateAsync(new IdentityRole("User"));
                    Console.WriteLine("✅ 'User' role created successfully.");
                }
            }

            app.Run();
        }
    }
}
