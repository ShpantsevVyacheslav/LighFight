# LighFight: доставка ресурсов

Player содержит боевой C#-код, HostBridge, ContentBootstrap и загрузчик Addressables.
В remote-группе `LighFight Remote Starter` лежит `GameContent` и его зависимости:
восемь prefab бойцов, пять prefab арен, материалы и меши. Ресурсы не входят
в локальную bootstrap-сцену. Финальные художественные ассеты ещё не созданы.

## Сборка

Из корня проекта; указать абсолютный путь Editor. URL — каталог платформы и
неизменяемой версии, новая сборка получает новый `LIGHFIGHT_CONTENT_VERSION`.

```sh
LIGHFIGHT_CONTENT_VERSION=starter-3 \
LIGHFIGHT_CONTENT_URL=https://YOUR-CDN/lighfight/Android/starter-3 \
"$UNITY_EDITOR" -batchmode -quit -projectPath unity -buildTarget Android \
-executeMethod LighFight.Editor.ContentBuild.Build -logFile .tools/reports/content-android.log
```

Аналогично `-buildTarget iOS`. Результат — `.tools/content/<BuildTarget>/<version>`:
`manifest.json`, бинарный каталог, hash и bundle. Каталог публикуется после bundles.
Повторная сборка той же версии запрещена. `addressables_content_state.bin` из
Library сохраняется отдельно при выпуске; понадобится для content update.

```sh
LIGHFIGHT_MANIFEST_URL=https://YOUR-CDN/lighfight/Android/starter-3/manifest.json \
"$UNITY_EDITOR" -batchmode -quit -projectPath unity \
-executeMethod LighFight.Editor.ContentBuild.Bootstrap -logFile .tools/reports/bootstrap.log
```

После этого native export использует Bootstrap, а не локальную Prototype-сцену.
У двух платформ разные manifests/каталоги/exports; не собирать iOS Player с Android URL.
HTTPS обязателен; только development разрешает HTTP на localhost/127.0.0.1/10.0.2.2.
Публичный CDN ещё не подключён, загрузка с телефона через интернет не проверена.

## Загрузчик

Проверяет схему, minimum player, Unity patch, платформу, версию, URL, label/address
и допустимый размер. Загружает catalog, получает фактический размер зависимостей,
показывает кнопку скачивания, progress и повтор при ошибке. Перед созданием игры
проверяет полный набор prefab и компоненты. Handles освобождаются.

После успешного открытия сохраняет manifest атомарно; при ошибке сети пробует его
из кэша. Addressables отвечает за кэш bundles/catalog. Свободное место проверяется через Android StatFs / iOS filesystem attributes;
требуется удвоенный размер загрузки плюс 32 MiB. Неизвестный объём допускает попытку
скачивания. Возобновление частичного скачивания не обещается. Выход во время
операции завершает Unity host; это не API отмены Addressables.

## Проверки 2026-10-10

- Unity 46/46 и 40 тестов .NET; шесть новых manifest-тестов обнаруживают намеренные дефекты.
- Packed HTTP smoke: каталог и bundle реально запрошены у локального сервера,
  GameContent содержит 8 бойцов / 5 арен; загруженный rig создаётся и позируется.
- Offline packed smoke после остановки HTTP сервера: PASS, download size 0.
- Android (133001 байт) и iOS (132711 байт) bundles собраны. Указанные в manifests
  URL — подготовленный путь хранилища; объекты не опубликованы, доступ не настроен.
- Это проверка ресурсов для StandaloneOSX. Mobile bundles и native device smoke
  фиксируются отдельно; desktop smoke не подменяет проверку телефонов.

## Публикация

Репозиторий GitHub private. Push, публикация bundles и изменения облачного
хранилища требуют явной команды владельца по правилам автономной работы.
Никаких фотографий сотрудников в текущем starter-пакете нет.

## Изолированный Firebase Hosting

`content-hosting/firebase.json` предназначен для отдельного сайта
`lighfight-assets-72b24` проекта `project-72b24`. Этот сайт пока не создан.
Существующий сайт LighChat не используется. Публичный доступ ко всему Storage
bucket не требуется.

```sh
LIGHFIGHT_CONTENT_VERSION=starter-4 scripts/prepare-content-hosting.sh
```

Скрипт собирает Android и iOS, сохраняет content state отдельно и готовит
`.tools/publish`. Версионные bundles/catalogs неизменяемы; `latest.json` —
коротко кэшируемый указатель manifest. Сам скрипт ничего не публикует.
Нативный export требует `LIGHFIGHT_MANIFEST_URL`, например
`https://lighfight-assets-72b24.web.app/lighfight/Android/latest.json`.
URL станет рабочим после создания сайта и публикации.
Для локального preview используется только явный `LIGHFIGHT_EMBED_PROTOTYPE=1`.

Перед облачным выпуском: разрешение владельца, публикация, проверка доступности
обоих manifests и bundles, exports с соответствующими URLs, полный клиент,
первое скачивание / кэш / возврат / повторный вход на Android и iOS.
