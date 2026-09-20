# Аудит модели данных и фронтенда — FootballAcademy (ASP.NET Core, net8.0)

Дата: read-only анализ. Файлы не изменялись (единственный созданный артефакт — этот отчёт).

## Сводная таблица находок

| # | Severity | Область | Находка | Доказательство |
|---|---|---|---|---|
| 1 | 🔴 Critical | Data | 15 из 16 конфигураций EF (`Core\Configurations\*.cs`) НЕ применяются: в `OnModelCreating` вызван только `ApplyConfiguration(new PlayerConfiguration())` | `Core\Entity\ContextAuth.cs:51`; `grep ApplyConfiguration` = 1 совпадение |
| 2 | 🔴 Critical | Frontend/Sec | `UsersApiController` — весь API администрирования пользователей без авторизации (`[Authorize]` закомментирован); аноним может сменить роль, сбросить пароль, создать менеджера | `UI\ApiController\UsersApiController.cs:14`, методы `CreateManager` :95, `ChangeRole` :112, `ResetPassword` :116 |
| 3 | 🔴 Critical | Frontend/Sec | CSRF полностью отключён на всех API: `[IgnoreAntiforgeryToken]` на 13 контроллерах, cookie-аутентификация — единственная | `PlayersApiController.cs:16`, `GroupsApiController.cs:12`, `UsersApiController.cs:15`, `CabinetController.cs`-вызовы из `cabinet.js`, `users.js` |
| 4 | 🔴 Critical | Data | `AbsenceNotices` без FK на `CreatedByUserId`, `Attendances.SubscriptionId` без FK в БД, `ChatReadMarks`/`Notifications` вообще без FK и без индексов — объявленные в конфигурациях связи не существуют в схеме | `ContextAuthModelSnapshot.cs:25-66`, `:312-343`, `:389-427` vs `AttendanceConfiguration.cs:23-27`, `AbsenceNoticeConfiguration.cs` |
| 5 | 🔴 Critical | Data/Deploy | Расхождение версий EF Core: Core компилируется с EF 8.0.30 / Npgsql 8.0.11, UI тянет EF 9.0.19 / Npgsql 9.0.5, фактически исполняется **EF Core 9 на net8.0**; снапшот модели сгенерирован EF 9 | `UI\bin\Debug\net8.0\Microsoft.EntityFrameworkCore.dll 9.0.1926.37004`, `Npgsql.dll 9.0.5.0`; `ContextAuthModelSnapshot.cs:20` ProductVersion 9.0.19 |
| 6 | 🟠 Major | Data/Deploy | `await context.Database.MigrateAsync()` закомментирован, схема накатывается только вручную `dotnet ef database update`; при обычном запуске БД пустая → EF-запросы падают | `UI\Program.cs:217` |
| 7 | 🟠 Major | Data | Три разных часовых пояса: `AppTime` = «Belarus Standard Time»/Europe/Minsk, `ScheduleService`/`PlayerCabinetService`/`ParentService` = жёстко `Europe/Moscow` | `UI\Models\DataModels\AppTime.cs:7`, `UI\Services\ScheduleService.cs:14`, `PlayerCabinetService.cs:11`, `ParentService.cs:145` |
| 8 | 🟠 Major | Data | `PostService` вызывает `ToUniversalTime()` над значением, которое десериализовано как `Unspecified` (фронт присылает UTC-строку `toISOString()`), → дата публикации/события съезжает на смещение пояса | `UI\Services\PostService.cs:88`, `UI\wwwroot\js\post.js:44`, `PostEditDto.cs:10` |
| 9 | 🟠 Major | Frontend/Sec | XSS: данные из БД вставляются через `innerHTML` без экранирования в 10+ файлах | `subscriptions.js:46,96-99`, `coach-trainig.js:24-40`, `dashboard.js:9`, `parent-progress.js:28,45`, `coach-assess.js:15-19`, `player-schedule.js:22`, `parent-schedule.js:14,41`, `player-cab.js:16,21`, `parent-attendance.js:35`, `users.js:75`, `groups.js:77` |
| 10 | 🟠 Major | Frontend | `IsStaff` проверяет несуществующую роль `Admin`; значит `[Authorize(Roles="Admin,Coach")]` на удалении игрока и справочниках недоступно менеджеру | `UI\ApiController\PlayersApiController.cs:33`, `:84`, `:90`; роли создаются в `Core\DbInitializer.cs:28` (Manager, Coach, Parent, Player) |
| 11 | 🟠 Major | Frontend | JS-ошибка на странице прогресса: `$('#tblProgress tbody' && 'tblProgress')` → `querySelector('tblProgress')` = null → `TypeError` | `UI\wwwroot\js\parent-progress.js:24` |
| 12 | 🟠 Major | Migration | 15 миграций, из них 2 полностью пустые (`addUserProfileUpdate`, `AddConfOne`), «Correct» — переименование таблицы, серия `addUserProfile*2`, `AddConfOne/Two` — латание уже существующей модели | `20260909182741_addUserProfileUpdate.cs:11-21`, `20260919071900_AddConfOne.cs:11-21`, `20260906091746_Correct.cs:21-28` |
| 13 | 🟠 Major | Data | Секреты в репозитории: пароль БД `postgres` и симметричный ключ JWT в открытом виде | `UI\appsettings.json:4-5,11` |
| 14 | 🟠 Major | Frontend | FullCalendar JS грузится, но его CSS закомментирован и файла в `wwwroot` нет → календарь без стилей | `UI\Views\Shared\_Layout.cshtml:22-23`, `242`; `wwwroot\css` не содержит fullcalendar |
| 15 | 🟡 Medium | Data | Мягкое удаление не покрывает уникальные индексы и представления: `Players.UserId` — уникальный индекс, soft-deleted игрок навсегда «держит» UserId | `Core\Configurations\PlayerConfiguration.cs:30`, `ContextAuthModelSnapshot.cs:572-576` |
| 16 | 🟡 Medium | Data | `IgnoreQueryFilters` не используется нигде → soft-deleted сущности недостижимы, восстановления/аудита нет; удаление родителя делает его детей «без родителя» во всех проекциях | `grep IgnoreQueryFilters` = 0 совпадений; `PlayerService.cs:115` читает `p.Parent` |
| 17 | 🟡 Medium | Data | Плейсхолдер в продакшн-коде: имя родителя подменяется строкой «не добавляет ФИО родителя(разобраться)» | `UI\Services\PlayerService.cs:131` |
| 18 | 🟡 Medium | Data | Целостность данных в БД не подтверждается: триггеров/check-constraints нет, `SkillScore.Value` (1..10) проверяется только на UI | `SkillScoreConfiguration.cs:19` (закомментировано), `ContextAuth.cs:47-113` |
| 19 | 🟡 Medium | Data | Смешение `DateTime` / `DateTimeOffset` / `DateOnly`: UTC-поле `StartsAt` и `PaidAt`, локальные `DateTime.Now` в представлениях, `LockoutEnd` — `DateTimeOffset` | `Training.cs:19-20`, `Payment.cs:16`, `_Layout.cshtml:226`, `Groups\Print.cshtml:88` |
| 20 | 🟡 Medium | Data | `SaveChanges()` переопределён только асинхронно: любой синхронный вызов (есть один — `DbInitializer`) не проставляет `CreatedAt`/`UpdatedAt` и делает физическое удаление | `Core\Entity\ContextAuth.cs:116`, `Core\DbInitializer.cs:141` |
| 21 | 🟡 Medium | Data | Логические несоответствия сидов: 8 занятий/30 дней/80 руб. = месяц, 16 занятий = квартал, `Visits`-планы заполняют `PeriodUnit` (поле «только для Period»), `SortOrder` у всех = 0, `PeriodUnit.Quarter = 3`/`Year = 12` используются как «месяцы» | `Core\DbInitializer.cs:104-116`; `TrainingPlan.cs:15-16`; `Enums\PeriodUnit.cs:9-12` |
| 22 | 🟡 Medium | Frontend | Несуществующие ссылки: `_ValidationScriptsPartial` подключён только в Login/Register, остальные 7+ модальных форм валидируются вручную; обработчики `deletePlayer`/`editPlayer` вызываются, но закомментированы | `UI\Views\Manager\AllUsers.cshtml:72,124,127`; `_Layout` → `~/lib/...` без version-tag |
| 23 | 🟡 Medium | Frontend | Только 13 DataAnnotations на весь проект (Login/Register/ChangePassword); все DTO (`PlayerEditDto`, `PostEditDto`, `GroupEditDto`, `VenueDto`, …) без атрибутов валидации | `grep Required|StringLength` в `UI\Models\ViewModels` = 13 |
| 24 | 🟡 Medium | Frontend | Дублирование библиотек: fullcalendar 6.1.10 **и** 6.1.15, tabulator 6.3.0 **и** 6.5.0, `jquery-3.7.0.min.js` + `lib/jquery`, `bootstrap@5.3.0` + `lib/bootstrap`; CSS табулятора 6.2.0 в layout и 6.3.0 в Player/Index | `wwwroot\js\framework\*`, `wwwroot\lib\*`, `_Layout.cshtml:19,238,242`, `Player\Index.cshtml:15` |
| 25 | 🟡 Medium | Frontend | `moment.min.js` загружается глобально, но не используется ни в одном файле (Tabulator нулевой конфигурации) | `_Layout.cshtml:245`; `grep "moment("` в `wwwroot\js` — только сам файл библиотеки |
| 26 | 🟡 Medium | Frontend | Accessibility: `<label>` без `for`, инпуты без `id`, инпуты без `aria-label`, `img` без `alt` (в `Profile\Index.cshtml:14` alt пустой), `<html lang="en">` при русском UI | `Cabinet\Index.cshtml:118-122`, `Venues\Index.cshtml:49-53`, `My\Attendance`-фильтры `Parent\Attendance.cshtml:12`, `_Layout.cshtml:5` |
| 27 | 🟢 Minor | Frontend | Дубли `id` в одной странице (`chkActive` дважды в Venues), `onclick="print()"`/`confirm()`/`prompt()`/`alert()` вместо модальных окон, `console.log` в продакшене | `Venues\Index.cshtml:57,60`; `Groups\Print.cshtml:78`; `subscriptions.js:127`; `groups.js:21,25,40,57,58,90` |
| 28 | 🟢 Minor | Data | Мёртвый код и заглушки: `AppUserRole : IdentityRole<Guid>` (дубликат `AppRole`, неиспользуем), `GenericRepo.Test()`, `ManagerController`/`AllUsers` — устаревший модуль, закомментированные блоки в `Program.cs:30-81`, `DbInitializer.cs:19-24` | `Core\Entity\AppUserRole.cs:10-14`, `Core\Repositories\GenericRepo.cs:24-28`, `UI\Views\Manager\AllUsers.cshtml` |
| 29 | 🟢 Minor | Data | `ChatReadMarks` без индексов и FK → `MarkRead` сканирует всю таблицу; `Notifications` без индекса по `UserId` | `ContextAuthModelSnapshot.cs:312-343`, `:389-427` |
| 30 | 🟢 Minor | Data | Синхронный `.Result` в асинхронном сервисе (deadlock-риск в UI-потоке) | `UI\Services\SubscriptionService.cs:68` |

