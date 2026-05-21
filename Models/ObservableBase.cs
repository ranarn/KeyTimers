using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace KeyTimers.Models;

/// <summary>
/// Shared base for all observable model classes.
/// Provides <see cref="INotifyPropertyChanged"/> and a change-guarded <see cref="Set{T}"/> helper.
/// </summary>
public abstract class ObservableBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets <paramref name="field"/> to <paramref name="value"/> and raises
    /// <see cref="PropertyChanged"/> when the value actually changes.
    /// Returns <c>true</c> when a change occurred.
    /// </summary>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
