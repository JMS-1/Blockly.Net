

using BlocklyNet.Core.Model;

namespace BlocklyNet.Core.Blocks.Math;

/// <summary>
/// 
/// </summary>
public class MathChange : Block
{
  /// <inheritdoc/>
  protected override async Task<object?> EvaluateAsync(Context context)
  {
    var variableName = Fields["VAR"];
    var delta = await Values.EvaluateDoubleAsync("DELTA", context);

    if (!context.Variables.ContainsKey(variableName))
      throw new ApplicationException($"variable {variableName} not declared");

    var value = (double)context.Variables[variableName]!;
    value += delta;
    context.Variables[variableName] = value;

    return await base.EvaluateAsync(context);
  }
}