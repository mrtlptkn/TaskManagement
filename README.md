# Task Management API

.NET 8 ile geliştirilmiş, **Clean Architecture** + **Hexagonal Architecture (Ports & Adapters)**
prensiplerine uygun, **Mediator (CQRS)** tabanlı, **model güdümlü** bir görev yönetimi REST API'si.

Bu proje eğitim amaçlıdır: SOLID prensipleri, IoC/Dependency Injection, coupling/cohesion,
Resource Based Authorization ve test stratejilerini (unit + integration) somut bir
kod tabanı üzerinden incelemek için tasarlanmıştır.

---

## 1. Mimari Genel Bakış

```
src/
├── TaskManagement.Domain          -> Entity'ler, Enum'lar, Domain Exception'lar, is kurallari
│                                     (Hicbir dis katmana bagimli degil)
│
├── TaskManagement.Application      -> Use Case'ler (Command/Query + Handler), Port'lar (interface),
│                                     Validation, Mapping, Pipeline Behaviors
│                                     (Sadece Domain'e bagimli)
│
├── TaskManagement.Infrastructure   -> EF Core DbContext, Repository'ler, Identity, JWT, Authorization
│                                     (Application'daki Port'lari implement eder - Adapter)
│
└── TaskManagement.API              -> Controller'lar, Middleware, Program.cs
                                      (Application + Infrastructure'i bir araya getirir)

tests/
├── TaskManagement.Domain.UnitTests        -> TaskItem entity is kurallari
├── TaskManagement.Application.UnitTests   -> Handler'lar (Moq ile mock'lanmis port'lar)
└── TaskManagement.API.IntegrationTests    -> Uctan uca HTTP testleri (WebApplicationFactory + InMemory DB)
```

### Bagimlilik yonu (Dependency Rule)

```
API --> Application <-- Infrastructure
         ^
         |
       Domain
```

