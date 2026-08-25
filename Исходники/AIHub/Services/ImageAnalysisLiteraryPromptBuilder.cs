using System.Text;
using AIHub.Models;

namespace AIHub.Services;

public static class ImageAnalysisLiteraryPromptBuilder
{
    public static string BuildVisionPrompt(ImageAnalysisLiterarySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return "Describe what you see in this image.";
    }

    public static string BuildInitialSystemPrompt(ImageAnalysisLiterarySettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return """
            Ты — опытный литературный редактор издательства. Твоя работа — превращать сырой черновик в цельный, грамотный и готовый к публикации текст, сохраняя его содержательную основу и свободно перестраивая форму.

            Текущая задача — подготовить описание изображения. Главная цель — помочь читателю ясно представить основные объекты, их расположение и взаимосвязи, обстановку и общее впечатление от изображения. Надписи внутри изображения упоминай только тогда, когда они важны для понимания содержания; не превращай описание в расшифровку всего видимого текста.

            Верни строго один JSON-объект без Markdown и без дополнительного текста:
            {"title":"заголовок или null","paragraphs":["содержательный абзац"],"review_items":["3–6 кратких главных деталей"],"uncertainties":["0–2 существенные неопределённости"]}
            """;
    }

    public static string BuildInitialUserPrompt(
        ImageAnalysisLiterarySettings settings,
        string visualReport)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(visualReport);

        var builder = new StringBuilder();
        builder.AppendLine("Подготовь описание изображения.");
        builder.AppendLine($"Язык результата: {DescribeOutputLanguage(settings.LanguageCode)}.");
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
        builder.AppendLine("Черновик:");
        builder.AppendLine(visualReport.Trim());
        builder.AppendLine();
        builder.AppendLine("Верни только JSON по системному контракту. Все текстовые поля должны быть написаны на выбранном языке результата.");
        return builder.ToString().Trim();
    }

    public static string BuildRevisionSystemPrompt() => """
        Ты — опытный литературный редактор издательства. Твоя работа — превращать черновики в цельные, грамотные и готовые к публикации тексты, сохраняя содержательную основу и свободно перестраивая форму.

        Текущая задача — подготовить новую редакцию описания изображения по просьбе автора. Сохрани достоверные объекты, их расположение и взаимосвязи, обстановку и общее впечатление. Не превращай описание в расшифровку надписей внутри изображения.

        Верни только новую полную версию текста без служебных пояснений, JSON и перечня внесённых изменений.
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
            - язык результата: {DescribeOutputLanguage(settings.LanguageCode)};
            - точность: {DescribeAccuracy(settings.Accuracy)};
            - стиль: {DescribeStyle(settings.Style)};
            - объём: {DescribeLength(settings.Length)};
            - форма: {DescribeForm(settings.Form)}.

            Исходный черновик:
            {visualReport.Trim()}

            Текущая версия:
            {currentText.Trim()}

            Верни новую полную версию на выбранном языке результата, а не отдельный фрагмент и не список правок.
            """.Trim();
    }

    private static string DescribeAccuracy(string value) => value switch
    {
        ImageAnalysisAccuracyModes.Strict => "строго сохранять содержательную основу черновика и не добавлять неподтверждённые события или объекты",
        ImageAnalysisAccuracyModes.Free => "допустима осторожная художественная интерпретация без противоречия содержательной основе черновика",
        _ => "сохранять содержательную основу черновика, используя выразительный художественный язык"
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
        ImageAnalysisTextLengths.Detailed => "подробное описание из 4–7 содержательных абзацев; не увеличивать объём пустыми повторениями",
        _ => "2–4 содержательных абзаца"
    };

    private static string DescribeForm(string value) => value switch
    {
        ImageAnalysisTextForms.WithTitle => "короткий выразительный заголовок в поле title и связный основной текст в paragraphs",
        _ => "без отдельного заголовка: поле title должно быть null"
    };

    private static string DescribeOutputLanguage(string? languageCode) =>
        languageCode?.StartsWith("en", StringComparison.OrdinalIgnoreCase) == true
            ? "English"
            : "русский";
}
