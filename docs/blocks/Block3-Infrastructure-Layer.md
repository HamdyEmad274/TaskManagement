# Block 3 — Infrastructure Layer

> الـ Infrastructure هي الطبقة اللي بتنفذ الـ Interfaces اللي عرّفناها في الـ Domain والـ Application.
> هنا أول ما بنلمس قاعدة البيانات فعلياً — EF Core، SQL Server، Repositories، DI.

---

## الـ Folder Structure المطلوبة — اعملها الأول

```
src/TaskManagement.Infrastructure/
├── Persistence/
│   ├── AppDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   └── TaskItemConfiguration.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   └── TaskRepository.cs
│   └── Seeders/
│       └── DbSeeder.cs
└── Extensions/
    └── InfrastructureServiceExtensions.cs
```

---

## TODO-00 — ⚠️ تعديل مسبق على الـ Domain Interfaces قبل ما تبدأ

الـ `IUserRepository` و `ITaskRepository` في Domain بترجع `Task<User>` — ده غلط.
الصح إنهم يرجعوا `Task<User?>` و `Task<TaskItem?>` لأن الـ `FindAsync` و `FirstOrDefaultAsync` ممكن يرجعوا `null`.

افتح `IUserRepository.cs` وعدّل:

```csharp
Task<User?> GetByIdAsync(Guid id);
Task<User?> GetByEmailAsync(string email);
```

افتح `ITaskRepository.cs` وعدّل:

```csharp
Task<TaskItem?> GetByIdAsync(Guid id);
```

> **ليه ده مهم؟**
> الـ Repository Implementations اللي هتكتبها في TODO-05 و TODO-06 هترجع `Task<User?>` بـ `?`.
> لو الـ Interface مش بترجع نفس الـ Type، الـ Compiler هيقولك إن الـ class مش بتنفّذ الـ Interface صح ويـfail.
> القاعدة: الـ Interface والـ Implementation لازم يكون عندهم نفس الـ Signature بالظبط.
>
> ✅ **التعديل ده اتعمل بالفعل في الـ Source Files — مش محتاج تعمله يدوي.**

---

## TODO-01 — ثبّت الـ NuGet Packages في مشروع Infrastructure

```
Microsoft.EntityFrameworkCore
Microsoft.EntityFrameworkCore.SqlServer
Microsoft.EntityFrameworkCore.Tools
```

> **ليه الـ 3 دول بالتحديد؟**
> - `EntityFrameworkCore` — الـ Core library، فيها الـ DbContext والـ DbSet وكل حاجة
> - `EntityFrameworkCore.SqlServer` — الـ Provider الخاص بـ SQL Server (لو شغّال Postgres هتثبت غيره)
> - `EntityFrameworkCore.Tools` — بتتثبت عشان تقدر تشغّل `dotnet ef migrations add` من الـ CLI
>
> الـ Tools بتتثبت في Infrastructure لأنه هو اللي فيه الـ DbContext — الـ CLI محتاج يلاقيه.

⚠️ **تأكد كمان إن Infrastructure عنده Project References للاتنين دول:**
- Reference لـ `TaskManagement.Domain` — عشان يعرف الـ Entities والـ Interfaces
- Reference لـ `TaskManagement.Application` — عشان `DbSeeder` محتاج `IPasswordHasher`

لو مش موجودين، ضيفهم من Visual Studio بـ Right Click على Infrastructure → Add → Project Reference.

---

## TODO-02 — اعمل `AppDbContext.cs` في `Persistence/`

```csharp
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
```

> **ليه `DbContextOptions<AppDbContext>` وفيها Generic؟**
> الـ Generic بيحدد إن الـ Options دي بتاعت `AppDbContext` تحديداً — مش أي DbContext تاني.
> لو عندك أكتر من DbContext في نفس الـ Project، كل واحد هياخد Options خاصة بيه.
>
> **ليه `DbSet<User>` بـ Generic؟**
> بيقول لـ EF Core: "الـ Property دي بتمثل جدول الـ `User` في الـ DB".
> لو كتبت `DbSet` بدون Generic — مش هيعرف انت بتتكلم عن إيه.
>
> **ليه `ApplyConfigurationsFromAssembly` بدل ما تكتب الـ Config يدوي؟**
> بيشيل تلقائياً كل Class عنده `IEntityTypeConfiguration<T>` في نفس الـ Assembly.
> يعني مش محتاج تضيف `modelBuilder.ApplyConfiguration(new UserConfiguration())` كل مرة تعمل جدول جديد.

---

