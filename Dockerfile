FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

ENV ASPNETCORE_URLS=http://+8080;https://+44303
ENV ASPNETCORE_HTTP_PORT=8080
ENV ASPNETCORE_HTTPS_PORT=44303
EXPOSE 8080
EXPOSE 44303

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