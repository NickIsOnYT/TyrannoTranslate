using System.Windows.Threading;

namespace TyrannoTranslate.Helpers;

public sealed class Debouncer
{
    private readonly TimeSpan _delay;
    private readonly Action _action;
    private readonly Dispatcher _dispatcher;
    private DispatcherTimer? _timer;

    public Debouncer(Action action, TimeSpan delay, Dispatcher dispatcher)
    {
        _action = action;
        _delay = delay;
        _dispatcher = dispatcher;
    }

    public void Schedule()
    {
        _timer ??= new DispatcherTimer(_delay, DispatcherPriority.Background, (_, _) =>
        {
            _timer?.Stop();
            _action();
        }, _dispatcher);

        _timer.Stop();
        _timer.Start();
    }
}
