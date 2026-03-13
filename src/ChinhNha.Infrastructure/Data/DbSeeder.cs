using ChinhNha.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using ChinhNha.Application.Interfaces;

namespace ChinhNha.Infrastructure.Data;

public static class DbSeeder
{
    private const string DefaultAdminEmail = "admin@chinhnha.id.vn";
    private const string DefaultAdminPassword = "Admin@123";
    private const string DefaultAdminFullName = "Administrator";

    public static async Task SeedAsync(AppDbContext context, IPasswordHashService passwordHashService)
    {
        // 1. Tạo Roles
        if (!await context.Roles.AnyAsync(r => r.Name == "Admin"))
        {
            context.Roles.Add(new Role { Name = "Admin" });
        }

        if (!await context.Roles.AnyAsync(r => r.Name == "Customer"))
        {
            context.Roles.Add(new Role { Name = "Customer" });
        }

        await context.SaveChangesAsync();

        // 2. Upsert Admin User
        var adminUser = await context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == DefaultAdminEmail.ToLower());

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                Email = DefaultAdminEmail,
                FullName = DefaultAdminFullName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = passwordHashService.HashPassword(DefaultAdminPassword)
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
        else
        {
            adminUser.Email = DefaultAdminEmail;
            adminUser.FullName = string.IsNullOrWhiteSpace(adminUser.FullName)
                ? DefaultAdminFullName
                : adminUser.FullName;
            adminUser.IsActive = true;
            adminUser.PasswordHash = passwordHashService.HashPassword(DefaultAdminPassword);

            await context.SaveChangesAsync();
        }

        var adminRole = await context.Roles.FirstAsync(r => r.Name == "Admin");
        var hasAdminRole = await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
        if (!hasAdminRole)
        {
            context.UserRoles.Add(new AppUserRole { UserId = adminUser.Id, RoleId = adminRole.Id });
            await context.SaveChangesAsync();
        }

        // 3. Tạo Supplier Mẫu (Nếu chưa có)
        if (!await context.Suppliers.AnyAsync())
        {
            var suppliers = new List<Supplier>
            {
                new() { Name = "Công ty CP Phân bón Việt Nam", Phone = "0901234567", Email = "lh@cpvn.com.vn", Address = "TP. Hồ Chí Minh", IsActive = true, CreatedAt = DateTime.UtcNow },
                new() { Name = "Công ty TNHH Agri Miền Nam", Phone = "0907654321", Email = "sales@agrimn.vn", Address = "Cần Thơ", IsActive = true, CreatedAt = DateTime.UtcNow },
            };
            await context.Suppliers.AddRangeAsync(suppliers);
            await context.SaveChangesAsync();
        }

        // 4. Tạo Category Mẫu (Nếu chưa có)
        if (!await context.ProductCategories.AnyAsync())
        {
            var categories = new List<ProductCategory>
            {
                new() { Name = "Phân Bón Hóa Học",          Slug = "phan-bon-hoa-hoc",           Description = "Phân bón hóa học cho năng suất cao",                    DisplayOrder = 1, CreatedAt = DateTime.UtcNow },
                new() { Name = "Phân Bón Hữu Cơ",           Slug = "phan-bon-huu-co",            Description = "Phân bón hữu cơ cải tạo đất, an toàn nông sản",         DisplayOrder = 2, CreatedAt = DateTime.UtcNow },
                new() { Name = "Thuốc Bảo Vệ Thực Vật",     Slug = "thuoc-bao-ve-thuc-vat",      Description = "Hóa chất bảo vệ cây trồng tổng hợp",                    DisplayOrder = 3, CreatedAt = DateTime.UtcNow },
                new() { Name = "Thuốc Trừ Bệnh",             Slug = "thuoc-tru-benh",             Description = "Thuốc đặc trị nấm, vi khuẩn, bệnh hại cây trồng",      DisplayOrder = 4, CreatedAt = DateTime.UtcNow },
                new() { Name = "Thuốc Trừ Sâu",              Slug = "thuoc-tru-sau",              Description = "Thuốc phòng trừ sâu hại, côn trùng phá hoại cây trồng", DisplayOrder = 5, CreatedAt = DateTime.UtcNow },
                new() { Name = "Bạt Phủ Nông Nghiệp",        Slug = "bat-phu-nong-nghiep",        Description = "Bạt phủ đất, màng phủ nông nghiệp các loại",            DisplayOrder = 6, CreatedAt = DateTime.UtcNow },
            };
            await context.ProductCategories.AddRangeAsync(categories);
            await context.SaveChangesAsync();
        }

        // 5. Tạo Blog Category Mẫu (Nếu chưa có)
        if (!await context.BlogCategories.AnyAsync())
        {
            var blogCats = new List<BlogCategory>
            {
                new() { Name = "Kiến thức Nông nghiệp", Slug = "kien-thuc-nong-nghiep", Description = "Chia sẻ kiến thức trồng trọt, chăm sóc cây", DisplayOrder = 1 },
                new() { Name = "Tin tức thị trường", Slug = "tin-tuc-thi-truong", Description = "Cập nhật giá phân bón, thông tin thị trường", DisplayOrder = 2 },
                new() { Name = "Kinh nghiệm sử dụng phân bón", Slug = "kinh-nghiem-su-dung-phan-bon", Description = "Hướng dẫn sử dụng phân bón hiệu quả", DisplayOrder = 3 },
            };
            await context.BlogCategories.AddRangeAsync(blogCats);
            await context.SaveChangesAsync();
        }

