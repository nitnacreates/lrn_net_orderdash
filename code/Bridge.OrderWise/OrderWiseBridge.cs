using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bridge.OrderWise
{
    /// <summary>
    /// Skeleton of the real OrderWise bridge (§7, §10.3). This is the same logic MockOrderWise
    /// already exercises, packaged as an OrderWise plugin: OrderWise loads the assembly and calls
    /// these entry points, which talk to Central over the two bridge endpoints.
    /// OrderWise's own interfaces (IImportSOPlugin / IExportPlugin) can't be referenced here, so the
    /// methods are plain entry points with the mapping left as TODO.
    /// </summary>
    public class OrderWiseBridge
    {
        private readonly HttpClient _http;

        public OrderWiseBridge(string centralUrl, string apiKey)
        {
            _http = new HttpClient { BaseAddress = new Uri(centralUrl) };
            _http.DefaultRequestHeaders.Add("X-Api-Key", apiKey);
        }

        /// <summary>Import: pull pending Central orders and save them as OrderWise SalesOrders.</summary>
        public async Task ImportAsync()
        {
            var pending = await _http.GetStringAsync("/api/orders/pending");

            // TODO: deserialise `pending` -> OrderWise SalesOrder (§10.3):
            //   OrderNumber <- OrderNumber, CustomerOrderRef <- CustomerRef,
            //   OrderDate, RequiredDate/PromisedDate, lines (Code <- Sku, Qty, Price, TaxCode).
            Console.WriteLine("TODO: map " + pending.Length + " bytes of canonical orders into OrderWise.");
        }

        /// <summary>Export: push a dispatched OrderWise SalesOrder back to Central.</summary>
        public async Task ExportDispatchAsync(object salesOrder)
        {
            // TODO: map the dispatched OrderWise SalesOrder -> canonical dispatch.
            var content = new StringContent("{}", Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/dispatch") { Content = content };
            request.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString("N"));

            var response = await _http.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
    }
}
