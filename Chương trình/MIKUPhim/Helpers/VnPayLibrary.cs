using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;

namespace MIKUPhim.Helpers
{
    public class VnPayLibrary
    {
        private SortedList<string, string> requestData =
            new SortedList<string, string>();

        public void AddRequestData(string key, string value)
        {
            if (!string.IsNullOrEmpty(value))
            {
                requestData.Add(key, value);
            }
        }

        public string CreateRequestUrl(string baseUrl, string hashSecret)
        {
            StringBuilder data = new StringBuilder();
            StringBuilder hashData = new StringBuilder();

            foreach (var item in requestData)
            {
                if (hashData.Length > 0)
                {
                    hashData.Append("&");
                    data.Append("&");
                }

                hashData.Append(
                    WebUtility.UrlEncode(item.Key) + "=" +
                    WebUtility.UrlEncode(item.Value)
                );

                data.Append(
                    WebUtility.UrlEncode(item.Key) + "=" +
                    WebUtility.UrlEncode(item.Value)
                );
            }

            string secureHash =
                HmacSHA512(hashSecret, hashData.ToString());

            data.Append("&vnp_SecureHash=" + secureHash);

            return baseUrl + "?" + data.ToString();
        }

        private string HmacSHA512(string key, string inputData)
        {
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);

            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);

                StringBuilder hash = new StringBuilder();

                foreach (var b in hashValue)
                {
                    hash.Append(b.ToString("x2"));
                }

                return hash.ToString();
            }
        }
    }
}