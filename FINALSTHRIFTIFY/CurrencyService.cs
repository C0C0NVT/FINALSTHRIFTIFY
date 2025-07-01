using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FINALSTHRIFTIFY
{
    public class CurrencyService
    {
        private const string ApiKey = "d89a6461fbc3d698e445d8d7";
        private const string BaseUrl = "https://v6.exchangerate-api.com/v6/";

        private readonly HttpClient _httpClient;

        // Updated exchange rates - Fixed logic: 1 USD = 56 PHP (not the other way around)
        private readonly Dictionary<string, decimal> phpExchangeRates = new Dictionary<string, decimal>
        {
            { "USD", 56.50m },    // 1 USD = 56.50 PHP
            { "EUR", 60.25m },    // 1 EUR = 60.25 PHP  
            { "JPY", 0.38m },     // 1 JPY = 0.38 PHP
            { "KRW", 0.042m },    // 1 KRW = 0.042 PHP
            { "GBP", 70.15m },    // 1 GBP = 70.15 PHP
            { "AUD", 37.80m },    // 1 AUD = 37.80 PHP
            { "CAD", 41.25m },    // 1 CAD = 41.25 PHP
            { "SGD", 41.50m },    // 1 SGD = 41.50 PHP
            { "HKD", 7.25m },     // 1 HKD = 7.25 PHP
            { "CNY", 7.80m },     // 1 CNY = 7.80 PHP
        };

        public CurrencyService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(10); // Set timeout
        }

        public async Task<decimal> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            try
            {
                // If converting from PHP to another currency
                if (fromCurrency.Equals("PHP", StringComparison.OrdinalIgnoreCase))
                {
                    if (phpExchangeRates.ContainsKey(toCurrency.ToUpper()))
                    {
                        // Try to get live rate first
                        var liveRate = await GetLiveExchangeRate(fromCurrency, toCurrency);
                        if (liveRate > 0)
                        {
                            return liveRate;
                        }
                        
                        // Fall back to hardcoded rate
                        return phpExchangeRates[toCurrency.ToUpper()];
                    }
                }
                
                // If converting to PHP from another currency
                if (toCurrency.Equals("PHP", StringComparison.OrdinalIgnoreCase))
                {
                    if (phpExchangeRates.ContainsKey(fromCurrency.ToUpper()))
                    {
                        // Try to get live rate first
                        var liveRate = await GetLiveExchangeRate(fromCurrency, toCurrency);
                        if (liveRate > 0)
                        {
                            return liveRate;
                        }
                        
                        // Fall back to hardcoded rate (inverse)
                        return 1m / phpExchangeRates[fromCurrency.ToUpper()];
                    }
                }

                // For other currency pairs, try live rate
                var otherRate = await GetLiveExchangeRate(fromCurrency, toCurrency);
                return otherRate > 0 ? otherRate : 1m;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Currency conversion error: {ex.Message}");
                return GetFallbackRate(fromCurrency, toCurrency);
            }
        }

        private async Task<decimal> GetLiveExchangeRate(string fromCurrency, string toCurrency)
        {
            try
            {
                string url = $"{BaseUrl}{ApiKey}/pair/{fromCurrency.ToUpper()}/{toCurrency.ToUpper()}";
                HttpResponseMessage response = await _httpClient.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    dynamic data = JsonConvert.DeserializeObject(json);
                    
                    if (data?.result == "success" && data?.conversion_rate != null)
                    {
                        return (decimal)data.conversion_rate;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Live rate fetch error: {ex.Message}");
            }

            return 0m; // Return 0 to indicate failure
        }

        private decimal GetFallbackRate(string fromCurrency, string toCurrency)
        {
            var from = fromCurrency.ToUpper();
            var to = toCurrency.ToUpper();

            // PHP to other currencies
            if (from == "PHP" && phpExchangeRates.ContainsKey(to))
            {
                return phpExchangeRates[to];
            }

            // Other currencies to PHP  
            if (to == "PHP" && phpExchangeRates.ContainsKey(from))
            {
                return 1m / phpExchangeRates[from];
            }

            // Common currency pairs (hardcoded fallbacks)
            var fallbackRates = new Dictionary<string, decimal>
            {
                { "USD-EUR", 0.92m },
                { "EUR-USD", 1.09m },
                { "USD-GBP", 0.81m },
                { "GBP-USD", 1.24m },
                { "USD-JPY", 148.50m },
                { "JPY-USD", 0.0067m },
                { "EUR-GBP", 0.88m },
                { "GBP-EUR", 1.14m }
            };

            string pairKey = $"{from}-{to}";
            if (fallbackRates.ContainsKey(pairKey))
            {
                return fallbackRates[pairKey];
            }

            // If no rate found, return 1 (no conversion)
            return 1m;
        }

        public decimal ConvertCurrency(decimal amount, string fromCurrency, string toCurrency)
        {
            try
            {
                var rate = GetExchangeRate(fromCurrency, toCurrency).Result;
                return amount * rate;
            }
            catch
            {
                return amount; // Return original amount if conversion fails
            }
        }

        public string FormatCurrency(decimal amount, string currencyCode)
        {
            var currencySymbols = new Dictionary<string, string>
            {
                { "PHP", "₱" },
                { "USD", "$" },
                { "EUR", "€" },
                { "GBP", "£" },
                { "JPY", "¥" },
                { "KRW", "₩" },
                { "AUD", "A$" },
                { "CAD", "C$" },
                { "SGD", "S$" },
                { "HKD", "HK$" },
                { "CNY", "¥" }
            };

            string symbol = currencySymbols.ContainsKey(currencyCode.ToUpper()) 
                ? currencySymbols[currencyCode.ToUpper()] 
                : currencyCode.ToUpper();

            return $"{symbol}{amount:N2}";
        }

        public List<CurrencyInfo> GetSupportedCurrencies()
        {
            return new List<CurrencyInfo>
            {
                new CurrencyInfo { Code = "PHP", Name = "Philippine Peso", Symbol = "₱" },
                new CurrencyInfo { Code = "USD", Name = "US Dollar", Symbol = "$" },
                new CurrencyInfo { Code = "EUR", Name = "Euro", Symbol = "€" },
                new CurrencyInfo { Code = "GBP", Name = "British Pound", Symbol = "£" },
                new CurrencyInfo { Code = "JPY", Name = "Japanese Yen", Symbol = "¥" },
                new CurrencyInfo { Code = "KRW", Name = "South Korean Won", Symbol = "₩" },
                new CurrencyInfo { Code = "AUD", Name = "Australian Dollar", Symbol = "A$" },
                new CurrencyInfo { Code = "CAD", Name = "Canadian Dollar", Symbol = "C$" },
                new CurrencyInfo { Code = "SGD", Name = "Singapore Dollar", Symbol = "S$" },
                new CurrencyInfo { Code = "HKD", Name = "Hong Kong Dollar", Symbol = "HK$" },
                new CurrencyInfo { Code = "CNY", Name = "Chinese Yuan", Symbol = "¥" }
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class CurrencyInfo
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public string Symbol { get; set; }
        
        public override string ToString()
        {
            return $"{Code} - {Name}";
        }
    }
}
