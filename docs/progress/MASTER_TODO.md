# Master TODO Tracker

> Legend: ✅ Done | 🔄 In Progress | ⬜ Not Started

---

## Block 1 — Solution Setup + Domain Layer ✅ COMPLETE

- [x] TODO-01 — Create blank solution
- [x] TODO-02 — Create 4 projects: Domain, Application, Infrastructure, API
- [x] TODO-03 — Project references بالاتجاه الصح
- [x] TODO-04 — Domain folder structure
- [x] TODO-05 — `BaseEntity.cs` — Id (Guid) + CreatedAt (DateTime) بـ private set
- [x] TODO-06 — `User.cs`
- [x] TODO-07 — `TaskItem.cs`
- [x] TODO-08 — `UserRole.cs` enum
- [x] TODO-09 — `AppTaskStatus.cs` enum
- [x] TODO-10 — `TaskPriority.cs` enum
- [x] TODO-11 — `IUserRepository.cs` — fully async
- [x] TODO-12 — `ITaskRepository.cs` — fully async, returns TaskItem
- [x] TODO-13 — `DomainException.cs` — inherits Exception

---

## Block 2 — Application Layer 🔄 CURRENT

> الـ Application Layer فيها الـ Use Cases والـ Contracts بس — مش فيها أي Implementation.
> كل حاجة هنكتبها هنا هي Interfaces وDTOs فقط. مفيش EF Core، مفيش BCrypt، مفيش أي Library.

### الـ Folder Structure المطلوبة — اعملها الأول

```
Application/
├── Common/
│   └── Interfaces/
│       ├── IPasswordHasher.cs
│       └── IJwtTokenGenerator.cs
├── Users/
│   ├── DTOs/
│   │   ├── RegisterUserRequest.cs
│   │   ├── LoginRequest.cs
│   │   └── UserResponse.cs
│   └── Interfaces/
│       └── IUserService.cs
└── Tasks/
    ├── DTOs/
    │   ├── CreateTaskRequest.cs
    │   ├── UpdateTaskStatusRequest.cs
    │   └── TaskResponse.cs
    └── Interfaces/
        └── ITaskService.cs
```

---

- [ ] **TODO-01** — اعمل `IPasswordHasher.cs` في `Common/Interfaces/`

  ```csharp
  namespace TaskManagement.Application.Common.Interfaces;

  public interface IPasswordHasher
  {
      string Hash(string password);
      bool Verify(string password, string hash);
  }
  ```

  > **ليه موجود هنا في Application؟**
  > الـ `UserService` (اللي هنكتبه في Block 5) محتاج يعمل Hash للـ Password وقت الـ Register.
  > لو حطينا الـ Interface في Infrastructure، الـ Application هتحتاج Reference للـ Infrastructure — وده عكس الاتجاه الصح.
  > الحل: Application بتعرّف الـ Contract هنا، Infrastructure بتنفّذه في Block 4.

---

- [ ] **TODO-02** — اعمل `IJwtTokenGenerator.cs` في `Common/Interfaces/`

  ```csharp
  using TaskManagement.Domain.Entities;

  namespace TaskManagement.Application.Common.Interfaces;

  public interface IJwtTokenGenerator
  {
      string GenerateToken(User user);
  }
  ```

  > **ليه بياخد `User` Entity مش بياخد Id وEmail منفصلين؟**
  > لأن الـ Token بيحتاج بيانات متعددة من الـ User (Id, Email, Role).
  > بدل ما تمرر 3 parameters، بتمرر الـ Object كله. لو احتجت تضيف Claim جديد مستقبلاً، ما هتغيرش الـ Signature.

---

- [ ] **TODO-03** — اعمل `RegisterUserRequest.cs` في `Users/DTOs/`

  ```csharp
  namespace TaskManagement.Application.Users.DTOs;

  public class RegisterUserRequest
  {
      public string Name { get; set; } = string.Empty;
      public string Email { get; set; } = string.Empty;
      public string Password { get; set; } = string.Empty;
  }
  ```

  > **ليه `Password` مش `PasswordHash`؟**
  > الـ Request ده بييجي من الـ Client — الـ Client بيبعت الـ Password كـ Plain Text.
  > الـ Hashing بييحصل في الـ Service Layer، مش في الـ DTO.
  > الـ DTO بيمثل شكل الـ Input اللي جاي من بره — مش الشكل الداخلي.

