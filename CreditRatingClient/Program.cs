using System;
using System.Threading.Tasks;
using CreditRatingService;
using Grpc.Net.Client;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Extensions.Configuration;
using Grpc.Core;
using Auth0.AuthenticationApi;
using Auth0.AuthenticationApi.Models;

namespace CreditRatingClient
{
  class Program
  {
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
      var auth0Client = new AuthenticationApiClient(appAuth0Settings["Domain"]);
      var tokenRequest = new ClientCredentialsTokenRequest()
      {
        ClientId = appAuth0Settings["ClientId"],
        ClientSecret = appAuth0Settings["ClientSecret"],
        Audience = appAuth0Settings["Audience"]
      };
      var tokenResponse = await auth0Client.GetTokenAsync(tokenRequest);

      return tokenResponse.AccessToken;
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
