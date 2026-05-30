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
