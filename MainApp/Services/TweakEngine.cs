using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.Services;

public sealed class TweakRunResult
{
    public int Applied { get; set; }
    public int Failed { get; set; }
    public List<string> Errors { get; } = new();
}

public sealed class TweakEngine
{
    private readonly ConfigService _config;
    private readonly RegistryService _registry;
    private readonly ServiceManager _services;
    private readonly FeatureService _features;
    private readonly UndoStore _undo;
    private readonly LogService _log;

    public TweakEngine(
        ConfigService config,
        RegistryService registry,
        ServiceManager services,
        FeatureService features,
        UndoStore undo,
        LogService log)
    {
        _config = config;
        _registry = registry;
        _services = services;
        _features = features;
        _undo = undo;
        _log = log;
    }

    public IReadOnlyList<string> AppliedKeys => _undo.All.Select(record => record.Key).ToList();

    public async Task<TweakRunResult> ApplyAsync(
        IReadOnlyList<TweakDefinition> tweaks,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var result = new TweakRunResult();
        for (var index = 0; index < tweaks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tweak = tweaks[index];
            output?.Invoke($"[{index + 1}/{tweaks.Count}] Applying {tweak.Content}");
            _log.Info($"Applying tweak {tweak.Key}: {tweak.Content}");

            var record = new AppliedTweakRecord
            {
                Key = tweak.Key,
                Name = tweak.Content,
                AppliedByDesktop = true
            };

            try
            {
                await ApplyDefinitionAsync(tweak, record, output, cancellationToken);
                _undo.Upsert(record);
                result.Applied++;
                _log.Info($"Applied tweak {tweak.Key}.");
            }
            catch (OperationCanceledException)
            {
                _undo.Upsert(record);
                throw;
            }
            catch (Exception exception)
            {
                _undo.Upsert(record);
                result.Failed++;
                result.Errors.Add($"{tweak.Content}: {exception.Message}");
                _log.Error($"Tweak {tweak.Key} failed: {exception.Message}");
            }
        }

        return result;
    }

    public async Task<TweakRunResult> UndoAsync(
        IReadOnlyList<TweakDefinition> tweaks,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var result = new TweakRunResult();
        for (var index = 0; index < tweaks.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tweak = tweaks[index];
            output?.Invoke($"[{index + 1}/{tweaks.Count}] Undoing {tweak.Content}");
            _log.Info($"Undoing tweak {tweak.Key}: {tweak.Content}");

            try
            {
                await UndoDefinitionAsync(tweak, output, cancellationToken);
                result.Applied++;
                _log.Info($"Undid tweak {tweak.Key}.");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                result.Failed++;
                result.Errors.Add($"{tweak.Content}: {exception.Message}");
                _log.Error($"Undo of {tweak.Key} failed: {exception.Message}");
            }
        }

        return result;
    }

    public async Task<TweakRunResult> UndoAllAsync(
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var records = _undo.All.ToList();
        var result = new TweakRunResult();

        for (var index = 0; index < records.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var record = records[index];
            output?.Invoke($"[{index + 1}/{records.Count}] Undoing {record.Name}");
            try
            {
                _config.Tweaks.TryGetValue(record.Key, out var tweak);
                if (tweak is null)
                {
                    _config.Features.TryGetValue(record.Key, out tweak);
                }

                if (tweak is not null)
                {
                    await UndoDefinitionAsync(tweak, output, cancellationToken);
                }
                else
                {
                    await RestoreRecordAsync(record, null, output, cancellationToken);
                }

                result.Applied++;
            }
            catch (Exception exception)
            {
                result.Failed++;
                result.Errors.Add($"{record.Name}: {exception.Message}");
                _log.Error($"Undo all failed for {record.Key}: {exception.Message}");
            }
        }

        return result;
    }

    public Task<TweakRunResult> ApplyPresetAsync(
        string preset,
        Action<string>? output,
        CancellationToken cancellationToken = default)
    {
        var tweaks = _config.GetPresetTweaks(preset);
        _log.Info($"Applying the {preset} preset ({tweaks.Count} tweaks).");
        return ApplyAsync(tweaks, output, cancellationToken);
    }

