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
            .ForMember(dest => dest.UserFullName, opt => opt.MapFrom(src => src.User != null ? src.User.FullName : string.Empty))
            .ReverseMap();
        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.ProductVariantId))
            .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.VariantName)) // already denormalized
            .ReverseMap();

        // Cart
        CreateMap<Cart, CartDto>().ReverseMap();
        CreateMap<CartItem, CartItemDto>()
            .ForMember(dest => dest.VariantId, opt => opt.MapFrom(src => src.ProductVariantId))
            .ForMember(dest => dest.VariantName, opt => opt.MapFrom(src => src.ProductVariant != null ? src.ProductVariant.VariantName : null))
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
        CreateMap<InventoryForecast, InventoryForecastDto>()
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product.Name))
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
    }
}
