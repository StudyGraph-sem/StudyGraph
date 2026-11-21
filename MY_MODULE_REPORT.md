# Мой модуль: авторизация StudyGraph

## Состояние и результат

На 21.11.2026 этап backend-авторизации завершён. До изменений Git-статус был чистым. Уже существовали User, конфигурация users с уникальным индексом email, AppDbContext, IUserRepository/UserRepository, регистрация с нормализацией email и PasswordHasher, вход, JWT на 15 минут, проверка issuer/audience/signature/lifetime, endpoint /me, CORS и DI. Проверены DTO, JWT-настройки и исходная миграция InitialUsers; работающий код сохранён.

Добавлены refresh/logout, ротация и отзыв сессии, проверка Admin, CSRF-проверка для операций с cookie и ограничение частоты запросов. Контракт IUserRepository перемещён из Infrastructure в Application/Repositories/Interfaces без смены namespace и без дублирования. Исправлены nullable-предупреждения User. При неизвестном email выполняется проверка фиктивного хеша пароля, чтобы уменьшить разницу времени ответа. JwtTokenService выдаёт токены только с ролями User или Admin. Публичная регистрация всегда назначает User; API назначения Admin не добавлялся.

## Новые типы

| Тип | Назначение |
| --- | --- |
| Domain/Entities/RefreshToken | Запись сессии: пользователь, SHA-256 хеш токена, семейство, абсолютный срок и дата отзыва |
| Infrastructure/Persistence/Configurations/RefreshTokenConfiguration | Таблица refresh_tokens, уникальность хеша, индекс семейства и внешний ключ users |
| Application/Repositories/Interfaces/IRefreshTokenRepository | Контракт сохранения, атомарной ротации и отзыва |
| Infrastructure/Persistence/Repositories/Implementations/RefreshTokenRepository | Реализация EF Core/PostgreSQL, транзакции и сериализация операций пользователя |
| Application/Services/Interfaces/IRefreshTokenService | Контракт сценариев создания сессии, refresh и logout |
| Application/Services/Implementations/RefreshTokenService | Криптографическая генерация, хеширование и получение новых access tokens |
| Application/DTOs/Responses/RefreshSession | Внутренний результат сервиса: сырой refresh token для установки cookie, срок и AuthResponse; целиком клиенту не сериализуется |
| Миграция AddRefreshTokens | Добавляет таблицу, индексы и FK; существующие users не изменяет |
| StudyGraph.AuthTests/AuthTests | Интеграционные проверки через настоящий HTTP pipeline и PostgreSQL |
| StudyGraph.AuthTests/JwtTokenTests | Unit-тесты состава JWT и срока действия |
| partial Program | Точка доступа WebApplicationFactory к приложению; собственного поведения не добавляет |

## Новые публичные методы

Во всех async-методах cancellationToken отменяет операцию и передаётся в EF Core. Интерфейсы и соответствующие реализации имеют одинаковые сигнатуры.

| Метод | Параметры → результат | Назначение и место вызова |
| --- | --- | --- |
| RefreshTokenConfiguration.Configure | EntityTypeBuilder<RefreshToken> → void | Настраивает модель; вызывается AppDbContext через ApplyConfigurationsFromAssembly |
| IRefreshTokenRepository / RefreshTokenRepository.AddAsync | RefreshToken, CancellationToken → Task | Сохраняет хеш новой сессии; вызывается CreateAsync |
| IRefreshTokenRepository / RefreshTokenRepository.RotateAsync | string hash, RefreshToken replacement, CancellationToken → Task<RefreshToken?> | В транзакции меняет токен; null означает неизвестный, истёкший или повторно использованный токен; вызывается RefreshAsync сервиса |
| IRefreshTokenRepository / RefreshTokenRepository.RevokeAsync | string hash, CancellationToken → Task | Отзывает всё семейство найденного токена; вызывается LogoutAsync |
| IRefreshTokenService / RefreshTokenService.CreateAsync | AuthResponse, CancellationToken → Task<RefreshSession> | Создаёт независимую сессию после успешного входа; вызывается login endpoint |
| IRefreshTokenService / RefreshTokenService.RefreshAsync | string token, CancellationToken → Task<RefreshSession?> | Хеширует сырой токен, запускает ротацию, получает актуального пользователя и новый JWT; вызывается refresh endpoint |
| IRefreshTokenService / RefreshTokenService.LogoutAsync | string token, CancellationToken → Task | Передаёт хеш в репозиторий для отзыва; вызывается logout endpoint |

