using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Application.Interfaces;

public interface IVNPayService
{
    string CreatePaymentUrl(PaymentInformationDto model, string ipAddress, string? baseUrl = null);
    PaymentResponseDto PaymentExecute(IDictionary<string, string> queryParameters);
}