---

# Часть A — Модель данных

Объём: 20 сущностей, 16 файлов конфигураций, 9 перечислений, 15 миграций + снапшот модели (1507 строк), `DbInitializer`.

## A1. Сущности: проблемы проектирования

### A1.1 🔴 Конфигурации существуют, но не применяются
`Core\Entity\ContextAuth.cs:47-51`:

```csharp
protected override void OnModelCreating(ModelBuilder builder)
{
    base.OnModelCreating(builder);
    builder.ApplyConfiguration(new PlayerConfiguration());
```

`ApplyConfigurationsFromAssembly` не вызывается (проверено grep — единственное совпадение). Значит **все** настройки из 15 остальных файлов `Core\Configurations\*.cs` мертвы:

* `AbsenceNoticeConfiguration.cs:16` — `b.ToTable("Attendances")` (коллизия таблиц с `Attendance`!) и, что важнее, `:22-23` — `HasForeignKey(t => t.TrainingId)` для связи `Player` (ошибка копипасты: FK для Player берётся от TrainingId). Если бы конфигурация применилась, EF выбросил бы исключение о конфликте таблиц — то есть она ещё и неработоспособна «как написано».
* `AttendanceConfiguration.cs:27` — `HasIndex(new { TrainingId, PlayerId }).IsUnique()`. В БД этого индекса **нет** (`ContextAuthModelSnapshot.cs:257-261` — только одиночные индексы), значит дубли посещаемости на одну тренировку возможны, и `TrainingsUsed` будет списываться дважды.
* `PaymentConfiguration.cs:17-18` — `HasPrecision(12,2)` и `HasConversion<string>()` для `Method` не применены: `Payments.Price` в снапшоте `numeric` без точности (`:805-806`), enum хранится как `integer` (`:811`), `PaymentMethod` в снапшоте — `int`, а `Subscriptions.Status`, `Trainings.Status/Kind`, `Posts.Type/Audience` — тоже `integer`, хотя в UI и JS повсеместно сравниваются **строки** (`subscriptions.js:7-13` — ключи 0..4 как числа; `player.js`, `feed.js:4-9` — числовые ключи; но `coach-trainig.js:11-18` и `cabinet.js:77,93` — строки `'Match'`, `'Visits'`, `'PendingPayment'`). Это скрытый источник ошибок сериализации.
* `NotificationConfiguration.cs:21` — `HasIndex(new { UserId, ReadAt }).IsUnique()`: если бы применилось, у пользователя могло бы быть **только одно** непрочитанное уведомление. Хорошо, что не применилось; плохо, что такой код вообще написан.
* `TrainingGroupConfiguration.cs:26` — фильтрованный уникальный индекс `Name` для неархивных; в снапшоте отсутствует (`:956` только `HasIndex("CoachId")`). Поскольку `TrainingGroupConfiguration` не применяется, а `AddConfTwo` довёл `Players` до модели — состояние «модель времени выполнения ≠ снапшот» сохраняется, и следующая `dotnet ef migrations add` сгенерирует миграцию, откатывающую/добавляющую всё это заново.
* `VenueConfiguration.cs:20`, `SkillConfiguration.cs:16`, `TrainingPlanConfiguration.cs:16`, `TrainingConfiguration.cs:17-39`, `SubscriptionConfiguration.cs:17-25`, `CoachConfiguration.cs:17-22`, `ParentProfileConfiguration.cs:18-21`, `PostConfiguration.cs:20-60`, `SkillScoreConfiguration.cs:20`, `SkillAssessmentConfiguration.cs:17-18` — не применены. Проверка по снапшоту: `TrainingConfiguration` требует `IQ_Trainings_StartsAt_EndsAt`, `IX_Trainings_VenueId_StartsAt`, `IX_Trainings_SeriesId` — в снапшоте (`:895-899`) только одиночные `GroupId`, `OpponentGroupId`, `VenueId`. `SubscriptionConfiguration` требует три составных индекса — в снапшоте (`:828-830`) только `PlanId`, `PlayerId`. `PostConfiguration` требует `IX_Posts_Published_Pinned_Date` — отсутствует. `SkillScoreConfiguration` требует `IX_SkillScores_AssessmentId_SkillId` — отсутствует.

