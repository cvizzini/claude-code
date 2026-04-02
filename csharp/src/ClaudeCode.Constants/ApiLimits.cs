namespace ClaudeCode.Constants;

public static class ApiLimits
{
    // Image limits
    public const int ApiImageMaxBase64Size = 5 * 1024 * 1024; // 5 MB base64
    public const int ImageTargetRawSize = (int)(ApiImageMaxBase64Size * 3.0 / 4); // ~3.75 MB
    public const int ImageMaxWidth = 2000;
    public const int ImageMaxHeight = 2000;

    // PDF limits
    public const int PdfTargetRawSize = 20 * 1024 * 1024; // 20 MB
    public const int ApiPdfMaxPages = 100;
    public const int PdfExtractSizeThreshold = 3 * 1024 * 1024; // 3 MB
    public const int PdfMaxExtractSize = 100 * 1024 * 1024; // 100 MB
    public const int PdfMaxPagesPerRead = 20;
    public const int PdfAtMentionInlineThreshold = 10;

    // General limits
    public const int ApiMaxMediaPerRequest = 100;
    public const int DefaultMaxTokens = 8096;
    public const int MaxContextTokens = 200000;
    public const int DefaultMaxFileSize = 10 * 1024 * 1024; // 10 MB
    public const int MaxToolResultLength = 100000;
    public const int MaxConversationHistory = 100;
}
