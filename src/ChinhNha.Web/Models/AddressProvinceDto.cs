namespace ChinhNha.Web.Models;

public class AddressProvinceDto
{
    public string Name { get; set; } = string.Empty;
    public List<AddressDistrictDto> Districts { get; set; } = new();
}

public class AddressDistrictDto
{
    public string Name { get; set; } = string.Empty;
    public List<AddressWardDto> Wards { get; set; } = new();
}

public class AddressWardDto
{
    public string Name { get; set; } = string.Empty;
}
