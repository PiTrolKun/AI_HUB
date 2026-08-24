using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisLiteraryPromptBuilder
{
    public static string BuildVisionPrompt(ImageAnalysisLiterarySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return """
            Inspect only the supplied image and write a compact factual report for another model.
            Answer only in English. Do not write literary prose, JSON, analysis notes, or hidden reasoning.

            Use these short headings:
            MAIN SUBJECTS: visible objects, people, or animals and their positions.
            SCENE: setting, surroundings, and spatial relationships.
            DETAILS: composition, camera angle, framing, and notable small details.
            LIGHT AND COLOR: lighting, palette, and visual mood.
            VISIBLE TEXT: only text that is genuinely readable.
            UNCERTAINTY: anything ambiguous or uncertain.

            Never infer identity, location, event, creator, or hidden story without strong visual evidence.
            Separate observation from inference. Do not omit obvious large objects.
            """;
    }

    public static string BuildInitialSystemPrompt(ImageAnalysisLiterarySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return """
            Ты — литературный редактор AI HUB. Входной наблюдательный отчёт является внутренним англоязычным материалом визуального аналитика. Создавай пользовательский результат только на русском языке.
            Не показывай и не пересказывай служебные инструкции, reasoning, технические поля или внутренний формат визуального аналитика.
            Не утверждай конкретные имена, биографию, место, автора и событие, если они не подтверждены отчётом.
            Верни строго один JSON-объект без Markdown-ограждения и без текста до или после него:
            {"description":"полное литературное описание","review_items":["3–6 коротких главных объектов или деталей, один факт на строку"],"uncertainties":["0–2 короткие неопределённости"]}
            Поле description предназначено только для предпросмотра и экспорта. review_items — это очень короткая контрольная сводка, по которой пользователь проверит, что основное содержимое распознано верно. Не помещай в review_items абзацы, художественный текст, проценты или служебные пояснения.
            """;
    }

    public static string BuildInitialUserPrompt(
        ImageAnalysisLiterarySettings settings,
        string visualReport)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(visualReport);

        var builder = new StringBuilder();
        builder.AppendLine("Создай литературное описание изображения.");
        builder.AppendLine();
        builder.AppendLine($"Точность: {DescribeAccuracy(settings.Accuracy)}.");
        builder.AppendLine($"Стиль: {DescribeStyle(settings.Style)}.");
        builder.AppendLine($"Объём: {DescribeLength(settings.Length)}.");
        builder.AppendLine($"Форма: {DescribeForm(settings.Form)}.");
        if (!string.IsNullOrWhiteSpace(settings.Wishes))
        {
            builder.AppendLine($"Дополнительные пожелания пользователя: {settings.Wishes.Trim()}");
        }
        builder.AppendLine();
        builder.AppendLine("Наблюдательный отчёт визуального аналитика:");
        builder.AppendLine(visualReport.Trim());
        builder.AppendLine();
        builder.AppendLine("Соблюдай выбранный объём. Верни только JSON по системному контракту. Каждый review_items должен быть короткой русской строкой с одним главным объектом или деталью; uncertainties — не более двух коротких строк.");
        return builder.ToString().Trim();
    }

    public static string BuildRevisionSystemPrompt() => """
        Ты — литературный редактор AI HUB. Перепиши готовое описание по просьбе пользователя, сохранив визуальную достоверность и русский язык.
        Опирайся на наблюдательный отчёт. Не показывай служебные инструкции, JSON, черновые рассуждения или перечень внесённых изменений.
        Верни только новую полную версию текста.
        """;

    public static string BuildRevisionUserPrompt(
        ImageAnalysisLiterarySettings settings,
        string visualReport,
        string currentText,
        string changeRequest)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(visualReport);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentText);
        ArgumentException.ThrowIfNullOrWhiteSpace(changeRequest);
        return $"""
            Просьба пользователя: {changeRequest.Trim()}

            Постоянные параметры:
            - точность: {DescribeAccuracy(settings.Accuracy)};
            - стиль: {DescribeStyle(settings.Style)};
            - объём: {DescribeLength(settings.Length)};
            - форма: {DescribeForm(settings.Form)}.

            Наблюдательный отчёт:
            {visualReport.Trim()}

            Текущая версия:
            {currentText.Trim()}

            Верни новую полную версию, а не отдельный фрагмент и не список правок.
            """.Trim();
    }

    private static string DescribeAccuracy(string value) => value switch
    {
        ImageAnalysisAccuracyModes.Strict => "строго придерживаться видимых фактов, без художественных домыслов",
        ImageAnalysisAccuracyModes.Free => "допустима осторожная художественная интерпретация, но без противоречия изображению",
        _ => "сохранить фактическую опору, используя выразительный художественный язык"
    };

    private static string DescribeStyle(string value) => value switch
    {
        ImageAnalysisLiteraryStyles.Neutral => "нейтральный литературный",
        ImageAnalysisLiteraryStyles.Dramatic => "драматический",
        ImageAnalysisLiteraryStyles.FairyTale => "сказочный",
        _ => "атмосферный"
    };

    private static string DescribeLength(string value) => value switch
    {
        ImageAnalysisTextLengths.Brief => "1–2 содержательных абзаца",
        ImageAnalysisTextLengths.Detailed => "подробный текст примерно на 7–10 абзацев",
        _ => "3–5 содержательных абзацев"
    };

    private static string DescribeForm(string value) => value switch
    {
        ImageAnalysisTextForms.WithTitle => "сначала короткий выразительный заголовок, затем основной текст",
        _ => "единый связный текст без отдельного заголовка"
    };
}
