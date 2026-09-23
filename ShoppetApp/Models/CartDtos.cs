namespace ShoppetApp.Models;

public record CartDto(int CartId, int UserId, List<CartItemDto> Items, decimal TotalAmount);

public record CartItemDto(int Id, int ProductId, string ProductName, decimal UnitPrice, int Quantity, decimal LineTotal, string ImageUrl);

public record AddToCartRequest(int ProductId, int Quantity = 1);

public record UpdateCartItemRequest(int Quantity);

public record OrderDto(int Id, int UserId, decimal TotalAmount, string Status, DateTime OrderedAt, List<OrderItemDto> Items);

public record OrderItemDto(int ProductId, string ProductName, int Quantity, decimal UnitPrice, decimal LineTotal);
