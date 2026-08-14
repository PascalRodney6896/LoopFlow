using System;
using System.Threading.Tasks;
using System.Web.Mvc;
using LoopFlow.Models;
using LoopFlow.Services;

namespace LoopFlow.Controllers
{
    public class LoopPaymentsApiController : Controller
    {
        private readonly ILoopClientService _loopClient;

        public LoopPaymentsApiController()
        {
            _loopClient = new LoopClientService();
        }

        // POST: /api/payments/pay-to-till (API #7)
        [HttpPost]
        public async Task<ActionResult> PayToTill(string merchantRcvTill, string accountNumber, decimal amount, string txnRef = null)
        {
            if (string.IsNullOrEmpty(merchantRcvTill) || amount <= 0)
            {
                return Json(new { success = false, message = "Invalid recipient till or amount." }, JsonRequestBehavior.AllowGet);
            }

            var result = await _loopClient.PayToMpesaTillAsync(merchantRcvTill, accountNumber, amount, txnRef);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: /api/payments/pay-to-paybill (API #8)
        [HttpPost]
        public async Task<ActionResult> PayToPaybill(string paybillNumber, string accountNumber, decimal amount, string txnRef = null)
        {
            if (string.IsNullOrEmpty(paybillNumber) || amount <= 0)
            {
                return Json(new { success = false, message = "Invalid paybill number or amount." }, JsonRequestBehavior.AllowGet);
            }

            var result = await _loopClient.PayToMpesaPaybillAsync(paybillNumber, accountNumber, amount, txnRef);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: /api/payments/send-money-loop (API #9)
        [HttpPost]
        public async Task<ActionResult> SendMoneyLoop(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null)
        {
            if (string.IsNullOrEmpty(recipientMobileNo) || amount <= 0)
            {
                return Json(new { success = false, message = "Invalid recipient mobile number or amount." }, JsonRequestBehavior.AllowGet);
            }

            var result = await _loopClient.SendMoneyLoopAsync(recipientMobileNo, amount, purposeOfPayment, txnRef);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: /api/payments/send-money-mpesa (API #10)
        [HttpPost]
        public async Task<ActionResult> SendMoneyMpesa(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null)
        {
            if (string.IsNullOrEmpty(recipientMobileNo) || amount <= 0)
            {
                return Json(new { success = false, message = "Invalid recipient mobile number or amount." }, JsonRequestBehavior.AllowGet);
            }

            var result = await _loopClient.SendMoneyMpesaAsync(recipientMobileNo, amount, purposeOfPayment, txnRef);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // POST: /api/payments/send-money-pesalink (API #11)
        [HttpPost]
        public async Task<ActionResult> SendMoneyPesalink(string recipientMobileNo, decimal amount, string purposeOfPayment, string txnRef = null)
        {
            if (string.IsNullOrEmpty(recipientMobileNo) || amount <= 0)
            {
                return Json(new { success = false, message = "Invalid recipient mobile number or amount." }, JsonRequestBehavior.AllowGet);
            }

            var result = await _loopClient.SendMoneyPesalinkAsync(recipientMobileNo, amount, purposeOfPayment, txnRef);
            return Json(result, JsonRequestBehavior.AllowGet);
        }

        // GET: /api/payments/test-connectivity (Sandbox Connectivity Verification)
        [HttpGet]
        public async Task<ActionResult> TestConnectivity()
        {
            var authService = new LoopAuthService();
            var signatureService = new LoopSignatureService();

            string token = await authService.GetAccessTokenAsync();
            var sig = signatureService.GenerateSignature("133238");

            var testResult = await _loopClient.SendMoneyLoopAsync("254705568254", 10.00m, "Sandbox Connectivity Test", "TEST-CONN-" + Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper());

            return Json(new
            {
                sandboxUrl = "https://sandbox.loop.co.ke",
                authenticatedTokenObtained = !string.IsNullOrEmpty(token),
                signatureGenerationSuccess = !string.IsNullOrEmpty(sig.Signature),
                canonicalSignature = sig.Signature,
                timestamp = sig.Timestamp,
                nonce = sig.Nonce,
                realResponse = testResult
            }, JsonRequestBehavior.AllowGet);
        }
    }
}
