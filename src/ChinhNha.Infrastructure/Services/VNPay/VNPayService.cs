using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace ChinhNha.Infrastructure.Services.VNPay;

public class VNPayService : IVNPayService
{
    private readonly IConfiguration _configuration;

    public VNPayService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreatePaymentUrl(PaymentInformationDto model, string ipAddress, string? baseUrl = null)
    {
        var vnpay = new VnPayLibrary();

        vnpay.AddRequestData("vnp_Version", _configuration["VNPay:Version"] ?? "2.1.0");
        vnpay.AddRequestData("vnp_Command", _configuration["VNPay:Command"] ?? "pay");
        vnpay.AddRequestData("vnp_TmnCode", _configuration["VNPay:TmnCode"] ?? "");
        
        // Cần nhân 100 theo chuẩn VNPay
        var amountLength = (long)(model.Amount * 100);
        vnpay.AddRequestData("vnp_Amount", amountLength.ToString());
        
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", _configuration["VNPay:CurrCode"] ?? "VND");
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);
        vnpay.AddRequestData("vnp_Locale", _configuration["VNPay:Locale"] ?? "vn");
        vnpay.AddRequestData("vnp_OrderInfo", "Thanh toan don hang: " + model.OrderId);
        vnpay.AddRequestData("vnp_OrderType", model.OrderType);
        var returnUrl = !string.IsNullOrWhiteSpace(baseUrl)
            ? $"{baseUrl.TrimEnd('/')}/Checkout/PaymentCallback"
            : (_configuration["VNPay:PaymentBackReturnUrl"] ?? "");

        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", $"{model.OrderId}_{DateTime.Now.Ticks}"); // Unique transaction ref

        var gatewayBaseUrl = _configuration["VNPay:BaseUrl"] ?? "";
        var hashSecret = _configuration["VNPay:HashSecret"] ?? "";
        
        var paymentUrl = vnpay.CreateRequestUrl(gatewayBaseUrl, hashSecret);

        return paymentUrl;
    }

    public PaymentResponseDto PaymentExecute(IDictionary<string, string> queryParameters)
    {
        var vnpay = new VnPayLibrary();
        foreach (var (key, value) in queryParameters)
        {
            if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
            {
                vnpay.AddResponseData(key, value);
            }
        }

        var txnRefStr = vnpay.GetResponseData("vnp_TxnRef");
        var vnp_orderId = !string.IsNullOrEmpty(txnRefStr) ? txnRefStr.Split('_').First() : "0";
        var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
        
        queryParameters.TryGetValue("vnp_SecureHash", out var vnp_SecureHash);
        
        var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

        var hashSecret = _configuration["VNPay:HashSecret"] ?? "";
        bool checkSignature = vnpay.ValidateSignature(vnp_SecureHash ?? "", hashSecret);
        
        if (!checkSignature)
        {
            return new PaymentResponseDto
            {
                Success = false,
                OrderDescription = "Chữ ký không hợp lệ."
            };
        }

        return new PaymentResponseDto
        {
            Success = vnp_ResponseCode == "00",
            PaymentMethod = "VnPay",
            OrderDescription = vnp_OrderInfo,
            OrderId = vnp_orderId,
            TransactionId = vnp_TransactionId,
            PaymentId = vnp_TransactionId,
            VnPayResponseCode = vnp_ResponseCode
        };
    }
}
