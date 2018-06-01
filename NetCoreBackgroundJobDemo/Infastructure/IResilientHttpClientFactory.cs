using Microsoft.eShopOnContainers.BuildingBlocks.Resilience.Http;

namespace NetCoreBackgroundJobDemo.Infastructure
{
    public interface IResilientHttpClientFactory
    {
        ResilientHttpClient CreateResilientHttpClient();
    }
}
