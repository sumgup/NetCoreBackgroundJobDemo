namespace NetCoreBackgroundJobDemo.Payloads
{
    public class StripeCharge
    {
        public string Amount { get; set; }
        public string Currency { get; set; }
        public string Source { get; set; }
        public string Description { get; set; }
    }
}
