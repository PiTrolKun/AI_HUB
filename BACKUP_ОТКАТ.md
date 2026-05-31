# BACKUP / ОТКАТ — AI_HUB

Этот файл фиксирует backup-и, rollback-важные шаги и способ отката изменений.

## 2026-05-30 — стартовый скелет проекта

Задача: создать полный стартовый набор папок и текстовых файлов для AI_HUB.

### Изменения

- Создаются новые служебные документы в корне проекта.
- Создаются новые документы в `Документы_проекта`.
- Создаются рабочие папки для будущего кода, тестов, инструментов, ТЗ, backup и runtime-данных.
- В важные папки добавляются короткие `README.md`.

### Backup

Существующие файлы не изменялись:

- `local_ai_hub_final_summary_ru.md`
- `Инструкции/AGENTS.md`
- `Инструкции/CODEX.md`

Поэтому backup существующих файлов для этой задачи не требуется.

### Откат

Для отката этой задачи удалить только новые файлы и папки, созданные в рамках стартового скелета. Не удалять папку `Инструкции` и файл `local_ai_hub_final_summary_ru.md`.

Перед любым будущим изменением существующих файлов создавать отдельный backup в папке `Backups` и фиксировать путь здесь.

## 2026-05-30 — перенос завершённого ТЗ в архив

Задача: перенести выполненное ТЗ стартового скелета в архив.

### Изменения

- Файл `ТЗ/2026-05-30_стартовый_скелет_AI_HUB.md` перенесён в `ТЗ/Архив/2026-05-30_стартовый_скелет_AI_HUB.md`.
- Обновлён этот журнал отката.

### Backup

Создан backup:

- `Backups/20260530_194719_archive_start_skeleton_tz/TZ_2026-05-30_start_skeleton_AI_HUB.md`
- `Backups/20260530_194719_archive_start_skeleton_tz/BACKUP_OTKAT.md`

### Откат

Для отката перенести файл из `ТЗ/Архив/2026-05-30_стартовый_скелет_AI_HUB.md` обратно в `ТЗ/2026-05-30_стартовый_скелет_AI_HUB.md`.

Если нужно откатить журнал, восстановить `BACKUP_ОТКАТ.md` из backup-копии `Backups/20260530_194719_archive_start_skeleton_tz/BACKUP_OTKAT.md`.

## 2026-05-30 — добавление scanner-а кириллицы

Задача: добавить внутренний PowerShell-скрипт проверки UTF-8 и типичных следов поломанной кириллицы.

### Изменения

- Создан файл `Инструменты/check-cyrillic-integrity.ps1`.
- Обновлён `Инструменты/README.md` с инструкцией запуска.
- Обновлён `Документы_проекта/REESTR.md`: scanner добавлен как developer tooling.
- Обновлён этот журнал отката.

### Backup

Создан backup:

- `Backups/20260530_194909_add_cyrillic_scanner/Dokumenty_proekta_REESTR.md`
- `Backups/20260530_194909_add_cyrillic_scanner/Instrumenty_README.md`
- `Backups/20260530_194909_add_cyrillic_scanner/BACKUP_OTKAT.md`

### Откат

Для отката удалить `Инструменты/check-cyrillic-integrity.ps1` и восстановить изменённые документы из backup-папки `Backups/20260530_194909_add_cyrillic_scanner`.

### Дополнительные исправления scanner-а

После первого запуска были исправлены ложные срабатывания:

- `Backups/20260530_195009_fix_cyrillic_scanner_false_positive/check-cyrillic-integrity.ps1` — backup перед исправлением regex обычной кириллицы.
- `Backups/20260530_195009_fix_cyrillic_scanner_false_positive/BACKUP_OTKAT.md` — backup журнала перед фиксацией исправления.
- `Backups/20260530_195024_fix_scanner_readme_example/Instrumenty_README.md` — backup перед заменой примера символа `U+FFFD` в README.
- `Backups/20260530_195024_fix_scanner_readme_example/BACKUP_OTKAT.md` — backup журнала перед фиксацией исправления README.

## 2026-05-30 — установка .NET SDK 10 для WPF-разработки

Задача: проверить подготовительные зависимости для выбранного стека `C# / .NET / WPF` и установить недостающее.

### Изменения

- Установлен Microsoft .NET SDK 10.0.300 через `winget`.
- Проверено, что доступны WPF-шаблоны.
- Проверено, что временный WPF-проект на `net10.0-windows` собирается без ошибок и предупреждений.
- Обновлены `Документы_проекта/REESTR.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_200107_dotnet10_sdk_setup_docs/Dokumenty_proekta_REESTR.md`
- `Backups/20260530_200107_dotnet10_sdk_setup_docs/BACKUP_OTKAT.md`
- `Backups/20260530_200107_dotnet10_sdk_setup_docs/CONTEXTHUB.md`
- `Backups/20260530_200107_dotnet10_sdk_setup_docs/Dialog_szhato.md`

### Откат

Документы можно восстановить из backup-папки `Backups/20260530_200107_dotnet10_sdk_setup_docs`.

Системный откат SDK выполняется отдельно через `winget uninstall --id Microsoft.DotNet.SDK.10 --exact`, если пользователь явно попросит удалить SDK.

## 2026-05-30 — подготовка первой публикации на GitHub

Задача: подготовить локальный проект к первой отправке в репозиторий `https://github.com/PiTrolKun/AI_HUB`.

### Изменения

- Установлен GitHub CLI 2.93.0 через `winget`.
- Создан `.gitignore`, чтобы не публиковать backup-содержимое, runtime-данные, build output и secrets.
- Создан `.gitattributes`, чтобы стабилизировать окончания строк для документации, PowerShell-скриптов и будущих C# / WPF-файлов.
- Обновлены `Документы_проекта/REESTR.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_200837_github_initial_publish_docs/Dokumenty_proekta_REESTR.md`
- `Backups/20260530_200837_github_initial_publish_docs/BACKUP_OTKAT.md`
- `Backups/20260530_200837_github_initial_publish_docs/CONTEXTHUB.md`
- `Backups/20260530_200837_github_initial_publish_docs/Dialog_szhato.md`

### Откат

Документы можно восстановить из backup-папки `Backups/20260530_200837_github_initial_publish_docs`.

Системный откат GitHub CLI выполняется отдельно через `winget uninstall --id GitHub.cli --exact`, если пользователь явно попросит удалить GitHub CLI.

## 2026-05-30 — завершение первой публикации на GitHub

Задача: зафиксировать успешный push первого коммита и перенести ТЗ публикации в архив.

### Изменения

- Первый коммит `9a5e76c Initial project skeleton` отправлен в `origin/main`.
- GitHub подтвердил репозиторий `PiTrolKun/AI_HUB`, видимость `PRIVATE`, default branch `main`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- ТЗ `ТЗ/2026-05-30_первая_публикация_github.md` перенесено в `ТЗ/Архив/2026-05-30_первая_публикация_github.md`.

### Backup

Создан backup:

- `Backups/20260530_201505_github_publish_complete_docs/BACKUP_OTKAT.md`
- `Backups/20260530_201505_github_publish_complete_docs/CONTEXTHUB.md`
- `Backups/20260530_201505_github_publish_complete_docs/Dialog_szhato.md`
- `Backups/20260530_201505_github_publish_complete_docs/TZ_2026-05-30_github_publish.md`

### Откат

Документы можно восстановить из backup-папки `Backups/20260530_201505_github_publish_complete_docs`.

После отправки в GitHub откат содержимого делать новым коммитом. Не переписывать опубликованную историю без отдельного решения пользователя.

## 2026-05-30 — правила версий и GitHub-публикации при архивировании ТЗ

Задача: добавить в правила работы строгую нумерацию версий и обязательную публикацию актуального состояния проекта в GitHub при переносе ТЗ в архив.

### Изменения

- Обновлён `Инструкции/AGENTS.md`.
- Обновлён `Инструкции/CODEX.md`.
- Выполненное ТЗ `ТЗ/2026-05-30_подготовка_dotnet_wpf_окружения.md` перенесено в `ТЗ/Архив/2026-05-30_подготовка_dotnet_wpf_окружения.md`.
- По прямому текущему указанию пользователя повторная публикация в GitHub для этого переноса не выполняется, потому что заготовка проекта только что уже была опубликована.

### Backup

Создан backup:

- `Backups/20260530_202127_rules_versioning_tz_github/Instrukcii_AGENTS.md`
- `Backups/20260530_202127_rules_versioning_tz_github/Instrukcii_CODEX.md`
- `Backups/20260530_202127_rules_versioning_tz_github/BACKUP_OTKAT.md`
- `Backups/20260530_202127_rules_versioning_tz_github/TZ_2026-05-30_dotnet_wpf_env.md`

### Откат

Для отката восстановить `AGENTS.md`, `CODEX.md` и `BACKUP_ОТКАТ.md` из backup-папки `Backups/20260530_202127_rules_versioning_tz_github`.

Если нужно вернуть ТЗ из архива, перенести `ТЗ/Архив/2026-05-30_подготовка_dotnet_wpf_окружения.md` обратно в `ТЗ/2026-05-30_подготовка_dotnet_wpf_окружения.md`.

## 2026-05-30 — правило обязательной записи в истории

Задача: исправить пропуск записи в истории и добавить правило, что история проекта обновляется после каждого выполненного действия.

### Изменения

- Обновлён `Инструкции/AGENTS.md`.
- Обновлён `Инструкции/CODEX.md`.
- Обновлён `CONTEXTHUB.md`.
- Обновлён `Диалог_сжато.md`.
- Обновлён этот журнал отката.

### Backup

Создан backup:

- `Backups/20260530_202318_history_every_action_rule/Instrukcii_AGENTS.md`
- `Backups/20260530_202318_history_every_action_rule/Instrukcii_CODEX.md`
- `Backups/20260530_202318_history_every_action_rule/CONTEXTHUB.md`
- `Backups/20260530_202318_history_every_action_rule/Dialog_szhato.md`
- `Backups/20260530_202318_history_every_action_rule/BACKUP_OTKAT.md`

### Откат

Для отката восстановить изменённые документы из backup-папки `Backups/20260530_202318_history_every_action_rule`.

