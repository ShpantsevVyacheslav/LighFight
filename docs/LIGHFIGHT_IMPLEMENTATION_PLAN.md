# LighFight — план реализации

Дата: 2026-10-09. Хост: `/Users/macbook1/Desktop/LighChat`, Flutter, iOS/Android.

## Цель и границы

Game Hub → Скачать → Играть → 3D-бой против бота → вернуться в LighChat.
Unity runtime, IL2CPP-код и загрузчик входят в приложение. Модели, сцены, текстуры,
анимации и звук доставляются через Addressables. Скачивание контента не заменяет
обновление приложения при изменении C#-кода. Название во всех новых материалах: LighFight.
Текущая браузерная игра сохраняется как прототип и референс механики.

## 1. Технический прототип

- Unity 6.3 LTS; закрепить конкретный patch после первой успешной сборки.
- Отдельные Core (без Unity API), Runtime, Editor, Tests.
- Симуляция 60 тиков/с; данные приёмов в кадрах, одна атака — одно попадание.
- 12 исходных ударов: jab/cross/hook/body/frontKick/lowKick/sweep/uppercut/knee/elbow/spinKick/jumpKick.
- Дальность, high/mid/low, стоячий/нижний блок, уклонение от high приседанием,
  hit stun, recovery, симметричный trade, границы арены и коллизия бойцов.
- Сначала примитивы, затем анимированные модели без изменения боевого ядра.
- Проверка: unit-тесты startup/range/guard/trade/recovery и запуск сцены в Editor.
- Готовность: прототип работает на Android и iPhone, управление отзывчивое.

Текущий статус: прототип импортирован и скомпилирован в Unity 6000.3.26f1,
сцена Prototype создана, 17/17 EditMode-тестов прошли в Editor.
Ранее 16 тестов также прошли через .NET 8 + NUnitLite.
Визуальная проверка сцены и запуск на мобильных устройствах ещё требуются.

## 2. Механика и анимации

- Разделить input, симуляцию, визуальное представление и звуковые события.
- Добавить input buffer, разрешённые маршруты комбо и cancel windows.
- Доделать knockdown/getup, launch/juggle с лимитом, throw/throw-break, sidestep,
  dash/backdash, counter hits и frame advantage. Это ещё не реализовано.
- Позиции и временные окна попаданий принадлежат симуляции, а не Animation Events.
- Animator: idle/walk/guard/crouch/attacks/hit/fall/getup; плавные переходы,
  фиксация ног, поворот таза и корпуса; root motion сверять с симуляцией.
- Виртуальные стрелки с удержанием и multitouch, 4 основные кнопки атаки,
  модификаторы направления; отдельный экран обучения.
- Бот: дистанция, вероятность блока, выбор безопасных атак, реакция с задержкой.
- Проверка: прерывание атак, spam, corner cases, touch+attack одновременно,
  одинаковая скорость симуляции при 30/60/120 FPS.

## 3. Персонажи и офис

- Два персонажа для первой версии; имена/описания уточнить перед пользовательским UI.
- Фото → моделирование головы и тела → ретопология → UV → материалы → rig → анимации.
- Сгенерированные изображения являются концептами, не готовыми rigged-моделями.
- Первая арена: переговорная; затем кухня, open space, двор и остальные локации.
- Офисные детали вынести из зоны боя; определить collision bounds независимо от декора.
- LOD, атласы текстур, baked lighting, ограничение прозрачности и динамических теней.
- Бюджеты полигонов/текстур/памяти зафиксировать по замерам первого устройства.
- Приёмка: узнаваемые персонажи и офис, стабильная производительность на целевых телефонах.

## 4. Доставка контента

- Локальная bootstrap-сцена; remote labels `lighfight-starter`, затем персонажи/арены отдельно.
- Отдельные каталоги/бандлы iOS и Android, с привязкой к версии Player и Unity.
- Версионированные неизменяемые URL, HTTPS/CDN, хэши, атомарная публикация каталога.
- Manifest: schemaVersion, contentVersion, minimumPlayerVersion, platform,
  starterLabel, каталог, объём загрузки. Ни ключей Firebase, ни исполняемых DLL.
- Состояния: notInstalled/checking/awaitingDownload/downloading/ready/error/updating.
- Размер и свободное место до загрузки, понятные ошибки, повтор, кэш.
- Отмена UI не означает гарантированную отмену активного Addressables-запроса:
  корректно завершать операции и освобождать handles; не заявлять неподдерживаемое resume.
- Проверка offline, network loss, storage full, corrupted cache, несовместимый catalog.
- Удаление только игровых dependency bundles; историю и аккаунт не удалять.
- Не запускать удалённую сцену до проверки наличия всех требуемых ресурсов.

Текущий статус: сервис estimate/download с progress и release handles написан;
каталог, CDN, проверка совместимости, free-space и экран загрузки ещё не созданы.

## 5. Flutter ↔ Unity

- Новая карточка в `mobile/app/lib/features/chat/data/games_registry.dart`;
  обработчик запуска в `games_hub_screen.dart`, расширение всех switch по типу игры.
- Добавить `LighFightHost` Dart facade и нативный канал `lighchat/lighfight`.
- iOS: UnityFramework + Swift host; Android: unityLibrary + Kotlin host.
- Launch JSON v1: protocolVersion, sessionId, mode=bot, fighterId, arenaId, locale.
- Host → Unity: launch/pause/resume/exit. Unity → Host: ready/progress/error/result/closed.
- Результат: sessionId, matchId, winner, rounds, durationTicks, build/contentVersion;
  повторная доставка идемпотентна. Клиентский результат не подтверждает рейтинг.
