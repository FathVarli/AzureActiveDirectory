FROM mcr.microsoft.com/dotnet/aspnet:5.0-buster-slim AS base
# LDAP bağımlılıklarını ekleyelim
RUN apt-get update && apt-get install -y libldap-2.4-2 libldap-dev
# Sembolik bağlantı oluşturalım (bazı durumlarda gerekli olabilir)
RUN ln -s /usr/lib/x86_64-linux-gnu/libldap_r-2.4.so.2 /usr/lib/libldap.so.2
RUN ln -s /usr/lib/x86_64-linux-gnu/liblber-2.4.so.2 /usr/lib/liblber.so.2
RUN apt-get install curl -y
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0-buster-slim AS build
WORKDIR /src
# Proje dosyasını doğru yola kopyala
COPY ["AzureExternalDirectory.csproj", "AzureExternalDirectory/"]
RUN dotnet restore "AzureExternalDirectory/AzureExternalDirectory.csproj"
# Tüm source kodunu kopyala
COPY . AzureExternalDirectory/
WORKDIR "/src/AzureExternalDirectory"
RUN dotnet build "AzureExternalDirectory.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AzureExternalDirectory.csproj" -c Release -o /app/build

FROM base AS final
WORKDIR /app
COPY --from=publish /app/build .
ENV ASPNETCORE_ENVIRONMENT="Production"
# Çalışma zamanında LD_LIBRARY_PATH'i ayarlayalım
ENV LD_LIBRARY_PATH="/usr/lib:/usr/lib/x86_64-linux-gnu:${LD_LIBRARY_PATH}"
ENTRYPOINT ["dotnet", "AzureExternalDirectory.dll"]