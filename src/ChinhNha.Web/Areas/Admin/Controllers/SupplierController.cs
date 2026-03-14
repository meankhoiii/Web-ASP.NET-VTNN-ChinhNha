using ChinhNha.Domain.Entities;
using ChinhNha.Domain.Interfaces;
using ChinhNha.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChinhNha.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Policy = "AdminOnly")]
public class SupplierController : Controller
{
    private readonly IRepository<Supplier> _supplierRepository;

    public SupplierController(IRepository<Supplier> supplierRepository)
    {
        _supplierRepository = supplierRepository;
    }

    public async Task<IActionResult> Index()
    {
        var suppliers = await _supplierRepository.ListAllAsync();
        var ordered = suppliers.OrderBy(s => s.Name).ToList();
        return View(ordered);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new SupplierFormViewModel { IsActive = true, LeadTimeDays = 3 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var supplier = new Supplier
        {
            Name = model.Name.Trim(),
            ContactPerson = model.ContactPerson,
            Phone = model.Phone,
            Email = model.Email,
            Address = model.Address,
            Website = model.Website,
            LeadTimeDays = model.LeadTimeDays,
            IsActive = model.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _supplierRepository.AddAsync(supplier);
        TempData["SuccessMessage"] = "Đã tạo nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            return NotFound();
        }

        var model = new SupplierFormViewModel
        {
            Id = supplier.Id,
            Name = supplier.Name,
            ContactPerson = supplier.ContactPerson,
            Phone = supplier.Phone,
            Email = supplier.Email,
            Address = supplier.Address,
            Website = supplier.Website,
            LeadTimeDays = supplier.LeadTimeDays,
            IsActive = supplier.IsActive
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(SupplierFormViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var supplier = await _supplierRepository.GetByIdAsync(model.Id);
        if (supplier == null)
        {
            return NotFound();
        }

        supplier.Name = model.Name.Trim();
        supplier.ContactPerson = model.ContactPerson;
        supplier.Phone = model.Phone;
        supplier.Email = model.Email;
        supplier.Address = model.Address;
        supplier.Website = model.Website;
        supplier.LeadTimeDays = model.LeadTimeDays;
        supplier.IsActive = model.IsActive;

        await _supplierRepository.UpdateAsync(supplier);
        TempData["SuccessMessage"] = "Đã cập nhật nhà cung cấp.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _supplierRepository.GetByIdAsync(id);
        if (supplier == null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy nhà cung cấp cần xóa.";
            return RedirectToAction(nameof(Index));
        }

        try
        {
            await _supplierRepository.DeleteAsync(supplier);
            TempData["SuccessMessage"] = "Đã xóa nhà cung cấp.";
        }
        catch
        {
            TempData["ErrorMessage"] = "Không thể xóa nhà cung cấp do đang có dữ liệu liên quan.";
        }

        return RedirectToAction(nameof(Index));
    }
}
