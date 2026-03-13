using System.Text.Json;
using ChinhNha.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ChinhNha.Web.Controllers;

[ApiController]
[Route("api/address")]
public class AddressController : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IWebHostEnvironment _env;
    private static IReadOnlyList<AddressProvinceDto>? _cached;
    private static readonly object LockObj = new();

    public AddressController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet("provinces")]
    public async Task<IActionResult> GetProvinces()
    {
        var data = await GetDataAsync();
        var provinces = data
            .Select(p => new { p.Name })
            .OrderBy(p => p.Name)
            .ToList();
        return Ok(provinces);
    }

    [HttpGet("districts")]
    public async Task<IActionResult> GetDistricts([FromQuery] string provinceName)
    {
        if (string.IsNullOrWhiteSpace(provinceName))
        {
            return Ok(Array.Empty<object>());
        }

        var data = await GetDataAsync();
        var province = data.FirstOrDefault(p =>
            string.Equals(p.Name, provinceName, StringComparison.OrdinalIgnoreCase));

        var districts = (province?.Districts ?? new List<AddressDistrictDto>())
            .Select(d => new { d.Name })
            .OrderBy(d => d.Name)
            .ToList();

        return Ok(districts);
    }

    [HttpGet("wards")]
    public async Task<IActionResult> GetWards([FromQuery] string provinceName, [FromQuery] string districtName)
    {
        if (string.IsNullOrWhiteSpace(provinceName) || string.IsNullOrWhiteSpace(districtName))
        {
            return Ok(Array.Empty<object>());
        }

        var data = await GetDataAsync();
        var province = data.FirstOrDefault(p =>
            string.Equals(p.Name, provinceName, StringComparison.OrdinalIgnoreCase));

        var district = province?.Districts.FirstOrDefault(d =>
            string.Equals(d.Name, districtName, StringComparison.OrdinalIgnoreCase));

        var wards = (district?.Wards ?? new List<AddressWardDto>())
            .Select(w => new { w.Name })
            .OrderBy(w => w.Name)
            .ToList();

        return Ok(wards);
    }

    private async Task<IReadOnlyList<AddressProvinceDto>> GetDataAsync()
    {
        if (_cached != null)
        {
            return _cached;
        }

        var filePath = Path.Combine(_env.WebRootPath, "data", "vn-addresses.json");
        if (!System.IO.File.Exists(filePath))
        {
            return Array.Empty<AddressProvinceDto>();
        }

        string json = await System.IO.File.ReadAllTextAsync(filePath);
        var parsed = ParseAddressData(json);

        lock (LockObj)
        {
            _cached ??= parsed;
        }

        return _cached;
    }

    private static List<AddressProvinceDto> ParseAddressData(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<AddressProvinceDto>();
        }

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            var data = JsonSerializer.Deserialize<List<AddressProvinceDto>>(root.GetRawText(), JsonOptions)
                ?? new List<AddressProvinceDto>();
            return NormalizeAddressData(data);
        }

        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty("value", out var valueElement) &&
            valueElement.ValueKind == JsonValueKind.Array)
        {
            var data = JsonSerializer.Deserialize<List<AddressProvinceDto>>(valueElement.GetRawText(), JsonOptions)
                ?? new List<AddressProvinceDto>();
            return NormalizeAddressData(data);
        }

        return new List<AddressProvinceDto>();
    }

    private static List<AddressProvinceDto> NormalizeAddressData(List<AddressProvinceDto> data)
    {
        foreach (var province in data)
        {
            province.Name = FixMojibakeVietnamese(province.Name);

            if (province.Districts == null)
            {
                continue;
            }

            foreach (var district in province.Districts)
            {
                district.Name = FixMojibakeVietnamese(district.Name);

                if (district.Wards == null)
                {
                    continue;
                }

                foreach (var ward in district.Wards)
                {
                    ward.Name = FixMojibakeVietnamese(ward.Name);
                }
            }
        }

        return data;
    }

    private static string FixMojibakeVietnamese(string? input)
    {
        if (string.IsNullOrWhiteSpace(input) || !LooksLikeMojibake(input))
        {
            return input ?? string.Empty;
        }

        try
        {
            var latin1Bytes = Encoding.Latin1.GetBytes(input);
            var decoded = Encoding.UTF8.GetString(latin1Bytes);
            return string.IsNullOrWhiteSpace(decoded) ? input : decoded;
        }
        catch
        {
            return input;
        }
    }

    private static bool LooksLikeMojibake(string input)
    {
        return input.Contains("Ã")
            || input.Contains("Ä")
            || input.Contains("Å")
            || input.Contains("Æ")
            || input.Contains("áº")
            || input.Contains("á»");
    }
}
