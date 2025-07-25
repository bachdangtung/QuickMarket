using BussinessLogic.DTOs.Wallet;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace Services.Implementations
{
    public class VNPayService
    {
        private readonly IConfiguration _configuration;
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _paymentUrl;
        private readonly string _returnUrl;
        private readonly string _version;

        public VNPayService(IConfiguration configuration)
        {
            _configuration = configuration;
            _tmnCode = _configuration["VNPay:TmnCode"] ?? "XZK7XOT0";
            _hashSecret = _configuration["VNPay:HashSecret"] ?? "0X0MWI021ZCP1Y2U5SINHM6230ODL4SR";
            _paymentUrl = _configuration["VNPay:PaymentUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _returnUrl = _configuration["VNPay:ReturnUrl"] ?? "https://d94af6116a67.ngrok-free.app/Wallet/VNPayReturn";
            _version = _configuration["VNPay:Version"] ?? "2.1.0";
        }

        public string CreatePaymentUrl(TopupRequestDto request, string orderCode, string ipAddress)
        {
            Console.WriteLine($"Debug VNPayService: Creating payment URL for amount: {request.Amount}, orderCode: {orderCode}");
            Console.WriteLine($"Debug VNPayService: Config values - TmnCode: {_tmnCode}, ReturnUrl: {_returnUrl}");
            
            var pay = new VNPayRequest
            {
                vnp_Version = _version,
                vnp_Command = "pay",
                vnp_TmnCode = _tmnCode,
                vnp_Amount = ((long)(request.Amount * 100)).ToString(), // Nhân với 100 theo yêu cầu của VNPAY và chuyển thành long
                vnp_CreateDate = DateTime.Now.ToString("yyyyMMddHHmmss"),
                vnp_CurrCode = "VND",
                vnp_IpAddr = ipAddress,
                vnp_Locale = "vn",
                vnp_OrderInfo = $"Nạp tiền vào ví QuickMarket - {request.Amount} VND",
                vnp_OrderType = "250000",
                vnp_ReturnUrl = request.ReturnUrl ?? _returnUrl,
                vnp_TxnRef = orderCode
            };

            var requestData = new SortedList<string, string>(StringComparer.InvariantCultureIgnoreCase)
            {
                { "vnp_Version", pay.vnp_Version },
                { "vnp_Command", pay.vnp_Command },
                { "vnp_TmnCode", pay.vnp_TmnCode },
                { "vnp_Amount", pay.vnp_Amount },
                { "vnp_CreateDate", pay.vnp_CreateDate },
                { "vnp_CurrCode", pay.vnp_CurrCode },
                { "vnp_IpAddr", pay.vnp_IpAddr },
                { "vnp_Locale", pay.vnp_Locale },
                { "vnp_OrderInfo", pay.vnp_OrderInfo },
                { "vnp_OrderType", pay.vnp_OrderType },
                { "vnp_ReturnUrl", pay.vnp_ReturnUrl },
                { "vnp_TxnRef", pay.vnp_TxnRef }
            };

            if (!string.IsNullOrEmpty(pay.vnp_BankCode))
            {
                requestData.Add("vnp_BankCode", pay.vnp_BankCode);
            }

            // Tạo chuỗi ký (checksum data)
            var query = string.Join("&", requestData.Select(kv => $"{WebUtility.UrlEncode(kv.Key)}={WebUtility.UrlEncode(kv.Value)}"));
            
            Console.WriteLine($"Debug VNPayService: Query for signature: {query}");
            
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_hashSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(query));
            var secureHash = BitConverter.ToString(hash).Replace("-", "").ToLower();
            
            Console.WriteLine($"Debug VNPayService: Generated secure hash: {secureHash}");

            requestData.Add("vnp_SecureHash", secureHash);

            // Tạo URL thanh toán
            var paymentUrl = _paymentUrl + "?" + string.Join("&", requestData.Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));
            
            Console.WriteLine($"Debug VNPayService: Generated payment URL: {paymentUrl}");

            return paymentUrl;
        }

        public bool ValidateCallback(VNPayResponse response)
        {
            if (response == null)
                return false;

            // Tách riêng secure hash từ response
            var vnpSecureHash = response.vnp_SecureHash ?? "";
            
            // Loại bỏ tham số secure hash để tính toán lại
            var responseData = typeof(VNPayResponse).GetProperties()
                .Where(p => p.Name != "vnp_SecureHash" && p.Name != "IsSuccess" && p.Name != "Amount")
                .Where(p => p.GetValue(response) != null)
                .ToDictionary(
                    p => p.Name,
                    p => p.GetValue(response)?.ToString() ?? ""
                );

            // Sắp xếp theo key
            var sortedResponseData = new SortedDictionary<string, string>(responseData, StringComparer.InvariantCultureIgnoreCase);
            
            // Tạo chuỗi query để tạo checksum
            var checkSumData = string.Join("&", sortedResponseData
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .Select(kv => $"{kv.Key}={WebUtility.UrlEncode(kv.Value)}"));

            // Tính toán HMAC-SHA512
            var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(_hashSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(checkSumData));
            var calculatedHash = BitConverter.ToString(hash).Replace("-", "").ToLower();

            // So sánh hash tính toán với hash nhận được
            return calculatedHash == vnpSecureHash.ToLower();
        }

        // Kiểm tra thanh toán thành công
        public bool IsSuccessTransaction(VNPayResponse response)
        {
            return response != null && 
                   response.vnp_ResponseCode == "00" && 
                   response.vnp_TransactionStatus == "00" &&
                   ValidateCallback(response);
        }
    }
}