    private async Task ApplyDefinitionAsync(
        TweakDefinition tweak,
        AppliedTweakRecord record,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        foreach (var service in tweak.Service ?? new List<ServiceChange>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var current = await _services.QueryStartupTypeAsync(service.Name, cancellationToken);
            record.Service.Add(new ServiceSnapshot
            {
                Name = service.Name,
                StartupType = current
            });

            if (current is null)
            {
                _log.Warn($"Service {service.Name} was not found; skipping.");
                continue;
            }

            await _services.SetStartupTypeAsync(service.Name, service.StartupType, cancellationToken);
        }

        foreach (var change in tweak.Registry ?? new List<RegistryChange>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            _registry.Apply(change, record.Registry, cancellationToken);
        }

        foreach (var feature in tweak.Feature ?? new List<string>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var state = await _features.GetStateAsync(feature, cancellationToken);
            record.Features.Add(new FeatureSnapshot { Name = feature, State = state });
            output?.Invoke($"Enabling Windows feature {feature}...");
            await _features.SetEnabledAsync(new[] { feature }, true, output, cancellationToken);
        }

        foreach (var script in tweak.InvokeScript ?? new List<string>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(script))
            {
                continue;
            }

            output?.Invoke($"Running the {tweak.Content} actions...");
            var result = await PowerShellRunner.RunScriptAsync(PowerShellCompat.Wrap(script), output, cancellationToken);
            if (result.ExitCode != 0)
            {
                _log.Warn($"The action script for {tweak.Key} exited with code {result.ExitCode}.");
            }
        }

        if (tweak.Appx is { Count: > 0 })
        {
            _log.Warn($"{tweak.Key} removes AppX packages. That workflow is not available in the desktop app yet; use the PowerShell app.");
        }
    }

    private async Task UndoDefinitionAsync(
        TweakDefinition tweak,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        var record = _undo.Find(tweak.Key);
        if (record is not null)
        {
            await RestoreRecordAsync(record, tweak, output, cancellationToken);
            return;
        }

        _log.Warn($"No undo snapshot for {tweak.Key}; using the original values stored in the config.");
        foreach (var service in tweak.Service ?? new List<ServiceChange>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(service.OriginalType))
            {
                await _services.SetStartupTypeAsync(service.Name, service.OriginalType, cancellationToken);
            }
        }

        foreach (var change in tweak.Registry ?? new List<RegistryChange>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            _registry.RestoreOriginal(change);
        }

        foreach (var feature in tweak.Feature ?? new List<string>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _features.SetEnabledAsync(new[] { feature }, false, output, cancellationToken);
        }

        foreach (var script in tweak.UndoScript ?? new List<string>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(script))
            {
                continue;
            }

            var result = await PowerShellRunner.RunScriptAsync(PowerShellCompat.Wrap(script), output, cancellationToken);
            if (result.ExitCode != 0)
            {
                _log.Warn($"The undo script for {tweak.Key} exited with code {result.ExitCode}.");
            }
        }

        _undo.Remove(tweak.Key);
    }

    private async Task RestoreRecordAsync(
        AppliedTweakRecord record,
        TweakDefinition? tweak,
        Action<string>? output,
        CancellationToken cancellationToken)
    {
        foreach (var service in record.Service)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!string.IsNullOrWhiteSpace(service.StartupType))
            {
                await _services.SetStartupTypeAsync(service.Name, service.StartupType, cancellationToken);
            }
        }

        foreach (var snapshot in record.Registry)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _registry.Restore(snapshot);
        }

        foreach (var feature in record.Features)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _features.RestoreStateAsync(feature, output, cancellationToken);
        }

        foreach (var script in tweak?.UndoScript ?? new List<string>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(script))
            {
                continue;
            }

            output?.Invoke($"Running the undo actions for {record.Name}...");
            var result = await PowerShellRunner.RunScriptAsync(PowerShellCompat.Wrap(script), output, cancellationToken);
            if (result.ExitCode != 0)
            {
                _log.Warn($"The undo script for {record.Key} exited with code {result.ExitCode}.");
            }
        }

        _undo.Remove(record.Key);
    }
}
