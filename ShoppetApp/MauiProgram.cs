using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using ShoppetApp.Pages;
using ShoppetApp.Services;
using ShoppetApp.ViewModels;

namespace ShoppetApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Nunito-Regular.ttf", "Nunito");
                fonts.AddFont("Nunito-SemiBold.ttf", "NunitoSemiBold");
                fonts.AddFont("Nunito-Bold.ttf", "NunitoBold");
                fonts.AddFont("Nunito-ExtraBold.ttf", "NunitoExtraBold");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.Services.AddSingleton<ApiService>();

        // FIXED: Explicitly provide the local SQLite file path to DatabaseService
        builder.Services.AddSingleton<DatabaseService>(s =>
            new DatabaseService(Path.Combine(FileSystem.AppDataDirectory, "shoppet.db3")));

        builder.Services.AddSingleton<CartService>();

        builder.Services.AddTransient<SplashPage>();
        builder.Services.AddTransient<OnboardingPage>();
        builder.Services.AddTransient<AuthPage>();
        builder.Services.AddTransient<AuthViewModel>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<HomeViewModel>();
        builder.Services.AddTransient<ShopPage>();
        builder.Services.AddTransient<ShopViewModel>();
        builder.Services.AddTransient<PetsListPage>();
        builder.Services.AddTransient<PetsListViewModel>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<ProfileViewModel>();
        builder.Services.AddTransient<PetPassportPage>();
        builder.Services.AddTransient<PetPassportViewModel>();
        builder.Services.AddTransient<PetFormPage>();
        builder.Services.AddTransient<PetFormViewModel>();
        builder.Services.AddTransient<HealthLogFormPage>();
        builder.Services.AddTransient<HealthLogFormViewModel>();
        builder.Services.AddTransient<FoodLogFormPage>();
        builder.Services.AddTransient<FoodLogFormViewModel>();
        builder.Services.AddTransient<ContactFormPage>();
        builder.Services.AddTransient<ContactFormViewModel>();
        builder.Services.AddTransient<CartPage>();
        builder.Services.AddTransient<CartViewModel>();
        builder.Services.AddTransient<AppShell>();

        return builder.Build();
    }
}