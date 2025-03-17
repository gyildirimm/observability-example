using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace ExampleWebAPI;

public static class CustomMetricExtensions
{
    public static void AddCustomMetrics(this WebApplication app)
    {
        var meter = new Meter("Custom.Http.Meter", "1.0");

        // 1️⃣ HTTP istek toplam sayısı (http_requests_received_total)
        var httpRequestsReceivedTotal = meter.CreateCounter<long>(
            "http_requests_received_total", 
            description: "Total number of received HTTP requests");

        // 2️⃣ Anlık aktif HTTP istek sayısı (microsoft_aspnetcore_hosting_current_requests)
        var activeHttpRequests = 0;
        var httpRequestsCurrent = meter.CreateObservableGauge(
            "microsoft_aspnetcore_hosting_current_requests", 
            () => activeHttpRequests, 
            description: "Current number of active HTTP requests");

        // 3️⃣ HTTP request süreleri (http_request_duration_seconds_sum)
        var httpRequestDuration = meter.CreateHistogram<double>(
            "http_request_duration_seconds_sum", 
            unit: "seconds", 
            description: "Total duration of HTTP requests in seconds");

        // 4️⃣ CPU Kullanımı (system_runtime_cpu_usage)
        var systemRuntimeCpuUsage = meter.CreateObservableGauge(
            "system_runtime_cpu_usage", 
            () => Process.GetCurrentProcess().TotalProcessorTime.TotalSeconds, 
            description: "CPU usage in seconds");

        // 5️⃣ .NET Bellek Kullanımı (dotnet_total_memory_bytes)
        var dotnetMemoryUsage = meter.CreateObservableGauge(
            "dotnet_total_memory_bytes", 
            () => GC.GetTotalMemory(false), 
            description: "Total memory used by .NET runtime");

        // 6️⃣ Garbage Collector Heap Size (system_runtime_gc_heap_size)
        var gcHeapSize = meter.CreateObservableGauge(
            "system_runtime_gc_heap_size", 
            () => GC.GetGCMemoryInfo().HeapSizeBytes, 
            description: "Total heap size in bytes");

        // 7️⃣ HTTP Bağlantı Sayısı (microsoft_aspnetcore_http_connections_current_connections)
        var currentConnections = meter.CreateObservableGauge(
            "microsoft_aspnetcore_http_connections_current_connections", 
            () => new Random().Next(10, 50), // Gerçek bir değer için başka bir mekanizma kullanılabilir.
            description: "Current active HTTP connections");

        // 8️⃣ Thread Pool Tamamlanan İş Sayısı (system_threading_threadpool_completed_items)
        var threadPoolCompletedItems = meter.CreateCounter<long>(
            "system_runtime_threadpool_completed_items_count_total", 
            description: "Total number of completed thread pool items");

        // **Middleware for tracking metrics**
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "unknown";

            httpRequestsReceivedTotal.Add(1, new KeyValuePair<string, object>("path", path)); // 🔥 Path'e göre metrik kaydet

            Interlocked.Increment(ref activeHttpRequests); // Aktif istek sayısını artır
            var stopwatch = Stopwatch.StartNew();
            
            await next();
            
            stopwatch.Stop();
            Interlocked.Decrement(ref activeHttpRequests); // İşlem bittiğinde aktif istek sayısını azalt
            
            httpRequestDuration.Record(stopwatch.Elapsed.TotalSeconds, new KeyValuePair<string, object>("path", path)); // 🔥 Path'e göre süreyi kaydet
        });

        // **ThreadPool tamamlanan iş sayısını güncelleyen bir fonksiyon**
        ThreadPool.RegisterWaitForSingleObject(
            new AutoResetEvent(false), 
            (state, timedOut) => threadPoolCompletedItems.Add(1), 
            null, 
            -1, 
            true);
    }
}