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

## 2026-06-01 — диагностика и восстановление llama-server

Задача: победить запуск `llama-server.exe` для текущего backend-а `llama.cpp b9442`.

### Изменения

- Создано ТЗ `ТЗ/2026-06-01_диагностика_llama_server_backend.md`.
- Создана диагностическая папка `Runtime/Backends/llama.cpp/b9442/server-diagnostic-win-cuda-12.4-x64`.
- В диагностическую папку заново распакованы официальные архивы:
  - `llama-b9442-bin-win-cuda-12.4-x64.zip`;
  - `cudart-llama-bin-win-cuda-12.4-x64.zip`.
- Найдена причина первого сбоя: в основной папке отсутствовал `llama-server-impl.dll`.
- Недостающий `llama-server-impl.dll` скопирован в `Runtime/Backends/llama.cpp/b9442/win-cuda-12.4-x64`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md`, `REESTR.md`, `THIRD_PARTY_NOTICES.md` и это ТЗ.
- Код AI HUB и версия приложения не менялись.

### Backup

Созданы backup-папки:

- `Backups/20260601_013900_llama_server_tz_start`
- `Backups/20260601_014800_llama_server_docs`

### Откат

Для отката:

- удалить `Runtime/Backends/llama.cpp/b9442/server-diagnostic-win-cuda-12.4-x64`;
- удалить `Runtime/Backends/llama.cpp/b9442/win-cuda-12.4-x64/llama-server-impl.dll`, если нужно вернуть состояние до восстановления server-а;
- восстановить документы из backup-папок.

### Проверки

- `llama-server.exe --version` в чистой диагностической папке — exit code `0`.
- `llama-server.exe --version` в основной папке после восстановления DLL — exit code `0`.
- `GET /health` — `{ "status": "ok" }`.
- `POST /v1/chat/completions` с Qwen3 8B и `--reasoning off` — ответ `Основной сервер работает.`
- `llama-cli.exe` fallback не тронут.
- Защита Windows не отключалась, исключения Defender не добавлялись.

## 2026-06-01 — интеграция llama-server в debug-окно

Задача: вторым шагом текущего ТЗ подключить `llama-server.exe` к debug-окну AI HUB.

### Изменения

- Добавлен `Исходники/AIHub/Services/LlamaServerRuntimeService.cs`.
- `DebugChatWindow` теперь использует server-backend как основной runtime.
- `llama-cli` оставлен fallback-режимом.
- Server запускается скрытым дочерним процессом на автоматически выбранном loopback-порту.
- Перед запросом выполняется `/health`.
- Chat-запросы отправляются в `/v1/chat/completions`.
- Server запускается с `--reasoning off`.
- При закрытии debug-окна server останавливается.
- Кнопка `Стоп` отменяет запрос и останавливает server.
- Версия повышена до `0.0.21-dev`.

### Backup

Создан backup:

- `Backups/20260601_020500_llama_server_integration`

### Откат

Для отката:

- удалить `Исходники/AIHub/Services/LlamaServerRuntimeService.cs`;
- восстановить изменённые файлы из backup-папки;
- `llama-cli` fallback оставить.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` прошёл успешно, без предупреждений.
- `Инструменты/check-cyrillic-integrity.ps1` прошёл успешно.
- Ключи `ru.json` и `en.json` совпадают: `173/173`.
- Win32 smoke-test: `F12` открывает окно `AI HUB — отладка моделей`, процесс не падает.
- Server endpoint smoke-test: `/health` возвращает `ok`.
- Server endpoint prompt-test: ответ `Сервер бэкенда работает корректно.`
- Установщик не собирался по правилу проекта.

## 2026-06-01 — перенос текста в debug-чате и логах

Задача: убрать горизонтальный скроллинг в debug-окне.

### Изменения

- Для `ChatListBox` и `LogListBox` отключён горизонтальный scrollbar.
- Элементы списков отображаются через `TextBlock` с `TextWrapping=Wrap`.
- Перенос идёт по словам, без резки слов.
- Вертикальный scrollbar сохранён.
- Версия повышена до `0.0.22-dev`.

### Backup

Создан backup:

- `Backups/20260601_021800_debug_wrap_lists`

### Откат

Для отката восстановить изменённые файлы из backup-папки.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` прошёл успешно, без предупреждений.
- `Инструменты/check-cyrillic-integrity.ps1` прошёл успешно.
- Установщик не собирался по правилу проекта.

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

## 2026-06-01 — закрытие ТЗ диагностики llama-server

Задача: закрыть выполненное ТЗ `2026-06-01_диагностика_llama_server_backend.md`, перенести его в архив и опубликовать актуальное состояние проекта в GitHub.

### Изменения

- ТЗ перенесено из `ТЗ/2026-06-01_диагностика_llama_server_backend.md` в `ТЗ/Архив/2026-06-01_диагностика_llama_server_backend.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот rollback-журнал.
- Итоговая версия задачи: `0.0.22-dev`.
- Runtime backend `Runtime/Backends/llama.cpp/**` и скачанная модель `Данные_для_внедрения/Модели/**` остаются локальными и не публикуются в GitHub.
- Установщик не собирался по правилу проекта.

### Backup

Создан backup:

- `Backups/20260601_021838_archive_llama_server_task/BACKUP_ОТКАТ.md`
- `Backups/20260601_021838_archive_llama_server_task/CONTEXTHUB.md`
- `Backups/20260601_021838_archive_llama_server_task/Диалог_сжато.md`
- `Backups/20260601_021838_archive_llama_server_task/ТЗ__2026-06-01_диагностика_llama_server_backend.md`

### Откат

Для отката закрытия:

- вернуть ТЗ из `ТЗ/Архив/2026-06-01_диагностика_llama_server_backend.md` обратно в `ТЗ/2026-06-01_диагностика_llama_server_backend.md`;
- восстановить документы истории из backup-папки `Backups/20260601_021838_archive_llama_server_task`;
- если commit/push уже выполнены, откат публикации делать отдельным согласованным git-действием.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` — успешно, предупреждений `0`, ошибок `0`.
- `Инструменты\check-cyrillic-integrity.ps1` — успешно, проверено `90` текстовых файлов.
- Сверка локализаций — успешно, ключи `ru.json` и `en.json` совпадают `173/173`.
- Активных ТЗ вне архива нет, кроме служебного `ТЗ/README.md`.
- Runtime/model остаются игнорируемыми Git: `Runtime/Backends/llama.cpp/`, `Runtime/Publish/`, `Данные_для_внедрения/Модели/`.
- `F12` smoke-test — debug-окно открывается, приложение не падает.
- Endpoint-test `llama-server` — `/health` готов, `/v1/chat/completions` вернул ответ `Да, сервер AI HUB работает.`
- Установщик не собирался по правилу проекта.

## 2026-06-01 — контекст ядра и JSONL-журналы сессий

Задача: реализовать скрытый служебный контекст ядра, автоматическое примерное местоположение по IP, core-session файл на запуск программы и отдельную debug-зону для `F12`.

### Изменения

- Создано рабочее ТЗ `ТЗ/2026-06-01_контекст_ядра_и_журнал_сессий.md`.
- Добавлен локальный профиль `%LOCALAPPDATA%\AI_HUB\user-profile.json`.
- Добавлены сервисы:
  - `UserProfileStore`;
  - `IpLocationService`;
  - `UserContextService`;
  - `SessionPathService`;
  - `JsonlSessionLog`.
- Добавлены модели:
  - `UserProfile`;
  - `UserLocation`;
  - `UserContextSnapshot`.
- `MainWindow` создаёт core-session `jsonl` при запуске и пишет `session_end` при штатном закрытии.
- `DebugChatWindow` создаёт отдельный debug-session `jsonl`.
- `llama-server` и `llama-cli` получают скрытый служебный контекст даты/времени/местоположения.
- `.gitignore` расширен правилами для пользовательских результатов `AI_HUB\Core\Sessions`, `AI_HUB\Debug\ModelTester`, `AI_HUB\Tasks`, `AI_HUB\Tools`.
- `REESTR.md` и `THIRD_PARTY_NOTICES.md` обновлены под внешний сервис `ipwho.is`.
- Версия повышена до `0.0.23-dev`.
- Установщик не собирался по правилу проекта.

### Backup

Создан backup:

- `Backups/20260601_024623_core_context_sessions/VERSION`
- `Backups/20260601_024623_core_context_sessions/BACKUP_ОТКАТ.md`
- `Backups/20260601_024623_core_context_sessions/CONTEXTHUB.md`
- `Backups/20260601_024623_core_context_sessions/Диалог_сжато.md`
- `Backups/20260601_024623_core_context_sessions/THIRD_PARTY_NOTICES.md`
- `Backups/20260601_024623_core_context_sessions/Документы_проекта__REESTR.md`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__AIHub.csproj`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__Services__AppDataPaths.cs`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__Services__LlamaServerRuntimeService.cs`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__Services__LlamaCliRuntimeService.cs`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__DebugChatWindow.xaml.cs`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__MainWindow.xaml.cs`
- `Backups/20260601_024623_core_context_sessions/Исходники__AIHub__App.xaml.cs`
- Дополнительно после обнаружения изменения `.gitignore` сохранены:
  - `Backups/20260601_024623_core_context_sessions/.gitignore__before_task` — версия из Git до текущего ТЗ;
  - `Backups/20260601_024623_core_context_sessions/.gitignore__after_task` — версия после добавления ignore-правил.

### Откат

Для отката:

- восстановить изменённые файлы из backup-папки `Backups/20260601_024623_core_context_sessions`;
- для `.gitignore` использовать `Backups/20260601_024623_core_context_sessions/.gitignore__before_task`;
- удалить новые модели и сервисы контекста/журналов;
- удалить ТЗ `ТЗ/2026-06-01_контекст_ядра_и_журнал_сессий.md`;
- при необходимости удалить локальные пользовательские файлы `%LOCALAPPDATA%\AI_HUB\user-profile.json` и созданные `jsonl`-сессии из выбранной папки результатов.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` — успешно, предупреждений `0`, ошибок `0`.
- `Инструменты\check-cyrillic-integrity.ps1` — успешно.
- Живой запуск приложения создал core-session `jsonl`.
- Штатное закрытие окна записало `session_end`.
- Core-session JSONL валиден построчно и содержит видимую UTF-8 кириллицу.
- `F12` smoke-test прошёл, debug-окно открылось.
- Debug-session JSONL создан в отдельной debug-зоне, валиден построчно и содержит видимую UTF-8 кириллицу.
- Git показывает созданные сессии в папке результатов как ignored.

## 2026-06-01 — закрытие ТЗ контекста ядра и JSONL-сессий

Задача: закрыть выполненное ТЗ `2026-06-01_контекст_ядра_и_журнал_сессий.md`, перенести его в архив и опубликовать актуальное состояние проекта в GitHub.

### Изменения

- ТЗ перенесено из `ТЗ/2026-06-01_контекст_ядра_и_журнал_сессий.md` в `ТЗ/Архив/2026-06-01_контекст_ядра_и_журнал_сессий.md`.
- Обновлены `CONTEXTHUB.md`, `Диалог_сжато.md` и этот rollback-журнал.
- Итоговая версия задачи: `0.0.23-dev`.
- Установщик не собирался по правилу проекта.

### Backup

Создан backup:

- `Backups/20260601_030248_archive_core_context_sessions_task/BACKUP_ОТКАТ.md`
- `Backups/20260601_030248_archive_core_context_sessions_task/CONTEXTHUB.md`
- `Backups/20260601_030248_archive_core_context_sessions_task/Диалог_сжато.md`
- `Backups/20260601_030248_archive_core_context_sessions_task/ТЗ__2026-06-01_контекст_ядра_и_журнал_сессий.md`

### Откат

Для отката закрытия:

