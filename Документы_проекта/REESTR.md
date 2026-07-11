# REESTR — зависимости, модели, backends и инструменты

Этот файл фиксирует всё, что может повлиять на лицензии, поставку, безопасность и воспроизводимость проекта.

Реальные зависимости, модели и backends перечислены ниже.

## Runtime-зависимости

| Название | Назначение | Версия | Лицензия | Источник | Поставка вместе с программой | Ограничения |
|---|---|---|---|---|---|---|
| Microsoft .NET Runtime | Запуск будущего .NET-приложения | 10.0.8 | MIT | Установлено вместе с `Microsoft.DotNet.SDK.10` через winget / Microsoft | Нет, системная предпосылка | Требуется Windows x64 |
| Microsoft Windows Desktop Runtime | Запуск будущего WPF-приложения | 10.0.8 | MIT | Установлено вместе с `Microsoft.DotNet.SDK.10` через winget / Microsoft | Нет, системная предпосылка | Требуется Windows x64; нужен для WPF |
| eSpeak NG | Локальный синтетический голос ядра и события слов для синхронного раскрытия текста | 1.52.0 | GPL-3.0-or-later | Официальный release `https://github.com/espeak-ng/espeak-ng/releases/tag/1.52.0`; MSI `https://github.com/espeak-ng/espeak-ng/releases/download/1.52.0/espeak-ng.msi` | Да, DLL и `espeak-ng-data` копируются в `VoiceRuntime/eSpeakNG` при наличии подготовленного runtime | Только голос ядра; MSI SHA-256 `7F673C709EA5DD579D3B5EBB98688CC575328A6AB7438D2BC405B88CEDAEAFB9`, DLL SHA-256 `E737572DF0A35A32B7BD444537C661C1C916B13B0B91351030C7F1D531307BEB`; runtime готовится скриптом `Инструменты/setup-espeak-ng-runtime.ps1`; требуется поставка лицензии и доступность исходника |
| RHVoice через Windows SAPI | Альтернативный локальный голос ядра, выбираемый пользователем вместо eSpeak NG | Engine 1.18.1; Aleksandr 4.2.2; Slt 4.1.2 | RHVoice engine: LGPL-2.1-or-later для C API, репозиторий также помечен GPL-2.0; Slt: CMU License; лицензия пакета Aleksandr явно не объявлена в его репозитории | Официальные voice releases `RHVoice/aleksandr-rus` и `RHVoice/slt-eng`; установка скриптом `Инструменты/setup-rhvoice.ps1` | Нет, используется как отдельно установленный системный SAPI-компонент | В UI называется `Просто ИИ голос`; eSpeak NG остается вариантом по умолчанию `Привет из 80-ых`; установщики и voice data не включать в publish до отдельной юридической проверки пакета Aleksandr |
| Python reranker runtime | Временный локальный runtime для запуска `BAAI/bge-reranker-v2-m3` через Python/Transformers при rerank web-поиска | Python 3.12 venv в `Runtime/Python/reranker/.venv`; `torch 2.12.0`, `transformers 5.9.0`, `safetensors 0.7.0` | Python PSF; PyTorch BSD-style; Transformers Apache-2.0; Safetensors Apache-2.0 | Установлено локально через `pip` в runtime-папку проекта | Нет, runtime-папка не публикуется в GitHub | Временное dev-решение; перед релизом заменить на управляемую поставку/установку или ONNX/встроенный backend |

## Developer tooling

