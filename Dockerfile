FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS base

RUN sed -i 's/MinProtocol = TLSv1.2/MinProtocol = TLSv1.0/g' /etc/ssl/openssl.cnf

WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS publish
ARG VERSION=1.0.0
WORKDIR /src
COPY . .
RUN dotnet publish "src/Domain0.Service/Domain0.Service.csproj" \
    -c Release /p:Version=${VERSION} \
    -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Domain0.Service.dll"]