## 2026-05-30 — прототип стартового WPF-окна

Задача: создать первый реальный визуальный прототип стартового окна AI_HUB.

### Изменения

- Создано ТЗ `ТЗ/2026-05-30_прототип_стартового_окна_wpf.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Создан WPF-проект `Исходники/AIHub`.
- Реализован прототип стартового окна.
- Выполнены `dotnet restore`, `dotnet build` и scanner кириллицы.
- Приложение запущено для визуальной оценки.

### Backup

Создан backup:

- `Backups/20260530_203002_startup_window_prototype/CONTEXTHUB.md`
- `Backups/20260530_203002_startup_window_prototype/Dialog_szhato.md`
- `Backups/20260530_203002_startup_window_prototype/BACKUP_OTKAT.md`
- `Backups/20260530_203002_startup_window_prototype/Dokumenty_proekta_REESTR.md`

### Откат

Для отката удалить созданный WPF-проект из `Исходники` и восстановить изменённые документы из backup-папки `Backups/20260530_203002_startup_window_prototype`.

## 2026-05-30 — переключатель светлой и тёмной темы

Задача: добавить в стартовое окно кнопку переключения светлой и тёмной темы.

### Изменения

- Обновлён `Исходники/AIHub/MainWindow.xaml`.
- Обновлён `Исходники/AIHub/MainWindow.xaml.cs`.
- Добавлена кнопка `Тёмная тема` / `Светлая тема` в правый верхний угол.
- Тема переключается без перезапуска окна.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_203637_startup_theme_toggle/MainWindow.xaml`
- `Backups/20260530_203637_startup_theme_toggle/MainWindow.xaml.cs`
- `Backups/20260530_203637_startup_theme_toggle/CONTEXTHUB.md`
- `Backups/20260530_203637_startup_theme_toggle/Dialog_szhato.md`
- `Backups/20260530_203637_startup_theme_toggle/BACKUP_OTKAT.md`
- `Backups/20260530_203637_startup_theme_toggle/TZ_2026-05-30_startup_window_prototype.md`

### Откат

Для отката восстановить XAML, code-behind и документы из backup-папки `Backups/20260530_203637_startup_theme_toggle`.

## 2026-05-30 — исправление падения стартового окна и скрипт запуска

Задача: проверить сообщение пользователя о том, что окно сразу закрывается, исправить причину и добавить запуск программы двойным кликом.

### Изменения

- Проверен Windows Event Log: найдено падение `AIHub.exe` с `XamlParseException`.
- Исправлен `Исходники/AIHub/MainWindow.xaml`: начальный фон окна больше не берётся через ранний `StaticResource`, а тематические кисти используют `DynamicResource`.
- Исправлен `Исходники/AIHub/MainWindow.xaml.cs`: переключение темы заменяет кисти ресурсов и обновляет фон окна.
- Созданы `Запустить_AI_HUB.cmd` и `start-aihub.ps1` для запуска dev-версии двойным кликом.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, `Документы_проекта/REESTR.md`, активное ТЗ и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_204448_fix_startup_crash_launcher/MainWindow.xaml`
- `Backups/20260530_204448_fix_startup_crash_launcher/MainWindow.xaml.cs`
- `Backups/20260530_204448_fix_startup_crash_launcher/CONTEXTHUB.md`
- `Backups/20260530_204448_fix_startup_crash_launcher/Dialog_szhato.md`
- `Backups/20260530_204448_fix_startup_crash_launcher/BACKUP_OTKAT.md`
- `Backups/20260530_204448_fix_startup_crash_launcher/TZ_2026-05-30_startup_window_prototype.md`
- `Backups/20260530_204448_fix_startup_crash_launcher/Dokumenty_proekta_REESTR.md`
- `Backups/20260530_204448_fix_startup_crash_launcher/Zapustit_AI_HUB_cmd_bad_lf_backup.cmd`

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Smoke-запуск `AIHub.exe`: процесс остался активен после старта, немедленного падения нет.
- `Запустить_AI_HUB.cmd` успешно выполнил сборку и запустил окно.
- Клик по `Тёмная тема` через Windows UI Automation не завершил процесс.
- Scanner кириллицы прошёл без ошибок.

### Откат

Для отката восстановить XAML, code-behind и документы из backup-папки `Backups/20260530_204448_fix_startup_crash_launcher`, затем удалить `Запустить_AI_HUB.cmd` и `start-aihub.ps1`.

## 2026-05-30 — архивирование ТЗ стартового WPF-окна без push

Задача: закрыть выполненное ТЗ стартового WPF-окна и перенести его в архив.

### Изменения

- Файл `ТЗ/2026-05-30_прототип_стартового_окна_wpf.md` перенесён в `ТЗ/Архив/2026-05-30_прототип_стартового_окна_wpf.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_204949_archive_startup_window_tz_no_push/TZ_2026-05-30_startup_window_prototype.md`
- `Backups/20260530_204949_archive_startup_window_tz_no_push/CONTEXTHUB.md`
- `Backups/20260530_204949_archive_startup_window_tz_no_push/Dialog_szhato.md`
- `Backups/20260530_204949_archive_startup_window_tz_no_push/BACKUP_OTKAT.md`

### Исключение из правила GitHub-публикации

По прямому текущему указанию пользователя push в GitHub не выполнялся: "в гит хаб пока не кидай".

### Откат

Для отката перенести `ТЗ/Архив/2026-05-30_прототип_стартового_окна_wpf.md` обратно в `ТЗ/2026-05-30_прототип_стартового_окна_wpf.md` и восстановить документы истории из backup-папки `Backups/20260530_204949_archive_startup_window_tz_no_push`.

## 2026-05-30 — фиксация версии 0.0.1-dev

Задача: зафиксировать первую официальную dev-версию заготовки проекта.

### Изменения

- Создан файл `VERSION` со значением `0.0.1-dev`.
- В `Исходники/AIHub/AIHub.csproj` добавлены свойства версии:
  - `Version`: `0.0.1-dev`
  - `AssemblyVersion`: `0.0.1.0`
  - `FileVersion`: `0.0.1.0`
  - `InformationalVersion`: `0.0.1-dev`
  - `IncludeSourceRevisionInInformationalVersion`: `false`, чтобы .NET не добавлял git-хэш к dev-версии.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_205215_set_version_0_0_1_dev/AIHub.csproj`
- `Backups/20260530_205215_set_version_0_0_1_dev/CONTEXTHUB.md`
- `Backups/20260530_205215_set_version_0_0_1_dev/Dialog_szhato.md`
- `Backups/20260530_205215_set_version_0_0_1_dev/BACKUP_OTKAT.md`

### Откат

Для отката восстановить `AIHub.csproj` и документы истории из backup-папки `Backups/20260530_205215_set_version_0_0_1_dev`, затем удалить файл `VERSION`.

## 2026-05-30 — исправление выбора проекта в запускаторе

Задача: исправить ошибку `CS5001` при запуске через `Запустить_AI_HUB.cmd`.

### Причина

`start-aihub.ps1` искал `AIHub.csproj` рекурсивно по всему `H:\AI_HUB` и мог выбрать backup-копию проекта из `Backups`. Backup-копия `.csproj` лежит без `App.xaml`, поэтому WPF-точка входа `Main` не генерировалась.

### Изменения

- `start-aihub.ps1` больше не ищет проект рекурсивно.
- Скрипт использует точный путь `Исходники/AIHub/AIHub.csproj`.
- Путь к `AIHub.exe` также строится от настоящей папки проекта.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_205402_fix_launcher_project_path/start-aihub.ps1`
- `Backups/20260530_205402_fix_launcher_project_path/CONTEXTHUB.md`
- `Backups/20260530_205402_fix_launcher_project_path/Dialog_szhato.md`
- `Backups/20260530_205402_fix_launcher_project_path/BACKUP_OTKAT.md`

### Откат

Для отката восстановить `start-aihub.ps1` и документы истории из backup-папки `Backups/20260530_205402_fix_launcher_project_path`.

## 2026-05-30 — автоматическая версия в строке окна

Задача: добавить отображение версии в нижнюю строку стартового окна без второй ручной копии версии.

### Изменения

- В `MainWindow.xaml` нижний статусный `TextBlock` получил имя `StatusTextBlock`.
- В `MainWindow.xaml.cs` добавлено чтение версии из `AssemblyInformationalVersion`.
- Статусная строка окна теперь показывает `Версия 0.0.1-dev` автоматически из свойств сборки.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_205631_show_version_in_status_line/MainWindow.xaml`
- `Backups/20260530_205631_show_version_in_status_line/MainWindow.xaml.cs`
- `Backups/20260530_205631_show_version_in_status_line/CONTEXTHUB.md`
- `Backups/20260530_205631_show_version_in_status_line/Dialog_szhato.md`
- `Backups/20260530_205631_show_version_in_status_line/BACKUP_OTKAT.md`

### Откат

Для отката восстановить XAML, code-behind и документы истории из backup-папки `Backups/20260530_205631_show_version_in_status_line`.

## 2026-05-30 — правило одного источника версии

Задача: закрепить в правилах, что версия программы должна иметь один источник истины.

### Изменения

- Обновлён `Инструкции/AGENTS.md`.
- Обновлён `Инструкции/CODEX.md`.
- Добавлено правило: будущие UI-элементы, окно About, статусные строки, установщик, updater, release notes, диагностика, логи и модули должны читать версию из основного источника версии проекта.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_205806_single_version_source_rule/Instrukcii_AGENTS.md`
- `Backups/20260530_205806_single_version_source_rule/Instrukcii_CODEX.md`
- `Backups/20260530_205806_single_version_source_rule/CONTEXTHUB.md`
- `Backups/20260530_205806_single_version_source_rule/Dialog_szhato.md`
- `Backups/20260530_205806_single_version_source_rule/BACKUP_OTKAT.md`

### Откат

Для отката восстановить инструкции и документы истории из backup-папки `Backups/20260530_205806_single_version_source_rule`.

## 2026-05-30 — версия в заголовке окна

Задача: перенести отображение версии из нижней статусной строки в системный заголовок окна и убрать подчёркивание из названия в заголовке.

### Изменения

- В `MainWindow.xaml` базовый заголовок изменён с `AI_HUB` на `AI HUB`.
- В `MainWindow.xaml.cs` заголовок окна формируется автоматически как `AI HUB {версия}` из `AssemblyInformationalVersion`.
- Нижняя строка снова показывает только статус без версии.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_210414_move_version_to_window_title/MainWindow.xaml`
- `Backups/20260530_210414_move_version_to_window_title/MainWindow.xaml.cs`
- `Backups/20260530_210414_move_version_to_window_title/CONTEXTHUB.md`
- `Backups/20260530_210414_move_version_to_window_title/Dialog_szhato.md`
- `Backups/20260530_210414_move_version_to_window_title/BACKUP_OTKAT.md`

