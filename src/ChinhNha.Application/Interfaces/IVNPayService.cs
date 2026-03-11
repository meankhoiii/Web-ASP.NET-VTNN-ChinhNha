using ChinhNha.Application.DTOs.Orders;

namespace ChinhNha.Application.Interfaces;

public interface IVNPayService
{
    string CreatePaymentUrl(PaymentInformationDto model, string ipAddress);
    PaymentResponseDto PaymentExecute(IDictionary<string, string> queryParameters);
}