---

- [ ] **TODO-04** — اعمل `LoginRequest.cs` في `Users/DTOs/`

  ```csharp
  namespace TaskManagement.Application.Users.DTOs;

  public class LoginRequest
  {
      public string Email { get; set; } = string.Empty;
      public string Password { get; set; } = string.Empty;
  }
  ```

  > بسيطة — Email وPassword بس. النتيجة هتكون JWT Token String مش User Object.

---

- [ ] **TODO-05** — اعمل `UserResponse.cs` في `Users/DTOs/`

  ```csharp
  namespace TaskManagement.Application.Users.DTOs;

  public class UserResponse
  {
      public Guid Id { get; set; }
      public string Name { get; set; } = string.Empty;
      public string Email { get; set; } = string.Empty;
      public string Role { get; set; } = string.Empty;
      public DateTime CreatedAt { get; set; }
  }
  ```

  > **ليه `Role` بقى `string` مش `UserRole` Enum؟**
  > الـ API بترجع `"Admin"` أو `"User"` — مش الرقم `1` أو `2`.
  > الـ Client (موبايل، فرونت إند) محتاج نص قابل للقراءة مش integer.
  > التحويل من `UserRole.Admin` → `"Admin"` هيتعمل في الـ Service لما نعمل الـ Mapping.
  >
  > **لاحظ:** مفيش `PasswordHash` هنا — ده أهم حاجة. ما بترجعش بيانات حساسة أبداً.

---

- [ ] **TODO-06** — اعمل `IUserService.cs` في `Users/Interfaces/`

  ```csharp
  using TaskManagement.Application.Users.DTOs;

  namespace TaskManagement.Application.Users.Interfaces;

  public interface IUserService
  {
      Task<UserResponse> RegisterAsync(RegisterUserRequest request);
      Task<string> LoginAsync(LoginRequest request);
      Task<UserResponse> GetByIdAsync(Guid id);
      Task<IEnumerable<UserResponse>> GetAllAsync();
      Task DeleteAsync(Guid id);
  }
  ```

  > **ليه `LoginAsync` بترجع `string` مش `UserResponse`؟**
  > نتيجة الـ Login هي JWT Token — string بس. الـ Client هياخده ويبعته في كل Request جاي.
  > مفيش داعي لـ Object كامل هنا.
  >
  > **ليه `RegisterAsync` موجودة هنا وفي نفس الوقت في الـ Admin Controller؟**
  > الـ Admin بيستخدم نفس الـ `RegisterAsync` — الفرق في الـ Authorization مش في الـ Service.
  > الـ Controller هو اللي بيقرر مين يقدر يناديها.

---

- [ ] **TODO-07** — اعمل `CreateTaskRequest.cs` في `Tasks/DTOs/`

  ```csharp
  using TaskManagement.Domain.Enums;

  namespace TaskManagement.Application.Tasks.DTOs;

  public class CreateTaskRequest
  {
      public string Title { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public TaskPriority Priority { get; set; }
  }
  ```

  > **ليه `Priority` بقى Enum هنا (مش string زي الـ Response)؟**
  > الـ Client لما بيبعت Request بيبعت قيمة محددة — إما الـ Enum Value (`1`, `2`, `3`) أو الـ String (`"High"`).
  > ASP.NET Core بيعرف يعمل Deserialize من الاتنين تلقائياً.
  > في الـ Response بنحوّله لـ string عشان ما يرجعش integer للـ Client. في الـ Request قبول الاتنين مناسب.
  >
  > **لاحظ:** مفيش `UserId` هنا — ليه؟
  > لأن الـ `UserId` هيجي من الـ JWT Token، مش من الـ Client.
  > لو خلّيت الـ Client يبعت الـ `UserId`، ممكن يبعت UserId حد تاني ويعمل Tasks باسمه!

---

- [ ] **TODO-08** — اعمل `UpdateTaskStatusRequest.cs` في `Tasks/DTOs/`

  ```csharp
  using TaskManagement.Domain.Enums;

  namespace TaskManagement.Application.Tasks.DTOs;

  public class UpdateTaskStatusRequest
  {
      public AppTaskStatus Status { get; set; }
  }
  ```

  > Field واحد بس — بنحدث الـ Status فقط. ده مبدأ الـ `PATCH` في REST:
  > بتبعت بس اللي عايز تغيره، مش الـ Object كامل.

