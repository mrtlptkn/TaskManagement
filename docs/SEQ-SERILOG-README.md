# ?? Serilog + Seq Entegrasyonu

## ?? Yüklenen Paketler

`TaskManagement.API` projesine eklenen NuGet paketleri:

| Paket | Versiyon | Açýklama |
|---|---|---|
| `Serilog.AspNetCore` | 8.0.3 | ASP.NET Core entegrasyonu |
| `Serilog.Sinks.Seq` | 8.0.0 | Seq'e log gönderimi |
| `Serilog.Sinks.Console` | 6.0.0 | Console çýktýsý |
| `Serilog.Sinks.File` | 6.0.0 | Dosyaya log yazýmý |
| `Serilog.Enrichers.Environment` | 3.0.0 | MachineName enricher |
| `Serilog.Enrichers.Thread` | 4.0.0 | ThreadId enricher |

---

## ?? Konfigürasyon

### `appsettings.json` (Production)

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "File",
        "Args": {
          "path": "logs/log-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ],
    "Enrich": [ "FromLogContext", "WithMachineName", "WithThreadId" ],
    "Properties": {
      "Application": "TaskManagement.API"
    }
  }
}
```

### `appsettings.Development.json` (Development)

Development ortamýnda daha detaylý log seviyesi kullanýlýr:

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Information",
        "Microsoft.EntityFrameworkCore.Database.Command": "Information",
        "System": "Information"
      }
    },
    "WriteTo": [
      { "Name": "Console" },
      {
        "Name": "Seq",
        "Args": { "serverUrl": "http://localhost:5341" }
      }
    ]
  }
}
```

---

## ?? Program.cs Kurulumu

```csharp
// 1. Bootstrap logger - uygulama baþlamadan önce hatalarý yakalar
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("TaskManagement.API baþlatýlýyor...");

    var builder = WebApplication.CreateBuilder(args);

    // 2. Serilog'u appsettings.json'dan oku ve Host'a baðla
    builder.Host.UseSerilog((context, services, configuration) =>
        configuration.ReadFrom.Configuration(context.Configuration));

    // ...

    // 3. HTTP request logging middleware
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} ? {StatusCode} ({Elapsed:0.0000} ms)";
    });
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    // 4. Fatal hatalar - uygulama tamamen çöktüðünde
    Log.Fatal(ex, "TaskManagement.API beklenmedik hata ile durdu.");
}
finally
{
    // 5. Buffer'daki loglarý flush et ve kapat
    Log.CloseAndFlush();
}
```

---

## ?? Seq Kurulumu

### Docker ile (Önerilen)

```bash
docker run -d \
  --name seq \
  -e ACCEPT_EULA=Y \
  -p 5341:5341 \
  -p 8081:80 \
  -v seq-data:/data \
  datalust/seq:latest
```

### `docker-compose.yml`

```yaml
services:
  seq:
    image: datalust/seq:latest
    container_name: seq
    ports:
      - "5341:5341"   # Ingestion (log gönderme)
      - "8081:80"     # Web UI
    environment:
      - ACCEPT_EULA=Y
    volumes:
      - seq-data:/data
    restart: unless-stopped

volumes:
  seq-data:
```

```bash
docker-compose up -d
```

### Eriþim

| URL | Açýklama |
|---|---|
| `http://localhost:5341` | Log ingestion endpoint (API'nin yazdýðý yer) |
| `http://localhost:8081` | Seq Web UI (loglarý görüntüleme) |

---

## ?? Log Seviyeleri

| Seviye | Kullaným Yeri |
|---|---|
| `Verbose` | Çok detaylý debug (kapalý) |
| `Debug` | Development'ta detaylý bilgi |
| `Information` | Normal akýþ (request, command, query) |
| `Warning` | Beklenmedik durum ama hata deðil |
| `Error` | Yakalanan hatalar |
| `Fatal` | Uygulamayý durduran kritik hatalar |

---

## ?? Seq'de Kullanýþlý Filtreler

Seq Web UI (`http://localhost:8081`) üzerinde aþaðýdaki filtreler kullanýlabilir:

```
# Sadece hatalarý gör
@Level = 'Error' or @Level = 'Fatal'

# Belirli bir endpoint'in loglarý
RequestPath like '/api/v1/tasks%'

# Belirli bir kullanýcýnýn iþlemleri
UserId = 'xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx'

# Yavaþ istekler (100ms üzeri)
Elapsed > 100

# Belirli bir Command/Query
RequestName = 'UpdateTaskCommand'

# Sadece veritabaný komutlarý
SourceContext like 'Microsoft.EntityFrameworkCore%'
```

---

## ?? Projedeki Log Noktalarý

| Katman | Sýnýf | Ne Loglanýyor |
|---|---|---|
| API | `Program.cs` | Startup, Fatal hatalar |
| API | `UseSerilogRequestLogging` | Tüm HTTP istekleri (method, path, status, ms) |
| API | `ExceptionHandlingMiddleware` | Yakalanmamýþ exception'lar |
| Application | `LoggingBehavior` | Her MediatR Command/Query baþlangýç ve bitiþ süresi |
| Infrastructure | `IdentitySeeder` | Seed iþlemleri, oluþturulan kullanýcý ID'leri |
| Infrastructure | `TaskSeeder` | Seed iþlemleri |

---

## ?? Önemli Notlar

- **Seq portu (`5341`) açýk olmalý** — uygulama baþlarken Seq'e baðlanamasa bile çalýþmaya devam eder, sadece loglar Seq'e gitmez
- **`Log.CloseAndFlush()`** — uygulama kapanýrken buffer'daki loglarýn yazýlmasý için þarttýr
- **Bootstrap logger** — `builder.Build()` öncesindeki startup hatalarýný yakalar; bu olmadan DI kurulmadan önce oluþan hatalar loglanmaz
- **`HostAbortedException`** — `dotnet watch` ile çalýþýrken hot reload sýrasýnda fýrlatýlan bu exception fatal log olarak kayýt edilmez
