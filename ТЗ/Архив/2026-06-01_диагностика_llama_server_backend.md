# ТЗ: диагностика и восстановление llama-server backend

Дата: 2026-06-01

## Цель

Победить проблему запуска `llama-server.exe` для локального backend-а AI HUB.

Сейчас модель `Qwen3-8B-Q4_K_M.gguf` рабочая и отвечает через `llama-cli.exe`, но server-режим не работает. Нужно понять точную причину, восстановить запуск server-а или зафиксировать безопасный fallback.

## Контекст

Текущий backend:

- `llama.cpp`;
- release `b9442`;
- Windows CUDA 12.4 x64;
- локальная папка: `Runtime\Backends\llama.cpp\b9442\win-cuda-12.4-x64`;
- модель: `Данные_для_внедрения\Модели\Core\Qwen3-8B\Qwen3-8B-Q4_K_M.gguf`.

Официальные источники для backend-а:

- GitHub releases `ggml-org/llama.cpp`: `https://github.com/ggml-org/llama.cpp/releases`
- Windows-инструкция llama.vscode/llama.cpp: `https://github.com/ggml-org/llama.vscode/wiki/Windows`

Наблюдавшиеся симптомы:

- `llama-server.exe` завершался с кодом `0xC0000135` (`STATUS_DLL_NOT_FOUND`);
- после ручного добавления части DLL ранее появлялся код `0xC0000906` (`STATUS_VIRUS_INFECTED`);
- `llama-cli.exe` работает и отвечает.

## Принципы безопасности

- Не отключать Windows Defender.
- Не добавлять исключения Defender автоматически.
- Не запускать модель с доступом к файлам, интернету, shell, инструментам или системным настройкам.
- Не удалять рабочий `llama-cli` fallback.
- Все новые runtime-файлы оставлять в `Runtime`, который не публикуется в GitHub.

## План работ

1. Проверить текущую папку backend-а:
   - наличие `llama-server.exe`;
   - наличие `llama-server-impl.dll`;
   - наличие CUDA DLL;
   - коды выхода `llama-server.exe --version` и `llama-cli.exe --version`.
2. Проверить официальные архивы release `b9442`:
   - есть ли внутри `llama-server-impl.dll`;
   - не была ли текущая папка распакована неполно.
3. Создать чистую диагностическую папку server-backend-а.
4. Распаковать официальный набор заново, без ручной сборки отдельных DLL по памяти.
5. Проверить:
   - `llama-server.exe --version`;
   - запуск server-а на локальном `127.0.0.1` с тестовым портом;
   - доступность `/health` или совместимого endpoint-а;
   - короткий prompt к Qwen3 8B через server, если server стартует.
6. Если Defender снова блокирует файл:
   - зафиксировать точный файл и код;
   - не обходить защиту;
   - попробовать другой официальный вариант только отдельным шагом: CPU/Vulkan/другой release.
7. Если server стартует:
   - сохранить рабочую папку как текущий server backend;
   - обновить `REESTR.md`, `THIRD_PARTY_NOTICES.md` и историю;
   - подготовить следующий шаг для интеграции server-а в AI HUB отдельным ТЗ.

## Критерии готовности

Минимум:

- известна точная причина сбоя текущего `llama-server.exe`;
- есть безопасно зафиксированный результат: server работает или причина блокировки документирована;
- `llama-cli` fallback не сломан;
- `dotnet build` и scanner кириллицы проходят;
- runtime/model остаются игнорируемыми Git.

Желательно:

- `llama-server.exe` запускается;
- server отвечает на health/prompt;
- есть понятный путь будущей интеграции server-а в AI HUB.

## Проверки

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj`;
- `Инструменты\check-cyrillic-integrity.ps1`;
- проверка ключей `ru.json` / `en.json`, если менялись строки;
- `git status --ignored` для `Runtime` и `Данные_для_внедрения/Модели`;
- проверка `llama-cli` fallback;
- проверка `llama-server`.

## Rollback

Так как backend-файлы лежат в `Runtime`, откат должен быть простым:

- удалить новую диагностическую папку backend-а;
- восстановить документы из backup;
- рабочий `llama-cli` fallback не удалять.

## Результат диагностики 2026-06-01

Причина первого сбоя найдена:

- в текущей рабочей папке `Runtime\Backends\llama.cpp\b9442\win-cuda-12.4-x64` отсутствовал файл `llama-server-impl.dll`;
- при этом официальный архив `llama-b9442-bin-win-cuda-12.4-x64.zip` содержит `llama-server-impl.dll`;
- из-за отсутствия DLL `llama-server.exe` завершался с кодом `0xC0000135` (`STATUS_DLL_NOT_FOUND`).

Что сделано:

- создана чистая диагностическая папка `Runtime\Backends\llama.cpp\b9442\server-diagnostic-win-cuda-12.4-x64`;
- в неё заново распакованы оба официальных архива:
  - `llama-b9442-bin-win-cuda-12.4-x64.zip`;
  - `cudart-llama-bin-win-cuda-12.4-x64.zip`;
- проверено, что `llama-server-impl.dll` после чистой распаковки присутствует;
- `llama-server.exe --version` в диагностической папке завершился успешно;
- недостающий `llama-server-impl.dll` скопирован в основную рабочую папку backend-а.

Рабочий запуск server-а:

```text
llama-server.exe ^
  -m H:\AI_HUB\Данные_для_внедрения\Модели\Core\Qwen3-8B\Qwen3-8B-Q4_K_M.gguf ^
  --host 127.0.0.1 ^
  --port 18082 ^
  --ctx-size 4096 ^
  --n-gpu-layers 99 ^
  --jinja ^
  --reasoning off ^
  --no-webui
