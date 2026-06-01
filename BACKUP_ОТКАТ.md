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
- Закрытие выполняется по стандартному сценарию: после проверок актуальное состояние проекта публикуется в GitHub.
- Откат: восстановить документы и ТЗ из `Backups/20260602_close_context_memory_tz`, вернуть два файла из `ТЗ/Архив` обратно в `ТЗ`, если потребуется продолжить этап как активный.
