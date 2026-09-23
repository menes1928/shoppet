using ShoppetApp.Models;
using System.Net.Http.Json;

namespace ShoppetApp.Services;

public class ApiService
{
    // ── Change this URL to match wherever the API is running ──────────────────
    // For Android emulator use:   http://10.0.2.2:5020
    // For iOS simulator use:      http://localhost:5020
    // For Windows dev machine:    http://localhost:5020
#if ANDROID
    private const string BaseUrl = "http://10.0.2.2:5020/api";
#else
    private const string BaseUrl = "http://localhost:5020/api";
#endif

    private readonly HttpClient _http;
    private string? _token;

    public ApiService()
    {
        _http = new HttpClient { BaseAddress = new Uri(BaseUrl + "/") };
    }

    // ── Token management ──────────────────────────────────────────────────────

    public void SetToken(string token)
    {
        _token = token;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
    }

    public void ClearToken()
    {
        _token = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public bool IsAuthenticated => !string.IsNullOrEmpty(_token);

    // ── Auth ──────────────────────────────────────────────────────────────────

    public async Task<ApiResult<AuthResponse>> RegisterAsync(string fullName, string email, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("auth/register", new { fullName, email, password });
            if (res.IsSuccessStatusCode)
                return ApiResult<AuthResponse>.Ok(await res.Content.ReadFromJsonAsync<AuthResponse>()!);
            var err = await res.Content.ReadAsStringAsync();
            return ApiResult<AuthResponse>.Fail(res.StatusCode == System.Net.HttpStatusCode.Conflict
                ? "Email is already registered." : $"Registration failed: {err}");
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponse>.Fail($"Cannot reach server: {ex.Message}");
        }
    }