---

- [ ] **TODO-09** — اعمل `TaskResponse.cs` في `Tasks/DTOs/`

  ```csharp
  namespace TaskManagement.Application.Tasks.DTOs;

  public class TaskResponse
  {
      public Guid Id { get; set; }
      public string Title { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public string Status { get; set; } = string.Empty;
      public string Priority { get; set; } = string.Empty;
      public DateTime CreatedAt { get; set; }
      public Guid UserId { get; set; }
  }
  ```

  > `Status` و `Priority` كـ `string` — نفس السبب زي الـ `UserResponse`.
  > الـ Client هيشوف `"High"` و `"Pending"` — مش `3` و `1`.

---

- [ ] **TODO-10** — اعمل `ITaskService.cs` في `Tasks/Interfaces/`

  ```csharp
  using TaskManagement.Application.Tasks.DTOs;

  namespace TaskManagement.Application.Tasks.Interfaces;

  public interface ITaskService
  {
      Task<TaskResponse> CreateAsync(CreateTaskRequest request, Guid userId);
      Task<TaskResponse> GetByIdAsync(Guid id, Guid userId);
      Task<IEnumerable<TaskResponse>> GetAllByUserAsync(Guid userId);
      Task<TaskResponse> UpdateStatusAsync(Guid id, UpdateTaskStatusRequest request, Guid userId);
  }
  ```

  > **ليه كل method فيها `Guid userId`؟**
  > كل عملية على Task محتاجة تعرف مين اللي بيعملها عشان تطبق الـ Ownership Rule:
  > - `GetByIdAsync` → تتحقق إن الـ Task بتاعة الـ User ده
  > - `CreateAsync` → تحط الـ UserId على الـ Task الجديدة
  > - `UpdateStatusAsync` → تتحقق قبل ما تغير أي حاجة
  >
  > الـ `userId` بييجي من الـ JWT Token في الـ Controller — مش من الـ Client مباشرة.

---

## Block 3 — Infrastructure Layer ⬜ NOT STARTED

> هنا بنبدأ نلمس قاعدة البيانات لأول مرة. الـ Infrastructure هي الطبقة اللي بتنفذ الـ Interfaces اللي عرّفناها في الـ Domain والـ Application.

### الـ Folder Structure المطلوبة

```
Infrastructure/
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

- [ ] **TODO-01** — ثبّت الـ NuGet Packages الجاية في مشروع Infrastructure

  ```
  Microsoft.EntityFrameworkCore
  Microsoft.EntityFrameworkCore.SqlServer
  Microsoft.EntityFrameworkCore.Tools
  ```

  > **ليه؟** الـ EF Core هو الـ ORM اللي هيتكلم مع SQL Server. الـ Tools محتاجها عشان تعمل Migrations من الـ CLI.
  > الـ Tools بتتثبت في Infrastructure لأنه هو اللي فيه الـ DbContext.

---

- [ ] **TODO-02** — اعمل `AppDbContext.cs` في `Persistence/`

  ```csharp
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

  > **ليه `ApplyConfigurationsFromAssembly`؟** بدل ما تكتب `modelBuilder.Entity<User>()...` في الـ DbContext وتعمله ضخم، بيشيل كل الـ Configuration Classes تلقائياً من نفس الـ Assembly. كل جدول عنده Class لوحده.

---

- [ ] **TODO-03** — اعمل `UserConfiguration.cs` في `Persistence/Configurations/`

  ```csharp
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

  > **ليه `IEntityTypeConfiguration<T>` بدل Data Annotations؟**
  > الـ Data Annotations (`[Required]`, `[MaxLength]`) بتدخل في الـ Entity — يعني الـ Domain بيبقى عارف بـ EF Core. ده انتهاك للـ Clean Architecture.
  > الـ Fluent API في Configuration Class منفصلة = الـ Domain نظيف تماماً، وكل قرارات قاعدة البيانات مكانها في الـ Infrastructure.
  >
  > **ليه `HasConversion<string>()` على الـ Role؟**
  > بيخلي EF Core يخزن `"Admin"` أو `"User"` في الـ DB بدل `1` أو `2`.
  > أسهل في القراءة لما بتفتح الـ DB مباشرة، وأكثر أماناً لو ضفت قيم جديدة للـ Enum مستقبلاً.

---

- [ ] **TODO-04** — اعمل `TaskItemConfiguration.cs` في `Persistence/Configurations/`

  ```csharp
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
  > أكتر Query هنعملها هي "جيب كل Tasks بتاعة User معين". من غير Index، الـ SQL Server هيـscan كل الجدول. مع Index بيروح على الـ rows دي مباشرة.
  >
  > **ليه Index مركب على `{UserId, CreatedAt}`؟**
  > لأن هنـsort الـ Tasks بالـ Priority والـ CreatedAt. الـ Index المركب ده بيسرّع الـ Query دي تحديداً.
  >
  > **ليه `OnDelete(Cascade)`؟**
  > لو اتمسح الـ User، كل الـ Tasks بتاعته تتمسح معاه تلقائياً. منطق واضح.

