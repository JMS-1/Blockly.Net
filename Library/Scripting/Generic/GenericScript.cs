using System.Text.Json;
using BlocklyNet.Extensions.Builder;
using BlocklyNet.Scripting.Definition;
using BlocklyNet.Scripting.Engine;
using BlocklyNet.Scripting.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BlocklyNet.Scripting.Generic;

/// <summary>
/// Algorithm to execute a generic script.
/// </summary>
/// <param name="request">All parameters for the script.</param>
/// <param name="engine">Script engine executing the script.</param>
/// <param name="options">Additional configuration of the script run-time.</param>
public class GenericScript<TLogType, TModifierType>(StartGenericScript request, IScriptSite engine, StartScriptOptions? options)
    : Script<StartGenericScript, GenericResult, StartScriptOptions, TLogType, TModifierType>(request, engine, options), IGenericScript
    where TLogType : ScriptLoggingResult, new()
    where TModifierType : ScriptLogModifier
{
    IStartGenericScript IGenericScript.Request => Request;

    /// <inheritdoc/>
    protected override Task OnExecuteAsync() => ExecuteAsync(this, null, LookupBuiltInType);

    /// <inheritdoc/>
    protected override Task OnResetAsync() => Task.CompletedTask;

    /// <summary>
    /// 
    /// </summary>
    internal static Dictionary<string, Type> BuiltInTypesForUnitTest = [];

    /// <summary>
    /// 
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    protected virtual Type? LookupBuiltInType(string typeName) => BuiltInTypesForUnitTest.TryGetValue(typeName, out var type) ? type : null;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="script"></param>
    /// <param name="afterPresets"></param>
    /// <param name="getBuiltInType"></param>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <typeparam name="TOptions"></typeparam>
    /// <returns></returns>
    public static async Task ExecuteAsync<TRequest, TResult, TOptions>(Script<TRequest, TResult, TOptions, TLogType, TModifierType> script, Action<IScriptDefinition>? afterPresets = null, Func<string, Type?>? getBuiltInType = null)
        where TRequest : StartScript, IStartGenericScript
        where TResult : GenericResult, new()
        where TOptions : StartScriptOptions
    {
        /* Translate parameters. */
        var presets = script.Request.Presets.ToDictionary(p => p.Key, p => (p.Value is JsonElement json) ? json.ToJsonScalar() : p.Value);

        /* Find the script. */
        var di = script.Engine.ServiceProvider;
        var def = await di.GetRequiredService<IScriptDefinitionStorage>().GetAsync(script.Request.ScriptId) ?? throw new ArgumentException("Script not found.");

        /* Prepare for logging. */
        script.Request.Presets = presets.Select(d => new GenericScriptPreset { Key = d.Key, Value = d.Value }).ToList();

        afterPresets?.Invoke(def);

        /* Validate presets. */
        var models = di.GetRequiredService<IScriptModels>();

        foreach (var param in def.Parameters)
        {
            /* Check requirement. */
            presets.TryGetValue(param.Name, out var preset);

            if (param.Required == true && preset == null)
                throw new ArgumentException($"Required parameter {param.Name} not set.");

            /* See if conversion is possible. */
            if (preset is string enumString)
            {
                if (!models.Enums.TryGetValue(param.Type, out var enumInfo)) continue;

                presets[param.Name] = Enum.Parse(enumInfo.Type, enumString);
            }
            else if (preset is JsonElement json)
                if (json.ValueKind == JsonValueKind.Array)
                {
                    /* Must be some array type. */
                    if (!param.Type.EndsWith("[]") && !param.Type.EndsWith("{}")) continue;

                    /* Check for converter. */
                    var singleType = param.Type[..^2];
                    var scalarType = models.Models.TryGetValue(singleType, out var modelInfo) ? modelInfo.Type : getBuiltInType?.Invoke(singleType);

                    if (scalarType == null) continue;

                    /* Do convert if we get raw json. */
                    presets[param.Name] = JsonSerializer.Deserialize(json.ToString(), scalarType.MakeArrayType(), JsonUtils.JsonSettings);
                }
                else
                {
                    /* Check for converter. */
                    if (!models.Models.TryGetValue(param.Type, out var modelInfo)) continue;

                    /* Do convert if we get raw json. */
                    presets[param.Name] = JsonSerializer.Deserialize(json.ToString(), modelInfo.Type, JsonUtils.JsonSettings);
                }
        }

        /* Create log entry. */
        await script.WriteToLogAsync();

        /* Execute the script. */
        var result = await script.Engine.EvaluateAsync(def.Code, presets);

        /* Create result information - with some redundant information copied from the request. */
        script.SetResult(new TResult { Result = result, ResultType = script.Request.ResultType, ScriptId = script.Request.ScriptId });
    }
}
