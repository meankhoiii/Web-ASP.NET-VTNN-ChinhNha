using AutoMapper;
using ChinhNha.Domain.Entities;
using ChinhNha.Application.DTOs.Products;
using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.DTOs.Blogs;

namespace ChinhNha.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Products
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier != null ? src.Supplier.Name : null))
            .ReverseMap();

        CreateMap<ProductVariant, ProductVariantDto>().ReverseMap();
        CreateMap<ProductImage, ProductImageDto>().ReverseMap();

        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : src.ReceiverName))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : (src.ReceiverEmail ?? string.Empty)))
            .ForMember(dest => dest.ReceiverEmail, opt => opt.MapFrom(src => src.ReceiverEmail))
            .ForMember(dest => dest.ShippingName, opt => opt.MapFrom(src => src.ReceiverName))
            .ForMember(dest => dest.ShippingPhone, opt => opt.MapFrom(src => src.ReceiverPhone))
            .ForMember(dest => dest.ShippingNote, opt => opt.MapFrom(src => src.Note))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.OrderItems))
            .ForMember(dest => dest.PaymentMethod, opt => opt.Ignore())
            .ForMember(dest => dest.IsPaid, opt => opt.Ignore())
            .ForMember(dest => dest.TrackingNumber, opt => opt.Ignore())
            .ReverseMap();
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.ProductVariantId))
            .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.VariantName)) // already denormalized
            .ReverseMap();

        // Cart
        CreateMap<Cart, CartDto>()
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.CartItems))
            .ForMember(dest => dest.LastUpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt ?? src.CreatedAt));
        CreateMap<CartDto, Cart>()
            .ForMember(dest => dest.CartItems, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.LastUpdatedAt));
        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.ProductVariantId))
            .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.VariantName : null))
            .ReverseMap();

        // Customer
        CreateMap<AppUser, ChinhNha.Application.DTOs.Customers.CustomerProfileDto>()
            .ReverseMap();

        // Inventory
        CreateMap<InventoryTransaction, InventoryTransactionDto>()
            .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.ProductVariantId))
            .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.VariantName : null))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.TransactionType))
            .ForMember(dest => dest.OrderId, opt => opt.MapFrom(src => src.ReferenceId))
            .ForMember(dest => dest.TransactionDate, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.CreatedByUserId, opt => opt.MapFrom(src => src.CreatedById))
            .ReverseMap();
        CreateMap<Supplier, SupplierDto>().ReverseMap();
        CreateMap<PurchaseOrder, PurchaseOrderDto>()
            .ForMember(dest => dest.SupplierName, opt => opt.MapFrom(src => src.Supplier.Name))
            .ReverseMap();

        // Blogs
        CreateMap<BlogPost, BlogPostDto>()
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name))
            .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.FullName : string.Empty))
            .ReverseMap();
        CreateMap<BlogCategory, BlogCategoryDto>().ReverseMap();

        // Product Reviews
        CreateMap<ProductReview, ProductReviewDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Anonymous"))
            .ForMember(dest => dest.UserAvatarUrl, opt => opt.MapFrom(src => src.User != null ? src.User.AvatarUrl : null))
            .ReverseMap();

        // Audit Logs
        CreateMap<AuditLog, ChinhNha.Application.DTOs.Admin.AuditLogDto>()
            .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : "Unknown User"))
            .ForMember(dest => dest.UserEmail, opt => opt.MapFrom(src => src.User != null ? src.User.Email : string.Empty))
            .ReverseMap();

    }
}
