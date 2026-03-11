using AutoMapper;
using ChinhNha.Application.DTOs.Inventory;
using ChinhNha.Application.Interfaces;
using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Enums;
using ChinhNha.Domain.Interfaces;

namespace ChinhNha.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;

    public InventoryService(IInventoryRepository inventoryRepository, IProductRepository productRepository, IMapper mapper)
    {
        _inventoryRepository = inventoryRepository;
        _productRepository = productRepository;
        _mapper = mapper;
    }

    public async Task<int> GetCurrentStockAsync(int productId, int? variantId = null)
    {
        return await _inventoryRepository.GetCurrentStockQuantityAsync(productId, variantId);
    }

    public Task<IEnumerable<InventoryForecastDto>> GetProductForecastsAsync(int productId)
    {
        // ML.NET forecasting is handled in Phase 4
        return Task.FromResult<IEnumerable<InventoryForecastDto>>(new List<InventoryForecastDto>());
    }

    public async Task<IEnumerable<InventoryTransactionDto>> GetProductTransactionsAsync(int productId)
    {
        var transactions = await _inventoryRepository.GetTransactionsByProductIdAsync(productId);
        return _mapper.Map<IEnumerable<InventoryTransactionDto>>(transactions);
    }

    public async Task<InventoryTransactionDto> RecordTransactionAsync(
        int productId,
        TransactionType type,
        int quantity,
        string? note = null,
        int? variantId = null,
        decimal? unitCost = null,
        int? orderId = null,
        int? purchaseOrderId = null,
        string? createdByUserId = null)
    {
        var product = await _productRepository.GetProductWithDetailsByIdAsync(productId);
        if (product == null) throw new ArgumentException("Product not found");

        int stockBefore = await _inventoryRepository.GetCurrentStockQuantityAsync(productId, variantId);

        // Import and Return increase stock; Export, Loss, Adjustment decrease
        int stockAfter = (type == TransactionType.Import || type == TransactionType.Return)
            ? stockBefore + quantity
            : stockBefore - quantity;

        var referenceId = orderId ?? purchaseOrderId;
        var referenceType = orderId.HasValue ? "Order" : (purchaseOrderId.HasValue ? "PurchaseOrder" : "Manual");

        var transaction = new InventoryTransaction
        {
            ProductId = productId,
            ProductVariantId = variantId,
            TransactionType = type,
            Quantity = quantity,
            StockBefore = stockBefore,
            StockAfter = stockAfter,
            UnitCost = unitCost,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Note = note,
            CreatedById = createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        // Sync stock quantity on Product/Variant entity
        if (variantId.HasValue)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId);
            if (variant != null) variant.StockQuantity = stockAfter;
        }
        else
        {
            product.StockQuantity = stockAfter;
        }

        await _inventoryRepository.AddAsync(transaction);
        await _productRepository.UpdateAsync(product);

        return _mapper.Map<InventoryTransactionDto>(transaction);
    }
}