### Откат

Для отката восстановить XAML, code-behind и документы истории из backup-папки `Backups/20260530_210414_move_version_to_window_title`.

## 2026-05-30 — публикация актуального состояния после закрытия ТЗ

Задача: выполнить закрытие по правилам и опубликовать актуальное состояние проекта в GitHub.

### Состояние ТЗ

- Активных ТЗ вне архива нет.
- Последнее рабочее ТЗ стартового окна уже находится в `ТЗ/Архив/2026-05-30_прототип_стартового_окна_wpf.md`.

### Изменения

- Закрыто запущенное тестовое окно `AIHub.exe`, чтобы сборка не блокировалась занятым exe-файлом.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал перед публикацией.

### Backup

Создан backup:

- `Backups/20260530_210606_publish_current_state_github/CONTEXTHUB.md`
- `Backups/20260530_210606_publish_current_state_github/Dialog_szhato.md`
- `Backups/20260530_210606_publish_current_state_github/BACKUP_OTKAT.md`

### Откат

Документы истории можно восстановить из backup-папки `Backups/20260530_210606_publish_current_state_github`.

После push в GitHub откат опубликованного состояния выполнять новым коммитом, не переписывая историю без отдельного решения пользователя.

### Результат публикации

- Проверки перед push: `dotnet build`, `Запустить_AI_HUB.cmd`, scanner кириллицы.
- Коммит перед push: `0281b34 Record project state before GitHub publish`.
- Push выполнен успешно: `origin/main` обновлён с `f54bef1` до `0281b34`.

## 2026-05-30 — добавление логотипа приложения

Задача: добавить логотип, который отображается в заголовке окна, диспетчере задач, exe и будет использоваться будущим установщиком для ярлыков.

### Изменения

- Исходный PNG пользователя из `Данные_для_внедрения/Фото/logo_crt_black_hole_matrix_tight_transparent_1x1.png` скопирован в `Исходники/AIHub/Assets`.
- Создан `Исходники/AIHub/Assets/AppIcon.ico` с несколькими размерами значка.
- В `AIHub.csproj` добавлено свойство `ApplicationIcon`.
- В `MainWindow.xaml` добавлен `Icon="Assets/AppIcon.ico"`.
- В `.gitattributes` добавлены правила `*.png binary` и `*.ico binary`.
- Обновлены `BRANDING.md`, `NOTICE.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_224246_add_application_logo/AIHub.csproj`
- `Backups/20260530_224246_add_application_logo/MainWindow.xaml`
- `Backups/20260530_224246_add_application_logo/gitattributes`
- `Backups/20260530_224246_add_application_logo/BRANDING.md`
- `Backups/20260530_224246_add_application_logo/NOTICE.md`
- `Backups/20260530_224246_add_application_logo/VERSION`
- `Backups/20260530_224246_add_application_logo/CONTEXTHUB.md`
- `Backups/20260530_224246_add_application_logo/Dialog_szhato.md`
- `Backups/20260530_224246_add_application_logo/BACKUP_OTKAT.md`

### Откат

### Версия

Версия повышена с `0.0.1-dev` до `0.0.2-dev`, потому что добавление логотипа меняет приложение и exe.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- `Запустить_AI_HUB.cmd` успешно собрал и запустил приложение.
- UI Automation подтвердил заголовок окна `AI HUB 0.0.2-dev`.
- Из `AIHub.exe` успешно извлечён связанный значок 64x64.
- Scanner кириллицы прошёл без ошибок.

### Откат

Для отката восстановить изменённые текстовые файлы из backup-папки `Backups/20260530_224246_add_application_logo`, удалить `Исходники/AIHub/Assets/AppIcon.ico` и удалить скопированный PNG из `Исходники/AIHub/Assets`, если он больше не нужен.

## 2026-05-30 — иконка переключения темы и очистка верхней панели

Задача: убрать надпись `Первый запуск`, прижать переключатель темы к правому краю и заменить текстовую кнопку на значки луны/солнца с подсказкой.

### Изменения

- В `MainWindow.xaml` удалён текст `Первый запуск` из верхней панели.
- Кнопка темы стала квадратной кнопкой-иконкой у правого края.
- В светлой теме кнопка показывает луну `☾` и подсказку `Переключить на тёмную тему`.
- В тёмной теме кнопка показывает солнце `☀` и подсказку `Переключить на светлую тему`.
- Версия повышена с `0.0.2-dev` до `0.0.3-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_225921_theme_icon_button_header_cleanup/MainWindow.xaml`
- `Backups/20260530_225921_theme_icon_button_header_cleanup/MainWindow.xaml.cs`
- `Backups/20260530_225921_theme_icon_button_header_cleanup/AIHub.csproj`
- `Backups/20260530_225921_theme_icon_button_header_cleanup/VERSION`
- `Backups/20260530_225921_theme_icon_button_header_cleanup/CONTEXTHUB.md`
- `Backups/20260530_225921_theme_icon_button_header_cleanup/Dialog_szhato.md`
- `Backups/20260530_225921_theme_icon_button_header_cleanup/BACKUP_OTKAT.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260530_225921_theme_icon_button_header_cleanup`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- UI Automation подтвердил заголовок `AI HUB 0.0.3-dev`.
- UI Automation подтвердил, что текст `Первый запуск` больше не отображается.
- UI Automation подтвердил луну `☾` в светлой теме и солнце `☀` после переключения в тёмную тему.
- UI Automation подтвердил подсказку для солнца: `Переключить на светлую тему`.

## 2026-05-30 — цветное солнце на кнопке темы

Задача: сделать цветным значок солнца на кнопке переключения темы.

### Изменения

- В `MainWindow.xaml.cs` для тёмной темы значку солнца `☀` задан тёпло-жёлтый цвет `#FBBF24`.
- В светлой теме значок луны `☾` остаётся обычным цветом текста текущей темы.
- Версия повышена с `0.0.3-dev` до `0.0.4-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_230318_color_sun_theme_icon/MainWindow.xaml.cs`
- `Backups/20260530_230318_color_sun_theme_icon/AIHub.csproj`
- `Backups/20260530_230318_color_sun_theme_icon/VERSION`
- `Backups/20260530_230318_color_sun_theme_icon/CONTEXTHUB.md`
- `Backups/20260530_230318_color_sun_theme_icon/Dialog_szhato.md`
- `Backups/20260530_230318_color_sun_theme_icon/BACKUP_OTKAT.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260530_230318_color_sun_theme_icon`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Smoke-проверка запуска подтвердила заголовок `AI HUB 0.0.4-dev`.
- UI Automation подтвердила переключение кнопки темы с луны `☾` на солнце `☀`.

## 2026-05-30 — системная тёмная рамка Windows

Задача: сделать так, чтобы системный заголовок окна AI HUB лучше подчинялся тёмной теме Windows, без создания кастомной рамки окна.

### Изменения

- Создано ТЗ `ТЗ/2026-05-30_системная_темная_рамка_windows.md`.
- В `MainWindow.xaml.cs` добавлено чтение настройки Windows `AppsUseLightTheme` из пользовательской ветки реестра.
- При запуске AI HUB начальная тема окна теперь берётся из темы Windows для приложений.
- Системный заголовок окна синхронизируется с темой приложения через `DwmSetWindowAttribute`.
- При ручном переключении темы AI HUB обновляет не только содержимое окна, но и системный заголовок.
- Версия повышена с `0.0.4-dev` до `0.0.5-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_231105_windows_dark_titlebar/MainWindow.xaml.cs`
- `Backups/20260530_231105_windows_dark_titlebar/AIHub.csproj`
- `Backups/20260530_231105_windows_dark_titlebar/VERSION`
- `Backups/20260530_231105_windows_dark_titlebar/CONTEXTHUB.md`
- `Backups/20260530_231105_windows_dark_titlebar/Dialog_szhato.md`
- `Backups/20260530_231105_windows_dark_titlebar/BACKUP_OTKAT.md`
- `Backups/20260530_231105_windows_dark_titlebar/TZ_sistemnaya_temnaya_ramka_windows.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260530_231105_windows_dark_titlebar` и удалить ТЗ `ТЗ/2026-05-30_системная_темная_рамка_windows.md`, если задача будет полностью отменена.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Smoke-проверка запуска подтвердила заголовок `AI HUB 0.0.5-dev`.
- Smoke-проверка подтвердила, что окно стартует в тёмной теме Windows: первой отображается кнопка-солнце `☀`.
- UI Automation подтвердила переключение темы с солнца `☀` на луну `☾`; приложение не упало.

## 2026-05-30 — архивирование ТЗ системной тёмной рамки

Задача: закрыть подтверждённое пользователем ТЗ системной тёмной рамки Windows и опубликовать актуальное состояние проекта в GitHub.

### Изменения

- ТЗ `ТЗ/2026-05-30_системная_темная_рамка_windows.md` перенесено в `ТЗ/Архив/2026-05-30_системная_темная_рамка_windows.md`.
- В истории проекта зафиксировано подтверждение выполнения ТЗ пользователем.
- По правилу архивации ТЗ будет выполнен commit и push актуального состояния проекта в GitHub.

### Backup

Создан backup:

- `Backups/20260530_231359_archive_windows_dark_titlebar_tz/2026-05-30_системная_темная_рамка_windows.md`
- `Backups/20260530_231359_archive_windows_dark_titlebar_tz/CONTEXTHUB.md`
- `Backups/20260530_231359_archive_windows_dark_titlebar_tz/Dialog_szhato.md`
- `Backups/20260530_231359_archive_windows_dark_titlebar_tz/BACKUP_OTKAT.md`

