# Установка и проверка Unity для LighFight

Unity Hub установлен: `/Users/macbook1/Applications/Unity Hub.app` (3.22.2, ARM64).
Аккаунт Unity ID создан пользователем. Unity Personal активирована 2026-10-09;
дата активации проверена в Settings → Licenses. Пользователь отдельно подтвердил
принятие условий в Get Unity Personal.

Установка Unity 6.3 LTS 6000.3.26f1 ARM64 начата через Hub: Android Build Support,
OpenJDK, Android SDK/NDK и iOS Build Support (14 элементов очереди).
Требуемый объём по Hub — 21.41 GB. Автоматическая установка 6.6 отменена.
Editor установлен в `/Applications/Unity/Hub/Editor/6000.3.26f1/Unity.app`.
AndroidPlayer (включая SDK/NDK/OpenJDK) и iOSSupport установлены в соседней
папке `6000.3.26f1/PlaybackEngines`, а не внутри Unity.app.
Импорт и компиляция проекта завершены, сцена Prototype создана;
17/17 EditMode-тестов прошли в Unity. Удалена несуществующая зависимость
`com.unity.modules.inputlegacy`; legacy Input доступен в самом Editor.
Android export успешно создан в `unity/Builds/Android` (Gradle, unityLibrary,
ARM64 IL2CPP); iOS export — в `unity/Builds/iOS` (Unity-iPhone.xcodeproj,
UnityFramework). APK/IPA не собраны, запуск на телефонах ещё не проверен.
Логи и XML результатов тестов находятся в `.tools/reports`.

В Hub:
1. Завершить Sign in и проверить Licenses → Unity Personal.
2. Если лицензии нет: Settings → Licenses → Add license → Get a free personal license.
   Условия принимает пользователь или агент после подтверждения конкретного соглашения.
3. Installs → Install Editor → Unity 6.3 LTS (Apple Silicon).
   В release API на 2026-10-09 доступен 6000.3.26f1.
4. Выбрать Android Build Support, SDK & NDK Tools, OpenJDK и iOS Build Support.
   Не требуется второй редактор кода — VS Code уже установлен.
5. Add project from disk → `/Users/macbook1/Documents/ChatGPT/LighFight/unity`.
6. После импорта выполнить LighFight → Create prototype scene, затем Play.
7. Window → General → Test Runner → EditMode → Run All.
8. Выполнить exports из Editor/скриптов; проверить результаты на телефонах.

Объём по release API: Editor ~9.6 GB, Android Support ~6.7 GB без вложенных SDK/NDK,
iOS Support ~1.4 GB. Инсталлятор Editor ~5.2 GB; дополнительные скачивания и
временные файлы требуют запаса. После согласованной очистки на диске ~68 GiB.

Проверено отдельно: Core C# скомпилирован в .NET 8.0.425, NUnitLite 3.14.0,
16 тестов прошли. Это НЕ проверка Unity API, сцены, графики и мобильных exports.

Источники:
- https://docs.unity.com/en-us/hub/manage-license
- https://unity.com/legal/editor-terms-of-service/software
- https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.3&limit=1&architecture=ARM64&platform=MAC_OS
