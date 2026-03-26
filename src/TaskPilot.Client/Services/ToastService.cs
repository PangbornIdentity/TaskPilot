namespace TaskPilot.Client.Services;

public enum ToastType { Success, Error, Info, Undo }

public class ToastItem
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required string Message { get; init; }
    public ToastType Type { get; init; }
    public int Duration { get; init; } = 4000;
    public Func<Task>? UndoAction { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public class ToastService
{
    private readonly List<ToastItem> _toasts = new();

    public IReadOnlyList<ToastItem> Toasts => _toasts;

    public event Action? ToastsChanged;

    public void ShowSuccess(string message, int duration = 4000)
    {
        Add(new ToastItem { Message = message, Type = ToastType.Success, Duration = duration });
    }

    public void ShowError(string message, int duration = 6000)
    {
        Add(new ToastItem { Message = message, Type = ToastType.Error, Duration = duration });
    }

    public void ShowInfo(string message, int duration = 4000)
    {
        Add(new ToastItem { Message = message, Type = ToastType.Info, Duration = duration });
    }

    public void ShowUndo(string message, Func<Task> undoAction, int duration = 5000)
    {
        Add(new ToastItem { Message = message, Type = ToastType.Undo, Duration = duration, UndoAction = undoAction });
    }

    public void Dismiss(Guid id)
    {
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
        if (toast is not null)
        {
            _toasts.Remove(toast);
            ToastsChanged?.Invoke();
        }
    }

    private void Add(ToastItem toast)
    {
        if (_toasts.Count >= 3)
        {
            _toasts.RemoveAt(0);
        }
        _toasts.Add(toast);
        ToastsChanged?.Invoke();
        _ = AutoDismiss(toast);
    }

    private async Task AutoDismiss(ToastItem toast)
    {
        await Task.Delay(toast.Duration);
        Dismiss(toast.Id);
    }
}