| Название | Назначение | Версия | Лицензия | Источник | Обязательно для сборки | Ограничения |
|---|---|---|---|---|---|---|
| `check-cyrillic-integrity.ps1` | Внутренний scanner UTF-8 и поломанной кириллицы | 0.1 | GPL-3.0-or-later, как код проекта | `Инструменты/check-cyrillic-integrity.ps1` | Нет | Использует PowerShell и проверяет текстовые файлы проекта |
| `Запустить_AI_HUB.cmd` + `start-aihub.ps1` | Локальный запуск dev-сборки AI_HUB двойным кликом | 0.1 | GPL-3.0-or-later, как код проекта | Корень проекта | Нет | `.cmd` запускает PowerShell-скрипт, который выполняет `dotnet build` и стартует текущий Debug exe |
| Microsoft .NET SDK | Создание, restore и build будущего C# / WPF-проекта | 10.0.300 | MIT | `Microsoft.DotNet.SDK.10`, winget / Microsoft | Да | Установлен системно; WPF smoke-test на `net10.0-windows` прошёл |
| GitHub CLI | Авторизация и публикация проекта на GitHub | 2.93.0 | MIT | `GitHub.cli`, winget / GitHub | Нет | Установлен системно; текущая сессия вызывает `C:\Program Files\GitHub CLI\gh.exe` |
| MSTest.Sdk | Автоматические тесты JSON-контракта и состояния сценария | 4.2.3 | MIT | NuGet `MSTest.Sdk` | Да, только для test-проекта | Developer tooling; в runtime и установщик не включается |

## Внешние инструменты

| Название | Назначение | Версия | Лицензия | Источник | Автоустановка | Ограничения |
|---|---|---|---|---|---|---|
| Inno Setup | Создание тестового `.exe` установщика AI_HUB для Windows | 6.7.3 | Собственная лицензия Inno Setup, freeware; исходники доступны у JR Software | `JRSoftware.InnoSetup`, winget / JR Software | Установлен по подтверждению пользователя | Используется только как developer tooling; в поставку AI_HUB не встраивается |

## Backends

| Название | Назначение | Версия | Лицензия | Источник | Способ установки | Ограничения |
|---|---|---|---|---|---|---|
| llama.cpp | Локальный GGUF-backend: `llama-server.exe` как основной debug-runtime, `llama-cli.exe` как fallback | release `b9442`, Windows CUDA 12.4 x64 | MIT | GitHub `ggml-org/llama.cpp`, release `b9442` | Скачан локально в `Runtime/Backends/llama.cpp/b9442/win-cuda-12.4-x64`; не публикуется в GitHub | Для Qwen3 server нужно запускать с `--reasoning off`, иначе OpenAI-compatible `message.content` может быть пустым; debug-окно запускает server на свободном loopback-порту; модели не получают доступ к файлам, интернету, shell и настройкам Windows |

## Внешние сетевые сервисы

| Название | Назначение | Версия/API | Лицензия/условия | Источник | Поставка вместе с программой | Ограничения |
|---|---|---|---|---|---|---|
| ipwho.is / ipwhois.io | Примерное автоматическое определение местоположения пользователя по IP для скрытого контекста ядра | HTTPS JSON endpoint `https://ipwho.is/?lang=ru` | Внешний сервис; free endpoint без API-ключа, указан fair-use limit 1 запрос/сек и 60 запросов/60 сек; free endpoint предназначен для non-commercial use | Официальная документация `https://ipwhois.io/documentation` | Нет, это внешний запрос; IP не сохраняется в профиле AI HUB | Использовать редко и best-effort: при отсутствии сохранённого auto-местоположения; при недоступности сервиса программа работает без местоположения; перед публичным/коммерческим релизом пересмотреть условия или заменить провайдера |
| DuckDuckGo Lite | Первый dev-провайдер web-поиска для проверки Tool Gateway без API-ключа | HTML endpoint `https://lite.duckduckgo.com/lite/?q=...` | Внешний web-сервис; условия DuckDuckGo нужно пересмотреть перед публичным релизом | `https://lite.duckduckgo.com/lite/` | Нет, это внешний HTTPS-запрос | Используется как временный dev-провайдер; HTML-парсинг хрупкий, в будущем заменить на официальный API-провайдер или локальный SearXNG |
| Bing HTML Search | Резервный dev-провайдер web-поиска, если DuckDuckGo вернул пустую выдачу или HTML-парсер не сработал | HTML endpoint `https://www.bing.com/search?q=...` | Внешний web-сервис; условия Microsoft/Bing нужно пересмотреть перед публичным релизом | `https://www.bing.com/search` | Нет, это внешний HTTPS-запрос | Временный fallback для разработки; HTML-парсинг хрупкий, не считать финальным поисковым API |
| Hugging Face Hub API | Структурированный подбор моделей, файлов и построение проверяемого накопительного локального каталога вместо HTML-поиска | REST endpoints `https://huggingface.co/api/models` и `https://huggingface.co/api/models/{repoId}` | Внешний web-сервис Hugging Face; условия Hub/API нужно соблюдать отдельно от лицензий конкретных моделей | `https://huggingface.co/docs/hub/api` | Нет, это внешний HTTPS-запрос | Посевной справочник находится в `Каталоги/huggingface-catalog-seed.json`; синхронизатор сохраняет raw JSON/Model Card, URL источника, SHA ревизии, SHA-256 и JSONL-журнал изменений; радар автоматически допускает только публичные модели с подтверждённым размером `>8B` и отбрасывает чистые quantized-упаковки; metadata считается данными Hub, Model Card — утверждением автора; модели-кандидаты не скачиваются и не поставляются с программой |

