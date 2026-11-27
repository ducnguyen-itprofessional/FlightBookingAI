using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace FlightBookingAI.Web.Services
{
    public class ZaloPayCreateResponse
    {
        [JsonProperty("return_code")]
        public int ReturnCode { get; set; }

        [JsonProperty("return_message")]
        public string ReturnMessage { get; set; }

        [JsonProperty("sub_return_code")]
        public int SubReturnCode { get; set; }

        [JsonProperty("sub_return_message")]
        public string SubReturnMessage { get; set; }

        [JsonProperty("order_url")]
        public string OrderUrl { get; set; }

        [JsonProperty("zp_trans_token")]
        public string ZpTransToken { get; set; }
    }

    public class ZaloPayService
    {
        private readonly string _appId = ConfigurationManager.AppSettings["ZaloPayAppId"];
        private readonly string _key1 = ConfigurationManager.AppSettings["ZaloPayKey1"];
        private readonly string _key2 = ConfigurationManager.AppSettings["ZaloPayKey2"];
        private readonly string _createOrderUrl = ConfigurationManager.AppSettings["ZaloPayCreateOrderUrl"];
        private readonly string _redirectUrl = ConfigurationManager.AppSettings["ZaloPayRedirectUrl"];

        public async Task<ZaloPayCreateResponse> CreateOrderAsync(int bookingId, string bookingCode, decimal amount, string userEmail)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

            string appId = _appId.Trim();
            string key1 = _key1.Trim();
            string redirectUrl = _redirectUrl.Trim();
            string appUser = "user123";

            long appTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string appTransId = DateTime.Now.ToString("yyMMdd") + "_" + bookingId + "_" + new Random().Next(100000, 999999);
            long totalAmount = (long)amount;

            string embedData = JsonConvert.SerializeObject(new { redirecturl = redirectUrl }); 
            string items = "[]";

            string rawMac = $"{appId}|{appTransId}|{appUser}|{totalAmount}|{appTime}|{embedData}|{items}";
            string mac = ComputeHmacSha256(rawMac, key1);

            var requestBody = new
            {
                app_id = int.Parse(appId),
                app_user = appUser,
                app_time = appTime,
                amount = totalAmount,
                app_trans_id = appTransId,
                embed_data = embedData,
                item = items,
                description = "Thanh toan ve may bay " + bookingCode,
                bank_code = "",
                mac = mac
            };

            string jsonPayload = JsonConvert.SerializeObject(requestBody);

            using (var client = new HttpClient())
            {
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(_createOrderUrl.Trim(), content);
                var responseString = await response.Content.ReadAsStringAsync();

                var res = JsonConvert.DeserializeObject<ZaloPayCreateResponse>(responseString);
                if (res != null && res.ReturnCode != 1)
                {
                    res.SubReturnMessage = (res.SubReturnMessage ?? res.ReturnMessage) + " | AppId=" + appId;
                }
                return res;
            }
        }

        public bool VerifyRedirectChecksum(string appId, string appTransId, string pmcId, string bankCode, string amount, string discountAmount, string status, string checksum)
        {
            string rawData = $"{appId}|{appTransId}|{pmcId}|{bankCode}|{amount}|{discountAmount}|{status}";
            string computedChecksum = ComputeHmacSha256(rawData, _key2.Trim());
            return string.Equals(computedChecksum, checksum, StringComparison.OrdinalIgnoreCase);
        }

        private string ComputeHmacSha256(string data, string key)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            using (var hmac = new HMACSHA256(keyBytes))
            {
                byte[] hashBytes = hmac.ComputeHash(dataBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }
    }
}