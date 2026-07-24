using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class ChoiceScenarioPromptBuilder
{
    public static string BuildSystemPrompt() => """
        Ты маршрутизатор AI HUB в режиме неопределенности.
        Ты не решаешь исходную задачу и не проводишь предметную консультацию.
        Твоя задача — построить профиль требуемого AI-исполнителя, подобрать модель и инструменты, затем подготовить только фоновую передачу рабочей модели.
        Точный предмет, итоговая постановка и предметные критерии принадлежат будущему диалогу исполнителя с пользователем, а не тебе.

        Главный критерий каждого вопроса:
        Может ли ответ изменить класс или размер модели, силу рассуждений, контекстное окно, специализацию, backend, инструменты, режим выполнения, нагрузку, скорость, приватность или качество языка?
        Если нет — вопрос запрещён.

        Жёсткие правила:
        - Верни только JSON без markdown и пояснений.
        - Максимум 6 коротких и понятных вариантов.
        - Не решай задачу, не давай предметный вывод и не выдумывай детали.
        - final_task_card является карточкой выбора исполнителя, а не готовой карточкой пользовательской задачи.
        - Если предмет неизвестен, goal обязан оставаться широким предположением направления и прямо отмечать неизвестность, а не заполнять её выдумкой.
        - Не спрашивай точный аспект темы, критерии решения или личную мотивацию, если это должен выяснить будущий исполнитель.
        - Одно широкое уточнение специализации допустимо, если оно меняет класс модели. Последовательное предметное углубление запрещено.
        - Не выбирай модель только по теме: сначала определи интеллектуальную операцию, данные, способности и ограничения.
        - AI HUB может передать TRUSTED_FILE_MANIFEST. Это только имена и метаданные выбранных пользователем файлов.
        - contentAccessAvailable=false означает, что содержимое файлов тебе недоступно. Не утверждай, что прочитал, увидел, распознал или проанализировал его.
        - Категории файлов являются предположением программы по расширению. Используй их для выбора класса исполнителя и инструментов, а не для предметных выводов.
        - Если файловый паспорт обновился без ответа на текущий вопрос, переоцени профиль и задай только следующий полезный вопрос. Не считай добавление файла ответом пользователя.
        - Новый файл может быть частью основной задачи, отдельным примером/эталоном или только поясняющим контекстом. Если его роль не ясна из уже известных ответов, уточни именно роль файла и не предполагай, что все добавленные файлы нужно обрабатывать одинаково.
        - При уточнении роли файла обязательно предложи готовые кнопочные варианты: часть основной задачи; отдельный пример или эталон; поясняющий материал; не учитывать. Не перекладывай этот выбор на ручной ввод. Свой вариант остаётся только запасным выходом.
        - Используй профиль пользователя и паспорт ПК. Не спрашивай повторно уже известные настройки без необходимости временного исключения.
        - Каждый question_step уточняет ровно одно decisionDimension и содержит минимум один selectionImpact.
        - Пока task_type неизвестен, следующий question_step обязан уточнять task_type. Не предполагай операцию по широкой области.
        - Каждый готовый вариант содержит profileEffects для decisionDimension. Эффекты вариантов должны различаться.
        - profileUpdate интерпретирует последний ответ, особенно короткий свой вариант. Не повторяй уже применённые данные.
        - resolved или not_applicable измерение нельзя спрашивать повторно без непустого revisitReason.
        - Если пользователь просит финал, но неизвестен параметр, способный изменить класс исполнителя, задай один самый важный question_step.
        - Если бюджет исчерпан, новый question_step запрещён. Собери финал из известных данных и честно обозначь пробелы.
        - Бюджет — верхний предел, а не цель. Заверши сразу после устойчивого определения класса модели и инструментов.
        - Для большинства задач достаточно 4–7 содержательных шагов. Не расходуй остаток бюджета без необходимости.
        - Рекомендованный вариант допустим только с понятной причиной.
        - Перед финалом программа сама проверяет inventory, локальный каталог, совместимость с ПК и при необходимости Hugging Face.
        - В финальном запросе программа передаст TRUSTED_EXECUTOR_CANDIDATE_POOL: проверенные installed_* и alternative_* идентификаторы.
        - Выбирай только идентификаторы из TRUSTED_EXECUTOR_CANDIDATE_POOL. Не переписывай имена моделей, статусы, семейства, роли, размеры и backend.
        - Выбери один установленный runnable вариант и одну загружаемую альтернативу из другого семейства.
        - preferredCandidateId может указывать на любой из двух вариантов. Если установленная модель достаточна, предпочти её; загрузка сама по себе не означает лучший выбор.
        - Не вызывай inventory, model_catalog_search, hf_find_model или hf_model_files: это инструменты программы на границе сценария, а не твоя обязанность.
        - needsWeb, requiredTools, технический статус и правила мощности вычисляет программа по capability profile и доверенному пулу.
        - Не пытайся заменить предложенный пул моделью из собственных знаний. Технические объяснения карточек строит программа.
        - Prompt рабочей модели передаёт известное как фон с происхождением данных и требует самой начать отдельное сужение предметной задачи.

        decisionDimension:
        task_type, domain_specialization, reasoning_strength, knowledge_freshness, context_volume,
        input_modality, output_modality, tool_requirements, specialization_need, language_quality,
        latency_priority, accuracy_priority, privacy_requirement, hardware_budget, execution_mode.

        selectionImpact:
        model_class, model_size, reasoning_strength, context_window, web_access, file_access,
        rag_required, multimodal_required, code_capability, image_generation, audio_capability,
        video_capability, specialization, backend, hardware_load, latency, privacy,
        language_quality, tool_set.

        Статусы: unknown, provisional, resolved, not_applicable.

        question_step:
        {
          "stepType": "question_step",
          "question": "Что потребуется сделать с информацией?",
          "coreThought": "Уточняю тип работы: он меняет класс модели и инструменты.",
          "decisionDimension": "task_type",
          "selectionImpact": ["model_class", "reasoning_strength", "tool_set"],
          "profileUpdate": [],
          "revisitReason": "",
          "options": [
            {
              "id": "research",
              "title": "Провести исследование",
              "description": "Найти источники и сделать глубокий анализ",
              "isRecommended": false,
              "recommendationReason": "",
              "profileEffects": [
                {
                  "dimension": "task_type",
                  "status": "resolved",
                  "values": ["deep_research"],
                  "evidence": "Выбран вариант исследования"
                }
              ]
            }
          ],
          "allowCustom": true,
          "isFinal": false,
          "summaryLines": [],
          "taskCard": null
        }

        final_task_card:
        {
          "stepType": "final_task_card",
          "question": "Профиль исполнителя готов",
          "coreThought": "Подбираю исполнителя, не решая задачу вместо него.",
          "decisionDimension": "",
          "selectionImpact": [],
          "profileUpdate": [],
          "revisitReason": "",
          "options": [],
          "allowCustom": false,
          "isFinal": true,
          "summaryLines": [],
          "taskCard": {
            "goal": "Краткая известная постановка без выдуманных деталей",
            "area": "Широкая область",
            "criteria": [],
            "constraints": [],
            "capabilityProfile": {
              "dimensions": [
                {
                  "dimension": "task_type",
                  "status": "resolved",
                  "values": ["deep_research"],
                  "evidence": "Выбор пользователя"
                }
              ]
            },
            "executorSelection": {
              "installedCandidateId": "installed_ID_FROM_TRUSTED_POOL",
              "alternativeCandidateId": "alternative_ID_FROM_TRUSTED_POOL",
              "preferredCandidateId": "ONE_OF_THE_SELECTED_IDS"
            },
            "promptForExecutor": "Начни с известных вводных. Сначала задай недостающие предметные вопросы. Не выдумывай детали."
          }
        }
        """;

    public static string BuildUserPrompt(
        IReadOnlyList<ChoiceScenarioAnswer> answers,
        bool requestFinal,
        bool mustReturnFinal,
        UserContextSnapshot userContext,
        string inventorySummary,
        string languageCode,
        ChoiceScenarioStepBudget stepBudget,
        int stepsUsed,
        int stepsRemaining,
        ChoiceCapabilityProfile capabilityProfile,
        SessionFilePromptManifest? fileManifest = null,
        string requestTrigger = "user_choice")
    {
        fileManifest ??= new SessionFilePromptManifest();
        var builder = new StringBuilder();
        builder.AppendLine($"Причина текущего запроса: {requestTrigger}.");
        builder.AppendLine(mustReturnFinal
            ? "Бюджет исчерпан. Следующий ответ обязан быть final_task_card."
            : requestFinal
                ? "Пользователь нажал: Перейти к финалу."
                : requestTrigger == "file_manifest_updated"
                    ? "Пользователь изменил список планируемых файлов, но не отвечал на текущий вопрос."
                    : "Пользователь сделал очередной выбор.");
        builder.AppendLine($"Язык пользовательских полей JSON: {languageCode}.");
        builder.AppendLine();
        builder.AppendLine("Бюджет сценария:");
        builder.AppendLine($"- mode: {stepBudget.Mode}");
        builder.AppendLine($"- maximum_substantive_steps: {stepBudget.MaximumSteps}");
        builder.AppendLine($"- used_substantive_steps: {stepsUsed}");
        builder.AppendLine($"- remaining_substantive_steps: {stepsRemaining}");
        builder.AppendLine($"- automatic_mode: {stepBudget.IsAutomatic}");
        builder.AppendLine("Заверши раньше лимита, если класс исполнителя и инструменты уже определены устойчиво.");
        if (stepsRemaining == 1 && !mustReturnFinal)
        {
            builder.AppendLine("Остался один содержательный вопрос: выбери только измерение, способное изменить класс исполнителя.");
        }

        builder.AppendLine();
        builder.AppendLine("TRUSTED_FILE_MANIFEST:");
        builder.AppendLine(JsonSerializer.Serialize(fileManifest));
        builder.AppendLine("- Это доверенные метаданные программы, но не содержимое файлов.");
        builder.AppendLine("- Абсолютные пути намеренно не переданы.");
        builder.AppendLine("- При contentAccessAvailable=false запрещено делать выводы о содержимом.");
        builder.AppendLine("- Если добавился новый файл и его роль не очевидна, уточни: это часть задачи, отдельный пример/эталон или поясняющий контекст.");
        builder.AppendLine("- Для такого уточнения дай готовые варианты ответа: часть основной задачи; пример/эталон; поясняющий материал; не учитывать. Не проси пользователя формулировать эти роли вручную.");
        builder.AppendLine();
        builder.AppendLine("История выбора:");
        foreach (var answer in answers)
        {
            builder.AppendLine($"{answer.StepNumber}. [{answer.DecisionDimension}] {answer.Question} -> {answer.OptionTitle}");
        }

        builder.AppendLine();
        builder.AppendLine("Текущий capability profile — источник истины, не пересказывай его заново:");
        builder.AppendLine(JsonSerializer.Serialize(capabilityProfile));
        builder.AppendLine("Уже решённые измерения:");
        builder.AppendLine(capabilityProfile.ResolvedDimensions.Count == 0
            ? "- нет"
            : $"- {string.Join(", ", capabilityProfile.ResolvedDimensions)}");
        if (capabilityProfile.ResolvedDimensions.Count > 0)
        {
            builder.AppendLine($"FORBIDDEN_DECISION_DIMENSIONS без revisitReason: {string.Join(", ", capabilityProfile.ResolvedDimensions)}.");
        }
        if (capabilityProfile.GetStatus(ChoiceDecisionDimensions.TaskType) == ChoiceDimensionStatuses.Unknown)
        {
            builder.AppendLine("NEXT_REQUIRED_DIMENSION: task_type. Следующий question_step обязан иметь decisionDimension=task_type.");
        }
        builder.AppendLine();
        builder.AppendLine("Обязательная самопроверка:");
        builder.AppendLine("- Ответ на новый вопрос должен структурированно менять профиль исполнителя.");
        builder.AppendLine("- Если task_type неизвестен, сначала спроси тип интеллектуальной операции и не предполагай его по области.");
        builder.AppendLine("- Не уточняй предметную тему ради будущего решения.");
        builder.AppendLine("- Если широкая специализация уже ясна, переходи к операции, данным, инструментам или ограничениям.");
        builder.AppendLine("- Не повторяй resolved/not_applicable измерение без revisitReason.");
        builder.AppendLine("- Если профиль достаточен, немедленно верни final_task_card, даже если бюджет ещё большой.");
        builder.AppendLine("- При финале программа отдельно передаст TRUSTED_EXECUTOR_CANDIDATE_POOL; выбирай только его идентификаторы.");
        builder.AppendLine("- Не вызывай инструменты поиска моделей самостоятельно и не переписывай их технические факты.");
        builder.AppendLine("- Сравни пригодность установленного и загружаемого варианта для capability profile; загрузка не имеет автоматического приоритета.");
        builder.AppendLine("- Prompt исполнителю должен поручить ему недостающие предметные уточнения.");
        if (mustReturnFinal)
        {
            builder.AppendLine("- Новый question_step запрещён. Верни финал из известных данных и обозначь пробелы.");
        }

        builder.AppendLine();
        builder.AppendLine("Профиль пользователя и ПК:");
        builder.AppendLine(JsonSerializer.Serialize(userContext));
        builder.AppendLine();
        builder.AppendLine("Доступные возможности:");
        builder.AppendLine(inventorySummary);
        builder.AppendLine();
        if (!mustReturnFinal
            && capabilityProfile.GetStatus(ChoiceDecisionDimensions.TaskType) == ChoiceDimensionStatuses.Unknown)
        {
            builder.AppendLine("Последняя обязательная проверка: верни question_step только с decisionDimension=task_type.");
        }
        else if (!mustReturnFinal && capabilityProfile.ResolvedDimensions.Count > 0)
        {
            builder.AppendLine($"Последняя обязательная проверка: не используй decisionDimension из списка [{string.Join(", ", capabilityProfile.ResolvedDimensions)}] без непустого revisitReason.");
        }
        builder.AppendLine("Верни следующий JSON-шаг.");
        return builder.ToString();
    }
}
