# Serilog ve Seq ile .NET Uygulamalarında Yapılandırılmış Loglama

## Giriş

Modern yazılım geliştirmede loglama, uygulamaların sağlığını izlemek ve hata ayıklamak için kritik bir öneme sahiptir. Bu yazıda, .NET ekosisteminde popüler olan Serilog ve Seq teknolojilerini inceleyeceğiz.

## Serilog Nedir?

Serilog, .NET platformu için geliştirilmiş, yapılandırılmış log kayıtları oluşturmaya odaklanmış güçlü bir loglama kütüphanesidir. Geleneksel string formatlamalı loglar yerine, Serilog structured (yapılandırılmış) loglama yaklaşımını benimser.

### Serilog'un Avantajları

- **Yapılandırılmış Veri**: Loglar sadece metin değil, aynı zamanda aranabilir ve filtrelenebilir veri nesneleri olarak saklanır
- **Farklı Hedeflere Loglama**: Konsoldan veritabanına, dosyadan bulut servislerine kadar çeşitli hedeflere log gönderebilir
- **Zenginleştiriciler**: Log kayıtlarını otomatik olarak ek bilgilerle zenginleştirebilir
- **Performans Odaklı**: Minimum performans etkisiyle çalışacak şekilde tasarlanmıştır

## Seq Nedir?

Seq, yapılandırılmış logları toplamak, araştırmak ve analiz etmek için geliştirilmiş modern bir log sunucusudur. Özellikle Serilog gibi yapılandırılmış loglama kütüphaneleriyle mükemmel bir şekilde entegre olur.

### Seq'in Sunduğu Özellikler

- **Gerçek Zamanlı İzleme**: Logları anında görüntüleme ve filtreleme
- **Güçlü Sorgu Dili**: LINQ benzeri bir sözdizimi ile kompleks sorgulamalar yapabilme
- **Dashboard ve Görselleştirme**: Log verilerini anlamlı grafikler ve panolarla görselleştirme
- **Uyarılar**: Belirli koşullar oluştuğunda e-posta, Slack vb. kanallar üzerinden bildirim gönderme
- **API Desteği**: RESTful API aracılığıyla programatik erişim

Seq, özellikle çoklu uygulama ortamlarında ve mikroservis mimarilerinde log yönetimini basitleştirir.

## Gerekli NuGet Paketlerini Yükleme

Serilog ve Seq ile çalışmak için aşağıdaki NuGet paketlerini projenize eklemeniz gerekir:

```bash
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
dotnet add package Serilog.Sinks.Seq
dotnet add package Serilog.Enrichers.Environment
dotnet add package Serilog.Enrichers.Thread
```

### Paket Açıklamaları

| Paket | Açıklama |
|-------|----------|
| **Serilog.AspNetCore** | ASP.NET Core uygulamalarında Serilog entegrasyonu sağlar |
| **Serilog.Sinks.Console** | Logları konsola yazdırmak için kullanılır |
| **Serilog.Sinks.Seq** | Logları Seq sunucusuna göndermek için kullanılır |
| **Serilog.Enrichers.Environment** | Loglara makine adı gibi çevre bilgilerini ekler |
| **Serilog.Enrichers.Thread** | Loglara thread ID ve thread adı bilgilerini ekler |

## ASP.NET Core Uygulamasında Serilog Konfigürasyonu

Sadece uygulamanızda Program.cs içerisinde aşağıdaki configleri eklemelisiniz.

```csharp
    builder.Host.UseSerilog((context, loggerConfig) => loggerConfig.ReadFrom.Configuration(context.Configuration));
    .
    .
    .

    app.UseSerilogRequestLogging();
```

### Endpointler ve kullanımları

Serilog'u kullanarak çeşitli loglama seviyelerinde nasıl log yazabileceğinize dair örnekler:

```csharp
        app.MapGet("/information", (ILogger<Program> logger) => {
            logger.LogInformation("/information endpoint'i çağrıldı");
            return "Information log message created";
        });

        // Yeni warning endpoint'i
        app.MapGet("/warning", (ILogger<Program> logger) => {
            logger.LogWarning("/warning endpoint'i çağrıldı");
            return "Warning log message created";
        });

        // Yeni error endpoint'i
        app.MapGet("/error", (ILogger<Program> logger) => {
            logger.LogError("/error endpoint'i çağrıldı");
            return "Error log message created";
        });
```

### Appsettings ayarlama

```json
  "Serilog": {
    "Using": [
      "Serilog.Sinks.Console",
      "Serilog.Sinks.Seq"
    ],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://observability-seq:5341" }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "Observability.Api"
    }
  }
```

### Docker ile Seq Kurulumu

En hızlı kurulum yöntemi Docker kullanmaktır:

```bash
docker run --name seq -d --restart unless-stopped -e ACCEPT_EULA=Y -p 5341:5341 -p 5342:80 datalust/seq:latest
```

Dilerseniz compose dosyanıza da ekleyebilirsiniz

```yaml
  observability-seq:
    image: datalust/seq:latest
    container_name: observability-seq
    ports:
      - "9179:5341"
      - "9180:80"
    environment:
      - ACCEPT_EULA=Y 
    volumes:
      - seq-data:/data
    networks:
      - observability-example-network
```

### Seq'i Kullanmaya Başlama

Kurulum tamamlandıktan sonra, tarayıcınızda `http://localhost:9180` adresine giderek Seq arayüzüne erişebilirsiniz.

## Seq'te Sorgu Örnekleri

Seq'in güçlü sorgu yeteneklerinin birkaç örneği:

```text
// Hata loglarını bulmak
@Level = 'Error'

// Belirli bir kullanıcının işlemlerini bulmak
UserId = 'user@example.com'

// Belirli bir süre içinde gerçekleşen yavaş istekleri bulmak
RequestPath like '/api/%' and ElapsedMilliseconds > 500

// HTTP 500 hatalarını bulmak
StatusCode = 500
```

## Sonuç

Serilog ve Seq, .NET uygulamalarınızda yapılandırılmış loglama ve log analizi için güçlü bir kombinasyon sunar. Yapılandırılmış loglar, uygulama sorunlarını daha hızlı tanımlamanızı ve çözmenizi sağlar. Seq gibi bir görselleştirme ve analiz aracı ise, bu verileri anlamlandırmayı ve proaktif bir şekilde izlemeyi kolaylaştırır.

Bu teknolojileri kullanarak, uygulamanızın davranışını daha iyi anlayabilir, performans sorunlarını erken tespit edebilir ve kullanıcı deneyimini iyileştirebilirsiniz.

## Kaynaklar

- [Serilog Resmi Web Sitesi](https://serilog.net/)
- [Seq Resmi Web Sitesi](https://datalust.co/seq)
- [Serilog GitHub Repository](https://github.com/serilog/serilog)
- [Seq Dokümantasyon](https://docs.datalust.co/docs)
