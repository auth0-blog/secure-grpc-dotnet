using System;
using System.Threading.Tasks;
using CreditRatingService;
using Grpc.Net.Client;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using Grpc.Core;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;


namespace CreditRatingClient
{
  class Program
  {
    private static readonly HttpClient httpClient = new HttpClient();
    static async Task Main(string[] args)
    {
      var serverAddress = "https://localhost:5001";

      if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
      {
        // The following statement allows you to call insecure services. To be used only in development environments.
        AppContext.SetSwitch(
            "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        serverAddress = "http://localhost:5000";
      }

      var channel = GrpcChannel.ForAddress(serverAddress);
      var client = new CreditRatingCheck.CreditRatingCheckClient(channel);
      var creditRequest = new CreditRequest { CustomerId = "id0201", Credit = 7000 };

      var accessToken = await GetAccessToken();
      var headers = new Metadata();
      headers.Add("Authorization", $"Bearer {accessToken}");

      var reply = await client.CheckCreditRequestAsync(creditRequest, headers);

      Console.WriteLine($"Credit for customer {creditRequest.CustomerId} {(reply.IsAccepted ? "approved" : "rejected")}!");
      Console.WriteLine("Press any key to exit...");
      Console.ReadKey();
    }

    // Using an authorized channel doesn't work on MacOS
    //static async Task Main(string[] args)
    //{
    //    //// The port number(5001) must match the port of the gRPC server.
    //    var channel = await CreateAuthorizedChannel("https://localhost:5001");
    //    var client = new CreditRatingCheck.CreditRatingCheckClient(channel);
    //    var creditRequest = new CreditRequest { CustomerId = "id0201", Credit = 7000 };
    //    var reply = await client.CheckCreditRequestAsync(creditRequest);

    //    Console.WriteLine($"Credit for customer {creditRequest.CustomerId} {(reply.IsAccepted ? "approved" : "rejected")}!");
    //    Console.WriteLine("Press any key to exit...");
    //    Console.ReadKey();
    //}

    static IConfiguration GetAppSettings()
    {
      var builder = new ConfigurationBuilder()
           .SetBasePath(Directory.GetCurrentDirectory())
           .AddJsonFile("appsettings.json");

      return builder.Build();
    }

    static async Task<string> GetAccessToken()
    {
      var appAuth0Settings = GetAppSettings().GetSection("Auth0");

      var requestContent = JsonSerializer.Serialize(new
      {
        client_id = appAuth0Settings["ClientId"],
        client_secret = appAuth0Settings["ClientSecret"],
        audience = appAuth0Settings["Audience"],
        grant_type = "client_credentials"
      });

      var response = await httpClient.PostAsync($"https://{appAuth0Settings["Domain"]}/oauth/token",
              new StringContent(requestContent,
              UnicodeEncoding.UTF8,
              "application/json"));
      var responseString = response.Content.ReadAsStringAsync().Result;

      var responseObj = JsonSerializer.Deserialize<Dictionary<string, Object>>(responseString);

      return responseObj["access_token"].ToString();
    }

    private async static Task<GrpcChannel> CreateAuthorizedChannel(string address)
    {
      var accessToken = await GetAccessToken();

      var credentials = CallCredentials.FromInterceptor((context, metadata) =>
      {
        if (!string.IsNullOrEmpty(accessToken))
        {
          metadata.Add("Authorization", $"Bearer {accessToken}");
        }
        return Task.CompletedTask;
      });

      // SslCredentials is used here because this channel is using TLS.
      // CallCredentials can't be used with ChannelCredentials.Insecure on non-TLS channels.
      var channel = GrpcChannel.ForAddress(address, new GrpcChannelOptions
      {
        Credentials = ChannelCredentials.Create(new SslCredentials(), credentials)
      });
      return channel;
    }
  }
}
