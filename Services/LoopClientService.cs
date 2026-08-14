using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using LoopFlow.Models;

namespace LoopFlow.Services
{
    public interface ILoopClientService
    {
        Task<LoopNormalizedResponse> PayToMpesaTillAsync(string merchantRcvTill, string accountNumber, decimal amount, string txnRef = null);
        Task<LoopNormalizedResponse> PayToMpesaPaybillAsync(string paybillNumber, string accountNumber, decimal amount, string txnRef = null);
        Task<LoopNormalizedResponse> SendMoneyLoopAsync(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null);
        Task<LoopNormalizedResponse> SendMoneyMpesaAsync(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null);
        Task<LoopNormalizedResponse> SendMoneyPesalinkAsync(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null);
    }

    public class LoopClientService : ILoopClientService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILoopAuthService _authService;
        private readonly ILoopSignatureService _signatureService;

        private readonly string _baseUrl;
        private readonly string _merchantTill;
        private readonly int _maxRetries = 3;

        public LoopClientService()
        {
            _db = new ApplicationDbContext();
            _authService = new LoopAuthService();
            _signatureService = new LoopSignatureService();

            _baseUrl = ConfigurationManager.AppSettings["LOOP_BASE_URL"] ?? "https://sandbox.loop.co.ke";
            _merchantTill = ConfigurationManager.AppSettings["LOOP_MERCHANT_TILL"] ?? "133238";
        }

        public LoopClientService(ApplicationDbContext db, ILoopAuthService authService, ILoopSignatureService signatureService)
        {
            _db = db;
            _authService = authService;
            _signatureService = signatureService;

            _baseUrl = ConfigurationManager.AppSettings["LOOP_BASE_URL"] ?? "https://sandbox.loop.co.ke";
            _merchantTill = ConfigurationManager.AppSettings["LOOP_MERCHANT_TILL"] ?? "133238";
        }

