using JetBrains.Annotations;

namespace MoneySpot6.WebApp.Features.Mcp;

/// <summary>
/// Marks a controller action as an MCP tool. The <see cref="McpApiBridge"/> discovers every action
/// carrying this attribute and exposes it as a tool that calls the existing endpoint in-process —
/// so there is no per-endpoint tool code, only this opt-in marker plus a description.
/// </summary>
[MeansImplicitUse]
[AttributeUsage(AttributeTargets.Method)]
public sealed class McpToolAttribute : Attribute
{
    /// <summary>Tool name exposed to the MCP client. Defaults to <c>{Controller}_{Action}</c>.</summary>
    public string? Name { get; init; }

    /// <summary>Description shown to the model. Keep it action-oriented so the model knows when to use it.</summary>
    public string? Description { get; init; }
}
