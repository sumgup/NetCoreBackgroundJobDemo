namespace NetCoreBackgroundJobDemo.Code
{
    public class PostStripeChargeResult
    {
        public string Response { get; }

        public PostStripeChargeResult(string response)
        {
            Response = response;
        }
    }
}