### Откат

Для отката архивирования восстановить ТЗ из backup-папки `Backups/20260530_231359_archive_windows_dark_titlebar_tz` в папку `ТЗ`, а документы истории восстановить из той же backup-папки.

### Проверки и публикация

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Commit и push в GitHub выполняются после этой записи.

## 2026-05-30 — создание ТЗ логики стартового окна и паспорта ПК

Задача: сохранить новое рабочее ТЗ по оживлению кнопок стартового окна, заготовке окна настройки и паспорту компьютера.

### Изменения

- Создано ТЗ `ТЗ/2026-05-30_логика_стартового_окна_и_паспорт_пк.md`.
- В ТЗ зафиксировано, что `Начать настройку` и `Перенастроить` ведут в один сценарий.
- В ТЗ зафиксировано, что текущие кнопки нужно оживить, но пока они ведут только к заготовке окна настройки.
- В ТЗ зафиксировано автоматическое создание паспорта ПК при первом запуске и пересоздание при `Перенастроить`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_233107_save_start_window_logic_tz/CONTEXTHUB.md`
- `Backups/20260530_233107_save_start_window_logic_tz/Dialog_szhato.md`
- `Backups/20260530_233107_save_start_window_logic_tz/BACKUP_OTKAT.md`

### Откат

Для отката удалить ТЗ `ТЗ/2026-05-30_логика_стартового_окна_и_паспорт_пк.md` и восстановить документы истории из backup-папки `Backups/20260530_233107_save_start_window_logic_tz`.

### Проверки

- Scanner кириллицы прошёл без ошибок.

## 2026-06-01 — debug-окно проверки моделей и llama.cpp backend

Задача: в рамках ТЗ `2026-05-31_менеджер_основного_ядра_qwen3_8b.md` добавить отладочное окно по `F12` для живой проверки скачанных GGUF-моделей.

### Изменения

- Добавлено отдельное окно `DebugChatWindow`.
- Добавлены модели/сервисы debug-проверки:
  - `Models/DebugChatMessage.cs`;
  - `Models/DebugModelInfo.cs`;
  - `Services/DebugModelDiscoveryService.cs`;
  - `Services/LlamaCliRuntimeService.cs`.
- `MainWindow` научен открывать одно debug-окно по `F12`.
- `AppDataPaths` расширен путями к `Runtime` и `Runtime/Backends`.
- Версия повышена до `0.0.17-dev`.
- Локализация `ru.json` и `en.json` дополнена debug-строками.
- `REESTR.md` и `THIRD_PARTY_NOTICES.md` дополнены backend-ом `llama.cpp`.
- Локально скачан backend `llama.cpp b9442` Windows CUDA 12.4 x64 в `Runtime/Backends/llama.cpp/b9442/win-cuda-12.4-x64`.
- Runtime-файлы backend-а не публикуются в GitHub.

### Backup

Созданы backup-папки:

- `Backups/20260601_002800_debug_model_tester`
- `Backups/20260601_004625_debug_output_cleanup`

В них сохранены изменяемые кодовые файлы, документы истории, rollback-журнал, локализации и рабочее ТЗ.

### Откат

Для отката:

- восстановить изменённые файлы из backup-папок;
- удалить новые debug-файлы:
  - `Исходники/AIHub/DebugChatWindow.xaml`;
  - `Исходники/AIHub/DebugChatWindow.xaml.cs`;
  - `Исходники/AIHub/Models/DebugChatMessage.cs`;
  - `Исходники/AIHub/Models/DebugModelInfo.cs`;
  - `Исходники/AIHub/Services/DebugModelDiscoveryService.cs`;
  - `Исходники/AIHub/Services/LlamaCliRuntimeService.cs`;
- при необходимости удалить локальный runtime `Runtime/Backends/llama.cpp`.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` прошёл успешно, без предупреждений.
- `Инструменты/check-cyrillic-integrity.ps1` прошёл успешно.
- Ключи `ru.json` и `en.json` совпадают: `165/165`.
- Пользователь вручную проверил debug-окно: `F12` открывает окно, Qwen3 8B видна, prompt отправляется, ответ приходит, логи отображаются.
- Codex выполнил короткий живой тест `llama-cli.exe`: exit code `0`, ответ `Модель работает.`
- Установщик не собирался по правилу проекта.

## 2026-06-01 — тема списка моделей в debug-окне

Задача: исправить нечитаемый текст в списке выбора моделей debug-окна.

### Изменения

- `DebugChatWindow` теперь принимает текущую тему основного окна.
- При переключении темы в основном окне открытое debug-окно тоже обновляет цвета.
- Для `ComboBox`, `ComboBoxItem`, `TextBox` и `ListBox` debug-окна заданы явные цвета для светлой и тёмной темы.
- Версия повышена до `0.0.18-dev`.

### Backup

Создан backup:

- `Backups/20260601__debug_window_theme_fix`

В backup сохранены `VERSION`, `AIHub.csproj`, `DebugChatWindow.xaml`, `DebugChatWindow.xaml.cs`, `MainWindow.xaml.cs`, рабочее ТЗ и документы истории.

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260601__debug_window_theme_fix`.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` прошёл успешно, без предупреждений.
- `Инструменты/check-cyrillic-integrity.ps1` прошёл успешно.
- Ключи `ru.json` и `en.json` совпадают: `165/165`.
- Установщик не собирался по правилу проекта.

## 2026-06-01 — исправление crash debug-окна по F12

Задача: устранить падение AI HUB при открытии debug-окна по `F12`.

### Причина

По журналу Windows найдено исключение:

- `System.InvalidOperationException`;
- сообщение: WPF-объект кисти находится в состоянии `только чтение`;
- место: `DebugChatWindow.SetBrush`.

Причина: после мини-правки темы код пытался менять цвет существующей WPF-кисти ресурса, которая могла быть frozen/read-only.

### Изменения

- `DebugChatWindow.SetBrush` теперь заменяет ресурс новым `SolidColorBrush`, а не меняет существующую кисть.
- Theme-bound свойства debug-окна переведены на `DynamicResource`.
- `MainWindow.OpenDebugChatWindow` обёрнут в `try/catch`, чтобы debug-инструмент не ронял основное окно.
- В локализацию добавлен статус `Status.DebugChatOpenFailed`.
- Версия повышена до `0.0.19-dev`.

### Backup

Создан backup:

- `Backups/20260601_011000_debug_f12_crash_fix`

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` прошёл успешно, без предупреждений.
- `Инструменты/check-cyrillic-integrity.ps1` прошёл успешно.
- Ключи `ru.json` и `en.json` совпадают: `166/166`.
- Автоматический smoke-test: запуск `AIHub.exe`, отправка `F12`, найдено окно `AI HUB — отладка моделей`, процесс не упал.
- Установщик не собирался по правилу проекта.

## 2026-06-01 — читаемый текст native ComboBox debug-окна

Задача: исправить белый текст модели на светлом фоне native `ComboBox`.

### Изменения

- Для выбора модели добавлен ресурс `NativeComboTextBrush`.
- Выбранный элемент и элементы выпадающего списка модели используют тёмный текст `#1F1F1F`.
- Фон native `ComboBox` оставлен светлым, чтобы не ломать системный шаблон WPF.
- Версия повышена до `0.0.20-dev`.

### Backup

Создан backup:

- `Backups/20260601_011800_debug_combobox_text_fix`

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` прошёл успешно, без предупреждений.
- `Инструменты/check-cyrillic-integrity.ps1` прошёл успешно.
- Ключи `ru.json` и `en.json` совпадают: `166/166`.
- Автоматический smoke-test: запуск `AIHub.exe`, отправка `F12`, найдено окно `AI HUB — отладка моделей`, процесс не упал.
- Установщик не собирался по правилу проекта.

## 2026-06-01 — архивирование ТЗ основного ядра

Задача: закрыть подтверждённое пользователем ТЗ `2026-05-31_менеджер_основного_ядра_qwen3_8b.md`.

### Изменения

- ТЗ перенесено из `ТЗ` в `ТЗ/Архив`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Commit/push выполняются по правилу архивации ТЗ после финальных проверок.
- Установщик не собирался по правилу проекта.

### Backup

Создан backup:

- `Backups/20260601_012500_archive_core_model_task`

### Откат

Для отката вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ` и восстановить документы истории из backup-папки `Backups/20260601_012500_archive_core_model_task`.

## 2026-05-31 — менеджер основного ядра Qwen3 8B

Задача: добавить в AI HUB базовую логику основного ИИ-ядра `Qwen3 8B Q4_K_M`, проверку наличия модели, загрузку с прогрессом, resume и manifest.

### Изменения

- Добавлены модели состояния основного ядра:
  - `Исходники/AIHub/Models/CoreModelAvailability.cs`;
  - `Исходники/AIHub/Models/CoreModelCheckResult.cs`;
  - `Исходники/AIHub/Models/CoreModelDownloadProgress.cs`;
  - `Исходники/AIHub/Models/CoreModelManifest.cs`.
- Добавлен сервис:
  - `Исходники/AIHub/Services/CoreModelManager.cs`.
- В `MainWindow.xaml` добавлены нижняя панель подтверждения скачивания и панель прогресса загрузки.
- В `MainWindow.xaml.cs` подключены проверка ядра при запуске/сохранении настроек, кнопки `Скачать`, `Открыть настройки`, `Позже`, `Пауза`, `Отмена`.
- Обновлены `ru.json` и `en.json`.
- Версия повышена до `0.0.16-dev`.
- Обновлены `REESTR.md` и `THIRD_PARTY_NOTICES.md`.
- По прямому подтверждению пользователя модель скачана в выбранную папку моделей:
  - `H:\AI_HUB\Данные_для_внедрения\Модели\Core\Qwen3-8B\Qwen3-8B-Q4_K_M.gguf`.
- Проверены размер модели и SHA-256, создан `core-model.json` со статусом `installed`.

### Backup

Создан backup:

- `Backups/20260531_213359_core_model_manager_impl`

В backup входят ключевые файлы приложения и документов до реализации.

