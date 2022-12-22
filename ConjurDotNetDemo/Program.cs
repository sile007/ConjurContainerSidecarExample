// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Text;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Net;

namespace ConsoleApp1
{
    class Program
    {

        static string conjurApplianceURL = "";
        static string conjurAccount = "";
        static string conjurSSLCert = "";
        static string test = "";
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            conjurApplianceURL = Environment.GetEnvironmentVariable("CONJUR_APPLIANCE_URL");
            conjurAccount = Environment.GetEnvironmentVariable("CONJUR_ACCOUNT");
            conjurSSLCert = Environment.GetEnvironmentVariable("CONJUR_SSL_CERTIFICATE");

            Console.WriteLine("Conjur URL: " + conjurApplianceURL);
            Console.WriteLine("Conjur Account: " + conjurAccount);
            Console.WriteLine("Conjur SSL: " + conjurSSLCert);

            var file = File.Open("conjur.pem", FileMode.Create);
            byte[] conjurSSLCertByte = new UTF8Encoding(true).GetBytes(conjurSSLCert);
            file.Write(conjurSSLCertByte);

            readBase64Token();
        }

        static async void readBase64Token()
        {
            Console.WriteLine("I'm in function");
            try
            {
                var accessToken = File.ReadLines("/run/conjur/access-token");

                foreach (var line in accessToken)
                {
                    Console.WriteLine(line);
                    var getBytes = Encoding.ASCII.GetBytes(line);
                    var data = Convert.ToBase64String(getBytes);
                    Console.WriteLine(data);
                    var username = MakeGetRequest(conjurApplianceURL + "/secrets/" + conjurAccount + "/variable/sidecar-test-app-secrets/username", data);
                    var password = MakeGetRequest(conjurApplianceURL + "/secrets/" + conjurAccount + "/variable/sidecar-test-app-secrets/password", data);
                    Console.WriteLine(username.Result);
                    Console.WriteLine(password.Result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            Thread.Sleep(3000);
            readBase64Token();
        }

        static async Task<string> MakeGetRequest(string url, string accessToken)
        {
            var handler = new HttpClientHandler()
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using (HttpClient client = new HttpClient(handler))
            {
                Console.WriteLine("AccessToken: " + accessToken);
                ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Token", "token=\"" + accessToken + "\"");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.AcceptCharset.Add(new StringWithQualityHeaderValue("utf-8"));
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