**Итог:** «на бумаге» модель хорошо нормализована и проиндексирована, «в бою» — почти нет ни индексов, ни FK, ни проверок. Это главный вывод аудита модели данных.

### A1.2 🔴 Отсутствующие FK и навигации
По снапшоту модели и определениям сущностей:

| Сущность | Поле | FK/навигация | Следствие |
|---|---|---|---|
| `AbsenceNotice` | `CreatedByUserId` | нет (`AbsenceNotice.cs:18`) | нет ссылочной целостности, нет возможности показать автора уведомления |
| `AbsenceNotice` | `PlayerId` | FK есть (`20260904194428_ParentModule.cs:33`), но нет уникального индекса `(PlayerId, TrainingId)` | дубли уведомлений об отсутствии |
| `ChatReadMark` | `GroupId`, `UserId` | FK/индексов нет вообще (`ContextAuthModelSnapshot.cs:325-342`) | `conn.invoke('MarkRead', …)` + чтение → seq scan |
| `Notification` | `UserId` | FK/индекса нет (`:389-427`) | уведомления «висят» после удаления пользователя |
| `Subscription` | `RequestedByUserId`, `ConfirmedByUserId` | FK/навигаций нет (`Subscription.cs:24-26`) | нет каскадов/проверок, `ConfirmedByUserId` не читается в UI нигде |
| `Payment` | — | ок (`SubscriptionConfiguration` не применён, но FK создан миграцией `Init`) | — |
| `AppUser` | `Posts` | есть (`AppUser.cs:30`), но не инициализирована (`null!` отсутствует) | NRT-предупреждения, риск NRE при `u.Posts.Add` |

### A1.3 🟡 Nullable/reference-type и атрибуты
* `Core\Entity\AppUser.cs:12-13` — `FirstName`/`LastName` без `= null!` при `<Nullable>enable</Nullable>`; в БД поля `NOT NULL` (миграция `Init.cs:37-38`), в C# — `string` без инициализации → предупреждения CS8618 и логическая неоднозначность.
* `Core\Entity\AppRole.cs:12` — `Description` `string` без `null!` и без `[Required]`, в снапшоте `NOT NULL` (`ContextAuthModelSnapshot.cs:86`).
* Ни в одной сущности нет DataAnnotations (`[Required]`, `[MaxLength]`, `[Range]`) — вся валидация держится на Fluent-конфигурации, которая **не применяется** (см. A1.1). Исключение — `BaseEntity.cs:13` `[Key]`.
* `Core\Entity\AbsenceNotice.cs:18` — `CreatedByUserId` без `Guid?`, хотя уведомление может создавать и менеджер.
* `Core\Entity\Payment.cs:16` — `PaidAt` без семантики UTC (`DateTime` без комментария), тогда как `Training.StartsAt/EndsAt` помечены «// UTC».

### A1.4 🟡 Смешение DateOnly / DateTime / DateTimeOffset
* `BaseEntity.CreatedAt/UpdatedAt/DeletedAt` — `DateTime` (в Npgsql → `timestamptz`), корректно заполняются `DateTime.UtcNow` в `ContextAuth.cs:118`.
* `Training.StartsAt/EndsAt`, `Payment.PaidAt`, `Post.EventDate`, `Post.PublishedAt`, `Notification.ReadAt`, `ChatReadMark.ReadUpTo`, `Training.CompletedAt`, `Subscription.ConfirmedAt` — все `DateTime` без явного `DateTimeKind`; `Training.cs:19-20` документирует UTC, остальные — нет.
* `AppUser.BirthDate`, `Player.BirthDate`, `Player.MedicalCertificateUntil`, `Coach.HiredAt`, `Subscription.From/To`, `SkillAssessment.Date` — `DateOnly` (хорошо, для дат без времени).
* `IdentityUser.LockoutEnd` — `DateTimeOffset`; сравнения в `UserAdminService.cs:177` (`DateTimeOffset.MaxValue`) корректны, но смешение типов в одном контексте затрудняет единые правила.
* `Training.StartsAt` — `timestamptz`: запись локального `DateTime.Now` (без Kind=Utc) приведёт к исключению Npgsql `Cannot write DateTime with Kind=Local`; в коде модели таких мест нет, но `_Layout.cshtml:226` и `Groups\Print.cshtml:88` используют `DateTime.Now` для отображения, а не для записи.

### A1.5 🟢 Мелочи именования/структуры
* `Core\Entity\AppUserRole.cs:10` — `class AppUserRole : IdentityRole<Guid>` — это не user-role link (должно быть `IdentityUserRole<Guid>`); класс не зарегистрирован ни в `DbSet`, ни в `OnModelCreating` → мёртвый код, и при попытке замапить дал бы коллизию с `Roles`.
* `ContextAuth.cs:35` — `public DbSet<AppRole> Roles` перекрывает унаследованное `IdentityDbContext.Roles`, `:43` `Users` — аналогично. Работает, но опасно: часть кода может обращаться к `base.Roles` (через `RoleManager`) и видеть другое поведение (в частности, из-за query-фильтра — см. A2).
* `PlayerConfiguration` — единственная применяемая конфигурация, при этом `Player.Parent` — `WithMany()` без обратной коллекции, а `PlayerService` постоянно обращается к `p.Parent.LastName` (`PlayerService.cs:59,68,115`) — работает через теневое свойство/EF-навигацию, но обратная навигация отсутствует.
* `Training.CreateAsync` создаёт события без проверки `Venue.IsActive` при апдейте серии (`ScheduleService.cs:191-192`) — при `UpdateAsync` валидация есть через `ValidateAsync`, ок; но `MoveAsync` не проверяет доступность места (`:201-227`) — конфликты проверяются, а `IsActive`/архив группы нет.
* `TrainingPlan.VisitsValidDays = 60` по умолчанию (`TrainingPlan.cs:17`), но у «8 занятий» в сиде 30, у «16 занятий» — 60 (`DbInitializer.cs:113,116`).