- **Domain**: Hicbir seye bagimli degil. `TaskItem` aggregate root'u tum durum gecisi ve
  yetkilendirme kurallarini kendi icinde barindirir (Anemic Domain Model'den kacinilmistir).
- **Application**: Sadece Domain'e bagimli. `Abstractions/` klasorundeki interface'ler
  (port'lar) burada tanimlanir; implementasyonlari Infrastructure'dadir.
- **Infrastructure**: Application'daki port'lari EF Core, ASP.NET Identity, JWT ile implement eder.
- **API**: Sadece `AddApplication()` ve `AddInfrastructure()` extension'larini cagirir;
  ic detaylari bilmez.

Bu yapi **dusuk coupling, yuksek cohesion** saglar: Infrastructure'i (ornegin SQL Server'dan
PostgreSQL'e, veya local disk storage'dan Azure Blob'a) degistirmek Application/Domain
katmanlarini etkilemez.

---

## 2. Domain Modeli

### TaskItem (Aggregate Root)

| Alan | Aciklama |
|---|---|
| `Title`, `Description` | Gorev bilgileri |
| `Priority` | `Low \| Medium \| High \| Urgent` |
| `Status` | `Todo \| InProgress \| InReview \| Completed` |
| `DeadLine` | Son tarih |
| `CreatedByUserId` | Gorevi olusturan (yetkili) kullanici |
| `AssignedToUserId` | Gorevin atandigi calisan |
| `TodoItems` | Checkbox'li "Yapilacaklar" listesi |
| `Attachments` | Dosya ekleri / dokumantasyon |
| `ActivityLogs` | Gorevle ilgili kronolojik akis (timeline) |

### Durum Akisi (State Machine)

```
Todo --(assignee: StartProgress)--> InProgress --(assignee: SubmitForReview)--> InReview
                                          ^                                         |
                                          |                                         |
                                          +--------(creator: RejectReview)---------+
                                                                                     |
                                                                  (creator: Approve) v
                                                                              Completed
```

Tum Todo ogeleri isaretlenirse ve gorev `InProgress` durumundaysa, sistem otomatik
olarak `InReview` durumuna gecer.

### Resource Based Authorization

| Islem | Yetkili |
|---|---|
| `Update` (gorev bilgilerini guncelleme) | Sadece `CreatedByUserId` |
| `StartProgress`, `SubmitForReview`, `ToggleTodoItem` | Sadece `AssignedToUserId` |
| `Approve`, `RejectReview` | Sadece `CreatedByUserId` |

Bu kurallar **Domain katmaninda** (`TaskItem` metodlari icinde) uygulanir ve
`UnauthorizedTaskOperationException` firlatilir. API katmanindaki
`ExceptionHandlingMiddleware` bu exception'i **403 Forbidden**'a cevirir.

---

## 3. Identity & Authentication

- `AppUser : IdentityUser<Guid>`, `AppRole : IdentityRole<Guid>`
- ASP.NET Core Identity + JWT Bearer authentication
- Roller: `Manager`, `Employee`

### Demo kullanicilar (seed data)

| Email | Sifre | Rol |
|---|---|---|
| `manager@taskmanagement.com` | `Manager123!` | Manager |
| `employee@taskmanagement.com` | `Employee123!` | Employee |

---

## 4. API Endpoint'leri

### Auth
| Method | Endpoint | Aciklama |
|---|---|---|
| POST | `/api/v1/auth/register` | Kullanici kaydi |
| POST | `/api/v1/auth/login` | Giris, JWT token doner |

### Users
| Method | Endpoint | Aciklama |
|---|---|---|
| GET | `/api/v1/users` | Atama formu icin kullanici listesi |

### Tasks
| Method | Endpoint | Aciklama |
|---|---|---|
| POST | `/api/v1/tasks` | Gorev olustur |
| GET | `/api/v1/tasks` | Filtreli/sayfali listele (asagiya bakiniz) |
| GET | `/api/v1/tasks/overdue` | Gecikmis gorevler |
| GET | `/api/v1/tasks/{id}` | Gorev detayi |
| PUT | `/api/v1/tasks/{id}` | Gorev guncelle (sadece creator) |
| PATCH | `/api/v1/tasks/{id}/status` | Durum gecisi (`StartProgress`/`SubmitForReview`/`Approve`/`RejectReview`) |
| PATCH | `/api/v1/tasks/{id}/todos/{todoId}` | Todo checkbox durumu degistir |
| POST | `/api/v1/tasks/{id}/comments` | Akis/yorum ekle |
| POST | `/api/v1/tasks/{id}/attachments` | Dosya eki yukle (multipart/form-data) |

### Filtreleme Parametreleri (`GET /api/v1/tasks`)

| Parametre | Tip | Aciklama |
|---|---|---|
| `priority` | `Low\|Medium\|High\|Urgent` | Onceliğe gore filtrele |
| `status` | `Todo\|InProgress\|InReview\|Completed` | Duruma gore filtrele |
| `assignedToUserId` | `guid` | Atanan kullaniciya gore filtrele |
| `createdByUserId` | `guid` | Olusturan kullaniciya gore filtrele |
| `isOverdue` | `bool` | Sadece gecikmis gorevler |
| `dueWithinWeek` | `bool` | Son tarihi onumuzdeki 7 gun icinde olanlar |
| `pageNumber`, `pageSize` | `int` | Sayfalama |

Ornek:
```
GET /api/v1/tasks?priority=Urgent&isOverdue=true&pageNumber=1&pageSize=20
```

---

## 5. Calistirma

### Gereksinimler
- .NET 8 SDK
- SQL Server (LocalDB yeterlidir) veya `appsettings.json`'daki connection string'i degistirin

### Adimlar

```bash
cd src/TaskManagement.API

# Ilk migration'i olustur (eger Migrations klasoru bossa)
dotnet ef migrations add InitialCreate --project ../TaskManagement.Infrastructure --startup-project .

# Calistir (Program.cs icinde otomatik migration + seed yapilir)
dotnet run
```

Swagger UI: `https://localhost:5081/swagger`

### appsettings.json - JWT ayarlari

Production'a almadan once `JwtSettings:SecretKey` degerini **en az 32 karakterlik**,
guvenli rastgele bir degerle degistirin.

---

## 6. Testleri Calistirma

```bash
# Tum testler
dotnet test

# Sadece Domain unit testleri
dotnet test tests/TaskManagement.Domain.UnitTests

# Sadece Application unit testleri (Moq ile mock'lanmis handler testleri)
dotnet test tests/TaskManagement.Application.UnitTests

# Sadece Integration testleri (WebApplicationFactory + EF Core InMemory)
dotnet test tests/TaskManagement.API.IntegrationTests
```

### Test stratejisi ozeti

| Katman | Yaklasim | Ornek |
|---|---|---|
| Domain | Saf unit test, mock yok | `TaskItem.Approve()` sadece creator tarafindan cagrilabilir mi? |
| Application | Handler + Moq ile mock'lanmis port'lar | `UpdateTaskCommandHandler`, creator olmayan kullanici icin `UnauthorizedTaskOperationException` firlatiyor mu? |
| API | `WebApplicationFactory` ile uctan uca HTTP | Employee, Manager'in olusturdugu gorevi guncellemeye calisirsa `403 Forbidden` donuyor mu? |

---

## 7. SOLID & Tasarim Desenleri Referans Noktalari

| Prensip / Desen | Nerede |
|---|---|
| **Single Responsibility** | Her CQRS handler tek bir use case'i yonetir |
| **Open/Closed** | Yeni bir filtre eklemek icin `TaskFilterSpecification`'a yeni bir kriter eklenir, mevcut kod degismez |
| **Liskov Substitution** | `IFileStorageService` -> `LocalFileStorageService` baska bir storage adapter'i ile sorunsuz degistirilebilir |
| **Interface Segregation** | `ITaskRepository`, `IUnitOfWork`, `ICurrentUserService` ayri ayri kucuk interface'ler |
| **Dependency Inversion** | Application, Infrastructure'a degil; Infrastructure, Application'daki port'lara bagimli |
| **Mediator (CQRS)** | MediatR ile her Command/Query kendi handler'inda islenir |
| **Specification Pattern** | `TaskFilterSpecification` ile filtreleme mantigi Application'da, EF'e sizmadan tanimlanir |
| **Repository + Unit of Work** | `ITaskRepository` + `IUnitOfWork` |
| **Pipeline Behavior (Decorator)** | `ValidationBehavior`, `LoggingBehavior` |
| **Resource Based Authorization** | `TaskItem` domain metodlarinda + `TaskOwnerAuthorizationHandler` |
