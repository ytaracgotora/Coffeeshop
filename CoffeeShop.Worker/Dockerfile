# Use the official .NET 10 SDK to build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["CoffeeShop.Shared/CoffeeShop.Shared.csproj", "CoffeeShop.Shared/"]
COPY ["CoffeeShop.Worker/CoffeeShop.Worker.csproj", "CoffeeShop.Worker/"]
RUN dotnet restore "CoffeeShop.Worker/CoffeeShop.Worker.csproj"
COPY . .
RUN dotnet publish "CoffeeShop.Worker/CoffeeShop.Worker.csproj" -c Release -o /app/publish

# Use the runtime image for a tiny, secure container
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CoffeeShop.Worker.dll"]