    public async Task<ApiResult<AuthResponse>> LoginAsync(string email, string password)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("auth/login", new { email, password });
            if (res.IsSuccessStatusCode)
                return ApiResult<AuthResponse>.Ok(await res.Content.ReadFromJsonAsync<AuthResponse>()!);
            return ApiResult<AuthResponse>.Fail("Invalid email or password.");
        }
        catch (Exception ex)
        {
            return ApiResult<AuthResponse>.Fail($"Cannot reach server: {ex.Message}");
        }
    }

    // ── Pets ──────────────────────────────────────────────────────────────────

    public async Task<List<Pet>> GetPetsAsync()
    {
        try { return await _http.GetFromJsonAsync<List<Pet>>("pets") ?? []; }
        catch { return []; }
    }

    public async Task<Pet?> SavePetAsync(Pet pet)
    {
        try
        {
            var body = new
            {
                UserId = pet.UserId, // Ensures the owner's ID is sent to the backend
                pet.Name,
                pet.Species,
                pet.Breed,
                pet.AgeYears,
                pet.Weight,
                pet.PhotoUrl
            };

            HttpResponseMessage res;
            if (pet.Id == 0)
                res = await _http.PostAsJsonAsync("pets", body);
            else
                res = await _http.PutAsJsonAsync($"pets/{pet.Id}", body);

            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync<Pet>();
        }
        catch { }
        return null;
    }

    public async Task<bool> DeletePetAsync(int petId)
    {
        try { return (await _http.DeleteAsync($"pets/{petId}")).IsSuccessStatusCode; }
        catch { return false; }
    }

    // ── Health Logs ───────────────────────────────────────────────────────────

    public async Task<List<HealthLog>> GetHealthLogsAsync(int petId)
    {
        try { return await _http.GetFromJsonAsync<List<HealthLog>>($"pets/{petId}/health") ?? []; }
        catch { return []; }
    }

    public async Task<HealthLog?> SaveHealthLogAsync(int petId, HealthLog log)
    {
        try
        {
            var body = new
            {
                log.Type,
                log.Name,
                log.DueDate,
                log.Completed,
                log.DateAdministered,
                log.ValidityInterval,
                log.ValidityUnit,
                log.MedicationIntervalHours,
                log.TimeStarted,
                log.DosageTotal,
                log.DosageRemaining,
                log.CheckupDate,
                log.DocumentPaths
            };
            HttpResponseMessage res;
            if (log.Id == 0)
                res = await _http.PostAsJsonAsync($"pets/{petId}/health", body);
            else
                res = await _http.PutAsJsonAsync($"pets/{petId}/health/{log.Id}", body);

            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync<HealthLog>();
        }
        catch { }
        return null;
    }

    public async Task<bool> DeleteHealthLogAsync(int petId, int logId)
    {
        try { return (await _http.DeleteAsync($"pets/{petId}/health/{logId}")).IsSuccessStatusCode; }
        catch { return false; }
    }

    // ── Food Logs ─────────────────────────────────────────────────────────────

    public async Task<List<FoodLog>> GetFoodLogsAsync(int petId)
    {
        try { return await _http.GetFromJsonAsync<List<FoodLog>>($"pets/{petId}/food") ?? []; }
        catch { return []; }
    }

    public async Task<FoodLog?> SaveFoodLogAsync(int petId, FoodLog log)
    {
        try
        {
            var body = new
            {
                log.FoodName,
                log.AmountGrams,
                log.IntervalHours,
                log.IntervalMinutes,
                log.StartTimestamp,
                log.Notes
            };
            HttpResponseMessage res;
            if (log.Id == 0)
                res = await _http.PostAsJsonAsync($"pets/{petId}/food", body);
            else
                res = await _http.PutAsJsonAsync($"pets/{petId}/food/{log.Id}", body);

            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync<FoodLog>();
        }
        catch { }
        return null;
    }

    public async Task<FoodLog?> MarkFoodDoneAsync(int petId, int logId)
    {
        try
        {
            var res = await _http.PostAsync($"pets/{petId}/food/{logId}/done", null);
            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync<FoodLog>();
        }
        catch { }
        return null;
    }

    public async Task<bool> DeleteFoodLogAsync(int petId, int logId)
    {
        try { return (await _http.DeleteAsync($"pets/{petId}/food/{logId}")).IsSuccessStatusCode; }
        catch { return false; }
    }

    // ── Contacts ──────────────────────────────────────────────────────────────

    public async Task<List<Models.Contact>> GetContactsAsync()
    {
        try { return await _http.GetFromJsonAsync<List<Models.Contact>>("contacts") ?? []; }
        catch { return []; }
    }

    public async Task<Models.Contact?> SaveContactAsync(Models.Contact contact)
    {
        try
        {
            var body = new { contact.Name, contact.Role, contact.Address, contact.Phone, contact.IsEmergency };
            HttpResponseMessage res;
            if (contact.Id == 0)
                res = await _http.PostAsJsonAsync("contacts", body);
            else
                res = await _http.PutAsJsonAsync($"contacts/{contact.Id}", body);

            if (res.IsSuccessStatusCode)
                return await res.Content.ReadFromJsonAsync<Models.Contact>();
        }
        catch { }
        return null;
    }

    public async Task<bool> DeleteContactAsync(int contactId)
    {
        try { return (await _http.DeleteAsync($"contacts/{contactId}")).IsSuccessStatusCode; }
        catch { return false; }
    }

    // ── Shop ──────────────────────────────────────────────────────────────────

    public async Task<List<Product>> GetProductsAsync(string? species = null, string? category = null, string? search = null)
    {
        try
        {
            var query = new List<string>();
            if (!string.IsNullOrEmpty(species)) query.Add($"species={Uri.EscapeDataString(species)}");
            if (!string.IsNullOrEmpty(category)) query.Add($"category={Uri.EscapeDataString(category)}");
            if (!string.IsNullOrEmpty(search)) query.Add($"search={Uri.EscapeDataString(search)}");
            var qs = query.Count > 0 ? "?" + string.Join("&", query) : "";
            return await _http.GetFromJsonAsync<List<Product>>($"shop/products{qs}") ?? [];
        }
        catch { return []; }
    }

    public async Task<List<ShopCategory>> GetCategoriesAsync()
    {
        try { return await _http.GetFromJsonAsync<List<ShopCategory>>("shop/categories") ?? []; }
        catch { return []; }
    }

    // ── Cart & Orders ─────────────────────────────────────────────────────────

    public async Task<CartDto?> GetCartAsync()
    {
        try { return await _http.GetFromJsonAsync<CartDto>("cart"); }
        catch { return null; }
    }

    public async Task<CartDto?> AddToCartAsync(AddToCartRequest request)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("cart/items", request);
            if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<CartDto>();
        }
        catch { }
        return null;
    }

    public async Task<CartDto?> UpdateCartItemAsync(int itemId, UpdateCartItemRequest request)
    {
        try
        {
            var res = await _http.PutAsJsonAsync($"cart/items/{itemId}", request);
            if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<CartDto>();
        }
        catch { }
        return null;
    }

    public async Task<CartDto?> RemoveFromCartAsync(int itemId)
    {
        try
        {
            var res = await _http.DeleteAsync($"cart/items/{itemId}");
            if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<CartDto>();
        }
        catch { }
        return null;
    }

    public async Task<CartDto?> ClearCartAsync()
    {
        try
        {
            var res = await _http.DeleteAsync("cart");
            if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<CartDto>();
        }
        catch { }
        return null;
    }

    public async Task<OrderDto?> CheckoutAsync()
    {
        try
        {
            var res = await _http.PostAsync("cart/checkout", null);
            if (res.IsSuccessStatusCode) return await res.Content.ReadFromJsonAsync<OrderDto>();
        }
        catch { }
        return null;
    }

    public async Task<List<OrderDto>> GetOrdersAsync()
    {
        try { return await _http.GetFromJsonAsync<List<OrderDto>>("cart/orders") ?? []; }
        catch { return []; }
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class AuthResponse
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = "PetOwner";  // ⭐ MUST ADD THIS
    public string Token { get; set; } = string.Empty;
}

public class ApiResult<T>
{
    public bool Success { get; private set; }
    public T? Data { get; private set; }
    public string? Error { get; private set; }

    public static ApiResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResult<T> Fail(string error) => new() { Success = false, Error = error };
}