---

- [ ] **TODO-05** — اعمل `UserRepository.cs` في `Persistence/Repositories/`

  ```csharp
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

  > **ليه `DeleteAsync` مش فيها `await`؟**
  > الـ EF Core بيعمل Delete بيـmark الـ Entity كـ Deleted في الـ ChangeTracker بس — مش بيروح للـ DB دلوقتي. الـ DB بتتحدث لما تعمل `SaveChangesAsync()` اللي هيتعمل في Service Layer.
  > يعني Delete في الـ Repository = "علّم الحاجة دي للمسح". الـ Commit بييجي بعدين.

---

- [ ] **TODO-06** — اعمل `TaskRepository.cs` في `Persistence/Repositories/`

  ```csharp
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

  > **لازم تلاحظ:** الـ `OrderByDescending(t => t.Priority)` هنا بيشتغل لأن الـ `TaskPriority` Enum عنده قيم `Low=1, Medium=2, High=3` — يعني الـ High هيجي أول لما نعمل Descending. ده مش صدفة، ده قرار اتاخد في Block 1 عشان يخدم الـ Business Logic هنا.

---

- [ ] **TODO-07** — اعمل `DbSeeder.cs` في `Persistence/Seeders/`

  ```csharp
  public static class DbSeeder
  {
      public static async Task SeedAsync(AppDbContext context, IPasswordHasher passwordHasher)
      {
          if (await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
              return;

          var admin = new User
          {
              // هنحدد الـ Id والـ Fields هنا
              // هنشوف إزاي نتعامل مع private set في TODO-08
          };

          await context.Users.AddAsync(admin);
          await context.SaveChangesAsync();
      }
  }
  ```

  > **ليه نتحقق بـ `AnyAsync` الأول؟**
  > عشان الـ Seeder بيتشغل كل ما التطبيق يقوم. لو مش موجود check، هيعمل Admin جديد كل مرة.
  > الـ check ده بيضمن إن الـ Seed بييحصل مرة واحدة بس.
  >
  > ⚠️ **مشكلة هنواجهها هنا:** الـ `User` entity عنده `private set` على الـ Id والـ CreatedAt.
  > هنحلها في TODO-08.

---

- [ ] **TODO-08** — اعمل Constructor في الـ `User` و `TaskItem` entities لتسمح بالـ Creation

  افتح `User.cs` في الـ Domain وضيف Constructor:

  ```csharp
  public class User : BaseEntity
  {
      public string Name { get; private set; }
      public string Email { get; private set; }
      public string PasswordHash { get; private set; }
      public UserRole Role { get; private set; }

      // Constructor للـ Creation
      public static User Create(string name, string email, string passwordHash, UserRole role)
      {
          return new User
          {
              Id = Guid.NewGuid(),
              Name = name,
              Email = email,
              PasswordHash = passwordHash,
              Role = role,
              CreatedAt = DateTime.UtcNow
          };
      }

      // Private constructor للـ EF Core
      private User() { }
  }
  ```

  افعل نفس الشيء في `TaskItem.cs`.

  > **ليه `static Create()` بدل Constructor عادي؟**
  > ده Factory Method Pattern. بيوضّح النية: مش بتعمل Object بس — بتـ"Create" Entity جديدة مع كل القواعد بتاعتها.
  > لو احتجت تعمل validation قبل الإنشاء (زي check على الاسم مثلاً)، مكانه هنا جوه الـ Create method، مش في الـ Service.
  >
  > **ليه `private User() { }`؟**
  > الـ EF Core محتاج Parameterless Constructor عشان يعمل Instance لما بيقرأ من الـ DB. بنخليه private عشان ما حدش يستخدمه من بره الكلاس.

