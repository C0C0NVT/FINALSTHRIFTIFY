using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FINALSTHRIFTIFY
{
    public class CurrencyService
    {
        private const string ApiKey = "d89a6461fbc3d698e445d8d7";
        private const string BaseUrl = "https://v6.exchangerate-api.com/v6/";

        private readonly HttpClient _httpClient;

        public CurrencyService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<decimal> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            try
            {
                string url = $"{BaseUrl}{ApiKey}/pair/{fromCurrency}/{toCurrency}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);
                    return (decimal)data.conversion_rate;
                }

                return GetHardcodedRate(fromCurrency, toCurrency);
            }
            catch
            {
                return GetHardcodedRate(fromCurrency, toCurrency);
            }
        }

        private decimal GetHardcodedRate(string fromCurrency, string toCurrency)
        {
            if (fromCurrency == "USD" && toCurrency == "PHP") return 56.50m;
            if (fromCurrency == "JPY" && toCurrency == "PHP") return 0.38m;
            if (fromCurrency == "KRW" && toCurrency == "PHP") return 0.042m;
            if (fromCurrency == "EUR" && toCurrency == "PHP") return 60.25m;

            return 1m;
        }
    }
}
