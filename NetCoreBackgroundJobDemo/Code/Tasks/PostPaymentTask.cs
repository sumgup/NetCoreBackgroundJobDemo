using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Http;
using Microsoft.eShopOnContainers.BuildingBlocks.Resilience.Http;
using Microsoft.Extensions.Logging;
using NetCoreBackgroundJobDemo.Payloads;
using Polly;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NetCoreBackgroundJobDemo.Code
{
    public class PostPaymentTask : IScheduledTask
    {
        private readonly IConfiguration _configuration;

        private IHttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccesor;
        public string Schedule => "*/1 * * * *"; // Every Minute

        private readonly ILogger<PostPaymentTask> _logger;

        public PostPaymentTask(IHttpClient httpClient, IHttpContextAccessor httpContextAccesor,
            ILogger<PostPaymentTask> logger, IConfiguration config)
        {
            _httpClient = httpClient;
            _httpContextAccesor = httpContextAccesor;
            _logger = logger;
            _configuration = config;
        }

        public async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"PostPaymentService is starting.");

            stoppingToken.Register(() => _logger.LogDebug($"#1 PostPaymentService background task is stopping."));

            _logger.LogDebug($"PostPaymentService background task is doing background work.");

            await GetScheduledPayments()
                .OnSuccess(payments => PostChargeOnCustomerCard(payments))
                .OnFailure(() => _logger.LogError("Error retrieving Payment records"));

            await Task.CompletedTask;
        }

        private Task LogFailure(string failure)
        {
            throw new NotImplementedException();
        }

        private async Task<Result<bool>> PostChargeOnCustomerCard(ICollection<ScheduledPayment>
            scheduledPayments)
        {
            // A bulkhead isolation policy to restrict number of concurrent calls
            // https://markheath.net/post/constraining-concurrent-threads-csharp
            var bulkhead = Policy.BulkheadAsync(20, Int32.MaxValue);
            var chargeCustomerTasks = new List<Task>();

            foreach (var scheduledPayment in scheduledPayments)
            {
                var t = bulkhead.ExecuteAsync(async () =>
                {
                    var postStripeChargeResult = await CreateCharge(scheduledPayment);

                    _logger.LogTrace(postStripeChargeResult.ToString());

                    // In the same loop Update the Payment
                    bool isPaymentPosted = IsPaymentPosted(postStripeChargeResult);

                    var updatePayment = await UpdatePayment(scheduledPayment.Id, isPaymentPosted);

                    if (!updatePayment)
                        PostFailureToQueue(scheduledPayment);
                });

                chargeCustomerTasks.Add(t);
            }

            await Task.WhenAll(chargeCustomerTasks);

            return Result.Ok(true);
        }

        private async Task<PostStripeChargeResult> CreateCharge(ScheduledPayment scheduledPayment)
        {
            var charge = new StripeCharge
            {
                Amount = scheduledPayment.Amount.ToString(),
                Currency = "usd",
                Source = "tok_visa ",
                Description = "Posted using ResilientHttpClient"
            };

            string response = "";
            try
            {
                var values = GetValues(scheduledPayment);
                string stripeApiKey = await GetStripeKeyAsync();

                var httpResponse = await _httpClient.PostToStripe(uri: "https://api.stripe.com/v1/charges",
                    values: values,
                    apiKey: stripeApiKey);

                response = await httpResponse.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            _logger.LogInformation(response);

            var mapResult = MapToPostStripeChargeResult(response);

            return mapResult;
        }

        /// <summary>
        /// In production app this has to retrieved to KeyVault or Encrypted Store
        /// 
        /// </summary>
        /// <returns></returns>
        private async Task<string> GetStripeKeyAsync()
        {
            var lines = await System.IO.File.ReadAllLinesAsync("StripeApiKey.txt");
            var stripeApiKey = lines[0].ToString();
            return stripeApiKey;
        }

        private void PostFailureToQueue(ScheduledPayment scheduledPayment)
        {
            // The case of Payment Microservice not available, post failures to SQS
        }

        private bool IsPaymentPosted(PostStripeChargeResult postStripeChargeResult)
        {
            return true;
        }

        private PostStripeChargeResult MapToPostStripeChargeResult(string response)
        {
            return new PostStripeChargeResult(response);
        }

        /// <summary>
        /// Simulating this for now. Ideally it needs to go through Adapter Layer
        /// </summary>
        /// <returns></returns>
        private async Task<bool> UpdatePayment(int paymentId, bool status)
        {
            return await Task.FromResult(true);
        }

        private Dictionary<string, string> GetValues(ScheduledPayment scheduledPayment)
                => new Dictionary<string, string>
                {
                   { "amount", scheduledPayment.Amount.ToString()},
                   { "currency", "usd" },
                   { "source", scheduledPayment.StripeCustomerId},
                   { "description", "From NetCoreBackGroundJon using ResilientHttpClient" }
                };

        private async Task<Result<ICollection<ScheduledPayment>>> GetScheduledPayments()
        {
            ICollection<ScheduledPayment> scheduledPayments = new List<ScheduledPayment>()
            {
                new ScheduledPayment(){Id = 1, Amount = 4000M, ManagerId = 1,
                    StripeCustomerId = "tok_visa"
                   , UserId = 1},
                // Invalid token to simulate erro
                new ScheduledPayment(){Id = 1, Amount = 4000M, ManagerId = 1,
                    StripeCustomerId = "tok_visa222"
                   , UserId = 1},
                new ScheduledPayment(){Id = 1, Amount = 5000M, ManagerId = 198,
                    StripeCustomerId = "tok_visa_debit",
                    UserId = 2229},
                new ScheduledPayment(){Id = 1, Amount = 6000M, ManagerId = 178,
                    StripeCustomerId = "tok_mastercard",
                    UserId = 2229},
            };

            return await Task.FromResult(Result.Ok(scheduledPayments));
        }
    }
}