

using BlocklyNet.Core.Model;

namespace BlocklyNet.Core.Blocks.Math;

/// <summary>
/// 
/// </summary>
public class MathModulo : Block
{
  /// <inheritdoc/>
  protected override async Task<object?> EvaluateAsync(Context context)
  {
    var dividend = await Values.EvaluateDoubleAsync("DIVIDEND", context);
    var divisor = await Values.EvaluateDoubleAsync("DIVISOR", context);

    return dividend % divisor;
  }
}