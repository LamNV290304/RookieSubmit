

using BankAccountSimulatorMVC.Services;
using BankAccountSimulatorMVC.Services.Interface;

namespace BankAccountSimulatorMVC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();
            builder.Services.AddSingleton<JsonBankDataStore>();
            builder.Services.AddSingleton<IBankAccountService, BankAccountService>();
            builder.Services.AddSingleton<ITransactionService, TransactionService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Accounts}/{action=Index}/{id?}")
                .WithStaticAssets();

            app.Run();
        }
    }
}
