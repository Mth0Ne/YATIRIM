using Microsoft.EntityFrameworkCore;
using SmartBIST.Application;
using SmartBIST.Infrastructure;
using SmartBIST.Core.Interfaces;
using SmartBIST.Infrastructure.Services;
using SmartBIST.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using SmartBIST.Core.Entities;
using SmartBIST.Application.Services;

var builder = WebApplication.CreateBuilder(args);

// Memory Cache ekle
builder.Services.AddMemoryCache();

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    // Çıktı önbelleğini etkinleştir
    options.CacheProfiles.Add("Default", new Microsoft.AspNetCore.Mvc.CacheProfile
    {
        Duration = 60,
        Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.Any
    });
    
    options.CacheProfiles.Add("Never", new Microsoft.AspNetCore.Mvc.CacheProfile
    {
        Location = Microsoft.AspNetCore.Mvc.ResponseCacheLocation.None,
        NoStore = true
    });

    // Add model binding for decimals with comma
    options.ModelBindingMessageProvider.SetValueMustNotBeNullAccessor(_ => "Bu alan zorunludur.");
})
.AddRazorRuntimeCompilation()
.AddViewOptions(options => {
    options.HtmlHelperOptions.ClientValidationEnabled = false;
});

builder.Services.AddRazorPages();

// Add application and infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Configure HSTS and other security
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(60);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Hata");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Statik dosyalar için caching uygula
        const int durationInSeconds = 60 * 60 * 24; // 1 gün
        ctx.Context.Response.Headers[Microsoft.Net.Http.Headers.HeaderNames.CacheControl] =
            "public,max-age=" + durationInSeconds;
    }
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

// Admin kullanıcısı ve rolü oluştur
using (var scope = app.Services.CreateScope())
{
    var serviceProvider = scope.ServiceProvider;
    try
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var dbContext = serviceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Database'i oluştur
        dbContext.Database.Migrate();
        
        // Admin rolünü oluştur
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            await roleManager.CreateAsync(new IdentityRole("Admin"));
            Console.WriteLine("Admin rolü oluşturuldu.");
        }
        
        // Admin kullanıcısını oluştur
        var adminUser = await userManager.FindByEmailAsync("admin@smartbist.com");
        if (adminUser == null)
        {
            adminUser = new ApplicationUser
            {
                UserName = "admin@smartbist.com",
                Email = "admin@smartbist.com",
                EmailConfirmed = true,
                PhoneNumberConfirmed = true
            };
            var result = await userManager.CreateAsync(adminUser, "Admin123!");
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(adminUser, "Admin");
                Console.WriteLine("Admin kullanıcısı oluşturuldu.");
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                Console.WriteLine($"Admin kullanıcısı oluşturulamadı: {errors}");
            }
        }
        
        // User rolünü oluştur
        if (!await roleManager.RoleExistsAsync("User"))
        {
            await roleManager.CreateAsync(new IdentityRole("User"));
            Console.WriteLine("User rolü oluşturuldu.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Admin kullanıcısı ve rolleri oluşturulurken hata: {ex.Message}");
    }
}

// Uygulama başlatıldığında ilk kez veri çekme işlemini başlat
if (!app.Environment.IsDevelopment()) // Sadece production ortamında çalıştır
{
    // Doğru şekilde scoped service kullanımı
    var _ = Task.Run(async () =>
    {
        try
        {
            // 5 saniye bekleyip sunucunun tamamen başlamasını sağla
            await Task.Delay(5000);
            
            // Her servisi kendi scope'unda kullanmak daha güvenli
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Uygulama başlangıcında ilk veri çekme işlemi başlatıldı");
                
                try
                {
                    var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
                    await stockService.EnsureStocksInitializedAsync();
                    logger.LogInformation("İlk veri çekme işlemi tamamlandı");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Veri çekme işleminde hata: {Message}", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            // Başka bir scope içinde loglama yapalım
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "İlk veri çekme işlemi başlatılırken hata oluştu");
            }
        }
    });
}

// Geliştirme ortamında manuel olarak veri çekme işlemini başlat
if (app.Environment.IsDevelopment())
{
    // Doğru şekilde scoped service kullanımı
    var _ = Task.Run(async () =>
    {
        try
        {
            // 10 saniye bekleyip sunucunun tamamen başlamasını sağla
            await Task.Delay(10000);
            
            // Her servisi kendi scope'unda kullanmak daha güvenli
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("Geliştirme ortamında otomatik veri çekme işlemi başlatıldı");
                
                try
                {
                    var stockService = scope.ServiceProvider.GetRequiredService<IStockService>();
                    await stockService.UpdateStockPricesAsync();
                    logger.LogInformation("Geliştirme ortamında veri çekme işlemi tamamlandı");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Veri çekme işleminde hata: {Message}", ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            // Başka bir scope içinde loglama yapalım
            using (var scope = app.Services.CreateScope())
            {
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
                logger.LogError(ex, "Geliştirme ortamında veri çekme işlemi sırasında hata oluştu");
            }
        }
    });
}

app.Run();
