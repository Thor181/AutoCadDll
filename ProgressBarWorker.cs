using System.Data;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ACDll
{
    internal static class ProgressBarWorker
    {
        internal static void ProgressBarInitialize(ProgressBar progressBar, int maximum)
        {
            progressBar.Maximum = maximum;
        }
        internal static void ProgressBasIncrement(ProgressBar progressBar, Dispatcher dispatcher)
        {
            dispatcher.Invoke(() =>
            {
                progressBar.Value++;
            });
        }
        internal static void ProgressBarsReset(params ProgressBar[] progressBars)
        {
            foreach (var bar in progressBars)
            {
                bar.Value = default(int);
            }
        }
    }
}