- Восстановить нативную навигацию, ориентацию, аудиосессию и focus после выхода.
- При входящем звонке поставить бой на паузу и отдать аудио/экран звонку.
- Не использовать Application.Quit для обычного возврата на iOS.
- Все новые UI-строки — во всех ARB-локалях LighChat; unit/widget/platform tests.
- Встраивать после проверки минимального Unity export, не показывать ложную кнопку «Играть».

Текущий статус: Unity-приёмник Launch/Pause/Exit написан; Flutter/Swift/Kotlin-мосты
и подключение Game Hub не реализованы. Файлы LighChat не изменены.

## 6. Backend и история

- Перед правками перечитать src/lib/types.ts, firestore.rules и правила LighChat.
- Расширить mobile/web/server game type согласованно, не ломать существующие 4 игры.
- Для solo определить отдельный контракт записи результата; не отправлять каждый тик в Firestore.
- Проверить авторизацию, App Check, валидацию данных, идемпотентность и лимиты запросов.
- Сохранять только итоговые данные; отображать отдельный режим против бота.
- Добавить тесты allow/deny при изменении rules, покрыть cascade delete новых данных.
- Приёмка: история переживает переустановку и не допускает записи в чужой аккаунт.

## 7. Сетевой режим (после solo)

- Приглашение из чата, lobby/session, согласование версии build/content и выбора бойцов.
- Выбрать transport и authority после прототипа задержек; rollback требует snapshot/restore,
  детерминированной симуляции и воспроизведения входов. Нынешний float-прототип
  не считать готовым детерминированным сетевым движком.
- Проверить 50/100/200 ms latency, jitter, packet loss, reconnect, background timeout.
- Проверка результатов сервером, политика disconnect/forfeit, защита рейтинга.
- Не переносить текущий пошаговый createGameLobby/applyMove напрямую на каждый удар.

## 8. Релиз и эксплуатация

- Editor compile + EditMode tests → Android/iOS builds → device smoke → внутренняя сборка.
- Матрица low/mid/high устройств; размер приложения, first download, FPS, memory,
  температура за несколько боёв, повторный launch/unload.
- CI: code tests, content build по платформам, compatibility validation, native exports.
- Обновление remote content сначала staging; rollback каталога с сохранением старых bundles.
- App review notes объясняют загрузку ресурсов; код поставляется в проверяемом приложении.
- Push/deploy LighChat только при явном разрешении пользователя.

## Порядок ближайшей работы

1. Открыть `unity` в Unity Hub/Editor 6.3 LTS и дождаться импорта пакетов.
2. `LighFight → Create prototype scene`, затем Play и EditMode tests.
3. Исправить выявленные ошибки; закрепить editor patch и package lock.
4. Сделать первый Android export и iOS export, проверить host lifecycle.
5. Доделать боевую механику и перейти от примитивов к двум rigged-моделям.
6. Настроить remote starter content и только затем включить карточку в Game Hub.

Размеры и сроки не обещаны до первого импорта, модели и замеров на устройствах.

## Обновление этапа 2026-10-09

- Установлен локальный .NET SDK 8.0.425 в `.tools/dotnet` (не в системный PATH).
- Проверено 16/16 тестов C# боевого ядра; браузерные проверки также прошли.
- Исправлено прерывание атак при попадании; trade вычисляется до изменения состояния.
- Prototype запускается явно из сцены, не создаётся автоматически в чужих сценах.
- Добавлены multitouch-поллинг, пауза при сворачивании, запуск нового матча через HostBridge.
- Выполнены batch setup/test/Android export/iOS export; оба exports завершились успешно.
- По разрешению пользователя удалены только DerivedData, .gradle/caches,
  LighChat/mobile/app/build и LighChat/.next; свободно стало около 68 GiB.
- Установлен Unity Hub 3.22.2 (Apple Silicon) в `~/Applications/Unity Hub.app`.
- Пользователь подтвердил право компании на Unity Personal и создал Unity ID;
  Unity Personal активирована и проверена в Hub (2026-10-09).
- Установлен 6000.3.26f1 ARM64 с Android/iOS-модулями; 6.6 отменена.
- Успешно выполнены импорт, компиляция и создание сцены; 17/17 тестов Unity прошли.
- Созданы Android Gradle-проект с unityLibrary и iOS Xcode-проект с UnityFramework.
  Финальные APK/IPA, интеграция с Flutter и проверка на устройствах впереди.

## Unity host integration — 2026-10-10

- Добавлен локальный Flutter-плагин `mobile/packages/lighfight_host` в LighChat.
- Game Hub показывает LighFight только при доступном нативном движке.
- Протокол v1: анонимный sessionId, bot mode, locale; ready/launch/exit handshake.
- Android — отдельный процесс и закрытая Activity; iOS — UnityFramework и возврат окна через unload.
- Собраны Debug ARM64 AAR и iOS UnityFramework; iOS Flutter-плагин собран в Xcode.
- Android-плагин скомпилирован в изолированном Gradle harness; итоговый манифест
  содержит одну launcher Activity, Unity Activity закрыта и находится в :lighfight.
- 8 тестов Flutter прошли; 17/17 EditMode-тестов Unity повторно прошли.
- Скачать контент через Addressables, собрать полный клиент и проверить цикл на
  физических телефонах ещё требуется. Прототип пока использует примитивные модели.