## TODO-03 — اعمل `UserConfiguration.cs` في `Persistence/Configurations/`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Name).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(200);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.PasswordHash).IsRequired();
        builder.Property(u => u.Role).HasConversion<string>();
    }
}
```

> **ليه `IEntityTypeConfiguration<User>` وفيها Generic؟**
> بتقول لـ EF Core: "الـ Configuration دي بتاعت جدول الـ `User` بالتحديد".
> لو شلت `<User>` — الـ Compiler هيقولك Error لأن الـ Interface Generic مش هيعرف هتشتغل على إيه.
>
> **ليه `EntityTypeBuilder<User>` في الـ parameter؟**
> نفس الفكرة — الـ builder ده متخصص في بناء قواعد الـ `User`. بيديك IntelliSense بيفهم Properties الـ User.
>
> **ليه `HasConversion<string>()` وفيها Generic؟**
> بتقول لـ EF Core: "خزّن الـ Enum ده في الـ DB كـ `string`".
> لو كتبت `HasConversion()` بدون Generic — محتاج تدي Expression يدوي للتحويل.
> `<string>` هو الـ shortcut — يحوّله تلقائياً لنص ويرجعه Enum لما بيقرأ.
>
> **ليه Fluent API هنا بدل Data Annotations (`[Required]`, `[MaxLength]`) على الـ Entity؟**
> لو حطيت `[Required]` على `User.cs` في الـ Domain — الـ Domain بيبقى عارف بـ EF Core.
> ده انتهاك للـ Clean Architecture. الـ Domain المفروض يكون نظيف من أي Library خارجية.
> الـ Configuration Class منفصلة تماماً في Infrastructure = الصح.

---

## TODO-04 — اعمل `TaskItemConfiguration.cs` في `Persistence/Configurations/`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence.Configurations;

public class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Description).HasMaxLength(1000);
        builder.Property(t => t.Status).HasConversion<string>();
        builder.Property(t => t.Priority).HasConversion<string>();
        builder.HasOne(t => t.User)
               .WithMany()
               .HasForeignKey(t => t.UserId)
               .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(t => t.UserId);
        builder.HasIndex(t => new { t.UserId, t.CreatedAt });
    }
}
```

> **ليه Index على `UserId`؟**
> أكتر Query هنعملها: "جيب كل Tasks بتاعة User معين".
> من غير Index — الـ SQL Server بيـfull scan الجدول كله.
> مع Index — بيروح على الـ rows دي مباشرة. الفرق ضخم مع بيانات كتير.
>
> **ليه Index مركب على `{ UserId, CreatedAt }`؟**
> لأن الـ Query غالباً هتكون: جيب Tasks الـ User ده مرتبة بالتاريخ.
> الـ Index المركب بيغطي الاتنين في نفس الوقت = أسرع من Index منفصل لكل واحد.
>
> **ليه `OnDelete(DeleteBehavior.Cascade)`؟**
> لو اتمسح الـ User، كل الـ Tasks بتاعته تتمسح معاه. ده الـ Business Logic المنطقي هنا.
> لو ما حددتهوش، الـ DB هتـblock الـ Delete لو فيه Tasks مرتبطة.

---

## TODO-05 — اعمل `UserRepository.cs` في `Persistence/Repositories/`

```csharp
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces.Repositories;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context) => _context = context;

    public async Task<User?> GetByIdAsync(Guid id) =>
        await _context.Users.FindAsync(id);

    public async Task<User?> GetByEmailAsync(string email) =>
        await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<IEnumerable<User>> GetAllAsync() =>
        await _context.Users.ToListAsync();

    public async Task AddAsync(User user) =>
        await _context.Users.AddAsync(user);

    public Task DeleteAsync(User user)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
}
```

> **ليه `Task<User?>` بعلامة `?`؟**
> الـ `?` معناها Nullable — يعني ممكن يرجع `null` لو الـ User مش موجود.
> لو مكتبتهاش، الـ Compiler هيحذّرك إن الـ method ممكن ترجع null بدون ما تتوقعه.
>
> **ليه `FindAsync` في `GetByIdAsync` وليس `FirstOrDefaultAsync`؟**
> `FindAsync` بتدور في الـ ChangeTracker الأول (الـ memory) قبل ما تروح للـ DB.
> لو الـ Entity اتجابت قبل كده في نفس الـ Request، بترجعها من الـ memory مباشرة. أسرع.
>
> **ليه `DeleteAsync` مفيهاش `await`؟**
> الـ `Remove()` بس بتعلّم الـ Entity كـ "Deleted" في الـ ChangeTracker — مش بيروح للـ DB دلوقتي.
> الـ DB بتتحدث لما `SaveChangesAsync()` يتعمل في الـ Service. الـ Commit بييجي بعدين.

