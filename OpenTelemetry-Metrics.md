# **📌 OpenTelemetry ile Observability (Gözlemlenebilirlik) Sağlama**

Bu dökümanda, **.NET Core** uygulamamıza **OpenTelemetry ile Tracing (İzleme), Logging (Günlükleme) ve Metrics (Metrikler)** ekleyerek nasıl gözlemlenebilirlik sağladığımızı anlatıyoruz.

---

## **1️⃣ OpenTelemetry Nedir ve Nasıl Eklenir?**
**OpenTelemetry**, dağıtılmış sistemlerde **izleme (tracing), metrik toplama (metrics) ve log yönetimi (logging)** için kullanılan bir gözlemlenebilirlik aracıdır. **Tracing, Metrics ve Logging** bileşenleri ile uygulamanın performansını ve hata takibini yapmayı sağlar.

### **📦 Gerekli NuGet Paketleri**
Aşağıdaki paketleri yükleyerek OpenTelemetry’yi ekledik:

```sh
dotnet add package OpenTelemetry.Trace
dotnet add package OpenTelemetry.Metrics
dotnet add package OpenTelemetry.Resources
```

### **🛠 OpenTelemetry’yi Uygulamaya Ekleme**
Aşağıdaki gibi **`Program.cs`** içinde **Tracing (İzleme) yapılandırmasını** ekledik:

```csharp
// OpenTelemetry yapılandırması
builder.Services.AddOpenTelemetry()
    .WithTracing(tracerProvider =>
    {
        tracerProvider
            .AddSource("ExampleWebAPI") // Uygulamamızın adını OpenTelemetry'ye tanıtıyoruz
            .SetResourceBuilder(ResourceBuilder.CreateDefault()
                .AddService("ExampleWebAPI") // Servis adını belirtiyoruz
                .AddTelemetrySdk()
                .AddEnvironmentVariableDetector())
            .AddAspNetCoreInstrumentation(options =>
            {
                options.Filter = httpContext =>
                {
                    return !httpContext.Request.Path.Value?.Contains("health") ?? true;
                };
                options.RecordException = true; // Hataları kaydeder
            })
            .AddHttpClientInstrumentation(options =>
            {
                options.RecordException = true;
            })
            .AddConsoleExporter(); // Konsola OpenTelemetry loglarını yazdırır
    });
```

### **🚀 Ne Kazandık?**
✅ **ASP.NET Core requestlerini izleme**  
✅ **HTTP Client çağrılarını izleme**  
✅ **Konsola tracing (izleme) loglarını aktarma**  
✅ **Servis adını OpenTelemetry’ye kaydetme**  

---

## **2️⃣ OpenTelemetry ve Logging**
Uygulamamızda **OpenTelemetry’yi logging ile entegre ettik**. Bu sayede **logları OpenTelemetry formatında yönetebilir ve merkezi bir loglama sistemine gönderebiliriz**.

### **📦 Gerekli NuGet Paketleri**
```sh
dotnet add package Serilog
dotnet add package Serilog.AspNetCore
dotnet add package Serilog.Sinks.Console
```

### **🛠 OpenTelemetry Logging’in Eklenmesi**
Aşağıdaki kod ile **Serilog** yapılandırmasını ve **OpenTelemetry Logging** desteğini ekledik:

```csharp
builder.Host.UseSerilog((context, loggerConfig) =>
{
    loggerConfig.ReadFrom.Configuration(context.Configuration);
});

// OpenTelemetry Logging (Uygulama loglarını OpenTelemetry ile entegre eder)
builder.Logging.AddOpenTelemetry(options =>
{
    // Log kapsamlarını OpenTelemetry'ye dahil eder
    options.IncludeScopes = true;

    // Formatlanmış mesajları OpenTelemetry'ye ekler
    options.IncludeFormattedMessage = true;

    // Log state'lerini OpenTelemetry formatına uygun hale getirir
    options.ParseStateValues = true;
});
```

### **🚀 Ne Kazandık?**
✅ **Serilog ile OpenTelemetry Logging entegrasyonu**  
✅ **Logların OpenTelemetry formatına uygun hale getirilmesi**  
✅ **Log kapsamlarını (scopes) dahil ederek detaylı loglama**  
✅ **Structured Logging (anahtar-değer bazlı logging) desteği**  

---

## **3️⃣ OpenTelemetry ile Metrics Kullanımı**
**Metrics (Metrikler)**, uygulamanın performansını ölçmek için kullanılan veriler sağlar. OpenTelemetry Metrics ile **CPU kullanımı, bellek tüketimi, HTTP request süreleri ve özel metrikleri** toplayabiliriz.

### **📦 Gerekli NuGet Paketleri**
```sh
dotnet add package OpenTelemetry.Metrics
dotnet add package OpenTelemetry.Instrumentation.Process
dotnet add package OpenTelemetry.Instrumentation.Runtime
```

### **🛠 OpenTelemetry Metrics’in Eklenmesi**
Aşağıdaki kod ile **Metrics (Metrikler)** yapılandırmasını OpenTelemetry’ye ekledik:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("MyService"))
            .AddAspNetCoreInstrumentation() // HTTP request metrikleri
            .AddHttpClientInstrumentation() // HTTP client metrikleri
            .AddProcessInstrumentation() // CPU, bellek kullanımı gibi işlem bazlı metrikler
            .AddMeter("System.Runtime") // Garbage Collection (GC) süresi, CPU kullanımı
            .AddMeter("System.Threading") // Thread havuzu metrikleri

            .AddEventCountersInstrumentation(options =>
            {
                // HTTP istek ve bağlantı metrikleri
                options.AddEventSources("System.Net.Http"); // HTTP istemci istek sayısı
                options.AddEventSources("System.Net.Sockets"); // TCP bağlantıları
                options.AddEventSources("System.Net.NameResolution"); // DNS istekleri

                // ASP.NET Core metrikleri
                options.AddEventSources("Microsoft.AspNetCore.Hosting"); // Request süreleri
                options.AddEventSources("Microsoft.AspNetCore.Http.Connections"); // WebSocket ve SignalR metrikleri
            })
            .AddConsoleExporter(); // Metrikleri konsola yazdır
    });
```

### **🚀 Ne Kazandık?**
✅ **HTTP request ve response sürelerini ölçme**  
✅ **HTTP Client çağrılarını izleme**  
✅ **CPU, bellek ve thread metriklerini OpenTelemetry’ye aktarma**  
✅ **EventCounters ile detaylı sistem metriklerini toplama**  

---

## **📌 Genel Sonuç**
Bu yapılandırma ile **OpenTelemetry’yi tam kapsamlı olarak kullanarak izleme (tracing), log yönetimi (logging) ve metrik (metrics) toplamayı sağladık.** 🎯  

- 🔵 **Tracing (İzleme)**: HTTP request’leri, hata yönetimi ve servisler arası çağrıları takip ediyoruz.  
- 🟢 **Logging (Günlükleme)**: Serilog ve OpenTelemetry entegrasyonu ile logları merkezi bir sistemde analiz ediyoruz.  
- 🟠 **Metrics (Metrikler)**: Uygulamanın CPU, bellek ve HTTP performansını gerçek zamanlı izliyoruz.  

🚀 **Bu sayede, dağıtılmış sistemlerde gözlemlenebilirlik (Observability) sağlanmış oldu!**