---

- [ ] **TODO-09** — اعمل أول EF Core Migration

  ```bash
  dotnet ef migrations add InitialCreate --project TaskManagement.Infrastructure --startup-project TaskManagement.API
  ```

  بعد ما يتعمل، **افتح ملف الـ Migration وقرأه بتمعن.** شوف الـ SQL اللي هيتنفذ وتأكد إن:
  - الجداول اتسمت صح (`Users`, `Tasks`)
  - الـ Indexes موجودة
  - الـ Foreign Key موجود
  - الـ Columns بـ Types المناسبة

  > **ليه نقرأ الـ Migration؟**
  > لأنك Senior بتفهم الـ SQL اللي بيتولد مش بس بتشغّله أعمى.
  > الـ Migration هي ترجمة حرفية للـ Configuration اللي كتبتها — لو في حاجة غلط في الـ Config، هتظهر هنا.

---

- [ ] **TODO-10** — طبّق الـ Migration على قاعدة البيانات

  ```bash
  dotnet ef database update --project TaskManagement.Infrastructure --startup-project TaskManagement.API
  ```

  بعدين افتح SQL Server (SSMS أو أي tool) وشوف الجداول اللي اتعملت بنفسك.

  > افتح كل جدول وشوف الـ Columns والـ Indexes. ده بيرسخ اللي عملته في دماغك.

---

