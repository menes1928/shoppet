using ShoppetApp.Models;

namespace ShoppetApp.Services
{
    public class CartService
    {
        private readonly DatabaseService _db;

        public List<CartItem> Items { get; private set; } = new();

        public CartService(DatabaseService db)
        {
            _db = db;
        }

        public async Task LoadCartAsync()
        {
            var cartItems = await _db.GetCartAsync();
            Items.Clear();
            foreach (var item in cartItems)
            {
                Items.Add(item);
            }
        }

        public async Task<List<CartItem>> GetCartAsync()
        {
            await LoadCartAsync();
            return Items;
        }

        public async Task AddToCart(int productId, int quantity = 1)
        {
            await _db.AddToCartAsync(productId, quantity);
            await LoadCartAsync();
        }

        public async Task AddToCart(Product product, int quantity = 1)
        {
            await _db.AddToCartAsync(product.Id, quantity);
            await LoadCartAsync();
        }

        public async Task RemoveFromCart(CartItem item)
        {
            if (item != null)
            {
                await _db.RemoveFromCartAsync(item.Id);
                await LoadCartAsync();
            }
        }

        public async Task RemoveFromCartAsync(int cartItemId)
        {
            await _db.RemoveFromCartAsync(cartItemId);
            await LoadCartAsync();
        }

        public async Task UpdateCartItemAsync(int cartItemId, int quantity)
        {
            await _db.UpdateCartItemAsync(cartItemId, quantity);
            await LoadCartAsync();
        }

        public async Task ClearCartAsync()
        {
            await _db.ClearCartAsync();
            Items.Clear();
        }

        public async Task<bool> CheckoutAsync()
        {
            bool success = await _db.CheckoutAsync();
            if (success)
            {
                Items.Clear();
            }
            return success;
        }
    }
}