```

Важное уточнение:

- без `--reasoning off` server для Qwen3 включал thinking-режим, и `message.content` в первом OpenAI-compatible ответе оказался пустым;
- с `--reasoning off` endpoint `/v1/chat/completions` вернул нормальный ответ.

Фактические проверки:

- `llama-server.exe --version` в основной рабочей папке — exit code `0`;
- `GET http://127.0.0.1:18082/health` — `{ "status": "ok" }`;
- `POST http://127.0.0.1:18082/v1/chat/completions` — ответ `Основной сервер работает.`;
- скорость тестового ответа: около `1478` prompt tokens/sec и `143` generated tokens/sec;
- `llama-cli.exe` fallback не тронут и остаётся рабочим.

Предварительный вывод:

`llama-server.exe` побеждён на уровне runtime-запуска. Следующий отдельный шаг — интегрировать server-backend в AI HUB как постоянный локальный сервис с запуском, остановкой, health-check и fallback на `llama-cli`.

## Шаг 2: интеграция server-backend в debug-окно

По решению пользователя интеграция `llama-server` выполняется в этом же ТЗ, а не отдельным документом.

### Цель шага 2

Сделать так, чтобы debug-окно AI HUB использовало `llama-server.exe` как предпочтительный backend, а `llama-cli.exe` оставался fallback.

### Реализация

- Добавить сервис `LlamaServerRuntimeService`.
- Сервис должен:
  - искать `llama-server.exe` в текущей runtime-папке `llama.cpp b9442`;
  - выбирать свободный локальный порт автоматически;
  - запускать server скрытым дочерним процессом;
  - ждать `/health`;
  - отправлять запросы в `/v1/chat/completions`;
  - запускать Qwen3 с `--reasoning off`;
  - останавливать server при закрытии debug-окна;
  - не давать модели доступ к файлам, интернету, shell, инструментам и системным настройкам.
- Debug-окно должно:
  - показывать в логах наличие server backend-а и CLI fallback-а;
  - сначала пробовать `llama-server`;
  - при ошибке server-а переключаться на `llama-cli`;
  - сохранять кнопку `Стоп`;
  - не ронять основное приложение при ошибке backend-а.

### Версия

После интеграции повысить:

`0.0.20-dev` -> `0.0.21-dev`

### Проверки шага 2

- `dotnet build`;
- scanner кириллицы;
- совпадение ключей `ru.json` / `en.json`;
- запуск debug-окна по `F12`;
- отправка prompt-а из debug-окна должна использовать `llama-server` и получить ответ;
- `llama-cli` fallback остаётся доступным;
- runtime/model остаются игнорируемыми Git;
- установщик не собирать без отдельной команды пользователя.

## Результат шага 2 2026-06-01

Сделано:

- добавлен `LlamaServerRuntimeService`;
- debug-окно теперь предпочитает `llama-server.exe`;
- `llama-server` запускается скрытым дочерним процессом;
- порт выбирается автоматически через свободный loopback-port;
- сервис ждёт `/health`;
- запросы отправляются в `/v1/chat/completions`;
- server запускается с `--reasoning off`;
- при закрытии debug-окна server останавливается;
- кнопка `Стоп` отменяет запрос и останавливает server-процесс;
- при ошибке server-а debug-окно переключается на `llama-cli` fallback;
- текущий prompt больше не дублируется в отправляемой истории;
- версия повышена до `0.0.21-dev`.

Фактические проверки:

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` — успешно, без предупреждений;
- `Инструменты\check-cyrillic-integrity.ps1` — успешно;
- ключи `ru.json` и `en.json` совпадают: `173/173`;
- Win32 smoke-test: `F12` открывает окно `AI HUB — отладка моделей`, процесс не падает;
- server endpoint smoke-test: `/health` возвращает `ok`;
- server endpoint prompt-test: ответ `Сервер бэкенда работает корректно.`;
- runtime/model остаются игнорируемыми Git;
- установщик не собирался по правилу проекта.

Ограничение:

- UI Automation smoke-test отправки prompt-а через само окно оказался хрупким и не нашёл окно по имени, поэтому не используется как критерий готовности. Ручная проверка пользователем остаётся желательной перед закрытием ТЗ.

## Мини-правка 2026-06-01: перенос текста в debug-окне

По замечанию пользователя исправлен раздражающий горизонтальный скроллинг в чате и логах debug-окна.

Сделано:

- для `ChatListBox` и `LogListBox` отключён горизонтальный scrollbar;
- элементы списков отображаются через `TextBlock` с `TextWrapping=Wrap`;
- перенос идёт по словам, без резки слов;
- вертикальный scrollbar сохранён;
- версия повышена до `0.0.22-dev`.

Проверки:

- `dotnet build H:\AI_HUB\Исходники\AIHub\AIHub.csproj` — успешно, без предупреждений;
- `Инструменты\check-cyrillic-integrity.ps1` — успешно;
- установщик не собирался по правилу проекта.
