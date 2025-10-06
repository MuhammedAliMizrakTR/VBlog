# ====================================================================
# AŞAMA 1: Uygulamayı Derle (Build Stage)
# ====================================================================

# .NET SDK imajını kullanarak uygulamayı derle
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src

# Proje dosyalarını kopyala
# Projenin bağımlılıklarını ve .csproj dosyasını kopyala
COPY ["VBlog/VBlog.csproj", "VBlog/"]
# Gerekirse diğer proje bağımlılıklarını (örneğin Solution dosyası varsa) buraya ekleyin
# COPY ["VBlog.sln", "."] 

# Bağımlılıkları geri yükle
# Bu adım, .csproj dosyasındaki NuGet paketlerini indirir.
RUN dotnet restore "VBlog/VBlog.csproj"

# Tüm proje dizinini kopyala (kaynak kodları, Controller'lar, Modeller vb.)
COPY . .

# VBlog projesini yayınla (release modunda, çıktı yayın klasörüne)
# '--no-restore' ile bağımlılıkları tekrar indirme
# '--output /app/build' ile derlenen çıktıları belirli bir klasöre yerleştir
WORKDIR /src/VBlog
RUN dotnet publish "VBlog.csproj" -c Release -o /app/publish --no-restore

# ====================================================================
# AŞAMA 2: Uygulamayı Çalıştır (Publish/Runtime Stage)
# ====================================================================

# Daha küçük ve daha güvenli olan .NET ASP.NET Runtime imajını kullan
# Bu imaj sadece uygulamayı çalıştırmak için gereken minimum bileşenleri içerir.
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

# AŞAMA 1'den (build) derlenmiş uygulama çıktılarını kopyala
# '/app/publish' klasöründeki her şeyi '/app' klasörüne kopyala
COPY --from=build /app/publish .

# Uygulamanın statik dosyalarına (wwwroot) erişim için Data klasörünü oluştur
# Bu klasör uygulama çalıştığında içi doldurulacak
# Program.cs'de Path.Combine(builder.Environment.ContentRootPath, "Data") kullanıldığı için,
# ContentRootPath genelde /app olduğu için Data klasörünü /app altında oluştururuz.
RUN mkdir -p Data

# ASP.NET Core uygulamasının dinleyeceği portu belirle
# Render.com genellikle kendi dinamik portlarını atar ve PORT ortam değişkenini kullanır.
# Bu, ASP.NET Core tarafından otomatik olarak algılanır.
EXPOSE 8080 
# Yorum satırı bir sonraki satıra taşındı veya kaldırıldı.


# Uygulamayı başlat
# VBlog.dll, dotnet publish komutunun çıktısı olan ana DLL dosyasıdır.
ENTRYPOINT ["dotnet", "VBlog.dll"]
