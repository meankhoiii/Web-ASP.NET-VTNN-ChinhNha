using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using ChinhNha.Application.Interfaces;

namespace ChinhNha.Infrastructure.Data;

public static class DbSeeder
{
    private const string DefaultAdminEmail = "minhkhoi78757@gmail.com";
    private const string LegacyAdminEmail = "admin@chinhnha.id.vn";
    private const string MistypedAdminEmail = "minhkhoi78797@gmail.com";
    private const string DefaultAdminPassword = "Minhkhoi78757";
    private const string DefaultAdminFullName = "Administrator";

    public static async Task SeedAsync(AppDbContext context, IPasswordHashService passwordHashService)
    {
        // 1. Upsert Admin User
        var normalizedDefaultEmail = DefaultAdminEmail.ToLower();
        var normalizedLegacyEmail = LegacyAdminEmail.ToLower();
        var normalizedMistypedEmail = MistypedAdminEmail.ToLower();

        var adminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedDefaultEmail);

        if (adminUser == null)
        {
            adminUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedLegacyEmail);
        }

        if (adminUser == null)
        {
            adminUser = await context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedMistypedEmail);
        }

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                Email = DefaultAdminEmail,
                FullName = DefaultAdminFullName,
                Role = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PasswordHash = passwordHashService.HashPassword(DefaultAdminPassword)
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }

        var mistypedAdminUser = await context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedMistypedEmail && u.Id != adminUser.Id);
        if (mistypedAdminUser != null && mistypedAdminUser.Role == "Admin")
        {
            mistypedAdminUser.Role = "Customer";
            await context.SaveChangesAsync();
        }
        else
        {
            adminUser.Email = DefaultAdminEmail;
            adminUser.FullName = string.IsNullOrWhiteSpace(adminUser.FullName)
                ? DefaultAdminFullName
                : adminUser.FullName;
            adminUser.Role = "Admin";
            adminUser.IsActive = true;
            adminUser.PasswordHash = passwordHashService.HashPassword(DefaultAdminPassword);

            await context.SaveChangesAsync();
        }

        // 2. Tạo Supplier Mẫu (Nếu chưa có)
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

        // 3. Tạo Category Mẫu (Nếu chưa có)
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

        // 4. Tạo Blog Category Mẫu (Nếu chưa có)
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

        // 5. Tạo Products Mẫu (Nếu chưa có)
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

        await SeedAdvancedDemandDataAsync(context, passwordHashService, adminUser);
    }

    private static async Task SeedAdvancedDemandDataAsync(
        AppDbContext context,
        IPasswordHashService passwordHashService,
        AppUser adminUser)
    {
        const string seedMarkerKey = "seed-demo-2years-v1";
        var markerExists = await context.SiteSettings
            .AnyAsync(s => s.Group == "Seed" && s.Key == seedMarkerKey);

        if (markerExists)
        {
            return;
        }

        var products = await context.Products
            .Include(p => p.Variants)
            .Where(p => p.IsActive)
            .ToListAsync();

        if (!products.Any())
        {
            return;
        }

        var seededCustomers = await EnsureSeedCustomersAsync(context, passwordHashService, 20);

        var random = new Random(20260314);
        var startDate = DateTime.UtcNow.Date.AddDays(-730); // 2 năm
        var endDate = DateTime.UtcNow.Date.AddDays(-1);

        var productDemandByDate = new Dictionary<(DateTime Day, int ProductId), int>();
        var productReturnByDate = new Dictionary<(DateTime Day, int ProductId), int>();

        var orders = new List<Order>(4000);
        var payments = new List<Payment>(4000);
        var orderCodeSeq = 1;

        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var orderCount = CalculateDailyOrderCount(day, random);

            for (var i = 0; i < orderCount; i++)
            {
                var isGuest = random.NextDouble() < 0.58;
                var customer = isGuest ? null : seededCustomers[random.Next(seededCustomers.Count)];

                var status = PickOrderStatus(random);
                var orderDate = day
                    .AddHours(7 + random.Next(12))
                    .AddMinutes(random.Next(0, 60));

                var targetBasket = PickTargetBasketValue(random);
                var selectedItems = BuildOrderItemsForBasket(products, targetBasket, random);
                if (!selectedItems.Any())
                {
                    continue;
                }

                var subtotal = selectedItems.Sum(x => x.UnitPrice * x.Quantity);
                var shipping = 30000m;
                var discountRate = random.NextDouble() < 0.18
                    ? (decimal)(0.03 + random.NextDouble() * 0.07)
                    : 0m;
                var discount = Math.Round(subtotal * discountRate, 0);
                var totalAmount = Math.Max(0, subtotal + shipping - discount);

                var receiverName = customer?.FullName ?? PickGuestName(random);
                var receiverPhone = customer?.Phone ?? PickGuestPhone(random);
                var receiverEmail = customer?.Email;

                var order = new Order
                {
                    OrderCode = $"S2{day:yyyyMMdd}{random.Next(1000, 9999)}{orderCodeSeq % 10000:0000}",
                    UserId = customer?.Id,
                    OrderDate = orderDate,
                    Status = status,
                    SubTotal = subtotal,
                    ShippingFee = shipping,
                    Discount = discount,
                    TotalAmount = totalAmount,
                    ReceiverName = receiverName,
                    ReceiverPhone = receiverPhone,
                    ReceiverEmail = receiverEmail,
                    ShippingProvince = "Cần Thơ",
                    ShippingDistrict = PickDistrict(random),
                    ShippingWard = "Phường An Bình",
                    ShippingAddress = PickAddress(random),
                    Note = random.NextDouble() < 0.28 ? "VNPay Test" : "Đơn seed mùa vụ",
                    CreatedAt = orderDate,
                    UpdatedAt = orderDate.AddHours(random.Next(4, 72))
                };

                foreach (var itemSeed in selectedItems)
                {
                    var lineTotal = itemSeed.UnitPrice * itemSeed.Quantity;
                    var orderItem = new OrderItem
                    {
                        ProductId = itemSeed.Product.Id,
                        ProductVariantId = itemSeed.Variant?.Id,
                        ProductName = itemSeed.Product.Name,
                        VariantName = itemSeed.Variant?.VariantName,
                        UnitPrice = itemSeed.UnitPrice,
                        Quantity = itemSeed.Quantity,
                        TotalPrice = lineTotal
                    };

                    order.OrderItems.Add(orderItem);

                    if (status is OrderStatus.Confirmed
                        or OrderStatus.Processing
                        or OrderStatus.Shipping
                        or OrderStatus.Delivered)
                    {
                        var key = (day, itemSeed.Product.Id);
                        productDemandByDate[key] = productDemandByDate.TryGetValue(key, out var existing)
                            ? existing + itemSeed.Quantity
                            : itemSeed.Quantity;
                    }

                    if (status == OrderStatus.Returned)
                    {
                        var key = (day, itemSeed.Product.Id);
                        productReturnByDate[key] = productReturnByDate.TryGetValue(key, out var existing)
                            ? existing + itemSeed.Quantity
                            : itemSeed.Quantity;
                    }
                }

                var paymentMethod = PickPaymentMethod(random);
                var paymentStatus = ResolvePaymentStatus(status, paymentMethod, random);

                payments.Add(new Payment
                {
                    Order = order,
                    PaymentMethod = paymentMethod,
                    PaymentStatus = paymentStatus,
                    Amount = totalAmount,
                    TransactionId = paymentMethod == PaymentMethod.VNPay && paymentStatus == PaymentStatus.Paid
                        ? $"VNP-{day:yyyyMMdd}-{orderCodeSeq:000000}"
                        : null,
                    PaidAt = paymentStatus == PaymentStatus.Paid ? orderDate.AddMinutes(random.Next(5, 120)) : null,
                    Note = paymentMethod == PaymentMethod.VNPay
                        ? "VNPay Test"
                        : "Seed dữ liệu thanh toán",
                    CreatedAt = orderDate
                });

                orders.Add(order);
                orderCodeSeq++;
            }
        }

        if (orders.Any())
        {
            await context.Orders.AddRangeAsync(orders);
            await context.Payments.AddRangeAsync(payments);
            await context.SaveChangesAsync();
        }

        var productStock = products.ToDictionary(
            p => p.Id,
            p => Math.Max(p.StockQuantity, 180 + random.Next(40, 160)));

        var inventoryTx = new List<InventoryTransaction>(12000);

        foreach (var product in products)
        {
            var initialQty = productStock[product.Id];
            inventoryTx.Add(new InventoryTransaction
            {
                ProductId = product.Id,
                TransactionType = TransactionType.Import,
                Quantity = initialQty,
                StockBefore = 0,
                StockAfter = initialQty,
                ReferenceType = "SeedOpening",
                Note = "Seed tồn đầu kỳ 2 năm",
                CreatedById = adminUser.Id,
                CreatedAt = startDate.AddDays(-1).AddHours(8)
            });
        }

        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            var seasonalFactor = GetSeasonalDemandMultiplier(day.Month);

            foreach (var product in products)
            {
                var productId = product.Id;
                var currentStock = productStock[productId];

                if (day.DayOfWeek == DayOfWeek.Monday
                    && (currentStock < product.MinStockLevel * 3 || random.NextDouble() < 0.08 * (double)seasonalFactor))
                {
                    var importQty = Math.Max(40, (int)Math.Round((80 + random.Next(40, 220)) * seasonalFactor));
                    var before = currentStock;
                    currentStock += importQty;
                    productStock[productId] = currentStock;

                    inventoryTx.Add(new InventoryTransaction
                    {
                        ProductId = productId,
                        TransactionType = TransactionType.Import,
                        Quantity = importQty,
                        StockBefore = before,
                        StockAfter = currentStock,
                        ReferenceType = "PurchaseOrder",
                        Note = "Seed nhập hàng theo mùa vụ",
                        UnitCost = Math.Round(product.BasePrice * 0.72m, 0),
                        CreatedById = adminUser.Id,
                        CreatedAt = day.AddHours(9)
                    });
                }

                var exportKey = (day, productId);
                if (productDemandByDate.TryGetValue(exportKey, out var exportQty) && exportQty > 0)
                {
                    if (currentStock < exportQty)
                    {
                        var topUp = Math.Max(exportQty - currentStock + 30, 50);
                        var beforeImport = currentStock;
                        currentStock += topUp;
                        productStock[productId] = currentStock;

                        inventoryTx.Add(new InventoryTransaction
                        {
                            ProductId = productId,
                            TransactionType = TransactionType.Import,
                            Quantity = topUp,
                            StockBefore = beforeImport,
                            StockAfter = currentStock,
                            ReferenceType = "EmergencyStock",
                            Note = "Seed bù tồn phục vụ xuất hàng",
                            UnitCost = Math.Round(product.BasePrice * 0.74m, 0),
                            CreatedById = adminUser.Id,
                            CreatedAt = day.AddHours(10)
                        });
                    }

                    var beforeExport = currentStock;
                    currentStock = Math.Max(0, currentStock - exportQty);
                    productStock[productId] = currentStock;

                    inventoryTx.Add(new InventoryTransaction
                    {
                        ProductId = productId,
                        TransactionType = TransactionType.Export,
                        Quantity = exportQty,
                        StockBefore = beforeExport,
                        StockAfter = currentStock,
                        ReferenceType = "Order",
                        Note = "Seed xuất kho theo đơn hàng",
                        CreatedById = adminUser.Id,
                        CreatedAt = day.AddHours(14)
                    });
                }

                if (productReturnByDate.TryGetValue(exportKey, out var returnQty) && returnQty > 0)
                {
                    var beforeReturn = currentStock;
                    currentStock += returnQty;
                    productStock[productId] = currentStock;

                    inventoryTx.Add(new InventoryTransaction
                    {
                        ProductId = productId,
                        TransactionType = TransactionType.Return,
                        Quantity = returnQty,
                        StockBefore = beforeReturn,
                        StockAfter = currentStock,
                        ReferenceType = "OrderReturn",
                        Note = "Seed hoàn kho từ đơn trả",
                        CreatedById = adminUser.Id,
                        CreatedAt = day.AddHours(16)
                    });
                }
            }
        }

        await context.InventoryTransactions.AddRangeAsync(inventoryTx);

        foreach (var product in products)
        {
            product.StockQuantity = productStock[product.Id];
            product.UpdatedAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();

        // Seed historical forecasts with ActualDemand + MAPE for backtest (3-6 tháng)
        var forecastRows = new List<InventoryForecast>(2000);
        var forecastStart = DateTime.UtcNow.Date.AddMonths(-6);
        var forecastEnd = DateTime.UtcNow.Date.AddDays(-7);

        foreach (var product in products)
        {
            for (var weekStart = GetWeekStart(forecastStart); weekStart <= forecastEnd; weekStart = weekStart.AddDays(7))
            {
                decimal actual = 0;
                for (var d = 0; d < 7; d++)
                {
                    var day = weekStart.AddDays(d);
                    if (productDemandByDate.TryGetValue((day, product.Id), out var qty))
                    {
                        actual += qty;
                    }
                }

                if (actual <= 0 && random.NextDouble() < 0.65)
                {
                    continue;
                }

                var predictionNoise = (decimal)(random.NextDouble() * 0.36 - 0.18);
                var predicted = Math.Max(0, Math.Round(actual * (1m + predictionNoise), 2));
                decimal? mape = actual > 0
                    ? Math.Round(Math.Abs((actual - predicted) / actual) * 100m, 2)
                    : null;

                forecastRows.Add(new InventoryForecast
                {
                    ProductId = product.Id,
                    ForecastDate = weekStart.AddDays(7),
                    PredictedDemand = predicted,
                    ConfidenceLower = Math.Max(0, Math.Round(predicted * 0.82m, 2)),
                    ConfidenceUpper = Math.Round(predicted * 1.18m, 2),
                    ActualDemand = Math.Round(actual, 2),
                    MAPE = mape,
                    ModelVersion = "SeedBaseline_v2",
                    GeneratedAt = weekStart.AddDays(1)
                });
            }
        }

        if (forecastRows.Any())
        {
            await context.InventoryForecasts.AddRangeAsync(forecastRows);
            await context.SaveChangesAsync();
        }

        context.SiteSettings.Add(new SiteSettings
        {
            Group = "Seed",
            Key = seedMarkerKey,
            Value = DateTime.UtcNow.ToString("O")
        });

        await context.SaveChangesAsync();
    }

    private static async Task<List<AppUser>> EnsureSeedCustomersAsync(
        AppDbContext context,
        IPasswordHashService passwordHashService,
        int requiredCount)
    {
        const string seedPrefix = "seed.customer";
        var existing = await context.Users
            .Where(u => u.Role == "Customer" && u.Email.StartsWith(seedPrefix))
            .OrderBy(u => u.Email)
            .ToListAsync();

        if (existing.Count >= requiredCount)
        {
            return existing.Take(requiredCount).ToList();
        }

        var toCreate = new List<AppUser>();
        for (var i = existing.Count + 1; i <= requiredCount; i++)
        {
            toCreate.Add(new AppUser
            {
                Email = $"{seedPrefix}{i:00}@chinhnha.local",
                FullName = $"Khách hàng seed {i:00}",
                Phone = $"09{i:00}{(100000 + i):000000}",
                Role = "Customer",
                IsActive = true,
                CreatedAt = DateTime.UtcNow.AddDays(-800 + (i * 7)),
                PasswordHash = passwordHashService.HashPassword("Customer@123")
            });
        }

        if (toCreate.Any())
        {
            await context.Users.AddRangeAsync(toCreate);
            await context.SaveChangesAsync();
            existing.AddRange(toCreate);
        }

        return existing.Take(requiredCount).ToList();
    }

    private static int CalculateDailyOrderCount(DateTime day, Random random)
    {
        var monthFactor = GetSeasonalDemandMultiplier(day.Month);
        var weekFactor = GetWeekOfMonthFactor(day);
        var dayOfWeekFactor = day.DayOfWeek switch
        {
            DayOfWeek.Monday => 1.15m,
            DayOfWeek.Tuesday => 1.05m,
            DayOfWeek.Wednesday => 1.00m,
            DayOfWeek.Thursday => 1.00m,
            DayOfWeek.Friday => 1.10m,
            DayOfWeek.Saturday => 0.92m,
            _ => 0.80m
        };

        var noise = (decimal)(0.70 + random.NextDouble() * 0.75);
        var demand = 2.8m * monthFactor * weekFactor * dayOfWeekFactor * noise;
        return Math.Max(0, (int)Math.Round(demand));
    }

    private static decimal GetSeasonalDemandMultiplier(int month)
    {
        return month switch
        {
            11 or 12 or 1 => 1.50m, // Vụ Đông Xuân
            2 or 3 => 1.00m,
            4 or 5 => 1.30m, // Vụ Hè Thu chuẩn bị
            6 or 7 or 8 => 0.70m, // Ít canh tác mùa lũ
            _ => 1.00m // 9-10 Thu Đông trung bình
        };
    }

    private static decimal GetWeekOfMonthFactor(DateTime day)
    {
        var week = ((day.Day - 1) / 7) + 1;
        return week switch
        {
            1 => 1.24m,
            2 => 1.12m,
            3 => 1.00m,
            4 => 0.88m,
            _ => 0.78m
        };
    }

    private static OrderStatus PickOrderStatus(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.48) return OrderStatus.Delivered;
        if (roll < 0.60) return OrderStatus.Shipping;
        if (roll < 0.70) return OrderStatus.Processing;
        if (roll < 0.76) return OrderStatus.Confirmed;
        if (roll < 0.90) return OrderStatus.Pending;
        if (roll < 0.97) return OrderStatus.Cancelled;
        return OrderStatus.Returned;
    }

    private static PaymentMethod PickPaymentMethod(Random random)
    {
        var roll = random.NextDouble();
        if (roll < 0.45) return PaymentMethod.COD;
        if (roll < 0.90) return PaymentMethod.VNPay;
        return PaymentMethod.BankTransfer;
    }

    private static PaymentStatus ResolvePaymentStatus(OrderStatus status, PaymentMethod method, Random random)
    {
        if (status == OrderStatus.Cancelled)
        {
            return random.NextDouble() < 0.7 ? PaymentStatus.Failed : PaymentStatus.Pending;
        }

        if (status == OrderStatus.Returned)
        {
            return PaymentStatus.Refunded;
        }

        if (status == OrderStatus.Delivered)
        {
            return random.NextDouble() < 0.80 ? PaymentStatus.Paid : PaymentStatus.Pending;
        }

        if (method == PaymentMethod.VNPay && random.NextDouble() < 0.15)
        {
            return PaymentStatus.Failed;
        }

        return random.NextDouble() < 0.25 ? PaymentStatus.Paid : PaymentStatus.Pending;
    }

    private static decimal PickTargetBasketValue(Random random)
    {
        var roll = random.NextDouble();
        var baseValue = roll switch
        {
            < 0.18 => 100000m,
            < 0.42 => 200000m,
            < 0.72 => 500000m,
            < 0.90 => 1000000m,
            _ => 2000000m
        };

        var variation = (decimal)(0.82 + random.NextDouble() * 0.42);
        return Math.Max(80000m, Math.Round(baseValue * variation, 0));
    }

    private sealed class ItemSeed
    {
        public Product Product { get; init; } = null!;
        public ProductVariant? Variant { get; init; }
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
    }

    private static List<ItemSeed> BuildOrderItemsForBasket(List<Product> products, decimal targetBasket, Random random)
    {
        var lines = random.Next(1, 5);
        var remaining = targetBasket;
        var items = new List<ItemSeed>(lines);

        for (var i = 0; i < lines; i++)
        {
            var product = products[random.Next(products.Count)];
            var activeVariants = product.Variants.Where(v => v.IsActive).ToList();
            var variant = activeVariants.Any()
                ? activeVariants[random.Next(activeVariants.Count)]
                : null;

            var unitPrice = variant?.SalePrice
                ?? variant?.Price
                ?? product.SalePrice
                ?? product.BasePrice;

            var maxQty = Math.Max(1, (int)Math.Min(8, Math.Ceiling((double)(remaining / Math.Max(unitPrice, 1m)))));
            var quantity = i == lines - 1
                ? Math.Max(1, maxQty)
                : Math.Max(1, random.Next(1, maxQty + 1));

            items.Add(new ItemSeed
            {
                Product = product,
                Variant = variant,
                UnitPrice = unitPrice,
                Quantity = quantity
            });

            remaining -= unitPrice * quantity;
        }

        return items;
    }

    private static string PickGuestName(Random random)
    {
        var names = new[]
        {
            "Nguyễn Văn Tài", "Trần Thị Hồng", "Lê Văn Phúc", "Phạm Văn Lộc", "Võ Thị Cẩm",
            "Huỳnh Văn Khoa", "Đặng Thị Mai", "Bùi Văn Nông", "Phan Thị Lan", "Trương Văn Hiếu"
        };
        return names[random.Next(names.Length)];
    }

    private static string PickGuestPhone(Random random)
    {
        return $"09{random.Next(10000000, 99999999)}";
    }

    private static string PickDistrict(Random random)
    {
        var districts = new[]
        {
            "Ninh Kiều", "Bình Thủy", "Cái Răng", "Ô Môn", "Thốt Nốt", "Phong Điền"
        };
        return districts[random.Next(districts.Length)];
    }

    private static string PickAddress(Random random)
    {
        var prefixes = new[]
        {
            "Ấp", "Tổ", "Số", "Khu vực"
        };
        return $"{prefixes[random.Next(prefixes.Length)]} {random.Next(1, 12)}, Đường {random.Next(1, 40)}";
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.Date.AddDays(-diff);
    }
}
