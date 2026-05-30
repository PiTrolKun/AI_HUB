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
