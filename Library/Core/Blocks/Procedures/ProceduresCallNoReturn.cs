

using BlocklyNet.Core.Model;

namespace BlocklyNet.Core.Blocks.Procedures;

/// <summary>
/// 
/// </summary>
public class ProceduresCallNoReturn : Block
{
  /// <inheritdoc/>
  protected override async Task<object?> EvaluateAsync(Context context)
  {
    // todo: add guard for missing name

    var name = Mutations.GetValue("name");

    if (!context.Functions.TryGetValue(name, out var statement))
      throw new MissingMethodException($"Method ${name} not defined");

    var funcContext = new Context(context);

    var counter = 0;

    foreach (var mutation in Mutations.Where(x => x.Domain == "arg" && x.Name == "name"))
    {
      context.Cancellation.ThrowIfCancellationRequested();

      var value = await Values.EvaluateAsync($"ARG{counter}", context);

      funcContext.Variables.Add(mutation.Value, value!);

      counter++;
    }

    context.Cancellation.ThrowIfCancellationRequested();

    try
    {
      await statement.EvaluateAsync(funcContext);
    }
    catch (ReturnProcedureIfException)
    {
    }

    return await base.EvaluateAsync(context);
  }
}