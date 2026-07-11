namespace AIHub.Models;

public sealed record CoreSpeechSegment(string Id, string Text);

public sealed record CoreSpeechRequest(
    IReadOnlyList<CoreSpeechSegment> Segments,
    string LanguageCode,
    CoreVoiceSettings Settings,
    string Source);

public sealed record CoreSpeechProgress(
    IReadOnlyDictionary<string, int> VisibleCharacters,
    bool IsComplete,
    bool UsesNativeWordEvents);

public sealed record CoreSpeechComposition(
    string Text,
    IReadOnlyList<CoreSpeechSegmentSpan> Segments);

public sealed record CoreSpeechSegmentSpan(
    string Id,
    string Text,
    int Start,
    int Length);

public sealed record SpeechRevealCue(int TimeMilliseconds, int VisibleCharacters);

public sealed record CoreSpeechPresentationResult(
    bool Completed,
    bool Skipped,
    bool UsesNativeWordEvents,
    string? ErrorCode = null);