## A2. Фильтр мягкого удаления в `ContextAuth.OnModelCreating`

`Core\Entity\ContextAuth.cs:100-113`:

```csharp
foreach (var entityType in builder.Model.GetEntityTypes()
             .Where(t => typeof(BaseEntity).IsAssignableFrom(t.ClrType)))
{
    builder.Entity(entityType.ClrType)
        .Property(nameof(BaseEntity.Id))
        .HasDefaultValueSql("gen_random_uuid()");
    var parameter = Expression.Parameter(entityType.ClrType, "e");
    var body = Expression.Not(Expression.Property(parameter, nameof(BaseEntity.IsDeleted)));
    builder.Entity(entityType.ClrType).HasQueryFilter(Expression.Lambda(body, parameter));
}
```

Конкретные риски:

1. **Фильтр навешивается на ВСЕ `BaseEntity`.** Включая `Player`, `Training`, `Attendance`, `Subscription`, `Payment`, `SkillAssessment`, `SkillScore`, `ChatMessage` — то есть и на «операционные» сущности, которые нельзя скрывать: отменённая/удалённая `Training` исчезнет из календаря, но останется в `Attendances` с FK → `NotifyEventAsync`/отчёты будут ссылаться на невидимые строки.
2. **Уникальные индексы против мягко удалённых строк.** `PlayerConfiguration.cs:30` задаёт уникальный `Players.UserId`; мягко удалённый игрок навсегда занимает `UserId`, и `PlayerAccountService.CreateAsync` (`:27`) проверяет `player.UserId is not null` — по скрытому игроку проверка не сработает, а повторное использование UserId упрётся в уникальный индекс. Аналогично `ParentProfile.UserId` (One-to-One, `ContextAuthModelSnapshot.cs:1232-1233`) и декларированный, но неприменённый `IX_Notifications_UserId_ReadAt`.
3. **Взаимодействие с Identity.** `AppUser` — не `BaseEntity`, но `IdentityDbContext` работает через `UserManager`/`RoleManager` и запросы `Users`. Если бы фильтр навесили и на `AppUser` (в текущем виде — нет), вход стал бы невозможен. Сейчас противоположная проблема: **`AppUser.IsDeleted` вообще не существует**, а `AppRole` (унаследованный от `IdentityRole<Guid>`) тоже вне `BaseEntity` → единый механизм мягкого удаления не покрывает аккаунты, тогда как `UserAdminService` использует блокировку (`Block`) — две несогласованные модели «удаления».
4. **Каскады.** `Training→Group` (`TrainingConfiguration.cs:23`, `Cascade`), `Attendance→Training/Player` (`:20-22`, `Cascade`), `SkillScore→Assessment` (`Cascade`), `ChatMessage→Group` (Cascade, снапшот `:1207`), `Coach→User` (`CoachConfiguration.cs:21`, Cascade). Мягкое удаление группы (`Groups.Delete` нет — есть `Archive`, `GroupService.cs:214`) не выполняется, но если удалить `Group` через `Remove`, EF+sentinel cascade пометит удалёнными все тренировки, посещаемость, сообщения чата и посты группы — массовая необратимая (без `IgnoreQueryFilters`) деградация. Плюс несоответствие с БД: `Trainings.VenueId` в БД имеет `Cascade` (снапшот `:1380-1381`), а `TrainingConfiguration` объявляет `Restrict` (`:31`) — при рассинхроне EF ждёт `Restrict`, БД удалит каскадом.
5. **Обязательные навигации.** `Player.GroupId` — `SetNull` (ок), `Attendance.PlayerId` — `Cascade`; при soft-deleted `Player` его `Attendance` остаются, а `Include(a => a.Player)` вернёт `null` → `NullReferenceException` в проекциях (`PlayerCabinetService`, `DashboardService`).
6. **`SaveChangesAsync` (ContextAuth.cs:116-137)** переопределён верно (`Added→CreatedAt`, `Modified→UpdatedAt`, `Deleted→IsDeleted/DeletedAt`), но `Id` объявлен `[Key]` **без** `[DatabaseGenerated(Identity)]` (закомментировано в `BaseEntity.cs:14`) — значит EF считает ключ задаваемым приложением и не подставит `gen_random_uuid()` автоматически для новых сущностей (SQL-default работает, только если EF не передаёт значение; при `Guid.Empty` EF передаст `00000000-...`, и PK-конфликт на втором инсерте). Это потенциальный 🔴 баг вставки, замаскированный тем, что большинство сервисов создают объекты через `new` без явного `Id` (тогда `Guid.Empty` → EF всё же подставляет default value, поскольку `ValueGeneratedOnAdd` выводится из `HasDefaultValueSql`; но в `DbInitializer` `Id` задаётся явно).
7. **Один фильтр на иерархию/TFH не учтён** — сейчас TPH нет, поэтому не проявляется, но `builder.Entity(entityType.ClrType).HasQueryFilter(...)` в цикле по всем типам при появлении наследования даст `InvalidOperationException` (фильтр на производный тип).
8. **`IsDeleted`/`DeletedAt` — «слепые» поля**: ни одна проекция в UI не показывает признак удаления, ни один запрос не использует `IgnoreQueryFilters` (0 совпадений по всему решению). Значит:
   * восстановить данные через UI невозможно;
   * `Groups\Print`/отчёты за прошлые периоды «потеряют» удалённых детей, но `Attendances` за них останутся → расхождение сумм посещаемости и абонементов (`Subscriptions.TrainingsUsed`);
   * FK-ссылки из «живых» строк на удалённые (`Player.ParentId → AppUser` — не `BaseEntity`, `Subscription.RequestedByUserId` — без FK) ломают `.Include`, что видно в `PlayerService.cs:115` (см. A1.3/#17).

## A3. Мягкое удаление и `IsDeleted` — где нужен `IgnoreQueryFilters` / где ломаются FK

* `PostService.DeleteAsync` (`PostService.cs:69-72`) помечает пост удалённым; при этом `Posts` — единственная сущность с реальной аудиторией и ссылками из уведомлений (`Notification.Link`) → переход по уведомлению ведёт на удалённый пост, `PostsApiController.Get` (`:22-26`) вернёт `NotFound`.
* `ChatService.DeleteAsync` (`ChatService.cs:76-82`) — soft-delete сообщения, но `ChatMessage.ReplyTo` (`ChatMessage.cs:16-17`) может указывать на удалённое сообщение; `render()` в `chat.js:44` ждёт `m.replyToText` — сервер должен тянуть предка через `IgnoreQueryFilters`, иначе цитата в ответе исчезнет (а сейчас не делает этого).
* `PlayerService.DeleteAsync` (`:27-36`) — soft-delete игрока; `Attendance`, `Subscription`, `SkillAssessment`, `AbsenceNotice` остаются. Все запросы к ним фильтруются отдельно, поэтому «мёртвые» игроки пропадают из отчётов, но суммы оплат (`DashboardService`) при подсчёте по `Subscriptions` без join на `Players` могут их учитывать.
* `CoachTrainingService.cs:270,281` и `ParentService.cs:85`, `PlayerCabinetService.cs:118` — удаляют отсутствия/оценки; повторное создание `Attendance`/`AbsenceNotice` через `Add` даст дубликат (уникального индекса нет, см. A1.1), а «оживление» через `IgnoreQueryFilters` нигде не реализовано.
* FK, которые физически помешают мягкому удалению, — только `Attendance.SubscriptionId` (`Restrict` в `AttendanceConfiguration`, в БД FK без `onDelete` → `NO ACTION`): если подписку удалить «по-настоящему», вставка/обновление посещаемости упадёт. Сейчас спасает soft-delete, но защита случайна, а не спроектирована.

## A4. Гигиена миграций

* **Количество:** 15 миграций (`Core\Migrations\*` без `.Designer`) + снапшот 1507 строк.
* **История:**
  | Миграция | Оценка |
  |---|---|
  | `Init` | 534 строки, создаёт всё; таблица планов названа `TrainingPlan` (ед. ч.) |
  | `PlayerUserAccount`, `MustChangePassword`, `AddSchedule`, `CoachTrainingNotes`, `ParentModule`, `SubscriptionService` | осмысленные |
  | `Correct` (`:13-36`) | переименование `TrainingPlan`→`TrainingPlans` — имя неинформативно |
  | `addUserProfile` | добавляет 6 колонок в `Users` + 2 в `Coaches` |
  | `addUserProfileUpdate` | **пустая** (`Up`/`Down` без операций) |
  | `addUserProfileUpdate2` | создаёт `ParentProfiles` — то, что должно было быть в `addUserProfile` |
  | `addPostAndChat`, `TrainingAssessment` | осмысленные |
  | `AddConfOne` | **пустая** |
  | `AddConfTwo` | приводит `Players` к `PlayerConfiguration` (MaxLength, FK, unique UserId, `IX_Players_LastName_FirstName`) |

  Вывод: минимум 3 миграции — «мусор» (2 пустых + переименование), одна миграция `addUserProfileUpdate2` на 2 минуты позже `addUserProfileUpdate` — следствие поспешных правок. Для дипломного проекта это нормальный «рабочий» трек, но для защиты стоит объединить в 3-5 осмысленных миграций.
* **Снапшот vs сущности:** `ContextAuthModelSnapshot.cs:20` — `ProductVersion 9.0.19`, тогда как `Core.csproj` ссылается на EF 8.0.30. Модель времени выполнения **отличается** от снапшота по всем пунктам A1.1 (индексы, exactness, enum-as-int, fullcalendar… — точнее, `Payments.Amount`, строковые enum'ы, составные индексы). Это значит `dotnet ef migrations has-pending-model-changes` покажет расхождения, а следующая миграция «откатит» БД к неверному виду.
* **`DbInitializer`** использует синхронный `data.SaveChanges()` (`Core\DbInitializer.cs:141`) — единственный синхронный вызов в решении. Для `TrainingPlan`/`Skill` это работает (значения `CreatedAt`/`IsDeleted` проставлены вручную, `Id` — default SQL), но:
  * `IsDeleted = false` приходится писать явно (иначе дефолт `bool` — тоже `false`, но это индикатор того, что автор не доверяет переопределению `SaveChangesAsync`);
  * любой будущий синхронный `SaveChanges` в обход переопределения даст физическое удаление и незаполненные аудит-поля;
  * `Skill`-сид дублирует описания (`DbInitializer.cs:126-127` — «Пас» и «Удар» оба «Использование мяча»), что видно в UI как подсказки `title` в оценках (`coach-assess.js:15`).
* **`await context.Database.MigrateAsync();` закомментирован** (`UI\Program.cs:217`). Значение для деплоя:
  * при первом запуске на чистом PostgreSQL `DbInitializer.Initialize` упадёт (нет таблиц `Roles`/`Users`) → приложение не стартует с `DbUpdateException`/`PostgresException 42P01`;
  * `app.UseMigrationsEndPoint()` (`Program.cs:226`) доступен только в Development, поэтому в Production нет ни автоматического накатывания, ни UI для миграций;
  * схема зависит от ручного `dotnet ef database update` с машины разработчика — на защите диплома это скрытый шаг развёртывания. Правильнее либо вернуть `MigrateAsync()` в guarded-виде, либо оформить SQL-скрипт (`dotnet ef migrations script`) как артефакт.
* **Пароль БД и ключ JWT** в открытом виде (`UI\appsettings.json:4-5,11`) и в git — стоит вынести в User Secrets/переменные окружения (в `UI.csproj:8` уже есть `UserSecretsId`).

## A5. Перечисления, модель подписки/периодов, консистентность сидов

* Все 9 enum'ов объявлены с явными значениями (`Core\Enums\*.cs`) — **плюс**: стабильные контракты. Но:
  * `PlanType { Period=0, Visits=1 }` (`PlanType.cs:9-12`) — 🔴 **отсутствует `Visits = 1`**? Нет, он есть; но `PeriodUnit { Month=1, Quarter=3, Year=12 }` (`PeriodUnit.cs:9-12`) использует значения как «число месяцев» — это неявное соглашение, нигде не задокументированное; `SubscriptionService.cs:49` делает `from.AddMonths((int)plan.Period!)` — работает, но `Period` — nullable, а `!` подавляет проверку; если у `Visits`-плана `Period` не задан (а в сиде он **задан**, см. ниже), будет `NullReferenceException`.
  * Enum-контракты дублируются на клиенте: `PostAudience.Everyone/Authenticated/Parents/Coaches` (`PostAudience.cs:11-14`), `PostType.Announcement/News/Holiday/Competition` (`PostType.cs:11-14`) — но `posts.js`/`feed.js` дублируют эти значения числовыми словарями (`feed.js:4-9`, `post.js:3-4`), то есть клиент и сервер связаны неявно; при переупорядочивании enum UI сломается без ошибки компиляции.
  * `AbsenceReason.Unknown=0` (нет «null-значения»); `Attendance.Reason` — `AbsenceReason?` (`Attendance.cs:21`), а `AbsenceNotice.Reason` — non-nullable (`AbsenceNotice.cs:16`) — при отсутствии причины записывается `Unknown` (0), и в UI `coach-trainig.js:4` `Unknown: 'без причины'`. Логически ок, но два разных подхода к «нет причины» в соседних сущностях.
  * `SubscriptionStatus.Frozen=3` (`SubscriptionStatus.cs:14`) есть, а в `TrainingStatus` нет «перенесена» — перенос реализован сдвигом `StartsAt` (`ScheduleService.cs:221`), история переносов теряется.
* **Консистентность сидов `DbInitializer.cs:100-116`:**

  | План | Type | Period | Visits | VisitsValidDays | Price | Проблема |
  |---|---|---|---|---|---|---|
  | «Месяц» | Period | Month | — | 30 | 80 | ок |
  | «3 месяца» | Period | Quarter | — | 90 | 200 | ок (цена ниже за месяц, чем тариф «Месяц»: 66.7/мес — вероятно, задумано) |
  | «Год» | Period | Year | — | 365 | 600 | 50/мес — тоже скидка, ок |
  | «8 занятий» | Visits | **Month** | **null** | 30 | 80 | 🔴 `Visits` не заполнен! `SubscriptionService.cs:58` тогда `TrainingsLimit = null` (безлимит), а UI (`cabinet.js:93`) покажет «${p.visits} занятий» = «null занятий». Также `Period` заполнен для типа `Visits`, хотя `TrainingPlan.cs:15` документирует «для Period». |
  | «16 занятий» | Visits | **Quarter** | **null** | 60 | 200 | то же + `VisitsValidDays=60`, а `Period=Quarter` (несогласованно) |

  Дополнительно: `SortOrder` у всех 5 планов = `0` (не задан) → порядок в `GetPlansAsync` (`SubscriptionService.cs:24`) недетерминирован, а `cabinet.js:87-98` рисует карточки в порядке выдачи, то есть тарифы «прыгают».
* **Валюта:** `DbInitializer.cs:104-116` цены в «80/200/600», `SubscriptionService.cs:75` — «₽», `cabinet.js:92` — «₽», `dashboard.js:3` — «Br», `subscriptions.js:6` — «Br». Для Беларуси (часовой пояс Минск) корректно «Br» — но три разных подписи в одном продукте.
* **`VisitsValidDays` vs `TrainingsLimit`:** для `Visits`-планов срок действия считается от `From` (`SubscriptionService.cs:49`), а не от даты первого занятия; при «заморозке» (`Frozen`) срок не продлевается (`SubscriptionService.cs:115-231` — надо проверить `SetStatusAsync`) → занятия «сгорают» во время заморозки. Для дипломной работы это логическая дыра в бизнес-правиле.

---

# Часть B — Фронтенд

Объём: 38 `.cshtml` (2460 строк), 24 прикладных `.js` (2192 строки), 12 инлайновых `<script>`-блоков, `wwwroot` с 8 библиотеками в 3 разных схемах хранения.

## B1. Библиотеки: дубли, версии, CDN vs локально

| Библиотека | Версии в проекте | Где подключается | Проблема |
|---|---|---|---|
| FullCalendar | 6.1.10 и 6.1.15 | `_Layout.cshtml:242` → 6.1.10; файл 6.1.15 лежит мёртвым грузом | два бандла по ~300 КБ |
| FullCalendar CSS | ожидается `~/css/fullcalendar@6.1.10/main.min.css` | `_Layout.cshtml:23` — **закомментировано**, файла нет | 🟠 календарь без стилей (в `wwwroot\css` только bootstrap, font-awesome, tabulator) |
| Tabulator JS | 6.5.0 (layout) и 6.3.0 (не используется) | `_Layout.cshtml:238` | дубль |
| Tabulator CSS | 6.2.0 (layout `:19`) и 6.3.0 (`Player\Index.cshtml:15`) | — | 🟡 JS 6.5 + CSS 6.2 в общем случае |
| jQuery | `js\jquery-3.7.0.min.js` + `lib\jquery\dist\jquery.min.js` | `_Layout.cshtml:232` | дубль (в `lib` — только для validation) |
| Bootstrap | `js\framework\bootstrap@5.3.0\bootstrap.bundle.min.js` + `lib\bootstrap\dist\js\*` | `:234` | дубль, `lib\bootstrap\dist\js\bootstrap.min.js` не бандл (без Popper) |
| Chart.js | 4.4.4 | `Dashboard\Index.cshtml:44` | ок, но грузится на одной странице |
| moment.js | — | `_Layout.cshtml:245` | 🟡 не используется нигде (проверено grep) |
| SignalR | `js\framework\signalr.min.js` без версии | `Chat\Index.cshtml:96` | версия неизвестна, обновление неконтролируемо |

CDN против локального: **все CDN-ссылки закомментированы, используются локальные копии** (`_Layout.cshtml:10,14,18,22,231,233,237,241`; `Groups\Index.cshtml:11,102`; `Users\Index.cshtml:10,107`; `Venues\Index.cshtml:8,76`; `Subscriptions\Index.cshtml:8,80`; `Player\Index.cshtml:14,80`; `Coach\Index.cshtml:11,71`; `Chat\Index.cshtml:95`; `Schedule\Index.cshtml:98,101-103`; `Coach\CoachTraining.cshtml:23-24`; `Parent\Schedule.cshtml:45-46`; `Parent\Progress.cshtml:36`; `Parent\Attendance.cshtml:38`). Это **сильная сторона** (офлайн-демонстрация на защите, нет внешних зависимостей), но оставленные закомментированные CDN-дубли мешают читаемости и провоцируют рассинхрон версий.

## B2. Инлайновый JS в представлениях (12 блоков)

* `_Layout.cshtml:281-298` — логика модального окна смены пароля (fetch + JSON) — 18 строк inline, при этом `:275` ссылается на `document.getElementById('logoutForm')`, которого в разметке **нет** (форма выхода — `:189-193` без `id`) → кнопка «Выйти» в модалке не работает.
* `Account\Register.cshtml:120-152` — toggle пароля + дубль HTML5-валидации (при `novalidate`, `:18`).
* `Account\Login.cshtml:71-87` — тот же toggle (копипаста из Register).
* `Manager\AllUsers.cshtml:48-270` — ~220 строк inline-JS на jQuery, устаревший модуль; внутри вызываются `deletePlayer`/`editPlayer` (`:72,124,127`), определённые только в комментарии (`:252`) → `ReferenceError` при клике.
* Мелкие: `Chat\Index.cshtml:97` (`window.chatInit`), `Coach\Training.cshtml:66` (`window.trainingId`), `Schedule\Index.cshtml:104` / `Post\Index.cshtml:140` (`window.schedulePage`), `Parent\Progress.cshtml:37` / `Parent\Attendance.cshtml:39` (`window.playerId`), `Venues\Index.cshtml:77-79`, `Player\Index.cshtml:81-83`.
* Плюс: значения передаются через JSON-подобный `window.*` и **не** интерполируются в HTML — это лучше, чем `@Html.Raw`. `Html.Raw` в представлениях **не используется вообще** (0 совпадений) — сильная сторона.
* Однако `window.chatInit` (`Chat\Index.cshtml:97`) вставляет `'@User.FindFirst(...)'` внутрь одинарных кавычек JS без `JavaScriptEncoder` — при экзотическом claim это потенциальный JS-injection; значения claim'ов GUID, риск низкий, но паттерн опасный.

## B3. Валидация

* DataAnnotations есть только в 3 файлах (`LoginViewModel`, `RegisterViewModel`, `ChangePasswordViewModel`) — 13 атрибутов на весь `UI\Models\ViewModels`. Все DTO для API (`PlayerEditDto`, `PostEditDto`, `GroupEditDto`, `VenueDto`, `UpdateProfileRequest`, `BlockDto`, `ChangeRoleDto`, `CreateManagerDto`) не имеют атрибутов → `[ApiController]`-автомодельстейт ничего не проверяет, всё держится на ручных проверках.
* Ручные проверки есть и неплохие: `PlayersApiController.ValidateAsync` (`:105-117` — ФИО, дата рождения 0..18 лет), `PostsApiController.Create` (`:34` — заголовок/тело), `ScheduleService.ValidateAsync` (`:247-263` — конец > начала, ≤6 ч, архив группы, активное место, матч без серии, дни недели).
* Пробелы: `CreateManagerDto` проверяется только `EmailAddressAttribute` (`UsersApiController.cs:97-98`), длина `FirstName/LastName` не валидируется, `VenueDto.Capacity`, `PaymentMethod`, `amount` в подтверждении оплаты (`subscriptions.js:111-115`) не проверяются на сервере.
* Клиентская валидация: `_ValidationScriptsPartial` подключён **только** в `Login.cshtml:70` и `Register.cshtml:119`; остальные формы — `checkValidity()` в JS, но:
  * `users.js:84` (`profileForm`) — `preventDefault()` **без** `checkValidity()` → пустые ФИО уйдут на сервер;
  * `chat.js:80` и `profile.js:74` (`pwdForm`) — без `checkValidity()`;
  * `post.js:186` — проверка есть;
  * `schedule.js:121`, `coaches.js:55`, `groups.js:201`, `cabinet.js:33`, `player.js:123`, `venue.js:147`, `subscriptions.js:103,155`, `users.js:118` — проверки есть.
* `targetFramework net8.0` + `UI.csproj:25-29`: `Microsoft.Extensions.Options 10.0.11` — ещё один пакет из будущей мажорной ветки.

## B4. XSS-риски (`innerHTML` с серверными данными)

Экранирование реализовано локально в 3 файлах: `chat.js:5`, `site.js:67-68`, `feed.js:13-14`. В остальных — «сырой» `innerHTML`:

| Файл:строка | Что вставляется | Источник |
|---|---|---|
| `subscriptions.js:46` | `parentComment`, `managerComment` | ввод родителя и менеджера |
| `subscriptions.js:96-99` | `playerName`, `planName`, `parentComment` | ввод родителя |
| `coach-trainig.js:24-40` | `lastName`, `firstName`, `comment` (в `value="…"`) | ввод родителя/тренера |
| `coach-assess.js:15-19` | `s.description` в `title="…"`, `p.name`, `p.comment` в `value="…"` | справочник + родитель |
| `dashboard.js:9` | `a.text`, `a.link` (в `href`) | формируется сервером, но включает имена/группы |
| `parent-progress.js:28,45` | `s.name`, `a.coachName`, `a.comment` | комментарий тренера |
| `parent-schedule.js:14,41` | `c.name`, `c.color` (в `style`), данные события | имена детей, `Group.Color` |
| `parent-attendance.js:35` | строки посещаемости | комментарии тренера |
| `player-schedule.js:22` | список занятий | названия групп/мест |
| `player-cab.js:16,21` | `d.lastHighlights` | заметки тренера |
| `coach-schedule.js:52` | расписание | — |
| `users.js:75` | `linkedPlayer`, счётчики | имена игроков |
| `groups.js:77` | `c.getValue()` в `style="background:…"` | `Group.Color` (валидируется только `maxlength(7)`, не паттерном) |
| `profile.js:42` | `pl.groupName` | — |

Конкретный рабочий сценарий: родитель в `Cabinet` создаёт ребёнка с фамилией `<img src=x onerror=fetch('/api/users/'+…)>` (`Cabinet\Index.cshtml:118` + `cabinet.js:39`, сервер сохраняет без санитайза — `PlayerService.Apply` только `Trim()`), затем тренер открывает «Тренировку» → `coach-trainig.js:27` вставляет `p.lastName` в `innerHTML` → скрипт исполняется в сессии тренера. Аналогично `title="${s.description}"` (`coach-assess.js:15`) позволяет вырваться из атрибута через `"`.

`Groups\Print.cshtml:85-88` использует Razor-экранирование (ок), `Home\Index.cshtml:35` — `alt=""` без подстановки (ок).

## B5. Сломанные ссылки и отсутствующие файлы

* `_Layout.cshtml:22-23` — CSS FullCalendar закомментирован, файла `wwwroot\css\fullcalendar*` нет → 🟠.
* `_ValidationScriptsPartial.cshtml:1-2` ссылается на `~/lib/jquery-validation/...` и `~/lib/jquery-validation-unobtrusive/...` — файлы существуют (проверено листингом `wwwroot`), но без `asp-append-version` → проблемы с кэшем при обновлении.
* `Manager\AllUsers.cshtml:72,124,127` — `deletePlayer`/`editPlayer` не определены.
* `_Layout.cshtml:275` — `logoutForm` не существует.
* `parent-progress.js:24` — некорректный селектор → JS-исключение, после которого **весь** блок построения радара/комментариев не выполняется (строка 24 после Chart на `:21` выполняется уже после построения радара, но таблица и `avgLine` не заполняются).
* `Groups\Index.cshtml:169-170` — прямые ссылки `/Groups/Print/{id}?mode=journal` — контроллер `GroupsController` требует `Manager`; для тренера кнопка приведёт к 403 (в представлении нет проверки роли).

## B6. Accessibility

* `<html lang="en">` (`_Layout.cshtml:5`) при полностью русском интерфейсе — скринридеры читают текст с английской фонетикой.
* `<label>` без `for` и `<input>` без `id`: `Cabinet\Index.cshtml:118-122`, `Venues\Index.cshtml:49-53`, `Parent\Index`-модалки, `My\Schedule.cshtml:22`, `Player\Index.cshtml:56-62`, `Groups\Index.cshtml:64-71`, `Post\Index.cshtml:33-37`, `Profile\Index.cshtml:46-53`. Клик по подписи не фокусирует поле.
* Дубли `id="chkActive"` в одном документе (`Venues\Index.cshtml:57` и `:60`) — невалидный HTML, `for="chkActive"` работает только для одного.
* Изображения: `Home\Index.cshtml:35` `alt=""` (динамическое изображение поста — нужен `alt` с заголовком), `feed.js:55` `<img src=…>` вообще без `alt`; `Profile\Index.cshtml:14` `alt=""` для аватара.
* Фильтры-инпуты без подписей и `aria-label`: `Parent\Attendance.cshtml:12` (`type="date"` × 2), `Groups\Index.cshtml:26`, `Subscriptions\Index.cshtml:32`, `Users\Index.cshtml` поиск.
* Кнопки-иконки без `aria-label`/`title`: `Chat\Index.cshtml:71,79` (есть `id`, нет подписи), `Cabinet\Index.cshtml:44,88,89`, `Groups\Index.cshtml:44,48`.
* Положительно: модальные окна Bootstrap 5 с `data-bs-dismiss`, `aria-expanded` у дропдаунов, `aria-label="Уведомления"` (`_Layout.cshtml:151`), `role="alert"` у summary валидации (`Login.cshtml:18`), `role="group"` у radio-групп, `autocomplete` у паролей (`_Layout.cshtml:261,265,269`, `Profile\Index.cshtml:31-33`), `minlength`/`maxlength` у части полей.

## B7. Прочее по фронтенду

* Дублирование кода: `Login.cshtml:71-87` и `Register.cshtml:120-152` — идентичный toggle пароля; `player-schedule.js`/`parent-schedule.js`/`coach-schedule.js` — три почти одинаковых инициализации FullCalendar.
* `console.log` в продакшене: `groups.js:21,25,40,57,58`, `subscriptions.js:17,90,109`, `users.js:17`.
* `confirm()`/`prompt()`/`alert()` вместо модальных окон: `chat.js:64,65`, `groups.js:60,179,226`, `subscriptions.js:127,136`, `cabinet.js:22,166,179,187,194`, `users.js:62,93,101,108`.
* Локализация: смесь русского и английского (`Login.cshtml:35` `placeholder="Enter your password"`, `Account` заголовок «Регистрационные данные»).
* Навигация не покрывает роли: нет ссылки на кабинет игрока (`MyController`, `Views\My\*`) и на «Прогресс/Посещаемость» родителя (`Views\Parent\*`) в `_Layout.cshtml:47-171` — доступ только по прямому URL.
* `window.schedulePage` объявлен и в `Schedule\Index.cshtml:104`, и в `Post\Index.cshtml:140` (копипаста имени), `post.js` его не использует.
* `IsStaff`/роли: `ROLE_RU` в `users.js:2-7` знает только 4 роли (совпадает с `DbInitializer.cs:28`), а `cabinet.js`/`chat.js:4` — ещё `Admin`; в системе роли `Admin` нет — мёртвая ветка.

---

# Сильные стороны

1. **Продуманная доменная модель**: разделение `Player`/`Coach`/`ParentProfile`, отдельные `TrainingPlan`/`Subscription`/`Payment`, `SkillAssessment`/`SkillScore`, `Attendance` с `AbsenceReason`, `SeriesId` для повторяющихся тренировок — модель покрывает реальный бизнес-процесс академии.
2. **Мягкое удаление** реализовано централизованно в `BaseEntity` + переопределение `SaveChangesAsync` (`ContextAuth.cs:116-137`) — правильный паттерн, а не разрозненные `IsDeleted` в каждом сервисе.
3. **UUID + `gen_random_uuid()`** как ключи (`BaseEntity.cs:15`, `ContextAuth.cs:106`) — безопасно для распределённых сценариев; `timestamp with time zone` для всех аудит-полей.
4. **Хорошие серверные проверки в критичных местах**: `ScheduleService.ValidateAsync` (`:247-263`) — шесть независимых правил, включая защиту от матча «сам с собой» и серии матчей; проверка пересечений по месту/группе/тренеру (`CheckAsync:78-109`) с исключением самого события; запрет прошлых дат в подписках (`SubscriptionService.cs:47`).
5. **Осмысленное использование локальных библиотек** вместо CDN (все внешние ссылки закомментированы) — демонстрация работает офлайн.
6. **XSS-экранирование реализовано там, где риск максимален** — `chat.js:5` (сообщения), `site.js:67-68` (уведомления), `feed.js:13-14` (публикации); `@Html.Raw` не используется ни разу.
7. **EF-индексы продуманы на уровне конфигураций** (составные индексы под календарь, подписки, посещаемость) — даже если не применяются, замысел верный; уникальные индексы `Players.UserId`, `Coaches.UserId`, `TrainingGroups.Name` (с фильтром), `Venues.Name`, `Users.Email/NormalizedEmail` защищают от дублей.
8. **Локализация Tabulator** (`groups.js:98-137`) и русские форматы `toLocaleDateString('ru-RU')` — интерфейс корректен для целевой аудитории.
9. **Разделение слоёв** `Core` (сущности, репозитории, UoW, конфигурации) / `UI` (сервисы, контроллеры, представления) — правильная направленность зависимостей, `IUoW`/`IGenericRepo` дают тестируемость.
10. **JWT + Cookie «умная» схема** (`Program.cs:179-205`) с корректным `ClockSkew`, `ValidateIssuer/Audience/Lifetime`, и обработкой 401/403 для `/api/*` вместо редиректа (`:143-162`) — грамотное решение для смешанного SPA/Razor-фронтенда.
11. **`MustChangePasswordFilter`** (`UI\Filters\MustChangePasswordFilter.cs`, `_Layout.cshtml:250-299`) — продуманный сценарий временных паролей для детских аккаунтов, включая принудительную модалку и безопасную передачу паролей (показ один раз).
12. **Фоновый `SubscriptionExpiryWorker`** (`UI\Models\SubscriptionExpiryWorker.cs`) с часовым интервалом — закрывает истечение абонементов без ручных действий.

---

# Приоритеты исправления

**Сначала (Critical):** #1 (применить все конфигурации через `ApplyConfigurationsFromAssembly` и пересоздать миграцию), #2 (вернуть `[Authorize(Roles="Manager")]`), #3 (включить antiforgery для cookie-API), #5 (выровнять версии EF/Npgsql на 8.0.x).

**Затем (Major):** #6 (`MigrateAsync` или скрипт), #7-8 (единый часовой пояс через `AppTime.Tz()` и конфиг; `DateTimeOffset` в DTO), #9 (единая функция `esc()` и отказ от `innerHTML` для серверных данных; CSP), #10 (`Admin`→`Manager`), #11 (селектор), #13 (секреты).

**Далее (Medium):** #15-16 (`IgnoreQueryFilters` для админ-сценариев, отдельные «архивные» представления), #21 (сиды: `Visits`, `SortOrder`, валюта), #22-26 (валидация DTO, дедупликация библиотек, момент, доступность).

Полный отчёт сохранён в `AUDIT_DATA_FRONTEND.md`.
