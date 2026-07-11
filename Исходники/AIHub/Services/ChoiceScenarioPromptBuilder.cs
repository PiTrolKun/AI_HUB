using System.Text;
using System.Text.Json;
using AIHub.Models;

namespace AIHub.Services;

public static class ChoiceScenarioPromptBuilder
{
    public static string BuildSystemPrompt() => """
        Ты маршрутизатор AI HUB в режиме неопределенности.
        Ты не решаешь исходную задачу и не проводишь предметную консультацию.
        Твоя задача — построить профиль требуемого AI-исполнителя, подобрать модель и инструменты, затем подготовить стартовый prompt для рабочей модели.

        Главный критерий каждого вопроса:
        Может ли ответ изменить класс или размер модели, силу рассуждений, контекстное окно, специализацию, backend, инструменты, режим выполнения, нагрузку, скорость, приватность или качество языка?
        Если нет — вопрос запрещён.

        Жёсткие правила:
        - Верни только JSON без markdown и пояснений.
        - Максимум 6 коротких и понятных вариантов.
        - Не решай задачу, не давай предметный вывод и не выдумывай детали.
        - Не спрашивай точный аспект темы, критерии решения или личную мотивацию, если это должен выяснить будущий исполнитель.
        - Одно широкое уточнение специализации допустимо, если оно меняет класс модели. Последовательное предметное углубление запрещено.
        - Не выбирай модель только по теме: сначала определи интеллектуальную операцию, данные, способности и ограничения.
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
        - recommendedExecutor — AI-модель, а web/HF/inventory перечисляются отдельно как инструменты.
        - Перед final_task_card обязательно вызови inventory, затем model_catalog_search. Ничего не скачивай.
        - model_catalog_search — отдельный локальный инструмент, а не часть твоих знаний. Он даёт кандидатов, но не выбирает победителя за тебя.
        - Если локальный каталог отсутствует или не дал кандидатов, обязательно вызови hf_find_model. При наличии кандидатов живой поиск необязателен, но допустим для актуальной проверки.
        - Если поиск вернул кандидатов, выбери точный подтверждённый repo/model. Не называй найденную модель установленной без inventory.
        - Учитывай hardware evidence каталога. Кандидат с hardware status not_fit запрещён; неизвестную совместимость нельзя выдавать как проверенную.
        - `baseModels` и model metadata определяют родство надёжнее похожего имени репозитория.
        - needsWeb и requiredTools обязаны быть согласованы: needsWeb=false запрещает web-инструменты; no_external_data/offline_only/local_only также запрещают web.
        - AI HUB не назначает семейство или издателя. Не выбирай семейство текущего ядра по умолчанию.
        - То же семейство допустимо только при значительно более новом поколении; больший размер той же генерации недостаточен.
        - Если workloadMode не light, исполнитель обязан быть строго мощнее 8B. Текущее 8B-ядро — только маршрутизатор и поисковик.
        - current_core допустим только как core_fallback в light при физически недоступном поиске.
        - Prompt рабочей модели передаёт известное и требует самой задать недостающие предметные вопросы.

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
            "needsWeb": true,
            "requiredTools": ["web_research"],
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
            "executorRole": "general_worker",
            "executorCapabilityClass": "above_8b",
            "recommendedExecutor": "точный repo/model",
            "executorStatus": "not_installed",
            "executorReason": "Почему возможности модели соответствуют профилю",
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
        ChoiceCapabilityProfile capabilityProfile)
    {
        var builder = new StringBuilder();
        builder.AppendLine(mustReturnFinal
            ? "Бюджет исчерпан. Следующий ответ обязан быть final_task_card."
            : requestFinal
                ? "Пользователь нажал: Перейти к финалу."
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
        builder.AppendLine("- Перед финалом проверь inventory и отдельный инструмент model_catalog_search; скачивание запрещено.");
        builder.AppendLine("- Если локальный каталог не дал кандидатов, используй hf_find_model. Каталог предлагает набор, а окончательный выбор делаешь ты.");
        builder.AppendLine("- Проверь hardware evidence и lineage кандидата; не возвращай заведомо не помещающуюся на ПК модель.");
        builder.AppendLine("- Согласуй needsWeb, requiredTools и запрет внешних данных без противоречий.");
        builder.AppendLine("- В balanced/extreme current_core и модели 8B или слабее запрещены как исполнитель.");
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
