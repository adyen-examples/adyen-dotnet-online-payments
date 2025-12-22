FROM mcr.microsoft.com/dotnet/sdk:6.0-focal

# Set environment variables
ENV DOTNET_MULTILEVEL_LOOKUP=0
ENV ASPNETCORE_ENVIRONMENT=Development

# Verify .NET 6.0 is installed (this will fail the build if wrong version)
RUN dotnet --version | grep -E "^6\." || (echo "ERROR: Expected .NET 6.0 but got:" && dotnet --version && exit 1)