---

## TODO-06 — اعمل `TaskRepository.cs` في `Persistence/Repositories/`

```csharp
using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Interfaces.Repositories;
using TaskManagement.Infrastructure.Persistence;

namespace TaskManagement.Infrastructure.Persistence.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly AppDbContext _context;

    public TaskRepository(AppDbContext context) => _context = context;

    public async Task<TaskItem?> GetByIdAsync(Guid id) =>
        await _context.Tasks.FindAsync(id);

    public async Task<IEnumerable<TaskItem>> GetAllByUserIdAsync(Guid userId) =>
        await _context.Tasks
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(TaskItem task) =>
        await _context.Tasks.AddAsync(task);

    public Task UpdateAsync(TaskItem task)
    {
        _context.Tasks.Update(task);
        return Task.CompletedTask;
    }
}
```

> **ليه `OrderByDescending(t => t.Priority)` بيشتغل صح؟**
> لأن `TaskPriority` Enum عنده قيم `Low=1, Medium=2, High=3`.
> الـ Descending بيرجع الأكبر الأول — يعني `High` هييجي أول.
> ده مش صدفة، ده قرار اتاخد في Block 1 عشان يخدم الـ Sorting هنا بالظبط.
>
> **ليه `UpdateAsync` مفيهاش `await`؟**
> نفس سبب الـ Delete — `Update()` بتعلّم الـ Entity كـ Modified في الـ ChangeTracker.
> الـ SQL UPDATE الحقيقي بييجي مع `SaveChangesAsync()` في الـ Service.

---

## TODO-07 — اعمل `DbSeeder.cs` في `Persistence/Seeders/`

```csharp
using Microsoft.EntityFrameworkCore;
using TaskManagement.Application.Common.Interfaces;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence.Seeders;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
    {
        if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
            return;

        var admin = User.Create(
            name: "Admin",
            email: "admin@taskmanagement.com",
            passwordHash: passwordHasher.Hash("Admin@123"),
            role: UserRole.Admin
        );

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();
    }
}
```

> **ليه `AnyAsync` قبل الـ Seed؟**
> الـ Seeder بيتشغّل كل ما التطبيق يقوم. من غير الـ check، هيعمل Admin جديد كل مرة.
> `AnyAsync` = "هل فيه أي Admin موجود؟" — لو آه، اخرج. لو لأ، اعمل الـ Seed.
>
> **ليه `User.Create(...)` وليس `new User { ... }`؟**
> الـ `User` entity عنده `private set` على كل Properties — ما تقدرش تعمل `new User { Name = "..." }` من بره الكلاس.
> الـ `Create()` static method هي الطريقة الصح — ده اللي هنعمله في TODO-08.
>
> **ليه `passwordHasher.Hash("Admin@123")` وليس الـ Plain Text مباشرة؟**
> ما بنخزنش Passwords في الـ DB كـ Plain Text أبداً. حتى في الـ Seed.

---

## TODO-08 — ضيف `Create()` Factory Method في `User` و `TaskItem`

افتح `User.cs` في Domain وعدّله:

```csharp
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class User : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }

    private User() { }

    public static User Create(string name, string email, string passwordHash, UserRole role)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Name = name,
            Email = email,
            PasswordHash = passwordHash,
            Role = role
        };
    }
}
```

افتح `TaskItem.cs` وعدّله:

```csharp
using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public AppTaskStatus Status { get; private set; }
    public TaskPriority Priority { get; private set; }
    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    private TaskItem() { }

    public static TaskItem Create(string title, string description, TaskPriority priority, Guid userId)
    {
        return new TaskItem
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTime.UtcNow,
            Title = title,
            Description = description,
            Status = AppTaskStatus.Pending,
            Priority = priority,
            UserId = userId
        };
    }

    public void UpdateStatus(AppTaskStatus newStatus)
    {
        Status = newStatus;
    }
}
```

