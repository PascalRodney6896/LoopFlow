namespace LoopFlow.Services
{
    public class LoopNormalizedResponse
    {
        public bool Success { get; set; }
        public string Status { get; set; } // COMPLETED, FAILED, RETRYING, PENDING
        public string Message { get; set; }
        public string TransactionId { get; set; }
        public string TxnReference { get; set; }
        public decimal Amount { get; set; }
        public string Channel { get; set; }
        public string ProviderReference { get; set; }
        public string TransferOrderId { get; set; }
        public string TransferRefNo { get; set; }
        public bool Retriable { get; set; }
        public string RawStatusCode { get; set; }
        public string ErrorCode { get; set; }
    }
}
