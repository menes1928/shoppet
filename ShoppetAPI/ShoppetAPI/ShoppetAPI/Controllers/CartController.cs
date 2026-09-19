using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Data;

namespace ShoppetAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public CartController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // ── Get User Cart ─────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> GetCart()
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();

                    // For now, default to UserId = 1 or grab from session if available
                    int userId = 1;

                    // Ensure cart exists
                    var cartCmd = new MySqlCommand("SELECT Id FROM shoppingcarts WHERE UserId = @UserId", connection);
                    cartCmd.Parameters.AddWithValue("@UserId", userId);
                    object cartIdResult = await cartCmd.ExecuteScalarAsync();

                    int cartId = 0;
                    if (cartIdResult == null)
                    {
                        var createCartCmd = new MySqlCommand("INSERT INTO shoppingcarts (UserId, UpdatedAt) VALUES (@UserId, @UpdatedAt); SELECT LAST_INSERT_ID();", connection);
                        createCartCmd.Parameters.AddWithValue("@UserId", userId);
                        createCartCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                        cartId = Convert.ToInt32(await createCartCmd.ExecuteScalarAsync());
                    }
                    else
                    {
                        cartId = Convert.ToInt32(cartIdResult);
                    }

                    // Fetch cart items with product details
                    var items = new List<object>();
                    decimal totalAmount = 0;

                    var itemsQuery = @"SELECT ci.Id, ci.ProductId, ci.Quantity, p.Name, p.Price, p.ImageUrl 
                                       FROM cartitems ci 
                                       JOIN products p ON ci.ProductId = p.Id 
                                       WHERE ci.CartId = @CartId";

                    using (var cmd = new MySqlCommand(itemsQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@CartId", cartId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int qty = reader.GetInt32("Quantity");
                                decimal price = reader.GetDecimal("Price");
                                totalAmount += (qty * price);

                                items.Add(new
                                {
                                    Id = reader.GetInt32("Id"),
                                    ProductId = reader.GetInt32("ProductId"),
                                    ProductName = reader.GetString("Name"),
                                    UnitPrice = price,
                                    Quantity = qty,
                                    ImageUrl = reader.IsDBNull(reader.GetOrdinal("ImageUrl")) ? "" : reader.GetString("ImageUrl")
                                });
                            }
                        }
                    }

                    return Ok(new
                    {
                        Id = cartId,
                        Items = items,
                        TotalAmount = totalAmount
                    });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error fetching cart: {ex.Message}");
            }
        }

        // ── Add Item to Cart ──────────────────────────────────────────────────
        [HttpPost("items")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    int userId = 1;

                    // Get or create cart
                    var cartCmd = new MySqlCommand("SELECT Id FROM shoppingcarts WHERE UserId = @UserId", connection);
                    cartCmd.Parameters.AddWithValue("@UserId", userId);
                    object cartIdResult = await cartCmd.ExecuteScalarAsync();

                    int cartId = 0;
                    if (cartIdResult == null)
                    {
                        var createCartCmd = new MySqlCommand("INSERT INTO shoppingcarts (UserId, UpdatedAt) VALUES (@UserId, @UpdatedAt); SELECT LAST_INSERT_ID();", connection);
                        createCartCmd.Parameters.AddWithValue("@UserId", userId);
                        createCartCmd.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow);
                        cartId = Convert.ToInt32(await createCartCmd.ExecuteScalarAsync());
                    }
                    else
                    {
                        cartId = Convert.ToInt32(cartIdResult);
                    }

                    // Check if item already exists in cart
                    var checkItemCmd = new MySqlCommand("SELECT Id, Quantity FROM cartitems WHERE CartId = @CartId AND ProductId = @ProductId", connection);
                    checkItemCmd.Parameters.AddWithValue("@CartId", cartId);
                    checkItemCmd.Parameters.AddWithValue("@ProductId", request.ProductId);

                    using (var reader = await checkItemCmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int existingId = reader.GetInt32("Id");
                            int existingQty = reader.GetInt32("Quantity");
                            reader.Close();

                            var updateCmd = new MySqlCommand("UPDATE cartitems SET Quantity = @Qty WHERE Id = @Id", connection);
                            updateCmd.Parameters.AddWithValue("@Qty", existingQty + request.Quantity);
                            updateCmd.Parameters.AddWithValue("@Id", existingId);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                        else
                        {
                            reader.Close();
                            var insertCmd = new MySqlCommand("INSERT INTO cartitems (CartId, ProductId, Quantity) VALUES (@CartId, @ProductId, @Quantity)", connection);
                            insertCmd.Parameters.AddWithValue("@CartId", cartId);
                            insertCmd.Parameters.AddWithValue("@ProductId", request.ProductId);
                            insertCmd.Parameters.AddWithValue("@Quantity", request.Quantity);
                            await insertCmd.ExecuteNonQueryAsync();
                        }
                    }

                    // Return updated cart
                    return await GetCart();
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error adding to cart: {ex.Message}");
            }
        }

        // ── Checkout ──────────────────────────────────────────────────────────
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout()
        {
            try
            {
                string connString = _configuration.GetConnectionString("DefaultConnection")!;
                using (var connection = new MySqlConnection(connString))
                {
                    await connection.OpenAsync();
                    int userId = 1;

                    // Get cart
                    var cartCmd = new MySqlCommand("SELECT Id FROM shoppingcarts WHERE UserId = @UserId", connection);
                    cartCmd.Parameters.AddWithValue("@UserId", userId);
                    object cartIdResult = await cartCmd.ExecuteScalarAsync();

                    if (cartIdResult == null) return BadRequest("Cart is empty.");
                    int cartId = Convert.ToInt32(cartIdResult);

                    // Calculate total and get items
                    var cartItems = new List<(int ProductId, int Quantity, decimal Price)>();
                    decimal totalAmount = 0;

                    string query = @"SELECT ci.ProductId, ci.Quantity, p.Price 
                                     FROM cartitems ci 
                                     JOIN products p ON ci.ProductId = p.Id 
                                     WHERE ci.CartId = @CartId";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@CartId", cartId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int prodId = reader.GetInt32("ProductId");
                                int qty = reader.GetInt32("Quantity");
                                decimal price = reader.GetDecimal("Price");
                                totalAmount += (qty * price);
                                cartItems.Add((prodId, qty, price));
                            }
                        }
                    }

                    if (cartItems.Count == 0) return BadRequest("Cart is empty.");

                    // Create Order
                    var orderCmd = new MySqlCommand("INSERT INTO orders (UserId, TotalAmount, Status, OrderedAt) VALUES (@UserId, @TotalAmount, 'Confirmed', @OrderedAt); SELECT LAST_INSERT_ID();", connection);
                    orderCmd.Parameters.AddWithValue("@UserId", userId);
                    orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    orderCmd.Parameters.AddWithValue("@OrderedAt", DateTime.UtcNow);
                    long orderId = Convert.ToInt64(await orderCmd.ExecuteScalarAsync());

                    // Create Order Items
                    foreach (var item in cartItems)
                    {
                        var orderItemCmd = new MySqlCommand("INSERT INTO orderitems (OrderId, ProductId, Quantity, UnitPrice) VALUES (@OrderId, @ProductId, @Quantity, @UnitPrice)", connection);
                        orderItemCmd.Parameters.AddWithValue("@OrderId", orderId);
                        orderItemCmd.Parameters.AddWithValue("@ProductId", item.ProductId);
                        orderItemCmd.Parameters.AddWithValue("@Quantity", item.Quantity);
                        orderItemCmd.Parameters.AddWithValue("@UnitPrice", item.Price);
                        await orderItemCmd.ExecuteNonQueryAsync();
                    }

                    // Clear Cart Items
                    var clearCmd = new MySqlCommand("DELETE FROM cartitems WHERE CartId = @CartId", connection);
                    clearCmd.Parameters.AddWithValue("@CartId", cartId);
                    await clearCmd.ExecuteNonQueryAsync();

                    return Ok(new { OrderId = orderId, TotalAmount = totalAmount, Status = "Confirmed" });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Checkout failed: {ex.Message}");
            }
        }
    }

    public class AddToCartRequest
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}