using System.Diagnostics;

namespace MoneySpot6.WebApp.Features.Core
{
    [SingletonService]
    public class WaitHelper
    {
        HashSet<Type> _triggered = new();

        public void Trigger<T>()
        {
            lock(_triggered)
            {
                _triggered.Add(typeof(T));
            }
        }

        public async Task Wait<T>(TimeSpan timespan, CancellationToken cancellationToken)
        {
            var sw = new Stopwatch();
            while (sw.Elapsed < timespan)
            {
                lock (_triggered)
                {
                    if (_triggered.Remove(typeof(T)))
                        return;
                }

                await Task.Delay(1000, cancellationToken).ContinueWith(_ => { });
            }
        }
    }
}
