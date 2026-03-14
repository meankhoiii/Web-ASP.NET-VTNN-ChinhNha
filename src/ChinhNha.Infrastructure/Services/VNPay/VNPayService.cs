using ChinhNha.Application.DTOs.Orders;
using ChinhNha.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

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

        vnpay.AddRequestData("vnp_Version", GetConfig("VNPay:Version", "VNPAY_VERSION") ?? "2.1.0");
        vnpay.AddRequestData("vnp_Command", GetConfig("VNPay:Command", "VNPAY_COMMAND") ?? "pay");
        vnpay.AddRequestData("vnp_TmnCode", GetConfig("VNPay:TmnCode", "VNPAY_TMN_CODE") ?? "");
        
        // Cần nhân 100 theo chuẩn VNPay
        var amountLength = (long)(model.Amount * 100);
        vnpay.AddRequestData("vnp_Amount", amountLength.ToString());
        
        vnpay.AddRequestData("vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_ExpireDate", DateTime.Now.AddMinutes(15).ToString("yyyyMMddHHmmss"));
        vnpay.AddRequestData("vnp_CurrCode", GetConfig("VNPay:CurrCode", "VNPAY_CURR_CODE") ?? "VND");
        vnpay.AddRequestData("vnp_IpAddr", ipAddress);
        vnpay.AddRequestData("vnp_Locale", GetConfig("VNPay:Locale", "VNPAY_LOCALE") ?? "vn");
        vnpay.AddRequestData("vnp_OrderInfo", BuildSafeOrderInfo(model.OrderId));
        vnpay.AddRequestData("vnp_OrderType", model.OrderType);
        var configuredReturnUrl = GetConfig("VNPay:PaymentBackReturnUrl", "VNPAY_RETURN_URL");
        var returnUrl = !string.IsNullOrWhiteSpace(configuredReturnUrl)
            ? configuredReturnUrl
            : (!string.IsNullOrWhiteSpace(baseUrl)
                ? $"{baseUrl.TrimEnd('/')}/Checkout/PaymentCallback"
                : string.Empty);

        vnpay.AddRequestData("vnp_ReturnUrl", returnUrl);
        vnpay.AddRequestData("vnp_TxnRef", $"{model.OrderId}_{DateTime.Now.Ticks}"); // Unique transaction ref

        var gatewayBaseUrl = GetConfig("VNPay:BaseUrl", "VNPAY_URL") ?? "";
        var hashSecret = GetConfig("VNPay:HashSecret", "VNPAY_HASH_SECRET") ?? "";
        
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
        var vnp_orderId = ExtractOrderIdFromTxnRef(txnRefStr);
        var vnp_TransactionId = vnpay.GetResponseData("vnp_TransactionNo");
        
        queryParameters.TryGetValue("vnp_SecureHash", out var vnp_SecureHash);
        
        var vnp_ResponseCode = vnpay.GetResponseData("vnp_ResponseCode");
        var vnp_OrderInfo = vnpay.GetResponseData("vnp_OrderInfo");

        var hashSecret = GetConfig("VNPay:HashSecret", "VNPAY_HASH_SECRET") ?? "";
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

    private string? GetConfig(string primaryKey, string fallbackKey)
    {
        var value = _configuration[primaryKey];
        return string.IsNullOrWhiteSpace(value) ? _configuration[fallbackKey] : value;
    }

    private static string BuildSafeOrderInfo(int orderId)
    {
        var raw = $"Thanh toan don hang CHINHNHA {orderId}";
        // VNPay recommends no special characters in OrderInfo.
        return Regex.Replace(raw, @"[^a-zA-Z0-9 ]", string.Empty);
    }

    private static string ExtractOrderIdFromTxnRef(string? txnRef)
    {
        if (string.IsNullOrWhiteSpace(txnRef))
        {
            return "0";
        }

        var leadingDigits = Regex.Match(txnRef, @"^\d+");
        if (leadingDigits.Success)
        {
            return leadingDigits.Value;
        }

        var firstNumericPart = txnRef
            .Split('_', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(static part => part.All(char.IsDigit));

        return string.IsNullOrWhiteSpace(firstNumericPart) ? "0" : firstNumericPart;
    }
}
