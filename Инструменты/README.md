# Инструменты — AI_HUB

Будущая папка для вспомогательных скриптов и инструментов разработки.

Сюда не нужно класть пользовательские результаты, модели или backends.

Если инструмент становится зависимостью проекта, его нужно зафиксировать в `Документы_проекта/REESTR.md`.

## Доступные инструменты

### `check-cyrillic-integrity.ps1`

Проверяет текстовые файлы проекта на:

- невалидный UTF-8;
- символ замены `U+FFFD`;
- типичные следы поломанной кириллицы после неправильной перекодировки.

Запуск из корня проекта:

```powershell
powershell -ExecutionPolicy Bypass -File .\Инструменты\check-cyrillic-integrity.ps1
```

По умолчанию папка `Backups` не проверяется, чтобы старые копии не мешали текущему результату.

### `build-installer.ps1`

Собирает тестовый установщик AI_HUB через Inno Setup.

Обычный запуск лучше делать двойным кликом по файлу в корне проекта:

```text
Собрать_установщик_AI_HUB.cmd
```

Что делает скрипт:

- читает версию из `VERSION`;
- выполняет `dotnet publish` для `win-x64`;
- собирает self-contained приложение;
- запускает Inno Setup Compiler;
- складывает готовый `.exe` в `Тесты/Установщики`.

Если Inno Setup не установлен, скрипт покажет команду:

```powershell
winget install --id JRSoftware.InnoSetup -e
```

### `setup-espeak-ng-runtime.ps1`

Скачивает официальный MSI eSpeak NG `1.52.0`, проверяет закреплённый SHA-256 и административно распаковывает переносимый runtime без установки системного голоса.

```powershell
powershell -ExecutionPolicy Bypass -File .\Инструменты\setup-espeak-ng-runtime.ps1
```

Runtime сохраняется в `Runtime/Voice/eSpeakNG/1.52.0` и копируется в build/publish через проект AIHub, если он подготовлен.

### `setup-rhvoice.ps1`

Скачивает с официальных GitHub releases, проверяет SHA-256 и тихо устанавливает четыре SAPI-профиля:

- `Aleksandr` — русский голос ядра;
- `Slt` — английский голос ядра;
- `Elena` — русский голос исполнителя «Режима неопределённости»;
- `Bdl` — английский голос исполнителя «Режима неопределённости».

```powershell
powershell -ExecutionPolicy Bypass -File .\Инструменты\setup-rhvoice.ps1
```

RHVoice является необязательной альтернативой. По умолчанию ядро AI HUB продолжает использовать eSpeak NG. Четыре профиля зарезервированы для указанных ролей; инструменты других сценариев не используют их без прямой просьбы пользователя.

### `setup-qwen-omni-runtime.ps1`

Создаёт отдельную Python 3.12-среду для полного Qwen2.5-Omni Heavy runtime в
`Runtime/Python/qwen3-omni/.venv`, устанавливает согласованные CUDA PyTorch
`2.11.0+cu130`, Torchvision `0.26.0+cu130`, Torchaudio `2.11.0+cu130` и
закреплённые версии библиотек Omni, затем проверяет CUDA и официальные
Omni-классы. Скрипт рассчитан на совместимые Windows/NVIDIA-компьютеры, а не
на конкретную RTX 4090.

```powershell
powershell -ExecutionPolicy Bypass -File .\Инструменты\setup-qwen-omni-runtime.ps1
```

При необходимости другой согласованной сборки PyTorch передать официальный
index и три версии через `-TorchIndexUrl`, `-TorchVersion`,
`-TorchVisionVersion` и `-TorchAudioVersion`. Скрипт скачивает крупные
runtime-пакеты и поэтому запускается только отдельно перед реальным Heavy-тестом;
веса модели он не скачивает.

Нормальный текстовый путь Heavy использует официальный Thinker-only. Worker
автоматически выбирает `flash_attention_2`, если совместимый внешний пакет
`flash-attn` уже установлен; в штатной native Windows-среде без него применяется
PyTorch SDPA с доступным CUDA flash backend. Setup-скрипт намеренно не пытается
собирать `flash-attn` без отдельного совместимого Windows toolchain. Для речи
worker временно переключается на полный Omni после явной команды пользователя.
