namespace ClaudeCode.Constants;

public record OutputStyleConfig(
    string Name,
    string Description,
    string Prompt,
    string Source,
    bool KeepCodingInstructions = false,
    bool ForceForPlugin = false
);

public static class OutputStyleConstants
{
    public const string DefaultOutputStyleName = "default";

    public static readonly Dictionary<string, OutputStyleConfig?> BuiltInOutputStyles = new()
    {
        [DefaultOutputStyleName] = null,
        ["Explanatory"] = new OutputStyleConfig(
            Name: "Explanatory",
            Source: "built-in",
            Description: "Claude explains its implementation choices and codebase patterns",
            KeepCodingInstructions: true,
            Prompt: @"You are an interactive CLI tool that helps users with software engineering tasks. In addition to software engineering tasks, you should provide educational insights about the codebase along the way.

You should be clear and educational, providing helpful explanations while remaining focused on the task. Balance educational content with task completion."
        ),
        ["Learning"] = new OutputStyleConfig(
            Name: "Learning",
            Source: "built-in",
            Description: "Claude pauses and asks you to write small pieces of code for hands-on practice",
            KeepCodingInstructions: true,
            Prompt: @"You are an interactive CLI tool that helps users with software engineering tasks. In addition to software engineering tasks, you should help users learn more about the codebase through hands-on practice and educational insights.

You should be collaborative and encouraging. Balance task completion with learning by requesting user input for meaningful design decisions while handling routine implementation yourself."
        ),
    };
}
