using BudgetMVC.Data;
using BudgetMVC.Services;
using Microsoft.EntityFrameworkCore;

namespace BudgetMVC
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<BudgetDbContext>(options =>
            {
                options.UseSqlServer(builder.Configuration.GetConnectionString("Default"));
            });

            builder.Services.AddHostedService<RecurringTransactionService>();

            builder.Services.AddSingleton<DbCache>(sp =>
            {
                var connectionString = builder.Configuration.GetConnectionString("Default");

                return new DbCache(() =>
                {
                    var options = new DbContextOptionsBuilder<BudgetDbContext>()
                        .UseSqlServer(connectionString)
                        .Options;

                    return new BudgetDbContext(options);
                });

            });

            var app = builder.Build();

            await using var scope = app.Services.CreateAsyncScope();
            try
            {
                await DataSeeder.SeedData(scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Seeder failed: {ex.Message}");
            }


            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Transactions}/{action=Index}/{id?}");

            app.Run();
        }
    }
}
