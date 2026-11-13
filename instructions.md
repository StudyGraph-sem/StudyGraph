# StudyGraph

Семестровый проект: ASP.NET Core API и клиент Vue + TypeScript.
Сейчас это стартовый каркас: API возвращает пример прогноза погоды, клиент показывает шаблонную страницу. База данных, авторизация и взаимодействие клиента с API пока не реализованы.

## Структура

```text
StudyGraph/
├── .gitignore                         # Общие исключения Git
├── backend/StudyGraph/
│   ├── .gitignore                     # Дополнительные исключения backend
│   ├── StudyGraph.sln                 # Решение для Visual Studio / Rider
│   └── StudyGraph/
│       ├── StudyGraph.csproj          # .NET 10, зависимости и настройки сборки
│       ├── Program.cs                # Запуск API, сервисы, middleware и маршруты
│       ├── Properties/launchSettings.json # Профили и порты локального запуска
│       ├── appsettings.json          # Общие настройки
│       ├── appsettings.Development.json # Настройки среды разработки
│       └── StudyGraph.http            # Примеры HTTP-запросов
└── frontend/
    ├── package.json                   # Зависимости и команды npm
    ├── index.html                     # HTML с контейнером приложения
    ├── vite.config.ts                 # Сборщик Vite; @ означает src/
    ├── tsconfig*.json                 # Настройки TypeScript
    ├── vitest.config.ts               # Настройки тестов
    ├── eslint.config.ts               # Проверка кода
    ├── .oxlintrc.json / .prettierrc.json # Линтер и форматирование
    ├── public/                        # Статические файлы без обработки Vite
    └── src/
        ├── main.ts                    # Подключение Vue, Pinia и Router
        ├── App.vue                    # Корневой компонент
        ├── router/index.ts            # Маршруты страниц; пока пустые
        ├── stores/counter.ts          # Демонстрационный store Pinia
        └── __tests__/App.spec.ts      # Тест шаблонной страницы
```

Повторение StudyGraph в пути backend — вложенность «папка backend → решение → проект». Открывай файл .sln для работы с сервером в IDE.

## Первый запуск

Нужны .NET SDK 10 и Node.js, соответствующий package.json: ^22.18.0 либо >=24.12.0, а также npm. Проверить установку:

```powershell
dotnet --version
node --version
npm --version
```

Все следующие пути отсчитываются от корня репозитория StudyGraph, где лежит этот README.

В первом терминале запусти сервер:

```powershell
dotnet restore backend/StudyGraph/StudyGraph.sln
dotnet dev-certs https --trust
dotnet run --project backend/StudyGraph/StudyGraph/StudyGraph.csproj --launch-profile https
```

Профиль https задаёт адреса https://localhost:7138 и http://localhost:5197. В API включено перенаправление на HTTPS. Проверь https://localhost:7138/weatherforecast — должен вернуться JSON. В Development описание API доступно по https://localhost:7138/openapi/v1.json. Графический Swagger UI сейчас не подключён. Корневой адрес / не имеет обработчика и может возвращать 404.

Во втором терминале запусти клиент:

```powershell
cd frontend
npm install
npm run dev
```

Открой адрес, который напечатает Vite (обычно http://localhost:5173). Для остановки любого сервера нажми Ctrl+C в его терминале. После появления package-lock.json добавь его в Git; при последующих чистых установках можно использовать npm ci.

## Как добавлять функциональность

### Сервер

Сейчас API использует Minimal API: обработчики объявлены через app.MapGet в Program.cs. Новые маршруты можно добавлять там же, а по мере роста выносить в отдельные файлы/группы endpoints. Сервисы регистрируются через builder.Services до builder.Build(). Middleware и маршруты подключаются после builder.Build(), до app.Run().

Для развития проекта можно создать папки Models (модели), DTOs (данные запросов и ответов), Services (логика) и Data (доступ к БД). Этих папок пока нет. Если выберешь контроллеры, потребуется явно подключить AddControllers() и MapControllers(): сейчас они не настроены.

### Клиент

Переиспользуемые элементы удобно хранить в src/components/, страницы — в src/views/, запросы к серверу — в src/api/, общие типы — в src/types/. Эти папки создавай по необходимости. Общее состояние размещай в src/stores/; локальное состояние компонента обычно достаточно хранить через ref/reactive.

Для добавления страницы создай компонент в views, зарегистрируй путь в router/index.ts и добавь <RouterView /> в App.vue. Сейчас в App.vue нет RouterView, поэтому одной регистрации маршрута недостаточно. Для ссылок между страницами используй RouterLink.

Файл .vue обычно состоит из script setup с TypeScript, template с разметкой и style со стилями. Атрибут scoped ограничивает действие стилей компонентом.

### Связь клиента с API

Сейчас клиент не отправляет запросы к серверу. В vite.config.ts также нет proxy, а API не настраивает CORS. Поэтому прямой fetch с другого порта потребует настройки CORS на сервере либо proxy в Vite. Для разработки можно проксировать /api на backend и обращаться из клиента к относительным адресам /api/…; пути и правила переписывания должны соответствовать реальным маршрутам сервера. Настройки Vite proxy действуют только при разработке: для публикации связь настраивается отдельно.

Не помещай секреты в переменные VITE_*: они доступны браузеру. Серверные секреты храни в переменных окружения или .NET User Secrets. appsettings*.json можно коммитить только без секретов.

## Проверки перед коммитом

Из корня репозитория:

```powershell
dotnet build backend/StudyGraph/StudyGraph.sln
```

Из frontend:

```powershell
npm run type-check
npm run test:unit -- --run
npm run build
```

Дополнительные команды: npm run lint проверяет код и автоматически исправляет часть замечаний; npm run format переформатирует src. Просматривай изменения после этих команд. npm run test:unit запускает тесты в режиме наблюдения, а npm run preview позволяет просмотреть собранный клиент. Серверного тестового проекта пока нет. При замене шаблонной страницы обнови App.spec.ts: сейчас он ожидает текст You did it!.

## Работа с Git

Выполняй команды из корня StudyGraph:

```powershell
git status
git diff
git add .
git diff --cached
git commit -m "Describe the change"
```

Перед коммитом проверяй staged diff на случайные изменения и секреты. Не добавляй bin, obj, node_modules, dist и локальные .env — их исключают .gitignore. Исходники, файлы проектов, миграции БД при их появлении и lock-файл зависимостей нужно хранить в Git. Внутри backend и frontend не запускай git init: у проекта один репозиторий в корне.