        // ==========================================
        // API #7: Pay to M-Pesa Till
        // Endpoint: /gateway/pay-to-mpesa-till/1.0/services/process-request
        // ==========================================
        public async Task<LoopNormalizedResponse> PayToMpesaTillAsync(string merchantRcvTill, string accountNumber, decimal amount, string txnRef = null)
        {
            string endpoint = _baseUrl + "/gateway/pay-to-mpesa-till/1.0/services/process-request";
            string serviceCode = "MRCHNT_PAYMENTS";
            string channel = "MPESA_TILL";
            txnRef = txnRef ?? ("TXN-TILL-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999));

            return await ExecutePaymentRequestAsync(
                apiOperation: "PayToMpesaTill",
                endpoint: endpoint,
                serviceCode: serviceCode,
                channel: channel,
                merchantRcvTill: merchantRcvTill,
                accountNumber: accountNumber,
                recipientMobileNo: null,
                amount: amount,
                purposeOfPayment: null,
                txnRef: txnRef
            );
        }

        // ==========================================
        // API #8: Pay to M-Pesa Paybill
        // Endpoint: /gateway/pay-to-paybill/1.0/services/process-request
        // ==========================================
        public async Task<LoopNormalizedResponse> PayToMpesaPaybillAsync(string paybillNumber, string accountNumber, decimal amount, string txnRef = null)
        {
            string endpoint = _baseUrl + "/gateway/pay-to-paybill/1.0/services/process-request";
            string serviceCode = "MRCHNT_PAYMENTS";
            string channel = "MPESA_PAYBILL";
            txnRef = txnRef ?? ("TXN-PB-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999));

            return await ExecutePaymentRequestAsync(
                apiOperation: "PayToMpesaPaybill",
                endpoint: endpoint,
                serviceCode: serviceCode,
                channel: channel,
                merchantRcvTill: paybillNumber,
                accountNumber: accountNumber,
                recipientMobileNo: null,
                amount: amount,
                purposeOfPayment: null,
                txnRef: txnRef
            );
        }

        // ==========================================
        // API #9: Send Money via LOOP
        // Endpoint: /gateway/send-money-loop/1.0/services/process-service-request2
        // ==========================================
        public async Task<LoopNormalizedResponse> SendMoneyLoopAsync(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null)
        {
            string endpoint = _baseUrl + "/gateway/send-money-loop/1.0/services/process-service-request2";
            string serviceCode = "MRCHNT_SENDMONEY";
            string channel = "LOOP";
            txnRef = txnRef ?? ("TXN-LOOP-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999));

            return await ExecutePaymentRequestAsync(
                apiOperation: "SendMoneyLoop",
                endpoint: endpoint,
                serviceCode: serviceCode,
                channel: channel,
                merchantRcvTill: null,
                accountNumber: null,
                recipientMobileNo: FormatPhoneNumber(recipientMobileNo, "LOOP"),
                amount: amount,
                purposeOfPayment: purposeOfPayment ?? "LOOP Trade Disbursement",
                txnRef: txnRef
            );
        }

        // ==========================================
        // API #10: Send Money via M-Pesa
        // Endpoint: /gateway/send-money-mpesa/1.0/services/process-request
        // ==========================================
        public async Task<LoopNormalizedResponse> SendMoneyMpesaAsync(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null)
        {
            string endpoint = _baseUrl + "/gateway/send-money-mpesa/1.0/services/process-request";
            string serviceCode = "MRCHNT_SENDMONEY";
            string channel = "MPESA";
            txnRef = txnRef ?? ("TXN-SMP-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999));

            return await ExecutePaymentRequestAsync(
                apiOperation: "SendMoneyMpesa",
                endpoint: endpoint,
                serviceCode: serviceCode,
                channel: channel,
                merchantRcvTill: null,
                accountNumber: null,
                recipientMobileNo: FormatPhoneNumber(recipientMobileNo, "MPESA"),
                amount: amount,
                purposeOfPayment: purposeOfPayment ?? "M-Pesa Supplier Payout",
                txnRef: txnRef
            );
        }

        // ==========================================
        // API #11: Send Money via PesaLink
        // Endpoint: /gateway/send-money-pesalink/1.0/services/process-request
        // ==========================================
        public async Task<LoopNormalizedResponse> SendMoneyPesalinkAsync(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null)
        {
            string endpoint = _baseUrl + "/gateway/send-money-pesalink/1.0/services/process-request";
            string serviceCode = "MRCHNT_SENDMONEY";
            string channel = "PESALINK";
            txnRef = txnRef ?? ("TXN-PESA-" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999));

            return await ExecutePaymentRequestAsync(
                apiOperation: "SendMoneyPesalink",
                endpoint: endpoint,
                serviceCode: serviceCode,
                channel: channel,
                merchantRcvTill: null,
                accountNumber: null,
                recipientMobileNo: FormatPhoneNumber(recipientMobileNo, "PESALINK"),
                amount: amount,
                purposeOfPayment: purposeOfPayment ?? "PesaLink Interbank Settlement",
                txnRef: txnRef
            );
        }

        // ==========================================
        // CENTRAL EXECUTOR WITH IDEMPOTENCY & RETRY ENGINE
        // ==========================================
        private async Task<LoopNormalizedResponse> ExecutePaymentRequestAsync(
            string apiOperation,
            string endpoint,
            string serviceCode,
            string channel,
            string merchantRcvTill,
            string accountNumber,
            string recipientMobileNo,
            decimal amount,
            string purposeOfPayment,
            string txnRef)
        {
            // 1. Idempotency Check: Verify if txnReference already exists
            var existingTxn = await _db.LoopTransactions.FirstOrDefaultAsync(t => t.TxnReference == txnRef);
            if (existingTxn != null && existingTxn.RequestStatus == "COMPLETED")
            {
                return new LoopNormalizedResponse
                {
                    Success = true,
                    Status = existingTxn.ServiceTransactionStatus ?? "COMPLETED",
                    Message = "Duplicate transaction reference resolved idempotently.",
                    TransactionId = existingTxn.InternalTransactionId,
                    TxnReference = existingTxn.TxnReference,
                    Amount = existingTxn.Amount,
                    Channel = existingTxn.Channel,
                    ProviderReference = existingTxn.TransactionRef,
                    TransferOrderId = existingTxn.TransferOrderId,
                    TransferRefNo = existingTxn.TransferRefNo,
                    Retriable = false,
                    RawStatusCode = existingTxn.LoopStatusCode
                };
            }

            // Create or update local transaction record
            var txn = existingTxn ?? new LoopTransaction
            {
                InternalTransactionId = "INT-LOOP-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper(),
                TxnReference = txnRef,
                ApiOperation = apiOperation,
                ServiceCode = serviceCode,
                Channel = channel,
                MerchantTill = _merchantTill,
                Recipient = recipientMobileNo ?? merchantRcvTill ?? accountNumber,
                Amount = amount,
                Purpose = purposeOfPayment,
                RequestStatus = "PENDING",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            if (existingTxn == null)
            {
                _db.LoopTransactions.Add(txn);
                await _db.SaveChangesAsync();
            }

            int attempt = 0;
            LoopNormalizedResponse finalResponse = null;

            while (attempt < _maxRetries)
            {
                attempt++;
                txn.RetryCount = attempt - 1;
                txn.UpdatedAt = DateTime.UtcNow;

                // 2. Fetch OAuth Token & Generate Fresh Signature Fields for every attempt
                string bearerToken = await _authService.GetAccessTokenAsync();
                var sigResult = _signatureService.GenerateSignature(_merchantTill);

                // 3. Build Documented Payload Envelope
                string payloadJson = BuildRequestPayload(
                    serviceCode: serviceCode,
                    channel: channel,
                    merchantTill: _merchantTill,
                    merchantRcvTill: merchantRcvTill,
                    accountNumber: accountNumber,
                    recipientMobileNo: recipientMobileNo,
                    amount: amount,
                    purposeOfPayment: purposeOfPayment,
                    txnRef: txnRef,
                    sigResult: sigResult
                );

                using (var httpClient = new HttpClient())
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

                    try
                    {
                        var content = new StringContent(payloadJson, Encoding.UTF8, "application/json");
                        var httpResponse = await httpClient.PostAsync(endpoint, content);
                        string rawResponse = await httpResponse.Content.ReadAsStringAsync();

                        txn.RawResponseJson = MaskSensitiveData(rawResponse);
                        txn.LoopStatusCode = ((int)httpResponse.StatusCode).ToString();

                        if (httpResponse.IsSuccessStatusCode)
                        {
                            // Success parsing
                            string rspCode = ExtractJsonValue(rawResponse, "rspCode") ?? ExtractJsonValue(rawResponse, "statusCode") ?? "OGW00000";
                            string serviceStatus = ExtractJsonValue(rawResponse, "serviceTransactionStatus") ?? "COMPLETED";
                            string transferStatus = ExtractJsonValue(rawResponse, "transferStatus") ?? "S";

                            bool isConfirmed = (rspCode == "OGW00000" || rspCode == "SAP00000" || transferStatus == "S" || serviceStatus == "COMPLETED");

                            txn.RequestStatus = isConfirmed ? "COMPLETED" : "FAILED";
                            txn.ServiceTransactionStatus = serviceStatus;
                            txn.LoopStatusCode = rspCode;
                            txn.LoopMessage = ExtractJsonValue(rawResponse, "responseDescription") ?? ExtractJsonValue(rawResponse, "message") ?? "Success";
                            txn.TransactionRef = ExtractJsonValue(rawResponse, "transactionRef");
                            txn.TransferOrderId = ExtractJsonValue(rawResponse, "transferOrderId");
                            txn.TransferRefNo = ExtractJsonValue(rawResponse, "transferRefNo");
                            txn.CompletedAt = DateTime.UtcNow;

                            await _db.SaveChangesAsync();

                            finalResponse = new LoopNormalizedResponse
                            {
                                Success = isConfirmed,
                                Status = serviceStatus,
                                Message = txn.LoopMessage,
                                TransactionId = txn.InternalTransactionId,
                                TxnReference = txn.TxnReference,
                                Amount = amount,
                                Channel = channel,
                                ProviderReference = txn.TransactionRef,
                                TransferOrderId = txn.TransferOrderId,
                                TransferRefNo = txn.TransferRefNo,
                                Retriable = false,
                                RawStatusCode = rspCode
                            };
                            return finalResponse;
                        }
                        else
                        {
                            int statusInt = (int)httpResponse.StatusCode;
                            bool isRetriable = (statusInt == 500 || statusInt == 502 || statusInt == 503);

                            txn.FailureReason = "HTTP " + statusInt + ": " + rawResponse;
                            await _db.SaveChangesAsync();

                            if (isRetriable && attempt < _maxRetries)
                            {
                                // Exponential backoff: 500ms, 1000ms, 2000ms
                                await Task.Delay((int)Math.Pow(2, attempt) * 250);
                                continue;
                            }

                            finalResponse = new LoopNormalizedResponse
                            {
                                Success = false,
                                Status = "FAILED",
                                Message = "LOOP API request failed with status code " + statusInt,
                                TransactionId = txn.InternalTransactionId,
                                TxnReference = txn.TxnReference,
                                Amount = amount,
                                Channel = channel,
                                Retriable = isRetriable,
                                RawStatusCode = statusInt.ToString(),
                                ErrorCode = "HTTP_" + statusInt
                            };
                            return finalResponse;
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Timeout scenario - retriable with same txnReference and fresh signing fields
                        txn.FailureReason = "HTTP Request Timeout on attempt " + attempt;
                        await _db.SaveChangesAsync();

                        if (attempt < _maxRetries)
                        {
                            await Task.Delay(500 * attempt);
                            continue;
                        }

                        return new LoopNormalizedResponse
                        {
                            Success = false,
                            Status = "TIMEOUT",
                            Message = "LOOP API connection timed out after " + _maxRetries + " attempts.",
                            TransactionId = txn.InternalTransactionId,
                            TxnReference = txn.TxnReference,
                            Amount = amount,
                            Channel = channel,
                            Retriable = true,
                            ErrorCode = "TIMEOUT"
                        };
                    }
                    catch (Exception ex)
                    {
                        txn.FailureReason = ex.Message;
                        await _db.SaveChangesAsync();

                        return new LoopNormalizedResponse
                        {
                            Success = false,
                            Status = "ERROR",
                            Message = "LOOP Client Exception: " + ex.Message,
                            TransactionId = txn.InternalTransactionId,
                            TxnReference = txn.TxnReference,
                            Amount = amount,
                            Channel = channel,
                            Retriable = false,
                            ErrorCode = "EXCEPTION"
                        };
                    }
                }
            }

            return finalResponse ?? new LoopNormalizedResponse
            {
                Success = false,
                Status = "FAILED",
                Message = "Maximum retries exceeded.",
                TxnReference = txnRef,
                Amount = amount,
                Channel = channel
            };
        }

        // ==========================================
        // PAYLOAD ENVELOPE BUILDER (APIs #7-#11)
        // ==========================================
        private string BuildRequestPayload(
            string serviceCode,
            string channel,
            string merchantTill,
            string merchantRcvTill,
            string accountNumber,
            string recipientMobileNo,
            decimal amount,
            string purposeOfPayment,
            string txnRef,
            LoopSignatureResult sigResult)
        {
            var sb = new StringBuilder();
            sb.Append("{");
            sb.Append("\"serviceCode\":\"").Append(serviceCode).Append("\",");
            sb.Append("\"txnReference\":\"").Append(txnRef).Append("\",");
            sb.Append("\"requestParameters\":{");

            sb.Append("\"merchantTill\":\"").Append(merchantTill).Append("\",");

            if (!string.IsNullOrEmpty(channel))
                sb.Append("\"channel\":\"").Append(channel).Append("\",");

            if (!string.IsNullOrEmpty(merchantRcvTill))
                sb.Append("\"merchantRcvTill\":\"").Append(merchantRcvTill).Append("\",");

            if (!string.IsNullOrEmpty(accountNumber))
                sb.Append("\"accountNumber\":\"").Append(accountNumber).Append("\",");

            if (!string.IsNullOrEmpty(recipientMobileNo))
                sb.Append("\"recipientMobileNo\":\"").Append(recipientMobileNo).Append("\",");

            if (!string.IsNullOrEmpty(purposeOfPayment))
                sb.Append("\"purposeOfPayment\":\"").Append(purposeOfPayment).Append("\",");

            sb.Append("\"amount\":").Append(amount.ToString("F2")).Append(",");

            // Documented HMAC Signing Envelope Fields inside requestParameters
            sb.Append("\"timestamp\":\"").Append(sigResult.Timestamp).Append("\",");
            sb.Append("\"nonce\":\"").Append(sigResult.Nonce).Append("\",");
            sb.Append("\"signature\":\"").Append(sigResult.Signature).Append("\"");

            sb.Append("}}");
            return sb.ToString();
        }

        private static string FormatPhoneNumber(string phone, string channel)
        {
            if (string.IsNullOrEmpty(phone)) return "254705568254";
            phone = phone.Trim().Replace(" ", "").Replace("-", "");

            if (channel == "PESALINK")
            {
                // Must be international format without leading + (e.g. 254705568254)
                if (phone.StartsWith("+")) return phone.Substring(1);
                if (phone.StartsWith("0")) return "254" + phone.Substring(1);
                return phone;
            }

            if (phone.StartsWith("+")) return phone;
            if (phone.StartsWith("07") || phone.StartsWith("01")) return "254" + phone.Substring(1);
            return phone;
        }

        private static string MaskSensitiveData(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Replace("secret", "***")
                       .Replace("access_token", "***")
                       .Replace("Bearer", "Bearer ***");
        }

        private static string ExtractJsonValue(string json, string key)
        {
            if (string.IsNullOrEmpty(json)) return null;
            int keyIdx = json.IndexOf("\"" + key + "\"");
            if (keyIdx == -1) return null;
            int colonIdx = json.IndexOf(":", keyIdx);
            if (colonIdx == -1) return null;
            int startQuote = json.IndexOf("\"", colonIdx);
            if (startQuote == -1)
            {
                int start = colonIdx + 1;
                int end = json.IndexOfAny(new[] { ',', '}' }, start);
                return end != -1 ? json.Substring(start, end - start).Trim() : null;
            }
            int endQuote = json.IndexOf("\"", startQuote + 1);
            return endQuote != -1 ? json.Substring(startQuote + 1, endQuote - startQuote - 1) : null;
        }
    }
}
