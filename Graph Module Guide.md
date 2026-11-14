# Модуль Graph — участник А

## Ветки

- `main` — полностью готовые блоки приложения.
- `dev` — проверенные и объединённые ветки задач; создаётся от `main`.
- Ветки задач (`structure`, `diagram`, `domain` и другие) создаются от актуальной `dev` и вливаются обратно в `dev` через PR после проверки.
- Рабочая ветка для текущего этапа Graph — `domain`.

## Границы ответственности

| Участник | Ответственность |
| --- | --- |
| А | Граф знаний: `subjects`, `roadmaps`, `topics`, `topic_dependencies`; логика зависимостей и интерфейс графа. |
| Б | Учебные материалы, общая инфраструктура PostgreSQL/EF Core, единый DbContext и координация миграций. |
| В | Пользователи, JWT, роли и расчёт прогресса. |

Требования к данным определяются `diagram_description.md` и утверждённой ER-диаграммой `diagram.png`. Изменения общих контрактов и исходников других модулей согласуются с ответственными участниками. DbContext, миграции и авторизация подключаются совместно с Б и В.

## Размещение кода

Сохраняется один backend-проект `backend/StudyGraph/StudyGraph.csproj` (.NET 10, Minimal API) и существующее Vue-приложение. Пути backend ниже указаны относительно `backend/StudyGraph/`:

| Содержимое | Каталог |
| --- | --- |
| Сущности графа | `Domain/Entities/Graph/` |
| Сценарии, DTO, валидация и сервисы графа | `Application/Graph/` |
| Контракты и реализации сервисов | `Application/Graph/Services/Interfaces/`, `Application/Graph/Services/Implementations/` |
| HTTP endpoints | `API/Endpoints/Graph/` |
| Конфигурации EF Core после согласования с Б | `Infrastructure/Persistence/Configurations/Graph/` |
| Клиентская часть графа | `frontend/src/features/graph/` от корня репозитория |

Каталоги добавляются вместе с нужными файлами по мере реализации. Существующие заготовки модуля прогресса сохраняются; незавершённые методы не подключаются к запуску приложения.

## Сущности графа

В `Domain/Entities/Graph/` описаны `Subject`, `Roadmap`, `Topic` и `TopicDependency` по разделам 2.2–2.5 `diagram_description.md` и `diagram.png`. Идентификаторы представлены `Guid`, вес — `decimal`, необязательные координаты — `decimal?`. `Description` и `ShareToken` допускают `null`. При создании объектов C# нужно явно задать `Title`, а для темы также `ImportanceWeight` через `required`.

Типы `SubjectVisibility` и `StudyStatus` определяют значения предметной области. При подключении хранения нужно явно настроить соответствие:

| Значение C# | Значение в PostgreSQL |
| --- | --- |
| `SubjectVisibility.Private` | `private` |
| `SubjectVisibility.Public` | `public` |
| `SubjectVisibility.Unlisted` | `unlisted` |
| `StudyStatus.NotStarted` | `not_started` |
| `StudyStatus.InProgress` | `in_progress` |
| `StudyStatus.Completed` | `completed` |

`visibility` хранится как PostgreSQL enum `subject_visibility`, а `study_status` — как `VARCHAR(20)` согласно диаграмме. Автоматическое преобразование enum через `ToString()` не обеспечит нужные значения.

Ограничения для будущей валидации и EF-конфигурации: непустые названия до 200 символов; строго положительный вес `NUMERIC(8,2)`; координаты `NUMERIC(12,3)`; уникальный `share_token`; составной ключ зависимости `(topic_id, prerequisite_topic_id)` и два составных FK через общий `roadmap_id`. `required` требует инициализацию свойства в C#, но не проверяет длину строки или положительность числа. Проверки входных данных и настройка БД выполняются на следующих этапах.

У зависимости нет отдельного `Id`; направление — `PrerequisiteTopicId → TopicId`. Блокировка вычисляется по предварительным темам и не входит в `StudyStatus` или поля `Topic`. Сущности не зависят от EF Core, HTTP или классов других модулей.

## Исходная проверка

Ветка `domain` создана от актуальной `dev`, коммит `5aeb8b1`. Проверка выполнена до изменения исходного кода:

```text
dotnet build backend/StudyGraph.sln
```

Сборка успешна с .NET SDK `10.0.301`, ошибок нет. NuGet сообщает предупреждение `NU1903` для транзитивного пакета `Microsoft.OpenApi` версии `2.0.0` (https://github.com/advisories/GHSA-v5pm-xwqc-g5wc); обновление зависимостей требует отдельного исправления. Серверный тестовый проект пока отсутствует.
