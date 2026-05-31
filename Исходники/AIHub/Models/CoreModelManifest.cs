namespace AIHub.Models;

public sealed class CoreModelManifest
{
    public string Id { get; set; } = "qwen3-8b-core";

    public string Name { get; set; } = "Qwen3 8B";

    public string Role { get; set; } = "core";

    public string Format { get; set; } = "GGUF";

    public string Quantization { get; set; } = "Q4_K_M";

    public string File { get; set; } = "Qwen3-8B-Q4_K_M.gguf";

    public string Sha256 { get; set; } = "d98cdcbd03e17ce47681435b5150e34c1417f50b5c0019dd560e4882c5745785";

    public string Source { get; set; } = "https://huggingface.co/Qwen/Qwen3-8B-GGUF/resolve/main/Qwen3-8B-Q4_K_M.gguf";

    public string SourceRepository { get; set; } = "Qwen/Qwen3-8B-GGUF";

    public string SourceCommit { get; set; } = "7c41481f57cb95916b40956ab2f0b139b296d974";

    public string License { get; set; } = "apache-2.0";

    public string Status { get; set; } = "missing";

    public long DownloadedBytes { get; set; }

    public long TotalBytes { get; set; } = 5_027_783_488;

    public DateTimeOffset? VerifiedAt { get; set; }
}
