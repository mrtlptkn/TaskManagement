using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Seed;

/// <summary>
/// Veritabanýna demo görevleri (tasks) ekler.
/// 50 adet farklý öncelik, durum ve todo öðelerine sahip görev oluþturur.
/// </summary>
public static class TaskSeeder
{
    private static readonly Random _random = new();

    private static readonly string[] _taskTitles = new[]
    {
        "API endpoint'lerini test et",
        "Veritabaný migration'ýný hazýrla",
        "Docker container'ýný ayarla",
        "CI/CD pipeline'ýný kur",
        "Kullanýcý arayüzünü güncelle",
        "Performans optimizasyonu yap",
        "Güvenlik açýklarýný kontrol et",
        "Unit testleri yaz",
        "Integration testleri ekle",
        "Dokümantasyonu tamamla",
        "Code review yap",
        "Bug fix: Login problemi",
        "Feature: Bildirim sistemi",
        "Refactoring: Service katmaný",
        "Database backup stratejisi oluþtur",
        "Monitoring sistemi kur",
        "E-posta þablonlarýný hazýrla",
        "Raporlama modülünü geliþtir",
        "Önbellekleme mekanizmasý ekle",
        "API versiyonlama sistemi",
        "Logging mekanizmasýný iyileþtir",
        "Authentication flow'unu güncelle",
        "Authorization kurallarýný gözden geçir",
        "Third-party entegrasyonlarý test et",
        "Mobile uygulama için API hazýrla",
        "WebSocket desteði ekle",
        "File upload özelliðini optimize et",
        "Search functionality ekle",
        "Export özelliði (Excel/PDF)",
        "Dashboard widget'larý oluþtur",
        "Kullanýcý profil sayfasý",
        "Admin panel geliþtir",
        "Multilanguage desteði ekle",
        "Dark mode implementasyonu",
        "Responsive tasarým iyileþtirmeleri",
        "SEO optimizasyonlarý",
        "Analytics entegrasyonu",
        "Payment gateway entegrasyonu",
        "Email notification sistemi",
        "SMS bildirimleri ekle",
        "Two-factor authentication",
        "Password reset akýþý",
        "Social media login",
        "API rate limiting",
        "Database indexing stratejisi",
        "Memory leak araþtýrmasý",
        "Load testing yap",
        "Security audit raporu hazýrla",
        "GDPR uyumluluk kontrolü",
        "Backup restore testi"
    };

    private static readonly string[] _taskDescriptions = new[]
    {
        "Bu görev yüksek öncelikli olup, hýzlý bir þekilde tamamlanmasý gerekmektedir.",
        "Detaylý analiz sonrasý gerekli deðiþikliklerin yapýlmasý planlanmaktadýr.",
        "Mevcut sistemin güvenliðini artýrmak için kritik bir görevdir.",
        "Kullanýcý deneyimini iyileþtirmek amacýyla tasarlanmýþtýr.",
        "Performans metrikleri takip edilerek optimize edilmelidir.",
        "Ekip ile koordineli çalýþýlmasý gereken kapsamlý bir görevdir.",
        "Test senaryolarýnýn eksiksiz hazýrlanmasý beklenmektedir.",
        "Dokümantasyon güncel tutulmalý ve detaylý olmalýdýr.",
        "Code quality standartlarýna uygun geliþtirilmelidir.",
        "Sprint hedeflerine ulaþmak için kritik öneme sahiptir.",
        "Stakeholder beklentilerini karþýlamak için önemlidir.",
        "Technical debt azaltmaya yönelik bir görevdir.",
        "Scalability için gerekli altyapý çalýþmasýdýr.",
        "Production ortamý için hazýrlýk sürecidir.",
        "Monitoring ve alerting mekanizmalarýný içermektedir."
    };

    private static readonly string[] _todoTemplates = new[]
    {
        "Gereksinim analizi yap",
        "Teknik tasarým dokümaný hazýrla",
        "Database schema deðiþikliklerini planla",
        "API endpoint'lerini tasarla",
        "Frontend component'lerini oluþtur",
        "Unit testleri yaz",
        "Integration testleri ekle",
        "Code review yaptýr",
        "Dokümantasyonu güncelle",
        "Production'a deploy et",
        "Performance testi yap",
        "Security scan çalýþtýr",
        "Stakeholder onayý al",
        "User acceptance test yap",
        "Monitoring dashboard'unu kontrol et",
        "Rollback planý hazýrla",
        "Database migration scriptlerini hazýrla",
        "Configuration ayarlarýný yap",
        "Log mekanizmasýný test et",
        "Error handling'i kontrol et"
    };

    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();
        var logger = serviceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        // Eðer zaten task varsa seed yapma
        if (await context.Tasks.AnyAsync())
        {
            logger.LogInformation("Veritabanýnda zaten görevler mevcut. Task seed iþlemi atlanýyor.");
            return;
        }

