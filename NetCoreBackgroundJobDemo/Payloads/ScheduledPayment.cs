namespace NetCoreBackgroundJobDemo.Payloads
{
    public class ScheduledPayment
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public int ManagerId { get; set; }
        public int UserId { get; set; }
        public string StripeCustomerId { get; set; }
        public string StripeId { get; set; }
    }
}