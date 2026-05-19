using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows.Forms;

namespace WindowsFormsApplication1
{
    static class Program
    {
        /// <summary>
        /// 應用程式的主要進入點。
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                RunCommandLine(args);
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        static void RunCommandLine(string[] args)
        {
            try
            {
                ExperimentSettings settings;
                bool persistSettings = true;
                if (String.Equals(args[0], "--experiment-smoke", StringComparison.OrdinalIgnoreCase))
                {
                    settings = ExperimentSettings.CreateSmoke();
                    persistSettings = false;
                }
                else if (String.Equals(args[0], "--experiment-self-test", StringComparison.OrdinalIgnoreCase))
                {
                    ExperimentSimulation.RunReservedNodeRequestSelfTest();
                    Console.WriteLine("SELF_TEST=ReservedNodeRequest PASSED");
                    return;
                }
                else if (String.Equals(args[0], "--experiment", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > 1 && File.Exists(args[1]))
                    {
                        settings = ExperimentSettings.Load(args[1]);
                        persistSettings = false;
                    }
                    else
                        settings = ExperimentSettings.LoadLast();
                }
                else
                {
                    return;
                }

                ExperimentBatchRunner runner = new ExperimentBatchRunner(delegate (string message)
                {
                    try
                    {
                        Console.WriteLine(message);
                    }
                    catch
                    {
                    }
                }, persistSettings);
                ExperimentBatchResult result = runner.Run(settings);
                try
                {
                    Console.WriteLine("WORKBOOK=" + result.WorkbookPath);
                }
                catch
                {
                }
            }
            catch (Exception ex)
            {
                try
                {
                    Console.Error.WriteLine(ex.ToString());
                }
                catch
                {
                }
                Environment.ExitCode = 1;
            }
        }
    }
}