AuthEndpoints.MapAuthEndpoints существовал ранее; теперь дополнительно регистрирует /refresh, /logout и /admin. Новые обработчики endpoint и вспомогательные методы закрытые. У RefreshToken и RefreshSession нет собственных публичных операций кроме доступа к данным. У миграции Up/Down — защищённые методы EF Core, а не публичный API приложения.

Тестовые InitializeAsync/DisposeAsync вызываются xUnit для создания/удаления только собственной уникальной схемы auth_test_<GUID>. Публичные тестовые методы RegistrationLoginAndAuthorization, RotationReplayAndLogout, ConcurrentRefreshRevokesFamily, ExpirationValidationCsrfAndRateLimit, DatabaseRejectsDuplicateEmail, RejectsInvalidJwt и GeneratesSubjectRoleAndExpiration вызываются xUnit и проверяют соответствующие сценарии; результат — Task или void и assertions. Параметр failure задаёт вид испорченного JWT, role — User/Admin.

## Сценарии HTTP

- POST /api/auth/register: проверка полей, trim/lowercase email, проверка дубликата, хеширование ASP.NET Core PasswordHasher и сохранение. Ответ 201; ошибки данных 400; занятый email 409, включая конфликт уникального индекса PostgreSQL.
- POST /api/auth/login: проверка Origin и CSRF-заголовка, проверка длины полей, поиск нормализованного email, проверка пароля. Неверные учётные данные дают одинаковый 401. Успех: 200 с accessToken/expiresAt/user и refresh cookie. Пароль и его хеш в ответ не включаются.
- GET /api/auth/me: Bearer JWT, затем поиск пользователя по sub. Без корректного токена 401, отсутствующий пользователь 404, успех 200.
- GET /api/auth/admin: проверка роли Admin; без токена 401, с ролью User 403, Admin получает 200. Это минимальный endpoint проверки роли, без пользовательских административных операций.
- POST /api/auth/refresh: cookie, Origin и CSRF-заголовок. Успех 200 с новым access token и новой cookie, невалидный/истёкший/отозванный refresh — 401, CSRF — 403.
- POST /api/auth/logout: те же Origin и CSRF-заголовок, отзыв семейства и удаление cookie, 204. Повторный logout допустим. Выход относится к одной сессии, а не ко всем устройствам.

На register/login/refresh действует общий fixed-window лимит 10 запросов в минуту на IP, без очереди; превышение даёт 429. Подмена X-Forwarded-For не используется для определения IP. При будущем reverse proxy потребуются доверенные forwarded headers и согласованная политика лимитов; текущая защита рассчитана на один экземпляр API.

## Refresh tokens и конкуренция

Генератор RandomNumberGenerator создаёт 48 случайных байт, передаваемых в Base64. В БД хранится только SHA-256 хеш. Это стандартные .NET криптографические средства, собственного алгоритма нет.

Сессия действует максимум 7 дней от входа. Ротация сохраняет FamilyId и исходный ExpiresAt, поэтому refresh не продлевает сессию бесконечно. При каждом успешном refresh старый токен помечается отозванным и создаётся замена.

Операции refresh/logout используют транзакцию и блокировку строки пользователя через атомарный no-op UPDATE роли. Вторая операция ждёт завершения первой и повторно читает токен. При конкурентном refresh один запрос успешен; второй обнаруживает повторное использование и отзывает всё семейство, включая только что выданную замену. Клиент должен выполнять refresh один раз для всех ожидающих запросов, а не параллельно. Неизвестный токен не позволяет определить семейство; для него просто возвращается 401. Удалять отозванные записи до окончания сессии нельзя: они нужны для обнаружения повторного использования.

Logout и обнаружение повтора отзывают refresh tokens. Уже выданные access tokens продолжают действовать до истечения 15 минут; немедленный blacklist access tokens в этом этапе не вводился. Изменение роли становится видно в новом JWT.

## Cookie, CSRF и будущий Vue

