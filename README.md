This repository contains two .NET Core projects implementing a service application (`CreditRatingService`) and a client (`CreditRatingClient`) communicating via gRPC and secured with [Auth0]("http://localhost:5000").

The following article describes the implementation details: [Securing gRPC-based Microservices in .NET Core](https://auth0.com/blog/securing-grpc-microservices-dotnet-core/)

## To run the applications:

Clone the repo: `git clone https://github.com/auth0-blog/secure-grpc-dotnet.git`

To run the `CreditRatingService` application:

1. Move to the `CreditRatingService` folder 
2. Type `dotnet run` in a terminal window



To run the `CreditRatingClient` application:

1. Move to the `CreditRatingClient` folder 
2. Type `dotnet run` in a terminal window



## Requirements:

- [.NET Core 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) installed on your machine

  