## AI-модели

| Название | Назначение | Версия/квант | Лицензия | Источник | Размер | Ограничения |
|---|---|---|---|---|---|---|
| Qwen3 8B GGUF | Основное ИИ-ядро AI HUB: быстрый диспетчер сценариев, будущей RAG-памяти и выбора инструментов | Qwen3 8B / GGUF / Q4_K_M / файл `Qwen3-8B-Q4_K_M.gguf` | Apache-2.0 | Hugging Face `Qwen/Qwen3-8B-GGUF`, commit `7c41481f57cb95916b40956ab2f0b139b296d974` | `5027783488` байт, около 5.03 ГБ | Не поставляется внутри установщика; скачивается отдельно пользователем через AI HUB в выбранную папку моделей; после загрузки проверяется SHA-256 `d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785` |
| Qwen3 0.6B GGUF | Тестовый web-download artifact для проверки, что ядро может попросить инструмент скачать маленькую модель | Qwen3 0.6B / GGUF / Q4_K_M / файл `Qwen3-0.6B-Q4_K_M.gguf` | Apache-2.0 | Hugging Face `jc-builds/Qwen3-0.6B-Q4_K_M-GGUF`, repo sha `c3111ad3faedb08c4abf76070a589e256258c62d` | `396705472` байт, около 397 МБ; SHA-256 `ac2d97712095a558e31573f62f466a3f9d93990898b0ec79d7c974c1780d524a` | Не является основным ядром и не включается в установщик; скачана в пользовательскую папку результатов `AI_HUB\Tools\Web\Downloads` для теста инструментов, Git ignored |
| BAAI bge-reranker-v2-m3 | Вспомогательная модель интернет-инструмента: reranker для выбора более подходящих web-результатов | `BAAI/bge-reranker-v2-m3`, safetensors, commit `953dc6f6f85a1b2dbfca4c34a2796e7dde08d41e` | Apache-2.0 | Hugging Face `BAAI/bge-reranker-v2-m3` | `2293242108` байт, около 2.29 ГБ | Не поставляется внутри установщика; автоматически скачивается AI HUB после основного ядра в выбранную папку моделей `Tools/Reranker/BAAI-bge-reranker-v2-m3`; используется `web_search` через локальный Python/Transformers runtime, при недоступности включается lexical fallback |

## Встроенные библиотеки

| Название | Назначение | Версия | Лицензия | Источник | Ограничения |
|---|---|---|---|---|---|
| System.Speech | Доступ к установленным Windows SAPI-голосам RHVoice и событиям произнесённых слов | 10.0.9 | MIT | NuGet `System.Speech`, Microsoft | Windows-only; библиотека включается в publish, сами RHVoice-голоса остаются внешней установкой |

## Правило

Перед добавлением любой зависимости нужно обновить этот файл и, если требуется, `THIRD_PARTY_NOTICES.md`.