Cookie называется __Host-StudyGraph.Refresh: HttpOnly, Secure, SameSite=Strict, Path=/, без Domain. Refresh token не возвращается в JSON. Ответы login/refresh имеют Cache-Control: no-store.

Login, refresh и logout требуют заголовки Origin и X-StudyGraph-CSRF: 1. Origin должен точно совпасть с origin самого API либо настройкой Frontend:Origin. По умолчанию для CORS сохранён http://localhost:5173. CORS разрешает credentials только выбранному origin, а не всем сайтам. Одного SameSite недостаточно: проверяются также Origin и нестандартный заголовок, недоступный чужой HTML-форме.

Для браузерной работы с Strict cookie клиент и сервер должны быть same-site, включая HTTPS-схему. Будущий Vue следует запускать по HTTPS и указать его HTTPS origin в Frontend:Origin, либо использовать один HTTPS origin через proxy. В fetch нужен credentials: 'include'; access token отправляется через Authorization: Bearer. Для PowerShell также указывай Origin и X-StudyGraph-CSRF: 1 на login/refresh/logout и сохраняй cookie в WebSession. Клиент в этом этапе не менялся.

Подход к SameSite сверялся с документацией ASP.NET Core: https://learn.microsoft.com/en-us/aspnet/core/security/samesite?view=aspnetcore-10.0 . Rate limiting: https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0 .

## SOLID на конкретных примерах

- SRP: endpoints отвечают за HTTP/cookie; AuthService — проверку учётных данных; JwtTokenService — выпуск JWT; RefreshTokenService — сценарии refresh; репозиторий — транзакционное хранение.
- OCP: новый сценарий реализован отдельным сервисом и репозиторием, регистрация сохранена.
- LSP: реализации используются через соответствующие интерфейсы; их null-результаты имеют определённый смысл.
- ISP: отдельные небольшие контракты JWT, пользователей и refresh tokens, без GenericRepository.
- DIP: сервис refresh зависит от IUserRepository, IRefreshTokenRepository и IJwtTokenService; реализации назначает DI в Program.cs. Один существующий AppDbContext сохранён.

## Проверка и миграции

Выполнены:

1. dotnet test StudyGraph/backend/StudyGraph.sln --no-restore --verbosity minimal — 11/11 пройдено, 0 пропущено. Проверены регистрация и нормализация, повторная регистрация и индекс БД, пароль, роли, JWT subject/срок, неправильные issuer/audience/signature/истёкший JWT, /me и /admin, cookie, ротация, logout, повтор, конкурентный refresh, истечение refresh, CSRF и 429.
2. dotnet ef database update --project StudyGraph/backend/StudyGraph --no-build — применена только новая 20261121135152_AddRefreshTokens. Применённая InitialUsers сохранена.
3. dotnet build StudyGraph/backend/StudyGraph.sln --no-restore — успешно, 0 предупреждений, 0 ошибок.

Первый запуск интеграционных тестов не прошёл из-за выключенного Docker/PostgreSQL на 55432. После запуска Docker Desktop существующий studygraph-postgres поднялся без пересоздания, и тесты прошли. Существующие данные и volumes не удалялись. Тесты создают уникальные схемы и удаляют только их. Подключение берётся из STUDYGRAPH_TEST_CONNECTION либо User Secrets проекта; нужны права CREATE SCHEMA. JWT-ключ для тестового сервера создаётся в памяти. Секреты не выводятся в отчёт.

Обновлены Microsoft.AspNetCore.OpenApi до 10.0.15 и явно согласован Microsoft.EntityFrameworkCore.Relational 10.0.15: предупреждение NU1903 и конфликт версий исчезли. EF CLI остаётся 10.0.5 и предупреждает о более новой runtime 10.0.15, но миграция выполнена успешно.

## Границы этапа

Незавершённых backend-сценариев из этого этапа нет. Frontend, учебные задачи, прогресс, RabbitMQ и MongoDB не изменялись. Для production отдельно потребуются HTTPS-развёртывание с согласованными origin, распределённый rate limiting при нескольких экземплярах, политика очистки истёкших сессий и административное назначение ролей вне публичной регистрации. Email verification, восстановление пароля, MFA и немедленный отзыв access JWT не входили в этот этап.
