FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

ENV ASPNETCORE_URLS=https://+:8080;http://+:5000;
EXPOSE 8080
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["adyen-dotnet-online-payments.csproj", "."]
RUN dotnet restore "./adyen-dotnet-online-payments.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "adyen-dotnet-online-payments.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "adyen-dotnet-online-payments.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "adyen-dotnet-online-payments.dll"]