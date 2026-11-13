# Backend Structure Guide

Памятка: куда добавлять код серверной части StudyGraph.
Корень проекта — `backend/StudyGraph/`, решение — `backend/StudyGraph.sln`.
Все пути ниже указаны относительно корня серверного проекта.

Сейчас файлы модуля задач и прогресса — заготовки: классы пустые, методы выбрасывают `NotImplementedException`. Они не подключены к приложению. PostgreSQL, MongoDB, JWT и RabbitMQ ещё предстоит настроить.

## Хочешь добавить сущность?

Создай класс в `Domain/Entities/`: например, `User.cs`, `StudyTask.cs` или `RefreshToken.cs`. Здесь описываются данные и правила предметной области. Не размещай здесь обработчики HTTP-запросов и код RabbitMQ.

`StudyTask.cs` уже создан как заготовка. Название StudyTask помогает избежать путаницы с системным типом Task.

## Хочешь добавить запрос или ответ API?

Запросы размещай в `Application/DTOs/Requests/`: например, `RegisterRequest.cs`, `LoginRequest.cs`, `CreateStudyTaskRequest.cs`.
Ответы — в `Application/DTOs/Responses/`: например, `AuthResponse.cs`, `StudyTaskResponse.cs`, `ProgressReportResponse.cs`.

DTO описывают внешний контракт API. Не возвращай клиенту сущность пользователя с паролем или хешем пароля.

## Хочешь добавить сервис?

1. Определи контракт в `Application/Services/Interfaces/`, например `IProgressService.cs`.
2. Реализацию размести в `Application/Services/Implementations/`, например `ProgressService.cs`.
3. Зарегистрируй сервис в `Program.cs` до `builder.Build()`, например через `builder.Services.AddScoped<IProgressService, ProgressService>()` с нужными using.

В сервисах размещай расчёт взвешенного прогресса, формирование отчёта и сценарии работы с задачами. Авторизацию и обновление токенов можно выделить в отдельный AuthService. Текущий ProgressService пока не реализован; его сигнатуры можно уточнить при разработке.

## Хочешь добавить репозиторий?

1. Контракт — в `Infrastructure/Persistence/Repositories/Interfaces/`.
2. Реализация — в `Infrastructure/Persistence/Repositories/Implementations/`.
3. Зарегистрируй зависимость в DI после подключения базы данных.

Например, `IStudyTaskRepository.cs` и `StudyTaskRepository.cs` уже существуют. Репозиторий отвечает за чтение и запись данных; расчёт прогресса размещай в сервисе. В этом проекте интерфейсы репозиториев находятся именно в Infrastructure, а не в Domain.

## Хочешь добавить endpoint?

Создай файл в `API/Endpoints/`: например, `AuthEndpoints.cs`, `StudyTaskEndpoints.cs` или `ProgressEndpoints.cs`. Обработчик принимает запрос, вызывает сервис и возвращает HTTP-ответ.

Подключай маршруты в `Program.cs` после `builder.Build()` и до `app.Run()`. Текущий `ProgressEndpoints.MapProgressEndpoints(IEndpointRouteBuilder endpoints)` — обычный статический метод-заготовка, а не extension method: после реализации его можно вызвать как `ProgressEndpoints.MapProgressEndpoints(app)` с нужным using. Пока метод выбрасывает исключение, подключать его нельзя. Для записи `app.MapProgressEndpoints()` сначала нужно оформить метод расширения с параметром `this IEndpointRouteBuilder`.

## Хочешь добавить проверку данных или преобразование?

Проверки входных DTO размещай в `Application/Validators/`, например в `CreateStudyTaskValidator.cs`. Преобразования сущностей в DTO — в `Application/Mappings/`, например в `ProgressReportMapping.cs`.

Эти классы сами по себе не выполняются: их нужно вызывать из соответствующего сценария или подключить выбранный механизм валидации. Библиотеки валидации и маппинга для заготовок не требуются.

## Хочешь подключить PostgreSQL или изменить схему?

DbContext размещай в `Infrastructure/Persistence/`, настройки сущностей — в `Configurations/`, настоящие миграции EF Core — в `Migrations/`.

`InitialProgressSchema.cs` сейчас обычная заглушка, а не миграция EF Core. Перед созданием настоящей начальной миграции убери или замени эту заглушку. Сначала подключи EF Core и PostgreSQL-провайдер, настрой контекст и строку подключения; затем создавай миграции инструментами EF Core. Таблицы users и tasks пока не созданы.

Секреты храни в .NET User Secrets или переменных окружения. В `appsettings.json` и `appsettings.Development.json` оставляй общие настройки без паролей и токенов.

## Хочешь подключить RabbitMQ, worker или MongoDB?

Таких папок и реализаций сейчас нет. По мере выполнения задач можно добавить `Infrastructure/Messaging/` для RabbitMQ, `Infrastructure/Workers/` для фонового обработчика и отдельный каталог внутри Persistence для MongoDB.

Сначала согласуй контракт события TopicCompleted, его идентификатор и правила повторной обработки. Интерфейс публикации, нужный сервисам приложения, можно разместить в `Application/Services/Interfaces/`, реализацию — в Infrastructure. Историю прогресса записывай через обработчик событий. Не подключай незавершённые заглушки к запуску приложения.

## Хочешь добавить тесты?

Серверного тестового проекта пока нет. Создай отдельный проект рядом с основным и добавь его в решение. Покрой расчёт прогресса модульными тестами, роли и авторизацию — тестами API, дедлайны и повторную обработку сообщений — соответствующими сценариями. Тесты работы с БД и брокером требуют отдельного тестового окружения.

## Что проверить перед коммитом?

Из корня репозитория: `dotnet build backend/StudyGraph.sln`. После добавления тестового проекта запускай `dotnet test backend/StudyGraph.sln`.

Не коммить bin, obj и секреты. Пустые папки Git не хранит; текущие заготовки позволяют сохранить структуру. Не считай наличие файла признаком готовой функциональности.
