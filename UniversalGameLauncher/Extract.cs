using System;
using System.ComponentModel;
using ICSharpCode.SharpZipLib.Zip;

namespace UniversalGameLauncher
{
    class Extract
    {
        private Application _application;

        public Extract(Application application)
        {
            _application = application;
        }

        public void Run(Action<RunWorkerCompletedEventArgs> onFinish)
        {
            BackgroundWorker bgw = new BackgroundWorker
            {
                WorkerReportsProgress = true
            };

            bgw.DoWork += new DoWorkEventHandler(
                delegate (object o, DoWorkEventArgs args)
                {
                    BackgroundWorker bw = o as BackgroundWorker;
                    FastZip fastZip = new FastZip();
                    fastZip.ExtractZip(Constants.ZIP_PATH, Constants.DESTINATION_PATH, null);
                });

            bgw.RunWorkerCompleted += new RunWorkerCompletedEventHandler(
            delegate (object o, RunWorkerCompletedEventArgs args)
            {
                onFinish(args);
            });

            bgw.RunWorkerAsync();

        }
    }
}