### Откат

Для отката:

- восстановить изменённые файлы из `Backups/20260531_213359_core_model_manager_impl`;
- удалить добавленные файлы `CoreModel*.cs` и `CoreModelManager.cs`;
- при необходимости удалить скачанную модель и manifest из `H:\AI_HUB\Данные_для_внедрения\Модели\Core\Qwen3-8B`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений после добавления сервиса.
- HEAD-запрос к Hugging Face подтвердил доступность файла и размер `5027783488` байт.
- Модель скачана с resume после сетевого обрыва.
- SHA-256 модели совпал с ожидаемым значением.
- Scanner кириллицы прошёл без ошибок.
- Ключи `ru.json` и `en.json` совпали: 126 ключей.
- Smoke-запуск приложения прошёл: окно `AI HUB 0.0.16-dev`, модель распознана как установленная, prompt скачивания не показан.
- Smoke-клик `Начать работу` открыл страницу `Начало работы`.
- Установщик не собирался.

## 2026-06-01 — игнорирование локально скачанных моделей

Задача: не допустить случайной публикации больших локальных моделей в GitHub.

### Изменения

- В `.gitignore` добавлено правило:
  - `Данные_для_внедрения/Модели/**`.

Скачанная модель остаётся локально на диске пользователя и не должна попадать в репозиторий.

### Backup

Создан backup:

- `Backups/20260601_001200_core_model_gitignore/.gitignore`
- `Backups/20260601_001200_core_model_gitignore/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить `.gitignore` и этот журнал из `Backups/20260601_001200_core_model_gitignore`.

### Проверки

- `git status --ignored` показывает `Данные_для_внедрения/Модели/` как ignored.

## 2026-05-31 — ТЗ менеджера основного ядра Qwen3 8B

Задача: сохранить ТЗ для внедрения основного ИИ-ядра проекта через реальное скачивание `Qwen3 8B Q4_K_M`.

### Изменения

- Создано ТЗ `ТЗ/2026-05-31_менеджер_основного_ядра_qwen3_8b.md`.
- В ТЗ зафиксировано, что базовое ядро проекта — `Qwen3 8B Q4_K_M`.
- В ТЗ зафиксировано, что модель скачивается через AI HUB после явного подтверждения пользователя, а не включается в основной установщик.
- Проверен официальный источник модели:
  - Hugging Face `Qwen/Qwen3-8B-GGUF`;
  - файл `Qwen3-8B-Q4_K_M.gguf`;
  - размер `5027783488` байт;
  - лицензия `apache-2.0`;
  - commit `7c41481f57cb95916b40956ab2f0b139b296d974`;
  - ETag/hash `d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Код приложения и версия не менялись.
- Commit/push не выполнялись: ТЗ создано и ожидает реализации.

### Backup

Создан backup:

- `Backups/20260531_212427_core_model_manager_tz/CONTEXTHUB.md`
- `Backups/20260531_212427_core_model_manager_tz/Диалог_сжато.md`
- `Backups/20260531_212427_core_model_manager_tz/BACKUP_ОТКАТ.md`

### Откат

Для отката удалить `ТЗ/2026-05-31_менеджер_основного_ядра_qwen3_8b.md` и восстановить документы истории из backup-папки `Backups/20260531_212427_core_model_manager_tz`.

### Проверки

- Scanner кириллицы прошёл без ошибок.

## 2026-05-31 — контрольная запись после дисконнекта

Задача: после дисконнекта проверить, не потерялась ли запись о завершении ТЗ страницы начала работы и публикации в GitHub.

### Изменения

- Проверено, что рабочее дерево чистое.
- Проверено, что `main` синхронизирован с `origin/main`.
- Проверено, что коммиты `8dc7c64 Archive work start page task` и `884fc73 Record work start task publish` существуют локально и опубликованы.
- Добавлена контрольная запись в `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал, чтобы итог был виден после дисконнекта.
- Код приложения и версия не менялись.

### Backup

Создан backup:

- `Backups/20260531_202531_post_disconnect_audit_record/CONTEXTHUB.md`
- `Backups/20260531_202531_post_disconnect_audit_record/Диалог_сжато.md`
- `Backups/20260531_202531_post_disconnect_audit_record/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить документы истории из backup-папки `Backups/20260531_202531_post_disconnect_audit_record`.

### Проверки

- Scanner кириллицы прошёл без ошибок.
- Commit/push выполнены: `8dc7c64 Archive work start page task` отправлен в `origin/main`.

## 2026-05-31 — запись о публикации ТЗ страницы начала работы

Задача: зафиксировать в истории факт успешной публикации закрытого ТЗ страницы начала работы.

### Изменения

- В `CONTEXTHUB.md`, `Диалог_сжато.md` и этом журнале записан успешный push коммита `8dc7c64`.
- Код приложения и версия не менялись.

### Backup

Создан backup:

- `Backups/20260531_194907_record_work_start_publish/CONTEXTHUB.md`
- `Backups/20260531_194907_record_work_start_publish/Диалог_сжато.md`
- `Backups/20260531_194907_record_work_start_publish/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить документы истории из backup-папки `Backups/20260531_194907_record_work_start_publish`.

### Проверки

- Scanner кириллицы прошёл без ошибок.

## 2026-05-31 — страница начала работы

Задача: добавить страницу `Начало работы`, которая открывается по кнопке `Начать работу` после завершённой настройки.

### Изменения

- Создано ТЗ `ТЗ/2026-05-31_страница_начала_работы.md`.
- В `MainWindow.xaml` добавлена новая внутренняя страница `WorkStartPage`.
- На странице добавлен блок `Новый проект` с режимом `Рассуждение / изучение`.
- Кнопка `Выбрать режим` пока показывает статус-заглушку.
- Добавлен изначально свернутый блок `Ранее начатое` с примером будущей записи.
- Добавлена рабочая кнопка `Назад`.
- Кнопка `Начать работу` на главной странице теперь открывает `WorkStartPage`, если настройка завершена.
- Новые user-facing строки добавлены в `ru.json` и `en.json`.
- Версия повышена с `0.0.14-dev` до `0.0.15-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.
- Commit/push не выполнялись: ТЗ ещё не подтверждено пользователем и не перенесено в архив.

### Backup

Создан backup:

- `Backups/20260531_192352_work_start_page/MainWindow.xaml`
- `Backups/20260531_192352_work_start_page/MainWindow.xaml.cs`
- `Backups/20260531_192352_work_start_page/ru.json`
- `Backups/20260531_192352_work_start_page/en.json`
- `Backups/20260531_192352_work_start_page/AIHub.csproj`
- `Backups/20260531_192352_work_start_page/VERSION`
- `Backups/20260531_192352_work_start_page/2026-05-31_страница_начала_работы.md`
- `Backups/20260531_192352_work_start_page/CONTEXTHUB.md`
- `Backups/20260531_192352_work_start_page/Диалог_сжато.md`
- `Backups/20260531_192352_work_start_page/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_192352_work_start_page` и удалить ТЗ `ТЗ/2026-05-31_страница_начала_работы.md`, если нужно полностью отменить задачу.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Проверено, что ключи `ru.json` и `en.json` совпадают.
- Smoke-проверка русского интерфейса через UI Automation прошла: `Начать работу` открывает страницу, `Выбрать режим` показывает заглушку, `Ранее начатое` раскрывается, `Назад` возвращает на главную.
- Smoke-проверка английского интерфейса через UI Automation прошла; пользовательская настройка языка после проверки возвращена обратно.

## 2026-05-31 — архивация ТЗ страницы начала работы

Задача: закрыть подтверждённое пользователем ТЗ страницы начала работы, перенести его в архив и подготовить публикацию состояния проекта в GitHub.

### Изменения

- ТЗ `ТЗ/2026-05-31_страница_начала_работы.md` помечено закрытым по подтверждению пользователя.
- ТЗ перенесено в `ТЗ/Архив/2026-05-31_страница_начала_работы.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Код приложения не менялся после пользовательского подтверждения ТЗ.
- По правилу архивации ТЗ выполняется подготовка к commit/push в GitHub.

### Backup

Создан backup:

- `Backups/20260531_193130_archive_work_start_page_task/2026-05-31_страница_начала_работы.md`
- `Backups/20260531_193130_archive_work_start_page_task/CONTEXTHUB.md`
- `Backups/20260531_193130_archive_work_start_page_task/Диалог_сжато.md`
- `Backups/20260531_193130_archive_work_start_page_task/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить документы истории из backup-папки и вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Проверено, что ключи `ru.json` и `en.json` совпадают.
- Проверено, что активных ТЗ вне архива нет.
- Тестовый установщик `Тесты/Установщики/AI_HUB_Setup_0.0.15-dev.exe` создан и проверен по наличию файла.
- Во время ожидания сборки установщика команда Codex получила timeout/дисконнект, но итоговый артефакт создан, процессы сборки не остались висеть, `.exe` игнорируется Git-ом.

## 2026-05-31 — правило сборки установщика только по команде

Задача: по замечанию пользователя закрепить, что тестовый установщик не нужно собирать автоматически после каждого ТЗ.

### Изменения

- В `Инструкции/AGENTS.md` добавлено правило: тестовый установщик собирать только по прямой команде пользователя.
- В `Инструкции/CODEX.md` добавлено такое же расширенное правило.
- Исключение: сборка установщика допустима без отдельной команды, если тест установленной версии критичен для задачи.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Код приложения и версия не менялись.

### Backup

Создан backup:

- `Backups/20260531_194652_installer_build_rule/AGENTS.md`
- `Backups/20260531_194652_installer_build_rule/CODEX.md`
- `Backups/20260531_194652_installer_build_rule/CONTEXTHUB.md`
- `Backups/20260531_194652_installer_build_rule/Диалог_сжато.md`
- `Backups/20260531_194652_installer_build_rule/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые документы из backup-папки `Backups/20260531_194652_installer_build_rule`.

### Проверки

- Scanner кириллицы прошёл без ошибок.

## 2026-05-31 — архивация ТЗ локализации и общих настроек

Задача: закрыть подтверждённое пользователем ТЗ локализации и общих настроек, перенести его в архив и подготовить публикацию состояния проекта в GitHub.

### Изменения

- ТЗ `ТЗ/2026-05-31_локализация_и_общие_настройки.md` помечено закрытым по подтверждению пользователя.
- ТЗ перенесено в `ТЗ/Архив/2026-05-31_локализация_и_общие_настройки.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Код приложения не менялся после пользовательского подтверждения ТЗ.
- По правилу архивации ТЗ выполняется подготовка к commit/push в GitHub.