- вернуть ТЗ из `ТЗ/Архив/2026-06-01_контекст_ядра_и_журнал_сессий.md` обратно в `ТЗ/2026-06-01_контекст_ядра_и_журнал_сессий.md`;
- восстановить документы истории из backup-папки `Backups/20260601_030248_archive_core_context_sessions_task`;
- если commit/push уже выполнены, откат публикации делать отдельным согласованным git-действием.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` — успешно, предупреждений `0`, ошибок `0`.
- `Инструменты\check-cyrillic-integrity.ps1` — успешно, проверено `99` текстовых файлов.
- Сверка локализаций — успешно, ключи `ru.json` и `en.json` совпадают `173/173`.
- JSONL-проверка — последние core/debug session-файлы валидны построчно.
- Активных ТЗ вне архива нет, кроме служебного `ТЗ/README.md`.
- Пользовательские сессии в `Тесты/1/AI_HUB/**`, runtime backend и GGUF-модель отображаются как ignored и не публикуются в GitHub.
- Установщик не собирался по правилу проекта.

## 2026-06-01 — интернет-инструменты ядра

Задача: реализовать первый слой интернет-инструментов для ядра AI HUB: поиск, чтение страницы, скачивание файла, проверка через `F12` и сценарий, где ядро само просит инструмент.

### Изменения

- Создано ТЗ `ТЗ/2026-06-01_интернет_инструменты_ядра.md`.
- Добавлены модели результата web-инструментов и прогресса скачивания.
- Добавлены сервисы `ToolGateway`, `WebSearchTool`, `WebPageReaderTool`, `WebDownloadTool`, `WebToolPathService`.
- `DebugChatWindow` получил ручные команды `web_search:`, `web_read:`, `web_download:` и сценарий `core_tool_test:`.
- `WebDownloadTool` показывает визуальный прогресс скачивания в F12-окне.
- Скачанные web-результаты сохраняются в выбранную папку результатов `AI_HUB\Tools\Web\...` и остаются ignored Git.
- Версия повышена до `0.0.24-dev`.
- Установщик не собирался по правилу проекта.

### Backup

Созданы backup-папки:

- `Backups/20260601_032733_core_web_tools`
- `Backups/20260601_035900_download_progress_ui`

### Откат

Для отката:

- восстановить изменённые файлы из backup-папок;
- удалить новые файлы `Models/Web*` и `Services/Web*`, `Services/ToolGateway.cs`;
- удалить ТЗ `ТЗ/2026-06-01_интернет_инструменты_ядра.md`;
- удалить локальные тестовые web-загрузки только по отдельному решению пользователя, так как они находятся в пользовательской папке результатов и игнорируются Git.

### Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` — успешно, предупреждений `0`, ошибок `0`.
- Smoke-test инструментов: `web_search`, `web_read`, `web_download` сработали; первая попытка через Bing RSS заменена на DuckDuckGo Lite из-за нерелевантной выдачи.
- `core_tool_test` подтвердил, что ядро запросило `web_download:` и AI HUB скачал `Qwen3-0.6B-Q4_K_M.gguf`.
- `Инструменты\check-cyrillic-integrity.ps1` — успешно, проверено `116` текстовых файлов.
- Сверка локализаций — успешно, ключи `ru.json` и `en.json` совпадают `184/184`.
- JSONL core-tool проверки валиден построчно: `Тесты\1\AI_HUB\Debug\ModelTester\Sessions\2026-06-01_03-47-22_debug-model-tester_435cef3c78024fd7aaadcaa7e35a5473.jsonl`.
- Git показывает `Тесты\1\AI_HUB\Tools\Web\Downloads` как ignored через родительскую папку `Тесты/1/`.

### Дополнение: полный инструментальный debug-режим

- Backup перед правкой: `Backups/20260601_041500_debug_full_tool_mode`.
- Обычные prompt-ы в F12-окне теперь идут через debug-ядро с инструментами.
- Старый прямой режим `llama-server` без инструментов больше не используется для обычных debug-сообщений; ручные `web_*` команды всё ещё выполняются напрямую.
- Версия повышена до `0.0.25-dev`.
- Smoke-test: обычный запрос на скачивание `https://example.com/` привёл к `tool_request` `web_download` и файлу `Тесты\1\AI_HUB\Tools\Web\Downloads\2026-06-01_04-03-30_download.bin`.

### Дополнение: определение типа скачанного файла

- Backup перед правкой: `Backups/20260601_041900_download_type_detection`.
- Причина: при запросе фото котика ядро скачало HTML-страницу поиска Яндекс.Картинок как файл без расширения.
- `WebDownloadTool` теперь добавляет расширение по `Content-Type`, возвращает `Content-Kind`, `ExtensionWasAdded`, `Warning`.
- Agent-loop больше не считает любой `web_download` финальным успехом; результат возвращается ядру для проверки соответствия задаче.
- Версия повышена до `0.0.26-dev`.
- Smoke-test: URL Яндекс.Картинок сохранён как `Тесты\1\AI_HUB\Tools\Web\Downloads\2026-06-01_04-11-23_search.html`, результат содержит `Content-Kind: html` и предупреждение, что это не прямой файл/картинка.
- Проверки после правки: `dotnet build` успешно, scanner кириллицы успешно (`118` текстовых файлов), локализации совпадают `186/186`, JSONL smoke-test валиден и содержит `Content-Kind: html` + `Warning`.

### Дополнение: универсальные типы файлов и recovery отказа

- Backup перед правкой: `Backups/20260601_042200_tool_refusal_recovery`.
- `Content-Kind` расширен до `html`, `json`, `text`, `image`, `document`, `audio`, `video`, `archive`, `binary`.
- `ToolGateway` теперь возвращает ошибки инструментов как текстовый `Tool error`, а не бросает исключение наверх.
- Если debug-ядро на первом шаге отвечает отказом `нет доступа`, AI HUB запускает fallback `web_search` по запросу пользователя и возвращает результат ядру.
- Версия повышена до `0.0.27-dev`.
- Smoke-test типов файлов: HTML, JSON, TXT, PNG, JPEG, PDF, MP3 и `403 Forbidden` отработали ожидаемо; MP3 сохранён как `.mp3` с `Content-Kind: audio`.

### Дополнение: несколько источников на одну задачу

- Backup перед правкой: `Backups/20260601_043000_multi_source_tool_loop`.
- `WebSearchTool` теперь возвращает до 10 результатов вместо 8.
- F12 agent-loop теперь допускает до 10 tool-запросов на одну задачу и после лимита просит ядро выбрать лучший результат из уже найденного.
- Prompt debug-ядра дополнен правилом: не ограничиваться первым сайтом, проверять несколько источников, продолжать после ошибки, HTML вместо файла или неподходящего результата.
- Версия повышена до `0.0.28-dev`.
- Проверки после правки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`122` текстовых файла), локализации совпадают `186/186`.
- Установщик не собирался по правилу проекта.
- Откат: восстановить `VERSION`, `AIHub.csproj`, `DebugChatWindow.xaml.cs`, `WebSearchTool.cs`, `CONTEXTHUB.md`, `Диалог_сжато.md`, `BACKUP_ОТКАТ.md` и ТЗ из backup-папки `Backups/20260601_043000_multi_source_tool_loop`.

### Дополнение: запрет ложного финала без скачивания

- Backup перед правкой: `Backups/20260601_044000_download_final_guard`.
- Причина: debug-ядро могло завершить задачу скачивания после нескольких `web_search`, написав что файл найден/сохранён, хотя `web_download` не выполнялся.
- Добавлен guard для download-задач: если запрос содержит `скачать`/`download`, `FINAL:` без успешного `Web download complete` блокируется.
- Если лимит tool-запросов достигнут без скачивания, AI HUB возвращает честный ответ: файл не скачан.
- Версия повышена до `0.0.29-dev`.
- Проверки после правки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`158` текстовых файлов), локализации совпадают `186/186`.
- Установщик не собирался по правилу проекта.
- Откат: восстановить `VERSION`, `AIHub.csproj`, `DebugChatWindow.xaml.cs`, `CONTEXTHUB.md`, `Диалог_сжато.md`, `BACKUP_ОТКАТ.md` и ТЗ из backup-папки `Backups/20260601_044000_download_final_guard`.

### Дополнение: прямые файлы из `web_read`

- Backup перед правкой: `Backups/20260601_044500_read_file_download_redirect`.
- Причина: debug-ядро находило прямой файловый URL, но вызывало `web_read`, из-за чего скачивание не происходило.
- Для задач скачивания `web_read` по прямому файловому URL теперь автоматически нормализуется в `web_download`.
- `WebDownloadTool` отправляет `User-Agent` и `Accept`, чтобы сайты, чувствительные к пустым HTTP-заголовкам, меньше отклоняли запросы.
- `WebPageReaderTool` теперь извлекает кандидаты прямых файлов из HTML-атрибутов `href`, `src`, `srcset`, `content` и возвращает их в результате `web_read`.
- Версия повышена до `0.0.30-dev`.
- Smoke-test: `web_read` статьи Wikipedia нашёл кандидаты файлов, а `web_download` успешно скачал `960px-V_Putin_2026.png` в `Tools\Web\Downloads`.
- Проверки после правки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`164` текстовых файла), локализации совпадают `186/186`.
- Установщик не собирался по правилу проекта.
- Откат: восстановить `VERSION`, `AIHub.csproj`, `DebugChatWindow.xaml.cs`, `WebDownloadTool.cs`, `WebPageReaderTool.cs`, `ToolGateway.cs`, `WebPageReadResponse.cs`, документы истории и ТЗ из backup-папки `Backups/20260601_044500_read_file_download_redirect`.

### Дополнение: авто-загрузка служебного reranker

- Backup перед правкой: `Backups/20260601_051500_reranker_autodownload`.
- Добавлены новые файлы `Models/ToolModelFileManifest.cs`, `Models/ToolModelManifest.cs`, `Services/ToolModelManager.cs`.
- `ToolModelManager` скачивает `BAAI/bge-reranker-v2-m3` после основного ядра в выбранную папку моделей `Tools/Reranker/BAAI-bge-reranker-v2-m3`, поддерживает `.part`, проверяет размер и SHA-256 для крупных файлов и пишет `tool-model.json`.
- `F12` discovery теперь видит `tool-model.json` и показывает служебные модели, но помечает их как `tool-only` и не разрешает запускать как чат-модель.
- Версия повышена до `0.0.31-dev`.
- Модель вручную не скачивалась: пользователь хотел проверить, что это сделает сама программа.
- Проверки после правки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`194` текстовых файла), локализации совпадают `199/199`.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из backup-папки `Backups/20260601_051500_reranker_autodownload`, удалить новые файлы `ToolModelFileManifest.cs`, `ToolModelManifest.cs`, `ToolModelManager.cs`, вернуть `VERSION`/`AIHub.csproj` на предыдущую версию. Если программа успеет скачать reranker, удалять его только по отдельному решению пользователя, так как это файл в пользовательской папке моделей.

### Дополнение: подключение reranker к `web_search`

- Backup перед правкой: `Backups/20260601_052500_reranker_search_logic`.
- Пользователь подтвердил, что `BAAI bge-reranker-v2-m3` скачалась и отображается в F12.
- Установлено локальное dev-окружение `Runtime/Python/reranker/.venv` с `torch 2.12.0`, `transformers 5.9.0`, `safetensors 0.7.0`.
- Добавлены `WebSearchRerankerService`, `WebSearchRerankInfo`, скрипт `Tools/bge_rerank.py`.
- `web_search` теперь после поиска пересортировывает результаты через reranker; при недоступности runtime/model использует `lexical-fallback`.
- Версия повышена до `0.0.32-dev`.
- Проверка скрипта напрямую: модель вернула высокий score для релевантного результата и низкий для нерелевантного.
- Scanner кириллицы сначала сработал на сторонние файлы `tokenizer.json` скачанной модели и LICENSE внутри `.venv`; это не поломка русских строк проекта. Scanner обновлён: теперь исключает runtime-папки `Runtime`, `.venv` и скачанные `Модели`.
- Backup scanner-правки: `Backups/20260601_053000_scanner_runtime_exclusions`.
- Повторная проверка scanner после исключений: успешно, проверено `179` текстовых файлов.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из backup-папки `Backups/20260601_052500_reranker_search_logic`, удалить новые файлы `WebSearchRerankerService.cs`, `WebSearchRerankInfo.cs`, `Tools/bge_rerank.py`; runtime-папку `Runtime/Python/reranker/.venv` удалять только по отдельному решению пользователя.

### Закрытие ТЗ интернет-инструментов и новое ТЗ возможностей

- Backup перед созданием нового ТЗ и архивированием текущего: `Backups/20260601_065000_new_capability_tz_and_close_web_tz`.
- Создано новое ТЗ `ТЗ/2026-06-01_реестр_возможностей_и_huggingface_provider.md`.
- Текущее ТЗ `ТЗ/2026-06-01_интернет_инструменты_ядра.md` подготовлено к переносу в архив как выполненное.
- Причина: основная часть интернет-инструментов реализована; новые идеи выделены в отдельный следующий этап.
- ТЗ перенесено в `ТЗ/Архив/2026-06-01_интернет_инструменты_ядра.md`.
- Финальные проверки закрытия: `dotnet build` успешно, scanner кириллицы успешно (`203` текстовых файла), локализации совпадают `199/199`.
- Установщик не собирался по правилу проекта.
- Commit/push выполняется по правилу закрытия ТЗ.
- Откат: вернуть ТЗ интернет-инструментов из backup-папки или из `ТЗ/Архив`, удалить новое ТЗ, если пользователь решит не продолжать этот этап.

### Исправление установленной версии: упаковка backend llama.cpp

- Backup перед правкой: `Backups/20260601_072500_installer_backend_runtime`.
- Причина: установленная версия искала `llama-server.exe` и `llama-cli.exe` в `%LOCALAPPDATA%\AI_HUB\Runtime\Backends\llama.cpp\b9442\win-cuda-12.4-x64`, но установщик не копировал туда backend-файлы.
- Изменены `Инструменты/build-installer.ps1` и `Инструменты/Installer/AI_HUB.iss`: сборка установщика теперь требует локальный backend и пакует его в пользовательский runtime-каталог установленной версии.
- Версия повышена до `0.0.33-dev`.
- Установщик пересобран как исключение из правила "только по команде", потому что баг проявляется именно в установленной версии.
- Новый установщик: `Тесты\Установщики\AI_HUB_Setup_0.0.33-dev.exe`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно, локализации совпадают `199/199`, Inno Setup сборка прошла успешно.
- Откат: восстановить `VERSION`, `Исходники/AIHub/AIHub.csproj`, `Инструменты/build-installer.ps1`, `Инструменты/Installer/AI_HUB.iss` и документы истории из backup-папки. Затем пересобрать установщик предыдущей версии, если нужно проверить старое поведение.

### Реестр возможностей, Hugging Face provider и диагностика нулевого поиска

- Backup перед правкой: `Backups/20260601_075500_search_empty_diagnostics`.
- Дополнительный backup перед правкой reranker-скрипта: `Backups/20260601_080600_reranker_bom_fix`.
- Дополнительный backup перед обновлением `REESTR.md`: `Backups/20260601_080900_reestr_hf_provider`.
- Причина: ядро в F12 поверхностно отвечало по актуальным новостям, могло писать о найденном при `0` результатах, не анализировало причину пустой выдачи и ещё не имело общего inventory/task-planner/Hugging Face provider из текущего ТЗ.
- Добавлены новые модели `CapabilityInventory*`, `TaskPlanResponse`, `HuggingFace*` и сервисы `CapabilityInventoryService`, `TaskPlannerService`, `HuggingFaceProviderTool`.
- `ToolGateway` получил команды `inventory:`, `task_plan:`, `hf_find_model:`, `hf_model_files:` и расширенный диагностический вывод `web_search`.
- `DebugChatWindow` получил правила: не финализировать актуальные факты при пустом поиске, пробовать fallback-запросы включая английский, а при найденных результатах читать страницы через `web_read`.
- `WebSearchTool` теперь возвращает статус, количество результатов, HTTP-статус, вероятную причину и следующие шаги; при пустом DuckDuckGo Lite пробует DuckDuckGo HTML.
- Исправлен `Tools/bge_rerank.py`: входной JSON читается через `utf-8-sig`, чтобы BOM не ломал reranker и не включал слабый fallback.
- `hf_find_model` теперь поддерживает многословный `query=...`.
- `REESTR.md` обновлён для внешнего сервиса Hugging Face Hub API и уточнён статус `BAAI bge-reranker-v2-m3` как уже подключённого к `web_search`.
- Версия повышена до `0.0.34-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`259` текстовых файлов), локализации совпадают `199/199`, smoke-test `inventory`, `task_plan`, `hf_find_model`, `web_search` с результатами и `web_search` с нулём результатов прошёл.
- Установщик не собирался по правилу проекта, потому что задача не требует проверки installed-only поведения.
- Откат: восстановить изменённые файлы из `Backups/20260601_075500_search_empty_diagnostics`, отдельно восстановить `Исходники/AIHub/Tools/bge_rerank.py` из `Backups/20260601_080600_reranker_bom_fix`, восстановить `REESTR.md` из `Backups/20260601_080900_reestr_hf_provider`, удалить новые файлы моделей/сервисов capability/HuggingFace/task planner и вернуть `VERSION`/`AIHub.csproj` на предыдущую версию.

### Архитектурный принцип Codex-подобной среды

- Backup перед правкой: `Backups/20260601_081500_codex_like_concept`.
- Причина: пользователь уточнил ключевую концепцию проекта — AI HUB должен быть Codex-подобной рабочей средой для локальных моделей, шире кодинга и с инструментальным каркасом вокруг слабых локальных моделей.
- Обновлены `Инструкции/AGENTS.md`, `Инструкции/CODEX.md`, `Документы_проекта/АРХИТЕКТУРА.md`, `CONTEXTHUB.md`, `Диалог_сжато.md`.
- Код не менялся, версия не повышалась.
- Откат: восстановить перечисленные документы из backup-папки `Backups/20260601_081500_codex_like_concept`.

### Закрытие ТЗ реестра возможностей без GitHub-публикации

- Backup перед переносом ТЗ и записями: `Backups/20260601_083000_close_capability_tz_no_publish`.
- ТЗ `ТЗ/2026-06-01_реестр_возможностей_и_huggingface_provider.md` перенесён в `ТЗ/Архив/2026-06-01_реестр_возможностей_и_huggingface_provider.md`.
- Пользователь прямо указал закрыть текущий ТЗ без публикации. GitHub commit/push не выполнялись как исключение из правила архивации.
- Код не менялся в рамках самого закрытия, версия не повышалась.
- Откат: вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ` или восстановить исходный файл и документы из backup-папки `Backups/20260601_083000_close_capability_tz_no_publish`.

### Стратегический web research для F12

- Backup перед правкой: `Backups/20260601_084500_web_research_strategy`.
- Создано ТЗ `ТЗ/2026-06-01_стратегический_web_research.md`.
- Добавлены модели `WebResearchAttempt`, `WebResearchPage`, `WebResearchSource`, `WebResearchResponse`.
- Добавлен сервис `SearchStrategyService`: генерация нескольких запросов, запуск поиска, фильтрация нерелевантных результатов, чтение до 3 страниц и сохранение JSON в `Tools/Web/Research`.
- `ToolGateway` получил команду `web_research:`.
- `DebugChatWindow` обновлён: для актуальных фактов и новостей debug-ядро должно предпочитать `web_research`; успешный research считается уже прочитанным источником.
- `WebSearchTool` получил Bing HTML fallback после пустого DuckDuckGo и распаковку `bing.com/ck/...` ссылок до реальных URL.
- `REESTR.md` обновлён для временного внешнего сервиса Bing HTML Search.
- Версия повышена до `0.0.35-dev`.
- Проверки: обычный `dotnet build` успешно без предупреждений; scanner кириллицы успешно (`177` текстовых файлов); локализации совпадают `199/199`; smoke-test `web_research` по космическим новостям дал `Research status: ok` и 3 прочитанных источника; заведомо пустой точный запрос дал `Research status: empty`.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260601_084500_web_research_strategy`, удалить новые файлы `Models/WebResearch*.cs` и `Services/SearchStrategyService.cs`, вернуть `VERSION`/`AIHub.csproj` на предыдущую версию.

### Дополнение web research: датированные пункты и F12 без лимита ответа

- Backup перед правкой датированных пунктов: `Backups/20260601_094500_web_research_dated_items`.
- Backup текущего состояния перед правкой длины debug-ответов: `Backups/20260601_095300_debug_output_length`.
- Добавлен `WebResearchDatedItem`, блок `Dated items` в `web_research` и фильтр дат под запросы вроде "за 3 дня".
- Исправлена устойчивость `web_research`: ошибки/таймауты отдельных поисковых провайдеров фиксируются как попытки поиска и не должны ломать весь инструмент.
- В debug-runtime убран искусственный лимит генерации: `llama-server` не отправляет `max_tokens`, `llama-cli` использует `--predict -1`.
- Версия повышена до `0.0.37-dev`.
- Проверка: `dotnet build` успешно без предупреждений.
- Установщик не собирался по правилу проекта.
- Откат: восстановить файлы из указанных backup-папок, удалить `Models/WebResearchDatedItem.cs`, вернуть `VERSION`/`AIHub.csproj` на нужную предыдущую версию.

### Закрытие ТЗ стратегического web research без GitHub-публикации

- Backup перед переносом ТЗ и записями: `Backups/20260601_104500_close_web_research_no_publish_and_new_debug_tz`.
- ТЗ `ТЗ/2026-06-01_стратегический_web_research.md` перенесён в `ТЗ/Архив/2026-06-01_стратегический_web_research.md`.
- Пользователь прямо указал закрыть текущий ТЗ без публикации. GitHub commit/push не выполнялись как исключение из правила архивации.
- Новый ТЗ пока не создан по уточнению пользователя: сначала обсуждение следующего этапа.
- Код не менялся в рамках закрытия, версия не повышалась.
- Откат: вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ` или восстановить исходный файл и документы из backup-папки `Backups/20260601_104500_close_web_research_no_publish_and_new_debug_tz`.

### Structured tool-calling для F12

- Backup перед правкой: `Backups/20260601_111500_structured_tool_calling_f12`.
- Создано ТЗ `ТЗ/2026-06-01_structured_tool_calling_f12.md`.
- Причина: старый F12 tool-agent опирался на текстовые команды вида `web_search: ...`, из-за чего модель могла писать намерение вызвать инструмент, но не выдавать строгую команду.
- Добавлена модель structured tool-call данных `Models/StructuredToolCall.cs`.
- `LlamaServerRuntimeService` теперь умеет отправлять `tools` в `/v1/chat/completions` и читать `message.tool_calls`.
- `DebugChatWindow` разделяет обычный чат, прямые команды инструментов и structured tool-agent; старый текстовый протокол оставлен как fallback.
- В structured-режиме описаны инструменты `web_search`, `web_research`, `web_read`, `web_download`, `inventory`, `task_plan`, `hf_find_model`, `hf_model_files`.
- `web_download` блокируется, если URL не был дан пользователем и не найден предыдущими инструментами.
- Новые видимые строки debug-лога добавлены в `ru.json` и `en.json`.
- Версия повышена до `0.0.38-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно, локализации совпадают `203/203`, независимый smoke-test `llama-server` вернул structured `web_search` tool call.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260601_111500_structured_tool_calling_f12`, удалить `Исходники/AIHub/Models/StructuredToolCall.cs`, вернуть `VERSION`/`AIHub.csproj` на предыдущую версию и при необходимости удалить диагностическую папку `Тесты/1/AI_HUB/Diagnostics/structured_tool_calling_smoke_2026-06-01_0.0.38-dev`.

### Закрытие ТЗ structured tool-calling

- Backup перед переносом ТЗ и записями: `Backups/20260601_223500_close_structured_tool_calling_tz`.
- ТЗ `ТЗ/2026-06-01_structured_tool_calling_f12.md` перенесён в `ТЗ/Архив/2026-06-01_structured_tool_calling_f12.md`.
- В `.gitignore` добавлено правило `**/AI_HUB/Diagnostics/**`, чтобы локальные diagnostic-прогоны не попадали в GitHub.
- Финальные проверки закрытия: `dotnet build` успешно без предупреждений, scanner кириллицы успешно, локализации совпадают `203/203`.
- Установщик не собирался по правилу проекта.
- По правилу архивации ТЗ выполнен commit/push в GitHub: commit `0b60253`, push в `origin/main`.
- Backup перед записью commit/push в историю: `Backups/20260601_224000_record_structured_tool_calling_push`.
- Откат: восстановить документы и `.gitignore` из backup-папки, вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ` или восстановить его из backup.

### Профиль пользователя

- Backup перед правкой: `Backups/20260601_225500_user_profile_page`.
- Создано ТЗ `ТЗ/2026-06-01_профиль_пользователя.md`.
- Добавлена страница профиля пользователя в основное окно: имя/ник, ручное местоположение, предпочтения ответов и режим нагрузки.
- Добавлена кнопка профиля в верхнюю панель. При неполном профиле она мягко моргает.
- При `Начать работу` и неполном профиле показывается промежуточная страница с предложением дозаполнить профиль или продолжить без него.
- Расширены `UserProfile`, `UserProfileStore` и `UserContextService`; профиль хранится локально в `user-profile.json`, добавлен `profileVersion`.
- Новые строки добавлены в `ru.json` и `en.json`.
- Версия повышена до `0.0.39-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`186` текстовых файлов), локализации совпадают `241/241`, smoke-запуск приложения прошёл и окно закрылось штатно.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260601_225500_user_profile_page`, удалить активное ТЗ `ТЗ/2026-06-01_профиль_пользователя.md`, вернуть `VERSION`/`AIHub.csproj` на предыдущую версию.

### Внешний smoke-тест на слабом ноутбуке

- Backup перед записью истории: `Backups/20260601_235814_external_weak_laptop_smoke`.
- Пользователь сообщил о ручном тесте старой сборки на втором ПК: очень слабом ноутбуке.
- Подтверждено: установленная версия AI HUB не привязана к ПК разработки; программа стартовала, нужные компоненты скачались, ядро в F12 запустилось.
- Ограничение: ноутбук слишком медленный для регулярного тестирования, но факт переносимости зафиксирован.
- Код не менялся, версия не повышалась, GitHub commit/push не выполнялись.
- Откат: восстановить документы истории из backup-папки `Backups/20260601_235814_external_weak_laptop_smoke`.

### Закрытие ТЗ профиля пользователя

- Backup перед переносом ТЗ и записями: `Backups/20260602_000500_close_user_profile_tz`.
- ТЗ `ТЗ/2026-06-01_профиль_пользователя.md` перенесено в `ТЗ/Архив/2026-06-01_профиль_пользователя.md`.
- Пользователь подтвердил, что всё работает и ТЗ выполнено.
- Закрытый состав: профиль пользователя, страница дозаполнения перед началом работы, локальное хранение `user-profile.json`, профильный контекст ядра, локализация и версия `0.0.39-dev`.
- Финальные проверки закрытия: `dotnet build` успешно без предупреждений, scanner кириллицы успешно, локализации совпадают `241/241`.
- Установщик не собирался по правилу проекта.
- По правилу архивации ТЗ выполнен commit/push в GitHub: commit `d116a15`, push в `origin/main`.
- Backup перед записью commit/push в историю: `Backups/20260602_001500_record_user_profile_push`.
- Откат: восстановить документы и ТЗ из `Backups/20260602_000500_close_user_profile_tz`, вернуть ТЗ из `ТЗ/Архив` обратно в `ТЗ` при необходимости.

### Самоидентификация ядра

- Backup перед правкой: `Backups/20260602_001900_core_identity_roles`.
- Создано ТЗ `ТЗ/2026-06-02_самоидентификация_ядра.md`.
- Добавлен `CoreIdentityService`: единый скрытый паспорт ядра, программы AI HUB, текущей модели, backend-а и режима взаимодействия.
- Добавлен `ToolMessageFormatter`: результаты инструментов для модели заворачиваются в `[AI_HUB_TOOL_RESULT]` и явно помечаются как не пользовательские сообщения.
- `llama-server`, structured tool-calling, text tool-agent fallback и `llama-cli` fallback подключены к новому слою идентичности.
- Версия повышена до `0.0.40-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`189` текстовых файлов), локализации совпадают `241/241`, smoke-запуск приложения прошёл и окно закрылось штатно.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260602_001900_core_identity_roles`, удалить новые файлы `CoreIdentityService.cs`, `ToolMessageFormatter.cs`, удалить активное ТЗ и вернуть `VERSION`/`AIHub.csproj` на предыдущую версию.

### Идея контекстного окна и сжатия памяти

- Backup перед записью идеи и истории: `Backups/20260602_idea_context_window_memory`.
- Создан файл идеи, не рабочее ТЗ: `ТЗ/ИДЕЯ_2026-06-02_контекстное_окно_и_сжатие_памяти.md`.
- В идее зафиксирован будущий механизм: индикатор занятости контекста, оценка/подсчёт токенов, резерв под ответ, структурированное сжатие старой истории и хранение memory-summary рядом с файлами сессии.
- Код не менялся, версия не повышалась, установщик не собирался, GitHub commit/push не выполнялись.
- Откат: удалить файл идеи `ТЗ/ИДЕЯ_2026-06-02_контекстное_окно_и_сжатие_памяти.md` и восстановить документы истории из `Backups/20260602_idea_context_window_memory`.

### Исправление контекста пользователя для ядра

- Backup перед правкой: `Backups/20260602_core_identity_user_profile_context_fix`.
- Причина: F12-тест показал, что ядро знает собственную идентичность, но не использует заполненную карточку пользователя.
- Изменено:
  - `UserContextService` явно сообщает модели, что карточка пользователя уже передана AI HUB;
  - `CoreIdentityService` отделяет запрет прямого доступа к файлам/интернету/shell от уже переданных служебных данных профиля и паспорта ПК;
  - `CoreIdentityService` добавляет в скрытый контекст паспорт компьютера: Windows, архитектура, CPU, RAM, GPU/VRAM и диски;
  - `LlamaCliRuntimeService` обновлён для фильтрации возможного echo новых служебных строк;
  - версия повышена до `0.0.41-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`190` текстовых файлов), локализации совпадают `241/241`, smoke-запуск приложения прошёл.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260602_core_identity_user_profile_context_fix` и вернуть версию `0.0.40-dev`.

### Закрытие ТЗ самоидентификации ядра без публикации

- Backup перед переносом ТЗ и записями: `Backups/20260602_close_core_identity_no_publish`.
- ТЗ `ТЗ/2026-06-02_самоидентификация_ядра.md` перенесено в `ТЗ/Архив/2026-06-02_самоидентификация_ядра.md`.
- Пользователь подтвердил, что ТЗ выполнено.
- Исключение из правила публикации: пользователь прямо указал "Пока не публикуй", поэтому GitHub commit/push не выполнялись.
- Код в рамках закрытия не менялся, версия не повышалась.
- Установщик не собирался по правилу проекта.
- Откат: вернуть ТЗ из `ТЗ/Архив/2026-06-02_самоидентификация_ядра.md` обратно в `ТЗ/2026-06-02_самоидентификация_ядра.md` или восстановить документы и ТЗ из `Backups/20260602_close_core_identity_no_publish`.

### Контекстная память ядра

- Backup перед правкой: `Backups/20260602_core_context_memory`.
- Создано ТЗ `ТЗ/2026-06-02_контекстная_память_ядра.md`.
- Добавлены `CoreMemoryStatus` и `CoreContextMemoryService`.
- В нижнюю инфо-зону главного окна добавлен индикатор памяти ядра: `🧠` + шкала, `🤯` при близком заполнении, притемнение при неактивном F12 и неопределённый режим во время сжатия.
- F12-окно публикует состояние памяти в главное окно.
- При опасном заполнении старая часть debug-истории сжимается в служебный memory-summary, сохраняется рядом с debug-сессией в папке `Memory` и подмешивается в будущие запросы как `system`-контекст.
- Официальные лимиты Qwen3-8B зафиксированы в коде: `32768` native и `131072` YaRN; фактическая шкала сейчас использует рабочий backend-лимит `4096`.
- Версия повышена до `0.0.42-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`193` текстовых файла), локализации совпадают `247/247`, smoke-запуск приложения прошёл.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260602_core_context_memory`, удалить новые файлы `Models/CoreMemoryStatus.cs` и `Services/CoreContextMemoryService.cs`, вернуть `VERSION`/`AIHub.csproj` на `0.0.41-dev`.

### Правка индикатора памяти ядра

- Backup перед правкой: `Backups/20260602_core_memory_indicator_limit_fix`.
- Исправлен цвет значка памяти ядра: активное состояние окрашивается в голубой, близкое заполнение в оранжевый, неактивное состояние в вторичный цвет темы.
- Из расчёта шкалы удалены искусственные резервы `BasePromptReserveUnits` и `ResponseReserveUnits`.
- Индикатор и порог автосжатия теперь используют официальный нативный лимит Qwen3-8B `32768`, а не временный backend `--ctx-size 4096`.
- Реальный `--ctx-size` backend-а не повышался: это отдельное решение, потому что оно влияет на расход RAM/VRAM.
- Версия повышена до `0.0.43-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`193` текстовых файла), локализации совпадают `247/247`, smoke-запуск приложения прошёл.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260602_core_memory_indicator_limit_fix` и вернуть версию `0.0.42-dev`.

### Повышение backend-контекста ядра

- Backup перед правкой: `Backups/20260602_core_ctx_size_32768`.
- Пользователь уточнил целевой минимум железа: средний игровой ПК, поэтому временный `--ctx-size 4096` признан излишне жёстким.
- `CoreContextRuntimeLimits.CurrentBackendContextLimit` установлен равным официальному нативному лимиту Qwen3-8B `32768`.
- `LlamaServerRuntimeService` и `LlamaCliRuntimeService` теперь передают `--ctx-size 32768` из общего источника.
- YaRN `131072` не включался.
- Версия повышена до `0.0.44-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`193` текстовых файла), локализации совпадают `247/247`, smoke-запуск приложения прошёл.
- Установщик не собирался по правилу проекта.
- Откат: восстановить изменённые файлы из `Backups/20260602_core_ctx_size_32768` и вернуть версию `0.0.43-dev`.

### Проверка смысловых потерь памяти ядра

- Backup документации перед записью результата: `Backups/20260602_core_memory_literary_test_docs`.
- Выполнен временный harness-тест без изменения кода проекта: проверено, как `CoreContextMemoryService` сжимает историю с длинным литературным отрывком.
- Результат зафиксирован в `CONTEXTHUB.md`, `Диалог_сжато.md` и активном ТЗ `ТЗ/2026-06-02_контекстная_память_ядра.md`.
- Код проекта не менялся, версия не повышалась, установщик не собирался.
- Откат: восстановить перечисленные документы из `Backups/20260602_core_memory_literary_test_docs`.

### Модельное сжатие памяти ядра и чтение текущего лога

- Backup перед правкой: `Backups/20260602_model_based_core_memory`.
- Активное ТЗ `ТЗ/2026-06-02_контекстная_память_ядра.md` расширено модельным сжатием и инструментом восстановления из текущего лога.
- Изменено:
  - `CoreContextMemoryService` теперь умеет создавать модельный план сжатия и применять модельную память;
  - добавлен `SessionLogReaderService` для чтения последних записей и поиска по текущему JSONL-логу F12;
  - `ToolGateway` получил команду `session_log`;
  - `DebugChatWindow` запускает модельное сжатие перед fallback-сжатием и сообщает ядру о `session_log`;
  - локализации обновлены новыми статусами сжатия;
  - версия повышена до `0.0.45-dev`.
- Проверки: `dotnet build` успешно без предупреждений, scanner кириллицы успешно (`203` текстовых файла), локализации совпадают `250/250`, smoke-запуск приложения прошёл.
- Установщик не собирался по правилу проекта, GitHub commit/push не выполнялись, ТЗ не архивировалось.
- Откат: восстановить изменённые файлы из `Backups/20260602_model_based_core_memory`, удалить новый файл `Исходники/AIHub/Services/SessionLogReaderService.cs`, вернуть `VERSION`/`AIHub.csproj` на `0.0.44-dev`.

### Повторный книжный тест модельной памяти

- Backup документации перед записью результата: `Backups/20260602_core_memory_book_model_test_docs`.
- Выполнен временный harness-тест через реальный `llama-server`; код проекта не менялся.
- Результат теста зафиксирован в `CONTEXTHUB.md`, `Диалог_сжато.md` и активном ТЗ `ТЗ/2026-06-02_контекстная_память_ядра.md`.
- Версия не повышалась, установщик не собирался, GitHub commit/push не выполнялись.
- Откат: восстановить перечисленные документы из `Backups/20260602_core_memory_book_model_test_docs`.

### Закрытие ТЗ контекстной памяти ядра

- Backup перед переносом ТЗ и записями: `Backups/20260602_close_context_memory_tz`.
- По подтверждению пользователя активное ТЗ и связанная идея перенесены в архив:
  - `ТЗ/Архив/2026-06-02_контекстная_память_ядра.md`;
  - `ТЗ/Архив/ИДЕЯ_2026-06-02_контекстное_окно_и_сжатие_памяти.md`.
- Закрытый этап включает индикатор контекста ядра, backend `--ctx-size 32768`, автоматическое и модельное сжатие, fallback-сжатие, `session_log` и диагностические тесты памяти.
- Версия этапа: `0.0.45-dev`.
- Установщик не собирался по правилу проекта.
- Проверки закрытия прошли: `dotnet build`, scanner кириллицы, сверка локализаций и smoke-запуск приложения.
- По стандартному правилу закрытия ТЗ актуальное состояние проекта опубликовано в GitHub: commit `2fb4151`, push в `origin/main`.
- Откат: восстановить документы и ТЗ из `Backups/20260602_close_context_memory_tz`, вернуть два файла из `ТЗ/Архив` обратно в `ТЗ`, если потребуется продолжить этап как активный.

### Запись факта публикации закрытия памяти ядра

- Backup перед записью факта публикации: `Backups/20260602_publish_context_memory_docs`.
- В историю добавлен hash публикации закрытого этапа: `2fb4151`.
- Откат: восстановить документы истории из `Backups/20260602_publish_context_memory_docs`.

### Старт первого боевого сценария выбора

- Backup перед созданием ТЗ и записями истории: `Backups/20260603_first_choice_scenario_tz`.
- Создано рабочее ТЗ `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`.
- Зафиксировано решение: сначала делаем первый реальный сценарий помощи в выборе, а полноценный RAG откладываем до появления живых данных сценария.
- Архитектурная роль: ядро AI HUB готовит задачу, затем передаёт её более мощной общепрофильной рабочей модели; временное кодовое имя роли `general_worker`.
- Окончательное название рабочей модели пока не выбрано.
- Код приложения на этом шаге ещё не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: удалить `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md` и восстановить документы истории из `Backups/20260603_first_choice_scenario_tz`.

### Уточнение концепции первого сценария выбора

- Backup перед правкой ТЗ и истории: `Backups/20260603_choice_scenario_concept_update`.
- В активном ТЗ зафиксирован формат боевого режима: не прямой чат, а NPC-опросник / режим планирования.
- Правило шага: начинать с больших групп направлений, затем сужать; максимум 6 готовых вариантов в 3 строки плюс отдельный `Свой вариант` 4-й строкой.
- `Свой вариант` должен классифицироваться ядром и возвращать пользователя в структуру сценария, а не открывать свободный чат.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: восстановить ТЗ и документы истории из `Backups/20260603_choice_scenario_concept_update`.

### Визуал динамических вариантов первого сценария выбора

- Попытка создать backup `Backups/20260603_choice_scenario_visual_options` через PowerShell не выполнилась из-за sandbox-ошибки запуска `CreateProcessAsUserW failed: 5`.
- Изменения внесены маленькими документальными patch-правками.
- В активном ТЗ зафиксирован визуал вариантов: показывать ровно столько кнопок, сколько предложило ядро; не держать 6 пустых слотов; раскладка готовых вариантов в две колонки до 6 пунктов; `Свой вариант` отдельной широкой строкой.
- Также зафиксированы спокойные анимации появления вариантов и мягкая подсветка выбранного варианта, без превращения AI HUB в игровой интерфейс.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные сегодня пункты про визуал динамических вариантов из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Граница финального шага первого сценария выбора

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- В активном ТЗ зафиксировано: сценарий должен дойти почти до конца подготовки, собрать карточку задачи, критерии, ограничения, будущий prompt и предложить оптимальную модель-исполнитель.
- Ядро не должно переходить от подготовки сразу к финальному решению, потому что это роль рабочей модели.
- Скачивание новой модели на этом этапе не подключается.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные пункты про границу финального шага из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Визуальная рекомендация варианта в первом сценарии выбора

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- В активном ТЗ зафиксировано: ядро может пометить вариант как рекомендуемый через структурный признак `isRecommended` и короткую причину `recommendationReason`.
- UI должен мягко выделять такой вариант цветом обводки/фона или меткой, но не скрывать остальные варианты и не принуждать пользователя.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные пункты про рекомендованный вариант из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Официальные документы для первого сценария выбора

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- В активном ТЗ добавлен раздел `Опора на официальные документы`.
- Зафиксированы источники Microsoft Learn: WPF data binding, `ItemsControl`, `ObservableCollection<T>`, WPF animation overview.
- Зафиксированы источники `llama.cpp`: `tools/server README` и `docs/function-calling.md`.
- Главный вывод для реализации: варианты кнопок не парсятся из обычного текста модели, а приходят как валидируемая JSON-структура шага.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить раздел `Опора на официальные документы` из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md` и связанные записи из `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Правило выбора не более слабого исполнителя

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- В активном ТЗ зафиксирована особенность `Режима неопределенности`: ядро не должно предлагать модель или инструмент слабее себя как оптимального исполнителя.
- Подбор исполнителя должен учитывать паспорт ПК, доступные backends, профиль пользователя и режим нагрузки.
- Если более сильный исполнитель не установлен, финальная карточка должна честно показать это; скачивание новой модели на этом этапе не запускается.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные пункты про правило выбора исполнителя из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Фиксированный первый шаг Режима неопределенности

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- В активном ТЗ зафиксирован первый шаг сценария: `К чему ближе вопрос?` с вариантами `Знания`, `Вещи`, `Жизнь`, `Технологии`, `Люди`, `Цели` и `Свой вариант`.
- Зафиксировано, что этот выбор может быть частью конструктора стартового prompt для ядра.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные пункты про фиксированный первый шаг из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Prompt-контур ядра и правило короткого своего варианта

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- В активном ТЗ зафиксировано: каждый шаг сценария должен передавать ядру полный prompt-контур заново, включая роль, правила, карточку состояния, последний выбор, профиль, паспорт ПК, доступные модели/инструменты и JSON-схему ответа.
- Зафиксировано правило `Свой вариант`: это короткая метка направления на 1-3 слова, а не свободный вопрос или мини-промт.
- Зафиксирован контроль вариантов: первый шаг из кода, дальше JSON от ядра, максимум 6 вариантов, невалидный JSON или лишние варианты возвращаются ядру на исправление.
- Финальный подбор исполнителя может использовать web/Hugging Face инструменты на этапе выбора модели/инструмента; скачивание пока не подключается.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные разделы `Рабочий prompt-контур ядра`, `Контроль вариантов после первого шага`, `Правило Свой вариант`, `Финальный подбор исполнителя`, `Итоговая схема реализации сценария` из активного ТЗ и связанные записи из `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Финальная чистка ТЗ Режима неопределенности

- Изменения внесены маленькими документальными patch-правками без shell-backup, так как запуск PowerShell в текущем sandbox ранее возвращал `CreateProcessAsUserW failed: 5`.
- После финального анализа ТЗ уточнено:
  - существующая заглушка переименуется в `Режим неопределенности`;
  - первая реализация нужна для проверки сценарного поведения ядра;
  - скачивание и запуск новой модели пока не подключаются;
  - финал первого этапа: карточка задачи, предложенный исполнитель и prompt для исполнителя;
  - отдельный режим `Изменить ответы` на финальной карточке не нужен;
  - добавлены состояния сценария `start_fixed_step`, `question_step`, `custom_input`, `final_task_card`, `structure_error`;
  - добавлены правила `Назад`, повторного исправления JSON, спокойной ошибки после двух невалидных структур и корректного помещения текста в кнопки;
  - при подборе исполнителя ядро сначала проверяет установленное, затем ищет в интернете, а при физически недоступном интернете предлагает текущее ядро как временный fallback.
- Код приложения не менялся, версия не повышалась, проверки не запускались, GitHub commit/push не выполнялись.
- Откат: вручную удалить добавленные пункты финальной чистки из `ТЗ/2026-06-03_первый_боевой_сценарий_выбора.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и этого файла.

### Реализация первого слоя Режима неопределенности

- Shell-backup и проверки не удалось выполнить из-за sandbox-ошибки запуска процессов `CreateProcessAsUserW failed: 5`.
- Изменения внесены patch-правками.
- Добавлены новые файлы:
  - `Исходники/AIHub/Models/ChoiceScenarioOption.cs`;
  - `Исходники/AIHub/Models/ChoiceScenarioStep.cs`;
  - `Исходники/AIHub/Models/ChoiceTaskCard.cs`;
  - `Исходники/AIHub/Services/ChoiceScenarioService.cs`.
- Изменены файлы:
  - `Исходники/AIHub/MainWindow.xaml`;
  - `Исходники/AIHub/MainWindow.xaml.cs`;
  - `Исходники/AIHub/Localization/ru.json`;
  - `Исходники/AIHub/Localization/en.json`;
  - `Исходники/AIHub/AIHub.csproj`;
  - `VERSION`;
  - `CONTEXTHUB.md`;
  - `Диалог_сжато.md`;
  - `BACKUP_ОТКАТ.md`.
- Реализовано:
  - переименование входа в `Режим неопределенности`;
  - внутренняя страница сценария без нового окна;
  - динамические варианты через `ItemsControl`;
  - отдельная зона `Мысль ядра` над вариантами;
  - отдельный `Свой вариант` с ограничением 1-3 слова;
  - финальная preview-карточка подготовки без скачивания и запуска новой модели;
  - версия повышена до `0.0.46-dev`.
- Проверки `dotnet build`, scanner кириллицы и сверка локализаций не запускались из-за `CreateProcessAsUserW failed: 5`.
- Установщик не собирался по правилу проекта.
- GitHub commit/push не выполнялись.
- Откат: удалить новые файлы сценария, восстановить изменённые файлы из предыдущего состояния или вручную убрать блоки `ChoiceScenarioPage`/обработчики сценария/новые ключи локализации, вернуть `VERSION` и `AIHub.csproj` на `0.0.45-dev`.

### Исправление неоднозначного Button в Режиме неопределенности

- Пользовательский build показал `CS0104`: `Button` был неоднозначной ссылкой между `System.Windows.Controls.Button` и `System.Windows.Forms.Button`.
- Исправлено в `Исходники/AIHub/MainWindow.xaml.cs`: два новых обращения к `Button` заменены на явный `System.Windows.Controls.Button`.
- Shell-проверку Codex не запускал из-за текущей sandbox-ошибки `CreateProcessAsUserW failed: 5`; пользовательский повторный запуск требуется для подтверждения.
- Откат: вернуть два обращения к `Button`, если потребуется восстановить предыдущее состояние.

### Переделка Режима неопределенности под ядро после первого шага

- Shell-backup и проверки не удалось выполнить из-за sandbox-ошибки запуска процессов `CreateProcessAsUserW failed: 5`.
- После визуального прогона пользователь уточнил: первый шаг фиксирован в коде, но все последующие шаги должно предлагать ядро; количество шагов заранее не ограничивается.
- В активное ТЗ добавлены:
  - ядро-ведомый сценарий после первого шага;
  - кнопка `Перейти к финалу`;
  - адаптивность страницы под размер окна.
- Добавлен файл `Исходники/AIHub/Models/ChoiceScenarioAnswer.cs`.
- Изменены:
  - `ChoiceScenarioService`: prompt-контур, JSON-парсер, fallback-шаг;
  - `LlamaServerRuntimeService`: `GenerateScenarioJsonAsync`;
  - `MainWindow.xaml`: кнопка `Перейти к финалу`, более адаптивная раскладка через `WrapPanel`;
  - `MainWindow.xaml.cs`: история ответов, запрос следующего шага у ядра, запрос финальной карточки;
  - `ru.json` и `en.json`: новые статусы и подписи.
- Жёсткий второй шаг теперь должен использоваться только как fallback при недоступности ядра.
- Код приложения изменён, версия осталась `0.0.46-dev`, потому что это доработка текущего незакрытого этапа.
- Проверки `dotnet build`, scanner кириллицы и сверка локализаций не запускались из-за `CreateProcessAsUserW failed: 5`; требуется пользовательский повторный build.
- Установщик не собирался по правилу проекта.
- Откат: удалить `ChoiceScenarioAnswer.cs`, убрать `GenerateScenarioJsonAsync`, вернуть прежний кодовый второй шаг/финальную preview-карточку в `ChoiceScenarioService` и `MainWindow`, удалить новые ключи локализации.

### Исправление раскладки вариантов Режима неопределенности

- Пользователь заметил, что адаптивность сделана неудачно: лишний скролл и одноколоночный столбик вариантов.
- В `Исходники/AIHub/MainWindow.xaml` варианты возвращены с `WrapPanel` на двухколоночный `UniformGrid`, основной блок центрирован, горизонтальный скролл отключён.
- Код приложения изменён в рамках текущей версии `0.0.46-dev`.
- Проверки Codex не запускал из-за `CreateProcessAsUserW failed: 5`; требуется пользовательский build.
- Откат: вернуть предыдущую раскладку `WrapPanel`, если потребуется.

### Удаление лишнего скролла первого экрана Режима неопределенности

- Пользователь уточнил, что скролл всё ещё есть и элементы должны центрироваться, съедая пустое место при сжатии окна.
- В `Исходники/AIHub/MainWindow.xaml` внутренний `ScrollViewer` первого экрана сценария заменён на центрированный `Grid`.
- Уменьшены вертикальные отступы, размер заголовка вопроса и высота карточек вариантов.
- Код приложения изменён в рамках текущей версии `0.0.46-dev`.
- Проверки Codex не запускал из-за `CreateProcessAsUserW failed: 5`; требуется пользовательский build.
- Откат: вернуть `ScrollViewer` и прежние размеры/отступы блока `ChoiceScenarioPage`, если потребуется.

### Исправление prompt-контракта Режима неопределенности

- По пользовательскому скриншоту выявлено: ядро выдумало конкретную задачу и назначило не-AI исполнителя (`владелец кота`), хотя должно было подбирать модель/инструмент.
- В `ChoiceScenarioService.BuildSystemPrompt` добавлены запреты:
  - не выдумывать конкретную задачу пользователя;
  - не возвращать `final_task_card`, если неизвестен конкретный предмет неопределённости;
  - `recommendedExecutor` может быть только моделью, backend-ом, инструментом или ролью AI HUB;
  - не назначать человеком, животным, пользователем или внешним актором как исполнителем.
- В `BuildUserPrompt` добавлено напоминание: если финал запрошен слишком рано, вернуть `question_step` с одним важным уточнением.
- В `MainWindow.xaml.cs` кнопка `Перейти к финалу` скрыта на первом шаге и появляется после хотя бы одного выбора.
- Проверки Codex не запускал из-за `CreateProcessAsUserW failed: 5`; требуется пользовательский build.
- Откат: убрать добавленные ограничения из prompt и вернуть постоянную видимость `ChoiceGoFinalButton`.

### JSONL-журналы Режима неопределенности

- Пользователь уточнил правило: у каждого сценария должна быть своя папка для JSONL-журналов ядра и будущих инструментов.
- В активное ТЗ добавлен раздел про журналы сценария.
- Добавлен файл `Исходники/AIHub/Services/ScenarioSessionLog.cs`.
- `MainWindow.xaml.cs` подключён к журналу сценария:
  - `scenario_session_start`;
  - `scenario_context_snapshot`;
  - `scenario_user_choice`;
  - `scenario_core_prompt`;
  - `scenario_core_raw_response`;
  - `scenario_parsed_step`;
  - `scenario_structure_error`;
  - `scenario_final_task_card`;
  - `scenario_core_unavailable`;
  - `scenario_session_end`.
- Путь журнала: `<Папка результатов>\AI_HUB\Scenarios\Uncertainty\Sessions`.
- Будущая область инструментов: `<Папка результатов>\AI_HUB\Scenarios\Uncertainty\Tools`.
- Проверки Codex не запускал из-за `CreateProcessAsUserW failed: 5`; требуется пользовательский build.
- Откат: удалить `ScenarioSessionLog.cs` и вызовы `_choiceScenarioLog` из `MainWindow.xaml.cs`, удалить раздел про журналы из активного ТЗ.

### Исправление using для ScenarioSessionLog

- Пользовательский build показал `CS0246`: не найден тип `StreamWriter` в `ScenarioSessionLog.cs`.
- Исправлено: добавлен `using System.IO;`.
- Требуется повторный пользовательский build.
- Откат: удалить строку `using System.IO;`, если файл будет переписан без типов из `System.IO`.

## 2026-07-10 — аудит и полное укрепление первого боевого сценария

- Перед правками создан path-safe backup: `H:\AI_HUB\Backups\20260710_full_project_audit_remediation`.
- В backup сохранены исходники `Исходники/AIHub` без `bin/obj`, проектные документы, активное ТЗ, правила, VERSION и документы истории.
- Реализованы: безопасные сценарные журналы, busy guard, снимки шагов, anti-loop, общая идентичность ядра, structured tool loop без скачивания, строгая JSON Schema, repair, проверка финального исполнителя, локализация, центрированное масштабирование, анимация вариантов, общий backend path helper и test-проект.
- Добавлены новые файлы в `Исходники/AIHub/Models`, `Исходники/AIHub/Services`, проект `Исходники/AIHub.Tests` и `global.json`.
- `VERSION` остаётся `0.0.46-dev`; `AIHub.csproj` теперь читает версию из корневого файла вместо второй ручной копии.
- Проверки: Debug/Release build без предупреждений, 8/8 тестов, format check, scanner `223` файла, локализации `315/315`, smoke-запуск и живой schema smoke на llama-server/Qwen3 8B.
- Установщик не собирался по правилу. Commit/push не выполнялись, потому что активное ТЗ ещё не закрыто пользователем.
- Откат: восстановить изменённые файлы из `Backups/20260710_full_project_audit_remediation`; удалить новые файлы и `Исходники/AIHub.Tests`, перечисленные текущим `git status`; пользовательские журналы, результаты и модели не удалять.

## 2026-07-11 — исправление финального подбора модели и правила >8B

- Создан backup `H:\AI_HUB\Backups\20260711_scenario_final_model_policy_fix`.
- Сохранены затронутые модели, сервисы сценария, runtime-сервис, MainWindow code-behind, тесты, активное ТЗ и документы истории.
- Реализованы forced `inventory`/`hf_find_model`, workload policy, `executorCapabilityClass`, проверка явного размера модели, нормализация `question_step + taskCard` и семантический anti-loop.
- Профили `balanced`/`extreme` запрещают 8B как рабочего исполнителя; `light` остаётся единственным режимом, допускающим лёгкий fallback.
- Проверки: Debug/Release build без предупреждений, 14/14 тестов, format check, scanner `225` файлов, smoke-запуск и живой forced-tool тест llama-server.
- Версия не менялась: `0.0.46-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: восстановить файлы из указанной backup-папки и удалить новый `ChoiceExecutorPolicy.cs`/`ChoiceExecutorPolicyTests.cs`; пользовательские журналы и модели не удалять.

## 2026-07-11 — бюджет содержательных шагов сценария

- Создан backup `H:\AI_HUB\Backups\20260711_scenario_step_budget`.
- Сохранены состояние сценария, prompt/orchestrator, MainWindow code-behind, локализации, тесты, активное ТЗ и документы истории.
- Добавлен `ChoiceScenarioStepBudget.cs` и стартовый выбор `4 / 10 / 20 / По ситуации`.
- Automatic-режим ограничен safety limit 30; бюджет считает только ответы на содержательные вопросы.
- Prompt получает maximum/used/remaining; при исчерпании orchestrator требует `final_task_card` и отклоняет новый вопрос.
- Проверки: Debug/Release build без предупреждений, 18/18 тестов, format check, scanner `226` файлов, локализации `329/329`, smoke-запуск.
- Версия остаётся `0.0.46-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: восстановить файлы из backup, удалить `ChoiceScenarioStepBudget.cs` и связанные тесты; пользовательские журналы и модели не удалять.

## 2026-07-11 — резервный поиск финальной модели

- Перед правками создан backup `H:\AI_HUB\Backups\20260711_final_model_search_fallback`.
- Сохранены policy, orchestrator, каталог инструментов, тесты, активное ТЗ и документы истории.
- Добавлены резервный поиск по семейству модели, отбор кандидата выше 8B и программная коррекция запрещённого выбора текущего ядра.
- Проверки: Debug/Release build без предупреждений, 20/20 тестов, format check, scanner `230` файлов, локализации `329/329`, `git diff --check`, smoke-запуск.
- Версия остаётся `0.0.46-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: восстановить файлы из указанной backup-папки и удалить новые `ChoiceModelCandidateSelector.cs` и `ChoiceModelCandidateSelectorTests.cs`; пользовательские журналы и модели не удалять.

## 2026-07-11 — парсер каталога Hugging Face

- Перед правками создан backup `H:\AI_HUB\Backups\20260711_huggingface_catalog_parser`.
- Исходное состояние документов сохранено в корне backup; промежуточная реализованная версия новых файлов сохранена в подпапке `implemented`.
- Добавлены модели каталога, parser/collector, отдельный `AIHub.CatalogProbe`, тесты и ТЗ.
- Живые raw-артефакты сохранены в `H:\AI_HUB\Тесты\HuggingFaceCatalogProbe`; модели и веса не скачивались.
- Проверки: Debug/Release build без предупреждений, Release probe build, 23/23 теста, format check, scanner `283` файлов, локализации `329/329`, `git diff --check`, smoke-запуск и сверка SHA-256 raw-источников.
- Версия остаётся `0.0.46-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: удалить новые файлы парсера, модели каталога, проект `AIHub.CatalogProbe`, связанные тесты и ТЗ; документы восстановить из backup. Тестовые raw-артефакты удалять только по отдельному решению пользователя.
- Перед добавлением открытых режимов обнаружения создан дополнительный снимок `Backups/20260711_huggingface_catalog_parser/before_open_discovery`.
- Перед изменением `.gitignore` создан снимок `Backups/20260711_huggingface_catalog_parser/.gitignore`; диагностические raw-артефакты исключены из Git.

## 2026-07-11 — удаление bias выбора модели сценария

- Перед правками создан backup `H:\AI_HUB\Backups\20260711_remove_scenario_model_bias`.
- Сохранены orchestrator, policy-зависимые сервисы, MainWindow, тесты, активное ТЗ и документы истории.
- Удалены Qwen-family fallback, предпочтительные издатели и программная подмена исполнителя; добавлены нейтральная валидация evidence и правило значительно более нового поколения для семейства ядра.
- Проверки: Debug/Release build без предупреждений, Release probe build, 24/24 теста, format check, scanner `283` файлов, локализации `329/329`, `git diff --check`, smoke-запуск и bias scan runtime-файлов.
- Откат: восстановить файлы из backup; пользовательские журналы, модели и каталоги не удалять.

## 2026-07-11 — накопительный каталог и радар Hugging Face

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260711_huggingface_catalog_sync`.
- Сохранены исходные файлы parser/collector, модели каталога, CLI, тесты, ТЗ, реестр и документы истории.
- Добавлены посевной реестр на 99 слотов, schema v2 накопительного каталога, JSONL-журнал изменений, контроль revision SHA и радар популярных новинок.
- Живой каталог создан в `H:\AI_HUB\Runtime\Каталоги\HuggingFace`; это локальные runtime-данные, они исключены из Git.
- Проверки: 28/28 тестов, Debug/Release build без предупреждений, format check, scanner `303` файлов, локализации `329/329`, `git diff --check`, smoke-запуск и повторная живая синхронизация без дублей и недоступных репозиториев.
- Версия остаётся `0.0.46-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: восстановить существовавшие файлы из backup и удалить новые файлы `HuggingFaceCatalogSeed*`, `HuggingFaceCatalogDatabase.cs`, `HuggingFaceSearchCandidate.cs`, `HuggingFaceCatalogStore.cs`, `HuggingFaceCatalogSyncService.cs`, тест синхронизации и `Каталоги/huggingface-catalog-seed.json`. Runtime-каталог удалять только по отдельному решению пользователя.

## 2026-07-11 — профиль AI-исполнителя в «Режиме неопределенности»

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260711_uncertainty_executor_profile`.
- Сохранены модели/сервисы сценария, MainWindow, тесты, локализации, активное ТЗ, версия и документы проекта; перед изменением отдельно добавлен backup `Инструкции/CODEX.md`.
- Сценарий переориентирован с предметного сужения на накопительный capability profile исполнителя.
- Добавлены закрытые измерения/влияния, структурированные эффекты вариантов, восстановление профиля при `Назад`, productivity validation и принудительный финал после повторного предметного нарушения.
- Выполнены временные живые harness-прогоны Qwen3 8B; их исходники и build-артефакты удалены после проверки.
- Проверки: restore, Debug/Release build без предупреждений, 36/36 тестов, format check, scanner `306` файлов, локализации `353/353`, `git diff --check`, smoke-запуск и живой полный финал с инструментами.
- Версия повышена с `0.0.46-dev` до `0.0.47-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: восстановить файлы из указанного backup и удалить новые `ChoiceCapabilityProfile.cs` / `ChoiceScenarioPromptBuilder.cs`; пользовательские журналы, модели и накопительный каталог не удалять.

## 2026-07-11 — подключение локального каталога как инструмента ядра

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260711_local_catalog_tool`.
- Сохранены orchestration/tool-сервисы, AppData paths, CatalogProbe, версия и проектные документы.
- Добавлены `ModelCatalogSearchResponse.cs`, `LocalModelCatalogTool.cs`, тесты и отдельное ТЗ интеграции.
- Runtime-каталог не изменялся и модели не скачивались.
- Проверки: 40/40 тестов, Debug/Release build без предупреждений, Release CatalogProbe build, format check, scanner `316` файлов, локализации `353/353`, `git diff --check`, локальный catalog-tool probe и smoke-запуск.
- Версия повышена с `0.0.47-dev` до `0.0.48-dev`. Установщик и GitHub-публикация не выполнялись.
- Откат: восстановить существовавшие файлы из backup и удалить новые model-catalog tool модели/сервис/тест/ТЗ. Пользовательский каталог, журналы и модели не удалять.

## 2026-07-11 — repair aliases и память отклонённых исполнителей

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260711_scenario_repair_aliases`.
- Источник диагностики: свежий JSONL пользовательского прогона; локальный каталог был исправен, финал ломался на aliases и повторе запрещённой модели.
- Добавлены консервативная нормализация aliases, память отклонённых executor repo и уточнённый UI-текст ошибки на русском/английском.
- Политика семейства и запрет исполнителя до 8B не ослаблялись; модели и runtime-каталог не изменялись.
- Версия повышена с `0.0.48-dev` до `0.0.49-dev`. Установщик и GitHub-публикация не выполнялись.
- Проверки: 42/42 теста, Debug/Release build без предупреждений, format check, scanner `318` файлов, локализации `353/353`, `git diff --check` и smoke-запуск.
- Откат: восстановить перечисленные файлы из backup; пользовательские JSONL-журналы, каталог и модели не удалять.

## 2026-07-11 — PC-fit, metadata lineage и startup sync

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260711_catalog_hardware_lineage_startup`.
- Сохранены catalog/scenario services и models, MainWindow, CatalogProbe, тесты, активные ТЗ, версия и проектные документы.
- Добавлены сервис оценки аппаратной совместимости, startup sync и связанные модели/тесты; seed-каталог включён в build output как content.
- Полный сетевой отказ больше не сохраняет все отслеживаемые модели как недоступные и не повреждает прежний каталог.
- Runtime-каталог и пользовательские модели вручную не изменялись; установщик и GitHub-публикация не выполнялись.
- Версия повышена с `0.0.49-dev` до `0.0.50-dev`.
- Проверки: 54/54 теста, Debug/Release и CatalogProbe Release build без предупреждений, format check, scanner `324` файлов, локализации `353/353`, bundled seed, `git diff --check`, два живых local-search probe и smoke startup-sync.
- Smoke-журнал подтвердил безопасный `skipped_fresh`; установщик не пересобирался, commit/push не выполнялись.
- Откат: восстановить существовавшие файлы из backup и удалить новые hardware/startup модели, сервисы и тесты. Пользовательские каталоги, журналы и модели не удалять.

## 2026-07-11 — закрытие всех активных ТЗ

- Перед архивированием создан path-safe backup `H:\AI_HUB\Backups\20260711_close_active_specs`.
- Три завершённых ТЗ перенесены из корня `ТЗ` в `ТЗ/Архив`; активных ТЗ не осталось.
- По обязательному правилу закрытия выполняются финальная проверка, commit и push актуального состояния в `origin/main`.
- Установщик не пересобирался по правилу проекта и отсутствию отдельной команды.
- В scanner добавлено исключение папки `Тесты`: внешняя web-выдача с корректным испанским словом создавала ложное срабатывание; диагностические данные не изменялись.
- Финальные проверки закрытия: Release build без предупреждений, 54/54 теста, scanner `178` исходных текстовых файлов, локализации `353/353`, `git diff --check`, активных ТЗ `0`.
- Основной commit закрытия `6571c12` успешно отправлен в `origin/main`.
- Откат архивации: вернуть три ТЗ из backup или `ТЗ/Архив` в корень `ТЗ`; код, runtime-каталог, журналы и модели не удалять.

## 2026-07-11 — ТЗ локального голоса ядра

- Перед изменением документов создан path-safe backup `H:\AI_HUB\Backups\20260711_core_local_voice_spec`.
- Сохранены `ТЗ/README.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и `BACKUP_ОТКАТ.md`.
- Создано новое активное ТЗ `ТЗ/2026-07-11_локальный_голос_ядра_и_синхронизация_вопросов.md`; код приложения и runtime не изменялись.
- Для отката удалить новый ТЗ-файл и восстановить четыре документа из backup. Пользовательские данные, журналы, модели и каталоги не затрагивать.
- Установщик, commit и push не выполнялись.

## 2026-07-11 — реализация голоса ядра и RHVoice-альтернативы

- Перед основной реализацией созданы backup `H:\AI_HUB\Backups\20260711_core_voice_implementation` и дополнительный `H:\AI_HUB\Backups\20260711_rhvoice_alternative`.
- Перед финальными тестами и документацией создан backup `H:\AI_HUB\Backups\20260711_rhvoice_names_and_tests`.
- Добавлены eSpeak NG runtime adapter, RHVoice SAPI adapter, маршрутизатор, синхронизация текста, настройки, локализации, setup-скрипты и автотесты.
- Пользовательские названия: eSpeak — `Привет из 80-ых`, RHVoice — `Просто ИИ голос`; eSpeak остаётся выбранным по умолчанию.
- Проверки: Debug/Release build без предупреждений, 62/62 теста, scanner `193` файлов, локализации `371/371`, русский и английский RHVoice live probe с native events, self-contained publish smoke без RHVoice installers.
- Установщик не пересобирался по правилу проекта; commit/push не выполнялись, активное ТЗ не архивировалось до пользовательской проверки RHVoice.
- Для отката восстановить файлы из последнего подходящего backup; системно установленные RHVoice-пакеты при необходимости удаляются отдельно стандартными средствами Windows. Пользовательские данные, модели, каталоги и журналы не затрагивать.

## 2026-07-11 — закрытие ТЗ голоса ядра

- Перед архивированием создан path-safe backup `H:\AI_HUB\Backups\20260711_close_core_voice_spec`, содержащий весь текущий набор изменённых и новых файлов голосового ТЗ.
- Пользователь подтвердил успешный ручной тест RHVoice; ТЗ перенесено из корня `ТЗ` в `ТЗ\Архив`, список активных ТЗ очищен.
- По обязательному правилу закрытия после финальных проверок выполняются commit и push в `origin/main`.
- Установщик не пересобирался: отдельной команды не было, installer flow не затронут.
- Для отката вернуть ТЗ из архива в корень и восстановить необходимые файлы из backup. Пользовательские данные, журналы, модели и каталоги не удалять.
- Финальные проверки: Release build без предупреждений, 62/62 теста, format check, scanner `193` файлов, локализации `371/371`, `git diff --check`, активных ТЗ `0`.
- Основной commit закрытия `d83e308` успешно отправлен в `origin/main`.
- Перед записью факта публикации создан дополнительный backup `H:\AI_HUB\Backups\20260711_close_core_voice_publish_record`.

## 2026-07-12 — запись пользовательского теста установщика

- Пользователь самостоятельно собрал и успешно проверил установщик актуальной версии `0.0.51-dev`; ошибок не сообщил.
- Код, installer-конфигурация и артефакты этой записью не изменялись.
- Перед изменением истории создан backup `H:\AI_HUB\Backups\20260712_installer_user_smoke_record`.

## 2026-07-12 — ТЗ модели-исполнителя

- Перед созданием нового активного ТЗ сделан path-safe backup `H:\AI_HUB\Backups\20260712_executor_handoff_spec`.
- Создано ТЗ безопасной загрузки GGUF-исполнителя, долгоживущей сессии, передачи карточки и Matrix-анимации.
- На этом шаге код, runtime, модели и installer не изменялись; новые AI-модели не скачивались.
- Для отката удалить новое ТЗ и восстановить документацию из backup. Пользовательские модели, журналы и каталоги не затрагивать.

## 2026-07-12 — реализация модели-исполнителя

- Перед кодовыми правками создан backup `H:\AI_HUB\Backups\20260712_executor_handoff_implementation`.
- Перед финальной документацией создан backup `H:\AI_HUB\Backups\20260712_executor_handoff_final_docs`.
- Добавлены artifact resolver, installer, executor workflow/session, context budget, SSE parser, Matrix control, отдельный executor log и UI финальной карточки.
- Реальные AI-модели не скачивались. Installer-тесты использовали только маленькие байтовые payload через подмененный `HttpClient`; live SSE probe использовал существующий Qwen3 8B.
- Версия повышена с `0.0.51-dev` до `0.0.52-dev`; установщик не пересобирался, commit/push не выполнялись.
- Для отката восстановить существовавшие файлы из backup и удалить новые executor/stream/Matrix файлы. Пользовательские модели, `.part`, manifests и журналы без отдельного решения не удалять.
- Финальные внутренние проверки: Release build без предупреждений, 68/68 тестов, format check, scanner `206` файлов, локализации `392/392`, `git diff --check`, publish smoke без моделей.
- Новые модели не скачивались, UI автоматически не управлялся, установщик не пересобирался. Commit/push отложены до закрытия активного ТЗ.

## 2026-07-12 — усиление executor pipeline после сквозного теста

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260712_executor_pipeline_hardening`.
- В backup включены изменяемые исходники, локализации, активное ТЗ, история, документы и полная копия ошибочно загруженной Gemma MTP-модели с шестью executor-журналами; общий размер backup около 516 МБ.
- По прямой команде пользователя из рабочего хранилища удалена папка `Данные_для_внедрения\Модели\Executors\bartowski_google_gemma-4-31B-it-GGUF` и шесть связанных executor JSONL. Каталожные карточки семейства Gemma и журналы выбора ядра сохранены, поскольку они не являются нерабочим артефактом.
- Для отката восстановить файлы и `RemovedData` из указанного backup. Новый файл `Services\GgufMetadataReader.cs` при полном откате удалить.
- Версия повышена с `0.0.52-dev` до `0.0.53-dev`. Новые модели не скачивались, установщик не пересобирался, UI автоматически не управлялся, commit/push не выполнялись.
- Для изменённого после основного снимка `HuggingFaceProviderTool.cs` сохранены post-change reference и отдельный обратимый diff `HuggingFaceProviderTool.rollback.patch`.
- Проверки: Release build без предупреждений, 72/72 теста, format check, scanner `207` файлов, локализации `393/393`, `git diff --check`, publish-smoke `Runtime\PublishSmoke\ExecutorHardening053`, отсутствие нерабочей Gemma и её executor-журналов.

## 2026-07-12 — сужение задачи исполнителем и зарезервированные RHVoice-профили

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260712_executor_discovery_and_reserved_voices` из 25 существующих файлов.
- Задача расширяет активное ТЗ: отдельный handoff с происхождением данных, повторяемые уточнения исполнителя, ручное завершение сессии и ролевой голос `Elena / BDL`.
- `Aleksandr / SLT` остаются общими голосами ядра; запрет четырёх профилей в других сценариях относится только к инструментам.
- Для полного отката восстановить существовавшие файлы из backup и удалить новые файлы этой задачи. Системные пакеты Elena/BDL при необходимости удаляются штатным деинсталлятором Windows.
- Новые голоса загружены только из официальных GitHub releases RHVoice; SHA-256 Elena `23E1301869E842F8F91FE64CC34533F9996724EC609962A24E6D5DEE7828B643`, Bdl `F9C51B0ED0C63E8DF1F9CD7C29D9F076220451B8A4EFAD822E03FFBA07ED4847`. Оба setup-файла не имеют Authenticode-подписи.
- Внутренние проверки: 74/74 теста, Debug/Release build без предупреждений, format check, scanner `209` файлов, локализации `406/406`, беззвучный SAPI synthesis Elena/Bdl и живой JSON Schema smoke на уже установленной Qwen3.6 27B.
- Новые AI-модели не скачивались, UI автоматически не управлялся, установщик не пересобирался. Активное ТЗ не архивируется до пользовательской проверки; commit/push не выполнялись.

## 2026-07-12 — обязательный выбор из двух исполнителей

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260712_dual_executor_choice` из 21 существующего файла.
- План: финальная карточка обязана содержать подтверждённый installed/runnable вариант и отдельный download-вариант с плюсами и ограничениями; пользователь выбирает модель кнопкой.
- Для отката восстановить файлы из backup и удалить новые файлы этой задачи. Установленные модели, RHVoice-профили, пользовательские журналы и результаты не изменять.

## 2026-07-14 — продолжение выбора двух исполнителей

- После прерывания по лимиту создан дополнительный path-safe backup `H:\AI_HUB\Backups\20260714_resume_dual_executor`.
- Исправлена память repair: отклоняется только кандидат, указанный в ошибке, а не вся пара; при общей ошибке отклоняется только предварительно рекомендованный вариант.
- Подсветка карточек уточнена: до выбора выделено предпочтение ядра, после выбора — явный выбор пользователя.
- Срабатывание Defender проверено штатным `MpCmdRun`: Debug-сборка AI_HUB и папка RHVoice installers угроз не содержат; обнаруженная запись относилась к заблокированной reflection-команде Codex, `DidThreatExecute=False`, `IsActive=False`.
- Финальные проверки: Debug/Release без предупреждений, 79/79 тестов, format check, scanner `211` файлов, локализации `415/415`, `git diff --check`, publish-smoke `Runtime\PublishSmoke\DualExecutor055Resume` без `.gguf`/`.part`.
- Новые модели не скачивались, UI не управлялся, установщик не пересобирался, commit/push не выполнялись.
- Для полного отката восстановить перечисленные файлы из дополнительного backup. Модели, runtime, голоса и пользовательские журналы не изменялись.

## 2026-07-14 — доверенный пул кандидатов исполнителя

- Перед перераспределением ответственности создан path-safe backup `H:\AI_HUB\Backups\20260714_trusted_executor_candidate_pool`.
- План правки: программа нормализует модели и семейства, формирует допустимый пул и собирает итоговую карточку; ядро выбирает только доверенные ID и объясняет сравнение.
- Альтернатива для установленной модели обязана быть из другого семейства; основной repo и GGUF-упаковка одинаковых весов считаются одной моделью.
- Новые модели, backends и голоса на этом этапе не скачиваются и не удаляются.
- Для отката восстановить существовавшие файлы из backup и удалить новые файлы candidate-pool этой правки.
- Реализован `ChoiceExecutorCandidatePoolService`: inventory, локальный каталог, live fallback, семейство и совместимость с ПК теперь проверяет программа; ядро выбирает только доверенные ID и смысловые оценки.
- Финальный контракт больше не требует от ядра переписывать model/repo/status/role/capability class; установленная модель может быть рекомендована, а альтернатива обязана быть из другого семейства.
- Модельные tools скрыты от ядра сценария, точные повторные web-команды блокируются, repair получает компактный пул повторно.
- Версия повышена до `0.0.56-dev`.
- Финальные внутренние проверки: restore, Release build без предупреждений, 84/84 теста, format check, scanner `214` файлов, локализации `415/415`, `git diff --check`, self-contained publish-smoke `Runtime\PublishSmoke\TrustedPool056` без `.gguf`/`.part`.
- Новые AI-модели не скачивались, UI автоматически не управлялся, установщик не пересобирался, commit/push не выполнялись. Активное ТЗ остаётся открытым до пользовательской сквозной проверки.

## 2026-07-14 — закрытие ТЗ модели-исполнителя

- Перед закрывающими правками создан path-safe backup `H:\AI_HUB\Backups\20260714_close_executor_handoff_spec` из текущих изменённых и новых файлов рабочего дерева.
- Пользователь подтвердил успешный сквозной тест доверенного выбора, запуска установленной Qwen3.6 27B и получения `final_result`.
- ТЗ признано выполненным; его перенос в `ТЗ\Архив`, очистка списка активных ТЗ, финальные проверки, commit и push выполняются по обязательному регламенту закрытия.
- Отдельно зафиксировано, что итоговый UX результата исполнителя требует будущего самостоятельного ТЗ и не входит в закрываемую работу.
- Установщик не пересобирается: прямой команды не было, а installer flow этой закрывающей правкой не изменяется.
- Для отката вернуть ТЗ из архива в корень, восстановить нужные файлы из backup и не удалять пользовательские модели, журналы, результаты и голоса.
- Закрывающие проверки пройдены: Release build без предупреждений, 84/84 теста, format check, scanner `214` файлов, локализации `415/415`, `git diff --check`, активных ТЗ `0`, publish-smoke без `.gguf`/`.part`.
- Основной commit закрытия `f22af34` (`Complete executor handoff workflow`) успешно отправлен в `origin/main`.
- Перед записью факта публикации создан дополнительный backup `H:\AI_HUB\Backups\20260714_close_executor_handoff_publish_record`; документационная запись публикуется отдельным commit.
## 2026-07-23 — многоэтапная сессия модели-исполнителя

- Перед реализацией создан path-safe backup `H:\AI_HUB\Backups\20260723_staged_executor_session` из 18 существующих файлов.
- Создано активное ТЗ `ТЗ\2026-07-23_многоэтапная_сессия_модели_исполнителя.md`.
- План: ручные переходы между четырьмя активными этапами, открытая сессия после текущего результата, защита от служебных заглушек и отдельное журналирование переходов.
- Экспорт и работа с пользовательскими файлами не входят в текущую реализацию. Новые AI-модели во внутренних тестах не скачиваются.
- Для полного отката восстановить существовавшие файлы из backup и удалить новые файлы этого ТЗ. Пользовательские модели, журналы, результаты и голоса не изменять.
- Реализована версия `0.0.57-dev`: четыре ручных этапа, открытая сессия после результата, repair фиктивного ответа и журналирование переходов.
- Внутренние проверки: Debug/Release без предупреждений, 88/88 тестов, format check, scanner, локализации `433/433`, `git diff --check`, self-contained publish smoke из `922` файлов без моделей.
- UI автоматически не управлялся, новые AI-модели не скачивались, установщик не пересобирался. Commit/push не выполнялись, поскольку активное ТЗ ожидает ручной тест пользователя.

## 2026-07-23 — закрытие ТЗ многоэтапной сессии исполнителя

- Пользователь подтвердил ручной тест и принял текущее поведение как ожидаемое для этапа песочницы, несмотря на остающуюся неполноту.
- Перед закрывающими изменениями создан path-safe backup `H:\AI_HUB\Backups\20260723_close_staged_executor_session` из 20 файлов.
- ТЗ помечается выполненным и переносится из `ТЗ` в `ТЗ\Архив`; список активных ТЗ очищается.
- Дальнейшая настройка поведения, визуал, файлы и экспорт не входят в закрываемое ТЗ.
- По регламенту выполняются финальные проверки, commit и push в `origin/main`.
- Установщик не пересобирается: прямой команды не было, installer flow не изменялся.
- Для отката вернуть ТЗ из архива в корень `ТЗ`, восстановить нужные файлы из backup и не удалять пользовательские модели, журналы, результаты, голоса или отдельные материалы пользователя.
- Закрывающие проверки пройдены: restore, Debug/Release без предупреждений, `88/88` тестов, format check, scanner `216` файлов, локализации `433/433`, publish-smoke `922` файла без `.gguf` и `.part`.
- Основной commit `8f2bd3f` (`Add staged executor sessions`) успешно отправлен в `origin/main`.
- Перед фиксацией результата публикации создан дополнительный backup `H:\AI_HUB\Backups\20260723_close_staged_executor_publish_record`; документационная запись публикуется отдельным commit.

## 2026-07-23 — автономная работа исполнителя и снимки результата

- Создано активное ТЗ `ТЗ\2026-07-23_автономная_работа_исполнителя_и_снимки_результата.md`.
- Перед реализацией создан path-safe backup `H:\AI_HUB\Backups\20260723_executor_autonomy_snapshots` из 18 существующих файлов.
- План: отдельное подтверждение постановки, автоматическое продолжение в той же executor-сессии, запрос пользователя только при критической неопределённости, безопасное динамическое подключение web-инструментов и немодальное окно версий результата.
- Новые AI-модели не скачиваются. Экспорт и работа с пользовательскими файлами не входят в задачу.
- Для полного отката восстановить существовавшие файлы из backup и удалить новые файлы этого ТЗ. Пользовательские модели, журналы, результаты и отдельные материалы не изменять.
- Реализация завершена в `0.0.58-dev`; добавлены новые файлы `ExecutorAutomationPolicy.cs`, `ExecutorMarkdownDocumentBuilder.cs`, `ExecutorResultWindow.xaml` и `ExecutorResultWindow.xaml.cs`.
- Для отката удалить перечисленные новые файлы, восстановить изменённые файлы из `H:\AI_HUB\Backups\20260723_executor_autonomy_snapshots` и не затрагивать пользовательский файл `Данные_для_внедрения\Фото\I want to believe llm.png`.
- Установщик не пересобирался, новые модели не скачивались, UI автоматически не управлялся.
- Проверки: restore, Debug/Release во временных каталогах без предупреждений, `92/92` теста, format check, scanner `221` файл, локализации `443/443`, `git diff --check`, self-contained publish smoke `922` файла без `.gguf` и `.part`.
- Штатный Debug output был занят уже запущенным `AIHub.exe`; пользовательский процесс не останавливался и не управлялся.

## 2026-07-23 — исправление первого теста автономного исполнителя

- Разобран executor JSONL `2026-07-23_14-27-54_executor_06d9f8f75eaa4de9aad3816000cb8962.jsonl`.
- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260723_executor_autonomy_session_fix`.
- Исправлены повторный `confirm_brief` на позднем этапе, рассинхронизация UI после автономного исключения и преждевременная видимость кнопки результата.
- Для отката восстановить файлы из указанного backup. Пользовательские модели, журналы тестов и отдельный файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.
- Проверки: Debug/Release во временных каталогах без предупреждений, `94/94` теста, format check, scanner `221` файл, локализации `443/443`, `git diff --check`, publish smoke `922` файла без моделей.

## 2026-07-23 — двухэтапная работа исполнителя и живой краткий результат

- Перед новой переработкой создан path-safe backup `H:\AI_HUB\Backups\20260723_executor_two_stage_live_preview` из 24 файлов.
- Backup сохраняет текущее незакоммиченное состояние активного ТЗ, включая новые файлы окна результата и executor-сервисов.
- План: заменить автоматический четырёхэтапный проход на техническую постановку и продолжительное практическое уточнение, показывать ограниченный размером правой половины окна краткий доступный результат и перенести ответы в нижнюю панель.
- Полный результат остаётся ручной командой пользователя и открывается отдельным окном без завершения сессии.
- Для отката восстановить файлы из указанного backup и удалить только новые файлы, явно созданные после этой точки. Пользовательские модели, журналы тестов и файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.
- Новые модели не скачивать, UI автоматически не управлять, установщик без прямой команды не пересобирать.
- Реализация завершена в `0.0.59-dev`: введены два этапа исполнителя, живой краткий результат справа, нижняя панель ответов и исключительно ручное создание полного документа.
- Добавлены новые файлы `ExecutorHandoffConsistencyPolicy.cs` и `ExecutorResultSummaryPolicy.cs`. Сохранены созданные ранее в рамках открытого ТЗ `ExecutorMarkdownDocumentBuilder.cs`, `ExecutorResultWindow.xaml` и `ExecutorResultWindow.xaml.cs`.
- Для отката восстановить изменённые файлы из `H:\AI_HUB\Backups\20260723_executor_two_stage_live_preview`, затем удалить перечисленные новые файлы только если их не было до восстановленной точки.
- Внутренние проверки: restore, Debug/Release без предупреждений, `92/92` теста в обеих конфигурациях, format check, scanner `280` файлов, локализации `447/447`, `git diff --check`, publish smoke `922` файла без `.gguf` и `.part`.
- Новые AI-модели не скачивались, UI автоматически не управлялся, установщик не пересобирался. ТЗ ожидает ручной сквозной тест пользователя.

## 2026-07-23 — исправление layout исполнителя и слоя Matrix

- Ручной тест `0.0.59-dev` подтвердил логику и выявил наложение старого экрана сценария под новым workspace исполнителя.
- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260723_executor_layout_matrix_layer_fix` из 8 файлов.
- План: разделить старый и новый контейнеры, закрепить левую, правую и нижнюю зоны и вынести Matrix-дождь на глобальный полупрозрачный слой окна.
- Для отката восстановить файлы из указанного backup. Пользовательские модели, журналы, результаты и файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.
- Новые модели не скачивать, UI автоматически не управлять, установщик без прямой команды не пересобирать.
- Исправление реализовано в `0.0.60-dev`: старый экран подготовки и workspace исполнителя разделены, три рабочие зоны закреплены, Matrix перенесён на глобальный слой.
- Для отката восстановить 8 файлов из `H:\AI_HUB\Backups\20260723_executor_layout_matrix_layer_fix`.
- Проверки: Debug/Release без предупреждений, `92/92` теста в обеих конфигурациях, format check, scanner `236` файлов, локализации `447/447`, `git diff --check`, publish smoke `922` файла без `.gguf` и `.part`.
- Новые модели не скачивались, UI автоматически не управлялся, установщик не пересобирался. Требуется ручной тест пользователя.

## 2026-07-23 — закрытие двухэтапного исполнителя без публикации

- Пользователь подтвердил выполнение активного ТЗ в `0.0.60-dev`.
- Перед закрывающими изменениями создан path-safe backup `H:\AI_HUB\Backups\20260723_close_executor_two_stage_no_publish` из 8 файлов.
- ТЗ `2026-07-23_автономная_работа_исполнителя_и_снимки_результата.md` переносится в `ТЗ\Архив`.
- По прямому указанию пользователя применяется исключение из правила публикации: локальный коммит создаётся, но `push` в GitHub не выполняется.
- Последний тест выявил отдельную будущую задачу: практический пересказ и snapshot описали техническое задание вместо самого результата. На этапе закрытия выполнен только анализ, код и промты не изменялись.
- Для отката восстановить документы из указанного backup и вернуть ТЗ из архива в корень `ТЗ`. Пользовательские модели, журналы, результаты и отдельный файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.
- Установщик не пересобирается; новых моделей и инструментов не скачивается.
- Финальные проверки закрытия: Release tests `92/92`, scanner `222` файла, локализации `447/447`, `git diff --check`; ранее в том же состоянии кода пройдены Debug/Release build, format и publish smoke `922` файла без моделей.
- Основной локальный commit закрытия: `f101fbd` (`Complete two-stage executor workflow`) в ветке `main`.
- `Push` намеренно не выполнялся по прямому указанию пользователя. На момент основного commit незатронутым и незастейдженным остался только пользовательский файл `Данные_для_внедрения\Фото\I want to believe llm.png`.
- Перед документационной записью создан дополнительный backup `H:\AI_HUB\Backups\20260723_close_executor_local_commit_record` из 3 файлов.

## 2026-07-23 — дерево сессии и содержательный результат

- Перед реализацией создан path-safe backup `H:\AI_HUB\Backups\20260723_session_knowledge_tree` из 19 существующих файлов.
- Создано активное ТЗ `ТЗ\2026-07-23_дерево_сессии_и_содержательный_результат.md`.
- План: хранить дерево решений, знаний и результата отдельно от контекста модели; показывать его в живом немодальном окне с анимацией роста; передавать модели компактный активный срез.
- Кнопка дерева размещается в верхней панели рядом с настройками, содержит только соответствующий знак и tooltip.
- Практический этап обязан накапливать сам доступный ответ, а не техническое задание на его будущее создание.
- Для полного отката восстановить существовавшие файлы из backup и удалить новые файлы этой задачи. Пользовательские модели, журналы, результаты и файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.
- Новые AI-модели не скачивать, установщик не пересобирать, UI автоматически не управлять.
- Реализовано в `0.0.61-dev`: `SessionKnowledgeTree`, компактный активный контекст, типизированные узлы, JSONL-события, содержательный `workingResultFragment`, semantic repair снимка и живое `SessionTreeWindow`.
- Кнопка со знаком дерева находится рядом с настройками и доступна только при активном дереве executor-сессии.
- Новые rollback-файлы: `Models\SessionKnowledgeTreeModels.cs`, `Services\SessionKnowledgeTree.cs`, `Services\SessionTreeLayoutEngine.cs`, `Services\ExecutorWorkingResultPolicy.cs`, `SessionTreeWindow.xaml`, `SessionTreeWindow.xaml.cs` и активный ТЗ.
- Внутренние проверки: Debug/Release без предупреждений, `96/96` тестов, format check, scanner `229` файлов, локализации `465/465`, `git diff --check`, self-contained publish smoke `922` файла без `.gguf` и `.part`.
- UI автоматически не управлялся; ручная проверка дерева и содержательности ответа остаётся за пользователем. Commit/push не выполнялись, ТЗ остаётся активным.
## 2026-07-23 — backup перед ручной финализацией executor-сессии

- Создан path-safe backup: `H:\AI_HUB\Backups\2026-07-23_17-43-29_executor-finalization` (первоначально папка была создана как `Backup`, затем безопасно перенесена в штатный каталог `Backups`).
- Сохранены текущие версии executor-контракта, сервисов, `MainWindow`, локализаций, тестов, версии, реестра зависимостей, проектных журналов и активного ТЗ дерева.
- Пользовательский файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменялся и в backup этой задачи не копировался.
- Откат выполнять восстановлением файлов из указанной папки с сохранением относительных путей.

## 2026-07-23 — реализация ручной финализации и DOCX

- Backup этой задачи находится в `H:\AI_HUB\Backups\2026-07-23_17-43-29_executor-finalization`.
- Реализованы нетерминальная рекомендация завершения, перенос кнопки, модальный выбор финала, прямой вывод и экспорт DOCX.
- Добавлены файлы `ExecutorFinishDialog.xaml`, `ExecutorFinishDialog.xaml.cs`, `Services/ExecutorDocxExporter.cs`, `Services/ExecutorTurnSemanticPolicy.cs` и активное ТЗ финализации.
- В `AIHub.csproj` добавлена runtime-зависимость `DocumentFormat.OpenXml 3.5.1`; для полного отката удалить PackageReference и восстановить документы лицензий из backup.
- Версия повышена до `0.0.62-dev`.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests `99/99`, package vulnerability check без известных уязвимостей, format, scanner `241` файл, локализации `491/491`, `git diff --check`, publish smoke `457` файлов без моделей.
- Новые AI-модели не скачивались, UI автоматически не управлялся, установщик не собирался. Commit/push не выполнялись; ТЗ ожидает ручной проверки пользователя.

## 2026-07-23 — исправление сигнала готовности executor-сессии

- Перед правками создан path-safe backup `H:\AI_HUB\Backups\20260723_1845_executor_finish_readiness` из 12 файлов.
- Диагностика выполнена по `2026-07-23_18-09-39_executor_684effab4655491993a7b1534c0c679b.jsonl`: последний ход был `ask_user`, хотя `stageSummary` сообщал о завершённом исследовании, `missingCriticalInputs` был пуст, а вопрос касался необязательного дополнения.
- В контракт добавлен `canFinalize`; для отката восстановить модели контракта, parser, semantic policy, session service, `MainWindow`, тесты, ТЗ, версию и проектные журналы из указанного backup.
- Версия повышена до `0.0.63-dev`.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests `101/101`, format check, scanner `248` файлов, локализации `491/491`, `git diff --check`.
- Новые модели не скачивались, UI автоматически не управлялся, установщик не собирался. Commit/push не выполнялись; активное ТЗ ожидает повторного ручного теста.

## 2026-07-23 — закрытие всех активных ТЗ

- Перед закрытием создан path-safe backup `H:\AI_HUB\Backups\20260723_1905_close_all_active_specs` из 8 файлов.
- Пользователь подтвердил выполнение ТЗ дерева сессии и ручной финализации/DOCX.
- Оба ТЗ перенесены из `ТЗ` в `ТЗ\Архив`; для отката восстановить документы из backup и вернуть два файла в корень `ТЗ`.
- Финальные проверки: Debug/Release build без предупреждений, Debug/Release tests `101/101`, format check, scanner `248` файлов, локализации `491/491`, `git diff --check`, NuGet vulnerability check без известных проблем.
- Publish smoke `Runtime\PublishSmoke\ExecutorClosure063_20260723` содержит `924` файла, включая Open XML SDK, и не содержит `.gguf` или `.part`.
- Новые AI-модели не скачивались, UI автоматически не управлялся, установщик не собирался.
- Commit и push в `origin/main` выполняются по штатному правилу закрытия ТЗ; пользовательский файл `Данные_для_внедрения\Фото\I want to believe llm.png` в публикацию не включается.
- Основной коммит закрытия `5a5cb27` успешно отправлен в `origin/main` (`2b99150..5a5cb27`).
- Перед записью точного результата публикации создан дополнительный backup `H:\AI_HUB\Backups\20260723_1920_record_spec_publication` из 3 журналов.

## 2026-07-23 — backup перед долговечными сессиями

- Создан path-safe backup `H:\AI_HUB\Backups\20260723_2015_resumable_sessions` из 16 существующих файлов.
- Создано активное ТЗ `ТЗ\2026-07-23_долговечные_сессии_и_продолжение_работы.md`.
- План: заменить заглушку `Ранее начатое` программным архивом с атомарными контрольными точками, восстановлением ядра и исполнителя, динамическими карточками, переименованием, локальной проверкой модели и подтверждаемым удалением.
- Для полного отката восстановить существовавшие файлы из backup и удалить новые файлы этой задачи.
- Пользовательские модели, экспортированные документы и файл `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.
- Новые AI-модели не скачивать внутренними тестами, UI автоматически не управлять, установщик без прямой команды не пересобирать.

## 2026-07-23 — реализация долговечных сессий

- На основе backup `H:\AI_HUB\Backups\20260723_2015_resumable_sessions` реализована версия `0.0.64-dev`.
- Добавлены новые файлы:
  - `Исходники\AIHub\Models\ResumableSessionModels.cs`;
  - `Исходники\AIHub\Services\ScenarioSessionArchiveService.cs`;
  - `Исходники\AIHub.Tests\ScenarioSessionArchiveServiceTests.cs`;
  - `ТЗ\2026-07-23_долговечные_сессии_и_продолжение_работы.md`.
- Для полного отката восстановить файлы из указанного backup, удалить перечисленные новые файлы и удалить только тестовые архивы сессий, созданные пользователем специально для проверки этой версии.
- Реальные пользовательские архивы сессий, внешние DOCX, модели и `Данные_для_внедрения\Фото\I want to believe llm.png` при откате автоматически не удалять.
- Проверки: Debug/Release без предупреждений, Debug/Release tests `109/109`, локализации, format, UTF-8 scanner и `git diff --check`.
- Commit/push не выполнялись: активное ТЗ ожидает ручной проверки. Установщик не пересобирался.

## 2026-07-23 — закрытие долговечных сессий без публикации

- Перед закрытием создан backup `H:\AI_HUB\Backups\20260723_2211_close_resumable_sessions_no_publish` из ТЗ, реестра, истории, rollback и документов проекта.
- Пользователь подтвердил полное выполнение ТЗ; файл перенесён в `ТЗ\Архив`.
- Активных ТЗ после закрытия нет.
- Закрывающие проверки: Debug/Release без предупреждений, Debug/Release tests `109/109`, format и UTF-8 scanner `252` файла.
- По прямому указанию пользователя обязательная публикация при архивации ТЗ отложена. Выполняется локальный commit без `push`; накопленные изменения будут опубликованы позже отдельной командой.
- Установщик не пересобирался, модели не скачивались, UI автоматически не управлялся.
- Основной локальный коммит закрытия: `5929513` (`Add resumable scenario sessions`). `Push` не выполнялся.
# 2026-07-23 — файловый паспорт сессии

- Перед реализацией создан path-safe backup: `H:\AI_HUB\Backups\20260723_2239_session_file_manifest`.
- В backup сохранены версия, активный реестр ТЗ, история, rollback-журнал, основной WPF UI, модели состояния, prompt/runtime-сервисы, локализации и связанные тесты.
- Новое активное ТЗ: `H:\AI_HUB\ТЗ\2026-07-23_файловый_паспорт_сессии.md`.
- Полный откат: восстановить сохранённые файлы по относительным путям и отдельно удалить новые файлы, добавленные текущим ТЗ.

## Реализованное состояние `0.0.65-dev`

- Новые файлы задачи:
  - `Исходники\AIHub\Models\SessionFileModels.cs`;
  - `Исходники\AIHub\Services\SessionFileManifestService.cs`;
  - `Исходники\AIHub\MainWindow.SessionFiles.cs`;
  - `Исходники\AIHub.Tests\SessionFileManifestServiceTests.cs`;
  - `ТЗ\2026-07-23_файловый_паспорт_сессии.md`.
- Существующие файлы из backup изменены для нулевого шага, handoff, prompt-контрактов, WPF, локализаций, долговечных checkpoint и тестов.
- `OpenAiSseStreamParserTests.cs` изменён только в тестовой синхронизации: асинхронный `Progress<T>` заменён синхронным test helper, production parser не менялся.
- Для отката восстановить файлы из `H:\AI_HUB\Backups\20260723_2239_session_file_manifest`, затем удалить перечисленные новые файлы.
- Реальные архивы пользовательских сессий и выбранные пользователем оригиналы файлов при откате не удалять.
- Проверки: Release без предупреждений, Debug/Release tests `119/119`, format, scanner `257` файлов, локализации `551/551`, `git diff --check`.
- Commit/push не выполнялись; активное ТЗ ожидает ручной проверки. Установщик не пересобирался.

## 2026-07-23 — уточнение роли добавленного файла

- Перед правкой создан path-safe backup `H:\AI_HUB\Backups\20260723_2313_file_role_prompt` из prompt-сервисов, теста, активного ТЗ и проектных журналов.
- В prompt ядра и исполнителя добавлено различие ролей нового файла: часть задачи, пример/эталон, поясняющий материал или не учитывать.
- Для вопроса о роли обязательны готовые кнопочные варианты; `Свой вариант` остаётся fallback.
- Для отката восстановить файлы из указанного backup.
- Проверки: Release без предупреждений, Debug/Release tests `119/119`, format, UTF-8 scanner `257` файлов, `git diff --check`.
- Commit/push не выполнялись; ТЗ остаётся активным. Установщик не пересобирался.

## 2026-07-23 — закрытие файлового паспорта

- Пользователь подтвердил полное выполнение активного ТЗ.
- Перед закрытием создан path-safe backup `H:\AI_HUB\Backups\20260723_2320_close_file_manifest_spec` из ТЗ, реестра, истории и rollback-журнала.
- ТЗ переносится в `ТЗ\Архив`, активных ТЗ после закрытия нет.
- Для отката документов закрытия восстановить файлы из указанного backup и вернуть ТЗ из архива в корень `ТЗ`.
- По штатному правилу архивации после финальных проверок выполняются commit и push в `origin/main`.
- Пользовательский файл `Данные_для_внедрения\Фото\I want to believe llm.png` не включать в commit.
- Финальные проверки: restore, Debug/Release build без предупреждений, Debug/Release tests `119/119`, format, UTF-8 scanner `257` файлов, локализации `551/551`, `git diff --check`, NuGet vulnerability check без известных проблем.
- Установщик не пересобирался, модели не скачивались, UI автоматически не управлялся.
- Основной коммит закрытия: `121908a` (`Add session file manifests`).
- Push выполнен успешно: `origin/main` обновлён с `7db6089` до `121908a`, включая накопленные локальные коммиты.
- Перед записью результата публикации создан backup `H:\AI_HUB\Backups\20260723_2323_record_file_manifest_publication`.

## 2026-07-24 — backup перед подготовкой каталога компонентов

- Перед изменением проектных документов создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_001134_component_catalog_spec`.
- В backup сохранены `ТЗ\README.md`, `CONTEXTHUB.md`, `Диалог_сжато.md` и
  `BACKUP_ОТКАТ.md`.
- Создан новый активный файл
  `ТЗ\2026-07-24_каталог_компонентов_и_загрузка_возможностей.md`.
- Для полного отката удалить новый ТЗ и восстановить четыре сохранённых файла
  из указанной backup-папки с сохранением относительных путей.
- Проверка ссылок выполнялась только по HTTP-заголовкам и редиректам; тела
  архивов, установщиков, моделей и пакетов не загружались.
- Код, NuGet-зависимости, версия приложения и установщик не изменялись.
- Пользовательский файл
  `Данные_для_внедрения\Фото\I want to believe llm.png` не изменять.

## 2026-07-24 — дополнение ТЗ отдельным каталогом просмотрщиков

- Перед правкой создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_003119_viewer_catalog_spec`.
- Сохранены текущие версии активного ТЗ, `CONTEXTHUB.md`,
  `Диалог_сжато.md` и `BACKUP_ОТКАТ.md`.
- Для отката дополнения восстановить четыре файла из указанной папки с
  сохранением относительных путей.
- В ТЗ добавлены отдельная программная секция просмотрщиков, системный fallback
  Windows, правила полной невидимости для LLM и 10 проверенных ссылок.
- Проверка выполнялась через HTTP-заголовки и редиректы без загрузки тел файлов.
- Код, зависимости, версия приложения и установщик не изменялись.
- Commit и push не выполнялись.

## 2026-07-24 — backup локальной шкалы загрузки компонентов

- Перед правкой создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_181232_component_progress_bar`.
- Сохранены модель карточки компонентов, orchestration загрузки, XAML главного
  окна, активное ТЗ и документы истории/отката.
- Для отката восстановить файлы из backup с сохранением относительных путей.
- Добавлены локальная шкала и процент на карточках processing- и
  viewer-компонентов; новые зависимости и строки локализации не добавлялись.
- Проверки: Debug/Release без предупреждений, Debug/Release tests `124/124`,
  UTF-8 scanner `269` файлов и `git diff --check`.
- UI автоматически не управлялся, тяжёлые компоненты не скачивались,
  установщик не пересобирался. Commit/push не выполнялись.

## 2026-07-24 — backup и откат реализации каталога компонентов

- Перед кодовыми изменениями создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_172944_component_catalog_implementation`.
- В backup сохранены исходники без `bin/obj`, активное ТЗ, реестр, notices,
  история и rollback-документы.
- Для полного отката восстановить файлы из backup с сохранением относительных
  путей и удалить новые файлы менеджера компонентов, viewer-окна и тестов,
  отсутствующие в backup.
- Реализация выполнена в `0.0.66-dev`; тяжёлые runtimes, viewer-пакеты и модели
  не скачивались.
- Финальные внутренние проверки: Debug/Release без предупреждений,
  Debug/Release tests `124/124`, локализации `618/618`, scanner `269` файлов,
  NuGet vulnerability check без известных проблем и `git diff --check`.
- Установщик не пересобирался. UI автоматически не управлялся.
- ТЗ остаётся активным до ручного теста; commit и push не выполнялись.

## 2026-07-24 — глобальная кнопка просмотра файла в ТЗ

- Перед дополнением создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_003450_global_file_viewer_spec`.
- Сохранены активное ТЗ, `CONTEXTHUB.md`, `Диалог_сжато.md` и
  `BACKUP_ОТКАТ.md`.
- Для отката восстановить четыре файла из указанной backup-папки с сохранением
  относительных путей.
- В ТЗ добавлена глобальная кнопка просмотра, отдельное немодальное окно,
  Matrix-блокировка и запрет автоматической передачи файла в AI-сессию.
- Код, зависимости, версия приложения и установщик не изменялись.
- Commit и push не выполнялись.

## 2026-07-24 — универсальная исполнимость комплекта

- Перед изменениями создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_193500_generic_execution_compatibility`.
- Сохранены затрагиваемые сервисы, модели, тесты, локализации, активное ТЗ,
  история и rollback-документы; исходная версия `0.0.66-dev` также сохранена.
- Для отката восстановить файлы из backup с сохранением относительных путей и
  удалить новые файлы
  `Исходники\AIHub\Services\ExecutionCompatibilityService.cs` и
  `Исходники\AIHub.Tests\ExecutionCompatibilityServiceTests.cs`.
- Версия реализации: `0.0.67-dev`.
- Тяжёлые компоненты и модели не скачивались, UI автоматически не управлялся,
  установщик не пересобирался. Commit/push не выполнялись.

## 2026-07-24 — переименование сценария в «Песочницу»

- Перед изменениями создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_193021_rename_uncertainty_to_sandbox`.
- Сохранены локализации, XAML, prompt исполнителя, правила проекта,
  документация, активное ТЗ, версия, история и rollback-файл.
- Для отката восстановить файлы из указанной папки с сохранением относительных
  путей.
- Внутренний ID `Uncertainty` и форматы пользовательских данных не менялись,
  миграция сохранённых сессий не требуется.
- Версия после переименования и принятия ТЗ: `0.0.69-dev`.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests
  `139/139`, format, локализации `631/631`, UTF-8 scanner `271` файл и
  `git diff --check`.
- Установщик по правилу не пересобирался.
- Основная публикация закрытого ТЗ: commit `627a0f6`
  (`feat: add component catalog and sandbox workflow`), успешно отправлен в
  `origin/main`.
- Перед записью результата публикации создан дополнительный backup:
  `H:\AI_HUB\Backups\20260724_193900_publication_record`.

## 2026-07-24 — операции профиля и неразрешённые возможности

- Перед изменениями создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_191400_generic_capability_operations`.
- Сохранены маппер возможностей, пул и проверка кандидатов, оркестратор,
  интерфейс, локализации, тесты, версия, активное ТЗ, история и rollback-файлы.
- Для отката восстановить 16 файлов из указанной backup-папки с сохранением
  относительных путей.
- Цель прохода: универсально разделить модальность данных и операцию над ними,
  а также не выдавать неполный исполнительный комплект за готовый к запуску.
- Реализация завершена в `0.0.68-dev`: добавлены канонические `generate.*`,
  сохранение координаторов при неполном комплекте и блокировка фактического
  запуска при `unresolved`.
- Проверки: Debug/Release без предупреждений, Debug/Release tests `139/139`,
  format, локализации `631/631`, UTF-8 scanner `271` файл и
  `git diff --check`.
- Тяжёлые компоненты и модели не скачивались, UI автоматически не управлялся,
  установщик не пересобирался. Commit/push не выполнялись.

## 2026-07-24 — backup безопасных инструментов исполнителя

- Перед реализацией создан path-safe backup:
  `H:\AI_HUB\Backups\20260724_195132_executor_safe_file_tools`.
- Сохранены затрагиваемые сервисы исполнителя и файлового манифеста,
  `MainWindow.xaml.cs`, тест манифеста, версия, индекс ТЗ, история и
  rollback-документ.
- Для отката восстановить файлы из backup с сохранением относительных путей и
  удалить новые файлы файлового шлюза, каталога инструментов и их тестов,
  отсутствующие в backup.
- Цель прохода: дать исполнителю read-only доступ только к файлам, явно
  добавленным в текущую сессию, без абсолютных путей, shell и прав записи.
- Исходная версия перед реализацией: `0.0.69-dev`.
- Перед обновлением roadmap в тот же backup дополнительно сохранён
  `Документы_проекта\ROADMAP.md`.
- Реализация завершена в `0.0.70-dev`; ТЗ оставлено активным до ручного теста.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests
  `146/146`, format, UTF-8 scanner `276` файлов и `git diff --check`.
- UI автоматически не управлялся, модели и тяжёлые компоненты не скачивались,
  установщик не пересобирался. Commit/push не выполнялись.

## 2026-07-24 — backup проверяемых действий исполнителя

- Перед доработкой создан path-safe backup:
  `H:\AI_HUB\Backups\2026-07-24_2115_executor_action_buttons`.
- Сохранены контракт и parser ответа исполнителя, runtime и gateway
  инструментов, checkpoint сессии, дерево диалога, XAML и обработчики окна,
  DOCX-экспорт, тесты, активное ТЗ, версия и документы истории.
- Цель прохода: отличать обычный ответ от подтверждаемого действия, связывать
  зелёную кнопку с реальным tool-call и не позволять исполнителю сформировать
  результат по явно указанному текстовому файлу без успешного чтения.
- Для отката восстановить файлы из backup с сохранением относительных путей.
- Исходная версия перед доработкой: `0.0.70-dev`.
- Итоговая версия: `0.0.71-dev`.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests
  `147/147`, format, UTF-8 scanner `276` файлов и `git diff --check`.
- UI автоматически не управлялся, модели и тяжёлые компоненты не скачивались,
  установщик не пересобирался. Commit/push не выполнялись.

## 2026-07-24 — backup семантических паспортов

- Перед изменениями создан path-safe backup:
  `H:\AI_HUB\Backups\2026-07-24_2335_semantic_passports`.
- Сохранены каталоги и карточки компонентов, модели и сервисы установки
  исполнителей, runtime, локализации, тесты, версия, индекс ТЗ, история и
  rollback-документ.
- Для отката восстановить 24 файла из backup с сохранением относительных путей
  и удалить новые файлы семантических паспортов и их тесты, отсутствующие в
  backup.
- Исходная версия перед реализацией: `0.0.71-dev`.
- Итоговая версия: `0.0.72-dev`.
- Добавлены двуязычные паспорта фиксированных компонентов, ручной паспорт
  Qwen 27B, атомарное хранилище `executor-model.json` и безопасная фоновая
  генерация паспортов будущих моделей после выгрузки исполнителя.
- Ошибка генерации паспорта изолирована от статуса установки модели.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests
  `151/151`, format, UTF-8 scanner `281` файл и `git diff --check`.
- UI и модели не запускались, загрузки не выполнялись, установщик не
  пересобирался. Commit/push не выполнялись.

## 2026-07-25 — backup динамических возможностей и комплектов инструментов

- Перед реализацией создан path-safe backup:
  `H:\AI_HUB\Backups\2026-07-25_dynamic_capability_plugins`.
- Сохранено 29 затрагиваемых файлов: контракты и runtime ядра/исполнителя,
  каталог компонентов, настройки и XAML, локализации, тесты, версия, индекс ТЗ,
  история и rollback-документ.
- Для отката восстановить файлы из backup с сохранением относительных путей и
  удалить новые файлы плагинного контура, отсутствующие в backup.
- Исходная версия перед реализацией: `0.0.72-dev`.
- Цель прохода: запрос нескольких возможностей, честное разделение пакета и
  адаптера, безопасные комплекты зависимостей, внешний поиск и временной бюджет
  самостоятельной работы ядра.
- UI автоматически не управляется, модели и тяжёлые компоненты не скачиваются,
  установщик без отдельной команды не пересобирается.
- Реализация завершена в `0.0.73-dev`; ТЗ остаётся активным до ручной проверки
  пользователем.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests
  `156/156`, format, локализации `640/640`, UTF-8 scanner `287` файлов и
  `git diff --check`.
## 2026-07-25 — ExecutionRoute до выбора координатора

- Backup: `H:\AI_HUB\Backups\2026-07-25_execution_route_handoff`
- Причина: общий маршрут выполнения, разделение декодирования и смыслового
  анализа, передача файлового манифеста в подбор исполнителя.
- Откат: восстановить сохраненные файлы с исходными относительными путями и
  удалить новые файлы этого дополнения, отсутствующие в backup.
- Реализация завершена в `0.0.74-dev`; ТЗ остаётся активным до ручной проверки.
- Проверки: Debug/Release build без предупреждений, Debug/Release tests
  `160/160`, format, локализации `656/656`, UTF-8 scanner `289` файлов и
  `git diff --check`.
- UI и модели не запускались, внешние компоненты не скачивались, установщик не
  пересобирался. Commit/push не выполнялись.

## 2026-07-25 — новое ТЗ Песочницы и условное архивирование старых ТЗ

- Перед изменением правил, ТЗ и истории создан path-safe backup:
  `H:\AI_HUB\Backups\20260725_041813_reframe_sandbox_specs`.
- В backup сохранены:
  - `Инструкции\AGENTS.md`;
  - `Инструкции\CODEX.md`;
  - `ТЗ\README.md`;
  - три прежних активных ТЗ;
  - `CONTEXTHUB.md`;
  - `Диалог_сжато.md`;
  - `BACKUP_ОТКАТ.md`.
- Создано новое активное ТЗ:
  `ТЗ\2026-07-25_песочница_сквозные_комплекты_и_гарантированный_результат.md`.
- Условно выполненными помечены и локально перенесены в `ТЗ\Архив`:
  - `2026-07-24_безопасные_инструменты_исполнителя.md`;
  - `2026-07-24_семантические_паспорта_компонентов_и_моделей.md`;
  - `2026-07-25_динамические_возможности_и_комплекты_инструментов.md`.
- Реализованные части старых ТЗ не удалялись. Новое ТЗ сохраняет их как
  фундамент и заменяет устаревшее правило жёсткой блокировки неполного
  маршрута.
- В `AGENTS.md` и `CODEX.md` добавлено согласованное правило вопросов перед
  реализацией архитектурно значимых идей.
- Для полного документального отката восстановить файлы из backup, удалить
  новое ТЗ и вернуть три архивированных ТЗ из `ТЗ\Архив` в корень `ТЗ`.
- По прямому текущему указанию пользователя исключение из стандартного правила
  закрытия: commit и push не выполнялись, публикация накопленных изменений
  отложена.
- Код, версия, модели, компоненты и установщик в этом проходе не изменялись.
- Проверки документов: один активный ТЗ, три архивных файла на месте,
  `git diff --check` без ошибок, UTF-8 scanner — `290` файлов без mojibake.

## 2026-07-25 — каталог рабочих паттернов Песочницы

- Перед дополнением активного ТЗ создан path-safe backup:
  `H:\AI_HUB\Backups\20260725_0442_sandbox_work_patterns_spec`.
- В backup сохранены активное ТЗ, `CONTEXTHUB.md`, `Диалог_сжато.md` и
  `BACKUP_ОТКАТ.md`.
- В ТЗ добавлены конечный каталог из 25 рабочих паттернов Песочницы, контракт
  их выбора ядром, правила программной проверки и сохранения, общий
  `AcquisitionPlan` и фактический `InstallationManifest`.
- Для отката восстановить сохранённые файлы с исходными относительными путями.
- Код, версия, модели, компоненты и установщик не менялись. Commit/push не
  выполнялись.

## 2026-07-25 — публикация накопленного состояния `0.0.74-dev`

- Перед записью результатов публикации создан path-safe backup истории:
  `H:\AI_HUB\Backups\20260725_050210_publish_accumulated_sandbox`.
- В публикацию включаются все накопленные изменения проекта, новое активное
  ТЗ, три условно закрытых архивных ТЗ и согласованное пользователем проектное
  изображение `Данные_для_внедрения\Фото\I want to believe llm.png`.
- Проверки перед commit/push: restore, Debug/Release build без предупреждений,
  Debug/Release tests `160/160`, format, локализации `656/656`, UTF-8 scanner
  `290` файлов и `git diff --check`.
- Приложение и модели не запускались, внешние компоненты не скачивались,
  установщик не пересобирался.
- Полный откат публикации выполняется штатным `git revert` публикуемого commit;
  локальный откат последних записей истории возможен из указанного backup.

## 2026-07-27 — реализация сквозных комплектов Песочницы `0.0.75-dev`

- Основной path-safe backup перед архитектурной реализацией:
  `H:\AI_HUB\Backups\20260726_234022_sandbox_end_to_end_075`.
- Дополнительный backup перед продолжением после разрыва:
  `H:\AI_HUB\Backups\20260727_implementation_resume_075`.
- Backup адаптеров, resolver, gateway, сессии исполнителя, тестов, версии и
  документов перед финальным проходом:
  `H:\AI_HUB\Backups\20260727_022517_sandbox_adapter_resume_075`.
- Для полного локального отката восстановить файлы из backup в порядке от
  более раннего к более позднему, используя сохранённые относительные пути.
  Для отката отдельного последнего блока достаточно последнего backup.
- Внешние модели и компоненты не скачивались, приложение и модели не
  запускались, установщик не пересобирался.
- Commit и push не выполнялись: ТЗ остаётся активным до пользовательского
  теста и прямого подтверждения закрытия.
- Финальные проверки: Debug/Release build без ошибок и предупреждений,
  Debug/Release tests `174/174`, format без изменений, локализации `668/668`,
  UTF-8 scanner `305` файлов и `git diff --check`.

## 2026-07-27 — продолжение ТЗ динамической рабочей среды Песочницы

- Перед созданием второго файла той же большой задачи выполнен path-safe
  backup:
  `H:\AI_HUB\Backups\20260727_043330_sandbox_spec_continuation`.
- Сохранены родительское ТЗ, `ТЗ\README.md`, `CONTEXTHUB.md`,
  `Диалог_сжато.md` и `BACKUP_ОТКАТ.md`.
- Создано активное продолжение
  `ТЗ\2026-07-27_песочница_продолжение_динамическая_рабочая_среда.md`.
- Для полного отката удалить новый файл и восстановить сохранённые документы
  из backup с исходными относительными путями.
- Код, версия, модели, компоненты и установщик не менялись. Приложение и модели
  не запускались. Commit и push не выполнялись.

## 2026-07-27 — адаптивная роль исполнителя в продолжении ТЗ

- Перед дополнением продолжения ТЗ создан path-safe backup:
  `H:\AI_HUB\Backups\20260727_043722_executor_adaptive_role_spec`.
- Сохранены продолжение ТЗ, `CONTEXTHUB.md`, `Диалог_сжато.md` и
  `BACKUP_ОТКАТ.md`.
- В ТЗ добавлен контракт `ExecutorRoleProfile`, правила генерации роли ядром,
  её закрепления в контексте и пересборки после изменения цели.
- Для отката восстановить указанные документы из backup с исходными
  относительными путями.
- Код, версия, модели, компоненты и установщик не менялись. Приложение и модели
  не запускались. Commit и push не выполнялись.

## 2026-07-27 — ранний запрос возможности исполнителем `0.0.76-dev`

- Перед изменением политики этапов, prompt, JSON-контракта, тестов, версии и
  документов создан path-safe backup:
  `H:\AI_HUB\Backups\20260727_051602_executor_early_capability_request`.
- Для полного отката восстановить сохранённые файлы с исходными относительными
  путями и удалить новый файл
  `Исходники\AIHub\Services\ExecutorTurnStagePolicy.cs`.
- Исправление разрешает `request_capability` до подтверждения карточки задачи,
  сохраняет тот же этап после ответа resolver и не показывает системный запрос
  как кнопку пользователя.
- Contract repair сохраняет массив `requestedCapabilities`; строгая схема
  облегчена для совместимости с grammar `llama.cpp`, а ограничения длины
  остаются в parser/policy.
- Версия повышена до `0.0.76-dev`.
- Проверки: restore успешно, Debug/Release tests `178/178`, Release build без
  ошибок и предупреждений, формат затронутых файлов чистый, локализации
  `668/668`, UTF-8 scanner `307` файлов, `git diff --check` без содержательных
  ошибок.
- Общая format-проверка обнаруживает прежние отступы в
  `Исходники\AIHub.Tests\SandboxOrchestrationTests.cs`; этот файл не изменялся.
- Приложение и модели не запускались, компоненты не скачивались, установщик не
  пересобирался. Commit/push не выполнялись.

## 2026-07-27 — подготовка недостающих возможностей `0.0.78-dev`

- Перед изменением resolver, реестра адаптеров, инструментов исполнителя,
  перехода к скачиванию, тестов, версии и документов создан path-safe backup:
  `H:\AI_HUB\_backups\20260727_missing_capability_acquisition_preflight`.
- Для полного отката восстановить сохранённые файлы из backup по исходным
  относительным путям.
- Добавлены вызываемые адаптеры ImageMagick и Tesseract. Известные недостающие
  пакеты теперь попадают в единый план загрузки, а после установки маршрут
  обязательно пересчитывается.
- Семантическое зрение не подменяется технической статистикой и не получает
  фиктивного пакета.
- Версия повышена до `0.0.78-dev`.
- Приложение, модели и установщики не запускались; компоненты не скачивались,
  установщик AI HUB не пересобирался.
- Проверки: restore успешно; Debug/Release build без ошибок и предупреждений;
  Debug/Release tests `186/186`; формат обоих проектов чистый; локализации
  `668/668`; UTF-8 scanner `340` файлов; `git diff --check` без содержательных
  ошибок.
- Commit/push не выполнялись. Активное ТЗ ожидает пользовательский сквозной
  тест.

## 2026-07-27 — ядро-владелец исполнительного комплекта `0.0.77-dev`

- Перед изменением контрактов карточки, пула кандидатов, resolver/route/bundle,
  orchestration, UI, тестов, версии и активного ТЗ создан path-safe backup:
  `H:\AI_HUB\_backups\20260727_implement_core_owned_bundle`.
- В backup сохранены все изменяемые исходники и документы. Для двух файлов,
  добавленных в backup после обнаружения неполного списка, точное состояние до
  правки восстановлено отдельным обратным патчем:
  `Models\CapabilityOrchestrationModels.cs` и
  `Services\SandboxExternalComponentDiscoveryService.cs`.
- Для полного локального отката восстановить файлы из backup по сохранённым
  относительным путям.
- Изменение отделяет смысловой план ядра от программной проверки фактов,
  разрешает одиночного установленного кандидата, запрещает неполную
  скачиваемую альтернативу и оставляет подтверждение только на скачивание.
- Проверки: restore успешно, Debug/Release build без ошибок и предупреждений,
  Debug/Release tests `182/182`, формат затронутых исходников чистый,
  локализации `668/668`, UTF-8 scanner `328` файлов,
  `git diff --check` без содержательных ошибок.
- Приложение и модели не запускались, компоненты не скачивались, установщик не
  пересобирался. Commit/push не выполнялись.
## 2026-08-05 — универсальный граф действий и доказательства результата

- Перед дополнением активного ТЗ и изменением контрактов оркестрации,
  исполнительной сессии, материализации, тестов, версии и истории создан
  path-safe backup:
  `H:\AI_HUB\_backups\20260805_universal_action_evidence`.
- В backup сохранены 13 исходных файлов и `manifest.json` с размерами и
  SHA-256. Для полного отката восстановить файлы по их исходным относительным
  путям.
- Приложение, модели, системные установщики и скачивание компонентов в этом
  проходе не запускаются. Commit/push и сборка установщика не выполняются.
- После реализации версия повышена до `0.0.79-dev`. Добавлены универсальный
  граф действий, квитанции фактического исполнения, программный пакет
  доказательств, раздельная техническая/доказательная/предметная проверка и
  честный `limited`-результат при отсутствии обязательных фактов.
- Проверки: restore успешно; Debug/Release build без ошибок и предупреждений;
  Debug/Release tests `193/193`; формат обоих проектов чистый; локализации
  `668/668`; UTF-8 scanner `356` файлов; `git diff --check` без содержательных
  ошибок.
- Приложение и модели не запускались, компоненты не скачивались, установщик не
  собирался. Commit/push не выполнялись. Активное ТЗ ожидает пользовательский
  сквозной тест.

## 2026-08-05 — контракт результата и покрытие маршрута

- Перед изменением моделей данных, планировщика маршрута, выбора координатора,
  каталога рабочих паттернов, исполнительной сессии, интерфейса, локализации,
  тестов, версии, активного ТЗ и истории создан path-safe backup:
  `H:\AI_HUB\_backups\20260805_outcome_route_contract`.
- В backup сохранены 29 существующих файлов с исходными относительными путями.
  Пропущенная при первом создании копия `CapabilityResolverService.cs`
  восстановлена в backup до исходного состояния до продолжения правок.
  Перед продолжением после разрыва дополнительно сохранён
  `MainWindow.Components.cs`.
  Новые файлы будут перечислены отдельно после реализации и удаляются при
  полном откате.
- Для полного локального отката восстановить сохранённые файлы из backup по
  исходным относительным путям.
- Приложение и модели не запускаются, компоненты не скачиваются, системные
  установщики и установщик AI HUB не запускаются. Commit/push не выполняются.
- Перед изменением тестов первичного внешнего поиска в тот же backup
  дополнительно сохранён
  `Исходники\AIHub.Tests\SandboxExternalComponentDiscoveryTests.cs`.
- Реализация доведена до `0.0.80-dev`: отчёт внешнего поиска хранится в
  `ChoiceTaskCard`, переживает контрольную точку сессии и не изменяет
  исполнимость маршрута без доверенного адаптера.
- При полном откате новые файлы, отсутствующие в исходном состоянии backup,
  требуется удалить после восстановления сохранённых файлов. В частности это
  модели и сервисы универсальной оркестрации, перечисленные в активном ТЗ.
- Финальные проверки: Debug build/tests успешно; Release build без ошибок и
  предупреждений; Release tests `200/200`; формат обоих проектов чистый;
  локализации `681/681`; UTF-8 scanner `388` файлов; `git diff --check` без
  содержательных ошибок.
- Приложение и модели не запускались, компоненты не скачивались, системные
  установщики и установщик AI HUB не запускались. Commit/push не выполнялись.

## 2026-08-06 — внутренний пакет семантического зрения и фильтрация веб-находок

- Перед изменением каталога компонентов, внешнего поиска, адаптеров,
  исполнительных инструментов, интерфейса, локализации, тестов, активного ТЗ,
  версии и истории создан path-safe backup:
  `H:\AI_HUB\_backups\20260806_vision_internal_recipe`.
- В backup сохранён 21 существующий файл с исходными относительными путями.
- Новый сервис семантического анализа изображений будет удаляться вручную при
  полном откате, затем сохранённые файлы следует восстановить из backup.
- Во время реализации приложение и модели не запускаются, компоненты не
  скачиваются, системные установщики и установщик AI HUB не запускаются.

## 2026-08-22 — посттестовый анализ vision-сессии и дополнение активного ТЗ

- Перед изменением активного ТЗ и документов истории создан path-safe backup:
  `H:\AI_HUB\_backups\20260822_session3_posttest_analysis_spec`.
- Сохранены исходные версии четырёх файлов с сохранением относительных путей:
  активное ТЗ динамической рабочей среды, `BACKUP_ОТКАТ.md`, `CONTEXTHUB.md`
  и `Диалог_сжато.md`.
- В активное ТЗ добавлен раздел 28 с подтверждёнными причинами сбоя после
  успешного `session_image_describe`, обязательными требованиями к
  `0.0.82-dev`, регрессионными проверками и границами реализации.
- Код и версия не менялись. Приложение и модели повторно не запускались,
  компоненты не скачивались, установщик AI HUB не пересобирался.
- Диагностический Release test текущего состояния: `201/203`. Два теста
  маршрута зависят от живого `Runtime/Components` и после реальной установки
  SmolVLM2 больше не воспроизводят ожидаемое состояние `package missing`.
- Commit/push и публикация не выполнялись.

## 2026-08-22 — реализация `0.0.82-dev`: доказательства и итоговый документ

- До первого изменения кода создан path-safe backup:
  `H:\AI_HUB\_backups\20260822_v082_evidence_result_chain`.
- Сохранены 33 существующих файла с исходными относительными путями:
  модели контрактов, сервисы доказательств/маршрута/исполнителя, vision,
  состояние компонентов, окно результата, локализации, затрагиваемые тесты,
  версия, активное ТЗ и документы истории.
- До изменения дополнительных resolver/acquisition-регрессий в ту же точку
  отката добавлены `ComponentCatalogTests.cs` и
  `SandboxOrchestrationTests.cs`.
- Пропущенный в первоначальном перечне `ExecutionRoutePlannerService.cs`
  добавлен в ту же точку отката и программно возвращён к точному состоянию до
  первого маленького патча; сравнение с рабочим файлом показывает только
  запланированную фильтрацию отрицательного OCR.
- Перед адаптацией сигнатуры gateway в ту же точку отката добавлен
  `SessionFileToolServiceTests.cs`.
- Новые файлы самостоятельной нормализации или тестовых helpers, если они
  появятся в этой реализации, при полном откате нужно удалить вручную, затем
  восстановить сохранённые файлы из backup.
- При полном откате удалить четыре новых файла:
  `Services\SpecialistToolEvidenceContractCatalog.cs`,
  `Services\SpecialistToolResultNormalizer.cs`,
  `Services\LimitedResultDocumentBuilder.cs` и
  `AIHub.Tests\TestComponentManagerFactory.cs`.
- Приложение, модели, загрузки компонентов и установщики в ходе правок не
  запускаются. Commit/push не выполняются без отдельной команды пользователя.
- Реализация завершена в версии `0.0.82-dev`. Restore успешно; Debug/Release
  tests `217/217`; Release build без ошибок и предупреждений; формат обоих
  проектов чистый; локализации `699/699`; UTF-8 scanner проверил 451 текстовый
  файл; `git diff --check` без содержательных ошибок.
- Приложение и модели не запускались, компоненты не скачивались, установщик не
  пересобирался. Commit/push не выполнялись. Для полного отката восстановить
  33 сохранённых файла по исходным относительным путям и удалить четыре новых
  файла из предыдущего пункта.

## 2026-08-22 — начало исправления WebP vision-runtime `0.0.83-dev`

- Перед дополнением активного ТЗ и изменением runtime, gateway, отбора рабочих
  паттернов, тестов, версии и истории создан path-safe backup:
  `H:\AI_HUB\_backups\20260822_v083_webp_vision_runtime`.
- Сохранены 14 существующих файлов с исходными относительными путями.
- Новые самостоятельные helpers подготовки vision-входа и диагностики runtime
  при полном откате нужно удалить вручную, затем восстановить сохранённые
  файлы из backup.
- Реализация ведётся без изменения исходных пользовательских изображений,
  без загрузки компонентов и без сборки установщика.
- При полном откате удалить два новых файла:
  `Исходники\AIHub\Services\VisionImagePayloadService.cs` и
  `Исходники\AIHub\Services\VisionRuntimeDiagnosticBuffer.cs`.
- Реализация завершена в `0.0.83-dev`. Restore, Debug/Release build,
  Debug/Release tests `225/225`, format и локализации `699/699` прошли.
  UTF-8 scanner проверил `3163` файла, `git diff --check` не обнаружил
  содержательных ошибок. Приложение и тяжёлая модель не запускались;
  компоненты не скачивались, установщик не пересобирался, commit/push не
  выполнялись.

## 2026-08-22 — фиксация Sandbox Alpha и закрытие активных ТЗ

- До изменения версии, локализаций, проектных документов и двух активных ТЗ
  создан path-safe backup:
  `H:\AI_HUB\_backups\20260822_sandbox_alpha_freeze_close_tz`.
- Сохранены 11 исходных файлов с прежними относительными путями; копии
  проверены по SHA-256.
- Полный откат этого прохода: восстановить файлы из backup, вернуть оба ТЗ из
  `ТЗ\Архив` в корень `ТЗ` и удалить добавленные записи закрытия из документов
  истории, если откатывается только решение о Sandbox Alpha.
- Локальные каталоги `_backups`, `Тесты\1` и пользовательский файл идеи не
  предназначены для публикации и не будут добавляться в Git.
- Итоговые проверки прошли: restore; Debug/Release build без ошибок и
  предупреждений; Debug/Release tests `225/225`; формат обоих проектов;
  локализации `699/699`; UTF-8 scanner `476` файлов; `git diff --check`.
  Собранная DLL сообщает `0.0.84-dev` / `0.0.84.0`.
- Приложение и тяжёлая модель не запускались, компоненты не скачивались,
  установщик не пересобирался по правилу проекта.
- Основной закрывающий commit `eb98381` (`feat: publish Sandbox Alpha
  foundation`) успешно отправлен в `origin/main`. Эта итоговая запись о
  публикации фиксируется отдельным документационным commit.
