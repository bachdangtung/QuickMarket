# Hướng dẫn triển khai tính năng đính kèm sản phẩm trong chat

## 1. Tạo migration để thêm bảng ChatProducts

```csharp
// Trong file migration
public partial class AddChatProductsTable : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ChatProducts",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("SqlServer:Identity", "1, 1"),
                UserId1 = table.Column<int>(nullable: false),
                UserId2 = table.Column<int>(nullable: false),
                ProductId = table.Column<int>(nullable: false),
                AddedDate = table.Column<DateTime>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ChatProducts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ChatProducts_Users_UserId1",
                    column: x => x.UserId1,
                    principalTable: "Users",
                    principalColumn: "UserId");
                table.ForeignKey(
                    name: "FK_ChatProducts_Users_UserId2",
                    column: x => x.UserId2,
                    principalTable: "Users",
                    principalColumn: "UserId");
                table.ForeignKey(
                    name: "FK_ChatProducts_Products_ProductId",
                    column: x => x.ProductId,
                    principalTable: "Products",
                    principalColumn: "ProductId");
            });

        migrationBuilder.CreateIndex(
            name: "IX_ChatProducts_UserId1",
            table: "ChatProducts",
            column: "UserId1");
        
        migrationBuilder.CreateIndex(
            name: "IX_ChatProducts_UserId2",
            table: "ChatProducts",
            column: "UserId2");
        
        migrationBuilder.CreateIndex(
            name: "IX_ChatProducts_ProductId",
            table: "ChatProducts",
            column: "ProductId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ChatProducts");
    }
}
```

## 2. Triển khai MessageService

Cập nhật MessageService để hỗ trợ đính kèm sản phẩm:

```csharp
public async Task<bool> AttachProductToChat(int currentUserId, int otherUserId, int productId)
{
    try
    {
        // Kiểm tra xem đã có sản phẩm trong cuộc trò chuyện này chưa
        var existingAttachment = await _context.ChatProducts
            .FirstOrDefaultAsync(cp => 
                ((cp.UserId1 == currentUserId && cp.UserId2 == otherUserId) || 
                 (cp.UserId1 == otherUserId && cp.UserId2 == currentUserId)) && 
                cp.ProductId == productId);

        if (existingAttachment != null)
        {
            // Cập nhật ngày thêm nếu đã tồn tại
            existingAttachment.AddedDate = DateTime.Now;
            _context.Update(existingAttachment);
        }
        else
        {
            // Tạo mới nếu chưa tồn tại
            var chatProduct = new ChatProduct
            {
                UserId1 = currentUserId,
                UserId2 = otherUserId,
                ProductId = productId,
                AddedDate = DateTime.Now
            };
            await _context.ChatProducts.AddAsync(chatProduct);
        }

        await _context.SaveChangesAsync();
        return true;
    }
    catch (Exception ex)
    {
        // Log exception
        return false;
    }
}

public async Task<ChatHistoryDto> GetChatHistory(int currentUserId, int otherUserId)
{
    // Lấy tin nhắn
    var messages = await _context.Messages
        .Where(m => (m.FromUserId == currentUserId && m.ToUserId == otherUserId) ||
                   (m.FromUserId == otherUserId && m.ToUserId == currentUserId))
        .OrderBy(m => m.SentTime)
        .ToListAsync();

    var otherUser = await _context.Users.FindAsync(otherUserId);
    
    if (otherUser == null)
    {
        throw new Exception("User not found");
    }

    // Lấy thông tin sản phẩm nếu có
    var chatProduct = await _context.ChatProducts
        .Include(cp => cp.Product)
        .ThenInclude(p => p.ProductImages)
        .Where(cp => (cp.UserId1 == currentUserId && cp.UserId2 == otherUserId) ||
                     (cp.UserId1 == otherUserId && cp.UserId2 == currentUserId))
        .OrderByDescending(cp => cp.AddedDate)
        .FirstOrDefaultAsync();

    ProductInfoDto productInfo = null;
    if (chatProduct != null)
    {
        var product = chatProduct.Product;
        var imageUrl = product.ProductImages.FirstOrDefault()?.ImageUrl ?? "";

        productInfo = new ProductInfoDto
        {
            ProductId = product.ProductId,
            Name = product.Name,
            Price = product.Price,
            ImageUrl = imageUrl
        };
    }

    // Map sang DTO
    var messageDtos = messages.Select(m => new MessageDto
    {
        MessId = m.MessId,
        FromUserId = m.FromUserId,
        FromUsername = m.FromUserId == currentUserId ? "Bạn" : otherUser.UserName,
        ToUserId = m.ToUserId,
        ToUsername = m.ToUserId == currentUserId ? "Bạn" : otherUser.UserName,
        MessageText = m.MessageText,
        SentTime = m.SentTime
    }).ToList();

    return new ChatHistoryDto
    {
        OtherUserId = otherUserId,
        OtherUsername = otherUser.UserName,
        Messages = messageDtos,
        ProductInfo = productInfo
    };
}
```

## 3. Cập nhật Repository

Nếu dự án sử dụng Repository pattern, cần cập nhật IMessageRepository và MessageRepository để hỗ trợ các chức năng mới.

## 4. Cập nhật Controllers

Cập nhật MessageController để gọi các phương thức mới của service.

## 5. Cập nhật Views

Đảm bảo Index.cshtml được cập nhật để sử dụng thông tin sản phẩm từ Model thay vì ViewBag.

## 6. Tạo migration và cập nhật cơ sở dữ liệu

Chạy lệnh sau để tạo và áp dụng migration:

```
dotnet ef migrations add AddChatProductsTable
dotnet ef database update
```