### Backup

Создан backup:

- `Backups/20260531_015639_archive_localization_settings_task/2026-05-31_локализация_и_общие_настройки.md`
- `Backups/20260531_015639_archive_localization_settings_task/CONTEXTHUB.md`
- `Backups/20260531_015639_archive_localization_settings_task/Диалог_сжато.md`
- `Backups/20260531_015639_archive_localization_settings_task/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить документы истории из backup-папки и вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Проверено, что ключи `ru.json` и `en.json` совпадают.
- Проверено, что активных ТЗ вне архива нет.
- `build-installer.ps1 -SkipPublish` успешно пересобрал тестовый Inno Setup установщик `Тесты/Установщики/AI_HUB_Setup_0.0.14-dev.exe`.
- Commit/push выполнены: `b28fcd2 Archive localization settings task` отправлен в `origin/main`.

## 2026-05-31 — запись о публикации ТЗ локализации

Задача: зафиксировать в истории факт успешной публикации закрытого ТЗ локализации и общих настроек.

### Изменения

- В `CONTEXTHUB.md`, `Диалог_сжато.md` и этом журнале записан успешный push коммита `b28fcd2`.
- Код приложения и версия не менялись.

### Backup

Создан backup:

- `Backups/20260531_015900_record_localization_publish/CONTEXTHUB.md`
- `Backups/20260531_015900_record_localization_publish/Диалог_сжато.md`
- `Backups/20260531_015900_record_localization_publish/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить документы истории из backup-папки `Backups/20260531_015900_record_localization_publish`.

### Проверки

- Scanner кириллицы прошёл без ошибок.

## 2026-05-31 — архивация закрытых ТЗ

Задача: закрыть все активные ТЗ по подтверждению пользователя и подготовить публикацию состояния в GitHub.

### Изменения

- ТЗ `2026-05-31_настройки_хранилищ_моделей_и_результатов.md` получило финальный статус и перенесено в `ТЗ/Архив`.
- ТЗ `2026-05-31_тестовый_установщик_inno_setup.md` получило финальный статус и перенесено в `ТЗ/Архив`.
- В папке `ТЗ` активных задач не осталось, только `README.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- По правилу архивации ТЗ требуется commit и push в GitHub.

### Backup

Создан backup:

- `Backups/20260531_011146_archive_completed_tasks/2026-05-31_настройки_хранилищ_моделей_и_результатов.md`
- `Backups/20260531_011146_archive_completed_tasks/2026-05-31_тестовый_установщик_inno_setup.md`
- `Backups/20260531_011146_archive_completed_tasks/CONTEXTHUB.md`
- `Backups/20260531_011146_archive_completed_tasks/Диалог_сжато.md`
- `Backups/20260531_011146_archive_completed_tasks/BACKUP_ОТКАТ.md`

### Откат

Для отката вернуть ТЗ из `ТЗ/Архив` в `ТЗ` и восстановить документы истории из backup-папки `Backups/20260531_011146_archive_completed_tasks`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Проверено, что в папке `ТЗ` нет активных ТЗ, кроме `README.md`.
- `build-installer.ps1 -SkipPublish` успешно пересобрал `Тесты/Установщики/AI_HUB_Setup_0.0.13-dev.exe`.
- Создан коммит `b19c002 Archive tasks and add installer tooling`.
- `main` успешно отправлен в `origin/main`.

## 2026-05-31 — запись о публикации архивированных ТЗ

Задача: зафиксировать в истории факт успешной публикации после архивации ТЗ.

### Изменения

- В `CONTEXTHUB.md`, `Диалог_сжато.md` и этом журнале записан коммит `b19c002 Archive tasks and add installer tooling`.
- Зафиксировано, что `main` успешно отправлен в `origin/main`.

### Backup

Создан backup:

- `Backups/20260531_011438_record_archive_publish/CONTEXTHUB.md`
- `Backups/20260531_011438_record_archive_publish/Диалог_сжато.md`
- `Backups/20260531_011438_record_archive_publish/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить документы истории из backup-папки `Backups/20260531_011438_record_archive_publish`.

### Проверки

- Scanner кириллицы прошёл без ошибок.
- Ожидается финальный commit/push записи.

## 2026-05-31 — локализация и общие настройки

Задача: внедрить простую JSON-локализацию, добавить кнопку общих настроек и выбор языка интерфейса.

### Изменения

- Создано ТЗ `ТЗ/2026-05-31_локализация_и_общие_настройки.md`.
- Добавлены `Исходники/AIHub/Localization/ru.json` и `en.json`.
- Добавлены `Models/AppSettings.cs`, `Services/AppSettingsStore.cs`, `Services/LocalizationService.cs`.
- Добавлено хранение настроек приложения в `%LOCALAPPDATA%\AI_HUB\settings.json`.
- Добавлена папка пользовательских переводов `%LOCALAPPDATA%\AI_HUB\Localization`.
- В верхнюю панель добавлена кнопка настроек с иконкой шестерёнки.
- Добавлена страница общих настроек внутри основного окна.
- На странице общих настроек реализован выбор языка интерфейса.
- Текущие видимые строки стартового окна и страниц настройки переведены на ключи локализации.
- Реализован fallback: выбранный язык -> русский -> ключ.
- В `AGENTS.md` и `CODEX.md` добавлено правило проверки английской базы локализации перед закрытием ТЗ.
- Версия повышена с `0.0.13-dev` до `0.0.14-dev`.
- Commit/push не выполнялись: ТЗ ещё не архивировано.

### Backup

Создан backup:

- `Backups/20260531_014012_localization_settings/VERSION`
- `Backups/20260531_014012_localization_settings/AIHub.csproj`
- `Backups/20260531_014012_localization_settings/MainWindow.xaml`
- `Backups/20260531_014012_localization_settings/MainWindow.xaml.cs`
- `Backups/20260531_014012_localization_settings/AGENTS.md`
- `Backups/20260531_014012_localization_settings/CODEX.md`
- `Backups/20260531_014012_localization_settings/CONTEXTHUB.md`
- `Backups/20260531_014012_localization_settings/Диалог_сжато.md`
- `Backups/20260531_014012_localization_settings/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_014012_localization_settings` и удалить созданные файлы локализации/настроек из исходников.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- JSON `ru.json` и `en.json` валидны.
- Проверено, что набор ключей `ru/en` совпадает.
- Smoke-проверка подтвердила русский интерфейс.
- Smoke-проверка подтвердила английский интерфейс.
- Smoke-проверка подтвердила fallback неполного пользовательского языка на русский.
- Smoke-проверка подтвердила, что кнопка настроек открывает локализованную страницу настроек.
- `build-installer.ps1` успешно собрал `Тесты/Установщики/AI_HUB_Setup_0.0.14-dev.exe`; файлы `Localization/en.json` и `Localization/ru.json` попали в publish.

## 2026-05-30 — реализация логики стартового окна и паспорта ПК

Задача: оживить текущие кнопки стартового окна, заменить `Позже` на `Перенастроить`, создать страницу настройки внутри главного окна и добавить автоматический паспорт компьютера.

### Изменения

- Кнопка `Позже` заменена на `Перенастроить`.
- Кнопки `Начать настройку` и `Перенастроить` открывают одну и ту же страницу настройки внутри основного окна AI HUB.
- Добавлена рабочая кнопка `Назад`, которая возвращает со страницы настройки на приветственную страницу.
- Добавлены модели состояния и паспорта компьютера.
- Добавлены сервисы хранения JSON-файлов в `%LOCALAPPDATA%/AI_HUB`.
- При запуске создаются или читаются:
  - `%LOCALAPPDATA%/AI_HUB/state.json`;
  - `%LOCALAPPDATA%/AI_HUB/computer-passport.json`.
- При `Перенастроить` паспорт компьютера пересоздаётся.
- Заготовка отдельного окна настройки была удалена после замечания пользователя: настройка должна быть страницей, а не новым окном.
- Версия повышена с `0.0.5-dev` до `0.0.6-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.

### Backup

Создан основной backup:

- `Backups/20260530_233412_start_window_logic_passport/MainWindow.xaml`
- `Backups/20260530_233412_start_window_logic_passport/MainWindow.xaml.cs`
- `Backups/20260530_233412_start_window_logic_passport/AIHub.csproj`
- `Backups/20260530_233412_start_window_logic_passport/VERSION`
- `Backups/20260530_233412_start_window_logic_passport/TZ_logika_startovogo_okna_i_pasport_pk.md`
- `Backups/20260530_233412_start_window_logic_passport/CONTEXTHUB.md`
- `Backups/20260530_233412_start_window_logic_passport/Dialog_szhato.md`
- `Backups/20260530_233412_start_window_logic_passport/BACKUP_OTKAT.md`

Создан дополнительный backup перед исправлением страницы настройки:

- `Backups/20260530_233841_setup_page_in_main_window_fix/MainWindow.xaml`
- `Backups/20260530_233841_setup_page_in_main_window_fix/MainWindow.xaml.cs`
- `Backups/20260530_233841_setup_page_in_main_window_fix/SetupWindow.xaml`
- `Backups/20260530_233841_setup_page_in_main_window_fix/SetupWindow.xaml.cs`
- `Backups/20260530_233841_setup_page_in_main_window_fix/TZ_logika_startovogo_okna_i_pasport_pk.md`
- `Backups/20260530_233841_setup_page_in_main_window_fix/BACKUP_OTKAT.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260530_233412_start_window_logic_passport`. Если нужно восстановить промежуточное состояние с отдельным окном настройки, использовать backup `Backups/20260530_233841_setup_page_in_main_window_fix`, но это состояние признано неправильным по замечанию пользователя.

### Проверки

- `dotnet build` уже проходил без ошибок и предупреждений после исправления.
- Smoke-проверка подтвердила, что страница настройки открывается внутри основного окна, кнопка `Назад` работает, а JSON-файлы состояния и паспорта существуют.
- Финальный `dotnet build` прошёл без ошибок и предупреждений.
- Финальный scanner кириллицы прошёл без ошибок.
- Финальная smoke-проверка подтвердила: `Начать настройку` и `Перенастроить` открывают страницу настройки внутри основного окна, `Назад` возвращает на приветственную страницу, `state.json` и `computer-passport.json` существуют, а `Перенастроить` пересоздаёт паспорт ПК.

## 2026-05-30 — сводка паспорта ПК на главной странице и GPU/VRAM

Задача: показать основные данные паспорта ПК прямо в пункте 3 главной страницы и научить сканер видеть GPU/VRAM.

### Изменения

- Старый текст пункта 3 `Посмотрим RAM, GPU, VRAM и свободное место.` оставлен в XAML как стартовый текст до завершения сканирования.
- После сканирования пункт 3 показывает `Сканирование ПК завершено. Найдено:` и краткую сводку CPU, RAM, GPU, VRAM и дисков.
- В паспорт ПК добавлена модель GPU.
- Сканер паспорта ПК читает список GPU и VRAM из стандартной ветки реестра Windows `SYSTEM\CurrentControlSet\Control\Video`.
- Новые внешние зависимости не добавлялись.
- Версия повышена с `0.0.6-dev` до `0.0.7-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.

