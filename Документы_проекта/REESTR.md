# REESTR — зависимости, модели, backends и инструменты

Этот файл фиксирует всё, что может повлиять на лицензии, поставку, безопасность и воспроизводимость проекта.

Пока реальные зависимости, модели и backends не добавлены.

## Runtime-зависимости

| Название | Назначение | Версия | Лицензия | Источник | Поставка вместе с программой | Ограничения |
|---|---|---|---|---|---|---|
| Microsoft .NET Runtime | Запуск будущего .NET-приложения | 10.0.8 | MIT | Установлено вместе с `Microsoft.DotNet.SDK.10` через winget / Microsoft | Нет, системная предпосылка | Требуется Windows x64 |
| Microsoft Windows Desktop Runtime | Запуск будущего WPF-приложения | 10.0.8 | MIT | Установлено вместе с `Microsoft.DotNet.SDK.10` через winget / Microsoft | Нет, системная предпосылка | Требуется Windows x64; нужен для WPF |
| Python reranker runtime | Временный локальный runtime для запуска `BAAI/bge-reranker-v2-m3` через Python/Transformers при rerank web-поиска | Python 3.12 venv в `Runtime/Python/reranker/.venv`; `torch 2.12.0`, `transformers 5.9.0`, `safetensors 0.7.0` | Python PSF; PyTorch BSD-style; Transformers Apache-2.0; Safetensors Apache-2.0 | Установлено локально через `pip` в runtime-папку проекта | Нет, runtime-папка не публикуется в GitHub | Временное dev-решение; перед релизом заменить на управляемую поставку/установку или ONNX/встроенный backend |

## Developer tooling

| Название | Назначение | Версия | Лицензия | Источник | Обязательно для сборки | Ограничения |
|---|---|---|---|---|---|---|
| `check-cyrillic-integrity.ps1` | Внутренний scanner UTF-8 и поломанной кириллицы | 0.1 | GPL-3.0-or-later, как код проекта | `Инструменты/check-cyrillic-integrity.ps1` | Нет | Использует PowerShell и проверяет текстовые файлы проекта |
| `Запустить_AI_HUB.cmd` + `start-aihub.ps1` | Локальный запуск dev-сборки AI_HUB двойным кликом | 0.1 | GPL-3.0-or-later, как код проекта | Корень проекта | Нет | `.cmd` запускает PowerShell-скрипт, который выполняет `dotnet build` и стартует текущий Debug exe |
| Microsoft .NET SDK | Создание, restore и build будущего C# / WPF-проекта | 10.0.300 | MIT | `Microsoft.DotNet.SDK.10`, winget / Microsoft | Да | Установлен системно; WPF smoke-test на `net10.0-windows` прошёл |
| GitHub CLI | Авторизация и публикация проекта на GitHub | 2.93.0 | MIT | `GitHub.cli`, winget / GitHub | Нет | Установлен системно; текущая сессия вызывает `C:\Program Files\GitHub CLI\gh.exe` |

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

## AI-модели

| Название | Назначение | Версия/квант | Лицензия | Источник | Размер | Ограничения |
|---|---|---|---|---|---|---|
| Qwen3 8B GGUF | Основное ИИ-ядро AI HUB: быстрый диспетчер сценариев, будущей RAG-памяти и выбора инструментов | Qwen3 8B / GGUF / Q4_K_M / файл `Qwen3-8B-Q4_K_M.gguf` | Apache-2.0 | Hugging Face `Qwen/Qwen3-8B-GGUF`, commit `7c41481f57cb95916b40956ab2f0b139b296d974` | `5027783488` байт, около 5.03 ГБ | Не поставляется внутри установщика; скачивается отдельно пользователем через AI HUB в выбранную папку моделей; после загрузки проверяется SHA-256 `d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785` |
| Qwen3 0.6B GGUF | Тестовый web-download artifact для проверки, что ядро может попросить инструмент скачать маленькую модель | Qwen3 0.6B / GGUF / Q4_K_M / файл `Qwen3-0.6B-Q4_K_M.gguf` | Apache-2.0 | Hugging Face `jc-builds/Qwen3-0.6B-Q4_K_M-GGUF`, repo sha `c3111ad3faedb08c4abf76070a589e256258c62d` | `396705472` байт, около 397 МБ; SHA-256 `ac2d97712095a558e31573f62f466a3f9d93990898b0ec79d7c974c1780d524a` | Не является основным ядром и не включается в установщик; скачана в пользовательскую папку результатов `AI_HUB\Tools\Web\Downloads` для теста инструментов, Git ignored |
| BAAI bge-reranker-v2-m3 | Будущая вспомогательная модель интернет-инструмента: reranker для выбора более подходящих web-результатов | `BAAI/bge-reranker-v2-m3`, safetensors, commit `953dc6f6f85a1b2dbfca4c34a2796e7dde08d41e` | Apache-2.0 | Hugging Face `BAAI/bge-reranker-v2-m3` | `2293242108` байт, около 2.29 ГБ | Не поставляется внутри установщика; будет автоматически скачиваться AI HUB после основного ядра в выбранную папку моделей `Tools/Reranker/BAAI-bge-reranker-v2-m3`; пока только регистрируется и показывается в F12 как служебная модель без запуска |

## Встроенные библиотеки

| Название | Назначение | Версия | Лицензия | Источник | Ограничения |
|---|---|---|---|---|---|
| | | | | | |

## Правило

Перед добавлением любой зависимости нужно обновить этот файл и, если требуется, `THIRD_PARTY_NOTICES.md`.
