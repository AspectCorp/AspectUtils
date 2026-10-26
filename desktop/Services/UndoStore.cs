using System.IO;
using System.Text.Json;
using AspectUtils.Desktop.Models;

namespace AspectUtils.Desktop.Services;

public sealed class UndoStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly object _gate = new();
    private readonly LogService _log;
    private List<AppliedTweakRecord> _records;

    public UndoStore(LogService log)
    {
        _log = log;
        StoreDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AspectUtils",
            "state");
        Directory.CreateDirectory(StoreDirectory);
        StorePath = Path.Combine(StoreDirectory, "applied-tweaks.json");
        _records = Load();
    }

    public string StoreDirectory { get; }
    public string StorePath { get; }

    public IReadOnlyList<AppliedTweakRecord> All
    {
        get
        {
            lock (_gate)
            {
                return _records.ToList();
            }
        }
    }

    public AppliedTweakRecord? Find(string key)
    {
        lock (_gate)
        {
            return _records.FirstOrDefault(record =>
                string.Equals(record.Key, key, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void Upsert(AppliedTweakRecord record)
    {
        lock (_gate)
        {
            _records.RemoveAll(existing =>
                string.Equals(existing.Key, record.Key, StringComparison.OrdinalIgnoreCase));
            _records.Add(record);
            Save();
        }
    }

    public void Remove(string key)
    {
        lock (_gate)
        {
            _records.RemoveAll(existing =>
                string.Equals(existing.Key, key, StringComparison.OrdinalIgnoreCase));
            Save();
        }
    }

    private List<AppliedTweakRecord> Load()
    {
        if (!File.Exists(StorePath))
        {
            return new List<AppliedTweakRecord>();
        }

        try
        {
            var json = File.ReadAllText(StorePath);
            return JsonSerializer.Deserialize<List<AppliedTweakRecord>>(json, Options) ??
                   new List<AppliedTweakRecord>();
        }
        catch (Exception exception)
        {
            _log.Warn($"Could not read the undo manifest: {exception.Message}");
            return new List<AppliedTweakRecord>();
        }
    }

    private void Save()
    {
        try
        {
            File.WriteAllText(StorePath, JsonSerializer.Serialize(_records, Options));
        }
        catch (Exception exception)
        {
            _log.Error($"Could not save the undo manifest: {exception.Message}");
        }
    }
}