        // 6. Tạo Products Mẫu (Nếu chưa có)
        if (!await context.Products.AnyAsync())
        {
            var supplier1 = await context.Suppliers.FirstAsync();
            var supplier2 = await context.Suppliers.Skip(1).FirstOrDefaultAsync() ?? supplier1;

            var catHoaHoc      = await context.ProductCategories.FirstAsync(c => c.Slug == "phan-bon-hoa-hoc");
            var catHuuCo       = await context.ProductCategories.FirstAsync(c => c.Slug == "phan-bon-huu-co");
            var catBVTV        = await context.ProductCategories.FirstAsync(c => c.Slug == "thuoc-bao-ve-thuc-vat");
            var catTruBenh     = await context.ProductCategories.FirstAsync(c => c.Slug == "thuoc-tru-benh");
            var catTruSau      = await context.ProductCategories.FirstAsync(c => c.Slug == "thuoc-tru-sau");
            var catBat         = await context.ProductCategories.FirstAsync(c => c.Slug == "bat-phu-nong-nghiep");

            // ── PHÂN BÓN HÓA HỌC ─────────────────────────────────────
            var ure = new Product
            {
                Name = "Phân Urê 46% Đạm Cà Mau", Slug = "phan-ure-46-ca-mau", SKU = "HH-URE-001",
                ShortDescription = "Phân đạm Urê hạt đục 46% N, tan nhanh, thích hợp bón thúc lúa và rau màu.",
                Description = "<p>Phân Urê 46% Đạm Cà Mau là loại phân đơn chứa 46% Nitơ nguyên chất dạng hạt đục, tan nhanh trong nước. Phù hợp bón thúc cho lúa, bắp, mía, rau màu và các loại cây ăn quả. Giúp cây xanh lá, tăng trưởng mạnh.</p>",
                TechnicalInfo = "<ul><li>Hàm lượng N: 46% min</li><li>Dạng: hạt đục</li><li>Độ ẩm: ≤ 0,5%</li><li>Biuret: ≤ 1,0%</li></ul>",
                UsageInstructions = "<p>Bón thúc: 3–5 kg/sào (500m²). Rải đều, tưới nước sau khi bón. Không bón khi trời mưa to.</p>",
                BasePrice = 350000, StockQuantity = 300, Unit = "Bao 50kg",
                CategoryId = catHoaHoc.Id, SupplierId = supplier1.Id,
                IsFeatured = true, IsActive = true, MinStockLevel = 30, CreatedAt = DateTime.UtcNow
            };
            var npk = new Product
            {
                Name = "Phân NPK 20-20-15 Đầu Trâu", Slug = "phan-npk-20-20-15-dau-trau", SKU = "HH-NPK-001",
                ShortDescription = "Phân NPK tổng hợp 3 thành phần, bón lót và bón thúc cho nhiều loại cây trồng.",
                Description = "<p>Phân NPK 20-20-15 Đầu Trâu cung cấp cân đối đạm, lân, kali giúp cây sinh trưởng đồng đều, tăng sức đề kháng và cải thiện chất lượng nông sản. Dạng hạt dễ rải, tan đều.</p>",
                TechnicalInfo = "<ul><li>N: 20% | P₂O₅: 20% | K₂O: 15%</li><li>Dạng hạt tròn, màu vàng xanh</li></ul>",
                UsageInstructions = "<p>Bón lót: 10–15 kg/sào trước khi trồng. Bón thúc: 5–8 kg/sào chia 2–3 lần trong vụ.</p>",
                BasePrice = 480000, StockQuantity = 200, Unit = "Bao 50kg",
                CategoryId = catHoaHoc.Id, SupplierId = supplier1.Id,
                IsFeatured = true, IsActive = true, MinStockLevel = 20, CreatedAt = DateTime.UtcNow
            };
            var dap = new Product
            {
                Name = "Phân DAP 18-46-0", Slug = "phan-dap-18-46-0", SKU = "HH-DAP-001",
                ShortDescription = "Phân lân DAP hàm lượng cao, bón lót kích rễ mạnh cho lúa và hoa màu.",
                Description = "<p>Phân DAP (Diammonium Phosphate) chứa 18% đạm và 46% lân, dạng hạt màu xám xanh. Sử dụng bón lót trước khi gieo trồng, kích thích rễ phát triển mạnh giai đoạn đầu.</p>",
                TechnicalInfo = "<ul><li>N: 18% | P₂O₅: 46%</li><li>Dạng hạt xám xanh, không mùi</li></ul>",
                UsageInstructions = "<p>Bón lót: 5–8 kg/sào trộn đều vào đất trước khi gieo cấy.</p>",
                BasePrice = 530000, StockQuantity = 150, Unit = "Bao 50kg",
                CategoryId = catHoaHoc.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 15, CreatedAt = DateTime.UtcNow
            };
            var kali = new Product
            {
                Name = "Phân Kali Đỏ KCl 60%", Slug = "phan-kali-do-kcl-60", SKU = "HH-KAL-001",
                ShortDescription = "Kali clorua hàm lượng 60%, tăng phẩm chất trái, chắc hạt, tăng khả năng chịu hạn.",
                Description = "<p>Phân Kali Đỏ (KCl) hàm lượng K₂O đạt 60% min, giúp cây cứng cáp, trái ngọt hơn, hạt chắc mẩy. Đặc biệt hiệu quả giai đoạn nuôi trái và trước thu hoạch.</p>",
                TechnicalInfo = "<ul><li>K₂O: 60% min</li><li>Dạng hạt đỏ hồng</li><li>Cl: ≤ 47%</li></ul>",
                UsageInstructions = "<p>Bón thúc: 2–4 kg/sào, chia 2 lần vào giai đoạn làm đòng và trước thu hoạch 3 tuần.</p>",
                BasePrice = 490000, StockQuantity = 180, Unit = "Bao 50kg",
                CategoryId = catHoaHoc.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 15, CreatedAt = DateTime.UtcNow
            };

            // ── PHÂN BÓN HỮU CƠ ─────────────────────────────────────
            var viSinh = new Product
            {
                Name = "Phân Hữu Cơ Vi Sinh Sông Gianh", Slug = "phan-huu-co-vi-sinh-song-gianh", SKU = "HC-VSG-001",
                ShortDescription = "Phân hữu cơ vi sinh bổ sung vi sinh vật có lợi, cải tạo đất bạc màu hiệu quả.",
                Description = "<p>Phân hữu cơ vi sinh Sông Gianh được sản xuất từ nguyên liệu hữu cơ lên men kết hợp vi sinh vật cố định đạm, phân giải lân. Giúp đất tơi xốp, tăng độ phì nhiêu và giảm sử dụng phân hóa học.</p>",
                TechnicalInfo = "<ul><li>Hữu cơ: ≥ 15%</li><li>Vi sinh vật hữu ích: ≥ 10⁶ CFU/g</li><li>Độ ẩm: ≤ 25%</li></ul>",
                UsageInstructions = "<p>Bón lót: 20–30 kg/sào. Có thể ủ thêm với phân chuồng trước khi bón để tăng hiệu quả.</p>",
                BasePrice = 180000, StockQuantity = 250, Unit = "Bao 25kg",
                CategoryId = catHuuCo.Id, SupplierId = supplier2.Id,
                IsFeatured = true, IsActive = true, MinStockLevel = 25, CreatedAt = DateTime.UtcNow
            };
            var huuCoCompost = new Product
            {
                Name = "Phân Hữu Cơ Compost Trùn Quế", Slug = "phan-huu-co-trun-que", SKU = "HC-TRQ-001",
                ShortDescription = "Phân trùn quế giàu dinh dưỡng, kích thích rễ và hệ vi sinh vật đất.",
                Description = "<p>Phân compost trùn quế là sản phẩm hữu cơ cao cấp từ quá trình trùn quế phân giải chất hữu cơ. Giàu axit humic, dinh dưỡng dễ hấp thu, kích hoạt hệ vi sinh vật đất tự nhiên.</p>",
                TechnicalInfo = "<ul><li>Hữu cơ: ≥ 30%</li><li>pH: 6,5–7,0</li><li>Axit humic: ≥ 8%</li></ul>",
                UsageInstructions = "<p>Dùng cho rau sạch, hoa màu: 3–5 kg/m². Phù hợp canh tác hữu cơ và nhà lưới.</p>",
                BasePrice = 95000, StockQuantity = 300, Unit = "Túi 5kg",
                CategoryId = catHuuCo.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 30, CreatedAt = DateTime.UtcNow
            };
            var huatHuuCo = new Product
            {
                Name = "Phân Bón Lá Hữu Cơ Amino Acid", Slug = "phan-bon-la-amino-acid", SKU = "HC-AMI-001",
                ShortDescription = "Phân bón lá giàu axit amin, tăng đề kháng và phục hồi cây sau stress.",
                Description = "<p>Sản phẩm chiết xuất từ thủy phân protein thực vật và động vật, cung cấp acid amin tự do cho cây hấp thu qua lá nhanh chóng. Giúp cây phục hồi nhanh sau sâu bệnh, hạn hán.</p>",
                TechnicalInfo = "<ul><li>Amino acid tổng: ≥ 10%</li><li>N hữu cơ: ≥ 5%</li><li>Dạng lỏng nâu đậm</li></ul>",
                UsageInstructions = "<p>Pha 10–15 ml/bình 16 lít nước. Phun đều lên lá 7–10 ngày/lần.</p>",
                BasePrice = 125000, StockQuantity = 400, Unit = "Chai 1 lít",
                CategoryId = catHuuCo.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 40, CreatedAt = DateTime.UtcNow
            };
            var biomix = new Product
            {
                Name = "Phân Hữu Cơ Khoáng Bio-Mix NPK", Slug = "phan-huu-co-khoang-bio-mix", SKU = "HC-BIO-001",
                ShortDescription = "Phân hữu cơ khoáng kết hợp, bổ sung dinh dưỡng và cải tạo đất đồng thời.",
                Description = "<p>Bio-Mix NPK là dòng phân hữu cơ khoáng kết hợp tối ưu giữa nguồn dinh dưỡng hữu cơ và khoáng chất, phù hợp canh tác VietGAP và GlobalGAP. Giảm tồn dư hóa chất trong đất.</p>",
                TechnicalInfo = "<ul><li>Hữu cơ: ≥ 12% | N+P₂O₅+K₂O: ≥ 8%</li><li>Vi lượng: Ca, Mg, S, Zn, B</li></ul>",
                UsageInstructions = "<p>Bón lót 15–20 kg/sào. Có thể kết hợp bón thúc 7–10 kg/sào giữa vụ.</p>",
                BasePrice = 220000, StockQuantity = 180, Unit = "Bao 25kg",
                CategoryId = catHuuCo.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 20, CreatedAt = DateTime.UtcNow
            };

            // ── THUỐC BẢO VỆ THỰC VẬT (tổng hợp) ───────────────────
            var score = new Product
            {
                Name = "Score 250EC – Trừ Nấm Phổ Rộng", Slug = "score-250ec-tru-nam", SKU = "BV-SCR-001",
                ShortDescription = "Thuốc trừ nấm phổ rộng, hiệu quả trên đạo ôn, thán thư, phấn trắng.",
                Description = "<p>Score 250EC chứa hoạt chất Difenoconazole 250 g/l, thuộc nhóm triazole, phòng trừ nhiều loại nấm gây bệnh trên lúa, cà phê, rau màu và cây ăn quả.</p>",
                TechnicalInfo = "<ul><li>Hoạt chất: Difenoconazole 250 g/l</li><li>Nhóm độc: III</li><li>Dạng: EC (nhũ tương)</li></ul>",
                UsageInstructions = "<p>Pha 10–15 ml/bình 16 lít. Phun phòng 7 ngày/lần, phun trị 5 ngày/lần. PHI: 14 ngày.</p>",
                BasePrice = 85000, StockQuantity = 300, Unit = "Chai 100ml",
                CategoryId = catBVTV.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 30, CreatedAt = DateTime.UtcNow
            };
            var virtako = new Product
            {
                Name = "Virtako 40WG – Trừ Sâu Cuốn Lá & Đục Thân", Slug = "virtako-40wg", SKU = "BV-VTK-001",
                ShortDescription = "Thuốc trừ sâu cao cấp 2 hoạt chất, diệt sâu cuốn lá, sâu đục thân lúa.",
                Description = "<p>Virtako 40WG là thuốc trừ sâu chứa hỗn hợp Thiamethoxam 20% và Chlorantraniliprole 20%, tác động tiếp xúc và vị độc, hiệu lực kéo dài 14–21 ngày trên đồng lúa.</p>",
                TechnicalInfo = "<ul><li>Thiamethoxam: 20% | Chlorantraniliprole: 20%</li><li>Dạng: WG (hạt thấm nước)</li><li>Nhóm độc: II</li></ul>",
                UsageInstructions = "<p>Pha 2,5–3 g/bình 16 lít. Phun vào giai đoạn sâu tuổi 1–2. PHI: 21 ngày.</p>",
                BasePrice = 145000, StockQuantity = 200, Unit = "Gói 6g",
                CategoryId = catBVTV.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 20, CreatedAt = DateTime.UtcNow
            };
            var roundup = new Product
            {
                Name = "Roundup 480SL – Thuốc Diệt Cỏ Hệ Thống", Slug = "roundup-480sl-diet-co", SKU = "BV-RND-001",
                ShortDescription = "Thuốc diệt cỏ toàn phần, trừ cỏ lá rộng và lá hẹp, hấp thu qua lá.",
                Description = "<p>Roundup 480SL chứa Glyphosate isopropylamine salt 480 g/l, tác động hệ thống, hấp thu qua lá và chuyển đến rễ, tiêu diệt toàn bộ cỏ dại trong 7–14 ngày.</p>",
                TechnicalInfo = "<ul><li>Glyphosate 480 g/l</li><li>Dạng: SL (dung dịch)</li><li>Nhóm độc: III</li></ul>",
                UsageInstructions = "<p>Pha 40–60 ml/bình 16 lít. Phun lên cỏ khi cỏ đang sinh trưởng mạnh, tránh phun lên cây trồng.</p>",
                BasePrice = 78000, StockQuantity = 350, Unit = "Chai 1 lít",
                CategoryId = catBVTV.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 35, CreatedAt = DateTime.UtcNow
            };
            var phytocide = new Product
            {
                Name = "Regent 800WG – Trừ Rầy Nâu Hại Lúa", Slug = "regent-800wg-tru-ray-nau", SKU = "BV-RGT-001",
                ShortDescription = "Thuốc trừ rầy nâu và rầy xanh, tác động tiếp xúc và vị độc mạnh.",
                Description = "<p>Regent 800WG chứa Fipronil 800 g/kg, thuộc nhóm phenylpyrazole, hiệu quả vượt trội trong phòng trừ rầy nâu, bọ trĩ, sâu năn trên lúa.</p>",
                TechnicalInfo = "<ul><li>Fipronil: 800 g/kg</li><li>Dạng: WG</li><li>Nhóm độc: II</li></ul>",
                UsageInstructions = "<p>Pha 0,75–1 g/bình 16 lít. Phun khi mật độ rầy đạt ngưỡng kinh tế. PHI: 14 ngày.</p>",
                BasePrice = 68000, StockQuantity = 250, Unit = "Gói 30g",
                CategoryId = catBVTV.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 25, CreatedAt = DateTime.UtcNow
            };

            // ── THUỐC TRỪ BỆNH ───────────────────────────────────────
            var anvil = new Product
            {
                Name = "Anvil 5SC – Trừ Bệnh Khô Vằn Lúa", Slug = "anvil-5sc-tru-kho-van", SKU = "TB-ANV-001",
                ShortDescription = "Đặc trị khô vằn, lem lép hạt, bệnh đạo ôn cổ bông trên lúa.",
                Description = "<p>Anvil 5SC chứa Hexaconazole 50 g/l, tác động nội hấp qua lá và thân, phòng trừ hiệu quả bệnh khô vằn, đạo ôn, lem lép hạt trên lúa trong điều kiện độ ẩm cao.</p>",
                TechnicalInfo = "<ul><li>Hexaconazole: 50 g/l</li><li>Dạng: SC (huyền phù)</li><li>Nhóm độc: III</li></ul>",
                UsageInstructions = "<p>Pha 15–20 ml/bình 16 lít. Phun trước khi trổ bông 5–7 ngày. PHI: 21 ngày.</p>",
                BasePrice = 72000, StockQuantity = 280, Unit = "Chai 100ml",
                CategoryId = catTruBenh.Id, SupplierId = supplier1.Id,
                IsActive = true, IsFeatured = true, MinStockLevel = 28, CreatedAt = DateTime.UtcNow
            };
            var ridomil = new Product
            {
                Name = "Ridomil Gold 68WP – Trừ Bệnh Sương Mai", Slug = "ridomil-gold-68wp", SKU = "TB-RDM-001",
                ShortDescription = "Thuốc trừ nấm nội hấp đặc trị sương mai, chết rạp trên cà chua, khoai tây.",
                Description = "<p>Ridomil Gold 68WP chứa hỗn hợp Metalaxyl-M 4% và Mancozeb 64%, bảo vệ kép cả phòng và trị bệnh sương mai, thối nhũn trên họ cà, họ bầu bí.</p>",
                TechnicalInfo = "<ul><li>Metalaxyl-M: 4% | Mancozeb: 64%</li><li>Dạng: WP (bột thấm nước)</li><li>Nhóm độc: III</li></ul>",
                UsageInstructions = "<p>Pha 25–30 g/bình 16 lít. Phun phòng 7 ngày/lần khi thời tiết ẩm ướt. PHI: 7 ngày.</p>",
                BasePrice = 58000, StockQuantity = 320, Unit = "Gói 50g",
                CategoryId = catTruBenh.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 32, CreatedAt = DateTime.UtcNow
            };
            var thiram = new Product
            {
                Name = "Carbendazim 500SC – Trừ Thán Thư, Phấn Trắng", Slug = "carbendazim-500sc-than-thu", SKU = "TB-CBD-001",
                ShortDescription = "Thuốc nội hấp phổ rộng, trừ thán thư, mốc sương, phấn trắng trên nhiều cây.",
                Description = "<p>Carbendazim 500SC tác động nội hấp 2 chiều, hiệu quả cao trên các bệnh do nấm Colletotrichum, Botrytis, Erysiphe gây ra trên ớt, xoài, thanh long và rau màu.</p>",
                TechnicalInfo = "<ul><li>Carbendazim: 500 g/l</li><li>Dạng: SC</li><li>Nhóm độc: III</li></ul>",
                UsageInstructions = "<p>Pha 10–15 ml/bình 16 lít. Phun luân phiên với nhóm thuốc khác để tránh kháng thuốc. PHI: 7 ngày.</p>",
                BasePrice = 45000, StockQuantity = 400, Unit = "Chai 100ml",
                CategoryId = catTruBenh.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 40, CreatedAt = DateTime.UtcNow
            };
            var kasugamycin = new Product
            {
                Name = "Kasumin 2SL – Trừ Bệnh Bạc Lá Lúa", Slug = "kasumin-2sl-bac-la-lua", SKU = "TB-KSM-001",
                ShortDescription = "Kháng sinh nông nghiệp đặc trị bạc lá, cháy bìa lá do vi khuẩn Xanthomonas.",
                Description = "<p>Kasumin 2SL chứa Kasugamycin 2 g/l, hoạt chất kháng sinh nguồn gốc vi sinh vật, đặc trị bệnh bạc lá do vi khuẩn Xanthomonas oryzae gây hại nặng trên lúa vụ Hè Thu.</p>",
                TechnicalInfo = "<ul><li>Kasugamycin: 2 g/l</li><li>Dạng: SL</li><li>Nhóm độc: III</li></ul>",
                UsageInstructions = "<p>Pha 20–25 ml/bình 16 lít. Phun khi phát hiện triệu chứng đầu tiên. PHI: 14 ngày.</p>",
                BasePrice = 92000, StockQuantity = 220, Unit = "Chai 100ml",
                CategoryId = catTruBenh.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 22, CreatedAt = DateTime.UtcNow
            };

            // ── THUỐC TRỪ SÂU ────────────────────────────────────────
            var abamectin = new Product
            {
                Name = "Abamectin 1.8EC – Trừ Nhện Đỏ, Bọ Trĩ", Slug = "abamectin-18ec-nhen-do", SKU = "TS-ABA-001",
                ShortDescription = "Thuốc sinh học nguồn gốc vi sinh, đặc trị nhện đỏ, bọ trĩ và sâu tơ.",
                Description = "<p>Abamectin 1.8EC chiết xuất từ Streptomyces avermitilis, tác động thần kinh côn trùng, hiệu quả cao với nhện đỏ, bọ trĩ, sâu tơ kháng thuốc trên rau, cây ăn quả.</p>",
                TechnicalInfo = "<ul><li>Abamectin: 18 g/l</li><li>Dạng: EC</li><li>Nhóm độc: II</li></ul>",
                UsageInstructions = "<p>Pha 7–10 ml/bình 16 lít. Phun ướt đều tán lá, kể cả mặt dưới lá. PHI: 7 ngày.</p>",
                BasePrice = 55000, StockQuantity = 350, Unit = "Chai 100ml",
                CategoryId = catTruSau.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 35, CreatedAt = DateTime.UtcNow
            };
            var chlorpyrifos = new Product
            {
                Name = "Chlorpyrifos 40EC – Trừ Sâu Đất, Mối", Slug = "chlorpyrifos-40ec-sau-dat", SKU = "TS-CHL-001",
                ShortDescription = "Thuốc trừ sâu tiếp xúc, vị độc diệt sâu đất, mối, bọ hung hại rễ.",
                Description = "<p>Chlorpyrifos 40EC chứa Chlorpyrifos ethyl, hiệu quả trừ sâu đất, mối, dế, bọ hung phá hại rễ cây trồng. Phổ biến trong xử lý đất vườn cây ăn quả trước khi trồng.</p>",
                TechnicalInfo = "<ul><li>Chlorpyrifos ethyl: 400 g/l</li><li>Dạng: EC</li><li>Nhóm độc: II</li></ul>",
                UsageInstructions = "<p>Tưới đất: pha 15–20 ml/10 lít nước, tưới 0,5–1 lít/m². Phun cây: pha 15 ml/bình 16 lít.</p>",
                BasePrice = 62000, StockQuantity = 280, Unit = "Chai 480ml",
                CategoryId = catTruSau.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 28, CreatedAt = DateTime.UtcNow
            };
            var emamectin = new Product
            {
                Name = "Emamectin 0.2EC – Trừ Sâu Xanh Đầu Đen", Slug = "emamectin-02ec-sau-xanh", SKU = "TS-EMA-001",
                ShortDescription = "Thuốc sinh học hiệu lực cao, trừ sâu xanh đầu đen táo và sâu ăn lá.",
                Description = "<p>Emamectin benzoate 0.2EC tác động thần kinh, làm tê liệt và chết sâu trong 24–48 giờ. Đặc biệt hiệu quả với sâu xanh đầu đen trên táo, sâu ăn tạp rau, sâu kéo màng trên dưa.</p>",
                TechnicalInfo = "<ul><li>Emamectin benzoate: 2 g/l</li><li>Dạng: EC</li><li>Nhóm độc: II</li></ul>",
                UsageInstructions = "<p>Pha 5–8 ml/bình 16 lít. Phun khi sâu tuổi 1–2. PHI: 7 ngày.</p>",
                BasePrice = 78000, StockQuantity = 300, Unit = "Chai 100ml",
                CategoryId = catTruSau.Id, SupplierId = supplier2.Id,
                IsActive = true, IsFeatured = true, MinStockLevel = 30, CreatedAt = DateTime.UtcNow
            };
            var indoxacarb = new Product
            {
                Name = "Indoxacarb 150SC – Trừ Sâu Kháng Thuốc", Slug = "indoxacarb-150sc-tru-sau", SKU = "TS-IDX-001",
                ShortDescription = "Hoạt chất thế hệ mới, hiệu quả với sâu đã kháng các nhóm thuốc cũ.",
                Description = "<p>Indoxacarb 150SC thuộc nhóm oxadiazine, cơ chế tác động mới, không bị kháng chéo với các nhóm thuốc khác. Phổ trừ sâu rộng, hiệu lực kéo dài 10–14 ngày.</p>",
                TechnicalInfo = "<ul><li>Indoxacarb: 150 g/l</li><li>Dạng: SC</li><li>Nhóm độc: II</li></ul>",
                UsageInstructions = "<p>Pha 8–10 ml/bình 16 lít. Luân phiên với Emamectin hoặc Abamectin để quản lý kháng thuốc.</p>",
                BasePrice = 110000, StockQuantity = 180, Unit = "Chai 100ml",
                CategoryId = catTruSau.Id, SupplierId = supplier1.Id,
                IsActive = true, MinStockLevel = 18, CreatedAt = DateTime.UtcNow
            };

            // ── BẠT PHỦ NÔNG NGHIỆP ─────────────────────────────────
            var batBac = new Product
            {
                Name = "Bạt Phủ Luống Bạc Đen PE", Slug = "bat-phu-luong-bac-den-pe", SKU = "BP-BDE-001",
                ShortDescription = "Màng phủ nông nghiệp 2 mặt bạc-đen, ức chế cỏ dại, giữ ẩm hiệu quả.",
                Description = "<p>Bạt phủ luống bạc-đen PE dày 25 micron, mặt bạc phản chiếu ánh sáng hạn chế sâu bệnh, mặt đen ức chế cỏ dại 100%. Phù hợp trồng dưa, ớt, cà, bí, dâu tây.</p>",
                TechnicalInfo = "<ul><li>Vật liệu: LDPE</li><li>Độ dày: 25 micron</li><li>Màu: bạc/đen 2 mặt</li></ul>",
                UsageInstructions = "<p>Trải bạt trước khi trồng, đục lỗ phù hợp khoảng cách cây. Cố định 2 cạnh bằng đất.</p>",
                BasePrice = 320000, StockQuantity = 120, Unit = "Cuộn 200m",
                CategoryId = catBat.Id, SupplierId = supplier2.Id,
                IsActive = true, IsFeatured = true, MinStockLevel = 12, CreatedAt = DateTime.UtcNow
            };
            var batTrang = new Product
            {
                Name = "Bạt Trắng Chống Rét Nhà Màng PE", Slug = "bat-trang-chong-ret-nha-mang", SKU = "BP-TRG-001",
                ShortDescription = "Màng phủ trắng trong PE che phủ vườn ươm, nhà màng chống rét và mưa đá.",
                Description = "<p>Màng phủ trắng trong PE được xử lý UV chống lão hóa, tuổi thọ 18–24 tháng. Truyền sáng 85%, phù hợp làm mái nhà lưới, vườn ươm và che phủ tạm thời.</p>",
                TechnicalInfo = "<ul><li>Vật liệu: LDPE + UV stabilizer</li><li>Độ truyền sáng: 85%</li><li>Độ dày: 50–100 micron</li></ul>",
                UsageInstructions = "<p>Căng bạt trên khung nhà lưới hoặc vòm cung. Chằng dây cẩn thận khi gặp gió mạnh.</p>",
                BasePrice = 450000, StockQuantity = 80, Unit = "Cuộn 100m",
                CategoryId = catBat.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 8, CreatedAt = DateTime.UtcNow
            };
            var batXanhBac = new Product
            {
                Name = "Lưới Che Nắng 50% Xanh-Bạc", Slug = "luoi-che-nang-50-xanh-bac", SKU = "BP-LCN-001",
                ShortDescription = "Lưới che nắng 50% dùng cho vườn rau màu, nhà kính giảm nhiệt mùa hè.",
                Description = "<p>Lưới che nắng 50% dệt HDPE 2 màu xanh-bạc, chịu nhiệt UV cao, tuổi thọ 3–5 năm. Giảm nhiệt độ vườn 5–7°C, phù hợp rau ăn lá, hoa, cây giống.</p>",
                TechnicalInfo = "<ul><li>Vật liệu: HDPE UV</li><li>Tỷ lệ che nắng: 50%</li><li>Kích cỡ lưới: 5×50m</li></ul>",
                UsageInstructions = "<p>Căng cố định trên giàn hoặc nhà lưới. Giặt định kỳ cuối vụ để tăng tuổi thọ.</p>",
                BasePrice = 380000, StockQuantity = 100, Unit = "Cuộn 5×50m",
                CategoryId = catBat.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 10, CreatedAt = DateTime.UtcNow
            };
            var batMulch = new Product
            {
                Name = "Bạt Phủ Đất Sinh Thái Tự Hủy PLA", Slug = "bat-phu-dat-sinh-thai-pla", SKU = "BP-PLA-001",
                ShortDescription = "Màng phủ sinh thái tự phân hủy 60–90 ngày, thân thiện môi trường.",
                Description = "<p>Bạt phủ đất PLA (polylactic acid) phân hủy sinh học hoàn toàn trong 60–90 ngày sau thu hoạch. Không gây ô nhiễm đất, phù hợp tiêu chuẩn hữu cơ và xuất khẩu.</p>",
                TechnicalInfo = "<ul><li>Vật liệu: PLA sinh học</li><li>Thời gian phân hủy: 60–90 ngày</li><li>Độ dày: 15 micron</li></ul>",
                UsageInstructions = "<p>Trải bạt và trồng cây bình thường. Sau thu hoạch cày cùng bạt vào đất, bạt tự phân hủy.</p>",
                BasePrice = 520000, StockQuantity = 60, Unit = "Cuộn 200m",
                CategoryId = catBat.Id, SupplierId = supplier2.Id,
                IsActive = true, MinStockLevel = 6, CreatedAt = DateTime.UtcNow
            };

            var allProducts = new List<Product>
            {
                ure, npk, dap, kali,
                viSinh, huuCoCompost, huatHuuCo, biomix,
                score, virtako, roundup, phytocide,
                anvil, ridomil, thiram, kasugamycin,
                abamectin, chlorpyrifos, emamectin, indoxacarb,
                batBac, batTrang, batXanhBac, batMulch
            };
            await context.Products.AddRangeAsync(allProducts);
            await context.SaveChangesAsync();

            // ── VARIANTS ────────────────────────────────────────────
            // Phân Urê: 3 quy cách đóng gói
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = ure.Id, VariantName = "Bao 50kg", SKU = "HH-URE-50", Price = 350000, StockQuantity = 200, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = ure.Id, VariantName = "Bao 25kg", SKU = "HH-URE-25", Price = 180000, StockQuantity = 100, DisplayOrder = 2, IsActive = true },
                new ProductVariant { ProductId = ure.Id, VariantName = "Bao 10kg", SKU = "HH-URE-10", Price = 78000,  StockQuantity = 150, DisplayOrder = 3, IsActive = true }
            );
            // NPK Đầu Trâu: 2 công thức
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = npk.Id, VariantName = "NPK 20-20-15 / 50kg", SKU = "HH-NPK-50", Price = 480000, StockQuantity = 200, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = npk.Id, VariantName = "NPK 16-16-8 / 50kg",  SKU = "HH-NPK-1668", Price = 420000, StockQuantity = 150, DisplayOrder = 2, IsActive = true }
            );
            // Compost trùn quế: 3 quy cách
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = huuCoCompost.Id, VariantName = "Túi 5kg",  SKU = "HC-TRQ-5",  Price = 95000,  StockQuantity = 300, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = huuCoCompost.Id, VariantName = "Bao 20kg", SKU = "HC-TRQ-20", Price = 320000, StockQuantity = 150, DisplayOrder = 2, IsActive = true },
                new ProductVariant { ProductId = huuCoCompost.Id, VariantName = "Bao 50kg", SKU = "HC-TRQ-50", Price = 750000, StockQuantity = 80,  DisplayOrder = 3, IsActive = true }
            );
            // Amino Acid: 3 dung tích
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = huatHuuCo.Id, VariantName = "Chai 500ml", SKU = "HC-AMI-500", Price = 68000,  StockQuantity = 200, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = huatHuuCo.Id, VariantName = "Chai 1 lít", SKU = "HC-AMI-1L",  Price = 125000, StockQuantity = 400, DisplayOrder = 2, IsActive = true },
                new ProductVariant { ProductId = huatHuuCo.Id, VariantName = "Can 5 lít",  SKU = "HC-AMI-5L",  Price = 580000, StockQuantity = 120, DisplayOrder = 3, IsActive = true }
            );
            // Score: 2 dung tích
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = score.Id, VariantName = "Chai 100ml", SKU = "BV-SCR-100", Price = 85000,  StockQuantity = 300, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = score.Id, VariantName = "Chai 250ml", SKU = "BV-SCR-250", Price = 195000, StockQuantity = 150, DisplayOrder = 2, IsActive = true }
            );
            // Roundup: 2 dung tích
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = roundup.Id, VariantName = "Chai 1 lít", SKU = "BV-RND-1L",  Price = 78000,  StockQuantity = 350, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = roundup.Id, VariantName = "Can 5 lít",  SKU = "BV-RND-5L",  Price = 360000, StockQuantity = 100, DisplayOrder = 2, IsActive = true }
            );
            // Bạt bạc-đen: 2 khổ cuộn
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = batBac.Id, VariantName = "Khổ 0,9m × 200m", SKU = "BP-BDE-09", Price = 280000, StockQuantity = 120, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = batBac.Id, VariantName = "Khổ 1,2m × 200m", SKU = "BP-BDE-12", Price = 360000, StockQuantity = 100, DisplayOrder = 2, IsActive = true },
                new ProductVariant { ProductId = batBac.Id, VariantName = "Khổ 1,6m × 200m", SKU = "BP-BDE-16", Price = 450000, StockQuantity = 80,  DisplayOrder = 3, IsActive = true }
            );
            // Lưới che nắng: 2 mức che
            context.ProductVariants.AddRange(
                new ProductVariant { ProductId = batXanhBac.Id, VariantName = "Che nắng 50% – 5×50m",  SKU = "BP-LCN-50", Price = 380000, StockQuantity = 100, DisplayOrder = 1, IsActive = true },
                new ProductVariant { ProductId = batXanhBac.Id, VariantName = "Che nắng 75% – 5×50m",  SKU = "BP-LCN-75", Price = 420000, StockQuantity = 80,  DisplayOrder = 2, IsActive = true }
            );

            await context.SaveChangesAsync();
        }
    }
}