- [ ] **TODO-11** — اعمل `InfrastructureServiceExtensions.cs` في `Extensions/`

  ```csharp
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

  ثم في `Program.cs`:
  ```csharp
  builder.Services.AddInfrastructure(builder.Configuration);
  ```

  > **ليه Extension Method بدل ما نكتب كل حاجة في `Program.cs`؟**
  > الـ `Program.cs` مش مكانه يعرف تفاصيل الـ Infrastructure. الـ Extension Method بتخبّي التفاصيل دي وبتديك API نظيف: `AddInfrastructure()`.
  > لو ضفت Service جديدة في الـ Infrastructure، بتضيفها في الملف ده بس، مش في `Program.cs`.
  >
  > **ليه `AddScoped` مش `AddSingleton`؟**
  > الـ DbContext مش Thread-Safe — ما ينفعش يتشارك بين Requests مختلفة.
  > `Scoped` = instance واحدة طول الـ HTTP Request وبتتمسح لما الـ Request يخلص. ده الصح للـ DbContext.

---

## Block 4 — Authentication (JWT + Hashing) ⬜ NOT STARTED

> هنبني الـ Authentication من الصفر. Password Hashing + JWT Generation.

---

- [ ] **TODO-01** — ثبّت في مشروع Infrastructure

  ```
  BCrypt.Net-Next
  ```

  > **ليه BCrypt؟** BCrypt بيضيف Salt تلقائياً ويعمل عمليات حساب متعددة (Work Factor). يعني حتى لو اتسرق الـ DB، صعب جداً يـcrack الـ passwords. أأمن من MD5 و SHA بكتير.

---

- [ ] **TODO-02** — نفّذ `PasswordHasher.cs` في `Infrastructure/` (implements `IPasswordHasher`)

  ```csharp
  public class PasswordHasher : IPasswordHasher
  {
      public string Hash(string password) =>
          BCrypt.Net.BCrypt.HashPassword(password);

      public bool Verify(string password, string hash) =>
          BCrypt.Net.BCrypt.Verify(password, hash);
  }
  ```

  > كل ما تعمل `Hash()` على نفس الكلمة، النتيجة مختلفة كل مرة بسبب الـ Salt العشوائي.
  > الـ `Verify()` بيعرف يتحقق لأن الـ Salt محفوظ جوه الـ Hash نفسه.

---

- [ ] **TODO-03** — ثبّت في مشروع API

  ```
  Microsoft.AspNetCore.Authentication.JwtBearer
  ```

---

- [ ] **TODO-04** — نفّذ `JwtTokenGenerator.cs` في `Infrastructure/` (implements `IJwtTokenGenerator`)

  ```csharp
  public class JwtTokenGenerator : IJwtTokenGenerator
  {
      private readonly IConfiguration _config;
      public JwtTokenGenerator(IConfiguration config) => _config = config;

      public string GenerateToken(User user)
      {
          var claims = new[]
          {
              new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
              new Claim(JwtRegisteredClaimNames.Email, user.Email),
              new Claim(ClaimTypes.Role, user.Role.ToString()),
              new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
          };

          var key = new SymmetricSecurityKey(
              Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
          var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

          var token = new JwtSecurityToken(
              issuer: _config["Jwt:Issuer"],
              audience: _config["Jwt:Audience"],
              claims: claims,
              expires: DateTime.UtcNow.AddMinutes(
                  int.Parse(_config["Jwt:ExpiryMinutes"]!)),
              signingCredentials: creds);

          return new JwtSecurityTokenHandler().WriteToken(token);
      }
  }
  ```

  > **الـ JWT مكون من 3 أجزاء:** Header.Payload.Signature
  > - الـ **Claims** هي الـ Payload — البيانات المخزنة جوه التوكن (UserId, Email, Role)
  > - الـ **Signature** بتضمن إن التوكن ما اتعدلش — أي تعديل بيبطّل الـ Signature
  > - السيرفر مش بيخزن التوكن — بس بيتحقق من الـ Signature بالـ Secret Key

---

- [ ] **TODO-05** — ضيف JWT config في `appsettings.json`

  ```json
  "Jwt": {
    "Secret": "your-super-secret-key-min-32-chars!!",
    "Issuer": "TaskManagement",
    "Audience": "TaskManagementUsers",
    "ExpiryMinutes": 60
  }
  ```

  > ⚠️ في Production الـ Secret ما يتحطش في `appsettings.json` — بيتحط في Environment Variables أو Azure Key Vault. هنا كده مناسب للـ Learning.

---

- [ ] **TODO-06** — سجّل الـ JWT Authentication في `Program.cs`

  ```csharp
  builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(options =>
      {
          options.TokenValidationParameters = new TokenValidationParameters
          {
              ValidateIssuer = true,
              ValidateAudience = true,
              ValidateLifetime = true,
              ValidateIssuerSigningKey = true,
              ValidIssuer = builder.Configuration["Jwt:Issuer"],
              ValidAudience = builder.Configuration["Jwt:Audience"],
              IssuerSigningKey = new SymmetricSecurityKey(
                  Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
          };
      });

  // وبعد app.Build() لازم تحط الاتنين دول بالترتيب ده:
  app.UseAuthentication();  // "مين انت؟"
  app.UseAuthorization();   // "مسموحلك؟"
  ```

  > **الترتيب مهم جداً:** Authentication الأول، Authorization تاني. لو عكستهم، الـ `[Authorize]` هيشتغل قبل ما يعرف مين المستخدم وكل حاجة هتـfail.

---

## Block 5 — Application Services + Controllers ⬜ NOT STARTED

> هنا بنكتب الـ Business Logic الحقيقية وبنكشفها عبر HTTP.

---

- [ ] **TODO-01** — نفّذ `UserService.cs` في `Application/Users/`

  بتـimplements `IUserService`. الـ Logic:
  - **Register:** Hash الـ Password → `User.Create(...)` → `AddAsync` → `SaveChanges` → return `UserResponse`
  - **Login:** `GetByEmailAsync` → `Verify` Password → `GenerateToken` → return token string
  - **GetById:** `GetByIdAsync` → map لـ `UserResponse`
  - **GetAll:** `GetAllAsync` → map لـ `IEnumerable<UserResponse>`
  - **Delete:** `GetByIdAsync` → `DeleteAsync` → `SaveChanges`

  > **لازم `SaveChangesAsync()` يكون في الـ Service مش في الـ Repository.**
  > ليه؟ لأن الـ Service هي اللي بتعرف متى الـ "Unit of Work" خلص. في بعض الحالات هتعمل أكتر من عملية (Add User + Send Email مثلاً) وعايز تـcommit كلهم مرة واحدة.

---

- [ ] **TODO-02** — نفّذ `TaskService.cs` في `Application/Tasks/`

  بتـimplements `ITaskService`. الـ Logic + Business Rules:

  **CreateAsync:**
  ```
  1. تحقق من Duplicate: نفس العنوان + نفس اليوم + نفس الـ User → DomainException
  2. TaskItem.Create(...) 
  3. AddAsync → SaveChanges
  4. أرسل للـ Background Queue (Block 7)
  5. return TaskResponse
  ```

  **GetByIdAsync:**
  ```
  1. GetByIdAsync من الـ Repository
  2. لو مش موجود → DomainException("Task not found")
  3. لو الـ UserId مش بتاعه → DomainException("Access denied")
  4. return TaskResponse
  ```

  > **الـ Ownership Check مهم جداً.** مش كفاية تتحقق إن الـ Task موجودة — لازم تتحقق إنها بتاعة الـ User ده. غلطة شائعة جداً عند المبتدئين.

---

- [ ] **TODO-03** — اعمل `AuthController.cs` في `API/Controllers/`

  ```
  POST /api/auth/register  → IUserService.RegisterAsync
  POST /api/auth/login     → IUserService.LoginAsync
  ```

  > الـ Endpoints دي **مش محتاجة** `[Authorize]` — هي اللي بتديك التوكن من الأساس.

---

- [ ] **TODO-04** — اعمل `UsersController.cs` في `API/Controllers/`

  ```
  GET    /api/users/me        → [Authorize]              → IUserService.GetByIdAsync
  GET    /api/users           → [Authorize(Roles="Admin")] → IUserService.GetAllAsync
  DELETE /api/users/{id}      → [Authorize(Roles="Admin")] → IUserService.DeleteAsync
  POST   /api/users           → [Authorize(Roles="Admin")] → IUserService.RegisterAsync
  ```

  > **إزاي بتجيب الـ userId من التوكن؟**
  > ```csharp
  > var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
  > ```
  > الـ `User` هنا مش الـ Entity — ده الـ `ClaimsPrincipal` اللي ASP.NET بيملّيه من الـ JWT تلقائياً.

---

- [ ] **TODO-05** — اعمل `TasksController.cs` في `API/Controllers/`

  ```
  POST   /api/tasks           → [Authorize] → ITaskService.CreateAsync
  GET    /api/tasks           → [Authorize] → ITaskService.GetAllByUserAsync
  GET    /api/tasks/{id}      → [Authorize] → ITaskService.GetByIdAsync
  PATCH  /api/tasks/{id}/status → [Authorize] → ITaskService.UpdateStatusAsync
  ```

  > **ليه `PATCH` مش `PUT` لتحديث الـ Status؟**
  > `PUT` = استبدال الـ Resource كاملاً. `PATCH` = تعديل جزء منه.
  > بما إننا بنغير الـ Status بس، `PATCH` هو الأصح في الـ RESTful design.

---

- [ ] **TODO-06** — فعّل Swagger مع JWT Support في `Program.cs`

  ```csharp
  builder.Services.AddSwaggerGen(options =>
  {
      options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
      {
          Name = "Authorization",
          Type = SecuritySchemeType.Http,
          Scheme = "Bearer",
          BearerFormat = "JWT",
          In = ParameterLocation.Header
      });
      options.AddSecurityRequirement(new OpenApiSecurityRequirement
      {
          {
              new OpenApiSecurityScheme {
                  Reference = new OpenApiReference {
                      Type = ReferenceType.SecurityScheme, Id = "Bearer" }
              },
              Array.Empty<string>()
          }
      });
  });
  ```

  > ده بيضيف زرار "Authorize" في Swagger UI تقدر تحط فيه الـ JWT Token وتختبر الـ Protected Endpoints.

---

## Block 6 — Redis Caching ⬜ NOT STARTED

> هنخلي الـ `GetTaskById` يرجع من الـ Cache بدل الـ DB في الـ Requests المتكررة.

---

- [ ] **TODO-01** — ثبّت في Infrastructure

  ```
  StackExchange.Redis
  ```

---

- [ ] **TODO-02** — اعمل `ICacheService.cs` في `Application/Common/Interfaces/`

  ```csharp
  public interface ICacheService
  {
      Task<T?> GetAsync<T>(string key);
      Task SetAsync<T>(string key, T value, TimeSpan expiry);
      Task RemoveAsync(string key);
  }
  ```

  > الـ Interface في Application لأن الـ TaskService محتاج يستخدمه — ونفس قاعدة الـ Dependency Inversion.

---

- [ ] **TODO-03** — نفّذ `RedisCacheService.cs` في Infrastructure (implements ICacheService)

  > بيتكلم مع Redis عبر `IConnectionMultiplexer`. بيـserialize الـ Object لـ JSON عشان يخزنه.

---

- [ ] **TODO-04** — ادمج الـ Cache في `TaskService.GetByIdAsync`

  ```
  1. ابحث في Cache بـ key = "task:{id}"
  2. لو موجود → return من الـ Cache مباشرة
  3. لو مش موجود → اجيب من الـ DB → خزّن في Cache بـ TTL = 10 دقايق → return
  ```

  > ده الـ **Cache-Aside Pattern** (أو Read-Through). الأشهر استخداماً في الـ Backend.

---

- [ ] **TODO-05** — ابطل الـ Cache في `TaskService.UpdateStatusAsync`

  ```
  بعد ما تعمل Update في الـ DB، اعمل:
  await _cache.RemoveAsync($"task:{id}");
  ```

  > لو ما عملتش ده، المستخدم ممكن يشوف Status قديم من الـ Cache لحد ما الـ TTL ينتهي.
  > ده بيسمى **Stale Cache** وهو من أشهر الـ Bugs في الـ Caching.

---

## Block 7 — Background Processing ⬜ NOT STARTED

> لما Task تتعمل، بنبعتها لـ Queue وWorker يشتغل عليها في الخلفية.

---

- [ ] **TODO-01** — اعمل `ITaskQueue.cs` في `Application/Common/Interfaces/`

  ```csharp
  public interface ITaskQueue
  {
      void Enqueue(Guid taskId);
      Task<Guid> DequeueAsync(CancellationToken cancellationToken);
  }
  ```

---

- [ ] **TODO-02** — نفّذ `TaskQueue.cs` في Infrastructure باستخدام `System.Threading.Channels`

  ```csharp
  public class TaskQueue : ITaskQueue
  {
      private readonly Channel<Guid> _channel = Channel.CreateUnbounded<Guid>();

      public void Enqueue(Guid taskId) =>
          _channel.Writer.TryWrite(taskId);

      public async Task<Guid> DequeueAsync(CancellationToken ct) =>
          await _channel.Reader.ReadAsync(ct);
  }
  ```

  > **ليه `Channel` مش `ConcurrentQueue`؟**
  > الـ `ConcurrentQueue` مش فيها Async Support — هتحتاج تعمل Polling (loop بيسأل "في حاجة؟" كل شوية) وده بيضيع CPU.
  > الـ `Channel` فيها `ReadAsync` بيـawait حتى في حاجة تجي — Zero CPU waste.

---

- [ ] **TODO-03** — اعمل `TaskProcessingWorker.cs` في Infrastructure (inherits `BackgroundService`)

  ```csharp
  public class TaskProcessingWorker : BackgroundService
  {
      private readonly ITaskQueue _queue;
      private readonly IServiceScopeFactory _scopeFactory;

      public TaskProcessingWorker(ITaskQueue queue, IServiceScopeFactory scopeFactory)
      {
          _queue = queue;
          _scopeFactory = scopeFactory;
      }

      protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
          while (!stoppingToken.IsCancellationRequested)
          {
              var taskId = await _queue.DequeueAsync(stoppingToken);
              using var scope = _scopeFactory.CreateScope();
              var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
              // اعمل processing هنا — مثلاً update الـ Status لـ InProgress
          }
      }
  }
  ```

  > **ليه `IServiceScopeFactory` مش Inject الـ DbContext مباشرة؟**
  > الـ `BackgroundService` عمره هو عمر الـ Application — ده Singleton.
  > الـ `DbContext` Scoped — ما يعيشش في Singleton مباشرة (Captive Dependency).
  > الحل: تعمل Scope جديد في كل مرة محتاج فيها الـ DbContext.

---

- [ ] **TODO-04** — سجّل الـ Queue والـ Worker في DI (في `InfrastructureServiceExtensions.cs`)

  ```csharp
  services.AddSingleton<ITaskQueue, TaskQueue>();
  services.AddHostedService<TaskProcessingWorker>();
  ```

  > الـ Queue بيكون `Singleton` عشان نفس الـ Instance تتشارك بين الـ HTTP Requests والـ Worker.

---

- [ ] **TODO-05** — في `TaskService.CreateAsync`، بعد `SaveChangesAsync()`، ضيف:

  ```csharp
  _taskQueue.Enqueue(task.Id);
  ```

  > الـ DB Save الأول عشان الـ Data محفوظة.
  > الـ Enqueue بييجي بعده عشان مش هنـqueue Task مش موجودة في الـ DB.
  > الترتيب مهم.
