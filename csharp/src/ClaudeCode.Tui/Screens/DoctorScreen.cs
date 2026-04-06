using ClaudeCode.Constants;
using ClaudeCode.Services.Config;
using ClaudeCode.Tui.Theme;
using Terminal.Gui;

namespace ClaudeCode.Tui.Screens;

/// <summary>
/// Diagnostics/health-check view that mirrors the <c>doctor</c> CLI sub-command but
/// renders inside the Terminal.Gui TUI as a modal dialog.
/// </summary>
internal sealed class DoctorScreen : Toplevel
{
    public DoctorScreen(IConfigService configService)
    {
        ColorScheme = AppTheme.DialogScheme;

        var config     = configService.LoadConfig();
        var copilotToken = Environment.GetEnvironmentVariable(ProductConstants.CopilotApiKeyEnvVar)
                          ?? Environment.GetEnvironmentVariable(ProductConstants.CopilotFallbackTokenEnvVar)
                          ?? config.OAuthToken
                          ?? config.ApiKey;

        var configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ProductConstants.ConfigDir,
            ProductConstants.GlobalConfigFile);

        var credentialSet = !string.IsNullOrEmpty(copilotToken);
        var looksLikePat  = LooksLikePat(copilotToken ?? string.Empty);

        var credentialDetail = credentialSet
            ? looksLikePat
                ? "PAT detected – GitHub OAuth sign-in required"
                : "GitHub credential available"
            : $"Run with --provider {ProductConstants.CopilotProvider} to sign in";

        var checks = new (string Name, bool Passed, string Detail)[]
        {
            ("Provider",      true,
                ProductConstants.CopilotProvider),
            ("Credential",    credentialSet && !looksLikePat,
                credentialDetail),
            ("Config file",   File.Exists(configPath),
                configPath),
            (".NET Runtime",  true,
                System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription),
            ("Model",         true,
                config.Model),
        };

        var dialog = new Dialog("Claude Code Doctor", 62, checks.Length + 9)
        {
            ColorScheme = AppTheme.DialogScheme,
        };

        var y = 1;
        foreach (var (name, passed, detail) in checks)
        {
            var icon      = passed ? "✓" : "✗";
            var iconColor = passed ? AppTheme.AssistantLabel : AppTheme.ErrorText;
            var iconLabel = new Label(icon)
            {
                X           = 2,
                Y           = y,
                ColorScheme = new ColorScheme
                {
                    Normal    = iconColor,
                    Focus     = iconColor,
                    HotNormal = iconColor,
                    HotFocus  = iconColor,
                },
            };

            var nameLabel = new Label($" {name}:")
            {
                X           = 3,
                Y           = y,
                Width       = 18,
                ColorScheme = AppTheme.DialogScheme,
            };

            var detailLabel = new Label(TruncateDetail(detail, 35))
            {
                X           = 21,
                Y           = y,
                ColorScheme = AppTheme.DialogScheme,
            };

            dialog.Add(iconLabel, nameLabel, detailLabel);
            y++;
        }

        var ok = new Button("OK", is_default: true) { X = Pos.Center() };
        ok.Clicked += () => Application.RequestStop();
        dialog.AddButton(ok);

        Add(dialog);
    }

    private static bool LooksLikePat(string token)
        => token.StartsWith("github_pat_", StringComparison.OrdinalIgnoreCase)
           || token.StartsWith("ghp_", StringComparison.OrdinalIgnoreCase);

    private static string TruncateDetail(string s, int max)
        => s.Length <= max ? s : "…" + s[^(max - 1)..];
}