        // Kullanýcýlarý al
        var users = await userManager.Users.ToListAsync();

        if (users.Count < 2)
        {
            logger.LogWarning("Görev oluþturmak için en az 2 kullanýcý gerekli. Task seed iþlemi atlanýyor.");
            return;
        }

        var managers = new List<AppUser>();
        var employees = new List<AppUser>();

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains(Roles.Manager))
                managers.Add(user);
            else if (roles.Contains(Roles.Employee))
                employees.Add(user);
        }

        // Eðer manager veya employee yoksa, tüm kullanýcýlarý her iki listede kullan
        if (managers.Count == 0) managers = users;
        if (employees.Count == 0) employees = users;

        logger.LogInformation("Task seed baþlatýlýyor. {ManagerCount} manager, {EmployeeCount} employee bulundu.",
            managers.Count, employees.Count);

        var tasks = new List<TaskItem>();

        // 50 adet task oluþtur
        for (int i = 0; i < 50; i++)
        {
            var manager = managers[_random.Next(managers.Count)];
            var employee = employees[_random.Next(employees.Count)];

            var priority = GetRandomPriority();
            var status = GetRandomStatus();
            var title = _taskTitles[i % _taskTitles.Length];
            var description = _taskDescriptions[_random.Next(_taskDescriptions.Length)];

            // Geçmiþ veya gelecek tarih oluþtur
            var daysOffset = _random.Next(-30, 60); // -30 ile +60 gün arasý
            var deadline = DateTime.UtcNow.AddDays(daysOffset);

            // Eðer tarih geçmiþse ve status Completed deðilse, tarihi güncelle
            if (deadline < DateTime.UtcNow && status != TaskStatusEnum.Completed)
            {
                deadline = DateTime.UtcNow.AddDays(_random.Next(1, 30));
            }

            try
            {
                var task = TaskItem.Create(
                    title: $"{title} #{i + 1}",
                    description: description,
                    priority: priority,
                    deadLine: deadline,
                    createdByUserId: manager.Id,
                    assignedToUserId: employee.Id
                );

                // Rastgele sayýda todo item ekle (2-6 arasý)
                var todoCount = _random.Next(2, 7);
                var selectedTodos = _todoTemplates
                    .OrderBy(x => _random.Next())
                    .Take(todoCount)
                    .ToList();

                foreach (var todoTitle in selectedTodos)
                {
                    task.AddTodoItem(todoTitle, employee.Id);
                }

                // Status'e göre iþlemler yap
                if (status == TaskStatusEnum.InProgress || status == TaskStatusEnum.InReview || status == TaskStatusEnum.Completed)
                {
                    try
                    {
                        task.StartProgress(employee.Id);
                    }
                    catch { /* Domain kuralý ihlali göz ardý edilir */ }
                }

                if (status == TaskStatusEnum.InReview || status == TaskStatusEnum.Completed)
                {
                    // Bazý todo'larý tamamla
                    var todosToComplete = _random.Next(todoCount / 2, todoCount + 1);
                    for (int j = 0; j < todosToComplete && j < task.TodoItems.Count; j++)
                    {
                        try
                        {
                            task.ToggleTodoItem(task.TodoItems.ElementAt(j).Id, true, employee.Id);
                        }
                        catch { /* Todo toggle hatasý göz ardý edilir */ }
                    }

                    try
                    {
                        task.SubmitForReview(employee.Id);
                    }
                    catch { /* Domain kuralý ihlali göz ardý edilir */ }
                }

                if (status == TaskStatusEnum.Completed)
                {
                    try
                    {
                        task.Approve(manager.Id);
                    }
                    catch { /* Domain kuralý ihlali göz ardý edilir */ }
                }

                tasks.Add(task);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Task #{Index} oluþturulurken hata oluþtu.", i + 1);
            }
        }

        await context.Tasks.AddRangeAsync(tasks);
        await context.SaveChangesAsync();

        logger.LogInformation("{Count} adet demo görev baþarýyla oluþturuldu.", tasks.Count);
    }

    private static TaskPriority GetRandomPriority()
    {
        var priorities = Enum.GetValues<TaskPriority>();
        return priorities[_random.Next(priorities.Length)];
    }

    private static TaskStatusEnum GetRandomStatus()
    {
        var statuses = Enum.GetValues<TaskStatusEnum>();
        // Status daðýlýmýný biraz daha gerçekçi yap
        var randomValue = _random.Next(100);

        if (randomValue < 30) return TaskStatusEnum.Todo;           // %30
        if (randomValue < 60) return TaskStatusEnum.InProgress;     // %30
        if (randomValue < 85) return TaskStatusEnum.InReview;       // %25
        return TaskStatusEnum.Completed;                             // %15
    }
}