> **ليه `private User() { }` ضروري؟**
> الـ EF Core لما بيقرأ من الـ DB بيعمل Instance من الـ Entity باستخدام Parameterless Constructor.
> لو مش موجود، هيـthrow Exception.
> بنخليه `private` عشان ما حدش يقدر يعمل `new User()` فاضي من بره الكلاس.
>
> **ليه `Id` و `CreatedAt` متحطوا جوه `Create()` وليس في الـ DB؟**
> بنولّد الـ `Id` في الـ Application بنفسنا بدل ما SQL Server يولّده.
> ليه؟ عشان نعرف الـ Id قبل ما الـ Record يتحفظ — مفيد في الـ Logging والـ Queue.
>
> **ليه `UpdateStatus()` method في `TaskItem`؟**
> بدل ما أي حاجة من بره تعمل `task.Status = AppTaskStatus.Done` مباشرة (setter عام) —
> بنمرر الـ Update من خلال method واضحة بتوضح النية، وممكن نضيف validation جوها مستقبلاً.
>
> **ليه `User User { get; private set; } = null!;`؟**
> الـ `null!` بتقول للـ Compiler: "عارف إنها null دلوقتي، بس EF Core هيملّيها". بتمنع الـ Nullable Warning.

---

## TODO-09 — اعمل أول EF Core Migration

قبل ما تشغّل الأمر، تأكد إن:
- الـ Infrastructure project عنده Reference للـ Domain project
- الـ API project عنده Reference للـ Infrastructure project
- الـ `appsettings.json` في الـ API فيه Connection String:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=TaskManagementDB;Trusted_Connection=True;TrustServerCertificate=True"
}
```

بعدين شغّل:

```bash
dotnet ef migrations add InitialCreate --project src/TaskManagement.Infrastructure --startup-project src/TaskManagement.API
```

بعد ما يتعمل، **افتح ملف الـ Migration في `Infrastructure/Migrations/` واقرأه**.
تأكد من:
- الجداول اسمها `"Users"` و `"Tasks"` (مش `"User"` و `"TaskItem"`)
- الـ `nvarchar` sizes صح (100، 200، 1000)
- الـ `Role`, `Status`, `Priority` نوعها `nvarchar` (لأن `HasConversion<string>()`)
- الـ Indexes موجودة في `migrationBuilder.CreateIndex(...)`
- الـ Foreign Key موجود بين `Tasks.UserId` و `Users.Id`

> **ليه نقرأ الـ Migration؟**
> الـ Migration هي ترجمة حرفية للـ Configuration اللي كتبتها.
> أي غلطة في الـ Fluent API هتظهر هنا قبل ما توصل للـ DB.
> Senior Developer بيقرأ الـ Migration — مش بس بيشغّلها.

---

## TODO-10 — طبّق الـ Migration على قاعدة البيانات

```bash
dotnet ef database update --project src/TaskManagement.Infrastructure --startup-project src/TaskManagement.API
```

بعد ما يخلص، افتح SSMS أو Azure Data Studio وشوف:
- جدول `Users` بالـ Columns والـ Indexes
- جدول `Tasks` بالـ Columns، الـ Foreign Key، والـ Indexes
- جدول `__EFMigrationsHistory` اللي EF Core بيخزن فيه الـ Migrations اللي اتطبقت

> الجدول `__EFMigrationsHistory` ده بيضمن إن نفس الـ Migration ما تتطبقش مرتين.
> لو شغّلت `database update` تاني، EF Core هيشوف إن `InitialCreate` موجود فيه ومش هيعمل حاجة.

---

## TODO-11 — اعمل `InfrastructureServiceExtensions.cs` في `Extensions/`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskManagement.Domain.Interfaces.Repositories;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories;

namespace TaskManagement.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ITaskRepository, TaskRepository>();

        return services;
    }
}
```

ثم في `Program.cs` في الـ API:

```csharp
builder.Services.AddInfrastructure(builder.Configuration);
```

> **ليه `AddDbContext<AppDbContext>` بـ Generic؟**
> بتسجّل `AppDbContext` تحديداً في الـ DI Container.
> لو شلت `<AppDbContext>` — الـ method مش هتعرف تسجّل إيه.
>
> **ليه `AddScoped<IUserRepository, UserRepository>` بـ 2 Generics؟**
> الأول `IUserRepository` هو الـ Interface — ده اللي الـ Services هتطلبه.
> التاني `UserRepository` هو الـ Implementation — ده اللي الـ DI هيديه فعلياً.
> يعني: "لما حاجة تطلب `IUserRepository`، ادّيها `UserRepository`".
>
> **ليه `AddScoped` مش `AddSingleton`؟**
> الـ DbContext مش Thread-Safe — ما ينفعش يتشارك بين Requests.
> `Scoped` = Instance واحدة طول الـ HTTP Request، بتتمسح لما الـ Request يخلص.
> `Singleton` = Instance واحدة طول عمر الـ Application — ده خطر مع الـ DbContext.