### Backup

Создан backup:

- `Backups/20260530_234911_passport_gpu_summary/MainWindow.xaml`
- `Backups/20260530_234911_passport_gpu_summary/MainWindow.xaml.cs`
- `Backups/20260530_234911_passport_gpu_summary/ComputerPassport.cs`
- `Backups/20260530_234911_passport_gpu_summary/ComputerPassportService.cs`
- `Backups/20260530_234911_passport_gpu_summary/AIHub.csproj`
- `Backups/20260530_234911_passport_gpu_summary/VERSION`
- `Backups/20260530_234911_passport_gpu_summary/2026-05-30_логика_стартового_окна_и_паспорт_пк.md`
- `Backups/20260530_234911_passport_gpu_summary/CONTEXTHUB.md`
- `Backups/20260530_234911_passport_gpu_summary/Диалог_сжато.md`
- `Backups/20260530_234911_passport_gpu_summary/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260530_234911_passport_gpu_summary` и удалить новый файл модели GPU, если он больше не нужен.

### Проверки

- `dotnet build` уже прошёл без ошибок и предупреждений после добавления GPU/VRAM.
- При первой проверке GPU был найден, но VRAM показывалась как `0`, потому что 32-битное поле `MemorySize` переполнялось на RTX 4090.
- Исправлено чтение VRAM: сначала используется 64-битное поле `HardwareInformation.qwMemorySize`, а для одинаковых GPU сохраняется лучший найденный объём VRAM.
- Финальный `dotnet build` прошёл без ошибок и предупреждений.
- Финальный scanner кириллицы прошёл без ошибок.
- Smoke-проверка подтвердила, что главная страница показывает сводку CPU/RAM/GPU/VRAM/диски.
- Smoke-проверка подтвердила, что `computer-passport.json` содержит GPU `NVIDIA GeForce RTX 4090` и VRAM `23.99 ГБ`.

## 2026-05-31 — мини-правка кнопки и архивирование ТЗ логики стартового окна

Задача: расширить кнопку `Перенастроить`, закрыть ТЗ логики стартового окна и паспорта ПК, затем опубликовать актуальное состояние проекта в GitHub.

### Изменения

- Кнопке `Перенастроить` задана увеличенная минимальная ширина, чтобы текст помещался в рамку.
- Версия повышена с `0.0.7-dev` до `0.0.8-dev`.
- ТЗ `ТЗ/2026-05-30_логика_стартового_окна_и_паспорт_пк.md` перенесено в `ТЗ/Архив/2026-05-30_логика_стартового_окна_и_паспорт_пк.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- По правилу архивации ТЗ будет выполнен commit и push в GitHub.

### Backup

Создан backup:

- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/MainWindow.xaml`
- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/AIHub.csproj`
- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/VERSION`
- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/2026-05-30_логика_стартового_окна_и_паспорт_пк.md`
- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/CONTEXTHUB.md`
- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/Диалог_сжато.md`
- `Backups/20260531_000144_fix_reconfigure_button_archive_tz/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_000144_fix_reconfigure_button_archive_tz` и вернуть ТЗ из архива в папку `ТЗ`, если задача будет открыта заново.

### Проверки и публикация

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Smoke-проверка подтвердила окно `AI HUB 0.0.8-dev`, наличие кнопки `Перенастроить`, увеличенную ширину кнопки и рабочий переход `Перенастроить` -> страница настройки -> `Назад`.
- Commit и push в GitHub выполняются после этой записи.

## 2026-05-31 — создание ТЗ настроек хранилищ моделей и результатов

Задача: сохранить новое рабочее ТЗ по настройкам `Хранилище моделей` и `Папка результатов`.

### Изменения

- Создано ТЗ `ТЗ/2026-05-31_настройки_хранилищ_моделей_и_результатов.md`.
- В ТЗ зафиксированы несколько адресов хранения для каждой категории.
- В ТЗ зафиксированы два способа добавления адреса: выбор папки Windows и ручной ввод пути.
- В ТЗ зафиксированы лимиты в ГБ, временное превышение в ГБ, порядок/приоритет адресов и отображение настроек на главной странице.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.

### Backup

Создан backup:

- `Backups/20260531_001722_save_storage_settings_tz/CONTEXTHUB.md`
- `Backups/20260531_001722_save_storage_settings_tz/Dialog_szhato.md`
- `Backups/20260531_001722_save_storage_settings_tz/BACKUP_OTKAT.md`

### Откат

Для отката удалить ТЗ `ТЗ/2026-05-31_настройки_хранилищ_моделей_и_результатов.md` и восстановить документы истории из backup-папки `Backups/20260531_001722_save_storage_settings_tz`.

### Проверки

- Scanner кириллицы прошёл без ошибок.

## 2026-05-31 — реализация настроек хранилищ моделей и результатов

Задача: реализовать настройки `Хранилище моделей` и `Папка результатов`, сохранить их в отдельный JSON-файл и показывать выбранное на главной странице.

### Изменения

- Добавлены модели настроек хранения:
  - `StorageSettings`;
  - `StorageCategorySettings`;
  - `StorageLocationSettings`.
- Добавлен сервис `StorageSettingsStore`.
- Добавлен путь `AppDataPaths.StorageSettingsPath`.
- Включён встроенный Windows Forms API для стандартного выбора папки Windows.
- На странице настройки внутри основного окна добавлены секции:
  - `Хранилище моделей`;
  - `Папка результатов`.
- Для каждой секции реализованы:
  - ручной ввод пути;
  - выбор папки через Windows;
  - добавление адреса;
  - удаление адреса;
  - изменение порядка `Вверх` / `Вниз`;
  - лимит адреса в ГБ;
  - общий лимит категории в ГБ;
  - временное превышение в ГБ.
- Настройки сохраняются в `%LOCALAPPDATA%/AI_HUB/storage-settings.json`.
- Пункты 1 и 2 на главной странице показывают сохранённые адреса и лимиты.
- Версия повышена с `0.0.8-dev` до `0.0.9-dev`.
- Тестовые значения в `storage-settings.json` после smoke-проверки очищены до пустой структуры, чтобы не оставлять пользовательскую среду с проверочными путями.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.

### Backup

Создан backup:

- `Backups/20260531_001923_storage_settings_implementation/MainWindow.xaml`
- `Backups/20260531_001923_storage_settings_implementation/MainWindow.xaml.cs`
- `Backups/20260531_001923_storage_settings_implementation/AIHub.csproj`
- `Backups/20260531_001923_storage_settings_implementation/AppDataPaths.cs`
- `Backups/20260531_001923_storage_settings_implementation/VERSION`
- `Backups/20260531_001923_storage_settings_implementation/2026-05-31_настройки_хранилищ_моделей_и_результатов.md`
- `Backups/20260531_001923_storage_settings_implementation/CONTEXTHUB.md`
- `Backups/20260531_001923_storage_settings_implementation/Диалог_сжато.md`
- `Backups/20260531_001923_storage_settings_implementation/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_001923_storage_settings_implementation` и удалить новые файлы моделей/сервиса настроек хранения, если задача будет отменена.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Проверка ручного добавления путей подтвердила создание `%LOCALAPPDATA%/AI_HUB/storage-settings.json`.
- Smoke-проверка подтвердила сохранение нескольких адресов, общих лимитов, лимитов адресов и временного превышения.
- Smoke-проверка подтвердила отображение настроек моделей и результатов на главной странице.
- Smoke-проверка подтвердила удаление адреса и изменение порядка `Вверх`.
- Финальный `dotnet build` прошёл без ошибок и предупреждений.
- Финальный scanner кириллицы прошёл без ошибок.
- Финальная smoke-проверка подтвердила окно `AI HUB 0.0.9-dev`, наличие секций `Хранилище моделей`, `Папка результатов`, `Паспорт компьютера`, кнопок настройки и рабочей кнопки `Назад`.

## 2026-05-31 — видимые подписи лимитов адресов

Задача: добавить явное пояснение над полями лимита адреса и зафиксировать правило взаимодействия общего лимита с лимитами адресов.

### Изменения

- Над полями ручного ввода добавлены видимые подписи:
  - `Адрес хранения`;
  - `Лимит адреса, ГБ`.
- В ТЗ зафиксировано, что общий лимит категории и лимиты отдельных адресов могут отличаться.
- В ТЗ зафиксировано будущее правило применения: при реальных операциях будет действовать более строгая фактическая граница.
- Версия повышена с `0.0.9-dev` до `0.0.10-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.

### Backup

Создан backup:

- `Backups/20260531_003430_storage_limit_labels/MainWindow.xaml`
- `Backups/20260531_003430_storage_limit_labels/AIHub.csproj`
- `Backups/20260531_003430_storage_limit_labels/VERSION`
- `Backups/20260531_003430_storage_limit_labels/2026-05-31_настройки_хранилищ_моделей_и_результатов.md`
- `Backups/20260531_003430_storage_limit_labels/CONTEXTHUB.md`
- `Backups/20260531_003430_storage_limit_labels/Диалог_сжато.md`
- `Backups/20260531_003430_storage_limit_labels/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_003430_storage_limit_labels`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Smoke-проверка подтвердила наличие видимых подписей `Адрес хранения` и `Лимит адреса, ГБ`.

## 2026-05-31 — завершение настройки и кнопка Начать работу

Задача: добавить недостающую логику главной кнопки: после завершения настройки она должна меняться с `Начать настройку` на `Начать работу`.

### Изменения

- После сохранения настроек программа проверяет обязательный минимум:
  - есть хотя бы один адрес для моделей;
  - есть хотя бы один адрес для результатов.
- Если оба адреса есть, `state.json` получает `hasCompletedSetup: true`.
- Главная кнопка меняется на `Начать работу`.
- Если один из обязательных адресов отсутствует, настройка не считается завершённой.
- Пока рабочий режим не реализован, `Начать работу` показывает статус-заглушку.
- Версия повышена с `0.0.10-dev` до `0.0.11-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.

### Backup

Создан backup:

- `Backups/20260531_003613_complete_setup_primary_action/MainWindow.xaml.cs`
- `Backups/20260531_003613_complete_setup_primary_action/AIHub.csproj`
- `Backups/20260531_003613_complete_setup_primary_action/VERSION`
- `Backups/20260531_003613_complete_setup_primary_action/2026-05-31_настройки_хранилищ_моделей_и_результатов.md`
- `Backups/20260531_003613_complete_setup_primary_action/CONTEXTHUB.md`
- `Backups/20260531_003613_complete_setup_primary_action/Диалог_сжато.md`
- `Backups/20260531_003613_complete_setup_primary_action/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_003613_complete_setup_primary_action`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Smoke-проверка подтвердила, что без обязательных адресов главная кнопка остаётся `Начать настройку`.
- Smoke-проверка подтвердила, что после сохранения адреса моделей и адреса результатов главная кнопка меняется на `Начать работу`.
- Тестовые значения в `%LOCALAPPDATA%/AI_HUB/storage-settings.json` и `state.json` после проверки очищены.

## 2026-05-31 — синхронизация статуса завершённой настройки

Задача: исправить расхождение, при котором главная кнопка уже показывала `Начать работу`, а нижняя строка всё ещё писала, что настройка не завершена.

### Изменения

- Добавлен единый helper для статуса главной страницы.
- При старте приложения и при возврате кнопкой `Назад` статусная строка теперь смотрит на `hasCompletedSetup`.
- Если настройка завершена, нижняя строка показывает: `Статус: настройка завершена. Можно начать работу или изменить параметры через Перенастроить.`
- Из статусной заглушки `Начать работу` убраны лишние обратные кавычки вокруг `Перенастроить`.
- Версия повышена с `0.0.11-dev` до `0.0.12-dev`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, ТЗ и этот журнал.

### Backup

Создан backup:

- `Backups/20260531_004505_setup_status_sync/MainWindow.xaml.cs`
- `Backups/20260531_004505_setup_status_sync/AIHub.csproj`
- `Backups/20260531_004505_setup_status_sync/VERSION`
- `Backups/20260531_004505_setup_status_sync/2026-05-31_настройки_хранилищ_моделей_и_результатов.md`
- `Backups/20260531_004505_setup_status_sync/CONTEXTHUB.md`
- `Backups/20260531_004505_setup_status_sync/Диалог_сжато.md`
- `Backups/20260531_004505_setup_status_sync/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые файлы из backup-папки `Backups/20260531_004505_setup_status_sync`.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- Scanner кириллицы прошёл без ошибок.
- Smoke-проверка подтвердила окно `AI HUB 0.0.12-dev`, кнопку `Начать работу`, статус завершённой настройки и отсутствие старого текста `Настройка пока не завершена`.

## 2026-05-31 — папка для тестовых установщиков

Задача: подготовить отдельное место для будущих тестовых установщиков и зафиксировать выбранный вариант установщика.

### Изменения

- Создана папка `Тесты/Установщики`.
- Добавлен `Тесты/Установщики/README.md` с назначением папки.
- Зафиксировано решение: будущий установщик делать через Inno Setup.
- Зафиксировано требование: сборка установщика должна запускаться простым двойным кликом через `.cmd`-обводку над PowerShell-скриптом.
- Код приложения и версия не менялись.
- Commit/push не выполнялись: ТЗ установщика ещё не создано и не архивировалось.

### Backup

Создан backup:

- `Backups/20260531_005410_installer_tests_folder/CONTEXTHUB.md`
- `Backups/20260531_005410_installer_tests_folder/Диалог_сжато.md`
- `Backups/20260531_005410_installer_tests_folder/BACKUP_ОТКАТ.md`

### Откат

Для отката удалить папку `Тесты/Установщики` и восстановить документы истории из backup-папки `Backups/20260531_005410_installer_tests_folder`.

### Проверки

- Scanner кириллицы прошёл без ошибок.
- Проверено, что папка `Тесты/Установщики` создана и содержит `README.md`.

## 2026-05-31 — первый тестовый установщик Inno Setup

Задача: сделать сборку тестового установщика AI HUB двойным кликом.

### Изменения

- Создано ТЗ `ТЗ/2026-05-31_тестовый_установщик_inno_setup.md`.
- Добавлен Inno Setup сценарий `Инструменты/Installer/AI_HUB.iss`.
- Добавлен PowerShell-сборщик `Инструменты/build-installer.ps1`.
- Добавлена `.cmd`-обводка `Собрать_установщик_AI_HUB.cmd` для запуска двойным кликом.
- По прямому подтверждению пользователя установлен Inno Setup 6.7.3 через `winget`.
- В `REESTR.md` добавлен Inno Setup как внешний developer tooling.
- Версия повышена с `0.0.12-dev` до `0.0.13-dev`.
- Создан тестовый установщик `Тесты/Установщики/AI_HUB_Setup_0.0.13-dev.exe`.
- Commit/push не выполнялись: ТЗ ещё не перенесено в архив.

### Backup

Создан backup:

- `Backups/20260531_005628_inno_installer_scripts/VERSION`
- `Backups/20260531_005628_inno_installer_scripts/AIHub.csproj`
- `Backups/20260531_005628_inno_installer_scripts/README.md`
- `Backups/20260531_005628_inno_installer_scripts/REESTR.md`
- `Backups/20260531_005628_inno_installer_scripts/CONTEXTHUB.md`
- `Backups/20260531_005628_inno_installer_scripts/Диалог_сжато.md`
- `Backups/20260531_005628_inno_installer_scripts/BACKUP_ОТКАТ.md`

### Откат

Для отката:

- удалить `Собрать_установщик_AI_HUB.cmd`;
- удалить `Инструменты/build-installer.ps1`;
- удалить папку `Инструменты/Installer`;
- удалить ТЗ `ТЗ/2026-05-31_тестовый_установщик_inno_setup.md`;
- при необходимости удалить `Тесты/Установщики/AI_HUB_Setup_0.0.13-dev.exe`;
- восстановить изменённые документы из backup-папки `Backups/20260531_005628_inno_installer_scripts`.

Удалять установленный Inno Setup нужно только отдельным решением пользователя, потому что это системный developer tooling.

### Проверки

- `dotnet build` прошёл без ошибок и предупреждений.
- `build-installer.ps1` выполнил self-contained publish `win-x64`.
- Inno Setup собрал `Тесты/Установщики/AI_HUB_Setup_0.0.13-dev.exe`.
- `.cmd`-обводка проверена через `cmd.exe`: publish, сборка установщика и `pause` отработали успешно.
- Scanner кириллицы прошёл без ошибок.
- Проверено, что установщик существует: размер около `51,37 МБ`.

## 2026-05-31 — игнорирование собранных установщиков

Задача: не допустить случайной публикации тяжёлых локальных `.exe` установщиков в GitHub.

### Изменения

- В `.gitignore` добавлено правило для локальных артефактов:
  - `Тесты/Установщики/*.exe`;
  - `Тесты/Установщики/*.msi`;
  - `Тесты/Установщики/*.zip`.
- `README.md` в папке установщиков остаётся отслеживаемым.

### Backup

Создан backup:

- `Backups/20260531_010304_gitignore_installer_artifacts/.gitignore`
- `Backups/20260531_010304_gitignore_installer_artifacts/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить `.gitignore` и этот журнал из backup-папки `Backups/20260531_010304_gitignore_installer_artifacts`.

### Проверки

- Scanner кириллицы прошёл без ошибок.
- `git status --ignored` подтвердил, что `AI_HUB_Setup_0.0.13-dev.exe` игнорируется, а папка `Тесты/Установщики` остаётся доступной для `README.md`.

## 2026-05-31 — релизная пометка о пути установки

Задача: зафиксировать в расширенных правилах, что текущий dev/test-установщик ставится в пользовательскую папку, а перед релизом нужно пересмотреть установку в `Program Files`.

### Изменения

- В `Инструкции/CODEX.md` добавлена пометка:
  - для dev/test-сборок допустима установка в `%LOCALAPPDATA%\Programs`;
  - перед публичным релизом обязательно пересмотреть режим `для всех пользователей`;
  - отдельно проверить путь `C:\Program Files\AI HUB`, права администратора, uninstall-поведение и миграцию настроек.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот журнал.
- Код приложения и версия не менялись.
- Commit/push не выполнялись: ТЗ установщика ещё не архивировано.

### Backup

Создан backup:

- `Backups/20260531_010812_release_installer_note/CODEX.md`
- `Backups/20260531_010812_release_installer_note/CONTEXTHUB.md`
- `Backups/20260531_010812_release_installer_note/Диалог_сжато.md`
- `Backups/20260531_010812_release_installer_note/BACKUP_ОТКАТ.md`

### Откат

Для отката восстановить изменённые документы из backup-папки `Backups/20260531_010812_release_installer_note`.

### Проверки

- Scanner кириллицы прошёл без ошибок.
