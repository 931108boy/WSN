using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace WindowsFormsApplication1
{
    public class ExperimentSettings
    {
        public string ProjectRoot { get; set; }
        public int BaseSeed { get; set; }
        public int RunCount { get; set; }
        public int SensorCount { get; set; }
        public double MapWidthMeters { get; set; }
        public double MapHeightMeters { get; set; }
        public double SimulationTimeSeconds { get; set; }
        public double InitialEnergyJ { get; set; }
        public double SensorBackgroundLifetimeSeconds { get; set; }
        public double InitialResidualJitterPercent { get; set; }
        public double EventRatePerSecond { get; set; }
        public double PacketBits { get; set; }
        public double RadioRangeMeters { get; set; }
        public double ReceiverEnergyNjPerBit { get; set; }
        public double AmplifierEnergyNjPerBitM2 { get; set; }
        public double PowerExponent { get; set; }
        public double WcvSpeedMetersPerSecond { get; set; }
        public double WcvChargeRateJPerSecond { get; set; }
        public double WcvCapacityJ { get; set; }
        public double WcvMoveCostJPerMeter { get; set; }
        public int NmaxTask { get; set; }
        public bool DynamicNmaxTask { get; set; }
        public string ThresholdMode { get; set; }
        public double RequestThresholdPercent { get; set; }
        public double TreqSeconds { get; set; }
        public double BprDeadlineThresholdSeconds { get; set; }
        public bool AllowStandaloneProactiveDispatch { get; set; }
        public double ProactivePredictionHorizonSeconds { get; set; }
        public double ProactiveCandidateMaxEnergyRatio { get; set; }
        public double ProactiveCooldownSeconds { get; set; }
        public double YuDangerWindowSeconds { get; set; }
        public int YuDangerThresholdK { get; set; }
        public double YuIntervalUncertaintySeconds { get; set; }
        public double PrateChange { get; set; }
        public double RateChangeVariationPercent { get; set; }
        public string SelectedAlgorithmsCsv { get; set; }
        public string OutputDirectory { get; set; }
        public string LastOutputWorkbookPath { get; set; }
        public int MaxParallelJobs { get; set; }

        public ExperimentSettings()
        {
            ProjectRoot = ResolveProjectRoot();
            BaseSeed = 42;
            RunCount = 1;
            SensorCount = 200;
            MapWidthMeters = 500.0;
            MapHeightMeters = 500.0;
            SimulationTimeSeconds = 50000.0;
            InitialEnergyJ = 500.0;
            SensorBackgroundLifetimeSeconds = 100000.0;
            InitialResidualJitterPercent = 0.0;
            EventRatePerSecond = 0.05;
            PacketBits = 10.0 * 1024.0 * 8.0;
            RadioRangeMeters = 60.0;
            ReceiverEnergyNjPerBit = 50.0;
            AmplifierEnergyNjPerBitM2 = 0.01;
            PowerExponent = 2.0;
            WcvSpeedMetersPerSecond = 5.0;
            WcvChargeRateJPerSecond = 5.0;
            WcvCapacityJ = 200000.0;
            WcvMoveCostJPerMeter = 10.0;
            NmaxTask = 30;
            DynamicNmaxTask = false;
            ThresholdMode = "Percent";
            RequestThresholdPercent = 10.0;
            TreqSeconds = 4620.0;
            BprDeadlineThresholdSeconds = 4620.0;
            AllowStandaloneProactiveDispatch = false;
            ProactivePredictionHorizonSeconds = 0.0;
            ProactiveCandidateMaxEnergyRatio = 0.95;
            ProactiveCooldownSeconds = 0.0;
            YuDangerWindowSeconds = 0.0;
            YuDangerThresholdK = 0;
            YuIntervalUncertaintySeconds = 0.0;
            PrateChange = 0.2;
            RateChangeVariationPercent = 12.5;
            SelectedAlgorithmsCsv = DefaultAlgorithmSelectionCsv();
            OutputDirectory = Path.Combine(ProjectRoot, "outputs");
            LastOutputWorkbookPath = "";
            MaxParallelJobs = 0;
        }

        public static string[] AllAlgorithms()
        {
            return new string[] {
                "EDF",
                "NJF",
                "TADP_LIN",
                "RCSS",
                "NJF_ZHENG_BPR",
                "NJF_YU_BPR",
                "NJF_ROUTE_ZHENG_BPR_LIMITED",
                "NJF_ROUTE_ZHENG_BPR_EXTENDED",
                "NJF_ROUTE_YU_BPR_LIMITED",
                "NJF_ROUTE_YU_BPR_EXTENDED",
                "FUZZY",
                "GENE",
                "PSO",
                "Cuckoo"
            };
        }

        public static string DefaultAlgorithmSelectionCsv()
        {
            return "EDF,NJF,TADP_LIN,RCSS,NJF_ZHENG_BPR,NJF_YU_BPR,NJF_ROUTE_ZHENG_BPR_LIMITED,NJF_ROUTE_YU_BPR_LIMITED,FUZZY";
        }

        public static string CanonicalAlgorithmKey(string algorithm)
        {
            if (String.IsNullOrWhiteSpace(algorithm))
                return "";

            string key = algorithm.Trim();
            if (String.Equals(key, "NJF_BPR", StringComparison.OrdinalIgnoreCase))
                return "NJF_ZHENG_BPR";
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE_LIMITED", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_ZHENG_BPR_LIMITED";
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE_EXTENDED", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_ZHENG_BPR_EXTENDED";
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_ZHENG_BPR_EXTENDED";
            return key;
        }

        public static ExperimentSettings CreateDefault()
        {
            ExperimentSettings settings = new ExperimentSettings();
            settings.Normalize();
            return settings;
        }

        public static ExperimentSettings CreateSmoke()
        {
            ExperimentSettings settings = CreateDefault();
            settings.BaseSeed = 42;
            settings.RunCount = 2;
            settings.SensorCount = 40;
            settings.MapWidthMeters = 250.0;
            settings.MapHeightMeters = 250.0;
            settings.SimulationTimeSeconds = 14000.0;
            settings.InitialEnergyJ = 80.0;
            settings.SensorBackgroundLifetimeSeconds = 12000.0;
            settings.EventRatePerSecond = 0.02;
            settings.WcvSpeedMetersPerSecond = 20.0;
            settings.WcvChargeRateJPerSecond = 20.0;
            settings.NmaxTask = 40;
            settings.PrateChange = 0.2;
            settings.RateChangeVariationPercent = 12.5;
            settings.SelectedAlgorithmsCsv = "EDF,FUZZY";
            settings.OutputDirectory = Path.Combine(settings.ProjectRoot, "outputs");
            settings.Normalize();
            return settings;
        }

        public static string ResolveProjectRoot()
        {
            string current = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 8 && !String.IsNullOrEmpty(current); i++)
            {
                if (File.Exists(Path.Combine(current, "powercontrol.sln")) || File.Exists(Path.Combine(current, "ZHENG.pdf")))
                    return current.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                DirectoryInfo parent = Directory.GetParent(current);
                if (parent == null)
                    break;
                current = parent.FullName;
            }

            return Directory.GetCurrentDirectory();
        }

        public static string LastSettingsPath()
        {
            return Path.Combine(ResolveProjectRoot(), "experiment-last-settings.xml");
        }

        public static ExperimentSettings LoadLast()
        {
            string path = LastSettingsPath();
            if (!File.Exists(path))
                return CreateDefault();

            return Load(path);
        }

        public static ExperimentSettings Load(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExperimentSettings));
            using (FileStream stream = File.OpenRead(path))
            {
                ExperimentSettings settings = (ExperimentSettings)serializer.Deserialize(stream);
                settings.Normalize();
                return settings;
            }
        }

        public void SaveLast()
        {
            Save(LastSettingsPath());
        }

        public void Save(string path)
        {
            Normalize();
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            XmlSerializer serializer = new XmlSerializer(typeof(ExperimentSettings));
            using (FileStream stream = File.Create(path))
            {
                serializer.Serialize(stream, this);
            }
        }

        public void Normalize()
        {
            string originalProjectRoot = ProjectRoot;
            if (!IsProjectRootValid(ProjectRoot))
                ProjectRoot = ResolveProjectRoot();
            if (String.IsNullOrWhiteSpace(OutputDirectory) || IsDefaultOutputDirectory(OutputDirectory, originalProjectRoot))
                OutputDirectory = Path.Combine(ProjectRoot, "outputs");
            BaseSeed = Math.Max(0, BaseSeed);
            RunCount = Math.Max(1, RunCount);
            SensorCount = Math.Max(2, SensorCount);
            double mapSizeMeters = MapWidthMeters > 0.0 ? MapWidthMeters : MapHeightMeters;
            mapSizeMeters = Math.Max(1.0, mapSizeMeters);
            MapWidthMeters = mapSizeMeters;
            MapHeightMeters = mapSizeMeters;
            SimulationTimeSeconds = Math.Max(1.0, SimulationTimeSeconds);
            InitialEnergyJ = Math.Max(0.001, InitialEnergyJ);
            SensorBackgroundLifetimeSeconds = Math.Max(1.0, SensorBackgroundLifetimeSeconds);
            InitialResidualJitterPercent = Clamp(InitialResidualJitterPercent, 0.0, 95.0);
            EventRatePerSecond = Math.Max(0.0, EventRatePerSecond);
            PacketBits = Math.Max(1.0, PacketBits);
            RadioRangeMeters = Math.Max(1.0, RadioRangeMeters);
            ReceiverEnergyNjPerBit = Math.Max(0.0, ReceiverEnergyNjPerBit);
            AmplifierEnergyNjPerBitM2 = Math.Max(0.0, AmplifierEnergyNjPerBitM2);
            PowerExponent = Math.Max(1.0, PowerExponent);
            WcvSpeedMetersPerSecond = Math.Max(0.001, WcvSpeedMetersPerSecond);
            WcvChargeRateJPerSecond = Math.Max(0.001, WcvChargeRateJPerSecond);
            WcvCapacityJ = Math.Max(0.001, WcvCapacityJ);
            WcvMoveCostJPerMeter = Math.Max(0.0, WcvMoveCostJPerMeter);
            NmaxTask = Math.Max(1, NmaxTask);
            if (String.IsNullOrWhiteSpace(ThresholdMode))
                ThresholdMode = "Percent";
            if (ThresholdMode != "TreqSeconds")
                ThresholdMode = "Percent";
            RequestThresholdPercent = Clamp(RequestThresholdPercent, 0.1, 99.0);
            TreqSeconds = Math.Max(1.0, TreqSeconds);
            BprDeadlineThresholdSeconds = Math.Max(1.0, BprDeadlineThresholdSeconds);
            ProactivePredictionHorizonSeconds = Math.Max(0.0, ProactivePredictionHorizonSeconds);
            ProactiveCandidateMaxEnergyRatio = Clamp(ProactiveCandidateMaxEnergyRatio, 0.1, 1.0);
            ProactiveCooldownSeconds = Math.Max(0.0, ProactiveCooldownSeconds);
            YuDangerWindowSeconds = Math.Max(0.0, YuDangerWindowSeconds);
            YuDangerThresholdK = Math.Max(0, YuDangerThresholdK);
            YuIntervalUncertaintySeconds = Math.Max(0.0, YuIntervalUncertaintySeconds);
            PrateChange = Clamp(PrateChange, 0.0, 1.0);
            RateChangeVariationPercent = Clamp(RateChangeVariationPercent, 0.0, 99.0);
            MaxParallelJobs = Math.Max(0, MaxParallelJobs);
            List<string> selectedAlgorithms = GetSelectedAlgorithms();
            if (selectedAlgorithms.Count == 0)
                SelectedAlgorithmsCsv = DefaultAlgorithmSelectionCsv();
            else
                SelectedAlgorithmsCsv = String.Join(",", selectedAlgorithms.ToArray());
            ClearStaleLastOutputWorkbookPath();
        }

        public List<string> GetSelectedAlgorithms()
        {
            List<string> selected = new List<string>();
            string[] allowed = AllAlgorithms();
            Dictionary<string, string> allowedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allowed.Length; i++)
                allowedMap[allowed[i]] = allowed[i];

            if (!String.IsNullOrWhiteSpace(SelectedAlgorithmsCsv))
            {
                string[] parts = SelectedAlgorithmsCsv.Split(new char[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    string requested = CanonicalAlgorithmKey(parts[i]);
                    string canonical;
                    if (allowedMap.TryGetValue(requested, out canonical) && !selected.Contains(canonical))
                        selected.Add(canonical);
                }
            }

            return selected;
        }

        public void SetSelectedAlgorithms(IEnumerable<string> algorithms)
        {
            List<string> selected = new List<string>();
            string[] allowed = AllAlgorithms();
            Dictionary<string, string> allowedMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < allowed.Length; i++)
                allowedMap[allowed[i]] = allowed[i];

            foreach (string algorithm in algorithms)
            {
                string requested = CanonicalAlgorithmKey(algorithm);
                string canonical;
                if (allowedMap.TryGetValue(requested, out canonical) && !selected.Contains(canonical))
                    selected.Add(canonical);
            }

            SelectedAlgorithmsCsv = String.Join(",", selected.ToArray());
        }

        private static bool IsProjectRootValid(string projectRoot)
        {
            if (String.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
                return false;
            return File.Exists(Path.Combine(projectRoot, "powercontrol.sln")) ||
                File.Exists(Path.Combine(projectRoot, "ZHENG.pdf"));
        }

        private static bool IsDefaultOutputDirectory(string outputDirectory, string projectRoot)
        {
            if (String.IsNullOrWhiteSpace(outputDirectory) || String.IsNullOrWhiteSpace(projectRoot))
                return false;

            try
            {
                string actual = Path.GetFullPath(outputDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string expected = Path.GetFullPath(Path.Combine(projectRoot, "outputs")).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                return String.Equals(actual, expected, StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        private void ClearStaleLastOutputWorkbookPath()
        {
            if (String.IsNullOrWhiteSpace(LastOutputWorkbookPath))
            {
                LastOutputWorkbookPath = "";
                return;
            }

            string path = LastOutputWorkbookPath.Trim();
            bool stale = true;
            try
            {
                string expectedSuffix = String.Format(CultureInfo.InvariantCulture, "-seed{0}-runs{1}.xlsx", BaseSeed, RunCount);
                stale = !File.Exists(path) ||
                    Path.GetFileName(path).IndexOf(expectedSuffix, StringComparison.OrdinalIgnoreCase) < 0 ||
                    !IsPathUnderDirectory(path, OutputDirectory);
            }
            catch
            {
                stale = true;
            }

            LastOutputWorkbookPath = stale ? "" : path;
        }

        private static bool IsPathUnderDirectory(string path, string directory)
        {
            if (String.IsNullOrWhiteSpace(path) || String.IsNullOrWhiteSpace(directory))
                return false;

            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullDirectory = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!fullDirectory.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                fullDirectory += Path.DirectorySeparatorChar;
            return fullPath.StartsWith(fullDirectory, StringComparison.OrdinalIgnoreCase);
        }

        public string CreateOutputWorkbookPath()
        {
            Normalize();
            Directory.CreateDirectory(OutputDirectory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture);
            return Path.Combine(OutputDirectory, String.Format(CultureInfo.InvariantCulture,
                "{0}-wsn-comparison-seed{1}-runs{2}.xlsx", timestamp, BaseSeed, RunCount));
        }

        public static double Clamp(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }

    public class ExperimentBatchRunner
    {
        private readonly Action<string> progress;
        private readonly bool persistSettings;
        private readonly object progressLock;

        public ExperimentBatchRunner()
            : this(null)
        {
        }

        public ExperimentBatchRunner(Action<string> progressCallback)
            : this(progressCallback, true)
        {
        }

        public ExperimentBatchRunner(Action<string> progressCallback, bool saveSettingsAfterRun)
        {
            progress = progressCallback;
            persistSettings = saveSettingsAfterRun;
            progressLock = new object();
        }

        public ExperimentBatchResult Run(ExperimentSettings settings)
        {
            settings.Normalize();
            List<string> algorithms = settings.GetSelectedAlgorithms();
            if (algorithms.Count == 0)
                throw new InvalidOperationException("至少需要選擇一個演算法。");

            ExperimentBatchResult result = new ExperimentBatchResult();
            result.Settings = settings;
            result.TaskDetailsDirectory = MissionDetailCsvWriter.PrepareTaskDetailsDirectory(settings.OutputDirectory);

            int totalWork = settings.RunCount * algorithms.Count;
            int completedWork = 0;
            int maxParallelJobs = ResolveMaxParallelJobs(settings, totalWork);
            RaiseThreadPoolMinimum(maxParallelJobs);
            ExperimentRunBatchResult[] runResults = new ExperimentRunBatchResult[settings.RunCount];
            Report(String.Format(CultureInfo.InvariantCulture,
                "平行批次啟動：runs={0}, algorithms={1}, max parallel jobs={2}{3}",
                settings.RunCount, algorithms.Count, maxParallelJobs,
                settings.MaxParallelJobs > 0 ? " (manual)" : " (auto)"));

            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = maxParallelJobs;
            Parallel.For(1, settings.RunCount + 1, options, delegate (int runIndex)
            {
                int seed = settings.BaseSeed + runIndex - 1;
                ReportProgress(completedWork, totalWork,
                    String.Format(CultureInfo.InvariantCulture, "產生共用資料 run {0}/{1}, seed={2}", runIndex, settings.RunCount, seed));

                ExperimentRunBatchResult runBatch = new ExperimentRunBatchResult(algorithms.Count);
                ExperimentArtifact artifact = ExperimentArtifact.Generate(settings, runIndex, seed);
                runBatch.Artifact = artifact;
                runResults[runIndex - 1] = runBatch;
            });

            Parallel.For(0, totalWork, options, delegate (int workIndex)
            {
                int runIndex = workIndex / algorithms.Count + 1;
                int algorithmIndex = workIndex % algorithms.Count;
                string algorithm = algorithms[algorithmIndex];
                ExperimentRunBatchResult runBatch = runResults[runIndex - 1];

                ReportProgress(completedWork, totalWork,
                    String.Format(CultureInfo.InvariantCulture, "run {0}/{1} 執行 {2} ({3}/{4})",
                        runIndex, settings.RunCount, algorithm, algorithmIndex + 1, algorithms.Count));

                ExperimentSimulation simulation = new ExperimentSimulation(settings, runBatch.Artifact, algorithm, result.TaskDetailsDirectory);
                ExperimentRunResult run = simulation.Run();
                runBatch.AlgorithmResults[algorithmIndex] = run;

                int done = Interlocked.Increment(ref completedWork);
                ReportProgress(done, totalWork,
                    String.Format(CultureInfo.InvariantCulture, "完成 run {0}/{1} {2} ({3}/{4})",
                        runIndex, settings.RunCount, algorithm, done, totalWork));
            });

            Report("所有 run 已完成，正在由單一執行緒合併結果並寫出 Excel。");
            MergeRunResults(result, runResults);

            string workbookPath = settings.CreateOutputWorkbookPath();
            ExperimentWorkbookWriter.Write(workbookPath, result);
            settings.LastOutputWorkbookPath = workbookPath;
            if (persistSettings)
                settings.SaveLast();
            result.WorkbookPath = workbookPath;
            Report("Excel 已輸出：" + workbookPath);
            Report("任務明細 CSV 已輸出：" + result.TaskDetailsDirectory);
            return result;
        }

        private void Report(string message)
        {
            if (progress != null)
            {
                lock (progressLock)
                {
                    progress(message);
                }
            }
        }

        private void ReportProgress(int completedWork, int totalWork, string detail)
        {
            double percent = totalWork <= 0 ? 100.0 : (double)completedWork * 100.0 / (double)totalWork;
            Report(String.Format(CultureInfo.InvariantCulture, "進度 {0:0.0}%：{1}", percent, detail));
        }

        private static void MergeRunResults(ExperimentBatchResult result, ExperimentRunBatchResult[] runResults)
        {
            for (int runIndex = 0; runIndex < runResults.Length; runIndex++)
            {
                ExperimentRunBatchResult runBatch = runResults[runIndex];
                if (runBatch == null)
                    continue;

                result.Artifacts.Add(runBatch.Artifact);
                for (int i = 0; i < runBatch.AlgorithmResults.Length; i++)
                {
                    ExperimentRunResult run = runBatch.AlgorithmResults[i];
                    if (run == null)
                        continue;
                    result.RunSummaries.Add(run.Summary);
                    result.TotalTaskRecordCount += run.TotalTaskRecordCount;
                    result.DeathRecords.AddRange(run.Deaths);
                }
            }
        }

        private static int ResolveMaxParallelJobs(ExperimentSettings settings, int totalWork)
        {
            int requested = settings.MaxParallelJobs > 0 ? settings.MaxParallelJobs : Environment.ProcessorCount;
            return Math.Max(1, Math.Min(Math.Max(1, totalWork), requested));
        }

        private static void RaiseThreadPoolMinimum(int maxParallelJobs)
        {
            int workerThreads;
            int completionPortThreads;
            ThreadPool.GetMinThreads(out workerThreads, out completionPortThreads);
            if (workerThreads < maxParallelJobs)
                ThreadPool.SetMinThreads(maxParallelJobs, completionPortThreads);
        }

        private class ExperimentRunBatchResult
        {
            public ExperimentArtifact Artifact;
            public ExperimentRunResult[] AlgorithmResults;

            public ExperimentRunBatchResult(int algorithmCount)
            {
                AlgorithmResults = new ExperimentRunResult[Math.Max(0, algorithmCount)];
            }
        }
    }

    public class ExperimentBatchResult
    {
        public ExperimentSettings Settings { get; set; }
        public List<ExperimentArtifact> Artifacts { get; private set; }
        public List<ExperimentRunSummary> RunSummaries { get; private set; }
        public List<ExperimentDeathRecord> DeathRecords { get; private set; }
        public string WorkbookPath { get; set; }
        public string TaskDetailsDirectory { get; set; }
        public int TotalTaskRecordCount { get; set; }

        public ExperimentBatchResult()
        {
            Artifacts = new List<ExperimentArtifact>();
            RunSummaries = new List<ExperimentRunSummary>();
            DeathRecords = new List<ExperimentDeathRecord>();
            WorkbookPath = "";
            TaskDetailsDirectory = "";
        }
    }

    public class ExperimentArtifact
    {
        public int RunIndex;
        public int Seed;
        public string ArtifactHash;
        public List<SensorTemplate> Sensors;
        public List<PacketEventTemplate> PacketEvents;
        public List<RateChangeTemplate> RateChanges;
        public double BaseX;
        public double BaseY;

        public ExperimentArtifact()
        {
            Sensors = new List<SensorTemplate>();
            PacketEvents = new List<PacketEventTemplate>();
            RateChanges = new List<RateChangeTemplate>();
            ArtifactHash = "";
        }

        public static ExperimentArtifact Generate(ExperimentSettings settings, int runIndex, int seed)
        {
            Random random = new Random(seed);
            ExperimentArtifact artifact = new ExperimentArtifact();
            artifact.RunIndex = runIndex;
            artifact.Seed = seed;
            artifact.BaseX = 0.0;
            artifact.BaseY = 0.0;

            bool connected = false;
            for (int attempt = 1; attempt <= 1000; attempt++)
            {
                BuildRandomSensors(settings, artifact, random);
                AssignRoutingParents(settings, artifact);
                if (artifact.CountMissingRoutingParents() == 0)
                {
                    connected = true;
                    break;
                }
            }

            if (!connected)
            {
                throw new InvalidOperationException(
                    "無法產生 connected topology：RadioRangeMeters 太小或 SensorCount 太低，無法在 1000 次重試內讓所有 sensor 連到 BS。");
            }

            int eventCount = Math.Max(0, (int)Math.Round(settings.EventRatePerSecond * settings.SimulationTimeSeconds));
            for (int i = 0; i < eventCount; i++)
            {
                PacketEventTemplate packetEvent = new PacketEventTemplate();
                packetEvent.TimeSeconds = random.NextDouble() * settings.SimulationTimeSeconds;
                packetEvent.SourceId = 1 + random.Next(settings.SensorCount);
                packetEvent.PacketBits = settings.PacketBits;
                artifact.PacketEvents.Add(packetEvent);
            }
            artifact.PacketEvents.Sort(delegate (PacketEventTemplate a, PacketEventTemplate b)
            {
                int compare = a.TimeSeconds.CompareTo(b.TimeSeconds);
                if (compare != 0)
                    return compare;
                return a.SourceId.CompareTo(b.SourceId);
            });

            for (double time = 10000.0; time <= settings.SimulationTimeSeconds + 1e-9; time += 10000.0)
            {
                for (int id = 1; id <= settings.SensorCount; id++)
                {
                    if (random.NextDouble() <= settings.PrateChange)
                    {
                        RateChangeTemplate change = new RateChangeTemplate();
                        change.TimeSeconds = time;
                        change.NodeId = id;
                        double variationRatio = settings.RateChangeVariationPercent / 100.0;
                        change.Multiplier = (1.0 - variationRatio) + random.NextDouble() * (variationRatio * 2.0);
                        artifact.RateChanges.Add(change);
                    }
                }
            }

            artifact.ArtifactHash = artifact.ComputeHash(settings);
            return artifact;
        }

        private static void BuildRandomSensors(ExperimentSettings settings, ExperimentArtifact artifact, Random random)
        {
            artifact.Sensors.Clear();

            SensorTemplate baseStation = new SensorTemplate();
            baseStation.Id = 0;
            baseStation.X = artifact.BaseX;
            baseStation.Y = artifact.BaseY;
            baseStation.InitialEnergyJ = Double.PositiveInfinity;
            baseStation.ParentId = -1;
            artifact.Sensors.Add(baseStation);

            for (int id = 1; id <= settings.SensorCount; id++)
            {
                SensorTemplate sensor = new SensorTemplate();
                sensor.Id = id;
                sensor.X = random.NextDouble() * settings.MapWidthMeters;
                sensor.Y = random.NextDouble() * settings.MapHeightMeters;
                double jitter = settings.InitialResidualJitterPercent / 100.0;
                double residualRatio = 1.0 - random.NextDouble() * jitter;
                sensor.InitialEnergyJ = settings.InitialEnergyJ * residualRatio;
                sensor.ParentId = -1;
                artifact.Sensors.Add(sensor);
            }
        }

        private static void AssignRoutingParents(ExperimentSettings settings, ExperimentArtifact artifact)
        {
            int nodeCount = artifact.Sensors.Count;
            for (int i = 0; i < nodeCount; i++)
                artifact.Sensors[i].ParentId = -1;

            List<int>[] graph = new List<int>[nodeCount];
            for (int i = 0; i < nodeCount; i++)
                graph[i] = new List<int>();

            for (int i = 0; i < nodeCount; i++)
            {
                for (int j = i + 1; j < nodeCount; j++)
                {
                    double linkDistance = Distance(artifact.Sensors[i].X, artifact.Sensors[i].Y,
                        artifact.Sensors[j].X, artifact.Sensors[j].Y);
                    if (linkDistance <= settings.RadioRangeMeters)
                    {
                        graph[i].Add(j);
                        graph[j].Add(i);
                    }
                }
            }

            bool[] visited = new bool[nodeCount];
            Queue<int> queue = new Queue<int>();
            visited[0] = true;
            queue.Enqueue(0);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                for (int i = 0; i < graph[current].Count; i++)
                {
                    int next = graph[current][i];
                    if (visited[next])
                        continue;

                    visited[next] = true;
                    artifact.Sensors[next].ParentId = current;
                    queue.Enqueue(next);
                }
            }
        }

        private string ComputeHash(ExperimentSettings settings)
        {
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                AddHash(ref hash, RunIndex);
                AddHash(ref hash, Seed);
                AddHash(ref hash, settings.SensorCount);
                AddHash(ref hash, settings.PrateChange);
                AddHash(ref hash, settings.RateChangeVariationPercent);
                for (int i = 0; i < Sensors.Count; i++)
                {
                    AddHash(ref hash, Sensors[i].Id);
                    AddHash(ref hash, Sensors[i].X);
                    AddHash(ref hash, Sensors[i].Y);
                    AddHash(ref hash, Sensors[i].InitialEnergyJ);
                    AddHash(ref hash, Sensors[i].ParentId);
                }
                for (int i = 0; i < PacketEvents.Count; i++)
                {
                    AddHash(ref hash, PacketEvents[i].TimeSeconds);
                    AddHash(ref hash, PacketEvents[i].SourceId);
                }
                for (int i = 0; i < RateChanges.Count; i++)
                {
                    AddHash(ref hash, RateChanges[i].TimeSeconds);
                    AddHash(ref hash, RateChanges[i].NodeId);
                    AddHash(ref hash, RateChanges[i].Multiplier);
                }
                return hash.ToString("X16", CultureInfo.InvariantCulture);
            }
        }

        private static void AddHash(ref ulong hash, int value)
        {
            AddHash(ref hash, value.ToString(CultureInfo.InvariantCulture));
        }

        private static void AddHash(ref ulong hash, double value)
        {
            AddHash(ref hash, value.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void AddHash(ref ulong hash, string value)
        {
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 1099511628211UL;
            }
        }

        public static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        public int CountMissingRoutingParents()
        {
            int count = 0;
            for (int i = 1; i < Sensors.Count; i++)
            {
                if (Sensors[i].ParentId < 0)
                    count++;
            }
            return count;
        }

        public double MissingRoutingParentRatio()
        {
            int sensorCount = Math.Max(1, Sensors.Count - 1);
            return (double)CountMissingRoutingParents() / (double)sensorCount;
        }
    }

    public class SensorTemplate
    {
        public int Id;
        public double X;
        public double Y;
        public double InitialEnergyJ;
        public int ParentId;
    }

    public class PacketEventTemplate
    {
        public double TimeSeconds;
        public int SourceId;
        public double PacketBits;
    }

    public class RateChangeTemplate
    {
        public double TimeSeconds;
        public int NodeId;
        public double Multiplier;
    }

    internal class ExperimentSimulation
    {
        private const double Epsilon = 1e-7;
        private const string ZhengBprWindowRemovalReason = "ZHENG_BPR_WINDOW_REMOVAL";
        private const string YuBprDangerIntervalRemovalReason = "YU_BPR_DANGER_INTERVAL_REMOVAL";
        private readonly ExperimentSettings settings;
        private readonly ExperimentArtifact artifact;
        private readonly string algorithm;
        private readonly Random algorithmRandom;
        private readonly SensorState[] sensors;
        private readonly List<ChargingRequest> activeRequests;
        private readonly List<ExperimentDeathRecord> deaths;
        private Dictionary<int, BprSTableEntry> bprSTableByNodeId;
        private readonly ExperimentRunSummary summary;
        private readonly MissionDetailCsvWriter csvWriter;
        private readonly HashSet<int> servedNodeIds;
        private int totalTaskRecordCount;
        private double totalDeliveredEnergyForTasks;
        private double totalDeliveredEnergyForProactiveTasks;
        private int proactiveTaskRecordCount;
        private int nextEventIndex;
        private int nextRateChangeIndex;
        private int nextRequestId;
        private int missionId;
        private double currentTime;
        private bool stopForFirstDeath;
        private HashSet<int> plannedMissionNodeIds;
        private int[] routingSubtreeSizeByNodeId;
        private double[] expectedRoutingTxPacketsPerSecondByNodeId;
        private double[] expectedRoutingRxPacketsPerSecondByNodeId;
        private double[] expectedRoutingForwardPacketsPerSecondByNodeId;
        private double[] estimatedRoutingTxLoadJPerSecondByNodeId;
        private double[] estimatedRoutingRxLoadJPerSecondByNodeId;
        private double[] estimatedRoutingLoadJPerSecondByNodeId;

        private enum BprProactiveSelectionMode
        {
            Deterministic,
            RouteInsertionCost
        }

        private enum YuProactiveSelectionMode
        {
            Deterministic,
            RouteInsertionCost
        }

        private sealed class BprSTableEntry
        {
            public int NodeId;
            public double LatestReportedDeadlineSeconds;
            public double LastUpdateTimeSeconds;
            public double EnergyJ;
            public double ConsumeRateJPerSecond;
            public double BaseConsumeRateJPerSecond;
            public double EffectiveConsumeRateJPerSecond;
            public double RoutingLoadJPerSecond;
            public double RoutingTxLoadJPerSecond;
            public double RoutingRxLoadJPerSecond;
            public int RoutingSubtreeSize;
            public double ExpectedRoutingForwardPacketsPerSecond;
            public bool IsPendingRequest;
            public bool IsScheduledInCurrentMission;
            public bool IsAlive;
            public string LastUpdateReason;
            public double LastChargedTimeSeconds;
            public double LastProactiveSelectedTimeSeconds;
        }

        private sealed class YuRequestInterval
        {
            public int NodeId;
            public double CenterRequestTimeSeconds;
            public double IntervalStartSeconds;
            public double IntervalEndSeconds;
            public double EnergyJ;
            public double ConsumeRateJPerSecond;
            public double BaseConsumeRateJPerSecond;
            public double EffectiveConsumeRateJPerSecond;
            public double RoutingLoadJPerSecond;
            public int RoutingSubtreeSize;
            public double ExpectedRoutingForwardPacketsPerSecond;
            public double UncertaintySeconds;
            public bool IsPendingRequest;
            public bool IsScheduledInCurrentMission;
            public bool IsAlive;
        }

        private sealed class BprPredictionSegment
        {
            public int NodeId;
            public double StartTimeSeconds;
            public double EndTimeSeconds;
            public double StartEnergyJ;
            public double EndEnergyJ;
            public double EffectiveConsumeRateJPerSecond;
            public bool CrossesRequestThreshold;
            public double RequestTimeSeconds;
            public bool CrossesDeathThreshold;
            public double DeathTimeSeconds;
            public string SegmentReason;
        }

        private sealed class BprPredictedRequest
        {
            public int NodeId;
            public double RequestTimeSeconds;
            public double DeathTimeSeconds;
            public double EnergyAtPredictionStartJ;
            public double EffectiveConsumeRateJPerSecond;
            public double SlackSeconds;
            public double RouteInsertionCost;
            public bool IsReserved;
            public bool IsPendingRequest;
            public bool IsScheduledInCurrentMission;
        }

        private sealed class BprWindow
        {
            public double WindowStartSeconds;
            public double WindowEndSeconds;
            public List<BprPredictedRequest> Requests;
            public int BottleneckCount;
            public int OverflowCount;
        }

        private sealed class BprRemovalDecision
        {
            public int NodeId;
            public double RequestTimeSeconds;
            public double DeathTimeSeconds;
            public double EffectiveConsumeRateJPerSecond;
            public double SlackSeconds;
            public double RouteInsertionCost;
            public double Score;
            public string Reason;
        }

        private sealed class YuPredictionSegment
        {
            public int NodeId;
            public double StartTimeSeconds;
            public double EndTimeSeconds;
            public double StartEnergyJ;
            public double EndEnergyJ;
            public double EffectiveConsumeRateJPerSecond;
            public bool CrossesRequestThreshold;
            public double RequestTimeSeconds;
            public bool CrossesDeathThreshold;
            public double DeathTimeSeconds;
            public string SegmentReason;
        }

        private sealed class YuPredictedInterval
        {
            public int NodeId;
            public double CenterRequestTimeSeconds;
            public double IntervalStartSeconds;
            public double IntervalEndSeconds;
            public double EarliestDeathTimeSeconds;
            public double LatestSafeServiceTimeSeconds;
            public double EnergyAtPredictionStartJ;
            public double EffectiveConsumeRateJPerSecond;
            public double UncertaintySeconds;
            public double SlackSeconds;
            public double RouteInsertionCost;
            public bool IsReserved;
            public bool IsPendingRequest;
            public bool IsScheduledInCurrentMission;
            public bool IsAlive;
        }

        private sealed class YuDangerWindow
        {
            public double WindowStartSeconds;
            public double WindowEndSeconds;
            public List<YuPredictedInterval> OverlappingIntervals;
            public int DangerCount;
            public int KStar;
            public int RemovalNeededCount;
        }

        private sealed class YuRemovalDecision
        {
            public int NodeId;
            public double CenterRequestTimeSeconds;
            public double IntervalStartSeconds;
            public double IntervalEndSeconds;
            public double EarliestDeathTimeSeconds;
            public double LatestSafeServiceTimeSeconds;
            public double EffectiveConsumeRateJPerSecond;
            public double UncertaintySeconds;
            public double SlackSeconds;
            public double RouteInsertionCost;
            public double Score;
            public string Reason;
        }

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName)
            : this(experimentSettings, sharedArtifact, schedulerName, null)
        {
        }

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName, string taskDetailsDirectory)
        {
            settings = experimentSettings;
            artifact = sharedArtifact;
            algorithm = ExperimentSettings.CanonicalAlgorithmKey(schedulerName);
            algorithmRandom = new Random(sharedArtifact.Seed * 397 + StableStringHash(algorithm));
            sensors = new SensorState[artifact.Sensors.Count];
            activeRequests = new List<ChargingRequest>();
            deaths = new List<ExperimentDeathRecord>();
            servedNodeIds = new HashSet<int>();
            csvWriter = String.IsNullOrWhiteSpace(taskDetailsDirectory)
                ? null
                : new MissionDetailCsvWriter(taskDetailsDirectory, artifact.RunIndex, algorithm);
            nextEventIndex = 0;
            nextRateChangeIndex = 0;
            nextRequestId = 1;
            missionId = 0;
            currentTime = 0.0;
            stopForFirstDeath = false;
            totalTaskRecordCount = 0;
            totalDeliveredEnergyForTasks = 0.0;
            totalDeliveredEnergyForProactiveTasks = 0.0;
            proactiveTaskRecordCount = 0;

            for (int i = 0; i < artifact.Sensors.Count; i++)
                sensors[i] = new SensorState(artifact.Sensors[i], settings);
            InitializeRoutingLoadEstimates();
            if (csvWriter != null)
                csvWriter.WriteRoutingLoad(artifact, routingSubtreeSizeByNodeId,
                    expectedRoutingTxPacketsPerSecondByNodeId,
                    expectedRoutingRxPacketsPerSecondByNodeId,
                    expectedRoutingForwardPacketsPerSecondByNodeId,
                    estimatedRoutingTxLoadJPerSecondByNodeId,
                    estimatedRoutingRxLoadJPerSecondByNodeId,
                    estimatedRoutingLoadJPerSecondByNodeId);
            InitializeBprSTable();

            summary = new ExperimentRunSummary();
            summary.RunIndex = artifact.RunIndex;
            summary.Seed = artifact.Seed;
            summary.Algorithm = algorithm;
            summary.ArtifactHash = artifact.ArtifactHash;
            summary.PrateChange = settings.PrateChange;
            summary.RateChangeVariationPercent = settings.RateChangeVariationPercent;
            summary.RateChangeScheduleCount = artifact.RateChanges.Count;
            summary.NetworkLifetimeSeconds = settings.SimulationTimeSeconds;
            summary.FirstDeadNodeId = -1;
            summary.FirstDeadTimeSeconds = -1.0;
            summary.FirstDeadReason = "";
            summary.FirstDeadReasonZh = "";
            summary.FirstDeadDirectEnergyCause = "";
            summary.FirstDeadDirectEnergyCauseZh = "";
            summary.FirstDeadSchedulingCause = "";
            summary.FirstDeadSchedulingCauseZh = "";
            summary.RoutingParentMissingNodeCount = artifact.CountMissingRoutingParents();
            summary.RoutingDisconnectedNodeRatio = artifact.MissingRoutingParentRatio();
        }

        public ExperimentRunResult Run()
        {
            CreateRequestsAtCurrentTime();

            int safety = 0;
            while (currentTime < settings.SimulationTimeSeconds - Epsilon && !stopForFirstDeath && safety < 1000000)
            {
                safety++;
                RemoveCompletedOrDeadRequests();

                if (activeRequests.Count > 0)
                {
                    ExecuteMission();
                    continue;
                }

                if (settings.AllowStandaloneProactiveDispatch &&
                    UsesBprBottleneckCandidates() &&
                    HasBprBottleneckCandidate())
                {
                    ExecuteMission();
                    continue;
                }

                double nextTime = FindNextInterestingTime(settings.SimulationTimeSeconds, null);
                if (settings.AllowStandaloneProactiveDispatch && UsesBprBottleneckCandidates())
                {
                    double bprTime = FindNextBprBottleneckCandidateTime();
                    if (bprTime >= currentTime - Epsilon)
                        nextTime = Math.Min(nextTime, bprTime);
                }
                if (nextTime <= currentTime + Epsilon)
                    nextTime = Math.Min(settings.SimulationTimeSeconds, currentTime + 1.0);
                AdvanceTo(nextTime, null);
            }

            if (summary.FirstDeadNodeId < 0)
                summary.NetworkLifetimeSeconds = settings.SimulationTimeSeconds;

            summary.MissionCount = missionId;
            summary.RequestCount = summary.NaturalRequestCount + summary.ProactiveTaskCount;
            summary.ChargeEfficiency = summary.DeliveredEnergyJ /
                Math.Max(summary.DeliveredEnergyJ + summary.MoveEnergyJ, 1e-9);
            summary.AverageWaitSeconds = summary.SuccessfulCharges > 0
                ? summary.TotalWaitSeconds / summary.SuccessfulCharges
                : 0.0;
            summary.UniqueServedNodeCount = servedNodeIds.Count;
            summary.AverageDeliveredEnergyPerTask = totalTaskRecordCount > 0
                ? totalDeliveredEnergyForTasks / totalTaskRecordCount
                : 0.0;
            summary.AverageDeliveredEnergyPerProactiveTask = proactiveTaskRecordCount > 0
                ? totalDeliveredEnergyForProactiveTasks / proactiveTaskRecordCount
                : 0.0;

            ExperimentRunResult result = new ExperimentRunResult();
            result.Summary = summary;
            result.TotalTaskRecordCount = totalTaskRecordCount;
            result.Deaths = deaths;
            if (csvWriter != null)
                csvWriter.Dispose();
            return result;
        }

        private void InitializeBprSTable()
        {
            bprSTableByNodeId = new Dictionary<int, BprSTableEntry>();
            for (int id = 1; id < sensors.Length; id++)
                RefreshBprSTableEntry(id, "initialize", true);
        }

        private void InitializeRoutingLoadEstimates()
        {
            int count = sensors.Length;
            routingSubtreeSizeByNodeId = new int[count];
            expectedRoutingTxPacketsPerSecondByNodeId = new double[count];
            expectedRoutingRxPacketsPerSecondByNodeId = new double[count];
            expectedRoutingForwardPacketsPerSecondByNodeId = new double[count];
            estimatedRoutingTxLoadJPerSecondByNodeId = new double[count];
            estimatedRoutingRxLoadJPerSecondByNodeId = new double[count];
            estimatedRoutingLoadJPerSecondByNodeId = new double[count];

            List<int>[] children = new List<int>[count];
            for (int i = 0; i < count; i++)
                children[i] = new List<int>();
            for (int id = 1; id < count; id++)
            {
                int parent = sensors[id].ParentId;
                if (parent >= 0 && parent < count)
                    children[parent].Add(id);
            }

            bool[] computed = new bool[count];
            bool[] visiting = new bool[count];
            for (int id = 1; id < count; id++)
                routingSubtreeSizeByNodeId[id] = ComputeRoutingSubtreeSize(id, children, computed, visiting);

            int sensorCount = Math.Max(1, count - 1);
            for (int id = 1; id < count; id++)
            {
                int subtreeSize = Math.Max(1, routingSubtreeSizeByNodeId[id]);
                double downstreamPacketsPerSecond = settings.EventRatePerSecond * Math.Max(0, subtreeSize - 1) / sensorCount;
                expectedRoutingTxPacketsPerSecondByNodeId[id] =
                    settings.EventRatePerSecond * subtreeSize / sensorCount;
                expectedRoutingRxPacketsPerSecondByNodeId[id] = downstreamPacketsPerSecond;
                expectedRoutingForwardPacketsPerSecondByNodeId[id] = downstreamPacketsPerSecond;
                RefreshRoutingLoadEstimateForNode(id);
            }
        }

        private int ComputeRoutingSubtreeSize(
            int nodeId,
            List<int>[] children,
            bool[] computed,
            bool[] visiting)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return 0;
            if (computed[nodeId])
                return routingSubtreeSizeByNodeId[nodeId];
            if (visiting[nodeId])
                return 1;

            visiting[nodeId] = true;
            int size = 1;
            for (int i = 0; i < children[nodeId].Count; i++)
                size += ComputeRoutingSubtreeSize(children[nodeId][i], children, computed, visiting);
            visiting[nodeId] = false;
            computed[nodeId] = true;
            routingSubtreeSizeByNodeId[nodeId] = size;
            return size;
        }

        private void RefreshRoutingLoadEstimateForNode(int nodeId)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length ||
                estimatedRoutingTxLoadJPerSecondByNodeId == null ||
                estimatedRoutingRxLoadJPerSecondByNodeId == null ||
                estimatedRoutingLoadJPerSecondByNodeId == null)
            {
                return;
            }

            SensorState sensor = sensors[nodeId];
            estimatedRoutingTxLoadJPerSecondByNodeId[nodeId] = GetRoutingTxLoadJPerSecond(sensor);
            estimatedRoutingRxLoadJPerSecondByNodeId[nodeId] = GetRoutingRxLoadJPerSecond(sensor);
            estimatedRoutingLoadJPerSecondByNodeId[nodeId] =
                estimatedRoutingTxLoadJPerSecondByNodeId[nodeId] +
                estimatedRoutingRxLoadJPerSecondByNodeId[nodeId];
        }

        private double GetRoutingTxLoadJPerSecond(SensorState sensor)
        {
            if (sensor == null || sensor.Id <= 0 || sensor.Id >= sensors.Length ||
                expectedRoutingTxPacketsPerSecondByNodeId == null)
                return 0.0;

            double packetsPerSecond = expectedRoutingTxPacketsPerSecondByNodeId[sensor.Id];
            if (packetsPerSecond <= 0.0)
                return 0.0;
            return packetsPerSecond * TxEnergyJ(sensor, settings.PacketBits);
        }

        private double GetRoutingRxLoadJPerSecond(SensorState sensor)
        {
            if (sensor == null || sensor.Id <= 0 || sensor.Id >= sensors.Length ||
                expectedRoutingRxPacketsPerSecondByNodeId == null)
                return 0.0;

            double packetsPerSecond = expectedRoutingRxPacketsPerSecondByNodeId[sensor.Id];
            if (packetsPerSecond <= 0.0)
                return 0.0;
            return packetsPerSecond * RxEnergyJ(sensor, settings.PacketBits);
        }

        private double GetRoutingLoadJPerSecond(SensorState sensor)
        {
            return GetRoutingTxLoadJPerSecond(sensor) + GetRoutingRxLoadJPerSecond(sensor);
        }

        private double GetEffectiveConsumeRateJPerSecond(SensorState sensor)
        {
            if (sensor == null)
                return 0.0;
            return sensor.ConsumeRateJPerSecond + GetRoutingLoadJPerSecond(sensor);
        }

        private double ComputePredictedBaseConsumeRateJPerSecond(double rateScale)
        {
            return settings.InitialEnergyJ * Math.Max(0.01, rateScale) /
                Math.Max(1.0, settings.SensorBackgroundLifetimeSeconds);
        }

        private double ComputePredictedTxEnergyJ(double rateScale, double bits)
        {
            double unitNj = settings.ReceiverEnergyNjPerBit +
                Math.Pow(settings.RadioRangeMeters, settings.PowerExponent) * settings.AmplifierEnergyNjPerBitM2;
            return unitNj * bits * Math.Max(0.01, rateScale) * 1e-9;
        }

        private double ComputePredictedRxEnergyJ(double rateScale, double bits)
        {
            return settings.ReceiverEnergyNjPerBit * bits * Math.Max(0.01, rateScale) * 1e-9;
        }

        private double ComputePredictedRoutingLoadJPerSecond(int nodeId, double rateScale)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length ||
                expectedRoutingTxPacketsPerSecondByNodeId == null ||
                expectedRoutingRxPacketsPerSecondByNodeId == null)
            {
                return 0.0;
            }

            double txPackets = expectedRoutingTxPacketsPerSecondByNodeId[nodeId];
            double rxPackets = expectedRoutingRxPacketsPerSecondByNodeId[nodeId];
            return txPackets * ComputePredictedTxEnergyJ(rateScale, settings.PacketBits) +
                rxPackets * ComputePredictedRxEnergyJ(rateScale, settings.PacketBits);
        }

        private double ComputePredictedEffectiveConsumeRateJPerSecond(int nodeId, double rateScale)
        {
            return ComputePredictedBaseConsumeRateJPerSecond(rateScale) +
                ComputePredictedRoutingLoadJPerSecond(nodeId, rateScale);
        }

        private double ComputeBprEffectiveConsumeRateJPerSecond(int nodeId, double segmentStart, double segmentEnd)
        {
            return ComputePredictedEffectiveConsumeRateJPerSecond(
                nodeId,
                ResolvePredictedRateScaleAtSegmentStart(nodeId, segmentStart));
        }

        private double ComputeYuEffectiveConsumeRateJPerSecond(int nodeId, double segmentStart, double segmentEnd)
        {
            return ComputeBprEffectiveConsumeRateJPerSecond(nodeId, segmentStart, segmentEnd);
        }

        private double ResolvePredictedRateScaleAtSegmentStart(int nodeId, double segmentStart)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return 1.0;

            double predictedRateScale = sensors[nodeId].RateScale;
            for (int i = 0; i < artifact.RateChanges.Count; i++)
            {
                RateChangeTemplate change = artifact.RateChanges[i];
                if (change.NodeId != nodeId)
                    continue;
                if (change.TimeSeconds <= currentTime + Epsilon)
                    continue;
                if (change.TimeSeconds <= segmentStart + Epsilon)
                    predictedRateScale *= change.Multiplier;
            }
            return predictedRateScale;
        }

        private double GetPredictedRequestThresholdJ(SensorState sensor, double effectiveRate, double rateScale)
        {
            if (sensor == null)
                return 0.0;

            if (settings.ThresholdMode == "TreqSeconds")
            {
                double serviceFloor = Math.Max(
                    ComputePredictedTxEnergyJ(rateScale, settings.PacketBits) * 2.0,
                    settings.InitialEnergyJ * 0.005);
                return Math.Min(sensor.CapacityJ * 0.95, effectiveRate * settings.TreqSeconds + serviceFloor);
            }

            return sensor.CapacityJ * settings.RequestThresholdPercent / 100.0;
        }

        private int GetRoutingSubtreeSize(int nodeId)
        {
            if (routingSubtreeSizeByNodeId == null || nodeId <= 0 || nodeId >= routingSubtreeSizeByNodeId.Length)
                return 0;
            return routingSubtreeSizeByNodeId[nodeId];
        }

        private double GetExpectedRoutingForwardPacketsPerSecond(int nodeId)
        {
            if (expectedRoutingForwardPacketsPerSecondByNodeId == null ||
                nodeId <= 0 ||
                nodeId >= expectedRoutingForwardPacketsPerSecondByNodeId.Length)
            {
                return 0.0;
            }
            return expectedRoutingForwardPacketsPerSecondByNodeId[nodeId];
        }

        private double GetRequestEffectiveConsumeRate(ChargingRequest request)
        {
            if (request == null)
                return 0.0;
            if (request.EffectiveConsumeRateJPerSecond > 0.0)
                return request.EffectiveConsumeRateJPerSecond;
            if (request.NodeId > 0 && request.NodeId < sensors.Length)
                return GetEffectiveConsumeRateJPerSecond(sensors[request.NodeId]);
            return request.ConsumeRateJPerSecond;
        }

        private void PopulateChargingRequestRoutingFields(ChargingRequest request, SensorState sensor)
        {
            if (request == null || sensor == null)
                return;
            request.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            request.BaseConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            request.RoutingTxLoadJPerSecond = GetRoutingTxLoadJPerSecond(sensor);
            request.RoutingRxLoadJPerSecond = GetRoutingRxLoadJPerSecond(sensor);
            request.RoutingLoadJPerSecond = request.RoutingTxLoadJPerSecond + request.RoutingRxLoadJPerSecond;
            request.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond + request.RoutingLoadJPerSecond;
            request.RoutingSubtreeSize = GetRoutingSubtreeSize(sensor.Id);
            request.ExpectedRoutingForwardPacketsPerSecond = GetExpectedRoutingForwardPacketsPerSecond(sensor.Id);
        }

        private double ComputeBprRequestDeadlineSeconds(SensorState sensor)
        {
            if (sensor == null || !sensor.Alive)
                return currentTime;

            double threshold = GetRequestThresholdJ(sensor);
            double effectiveRate = GetEffectiveConsumeRateJPerSecond(sensor);
            if (effectiveRate <= 1e-12)
                return Double.PositiveInfinity;
            if (sensor.EnergyJ <= threshold + Epsilon)
                return currentTime;
            return currentTime + (sensor.EnergyJ - threshold) / effectiveRate;
        }

        private void RefreshBprSTableEntry(int nodeId, string reason, bool forceUpdate)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return;
            if (bprSTableByNodeId == null)
                bprSTableByNodeId = new Dictionary<int, BprSTableEntry>();

            SensorState sensor = sensors[nodeId];
            BprSTableEntry entry = GetOrCreateBprSTableEntry(nodeId);

            double newDeadline = ComputeBprRequestDeadlineSeconds(sensor);
            if (forceUpdate)
            {
                UpdateBprSTableEntrySnapshot(entry, sensor, newDeadline, reason);
                return;
            }

            if (ShouldRefreshBprDeadline(entry.LatestReportedDeadlineSeconds, newDeadline))
            {
                UpdateBprSTableEntrySnapshot(entry, sensor, newDeadline, reason);
                return;
            }

            string snapshotReason = String.IsNullOrEmpty(reason) ? "snapshot_only" : reason + "_snapshot_only";
            UpdateBprSTableEntryStateFields(entry, sensor, snapshotReason);
        }

        private bool ShouldRefreshBprDeadline(double oldDeadline, double newDeadline)
        {
            return ShouldUpdateBprDeadline(oldDeadline, newDeadline, GetBprDeadlineThresholdSeconds());
        }

        private bool ShouldUpdateBprDeadline(double oldDeadline, double newDeadline, double threshold)
        {
            if (Double.IsNaN(oldDeadline) || Double.IsNaN(newDeadline))
                return true;
            if (Double.IsInfinity(oldDeadline) || Double.IsInfinity(newDeadline))
                return oldDeadline != newDeadline;
            return Math.Abs(newDeadline - oldDeadline) >= threshold - Epsilon;
        }

        private BprSTableEntry GetOrCreateBprSTableEntry(int nodeId)
        {
            if (bprSTableByNodeId == null)
                bprSTableByNodeId = new Dictionary<int, BprSTableEntry>();

            BprSTableEntry entry;
            if (!bprSTableByNodeId.TryGetValue(nodeId, out entry))
            {
                entry = new BprSTableEntry();
                entry.NodeId = nodeId;
                entry.LatestReportedDeadlineSeconds = Double.NaN;
                entry.LastChargedTimeSeconds = Double.NegativeInfinity;
                entry.LastProactiveSelectedTimeSeconds = Double.NegativeInfinity;
                bprSTableByNodeId[nodeId] = entry;
            }

            return entry;
        }

        private void RefreshBprDeadlineAfterRateChange(int nodeId)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return;

            SensorState sensor = sensors[nodeId];
            BprSTableEntry entry = GetOrCreateBprSTableEntry(nodeId);
            if (sensor == null || !sensor.Alive)
            {
                entry.LatestReportedDeadlineSeconds = currentTime;
                entry.LastUpdateTimeSeconds = currentTime;
                entry.EnergyJ = sensor == null ? 0.0 : sensor.EnergyJ;
                entry.ConsumeRateJPerSecond = sensor == null ? 0.0 : sensor.ConsumeRateJPerSecond;
                entry.IsAlive = false;
                entry.IsPendingRequest = false;
                entry.IsScheduledInCurrentMission = false;
                entry.LastUpdateReason = "rate_change_dead";
                return;
            }

            double oldDeadline = entry.LatestReportedDeadlineSeconds;
            RefreshRoutingLoadEstimateForNode(nodeId);
            double newDeadline = ComputeBprRequestDeadlineSeconds(sensor);
            double threshold = GetBprDeadlineThresholdSeconds();
            bool updateDeadline = ShouldUpdateBprDeadline(oldDeadline, newDeadline, threshold);
            if (updateDeadline)
            {
                entry.LatestReportedDeadlineSeconds = newDeadline;
                entry.LastUpdateTimeSeconds = currentTime;
                entry.LastUpdateReason = "rate_change_deadline_updated";
            }
            else
            {
                entry.LastUpdateReason = "rate_change_deadline_unchanged";
            }

            entry.EnergyJ = sensor.EnergyJ;
            entry.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            entry.BaseConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            entry.RoutingTxLoadJPerSecond = GetRoutingTxLoadJPerSecond(sensor);
            entry.RoutingRxLoadJPerSecond = GetRoutingRxLoadJPerSecond(sensor);
            entry.RoutingLoadJPerSecond = entry.RoutingTxLoadJPerSecond + entry.RoutingRxLoadJPerSecond;
            entry.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond + entry.RoutingLoadJPerSecond;
            entry.RoutingSubtreeSize = GetRoutingSubtreeSize(sensor.Id);
            entry.ExpectedRoutingForwardPacketsPerSecond = GetExpectedRoutingForwardPacketsPerSecond(sensor.Id);
            entry.IsAlive = sensor.Alive;
            entry.IsPendingRequest = sensor.HasPendingRequest || HasActiveRequestForNode(nodeId);
            entry.IsScheduledInCurrentMission = IsNodeReservedForCurrentMission(nodeId);
        }

        private void UpdateBprSTableEntrySnapshot(
            BprSTableEntry entry,
            SensorState sensor,
            double deadline,
            string reason)
        {
            entry.LatestReportedDeadlineSeconds = deadline;
            entry.LastUpdateTimeSeconds = currentTime;
            UpdateBprSTableEntryStateFields(entry, sensor, reason);
        }

        private void UpdateBprSTableEntryStateFields(
            BprSTableEntry entry,
            SensorState sensor,
            string reason)
        {
            entry.EnergyJ = sensor.EnergyJ;
            entry.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            entry.BaseConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            entry.RoutingTxLoadJPerSecond = GetRoutingTxLoadJPerSecond(sensor);
            entry.RoutingRxLoadJPerSecond = GetRoutingRxLoadJPerSecond(sensor);
            entry.RoutingLoadJPerSecond = entry.RoutingTxLoadJPerSecond + entry.RoutingRxLoadJPerSecond;
            entry.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond + entry.RoutingLoadJPerSecond;
            entry.RoutingSubtreeSize = GetRoutingSubtreeSize(sensor.Id);
            entry.ExpectedRoutingForwardPacketsPerSecond = GetExpectedRoutingForwardPacketsPerSecond(sensor.Id);
            entry.IsPendingRequest = sensor.HasPendingRequest || HasActiveRequestForNode(sensor.Id);
            entry.IsScheduledInCurrentMission = IsNodeReservedForCurrentMission(sensor.Id);
            entry.IsAlive = sensor.Alive;
            entry.LastUpdateReason = reason ?? "";
        }

        private List<BprSTableEntry> GetEligibleBprSTableEntries(HashSet<int> reservedNodeIds)
        {
            List<BprSTableEntry> entries = new List<BprSTableEntry>();
            if (bprSTableByNodeId == null)
                return entries;

            foreach (KeyValuePair<int, BprSTableEntry> pair in bprSTableByNodeId)
            {
                BprSTableEntry entry = pair.Value;
                if (entry == null)
                    continue;
                int nodeId = entry.NodeId;
                if (nodeId <= 0 || nodeId >= sensors.Length)
                    continue;
                if (reservedNodeIds != null && reservedNodeIds.Contains(nodeId))
                    continue;
                if (!entry.IsAlive || entry.IsPendingRequest || entry.IsScheduledInCurrentMission)
                    continue;
                if (!sensors[nodeId].Alive || sensors[nodeId].HasPendingRequest || HasActiveRequestForNode(nodeId))
                    continue;
                if (entry.EffectiveConsumeRateJPerSecond <= 1e-12)
                    continue;
                if (sensors[nodeId].EnergyJ >= sensors[nodeId].CapacityJ * settings.ProactiveCandidateMaxEnergyRatio - Epsilon)
                    continue;
                double cooldownSeconds = ResolveProactiveCooldownSeconds();
                if (currentTime - entry.LastChargedTimeSeconds < cooldownSeconds - Epsilon)
                    continue;
                if (currentTime - entry.LastProactiveSelectedTimeSeconds < cooldownSeconds - Epsilon)
                    continue;
                if (Double.IsNaN(entry.LatestReportedDeadlineSeconds) ||
                    Double.IsInfinity(entry.LatestReportedDeadlineSeconds))
                    continue;

                entries.Add(entry);
            }

            entries.Sort(CompareBprSTableByDeadline);
            return entries;
        }

        private void ExecuteMission()
        {
            missionId++;
            double dispatchTime = currentTime;
            List<ChargingRequest> route = BuildMissionRoute();
            int deduplicatedTaskCount;
            route = DeduplicateMissionRoute(route, out deduplicatedTaskCount);
            if (route.Count == 0)
            {
                currentTime = Math.Min(settings.SimulationTimeSeconds, currentTime + 1.0);
                return;
            }

            MissionRecord mission = new MissionRecord();
            mission.RunIndex = artifact.RunIndex;
            mission.Seed = artifact.Seed;
            mission.Algorithm = algorithm;
            mission.MissionId = missionId;
            mission.DispatchTimeSeconds = dispatchTime;
            mission.DeduplicatedTaskCount = deduplicatedTaskCount;
            mission.RouteNodeIds = new List<int>();
            if (deduplicatedTaskCount > 0)
            {
                Debug.WriteLine(String.Format(CultureInfo.InvariantCulture,
                    "Mission {0} deduplicated {1} repeated SensorId task(s).",
                    missionId, deduplicatedTaskCount));
            }
            plannedMissionNodeIds = BuildPlannedMissionNodeSet(route);
            foreach (int nodeId in plannedMissionNodeIds)
                RefreshBprSTableEntry(nodeId, "mission_scheduled", true);
            MarkProactiveNodesSelected(route);
            int startPacketSent = summary.PacketSent;
            int startPacketReceived = summary.PacketReceived;
            int startPacketLost = summary.PacketLost;
            int startRoutingFailedPacketLost = summary.RoutingFailedPacketLost;

            double wcvEnergy = settings.WcvCapacityJ;
            double posX = artifact.BaseX;
            double posY = artifact.BaseY;
            int order = 0;

            for (int i = 0; i < route.Count && !stopForFirstDeath; i++)
            {
                ChargingRequest request = route[i];
                SensorState sensor = sensors[request.NodeId];
                if (!sensor.Alive)
                {
                    ExperimentTaskRecord skipped = RecordSkippedTask(request, dispatchTime, currentTime, "節點已死亡");
                    AccumulateMissionTask(mission, skipped);
                    continue;
                }

                double distance = ExperimentArtifact.Distance(posX, posY, sensor.X, sensor.Y);
                double returnDistance = ExperimentArtifact.Distance(sensor.X, sensor.Y, artifact.BaseX, artifact.BaseY);
                double moveEnergy = distance * settings.WcvMoveCostJPerMeter;
                double returnEnergy = returnDistance * settings.WcvMoveCostJPerMeter;
                if (wcvEnergy < moveEnergy + returnEnergy)
                {
                    ExperimentTaskRecord skipped = RecordSkippedTask(request, dispatchTime, currentTime, "WCV 能量不足");
                    AccumulateMissionTask(mission, skipped);
                    summary.FailedOrLateTasks++;
                    break;
                }

                wcvEnergy -= moveEnergy;
                summary.MoveEnergyJ += moveEnergy;
                summary.MovementDistanceMeters += distance;
                mission.MoveEnergyJ += moveEnergy;
                mission.DistanceMeters += distance;
                AdvanceTo(currentTime + distance / settings.WcvSpeedMetersPerSecond, null);
                if (stopForFirstDeath)
                    break;

                order++;
                mission.RouteNodeIds.Add(request.NodeId);
                double arrivalTime = currentTime;
                double chargeStartTime = currentTime;
                double beforeEnergy = sensor.EnergyJ;
                string failReason = "";
                bool deadlineOk = arrivalTime <= request.DeadlineSeconds + Epsilon;
                bool success = sensor.Alive && deadlineOk;

                if (!deadlineOk)
                {
                    failReason = "逾期抵達";
                    summary.FailedOrLateTasks++;
                }

                ChargingContext context = new ChargingContext();
                context.NodeId = request.NodeId;
                context.ChargeRateJPerSecond = settings.WcvChargeRateJPerSecond;
                context.WcvEnergyJ = wcvEnergy;
                context.DeliveredEnergyJ = 0.0;

                int chargeSafety = 0;
                while (sensor.Alive && sensor.EnergyJ < sensor.CapacityJ - 1e-6 &&
                    context.WcvEnergyJ > 1e-9 && currentTime < settings.SimulationTimeSeconds - Epsilon &&
                    !stopForFirstDeath && chargeSafety < 10000)
                {
                    chargeSafety++;
                    double netRate = settings.WcvChargeRateJPerSecond - sensor.ConsumeRateJPerSecond;
                    if (netRate <= 1e-9)
                    {
                        failReason = "充電速率低於節點耗能率";
                        success = false;
                        break;
                    }

                    double timeToFull = (sensor.CapacityJ - sensor.EnergyJ) / netRate;
                    double timeToEmptyWcv = context.WcvEnergyJ / settings.WcvChargeRateJPerSecond;
                    double next = currentTime + Math.Max(0.001, Math.Min(timeToFull, timeToEmptyWcv));
                    AdvanceTo(Math.Min(next, settings.SimulationTimeSeconds), context);
                    if (context.WcvEnergyJ <= 1e-9 && sensor.EnergyJ < sensor.CapacityJ - 1e-6)
                    {
                        failReason = "WCV 充電能量耗盡";
                        success = false;
                        break;
                    }
                }

                wcvEnergy = context.WcvEnergyJ;
                double afterEnergy = sensor.EnergyJ;
                double chargeEndTime = currentTime;
                if (!sensor.Alive)
                {
                    failReason = "充電前或充電中死亡";
                    success = false;
                }
                if (sensor.EnergyJ >= sensor.CapacityJ - 1e-5)
                    sensor.EnergyJ = sensor.CapacityJ;
                if (sensor.Alive &&
                    sensor.EnergyJ < sensor.CapacityJ - 1e-5 &&
                    currentTime >= settings.SimulationTimeSeconds - Epsilon)
                {
                    success = false;
                    failReason = "模擬結束前未充飽";
                }
                if (sensor.Alive)
                {
                    RefreshBprSTableEntry(request.NodeId, "charged", true);
                    BprSTableEntry chargedEntry = GetOrCreateBprSTableEntry(request.NodeId);
                    chargedEntry.LastChargedTimeSeconds = currentTime;
                }

                ExperimentTaskRecord record = new ExperimentTaskRecord();
                record.RunIndex = artifact.RunIndex;
                record.Seed = artifact.Seed;
                record.Algorithm = algorithm;
                record.ArtifactHash = artifact.ArtifactHash;
                record.MissionId = missionId;
                record.TaskOrder = order;
                record.NodeId = request.NodeId;
                record.TaskSource = request.IsProactive ? "proactive" : "request";
                record.IsProactive = request.IsProactive;
                record.ProactiveReason = request.ProactiveReason ?? "";
                record.RequestTimeSeconds = request.RequestTimeSeconds;
                record.DeadlineSeconds = request.DeadlineSeconds;
                record.DispatchTimeSeconds = dispatchTime;
                record.ArrivalTimeSeconds = arrivalTime;
                record.WaitSeconds = Math.Max(0.0, arrivalTime - request.RequestTimeSeconds);
                record.ChargeStartSeconds = chargeStartTime;
                record.ChargeEndSeconds = chargeEndTime;
                record.EnergyBeforeJ = beforeEnergy;
                record.EnergyAfterJ = afterEnergy;
                record.InternalEnergyBeforeNj = beforeEnergy * 1000000000.0;
                record.InternalEnergyAfterNj = afterEnergy * 1000000000.0;
                record.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
                PopulateTaskRecordRoutingFields(record, request.NodeId, request);
                record.InternalRateNjPerTick = sensor.ConsumeRateJPerSecond * 1000000000.0 * 0.01;
                record.DeliveredEnergyJ = context.DeliveredEnergyJ;
                record.DistanceFromPreviousMeters = distance;
                record.Success = success && sensor.Alive && String.IsNullOrWhiteSpace(failReason);
                record.FailureReason = record.Success ? "" : failReason;
                record.WcvEnergyAfterJ = wcvEnergy;
                AddTaskRecord(record);
                AccumulateMissionTask(mission, record);

                summary.DeliveredEnergyJ += context.DeliveredEnergyJ;
                mission.DeliveredEnergyJ += context.DeliveredEnergyJ;
                if (record.Success)
                {
                    summary.SuccessfulCharges++;
                    summary.TotalWaitSeconds += record.WaitSeconds;
                }
                else if (String.IsNullOrWhiteSpace(failReason))
                {
                    summary.FailedOrLateTasks++;
                }

                CompleteRequestForTask(request);
                posX = sensor.X;
                posY = sensor.Y;
            }

            double backDistance = ExperimentArtifact.Distance(posX, posY, artifact.BaseX, artifact.BaseY);
            double backMoveEnergy = backDistance * settings.WcvMoveCostJPerMeter;
            if (wcvEnergy >= backMoveEnergy)
            {
                wcvEnergy -= backMoveEnergy;
                summary.MoveEnergyJ += backMoveEnergy;
                summary.MovementDistanceMeters += backDistance;
                mission.MoveEnergyJ += backMoveEnergy;
                mission.DistanceMeters += backDistance;
                AdvanceTo(currentTime + backDistance / settings.WcvSpeedMetersPerSecond, null);
            }
            else
            {
                summary.FailedOrLateTasks++;
                mission.FailedCount++;
            }

            mission.ReturnTimeSeconds = currentTime;
            mission.PacketSent = summary.PacketSent - startPacketSent;
            mission.PacketReceived = summary.PacketReceived - startPacketReceived;
            mission.PacketLost = summary.PacketLost - startPacketLost;
            mission.RoutingFailedPacketLost = summary.RoutingFailedPacketLost - startRoutingFailedPacketLost;
            mission.AverageWaitSeconds = mission.SuccessfulCharges > 0
                ? mission.TotalWaitSeconds / mission.SuccessfulCharges
                : 0.0;
            if (csvWriter != null)
                csvWriter.WriteMission(mission);
            List<int> releasedMissionNodeIds = new List<int>();
            if (plannedMissionNodeIds != null)
            {
                foreach (int nodeId in plannedMissionNodeIds)
                    releasedMissionNodeIds.Add(nodeId);
            }
            plannedMissionNodeIds = null;
            for (int i = 0; i < releasedMissionNodeIds.Count; i++)
                RefreshBprSTableEntry(releasedMissionNodeIds[i], "mission_released", true);
        }

        private void AccumulateMissionTask(MissionRecord mission, ExperimentTaskRecord record)
        {
            if (mission == null || record == null)
                return;

            mission.NodeCount++;
            if (record.IsProactive)
            {
                mission.ProactiveCount++;
                summary.ProactiveTaskCount++;
            }
            else
            {
                mission.RequestCount++;
            }

            if (record.Success)
            {
                mission.SuccessfulCharges++;
                mission.TotalWaitSeconds += record.WaitSeconds;
            }
            else
            {
                mission.FailedCount++;
            }
        }

        private ExperimentTaskRecord RecordSkippedTask(ChargingRequest request, double dispatchTime, double time, string reason)
        {
            ExperimentTaskRecord record = new ExperimentTaskRecord();
            record.RunIndex = artifact.RunIndex;
            record.Seed = artifact.Seed;
            record.Algorithm = algorithm;
            record.ArtifactHash = artifact.ArtifactHash;
            record.MissionId = missionId;
            record.TaskOrder = 0;
            record.NodeId = request.NodeId;
            record.TaskSource = request.IsProactive ? "proactive" : "request";
            record.IsProactive = request.IsProactive;
            record.ProactiveReason = request.ProactiveReason ?? "";
            record.RequestTimeSeconds = request.RequestTimeSeconds;
            record.DeadlineSeconds = request.DeadlineSeconds;
            record.DispatchTimeSeconds = dispatchTime;
            record.ArrivalTimeSeconds = time;
            record.WaitSeconds = Math.Max(0.0, time - request.RequestTimeSeconds);
            record.ChargeStartSeconds = time;
            record.ChargeEndSeconds = time;
            record.EnergyBeforeJ = sensors[request.NodeId].EnergyJ;
            record.EnergyAfterJ = sensors[request.NodeId].EnergyJ;
            record.InternalEnergyBeforeNj = record.EnergyBeforeJ * 1000000000.0;
            record.InternalEnergyAfterNj = record.EnergyAfterJ * 1000000000.0;
            record.ConsumeRateJPerSecond = sensors[request.NodeId].ConsumeRateJPerSecond;
            PopulateTaskRecordRoutingFields(record, request.NodeId, request);
            record.InternalRateNjPerTick = record.ConsumeRateJPerSecond * 1000000000.0 * 0.01;
            record.DeliveredEnergyJ = 0.0;
            record.DistanceFromPreviousMeters = 0.0;
            record.Success = false;
            record.FailureReason = reason;
            record.WcvEnergyAfterJ = 0.0;
            AddTaskRecord(record);
            return record;
        }

        private void AddTaskRecord(ExperimentTaskRecord record)
        {
            totalTaskRecordCount++;
            AccumulateRunTaskStats(record);
            if (csvWriter != null)
                csvWriter.WriteTask(record);
        }

        private void PopulateTaskRecordRoutingFields(ExperimentTaskRecord record, int nodeId, ChargingRequest request)
        {
            if (record == null)
                return;

            if (request != null && request.EffectiveConsumeRateJPerSecond > 0.0)
            {
                record.BaseConsumeRateJPerSecond = request.BaseConsumeRateJPerSecond;
                record.EffectiveConsumeRateJPerSecond = request.EffectiveConsumeRateJPerSecond;
                record.RoutingLoadJPerSecond = request.RoutingLoadJPerSecond;
                record.RoutingTxLoadJPerSecond = request.RoutingTxLoadJPerSecond;
                record.RoutingRxLoadJPerSecond = request.RoutingRxLoadJPerSecond;
                record.RoutingSubtreeSize = request.RoutingSubtreeSize;
                record.ExpectedRoutingForwardPacketsPerSecond = request.ExpectedRoutingForwardPacketsPerSecond;
                return;
            }

            if (nodeId <= 0 || nodeId >= sensors.Length)
                return;

            SensorState sensor = sensors[nodeId];
            record.BaseConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            record.RoutingTxLoadJPerSecond = GetRoutingTxLoadJPerSecond(sensor);
            record.RoutingRxLoadJPerSecond = GetRoutingRxLoadJPerSecond(sensor);
            record.RoutingLoadJPerSecond = record.RoutingTxLoadJPerSecond + record.RoutingRxLoadJPerSecond;
            record.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond + record.RoutingLoadJPerSecond;
            record.RoutingSubtreeSize = GetRoutingSubtreeSize(nodeId);
            record.ExpectedRoutingForwardPacketsPerSecond = GetExpectedRoutingForwardPacketsPerSecond(nodeId);
        }

        private void AccumulateRunTaskStats(ExperimentTaskRecord record)
        {
            if (record == null)
                return;

            totalDeliveredEnergyForTasks += record.DeliveredEnergyJ;
            if (record.IsProactive)
            {
                proactiveTaskRecordCount++;
                totalDeliveredEnergyForProactiveTasks += record.DeliveredEnergyJ;
                if (record.NodeId > 0 && record.NodeId < sensors.Length)
                {
                    double capacity = sensors[record.NodeId].CapacityJ;
                    if (record.EnergyBeforeJ >= capacity * 0.95 - Epsilon)
                        summary.ProactiveNearFullCount++;
                    if (record.DeliveredEnergyJ >= capacity * 0.05 - Epsilon)
                        summary.MeaningfulProactiveCount++;
                }
            }

            if (record.Success)
            {
                if (servedNodeIds.Contains(record.NodeId))
                    summary.RepeatChargeCount++;
                else
                    servedNodeIds.Add(record.NodeId);
            }
        }

        private void CompleteRequestForTask(ChargingRequest request)
        {
            if (request == null || request.NodeId <= 0 || request.NodeId >= sensors.Length)
                return;

            if (!request.IsProactive)
            {
                int index = FindActiveRequestIndex(request.NodeId, request.RequestId);
                if (index < 0)
                    index = FindActiveRequestIndex(request.NodeId, -1);
                if (index >= 0)
                    activeRequests.RemoveAt(index);
            }

            sensors[request.NodeId].HasPendingRequest = HasActiveRequestForNode(request.NodeId);
            if (!request.IsProactive)
                RefreshBprSTableEntry(request.NodeId, "request_complete", true);
        }

        private int FindActiveRequestIndex(int nodeId, int requestId)
        {
            for (int i = 0; i < activeRequests.Count; i++)
            {
                ChargingRequest request = activeRequests[i];
                if (request.NodeId != nodeId)
                    continue;
                if (requestId < 0 || request.RequestId == requestId)
                    return i;
            }
            return -1;
        }

        private bool HasActiveRequestForNode(int nodeId)
        {
            return FindActiveRequestIndex(nodeId, -1) >= 0;
        }

        private HashSet<int> BuildPlannedMissionNodeSet(List<ChargingRequest> route)
        {
            HashSet<int> planned = new HashSet<int>();
            if (route == null)
                return planned;

            for (int i = 0; i < route.Count; i++)
            {
                if (route[i] != null)
                    planned.Add(route[i].NodeId);
            }
            return planned;
        }

        private void MarkProactiveNodesSelected(List<ChargingRequest> route)
        {
            if (route == null)
                return;

            for (int i = 0; i < route.Count; i++)
            {
                ChargingRequest request = route[i];
                if (request == null || !request.IsProactive)
                    continue;
                BprSTableEntry entry = GetOrCreateBprSTableEntry(request.NodeId);
                entry.LastProactiveSelectedTimeSeconds = currentTime;
            }
        }

        private bool IsNodeReservedForCurrentMission(int nodeId)
        {
            return plannedMissionNodeIds != null && plannedMissionNodeIds.Contains(nodeId);
        }

        private List<ChargingRequest> DeduplicateMissionRoute(List<ChargingRequest> route, out int duplicateCount)
        {
            duplicateCount = 0;
            if (route == null || route.Count <= 1)
                return route ?? new List<ChargingRequest>();

            List<ChargingRequest> deduplicated = new List<ChargingRequest>();
            Dictionary<int, int> indexByNodeId = new Dictionary<int, int>();
            for (int i = 0; i < route.Count; i++)
            {
                ChargingRequest request = route[i];
                if (request == null)
                    continue;

                int existingIndex;
                if (!indexByNodeId.TryGetValue(request.NodeId, out existingIndex))
                {
                    indexByNodeId[request.NodeId] = deduplicated.Count;
                    deduplicated.Add(request);
                    continue;
                }

                duplicateCount++;
                ChargingRequest existing = deduplicated[existingIndex];
                if (HasHigherTaskSourcePriority(request, existing))
                    deduplicated[existingIndex] = request;
            }

            return deduplicated;
        }

        private bool HasHigherTaskSourcePriority(ChargingRequest candidate, ChargingRequest existing)
        {
            if (candidate == null)
                return false;
            if (existing == null)
                return true;

            if (existing.IsProactive && !candidate.IsProactive)
                return true;
            if (!existing.IsProactive && candidate.IsProactive)
                return false;

            if (candidate.DeadlineSeconds < existing.DeadlineSeconds - Epsilon)
                return true;
            if (Math.Abs(candidate.DeadlineSeconds - existing.DeadlineSeconds) <= Epsilon &&
                candidate.RequestTimeSeconds < existing.RequestTimeSeconds - Epsilon)
                return true;
            return false;
        }

        private List<ChargingRequest> BuildMissionRoute()
        {
            int maxTask = GetMissionTaskLimit();
            List<ChargingRequest> pool = new List<ChargingRequest>();
            for (int i = 0; i < activeRequests.Count; i++)
            {
                if (sensors[activeRequests[i].NodeId].Alive)
                    pool.Add(activeRequests[i].Clone());
            }

            if (algorithm == "NJF_ROUTE_ZHENG_BPR_LIMITED")
                return BuildRouteAwareZhengBpr(pool, maxTask, true);
            if (algorithm == "NJF_ROUTE_ZHENG_BPR_EXTENDED")
                return BuildRouteAwareZhengBpr(pool, maxTask, false);
            if (algorithm == "NJF_ROUTE_YU_BPR_LIMITED")
                return BuildRouteAwareYuBpr(pool, maxTask, true);
            if (algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
                return BuildRouteAwareYuBpr(pool, maxTask, false);

            if (algorithm == "NJF_ZHENG_BPR")
            {
                List<ChargingRequest> cplist = BuildZhengBprCplist(
                    pool,
                    maxTask,
                    BprProactiveSelectionMode.Deterministic,
                    false);
                return BuildNearestRoute(cplist, maxTask);
            }
            if (algorithm == "NJF_YU_BPR")
            {
                List<ChargingRequest> cplist = BuildYuBprCplist(
                    pool,
                    maxTask,
                    false,
                    YuProactiveSelectionMode.Deterministic);
                return BuildNearestRoute(cplist, maxTask);
            }

            if (algorithm == "FUZZY")
                AddProactiveCandidates(pool, maxTask);

            if (pool.Count == 0)
                return pool;

            if (algorithm == "EDF")
                return TakeSorted(pool, maxTask, CompareByDeadline);
            if (algorithm == "NJF")
                return BuildNearestRoute(pool, maxTask);
            if (algorithm == "TADP_LIN")
                return BuildCompositeRoute(pool, maxTask, 0.50, 0.50, 0.00);
            if (algorithm == "RCSS")
                return BuildCompositeRoute(pool, maxTask, 0.20, 0.25, 0.55);
            if (algorithm == "FUZZY")
                return BuildFuzzyRoute(pool, maxTask);
            if (algorithm == "GENE")
                return BuildGeneticRoute(pool, maxTask);
            if (algorithm == "PSO")
                return BuildPsoRoute(pool, maxTask);
            if (algorithm == "Cuckoo")
                return BuildCuckooRoute(pool, maxTask);

            return BuildNearestRoute(pool, maxTask);
        }

        private int GetMissionTaskLimit()
        {
            if (!settings.DynamicNmaxTask)
                return Math.Max(1, settings.NmaxTask);

            double avgEnergyPerTask = Math.Max(1.0, settings.InitialEnergyJ * 0.7);
            int energyLimited = (int)Math.Floor(settings.WcvCapacityJ / avgEnergyPerTask);
            return Math.Max(1, Math.Min(settings.NmaxTask, energyLimited));
        }

        private bool UsesBprBottleneckCandidates()
        {
            return algorithm == "NJF_ZHENG_BPR" ||
                algorithm == "NJF_YU_BPR" ||
                algorithm == "NJF_ROUTE_ZHENG_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_ZHENG_BPR_EXTENDED" ||
                algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_YU_BPR_EXTENDED";
        }

        private bool HasBprBottleneckCandidate()
        {
            if (!UsesBprBottleneckCandidates())
                return false;

            int maxTask = GetMissionTaskLimit();
            if (algorithm == "NJF_YU_BPR" ||
                algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
            {
                List<YuPredictedInterval> intervals = BuildYuPredictedIntervals(maxTask, new HashSet<int>());
                return BuildYuDangerWindows(intervals, maxTask).Count > 0;
            }

            List<BprPredictedRequest> predictedRequests = BuildBprPredictedRequests(maxTask, new HashSet<int>());
            return BuildBprSlidingWindows(predictedRequests, maxTask).Count > 0;
        }

        private double FindNextBprBottleneckCandidateTime()
        {
            if (!UsesBprBottleneckCandidates())
                return Double.PositiveInfinity;

            int maxTask = GetMissionTaskLimit();
            if (algorithm == "NJF_YU_BPR" ||
                algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
            {
                return FindNextYuBprBottleneckCandidateTime();
            }

            List<BprPredictedRequest> predictedRequests = BuildBprPredictedRequests(maxTask, new HashSet<int>());
            List<BprWindow> windows = BuildBprSlidingWindows(predictedRequests, maxTask);
            if (windows.Count == 0)
                return Double.PositiveInfinity;

            return Math.Max(currentTime, windows[0].WindowStartSeconds - EstimateBprTjobSeconds(maxTask));
        }

        private double FindNextYuBprBottleneckCandidateTime()
        {
            int maxTask = GetMissionTaskLimit();
            List<YuPredictedInterval> intervals = BuildYuPredictedIntervals(maxTask, new HashSet<int>());
            List<YuDangerWindow> windows = BuildYuDangerWindows(intervals, maxTask);
            if (windows.Count == 0)
                return Double.PositiveInfinity;

            return Math.Max(currentTime, windows[0].WindowStartSeconds - EstimateBprTjobSeconds(maxTask));
        }

        private void AddProactiveCandidates(List<ChargingRequest> pool, int maxTask)
        {
            if (pool.Count >= maxTask)
                return;

            // FUZZY keeps the legacy risk-based proactive supplement.
            HashSet<int> used = new HashSet<int>();
            for (int i = 0; i < pool.Count; i++)
                used.Add(pool[i].NodeId);

            List<ChargingRequest> candidates = new List<ChargingRequest>();
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive || sensor.HasPendingRequest || used.Contains(id) || HasActiveRequestForNode(id))
                    continue;

                double threshold = GetRequestThresholdJ(sensor);
                double effectiveRate = GetEffectiveConsumeRateJPerSecond(sensor);
                if (effectiveRate <= 1e-12)
                    continue;
                double timeToRequest = Math.Max(0.0, (sensor.EnergyJ - threshold) / effectiveRate);
                double timeToDeath = Math.Max(0.0, sensor.EnergyJ / effectiveRate);
                if (timeToRequest > settings.TreqSeconds * 1.5 && sensor.EnergyJ > settings.InitialEnergyJ * 0.35)
                    continue;

                ChargingRequest proactive = new ChargingRequest();
                proactive.RequestId = -id;
                proactive.NodeId = id;
                proactive.RequestTimeSeconds = currentTime;
                proactive.DeadlineSeconds = currentTime + timeToDeath;
                proactive.RequestEnergyJ = sensor.EnergyJ;
                PopulateChargingRequestRoutingFields(proactive, sensor);
                proactive.IsProactive = true;
                proactive.ProactiveReason = "FUZZY_RISK";
                proactive.CriticalDensity = ComputeCriticalNodeDensity(id);
                candidates.Add(proactive);
            }

            candidates.Sort(delegate (ChargingRequest a, ChargingRequest b)
            {
                return a.DeadlineSeconds.CompareTo(b.DeadlineSeconds);
            });

            for (int i = 0; i < candidates.Count && pool.Count < maxTask; i++)
                pool.Add(candidates[i]);
        }

        private List<ChargingRequest> BuildRouteAwareZhengBpr(List<ChargingRequest> requiredRequests, int maxTask, bool enforceTaskLimit)
        {
            List<ChargingRequest> clist = enforceTaskLimit
                ? BuildNearestRoute(requiredRequests, maxTask)
                : new List<ChargingRequest>();
            if (!enforceTaskLimit)
            {
                for (int i = 0; i < requiredRequests.Count; i++)
                    clist.Add(requiredRequests[i].Clone());
            }

            List<ChargingRequest> cplist = BuildZhengBprCplist(
                clist,
                maxTask,
                BprProactiveSelectionMode.RouteInsertionCost,
                !enforceTaskLimit);
            return BuildNearestRoute(cplist, enforceTaskLimit ? maxTask : cplist.Count);
        }

        private List<ChargingRequest> BuildRouteAwareYuBpr(List<ChargingRequest> requiredRequests, int maxTask, bool enforceTaskLimit)
        {
            List<ChargingRequest> clist = enforceTaskLimit
                ? BuildNearestRoute(requiredRequests, maxTask)
                : new List<ChargingRequest>();
            if (!enforceTaskLimit)
            {
                for (int i = 0; i < requiredRequests.Count; i++)
                    clist.Add(requiredRequests[i].Clone());
            }

            List<ChargingRequest> cplist = BuildYuBprCplist(
                clist,
                maxTask,
                !enforceTaskLimit,
                YuProactiveSelectionMode.RouteInsertionCost);
            return BuildNearestRoute(cplist, enforceTaskLimit ? maxTask : cplist.Count);
        }

        private double GetBprPredictionHorizonEndSeconds(int maxTask)
        {
            double horizon = ResolveProactivePredictionHorizonSeconds(maxTask);
            return Math.Min(settings.SimulationTimeSeconds, currentTime + Math.Max(1.0, horizon));
        }

        private double GetYuPredictionHorizonEndSeconds(int maxTask)
        {
            double horizon = ResolveProactivePredictionHorizonSeconds(maxTask);
            return Math.Min(settings.SimulationTimeSeconds, currentTime + Math.Max(1.0, horizon));
        }

        private bool IsPredictionEligibleNode(int nodeId, HashSet<int> reservedNodeIds, out BprSTableEntry entry)
        {
            entry = null;
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return false;
            if (reservedNodeIds != null && reservedNodeIds.Contains(nodeId))
                return false;
            if (bprSTableByNodeId == null || !bprSTableByNodeId.TryGetValue(nodeId, out entry) || entry == null)
                return false;

            SensorState sensor = sensors[nodeId];
            if (sensor == null || !sensor.Alive || sensor.HasPendingRequest || HasActiveRequestForNode(nodeId))
                return false;
            if (!entry.IsAlive || entry.IsPendingRequest || entry.IsScheduledInCurrentMission)
                return false;
            if (IsNodeReservedForCurrentMission(nodeId))
                return false;
            if (sensor.EnergyJ >= sensor.CapacityJ * settings.ProactiveCandidateMaxEnergyRatio - Epsilon)
                return false;

            double cooldownSeconds = ResolveProactiveCooldownSeconds();
            if (currentTime - entry.LastChargedTimeSeconds < cooldownSeconds - Epsilon)
                return false;
            if (currentTime - entry.LastProactiveSelectedTimeSeconds < cooldownSeconds - Epsilon)
                return false;

            return true;
        }

        private List<BprPredictionSegment> BuildBprPredictionTimeline(
            int nodeId,
            double horizonEnd,
            HashSet<int> reservedNodeIds)
        {
            List<BprPredictionSegment> timeline = new List<BprPredictionSegment>();
            BprSTableEntry entry;
            if (!IsPredictionEligibleNode(nodeId, reservedNodeIds, out entry))
                return timeline;
            if (horizonEnd <= currentTime + Epsilon)
                return timeline;

            SensorState sensor = sensors[nodeId];
            List<RateChangeTemplate> futureRateChanges = new List<RateChangeTemplate>();
            List<double> breakpoints = new List<double>();
            AddUniqueBreakpoint(breakpoints, currentTime);
            AddUniqueBreakpoint(breakpoints, horizonEnd);
            for (int i = 0; i < artifact.RateChanges.Count; i++)
            {
                RateChangeTemplate change = artifact.RateChanges[i];
                if (change.NodeId != nodeId)
                    continue;
                if (change.TimeSeconds <= currentTime + Epsilon ||
                    change.TimeSeconds > horizonEnd + Epsilon)
                    continue;
                futureRateChanges.Add(change);
                AddUniqueBreakpoint(breakpoints, change.TimeSeconds);
            }
            breakpoints.Sort();
            futureRateChanges.Sort(delegate (RateChangeTemplate a, RateChangeTemplate b)
            {
                int compare = a.TimeSeconds.CompareTo(b.TimeSeconds);
                if (compare != 0)
                    return compare;
                return a.NodeId.CompareTo(b.NodeId);
            });

            double predictedEnergy = Double.IsNaN(entry.EnergyJ) ? sensor.EnergyJ : entry.EnergyJ;
            double predictedRateScale = sensor.RateScale;
            int rateChangeIndex = 0;
            for (int i = 0; i < breakpoints.Count - 1; i++)
            {
                double segmentStart = breakpoints[i];
                double segmentEnd = breakpoints[i + 1];
                while (rateChangeIndex < futureRateChanges.Count &&
                    futureRateChanges[rateChangeIndex].TimeSeconds <= segmentStart + Epsilon)
                {
                    predictedRateScale *= futureRateChanges[rateChangeIndex].Multiplier;
                    rateChangeIndex++;
                }

                if (segmentEnd <= segmentStart + Epsilon)
                    continue;

                double effectiveRate = ComputePredictedEffectiveConsumeRateJPerSecond(nodeId, predictedRateScale);
                double threshold = GetPredictedRequestThresholdJ(sensor, effectiveRate, predictedRateScale);
                double duration = segmentEnd - segmentStart;
                double endEnergy = effectiveRate <= 1e-12
                    ? predictedEnergy
                    : predictedEnergy - effectiveRate * duration;

                BprPredictionSegment segment = new BprPredictionSegment();
                segment.NodeId = nodeId;
                segment.StartTimeSeconds = segmentStart;
                segment.EndTimeSeconds = segmentEnd;
                segment.StartEnergyJ = predictedEnergy;
                segment.EndEnergyJ = endEnergy;
                segment.EffectiveConsumeRateJPerSecond = effectiveRate;
                segment.RequestTimeSeconds = Double.PositiveInfinity;
                segment.DeathTimeSeconds = Double.PositiveInfinity;
                segment.SegmentReason = Math.Abs(segmentStart - currentTime) <= Epsilon ? "current" : "rate_change";

                if (effectiveRate > 1e-12)
                {
                    if (predictedEnergy > threshold + Epsilon && endEnergy <= threshold + Epsilon)
                    {
                        segment.CrossesRequestThreshold = true;
                        segment.RequestTimeSeconds = segmentStart +
                            Math.Max(0.0, (predictedEnergy - threshold) / effectiveRate);
                    }
                    if (predictedEnergy > Epsilon && endEnergy <= Epsilon)
                    {
                        segment.CrossesDeathThreshold = true;
                        segment.DeathTimeSeconds = segmentStart +
                            Math.Max(0.0, predictedEnergy / effectiveRate);
                    }
                }

                timeline.Add(segment);
                predictedEnergy = endEnergy;
            }

            return timeline;
        }

        private static void AddUniqueBreakpoint(List<double> breakpoints, double value)
        {
            for (int i = 0; i < breakpoints.Count; i++)
            {
                if (Math.Abs(breakpoints[i] - value) <= Epsilon)
                    return;
            }
            breakpoints.Add(value);
        }

        private List<BprPredictedRequest> BuildBprPredictedRequests(int maxTask, HashSet<int> reservedNodeIds)
        {
            List<BprPredictedRequest> predictedRequests = new List<BprPredictedRequest>();
            double horizonEnd = GetBprPredictionHorizonEndSeconds(maxTask);
            for (int nodeId = 1; nodeId < sensors.Length; nodeId++)
            {
                BprSTableEntry entry;
                bool reserved = reservedNodeIds != null && reservedNodeIds.Contains(nodeId);
                if (!IsPredictionEligibleNode(nodeId, reservedNodeIds, out entry))
                    continue;

                List<BprPredictionSegment> timeline = BuildBprPredictionTimeline(nodeId, horizonEnd, reservedNodeIds);
                BprPredictionSegment requestSegment = null;
                double deathTime = Double.PositiveInfinity;
                for (int i = 0; i < timeline.Count; i++)
                {
                    if (timeline[i].CrossesDeathThreshold && Double.IsPositiveInfinity(deathTime))
                        deathTime = timeline[i].DeathTimeSeconds;
                    if (requestSegment == null && timeline[i].CrossesRequestThreshold)
                        requestSegment = timeline[i];
                }
                if (requestSegment == null)
                    continue;

                BprPredictedRequest predicted = new BprPredictedRequest();
                predicted.NodeId = nodeId;
                predicted.RequestTimeSeconds = requestSegment.RequestTimeSeconds;
                predicted.DeathTimeSeconds = deathTime;
                predicted.EnergyAtPredictionStartJ = timeline.Count == 0 ? entry.EnergyJ : timeline[0].StartEnergyJ;
                predicted.EffectiveConsumeRateJPerSecond = requestSegment.EffectiveConsumeRateJPerSecond;
                predicted.SlackSeconds = deathTime - requestSegment.RequestTimeSeconds;
                predicted.RouteInsertionCost = 0.0;
                predicted.IsReserved = reserved;
                predicted.IsPendingRequest = entry.IsPendingRequest;
                predicted.IsScheduledInCurrentMission = entry.IsScheduledInCurrentMission;
                predictedRequests.Add(predicted);
            }

            predictedRequests.Sort(CompareBprPredictedRequestByRequestTime);
            return predictedRequests;
        }

        private static int CompareBprPredictedRequestByRequestTime(BprPredictedRequest a, BprPredictedRequest b)
        {
            int compare = a.RequestTimeSeconds.CompareTo(b.RequestTimeSeconds);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private List<BprWindow> BuildBprSlidingWindows(List<BprPredictedRequest> predictedRequests, int maxTask)
        {
            List<BprWindow> windows = new List<BprWindow>();
            if (predictedRequests == null || predictedRequests.Count == 0)
                return windows;

            double tjob = EstimateBprTjobSeconds(maxTask);
            for (int i = 0; i < predictedRequests.Count; i++)
            {
                double windowStart = predictedRequests[i].RequestTimeSeconds;
                double windowEnd = windowStart + tjob;
                BprWindow window = new BprWindow();
                window.WindowStartSeconds = windowStart;
                window.WindowEndSeconds = windowEnd;
                window.Requests = new List<BprPredictedRequest>();
                for (int j = 0; j < predictedRequests.Count; j++)
                {
                    BprPredictedRequest request = predictedRequests[j];
                    if (request.RequestTimeSeconds >= windowStart - Epsilon &&
                        request.RequestTimeSeconds <= windowEnd + Epsilon)
                    {
                        window.Requests.Add(request);
                    }
                }
                window.BottleneckCount = window.Requests.Count;
                window.OverflowCount = Math.Max(0, window.BottleneckCount - maxTask);
                if (window.OverflowCount > 0)
                    windows.Add(window);
            }

            windows.Sort(CompareBprWindowBySeverity);
            return windows;
        }

        private static int CompareBprWindowBySeverity(BprWindow a, BprWindow b)
        {
            int compare = b.OverflowCount.CompareTo(a.OverflowCount);
            if (compare != 0)
                return compare;
            compare = a.WindowStartSeconds.CompareTo(b.WindowStartSeconds);
            if (compare != 0)
                return compare;
            return b.BottleneckCount.CompareTo(a.BottleneckCount);
        }

        private List<YuPredictionSegment> BuildYuPredictionTimeline(
            int nodeId,
            double horizonEnd,
            HashSet<int> reservedNodeIds)
        {
            List<YuPredictionSegment> timeline = new List<YuPredictionSegment>();
            List<BprPredictionSegment> bprTimeline = BuildBprPredictionTimeline(nodeId, horizonEnd, reservedNodeIds);
            for (int i = 0; i < bprTimeline.Count; i++)
            {
                BprPredictionSegment source = bprTimeline[i];
                YuPredictionSegment segment = new YuPredictionSegment();
                segment.NodeId = source.NodeId;
                segment.StartTimeSeconds = source.StartTimeSeconds;
                segment.EndTimeSeconds = source.EndTimeSeconds;
                segment.StartEnergyJ = source.StartEnergyJ;
                segment.EndEnergyJ = source.EndEnergyJ;
                segment.EffectiveConsumeRateJPerSecond = source.EffectiveConsumeRateJPerSecond;
                segment.CrossesRequestThreshold = source.CrossesRequestThreshold;
                segment.RequestTimeSeconds = source.RequestTimeSeconds;
                segment.CrossesDeathThreshold = source.CrossesDeathThreshold;
                segment.DeathTimeSeconds = source.DeathTimeSeconds;
                segment.SegmentReason = source.SegmentReason;
                timeline.Add(segment);
            }
            return timeline;
        }

        private List<YuPredictedInterval> BuildYuPredictedIntervals(int maxTask, HashSet<int> reservedNodeIds)
        {
            List<YuPredictedInterval> intervals = new List<YuPredictedInterval>();
            double horizonEnd = GetYuPredictionHorizonEndSeconds(maxTask);
            for (int nodeId = 1; nodeId < sensors.Length; nodeId++)
            {
                BprSTableEntry entry;
                bool reserved = reservedNodeIds != null && reservedNodeIds.Contains(nodeId);
                if (!IsPredictionEligibleNode(nodeId, reservedNodeIds, out entry))
                    continue;

                List<YuPredictionSegment> timeline = BuildYuPredictionTimeline(nodeId, horizonEnd, reservedNodeIds);
                YuPredictionSegment requestSegment = null;
                double deathTime = Double.PositiveInfinity;
                for (int i = 0; i < timeline.Count; i++)
                {
                    if (timeline[i].CrossesDeathThreshold && Double.IsPositiveInfinity(deathTime))
                        deathTime = timeline[i].DeathTimeSeconds;
                    if (requestSegment == null && timeline[i].CrossesRequestThreshold)
                        requestSegment = timeline[i];
                }
                if (requestSegment == null)
                    continue;

                double uncertainty = ResolveYuPredictionUncertaintySeconds(requestSegment, maxTask);
                double center = requestSegment.RequestTimeSeconds;
                YuPredictedInterval interval = new YuPredictedInterval();
                interval.NodeId = nodeId;
                interval.CenterRequestTimeSeconds = center;
                interval.IntervalStartSeconds = Math.Max(currentTime, center - uncertainty);
                interval.IntervalEndSeconds = Math.Min(horizonEnd, center + uncertainty);
                interval.EarliestDeathTimeSeconds = deathTime;
                interval.LatestSafeServiceTimeSeconds = Double.IsPositiveInfinity(deathTime)
                    ? interval.IntervalEndSeconds
                    : deathTime;
                interval.EnergyAtPredictionStartJ = timeline.Count == 0 ? entry.EnergyJ : timeline[0].StartEnergyJ;
                interval.EffectiveConsumeRateJPerSecond = requestSegment.EffectiveConsumeRateJPerSecond;
                interval.UncertaintySeconds = uncertainty;
                interval.SlackSeconds = interval.LatestSafeServiceTimeSeconds - center;
                interval.RouteInsertionCost = 0.0;
                interval.IsReserved = reserved;
                interval.IsPendingRequest = entry.IsPendingRequest;
                interval.IsScheduledInCurrentMission = entry.IsScheduledInCurrentMission;
                interval.IsAlive = entry.IsAlive;
                intervals.Add(interval);
            }

            intervals.Sort(CompareYuPredictedIntervalByStart);
            return intervals;
        }

        private static int CompareYuPredictedIntervalByStart(YuPredictedInterval a, YuPredictedInterval b)
        {
            int compare = a.IntervalStartSeconds.CompareTo(b.IntervalStartSeconds);
            if (compare != 0)
                return compare;
            compare = a.CenterRequestTimeSeconds.CompareTo(b.CenterRequestTimeSeconds);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private List<YuDangerWindow> BuildYuDangerWindows(List<YuPredictedInterval> intervals, int maxTask)
        {
            List<YuDangerWindow> windows = new List<YuDangerWindow>();
            if (intervals == null || intervals.Count == 0)
                return windows;

            double windowSize = ResolveYuDangerWindowSeconds(maxTask);
            int kStar = ResolveYuDangerThresholdK(maxTask);
            List<double> windowStarts = new List<double>();
            for (int i = 0; i < intervals.Count; i++)
            {
                AddUniqueBreakpoint(windowStarts, intervals[i].IntervalStartSeconds);
                AddUniqueBreakpoint(windowStarts, intervals[i].CenterRequestTimeSeconds);
            }
            windowStarts.Sort();

            for (int i = 0; i < windowStarts.Count; i++)
            {
                double windowStart = windowStarts[i];
                double windowEnd = windowStart + windowSize;
                YuDangerWindow window = new YuDangerWindow();
                window.WindowStartSeconds = windowStart;
                window.WindowEndSeconds = windowEnd;
                window.KStar = kStar;
                window.OverlappingIntervals = new List<YuPredictedInterval>();
                for (int j = 0; j < intervals.Count; j++)
                {
                    YuPredictedInterval interval = intervals[j];
                    if (interval.IntervalEndSeconds >= windowStart - Epsilon &&
                        interval.IntervalStartSeconds <= windowEnd + Epsilon)
                    {
                        window.OverlappingIntervals.Add(interval);
                    }
                }
                window.DangerCount = window.OverlappingIntervals.Count;
                window.RemovalNeededCount = Math.Max(0, window.DangerCount - (kStar - 1));
                if (window.RemovalNeededCount > 0)
                    windows.Add(window);
            }

            windows.Sort(CompareYuDangerWindowBySeverity);
            return windows;
        }

        private static int CompareYuDangerWindowBySeverity(YuDangerWindow a, YuDangerWindow b)
        {
            int compare = b.RemovalNeededCount.CompareTo(a.RemovalNeededCount);
            if (compare != 0)
                return compare;
            compare = b.DangerCount.CompareTo(a.DangerCount);
            if (compare != 0)
                return compare;
            compare = a.WindowStartSeconds.CompareTo(b.WindowStartSeconds);
            if (compare != 0)
                return compare;
            return a.WindowEndSeconds.CompareTo(b.WindowEndSeconds);
        }

        private List<ChargingRequest> BuildZhengBprCplist(
            List<ChargingRequest> clist,
            int maxTask,
            BprProactiveSelectionMode selectionMode,
            bool allowCapacityOverflow)
        {
            maxTask = Math.Max(1, maxTask);
            List<ChargingRequest> cplist = new List<ChargingRequest>();
            HashSet<int> reservedNodeIds = new HashSet<int>();
            if (clist != null)
            {
                for (int i = 0; i < clist.Count; i++)
                {
                    if (clist[i] == null)
                        continue;
                    ChargingRequest copy = clist[i].Clone();
                    cplist.Add(copy);
                    reservedNodeIds.Add(copy.NodeId);
                }
            }

            if (!allowCapacityOverflow && cplist.Count >= maxTask)
                return cplist;

            int iteration = 0;
            int safety = 0;
            while (safety < sensors.Length * 2 + 4)
            {
                safety++;
                iteration++;
                List<BprPredictedRequest> predictedRequests = BuildBprPredictedRequests(maxTask, reservedNodeIds);
                List<BprWindow> windows = BuildBprSlidingWindows(predictedRequests, maxTask);
                if (windows.Count == 0)
                {
                    WriteBprDebug(iteration, null, null, "NO_OVERFLOW_WINDOW", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                BprWindow worstWindow = windows[0];
                int capacityLeft = allowCapacityOverflow
                    ? worstWindow.OverflowCount
                    : Math.Max(0, maxTask - cplist.Count);
                int addCount = Math.Min(worstWindow.OverflowCount, capacityLeft);
                if (addCount <= 0)
                {
                    WriteBprDebug(iteration, worstWindow, null, "LIMITED_CAPACITY_FULL", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    return cplist;
                }

                List<BprRemovalDecision> decisions = SelectZhengBprRemovalNodes(
                    worstWindow,
                    addCount,
                    cplist,
                    selectionMode);
                if (decisions.Count == 0)
                {
                    WriteBprDebug(iteration, worstWindow, null, "NO_SELECTABLE_NODE", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                for (int i = 0; i < decisions.Count; i++)
                {
                    BprRemovalDecision decision = decisions[i];
                    int before = cplist.Count;
                    cplist.Add(CreateZhengBprProactiveRequest(decision));
                    reservedNodeIds.Add(decision.NodeId);
                    WriteBprDebug(iteration, worstWindow, decision, decision.Reason, before, cplist.Count, maxTask, allowCapacityOverflow);
                }

                if (!allowCapacityOverflow && cplist.Count >= maxTask)
                    return cplist;
            }

            return cplist;
        }

        private List<ChargingRequest> BuildYuBprCplist(
            List<ChargingRequest> clist,
            int maxTask,
            bool allowCapacityOverflow,
            YuProactiveSelectionMode selectionMode)
        {
            maxTask = Math.Max(1, maxTask);
            List<ChargingRequest> cplist = new List<ChargingRequest>();
            HashSet<int> reservedNodeIds = new HashSet<int>();
            if (clist != null)
            {
                for (int i = 0; i < clist.Count; i++)
                {
                    if (clist[i] == null)
                        continue;
                    if (!allowCapacityOverflow && cplist.Count >= maxTask)
                        break;
                    ChargingRequest copy = clist[i].Clone();
                    cplist.Add(copy);
                    reservedNodeIds.Add(copy.NodeId);
                }
            }

            if (!allowCapacityOverflow && cplist.Count >= maxTask)
                return cplist;

            int iteration = 0;
            int safety = 0;
            while (safety < sensors.Length * 2 + 4)
            {
                safety++;
                iteration++;
                List<YuPredictedInterval> intervals = BuildYuPredictedIntervals(maxTask, reservedNodeIds);
                List<YuDangerWindow> windows = BuildYuDangerWindows(intervals, maxTask);
                if (windows.Count == 0)
                {
                    WriteYuBprDebug(iteration, null, null, "NO_DANGER_WINDOW", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                YuDangerWindow worstWindow = windows[0];
                int capacityLeft = allowCapacityOverflow
                    ? worstWindow.RemovalNeededCount
                    : Math.Max(0, maxTask - cplist.Count);
                int addCount = Math.Min(worstWindow.RemovalNeededCount, capacityLeft);
                if (addCount <= 0)
                {
                    WriteYuBprDebug(iteration, worstWindow, null, "LIMITED_CAPACITY_FULL", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    return cplist;
                }

                List<YuRemovalDecision> decisions = SelectYuRemovalNodes(
                    worstWindow,
                    addCount,
                    cplist,
                    selectionMode);
                if (decisions.Count == 0)
                {
                    WriteYuBprDebug(iteration, worstWindow, null, "NO_SELECTABLE_NODE", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                for (int i = 0; i < decisions.Count; i++)
                {
                    YuRemovalDecision decision = decisions[i];
                    int before = cplist.Count;
                    cplist.Add(CreateYuProactiveRequest(decision));
                    reservedNodeIds.Add(decision.NodeId);
                    WriteYuBprDebug(iteration, worstWindow, decision, decision.Reason, before, cplist.Count, maxTask, allowCapacityOverflow);
                }

                if (!allowCapacityOverflow && cplist.Count >= maxTask)
                    return cplist;
            }

            return cplist;
        }

        private List<BprRemovalDecision> SelectZhengBprRemovalNodes(
            BprWindow window,
            int addCount,
            List<ChargingRequest> cplist,
            BprProactiveSelectionMode selectionMode)
        {
            List<BprRemovalDecision> selected = new List<BprRemovalDecision>();
            if (window == null || window.Requests == null || addCount <= 0)
                return selected;

            List<BprPredictedRequest> selectable = new List<BprPredictedRequest>(window.Requests);
            if (selectionMode == BprProactiveSelectionMode.Deterministic)
            {
                selectable.Sort(CompareZhengPredictedRequestDeterministic);
                List<ChargingRequest> currentRoute = BuildNearestRoute(cplist ?? new List<ChargingRequest>(), cplist == null ? 0 : cplist.Count);
                for (int i = 0; i < selectable.Count && selected.Count < addCount; i++)
                {
                    BprPredictedRequest request = selectable[i];
                    double routeCost = ComputeRouteInsertionCost(request.NodeId, currentRoute);
                    selected.Add(CreateZhengRemovalDecision(request, routeCost, "ZHENG_DETERMINISTIC_WINDOW_REMOVAL"));
                }
                return selected;
            }

            List<ChargingRequest> previewCplist = CloneRequestList(cplist);
            while (selectable.Count > 0 && selected.Count < addCount)
            {
                List<ChargingRequest> currentRoute = BuildNearestRoute(previewCplist, previewCplist.Count);
                selectable.Sort(delegate (BprPredictedRequest a, BprPredictedRequest b)
                {
                    double da = ComputeRouteInsertionCost(a.NodeId, currentRoute);
                    double db = ComputeRouteInsertionCost(b.NodeId, currentRoute);
                    int compare = da.CompareTo(db);
                    if (compare != 0)
                        return compare;
                    return CompareZhengPredictedRequestDeterministic(a, b);
                });

                BprPredictedRequest picked = selectable[0];
                selectable.RemoveAt(0);
                double routeInsertionCost = ComputeRouteInsertionCost(picked.NodeId, currentRoute);
                BprRemovalDecision decision = CreateZhengRemovalDecision(
                    picked,
                    routeInsertionCost,
                    "ZHENG_ROUTE_COST_WINDOW_REMOVAL");
                selected.Add(decision);
                previewCplist.Add(CreateZhengBprProactiveRequest(decision));
            }

            return selected;
        }

        private static int CompareZhengPredictedRequestDeterministic(BprPredictedRequest a, BprPredictedRequest b)
        {
            int compare = a.DeathTimeSeconds.CompareTo(b.DeathTimeSeconds);
            if (compare != 0)
                return compare;
            compare = a.RequestTimeSeconds.CompareTo(b.RequestTimeSeconds);
            if (compare != 0)
                return compare;
            compare = a.SlackSeconds.CompareTo(b.SlackSeconds);
            if (compare != 0)
                return compare;
            compare = b.EffectiveConsumeRateJPerSecond.CompareTo(a.EffectiveConsumeRateJPerSecond);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private BprRemovalDecision CreateZhengRemovalDecision(
            BprPredictedRequest request,
            double routeInsertionCost,
            string reason)
        {
            BprRemovalDecision decision = new BprRemovalDecision();
            decision.NodeId = request.NodeId;
            decision.RequestTimeSeconds = request.RequestTimeSeconds;
            decision.DeathTimeSeconds = request.DeathTimeSeconds;
            decision.EffectiveConsumeRateJPerSecond = request.EffectiveConsumeRateJPerSecond;
            decision.SlackSeconds = request.SlackSeconds;
            decision.RouteInsertionCost = routeInsertionCost;
            decision.Score = routeInsertionCost;
            decision.Reason = reason;
            return decision;
        }

        private ChargingRequest CreateZhengBprProactiveRequest(BprRemovalDecision decision)
        {
            ChargingRequest proactive = new ChargingRequest();
            proactive.RequestId = -decision.NodeId;
            proactive.NodeId = decision.NodeId;
            proactive.RequestTimeSeconds = currentTime;
            proactive.DeadlineSeconds = decision.DeathTimeSeconds;
            if (decision.NodeId > 0 && decision.NodeId < sensors.Length)
            {
                SensorState sensor = sensors[decision.NodeId];
                proactive.RequestEnergyJ = sensor.EnergyJ;
                PopulateChargingRequestRoutingFields(proactive, sensor);
            }
            proactive.EffectiveConsumeRateJPerSecond = decision.EffectiveConsumeRateJPerSecond;
            proactive.CriticalDensity = 0.0;
            proactive.IsProactive = true;
            proactive.ProactiveReason = ZhengBprWindowRemovalReason;
            return proactive;
        }

        private List<YuRemovalDecision> SelectYuRemovalNodes(
            YuDangerWindow window,
            int addCount,
            List<ChargingRequest> cplist,
            YuProactiveSelectionMode selectionMode)
        {
            List<YuRemovalDecision> selected = new List<YuRemovalDecision>();
            if (window == null || window.OverlappingIntervals == null || addCount <= 0)
                return selected;

            List<YuPredictedInterval> selectable = new List<YuPredictedInterval>(window.OverlappingIntervals);
            if (selectionMode == YuProactiveSelectionMode.Deterministic)
            {
                selectable.Sort(CompareYuPredictedIntervalDeterministic);
                List<ChargingRequest> currentRoute = BuildNearestRoute(cplist ?? new List<ChargingRequest>(), cplist == null ? 0 : cplist.Count);
                for (int i = 0; i < selectable.Count && selected.Count < addCount; i++)
                {
                    YuPredictedInterval interval = selectable[i];
                    double routeCost = ComputeRouteInsertionCost(interval.NodeId, currentRoute);
                    selected.Add(CreateYuRemovalDecision(interval, routeCost, "YU_DETERMINISTIC_INTERVAL_REMOVAL"));
                }
                return selected;
            }

            List<ChargingRequest> previewCplist = CloneRequestList(cplist);
            while (selectable.Count > 0 && selected.Count < addCount)
            {
                List<ChargingRequest> currentRoute = BuildNearestRoute(previewCplist, previewCplist.Count);
                selectable.Sort(delegate (YuPredictedInterval a, YuPredictedInterval b)
                {
                    double da = ComputeRouteInsertionCost(a.NodeId, currentRoute);
                    double db = ComputeRouteInsertionCost(b.NodeId, currentRoute);
                    int compare = da.CompareTo(db);
                    if (compare != 0)
                        return compare;
                    return CompareYuPredictedIntervalDeterministic(a, b);
                });

                YuPredictedInterval picked = selectable[0];
                selectable.RemoveAt(0);
                double routeInsertionCost = ComputeRouteInsertionCost(picked.NodeId, currentRoute);
                YuRemovalDecision decision = CreateYuRemovalDecision(
                    picked,
                    routeInsertionCost,
                    "YU_ROUTE_COST_INTERVAL_REMOVAL");
                selected.Add(decision);
                previewCplist.Add(CreateYuProactiveRequest(decision));
            }

            return selected;
        }

        private static int CompareYuPredictedIntervalDeterministic(YuPredictedInterval a, YuPredictedInterval b)
        {
            int compare = a.EarliestDeathTimeSeconds.CompareTo(b.EarliestDeathTimeSeconds);
            if (compare != 0)
                return compare;
            compare = a.LatestSafeServiceTimeSeconds.CompareTo(b.LatestSafeServiceTimeSeconds);
            if (compare != 0)
                return compare;
            compare = a.IntervalStartSeconds.CompareTo(b.IntervalStartSeconds);
            if (compare != 0)
                return compare;
            compare = a.CenterRequestTimeSeconds.CompareTo(b.CenterRequestTimeSeconds);
            if (compare != 0)
                return compare;
            compare = a.SlackSeconds.CompareTo(b.SlackSeconds);
            if (compare != 0)
                return compare;
            compare = b.UncertaintySeconds.CompareTo(a.UncertaintySeconds);
            if (compare != 0)
                return compare;
            compare = b.EffectiveConsumeRateJPerSecond.CompareTo(a.EffectiveConsumeRateJPerSecond);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private YuRemovalDecision CreateYuRemovalDecision(
            YuPredictedInterval interval,
            double routeInsertionCost,
            string reason)
        {
            YuRemovalDecision decision = new YuRemovalDecision();
            decision.NodeId = interval.NodeId;
            decision.CenterRequestTimeSeconds = interval.CenterRequestTimeSeconds;
            decision.IntervalStartSeconds = interval.IntervalStartSeconds;
            decision.IntervalEndSeconds = interval.IntervalEndSeconds;
            decision.EarliestDeathTimeSeconds = interval.EarliestDeathTimeSeconds;
            decision.LatestSafeServiceTimeSeconds = interval.LatestSafeServiceTimeSeconds;
            decision.EffectiveConsumeRateJPerSecond = interval.EffectiveConsumeRateJPerSecond;
            decision.UncertaintySeconds = interval.UncertaintySeconds;
            decision.SlackSeconds = interval.SlackSeconds;
            decision.RouteInsertionCost = routeInsertionCost;
            decision.Score = routeInsertionCost;
            decision.Reason = reason;
            return decision;
        }

        private ChargingRequest CreateYuProactiveRequest(YuRemovalDecision decision)
        {
            ChargingRequest proactive = new ChargingRequest();
            proactive.RequestId = -decision.NodeId;
            proactive.NodeId = decision.NodeId;
            proactive.RequestTimeSeconds = currentTime;
            proactive.DeadlineSeconds = decision.LatestSafeServiceTimeSeconds;
            if (decision.NodeId > 0 && decision.NodeId < sensors.Length)
            {
                SensorState sensor = sensors[decision.NodeId];
                proactive.RequestEnergyJ = sensor.EnergyJ;
                PopulateChargingRequestRoutingFields(proactive, sensor);
            }
            proactive.EffectiveConsumeRateJPerSecond = decision.EffectiveConsumeRateJPerSecond;
            proactive.CriticalDensity = 0.0;
            proactive.IsProactive = true;
            proactive.ProactiveReason = YuBprDangerIntervalRemovalReason;
            return proactive;
        }

        private List<ChargingRequest> CloneRequestList(List<ChargingRequest> requests)
        {
            List<ChargingRequest> result = new List<ChargingRequest>();
            if (requests == null)
                return result;
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i] != null)
                    result.Add(requests[i].Clone());
            }
            return result;
        }

        private void WriteBprDebug(
            int iteration,
            BprWindow window,
            BprRemovalDecision decision,
            string reason,
            int cplistCountBefore,
            int cplistCountAfter,
            int maxTask,
            bool allowCapacityOverflow)
        {
            if (csvWriter == null)
                return;
            csvWriter.WriteBprDebug(
                artifact.RunIndex,
                artifact.Seed,
                algorithm,
                missionId,
                currentTime,
                iteration,
                window == null ? Double.NaN : window.WindowStartSeconds,
                window == null ? Double.NaN : window.WindowEndSeconds,
                window == null ? 0 : window.BottleneckCount,
                window == null ? 0 : window.OverflowCount,
                decision == null ? -1 : decision.NodeId,
                decision == null ? Double.NaN : decision.RequestTimeSeconds,
                decision == null ? Double.NaN : decision.DeathTimeSeconds,
                decision == null ? Double.NaN : decision.SlackSeconds,
                decision == null ? Double.NaN : decision.EffectiveConsumeRateJPerSecond,
                decision == null ? Double.NaN : decision.RouteInsertionCost,
                reason,
                cplistCountBefore,
                cplistCountAfter,
                maxTask,
                allowCapacityOverflow);
        }

        private void WriteYuBprDebug(
            int iteration,
            YuDangerWindow window,
            YuRemovalDecision decision,
            string reason,
            int cplistCountBefore,
            int cplistCountAfter,
            int maxTask,
            bool allowCapacityOverflow)
        {
            if (csvWriter == null)
                return;
            csvWriter.WriteYuBprDebug(
                artifact.RunIndex,
                artifact.Seed,
                algorithm,
                missionId,
                currentTime,
                iteration,
                window == null ? Double.NaN : window.WindowStartSeconds,
                window == null ? Double.NaN : window.WindowEndSeconds,
                window == null ? 0 : window.KStar,
                window == null ? 0 : window.DangerCount,
                window == null ? 0 : window.RemovalNeededCount,
                decision == null ? -1 : decision.NodeId,
                decision == null ? Double.NaN : decision.IntervalStartSeconds,
                decision == null ? Double.NaN : decision.CenterRequestTimeSeconds,
                decision == null ? Double.NaN : decision.IntervalEndSeconds,
                decision == null ? Double.NaN : decision.EarliestDeathTimeSeconds,
                decision == null ? Double.NaN : decision.LatestSafeServiceTimeSeconds,
                decision == null ? Double.NaN : decision.SlackSeconds,
                decision == null ? Double.NaN : decision.UncertaintySeconds,
                decision == null ? Double.NaN : decision.EffectiveConsumeRateJPerSecond,
                decision == null ? Double.NaN : decision.RouteInsertionCost,
                reason,
                cplistCountBefore,
                cplistCountAfter,
                maxTask,
                allowCapacityOverflow);
        }

        private List<YuPredictedInterval> BuildYuRequestIntervals(int maxTask, HashSet<int> reservedNodeIds)
        {
            return BuildYuPredictedIntervals(maxTask, reservedNodeIds);
        }

        private List<YuRequestInterval> BuildYuRequestIntervals(HashSet<int> reservedNodeIds)
        {
            List<YuRequestInterval> intervals = new List<YuRequestInterval>();
            List<BprSTableEntry> entries = BuildBprSTable(reservedNodeIds);
            for (int i = 0; i < entries.Count; i++)
            {
                BprSTableEntry entry = entries[i];
                if (entry == null)
                    continue;
                double center = entry.LatestReportedDeadlineSeconds;
                if (Double.IsNaN(center) || Double.IsInfinity(center))
                    continue;
                if (entry.EffectiveConsumeRateJPerSecond <= 1e-12)
                    continue;

                double uncertainty = ResolveYuIntervalUncertaintySeconds(entry);
                YuRequestInterval interval = new YuRequestInterval();
                interval.NodeId = entry.NodeId;
                interval.CenterRequestTimeSeconds = center;
                interval.IntervalStartSeconds = center - uncertainty;
                interval.IntervalEndSeconds = center + uncertainty;
                interval.EnergyJ = entry.EnergyJ;
                interval.ConsumeRateJPerSecond = entry.ConsumeRateJPerSecond;
                interval.BaseConsumeRateJPerSecond = entry.BaseConsumeRateJPerSecond;
                interval.EffectiveConsumeRateJPerSecond = entry.EffectiveConsumeRateJPerSecond;
                interval.RoutingLoadJPerSecond = entry.RoutingLoadJPerSecond;
                interval.RoutingSubtreeSize = entry.RoutingSubtreeSize;
                interval.ExpectedRoutingForwardPacketsPerSecond = entry.ExpectedRoutingForwardPacketsPerSecond;
                interval.UncertaintySeconds = uncertainty;
                interval.IsPendingRequest = entry.IsPendingRequest;
                interval.IsScheduledInCurrentMission = entry.IsScheduledInCurrentMission;
                interval.IsAlive = entry.IsAlive;
                intervals.Add(interval);
            }

            intervals.Sort(CompareYuIntervalByStart);
            return intervals;
        }

        private List<YuRequestInterval> BuildYuDangerOverlap(
            List<YuRequestInterval> intervals,
            double windowStart,
            double windowEnd)
        {
            List<YuRequestInterval> overlap = new List<YuRequestInterval>();
            for (int i = 0; i < intervals.Count; i++)
            {
                YuRequestInterval interval = intervals[i];
                if (interval.IntervalEndSeconds >= windowStart - Epsilon &&
                    interval.IntervalStartSeconds <= windowEnd + Epsilon)
                    overlap.Add(interval);
            }
            return overlap;
        }

        private List<YuRequestInterval> SelectYuProactiveIntervals(
            List<YuRequestInterval> candidates,
            int addCount,
            List<ChargingRequest> cplist,
            YuProactiveSelectionMode selectionMode)
        {
            List<YuRequestInterval> selected = new List<YuRequestInterval>();
            if (candidates == null || addCount <= 0)
                return selected;

            List<YuRequestInterval> selectable = new List<YuRequestInterval>(candidates);
            if (selectionMode == YuProactiveSelectionMode.Deterministic)
            {
                selectable.Sort(CompareYuIntervalDeterministic);
                for (int i = 0; i < selectable.Count && selected.Count < addCount; i++)
                    selected.Add(selectable[i]);
                return selected;
            }

            List<ChargingRequest> previewCplist = new List<ChargingRequest>();
            if (cplist != null)
            {
                for (int i = 0; i < cplist.Count; i++)
                    previewCplist.Add(cplist[i].Clone());
            }

            while (selectable.Count > 0 && selected.Count < addCount)
            {
                List<ChargingRequest> currentRoute = BuildNearestRoute(previewCplist, previewCplist.Count);
                selectable.Sort(delegate (YuRequestInterval a, YuRequestInterval b)
                {
                    double da = ComputeRouteInsertionCost(a.NodeId, currentRoute);
                    double db = ComputeRouteInsertionCost(b.NodeId, currentRoute);
                    int compare = da.CompareTo(db);
                    if (compare != 0)
                        return compare;
                    compare = a.CenterRequestTimeSeconds.CompareTo(b.CenterRequestTimeSeconds);
                    if (compare != 0)
                        return compare;
                    compare = b.UncertaintySeconds.CompareTo(a.UncertaintySeconds);
                    if (compare != 0)
                        return compare;
                    return a.NodeId.CompareTo(b.NodeId);
                });

                YuRequestInterval picked = selectable[0];
                selectable.RemoveAt(0);
                selected.Add(picked);
                previewCplist.Add(CreateYuProactiveRequest(picked));
            }

            return selected;
        }

        private static int CompareYuIntervalDeterministic(YuRequestInterval a, YuRequestInterval b)
        {
            int compare = a.CenterRequestTimeSeconds.CompareTo(b.CenterRequestTimeSeconds);
            if (compare != 0)
                return compare;
            compare = b.UncertaintySeconds.CompareTo(a.UncertaintySeconds);
            if (compare != 0)
                return compare;
            compare = b.EffectiveConsumeRateJPerSecond.CompareTo(a.EffectiveConsumeRateJPerSecond);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private ChargingRequest CreateYuProactiveRequest(YuRequestInterval interval)
        {
            ChargingRequest proactive = new ChargingRequest();
            proactive.RequestId = -interval.NodeId;
            proactive.NodeId = interval.NodeId;
            proactive.RequestTimeSeconds = currentTime;
            proactive.DeadlineSeconds = interval.CenterRequestTimeSeconds;
            proactive.RequestEnergyJ = interval.EnergyJ;
            proactive.ConsumeRateJPerSecond = interval.ConsumeRateJPerSecond;
            proactive.BaseConsumeRateJPerSecond = interval.BaseConsumeRateJPerSecond;
            proactive.EffectiveConsumeRateJPerSecond = interval.EffectiveConsumeRateJPerSecond;
            proactive.RoutingLoadJPerSecond = interval.RoutingLoadJPerSecond;
            proactive.RoutingTxLoadJPerSecond = 0.0;
            proactive.RoutingRxLoadJPerSecond = 0.0;
            if (interval.NodeId > 0 && interval.NodeId < sensors.Length)
            {
                proactive.RoutingTxLoadJPerSecond = GetRoutingTxLoadJPerSecond(sensors[interval.NodeId]);
                proactive.RoutingRxLoadJPerSecond = GetRoutingRxLoadJPerSecond(sensors[interval.NodeId]);
            }
            proactive.RoutingSubtreeSize = interval.RoutingSubtreeSize;
            proactive.ExpectedRoutingForwardPacketsPerSecond = interval.ExpectedRoutingForwardPacketsPerSecond;
            proactive.CriticalDensity = 0.0;
            proactive.IsProactive = true;
            proactive.ProactiveReason = YuBprDangerIntervalRemovalReason;
            return proactive;
        }

        private double ResolveYuDangerWindowSeconds(int maxTask)
        {
            if (settings.YuDangerWindowSeconds > 0.0)
                return settings.YuDangerWindowSeconds;
            return EstimateBprTjobSeconds(maxTask);
        }

        private int ResolveYuDangerThresholdK(int maxTask)
        {
            if (settings.YuDangerThresholdK > 0)
                return settings.YuDangerThresholdK;
            return Math.Max(1, maxTask + 1);
        }

        private double ResolveYuIntervalUncertaintySeconds(BprSTableEntry entry)
        {
            if (settings.YuIntervalUncertaintySeconds > 0.0)
                return settings.YuIntervalUncertaintySeconds;
            return GetBprDeadlineThresholdSeconds();
        }

        private double ResolveYuPredictionUncertaintySeconds(YuPredictionSegment segment, int maxTask)
        {
            if (settings.YuIntervalUncertaintySeconds > 0.0)
                return settings.YuIntervalUncertaintySeconds;
            return Math.Max(GetBprDeadlineThresholdSeconds(), EstimateBprTjobSeconds(maxTask) * 0.25);
        }

        private double ResolveProactivePredictionHorizonSeconds(int maxTask)
        {
            if (settings.ProactivePredictionHorizonSeconds > 0.0)
                return settings.ProactivePredictionHorizonSeconds;
            return settings.TreqSeconds + EstimateBprTjobSeconds(maxTask);
        }

        private double ResolveProactiveCooldownSeconds()
        {
            if (settings.ProactiveCooldownSeconds > 0.0)
                return settings.ProactiveCooldownSeconds;
            return settings.TreqSeconds;
        }

        private static int CompareYuIntervalByStart(YuRequestInterval a, YuRequestInterval b)
        {
            int compare = a.IntervalStartSeconds.CompareTo(b.IntervalStartSeconds);
            if (compare != 0)
                return compare;
            compare = a.CenterRequestTimeSeconds.CompareTo(b.CenterRequestTimeSeconds);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private List<BprSTableEntry> BuildBprSTable(HashSet<int> reservedNodeIds)
        {
            return GetEligibleBprSTableEntries(reservedNodeIds);
        }

        private static int CompareBprSTableByDeadline(BprSTableEntry a, BprSTableEntry b)
        {
            int compare = a.LatestReportedDeadlineSeconds.CompareTo(b.LatestReportedDeadlineSeconds);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private List<BprSTableEntry> BuildBprBottleList(
            List<BprSTableEntry> sTable,
            double windowStart,
            double windowEnd,
            double tdeadlineThreshold)
        {
            List<BprSTableEntry> bottleList = new List<BprSTableEntry>();
            for (int i = 0; i < sTable.Count; i++)
            {
                BprSTableEntry entry = sTable[i];
                double intervalStart = entry.LatestReportedDeadlineSeconds - tdeadlineThreshold;
                double intervalEnd = entry.LatestReportedDeadlineSeconds + tdeadlineThreshold;
                if (intervalEnd >= windowStart - Epsilon && intervalStart <= windowEnd + Epsilon)
                    bottleList.Add(entry);
            }
            return bottleList;
        }

        private List<BprSTableEntry> SelectBprProactiveEntries(
            List<BprSTableEntry> bottleList,
            int addCount,
            List<ChargingRequest> cplist,
            BprProactiveSelectionMode selectionMode)
        {
            List<BprSTableEntry> selected = new List<BprSTableEntry>();
            if (bottleList == null || addCount <= 0)
                return selected;

            List<BprSTableEntry> selectable = new List<BprSTableEntry>(bottleList);
            if (selectionMode == BprProactiveSelectionMode.Deterministic)
            {
                selectable.Sort(CompareBprSTableByDeadline);
                for (int i = 0; i < selectable.Count && selected.Count < addCount; i++)
                    selected.Add(selectable[i]);
                return selected;
            }

            List<ChargingRequest> previewCplist = new List<ChargingRequest>();
            if (cplist != null)
            {
                for (int i = 0; i < cplist.Count; i++)
                    previewCplist.Add(cplist[i].Clone());
            }

            while (selectable.Count > 0 && selected.Count < addCount)
            {
                List<ChargingRequest> currentRoute = BuildNearestRoute(previewCplist, previewCplist.Count);
                selectable.Sort(delegate (BprSTableEntry a, BprSTableEntry b)
                {
                    double da = ComputeRouteInsertionCost(a.NodeId, currentRoute);
                    double db = ComputeRouteInsertionCost(b.NodeId, currentRoute);
                    int compare = da.CompareTo(db);
                    if (compare != 0)
                        return compare;
                    compare = a.LatestReportedDeadlineSeconds.CompareTo(b.LatestReportedDeadlineSeconds);
                    if (compare != 0)
                        return compare;
                    return a.NodeId.CompareTo(b.NodeId);
                });

                BprSTableEntry picked = selectable[0];
                selectable.RemoveAt(0);
                selected.Add(picked);
                previewCplist.Add(CreateBprProactiveRequest(picked));
            }

            return selected;
        }

        private ChargingRequest CreateBprProactiveRequest(BprSTableEntry entry)
        {
            ChargingRequest proactive = new ChargingRequest();
            proactive.RequestId = -entry.NodeId;
            proactive.NodeId = entry.NodeId;
            proactive.RequestTimeSeconds = currentTime;
            proactive.DeadlineSeconds = entry.LatestReportedDeadlineSeconds;
            proactive.RequestEnergyJ = entry.EnergyJ;
            proactive.ConsumeRateJPerSecond = entry.ConsumeRateJPerSecond;
            proactive.BaseConsumeRateJPerSecond = entry.BaseConsumeRateJPerSecond;
            proactive.EffectiveConsumeRateJPerSecond = entry.EffectiveConsumeRateJPerSecond;
            proactive.RoutingLoadJPerSecond = entry.RoutingLoadJPerSecond;
            proactive.RoutingTxLoadJPerSecond = entry.RoutingTxLoadJPerSecond;
            proactive.RoutingRxLoadJPerSecond = entry.RoutingRxLoadJPerSecond;
            proactive.RoutingSubtreeSize = entry.RoutingSubtreeSize;
            proactive.ExpectedRoutingForwardPacketsPerSecond = entry.ExpectedRoutingForwardPacketsPerSecond;
            proactive.CriticalDensity = 0.0;
            proactive.IsProactive = true;
            proactive.ProactiveReason = ZhengBprWindowRemovalReason;
            return proactive;
        }

        private double GetBprDeadlineThresholdSeconds()
        {
            return settings.BprDeadlineThresholdSeconds;
        }

        private static string FormatBprDeadlineForLog(double deadline)
        {
            if (Double.IsPositiveInfinity(deadline))
                return "Infinity";
            if (Double.IsNegativeInfinity(deadline))
                return "-Infinity";
            if (Double.IsNaN(deadline))
                return "NaN";
            return deadline.ToString(CultureInfo.InvariantCulture);
        }

        private double EstimateBprTjobSeconds(int maxTask)
        {
            maxTask = Math.Max(1, maxTask);
            double side = Math.Max(1.0, Math.Max(settings.MapWidthMeters, settings.MapHeightMeters));
            double pathLength;
            if (maxTask <= 1)
            {
                pathLength = 2.0 * Math.Sqrt(2.0) * side;
            }
            else
            {
                double denominator = Math.Sqrt(maxTask) - 1.0;
                if (denominator <= 0.0)
                    pathLength = (maxTask + 1) * Math.Sqrt(2.0) * side;
                else
                    pathLength = ((maxTask - 1) * side) / denominator + 2.0 * Math.Sqrt(2.0) * side;
            }

            double travelSeconds = pathLength / Math.Max(1e-9, settings.WcvSpeedMetersPerSecond);
            double fullChargeSeconds = settings.InitialEnergyJ / Math.Max(1e-9, settings.WcvChargeRateJPerSecond);
            return Math.Max(1.0, travelSeconds + fullChargeSeconds * maxTask);
        }

        private double ComputeRouteInsertionCost(int nodeId, List<ChargingRequest> route)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return Double.MaxValue;

            SensorState candidate = sensors[nodeId];
            int routeCount = route == null ? 0 : route.Count;
            if (routeCount == 0)
                return 2.0 * ExperimentArtifact.Distance(artifact.BaseX, artifact.BaseY, candidate.X, candidate.Y);

            double best = Double.MaxValue;
            for (int position = 0; position <= routeCount; position++)
            {
                double prevX = artifact.BaseX;
                double prevY = artifact.BaseY;
                if (position > 0)
                {
                    SensorState prev = sensors[route[position - 1].NodeId];
                    prevX = prev.X;
                    prevY = prev.Y;
                }

                double nextX = artifact.BaseX;
                double nextY = artifact.BaseY;
                if (position < routeCount)
                {
                    SensorState next = sensors[route[position].NodeId];
                    nextX = next.X;
                    nextY = next.Y;
                }

                double original = ExperimentArtifact.Distance(prevX, prevY, nextX, nextY);
                double inserted =
                    ExperimentArtifact.Distance(prevX, prevY, candidate.X, candidate.Y) +
                    ExperimentArtifact.Distance(candidate.X, candidate.Y, nextX, nextY);
                best = Math.Min(best, inserted - original);
            }

            return best;
        }

        private static int CompareByDeadline(ChargingRequest a, ChargingRequest b)
        {
            int compare = a.DeadlineSeconds.CompareTo(b.DeadlineSeconds);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private List<ChargingRequest> TakeSorted(List<ChargingRequest> pool, int maxTask, Comparison<ChargingRequest> comparison)
        {
            List<ChargingRequest> route = new List<ChargingRequest>(pool);
            route.Sort(comparison);
            if (route.Count > maxTask)
                route.RemoveRange(maxTask, route.Count - maxTask);
            return route;
        }

        private List<ChargingRequest> BuildNearestRoute(List<ChargingRequest> pool, int maxTask)
        {
            List<ChargingRequest> remaining = new List<ChargingRequest>(pool);
            List<ChargingRequest> route = new List<ChargingRequest>();
            double x = artifact.BaseX;
            double y = artifact.BaseY;
            while (remaining.Count > 0 && route.Count < maxTask)
            {
                int bestIndex = 0;
                double bestDistance = Double.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    SensorState sensor = sensors[remaining[i].NodeId];
                    double distance = ExperimentArtifact.Distance(x, y, sensor.X, sensor.Y);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestIndex = i;
                    }
                }
                ChargingRequest next = remaining[bestIndex];
                route.Add(next);
                remaining.RemoveAt(bestIndex);
                x = sensors[next.NodeId].X;
                y = sensors[next.NodeId].Y;
            }
            return route;
        }

        private List<ChargingRequest> BuildCompositeRoute(List<ChargingRequest> pool, int maxTask, double urgencyWeight, double distanceWeight, double rateWeight)
        {
            List<ChargingRequest> remaining = new List<ChargingRequest>(pool);
            List<ChargingRequest> route = new List<ChargingRequest>();
            double x = artifact.BaseX;
            double y = artifact.BaseY;
            while (remaining.Count > 0 && route.Count < maxTask)
            {
                double maxSlack = 1.0;
                double maxDistance = 1.0;
                double maxRate = 1.0;
                for (int i = 0; i < remaining.Count; i++)
                {
                    maxSlack = Math.Max(maxSlack, Math.Max(0.0, remaining[i].DeadlineSeconds - currentTime));
                    maxDistance = Math.Max(maxDistance, DistanceFrom(x, y, remaining[i].NodeId));
                    maxRate = Math.Max(maxRate, GetRequestEffectiveConsumeRate(remaining[i]));
                }

                int bestIndex = 0;
                double bestScore = Double.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    double urgency = Math.Max(0.0, remaining[i].DeadlineSeconds - currentTime) / maxSlack;
                    double distance = DistanceFrom(x, y, remaining[i].NodeId) / maxDistance;
                    double rate = 1.0 - GetRequestEffectiveConsumeRate(remaining[i]) / maxRate;
                    double score = urgencyWeight * urgency + distanceWeight * distance + rateWeight * rate;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestIndex = i;
                    }
                }

                ChargingRequest next = remaining[bestIndex];
                route.Add(next);
                remaining.RemoveAt(bestIndex);
                x = sensors[next.NodeId].X;
                y = sensors[next.NodeId].Y;
            }

            return route;
        }

        private List<ChargingRequest> BuildFuzzyRoute(List<ChargingRequest> pool, int maxTask)
        {
            List<ChargingRequest> remaining = new List<ChargingRequest>(pool);
            List<ChargingRequest> route = new List<ChargingRequest>();
            double x = artifact.BaseX;
            double y = artifact.BaseY;

            while (remaining.Count > 0 && route.Count < maxTask)
            {
                int bestIndex = 0;
                double bestPriority = Double.MinValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    ChargingRequest request = remaining[i];
                    SensorState sensor = sensors[request.NodeId];
                    double residualRatio = sensor.CapacityJ <= 0.0 ? 0.0 : sensor.EnergyJ / sensor.CapacityJ;
                    double distanceRatio = DistanceFrom(x, y, request.NodeId) /
                        Math.Max(1.0, Math.Sqrt(settings.MapWidthMeters * settings.MapWidthMeters + settings.MapHeightMeters * settings.MapHeightMeters));
                    double baseRate = Math.Max(1e-9, settings.InitialEnergyJ / settings.SensorBackgroundLifetimeSeconds);
                    double rateRatio = GetEffectiveConsumeRateJPerSecond(sensor) / baseRate;
                    double routingRatio = GetRoutingLoadJPerSecond(sensor) / baseRate;
                    double density = ComputeCriticalNodeDensity(request.NodeId);
                    double priority = FuzzyPriority(residualRatio, distanceRatio, rateRatio, density, routingRatio);
                    if (priority > bestPriority)
                    {
                        bestPriority = priority;
                        bestIndex = i;
                    }
                }

                ChargingRequest next = remaining[bestIndex];
                route.Add(next);
                remaining.RemoveAt(bestIndex);
                x = sensors[next.NodeId].X;
                y = sensors[next.NodeId].Y;
            }

            return route;
        }

        private List<ChargingRequest> BuildGeneticRoute(List<ChargingRequest> pool, int maxTask)
        {
            int routeSize = Math.Min(maxTask, pool.Count);
            if (routeSize <= 1)
                return TakeSorted(pool, maxTask, CompareByDeadline);

            int populationSize = Math.Max(40, routeSize * 2);
            int generations = 80;
            List<List<ChargingRequest>> population = BuildInitialOptimizationPopulation(pool, maxTask, populationSize);
            List<ChargingRequest> bestRoute = FindBestOptimizationRoute(population);
            double bestFitness = EvaluateRouteFitness(bestRoute);

            for (int generation = 0; generation < generations; generation++)
            {
                List<List<ChargingRequest>> nextPopulation = new List<List<ChargingRequest>>();
                nextPopulation.Add(CopyRoute(bestRoute));

                List<ChargingRequest> secondElite = FindBestOptimizationRouteExcept(population, bestRoute);
                if (secondElite.Count > 0 && nextPopulation.Count < populationSize)
                    nextPopulation.Add(CopyRoute(secondElite));

                while (nextPopulation.Count < populationSize)
                {
                    List<ChargingRequest> parentA = TournamentSelectRoute(population, 3);
                    List<ChargingRequest> parentB = TournamentSelectRoute(population, 3);
                    List<ChargingRequest> child = OrderedCrossoverRoute(parentA, parentB, pool, maxTask);
                    MutateOptimizationRoute(child, pool, maxTask, 0.28, true);
                    nextPopulation.Add(NormalizeOptimizationRoute(child, pool, maxTask));
                }

                population = nextPopulation;
                List<ChargingRequest> generationBest = FindBestOptimizationRoute(population);
                double generationFitness = EvaluateRouteFitness(generationBest);
                if (generationFitness < bestFitness)
                {
                    bestFitness = generationFitness;
                    bestRoute = CopyRoute(generationBest);
                }
            }

            return CopyRoute(bestRoute);
        }

        private List<ChargingRequest> BuildPsoRoute(List<ChargingRequest> pool, int maxTask)
        {
            int routeSize = Math.Min(maxTask, pool.Count);
            if (routeSize <= 1)
                return TakeSorted(pool, maxTask, CompareByDeadline);

            int particleCount = Math.Max(40, routeSize * 2);
            int iterations = 80;
            List<List<ChargingRequest>> seedRoutes = BuildInitialOptimizationPopulation(pool, maxTask, Math.Min(4, particleCount));
            List<PsoParticle> particles = new List<PsoParticle>();
            PsoParticle globalBest = null;

            for (int i = 0; i < particleCount; i++)
            {
                PsoParticle particle = i < seedRoutes.Count
                    ? CreatePsoParticleFromRoute(pool, seedRoutes[i])
                    : CreateRandomPsoParticle(pool);
                List<ChargingRequest> route = BuildRouteFromRandomKeys(pool, particle.Position, maxTask);
                particle.BestFitness = EvaluateRouteFitness(route);
                particle.BestPosition = CopyArray(particle.Position);
                particles.Add(particle);
                if (globalBest == null || particle.BestFitness < globalBest.BestFitness)
                    globalBest = particle.CloneBestOnly();
            }

            double inertia = 0.72;
            double cognitive = 1.49;
            double social = 1.49;
            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < particles.Count; i++)
                {
                    PsoParticle particle = particles[i];
                    for (int d = 0; d < particle.Position.Length; d++)
                    {
                        double r1 = algorithmRandom.NextDouble();
                        double r2 = algorithmRandom.NextDouble();
                        particle.Velocity[d] = inertia * particle.Velocity[d] +
                            cognitive * r1 * (particle.BestPosition[d] - particle.Position[d]) +
                            social * r2 * (globalBest.BestPosition[d] - particle.Position[d]);
                        particle.Velocity[d] = ExperimentSettings.Clamp(particle.Velocity[d], -0.50, 0.50);
                        particle.Position[d] = ExperimentSettings.Clamp(particle.Position[d] + particle.Velocity[d], 0.0, 1.0);
                    }

                    List<ChargingRequest> route = BuildRouteFromRandomKeys(pool, particle.Position, maxTask);
                    double fitness = EvaluateRouteFitness(route);
                    if (fitness < particle.BestFitness)
                    {
                        particle.BestFitness = fitness;
                        particle.BestPosition = CopyArray(particle.Position);
                    }
                    if (fitness < globalBest.BestFitness)
                        globalBest = particle.CloneBestOnly();
                }
            }

            return BuildRouteFromRandomKeys(pool, globalBest.BestPosition, maxTask);
        }

        private List<ChargingRequest> BuildCuckooRoute(List<ChargingRequest> pool, int maxTask)
        {
            int routeSize = Math.Min(maxTask, pool.Count);
            if (routeSize <= 1)
                return TakeSorted(pool, maxTask, CompareByDeadline);

            int nestCount = Math.Max(40, routeSize * 2);
            int iterations = 80;
            double abandonmentProbability = 0.25;
            List<List<ChargingRequest>> nests = BuildInitialOptimizationPopulation(pool, maxTask, nestCount);
            List<ChargingRequest> bestRoute = FindBestOptimizationRoute(nests);
            double bestFitness = EvaluateRouteFitness(bestRoute);

            for (int iteration = 0; iteration < iterations; iteration++)
            {
                for (int i = 0; i < nests.Count; i++)
                {
                    List<ChargingRequest> newRoute = CopyRoute(nests[i]);
                    ApplyCuckooPermutationFlight(newRoute, pool, maxTask);
                    double newFitness = EvaluateRouteFitness(newRoute);
                    int targetIndex = algorithmRandom.Next(nests.Count);
                    if (newFitness < EvaluateRouteFitness(nests[targetIndex]))
                        nests[targetIndex] = newRoute;
                    if (newFitness < bestFitness)
                    {
                        bestFitness = newFitness;
                        bestRoute = CopyRoute(newRoute);
                    }
                }

                nests.Sort(delegate (List<ChargingRequest> a, List<ChargingRequest> b)
                {
                    return EvaluateRouteFitness(a).CompareTo(EvaluateRouteFitness(b));
                });

                int abandonCount = Math.Max(1, (int)Math.Round(nestCount * abandonmentProbability));
                for (int i = 0; i < abandonCount && i < nests.Count; i++)
                {
                    int index = nests.Count - 1 - i;
                    List<ChargingRequest> replacement = algorithmRandom.NextDouble() < 0.50
                        ? CopyRoute(bestRoute)
                        : BuildRandomOptimizationRoute(pool, maxTask);
                    ApplyCuckooPermutationFlight(replacement, pool, maxTask);
                    nests[index] = NormalizeOptimizationRoute(replacement, pool, maxTask);
                }

                List<ChargingRequest> iterationBest = FindBestOptimizationRoute(nests);
                double iterationFitness = EvaluateRouteFitness(iterationBest);
                if (iterationFitness < bestFitness)
                {
                    bestFitness = iterationFitness;
                    bestRoute = CopyRoute(iterationBest);
                }
            }

            return CopyRoute(bestRoute);
        }

        private double EvaluateRouteFitness(List<ChargingRequest> route)
        {
            if (route == null || route.Count == 0)
                return 1.0e12;

            Dictionary<int, double> pendingEnergy = new Dictionary<int, double>();
            HashSet<int> duplicateNodes = new HashSet<int>();
            double duplicatePenalty = 0.0;
            for (int i = 0; i < route.Count; i++)
            {
                int nodeId = route[i].NodeId;
                if (nodeId <= 0 || nodeId >= sensors.Length || !sensors[nodeId].Alive)
                {
                    duplicatePenalty += 1.0e7;
                    continue;
                }
                if (duplicateNodes.Contains(nodeId))
                {
                    duplicatePenalty += 1.0e7;
                    continue;
                }
                duplicateNodes.Add(nodeId);
                pendingEnergy[nodeId] = sensors[nodeId].EnergyJ;
            }

            double trialTime = currentTime;
            double wcvEnergy = settings.WcvCapacityJ;
            double x = artifact.BaseX;
            double y = artifact.BaseY;
            double totalDistance = 0.0;
            double totalLateness = 0.0;
            double energyPenalty = 0.0;
            double deathPenalty = 0.0;
            int successfulTasks = 0;
            HashSet<int> deadPenaltyNodes = new HashSet<int>();

            for (int i = 0; i < route.Count; i++)
            {
                ChargingRequest request = route[i];
                if (!pendingEnergy.ContainsKey(request.NodeId))
                    continue;

                SensorState sensor = sensors[request.NodeId];
                double distance = ExperimentArtifact.Distance(x, y, sensor.X, sensor.Y);
                double returnDistance = ExperimentArtifact.Distance(sensor.X, sensor.Y, artifact.BaseX, artifact.BaseY);
                double moveEnergy = distance * settings.WcvMoveCostJPerMeter;
                double returnEnergy = returnDistance * settings.WcvMoveCostJPerMeter;
                if (wcvEnergy < moveEnergy + returnEnergy)
                {
                    energyPenalty += 1.0e8 + (moveEnergy + returnEnergy - wcvEnergy) * 1000.0;
                    break;
                }

                wcvEnergy -= moveEnergy;
                totalDistance += distance;
                double travelSeconds = distance / Math.Max(1e-9, settings.WcvSpeedMetersPerSecond);
                DrainPendingRouteEnergy(pendingEnergy, travelSeconds, -1);
                deathPenalty += CountNewDeadPendingNodes(pendingEnergy, deadPenaltyNodes) * 1.0e7;
                trialTime += travelSeconds;

                double targetEnergy = pendingEnergy[request.NodeId];
                if (targetEnergy <= Epsilon)
                {
                    deathPenalty += 1.0e8;
                    pendingEnergy.Remove(request.NodeId);
                    x = sensor.X;
                    y = sensor.Y;
                    continue;
                }

                double lateness = Math.Max(0.0, trialTime - request.DeadlineSeconds);
                totalLateness += lateness;
                double netRate = settings.WcvChargeRateJPerSecond - sensor.ConsumeRateJPerSecond;
                if (netRate <= 1e-9)
                {
                    energyPenalty += 1.0e8;
                    pendingEnergy.Remove(request.NodeId);
                    x = sensor.X;
                    y = sensor.Y;
                    continue;
                }

                double timeToFull = Math.Max(0.0, (sensor.CapacityJ - targetEnergy) / netRate);
                double timeToEmptyWcv = wcvEnergy / Math.Max(1e-9, settings.WcvChargeRateJPerSecond);
                double chargeSeconds = Math.Min(timeToFull, timeToEmptyWcv);
                if (chargeSeconds > 0.0)
                {
                    DrainPendingRouteEnergy(pendingEnergy, chargeSeconds, request.NodeId);
                    deathPenalty += CountNewDeadPendingNodes(pendingEnergy, deadPenaltyNodes) * 1.0e7;
                    targetEnergy += netRate * chargeSeconds;
                    wcvEnergy -= settings.WcvChargeRateJPerSecond * chargeSeconds;
                    trialTime += chargeSeconds;
                }

                if (targetEnergy < sensor.CapacityJ - 1e-5)
                    energyPenalty += 1.0e8 + (sensor.CapacityJ - targetEnergy) * 1000.0;
                else if (lateness <= Epsilon)
                    successfulTasks++;

                pendingEnergy.Remove(request.NodeId);
                x = sensor.X;
                y = sensor.Y;
            }

            double backDistance = ExperimentArtifact.Distance(x, y, artifact.BaseX, artifact.BaseY);
            totalDistance += backDistance;
            double backEnergy = backDistance * settings.WcvMoveCostJPerMeter;
            if (wcvEnergy < backEnergy)
                energyPenalty += 1.0e8 + (backEnergy - wcvEnergy) * 1000.0;

            int failedTasks = Math.Max(0, route.Count - successfulTasks);
            return duplicatePenalty +
                failedTasks * 1000000.0 +
                totalLateness * 100.0 +
                totalDistance +
                energyPenalty +
                deathPenalty;
        }

        private void DrainPendingRouteEnergy(Dictionary<int, double> pendingEnergy, double deltaSeconds, int excludedNodeId)
        {
            if (deltaSeconds <= 0.0 || pendingEnergy.Count == 0)
                return;

            List<int> keys = new List<int>(pendingEnergy.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                int nodeId = keys[i];
                if (nodeId == excludedNodeId)
                    continue;
                pendingEnergy[nodeId] = pendingEnergy[nodeId] - GetEffectiveConsumeRateJPerSecond(sensors[nodeId]) * deltaSeconds;
            }
        }

        private int CountNewDeadPendingNodes(Dictionary<int, double> pendingEnergy, HashSet<int> deadPenaltyNodes)
        {
            int count = 0;
            foreach (KeyValuePair<int, double> pair in pendingEnergy)
            {
                if (pair.Value <= Epsilon && !deadPenaltyNodes.Contains(pair.Key))
                {
                    deadPenaltyNodes.Add(pair.Key);
                    count++;
                }
            }
            return count;
        }

        private List<List<ChargingRequest>> BuildInitialOptimizationPopulation(List<ChargingRequest> pool, int maxTask, int populationSize)
        {
            List<List<ChargingRequest>> population = new List<List<ChargingRequest>>();
            AddOptimizationSeed(population, TakeSorted(pool, maxTask, CompareByDeadline), pool, maxTask);
            AddOptimizationSeed(population, BuildNearestRoute(pool, maxTask), pool, maxTask);
            AddOptimizationSeed(population, BuildCompositeRoute(pool, maxTask, 0.45, 0.35, 0.20), pool, maxTask);
            AddOptimizationSeed(population, BuildCompositeRoute(pool, maxTask, 0.20, 0.25, 0.55), pool, maxTask);

            while (population.Count < populationSize)
                population.Add(BuildRandomOptimizationRoute(pool, maxTask));

            return population;
        }

        private void AddOptimizationSeed(List<List<ChargingRequest>> population, List<ChargingRequest> route, List<ChargingRequest> pool, int maxTask)
        {
            List<ChargingRequest> normalized = NormalizeOptimizationRoute(route, pool, maxTask);
            if (normalized.Count > 0)
                population.Add(normalized);
        }

        private List<ChargingRequest> BuildRandomOptimizationRoute(List<ChargingRequest> pool, int maxTask)
        {
            List<ChargingRequest> shuffled = new List<ChargingRequest>(pool);
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int j = algorithmRandom.Next(i + 1);
                ChargingRequest temp = shuffled[i];
                shuffled[i] = shuffled[j];
                shuffled[j] = temp;
            }

            int routeSize = Math.Min(maxTask, shuffled.Count);
            List<ChargingRequest> route = new List<ChargingRequest>();
            for (int i = 0; i < routeSize; i++)
                route.Add(shuffled[i]);
            return route;
        }

        private List<ChargingRequest> NormalizeOptimizationRoute(List<ChargingRequest> route, List<ChargingRequest> pool, int maxTask)
        {
            int routeSize = Math.Min(maxTask, pool.Count);
            HashSet<int> used = new HashSet<int>();
            List<ChargingRequest> normalized = new List<ChargingRequest>();
            for (int i = 0; i < route.Count && normalized.Count < routeSize; i++)
            {
                ChargingRequest request = route[i];
                if (request == null || used.Contains(request.NodeId) || !PoolContainsNode(pool, request.NodeId))
                    continue;
                used.Add(request.NodeId);
                normalized.Add(request);
            }
            if (normalized.Count >= routeSize)
                return normalized;

            List<ChargingRequest> filler = BuildRandomOptimizationRoute(pool, pool.Count);
            for (int i = 0; i < filler.Count && normalized.Count < routeSize; i++)
            {
                if (used.Contains(filler[i].NodeId))
                    continue;
                used.Add(filler[i].NodeId);
                normalized.Add(filler[i]);
            }

            return normalized;
        }

        private bool PoolContainsNode(List<ChargingRequest> pool, int nodeId)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].NodeId == nodeId)
                    return true;
            }
            return false;
        }

        private List<ChargingRequest> CopyRoute(List<ChargingRequest> route)
        {
            return new List<ChargingRequest>(route);
        }

        private List<ChargingRequest> FindBestOptimizationRoute(List<List<ChargingRequest>> routes)
        {
            List<ChargingRequest> best = routes.Count == 0 ? new List<ChargingRequest>() : routes[0];
            double bestFitness = EvaluateRouteFitness(best);
            for (int i = 1; i < routes.Count; i++)
            {
                double fitness = EvaluateRouteFitness(routes[i]);
                if (fitness < bestFitness)
                {
                    bestFitness = fitness;
                    best = routes[i];
                }
            }
            return CopyRoute(best);
        }

        private List<ChargingRequest> FindBestOptimizationRouteExcept(List<List<ChargingRequest>> routes, List<ChargingRequest> excluded)
        {
            List<ChargingRequest> best = new List<ChargingRequest>();
            double bestFitness = Double.MaxValue;
            for (int i = 0; i < routes.Count; i++)
            {
                if (SameRoute(routes[i], excluded))
                    continue;
                double fitness = EvaluateRouteFitness(routes[i]);
                if (fitness < bestFitness)
                {
                    bestFitness = fitness;
                    best = routes[i];
                }
            }
            return CopyRoute(best);
        }

        private bool SameRoute(List<ChargingRequest> a, List<ChargingRequest> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return false;
            for (int i = 0; i < a.Count; i++)
            {
                if (a[i].NodeId != b[i].NodeId)
                    return false;
            }
            return true;
        }

        private List<ChargingRequest> TournamentSelectRoute(List<List<ChargingRequest>> population, int tournamentSize)
        {
            List<ChargingRequest> best = null;
            double bestFitness = Double.MaxValue;
            for (int i = 0; i < tournamentSize; i++)
            {
                List<ChargingRequest> candidate = population[algorithmRandom.Next(population.Count)];
                double fitness = EvaluateRouteFitness(candidate);
                if (best == null || fitness < bestFitness)
                {
                    best = candidate;
                    bestFitness = fitness;
                }
            }
            return CopyRoute(best);
        }

        private List<ChargingRequest> OrderedCrossoverRoute(List<ChargingRequest> parentA, List<ChargingRequest> parentB, List<ChargingRequest> pool, int maxTask)
        {
            int routeSize = Math.Min(maxTask, pool.Count);
            ChargingRequest[] child = new ChargingRequest[routeSize];
            HashSet<int> used = new HashSet<int>();
            int start = algorithmRandom.Next(routeSize);
            int end = algorithmRandom.Next(routeSize);
            if (start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            for (int i = start; i <= end && i < parentA.Count; i++)
            {
                child[i] = parentA[i];
                used.Add(parentA[i].NodeId);
            }

            int writeIndex = (end + 1) % routeSize;
            FillOrderedCrossoverSlots(child, used, parentB, ref writeIndex);
            FillOrderedCrossoverSlots(child, used, parentA, ref writeIndex);
            if (HasEmptyCrossoverSlot(child))
                FillOrderedCrossoverSlots(child, used, BuildRandomOptimizationRoute(pool, pool.Count), ref writeIndex);

            List<ChargingRequest> result = new List<ChargingRequest>();
            for (int i = 0; i < child.Length; i++)
            {
                if (child[i] != null)
                    result.Add(child[i]);
            }
            return NormalizeOptimizationRoute(result, pool, maxTask);
        }

        private bool HasEmptyCrossoverSlot(ChargingRequest[] child)
        {
            for (int i = 0; i < child.Length; i++)
            {
                if (child[i] == null)
                    return true;
            }
            return false;
        }

        private void FillOrderedCrossoverSlots(ChargingRequest[] child, HashSet<int> used, List<ChargingRequest> source, ref int writeIndex)
        {
            for (int i = 0; i < source.Count; i++)
            {
                if (used.Contains(source[i].NodeId))
                    continue;
                int safety = 0;
                while (child[writeIndex] != null && safety < child.Length)
                {
                    writeIndex = (writeIndex + 1) % child.Length;
                    safety++;
                }
                if (safety >= child.Length)
                    return;
                child[writeIndex] = source[i];
                used.Add(source[i].NodeId);
                writeIndex = (writeIndex + 1) % child.Length;
            }
        }

        private void MutateOptimizationRoute(List<ChargingRequest> route, List<ChargingRequest> pool, int maxTask, double mutationRate, bool allowReplacement)
        {
            if (route.Count < 2 || algorithmRandom.NextDouble() > mutationRate)
                return;

            int moveCount = 1;
            while (moveCount < route.Count && algorithmRandom.NextDouble() < 0.35)
                moveCount++;

            for (int move = 0; move < moveCount; move++)
                ApplyRandomPermutationMove(route, pool, maxTask, allowReplacement);
        }

        private void ApplyCuckooPermutationFlight(List<ChargingRequest> route, List<ChargingRequest> pool, int maxTask)
        {
            int moveCount = 1;
            while (moveCount < Math.Min(8, route.Count) && algorithmRandom.NextDouble() < 0.55)
                moveCount++;
            for (int i = 0; i < moveCount; i++)
                ApplyRandomPermutationMove(route, pool, maxTask, true);
        }

        private void ApplyRandomPermutationMove(List<ChargingRequest> route, List<ChargingRequest> pool, int maxTask, bool allowReplacement)
        {
            if (route.Count < 2)
                return;

            int operation = algorithmRandom.Next(allowReplacement ? 4 : 3);
            int a = algorithmRandom.Next(route.Count);
            int b = algorithmRandom.Next(route.Count);
            if (a > b)
            {
                int temp = a;
                a = b;
                b = temp;
            }

            if (operation == 0)
            {
                ChargingRequest temp = route[a];
                route[a] = route[b];
                route[b] = temp;
            }
            else if (operation == 1)
            {
                if (b > a)
                    route.Reverse(a, b - a + 1);
            }
            else if (operation == 2)
            {
                ChargingRequest item = route[b];
                route.RemoveAt(b);
                route.Insert(a, item);
            }
            else
            {
                ReplaceRandomRouteTask(route, pool);
            }
        }

        private void ReplaceRandomRouteTask(List<ChargingRequest> route, List<ChargingRequest> pool)
        {
            if (pool.Count <= route.Count)
                return;

            HashSet<int> used = new HashSet<int>();
            for (int i = 0; i < route.Count; i++)
                used.Add(route[i].NodeId);

            List<ChargingRequest> candidates = new List<ChargingRequest>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (!used.Contains(pool[i].NodeId))
                    candidates.Add(pool[i]);
            }
            if (candidates.Count == 0)
                return;

            int replaceIndex = algorithmRandom.Next(route.Count);
            route[replaceIndex] = candidates[algorithmRandom.Next(candidates.Count)];
        }

        private PsoParticle CreateRandomPsoParticle(List<ChargingRequest> pool)
        {
            PsoParticle particle = new PsoParticle();
            particle.Position = new double[pool.Count];
            particle.Velocity = new double[pool.Count];
            for (int i = 0; i < pool.Count; i++)
            {
                particle.Position[i] = algorithmRandom.NextDouble();
                particle.Velocity[i] = algorithmRandom.NextDouble() * 0.20 - 0.10;
            }
            return particle;
        }

        private PsoParticle CreatePsoParticleFromRoute(List<ChargingRequest> pool, List<ChargingRequest> route)
        {
            PsoParticle particle = CreateRandomPsoParticle(pool);
            for (int i = 0; i < particle.Position.Length; i++)
                particle.Position[i] = 0.50 + algorithmRandom.NextDouble() * 0.50;

            for (int rank = 0; rank < route.Count; rank++)
            {
                int index = FindPoolIndexByNodeId(pool, route[rank].NodeId);
                if (index >= 0)
                    particle.Position[index] = ((double)rank + 1.0) / ((double)pool.Count + 1.0) * 0.45;
            }
            return particle;
        }

        private int FindPoolIndexByNodeId(List<ChargingRequest> pool, int nodeId)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                if (pool[i].NodeId == nodeId)
                    return i;
            }
            return -1;
        }

        private List<ChargingRequest> BuildRouteFromRandomKeys(List<ChargingRequest> pool, double[] position, int maxTask)
        {
            List<RandomKeyItem> items = new List<RandomKeyItem>();
            for (int i = 0; i < pool.Count; i++)
            {
                RandomKeyItem item = new RandomKeyItem();
                item.Index = i;
                item.Key = position[i];
                items.Add(item);
            }

            items.Sort(delegate (RandomKeyItem a, RandomKeyItem b)
            {
                int compare = a.Key.CompareTo(b.Key);
                if (compare != 0)
                    return compare;
                return pool[a.Index].NodeId.CompareTo(pool[b.Index].NodeId);
            });

            int routeSize = Math.Min(maxTask, pool.Count);
            List<ChargingRequest> route = new List<ChargingRequest>();
            for (int i = 0; i < routeSize; i++)
                route.Add(pool[items[i].Index]);
            return route;
        }

        private static double[] CopyArray(double[] source)
        {
            double[] copy = new double[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private class RandomKeyItem
        {
            public int Index;
            public double Key;
        }

        private class PsoParticle
        {
            public double[] Position;
            public double[] Velocity;
            public double[] BestPosition;
            public double BestFitness;

            public PsoParticle CloneBestOnly()
            {
                PsoParticle clone = new PsoParticle();
                clone.Position = CopyArray(Position);
                clone.Velocity = CopyArray(Velocity);
                clone.BestPosition = CopyArray(BestPosition);
                clone.BestFitness = BestFitness;
                return clone;
            }
        }

        private List<ChargingRequest> ImproveRouteByTwoOpt(List<ChargingRequest> route)
        {
            if (route.Count < 4)
                return route;

            bool improved = true;
            int rounds = 0;
            while (improved && rounds < 20)
            {
                improved = false;
                rounds++;
                for (int i = 0; i < route.Count - 2; i++)
                {
                    for (int k = i + 2; k < route.Count - 1; k++)
                    {
                        double oldCost = RouteSegmentCost(route, i, k);
                        route.Reverse(i + 1, k - i);
                        double newCost = RouteSegmentCost(route, i, k);
                        if (newCost + Epsilon < oldCost)
                        {
                            improved = true;
                        }
                        else
                        {
                            route.Reverse(i + 1, k - i);
                        }
                    }
                }
            }

            return route;
        }

        private double RouteSegmentCost(List<ChargingRequest> route, int i, int k)
        {
            double x0 = i == 0 ? artifact.BaseX : sensors[route[i].NodeId].X;
            double y0 = i == 0 ? artifact.BaseY : sensors[route[i].NodeId].Y;
            double x1 = sensors[route[i + 1].NodeId].X;
            double y1 = sensors[route[i + 1].NodeId].Y;
            double x2 = sensors[route[k].NodeId].X;
            double y2 = sensors[route[k].NodeId].Y;
            double x3 = k + 1 >= route.Count ? artifact.BaseX : sensors[route[k + 1].NodeId].X;
            double y3 = k + 1 >= route.Count ? artifact.BaseY : sensors[route[k + 1].NodeId].Y;
            return ExperimentArtifact.Distance(x0, y0, x1, y1) + ExperimentArtifact.Distance(x2, y2, x3, y3);
        }

        private double FuzzyPriority(double residualRatio, double distanceRatio, double rateRatio, double densityRatio, double routingRatio)
        {
            double reLow = LeftShoulder(residualRatio, 0.15, 0.45);
            double reMid = Triangle(residualRatio, 0.20, 0.50, 0.80);
            double reHigh = RightShoulder(residualRatio, 0.55, 0.90);

            double dNear = LeftShoulder(distanceRatio, 0.15, 0.45);
            double dMid = Triangle(distanceRatio, 0.20, 0.50, 0.80);
            double dFar = RightShoulder(distanceRatio, 0.55, 0.90);

            double rateLow = LeftShoulder(rateRatio, 0.80, 1.10);
            double rateMid = Triangle(rateRatio, 0.90, 1.25, 1.65);
            double rateHigh = RightShoulder(rateRatio, 1.35, 1.90);

            double denLow = LeftShoulder(densityRatio, 0.15, 0.35);
            double denMid = Triangle(densityRatio, 0.20, 0.50, 0.80);
            double denHigh = RightShoulder(densityRatio, 0.60, 0.90);

            double numerator = 0.0;
            double denominator = 0.0;
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dNear, rateHigh, denHigh, 0.95);
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dNear, rateHigh, denMid, 0.92);
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dMid, rateHigh, denHigh, 0.90);
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dFar, rateHigh, denHigh, 0.84);
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dNear, rateMid, denHigh, 0.88);
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dMid, rateMid, denMid, 0.78);
            AccumulateFuzzyRule(ref numerator, ref denominator, reLow, dFar, rateLow, denLow, 0.58);
            AccumulateFuzzyRule(ref numerator, ref denominator, reMid, dNear, rateHigh, denHigh, 0.80);
            AccumulateFuzzyRule(ref numerator, ref denominator, reMid, dNear, rateMid, denMid, 0.64);
            AccumulateFuzzyRule(ref numerator, ref denominator, reMid, dMid, rateHigh, denHigh, 0.68);
            AccumulateFuzzyRule(ref numerator, ref denominator, reMid, dFar, rateHigh, denHigh, 0.56);
            AccumulateFuzzyRule(ref numerator, ref denominator, reHigh, dNear, rateHigh, denHigh, 0.46);
            AccumulateFuzzyRule(ref numerator, ref denominator, reHigh, dNear, rateLow, denLow, 0.22);
            AccumulateFuzzyRule(ref numerator, ref denominator, reHigh, dFar, rateLow, denLow, 0.08);

            double analyticBias =
                (1.0 - residualRatio) * 0.36 +
                (1.0 - distanceRatio) * 0.16 +
                Math.Min(1.0, rateRatio / 2.0) * 0.22 +
                densityRatio * 0.12 +
                Math.Min(1.0, routingRatio) * 0.14;
            if (denominator <= 1e-9)
                return analyticBias;

            return ExperimentSettings.Clamp(numerator / denominator * 0.75 + analyticBias * 0.25, 0.0, 1.0);
        }

        private static void AccumulateFuzzyRule(ref double numerator, ref double denominator, double a, double b, double c, double d, double output)
        {
            double strength = Math.Min(Math.Min(a, b), Math.Min(c, d));
            if (strength <= 0.0)
                return;
            numerator += strength * output;
            denominator += strength;
        }

        private static double LeftShoulder(double x, double a, double b)
        {
            if (x <= a)
                return 1.0;
            if (x >= b)
                return 0.0;
            return (b - x) / (b - a);
        }

        private static double RightShoulder(double x, double a, double b)
        {
            if (x <= a)
                return 0.0;
            if (x >= b)
                return 1.0;
            return (x - a) / (b - a);
        }

        private static double Triangle(double x, double a, double b, double c)
        {
            if (x <= a || x >= c)
                return 0.0;
            if (Math.Abs(x - b) < 1e-9)
                return 1.0;
            if (x < b)
                return (x - a) / (b - a);
            return (c - x) / (c - b);
        }

        private double ComputeCriticalNodeDensity(int nodeId)
        {
            SensorState center = sensors[nodeId];
            double radius = settings.RadioRangeMeters * 1.5;
            int total = 0;
            int critical = 0;
            for (int i = 1; i < sensors.Length; i++)
            {
                if (i == nodeId || !sensors[i].Alive)
                    continue;
                double distance = ExperimentArtifact.Distance(center.X, center.Y, sensors[i].X, sensors[i].Y);
                if (distance <= radius)
                {
                    total++;
                    double threshold = GetRequestThresholdJ(sensors[i]);
                    if (sensors[i].EnergyJ <= threshold * 1.25 || sensors[i].HasPendingRequest)
                        critical++;
                }
            }
            if (total == 0)
                return 0.0;
            return ExperimentSettings.Clamp((double)critical / (double)total, 0.0, 1.0);
        }

        private double DistanceFrom(double x, double y, int nodeId)
        {
            return ExperimentArtifact.Distance(x, y, sensors[nodeId].X, sensors[nodeId].Y);
        }

        private void AdvanceTo(double targetTime, ChargingContext charging)
        {
            targetTime = Math.Min(targetTime, settings.SimulationTimeSeconds);
            int safety = 0;
            while (currentTime < targetTime - Epsilon && !stopForFirstDeath && safety < 1000000)
            {
                safety++;
                double next = FindNextInterestingTime(targetTime, charging);
                if (next <= currentTime + Epsilon)
                    next = Math.Min(targetTime, currentTime + 0.001);

                double delta = next - currentTime;
                ApplyContinuousEnergy(delta, charging);
                currentTime = next;

                ApplyRateChangesAtCurrentTime();
                ProcessPacketEventsAtCurrentTime();
                CreateRequestsAtCurrentTime();
                CheckDeadSensors("continuous");

                if (charging != null)
                {
                    if (charging.WcvEnergyJ <= 1e-9)
                        break;
                    SensorState target = sensors[charging.NodeId];
                    if (!target.Alive || target.EnergyJ >= target.CapacityJ - 1e-6)
                        break;
                }
            }
        }

        private double FindNextInterestingTime(double upperBound, ChargingContext charging)
        {
            double next = upperBound;
            if (nextEventIndex < artifact.PacketEvents.Count)
                next = Math.Min(next, artifact.PacketEvents[nextEventIndex].TimeSeconds);
            if (nextRateChangeIndex < artifact.RateChanges.Count)
                next = Math.Min(next, artifact.RateChanges[nextRateChangeIndex].TimeSeconds);

            double requestTime = FindNextRequestTime(charging);
            if (requestTime >= currentTime - Epsilon)
                next = Math.Min(next, requestTime);

            double deathTime = FindNextDeathTime(charging);
            if (deathTime >= currentTime - Epsilon)
                next = Math.Min(next, deathTime);

            if (charging != null && charging.WcvEnergyJ > 0.0)
            {
                SensorState target = sensors[charging.NodeId];
                if (target.Alive)
                {
                    double netRate = settings.WcvChargeRateJPerSecond - target.ConsumeRateJPerSecond;
                    if (netRate > 1e-9 && target.EnergyJ < target.CapacityJ)
                        next = Math.Min(next, currentTime + (target.CapacityJ - target.EnergyJ) / netRate);
                    next = Math.Min(next, currentTime + charging.WcvEnergyJ / settings.WcvChargeRateJPerSecond);
                }
            }

            return next;
        }

        private double FindNextRequestTime(ChargingContext charging)
        {
            double best = Double.PositiveInfinity;
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive || sensor.HasPendingRequest || IsNodeReservedForCurrentMission(id))
                    continue;
                if (HasActiveRequestForNode(id))
                {
                    sensor.HasPendingRequest = true;
                    continue;
                }

                double threshold = GetRequestThresholdJ(sensor);
                if (sensor.EnergyJ <= threshold + Epsilon)
                    return currentTime;

                double consumeRate = sensor.ConsumeRateJPerSecond;
                if (charging != null && charging.NodeId == id)
                    consumeRate -= charging.ChargeRateJPerSecond;
                if (consumeRate <= 1e-12)
                    continue;

                best = Math.Min(best, currentTime + (sensor.EnergyJ - threshold) / consumeRate);
            }
            return best;
        }

        private double FindNextDeathTime(ChargingContext charging)
        {
            double best = Double.PositiveInfinity;
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive)
                    continue;
                if (sensor.EnergyJ <= Epsilon)
                    return currentTime;

                double consumeRate = sensor.ConsumeRateJPerSecond;
                if (charging != null && charging.NodeId == id)
                    consumeRate -= charging.ChargeRateJPerSecond;
                if (consumeRate <= 1e-12)
                    continue;

                best = Math.Min(best, currentTime + sensor.EnergyJ / consumeRate);
            }
            return best;
        }

        private void ApplyContinuousEnergy(double deltaSeconds, ChargingContext charging)
        {
            if (deltaSeconds <= 0.0)
                return;

            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive)
                    continue;
                sensor.EnergyJ -= sensor.ConsumeRateJPerSecond * deltaSeconds;
            }

            if (charging != null && charging.NodeId > 0 && charging.NodeId < sensors.Length)
            {
                SensorState target = sensors[charging.NodeId];
                if (target.Alive && target.EnergyJ < target.CapacityJ && charging.WcvEnergyJ > 0.0)
                {
                    double requested = charging.ChargeRateJPerSecond * deltaSeconds;
                    double delivered = Math.Min(requested, charging.WcvEnergyJ);
                    delivered = Math.Min(delivered, Math.Max(0.0, target.CapacityJ - target.EnergyJ));
                    target.EnergyJ += delivered;
                    charging.WcvEnergyJ -= delivered;
                    charging.DeliveredEnergyJ += delivered;
                }
            }
        }

        private void ApplyRateChangesAtCurrentTime()
        {
            while (nextRateChangeIndex < artifact.RateChanges.Count &&
                artifact.RateChanges[nextRateChangeIndex].TimeSeconds <= currentTime + Epsilon)
            {
                RateChangeTemplate change = artifact.RateChanges[nextRateChangeIndex];
                if (change.NodeId > 0 && change.NodeId < sensors.Length && sensors[change.NodeId].Alive)
                {
                    SensorState sensor = sensors[change.NodeId];
                    double oldRate = sensor.ConsumeRateJPerSecond;
                    BprSTableEntry entry = GetOrCreateBprSTableEntry(change.NodeId);
                    double oldDeadline = entry.LatestReportedDeadlineSeconds;
                    sensor.RateScale *= change.Multiplier;
                    sensor.RefreshConsumeRate(settings);
                    RefreshRoutingLoadEstimateForNode(change.NodeId);
                    double newDeadline = ComputeBprRequestDeadlineSeconds(sensor);
                    double threshold = GetBprDeadlineThresholdSeconds();
                    bool updated = ShouldUpdateBprDeadline(oldDeadline, newDeadline, threshold);
                    RefreshBprDeadlineAfterRateChange(change.NodeId);
                    Debug.WriteLine(String.Format(CultureInfo.InvariantCulture,
                        "[BPR_STABLE] node={0}, reason=rate_change, oldDeadline={1}, newDeadline={2}, threshold={3}, updated={4}, oldRate={5}, newRate={6}",
                        change.NodeId,
                        FormatBprDeadlineForLog(oldDeadline),
                        FormatBprDeadlineForLog(newDeadline),
                        threshold,
                        updated ? "true" : "false",
                        oldRate,
                        sensor.ConsumeRateJPerSecond));
                    summary.AppliedRateChangeCount++;
                }
                nextRateChangeIndex++;
            }
        }

        private void ProcessPacketEventsAtCurrentTime()
        {
            while (nextEventIndex < artifact.PacketEvents.Count &&
                artifact.PacketEvents[nextEventIndex].TimeSeconds <= currentTime + Epsilon &&
                !stopForFirstDeath)
            {
                ProcessPacketEvent(artifact.PacketEvents[nextEventIndex]);
                nextEventIndex++;
            }
        }

        private void ProcessPacketEvent(PacketEventTemplate packetEvent)
        {
            if (packetEvent.SourceId <= 0 || packetEvent.SourceId >= sensors.Length)
                return;
            SensorState source = sensors[packetEvent.SourceId];
            if (!source.Alive)
            {
                summary.PacketLost++;
                return;
            }

            int current = packetEvent.SourceId;
            int guard = 0;
            bool delivered = true;
            bool routingFailed = false;
            while (current != 0 && guard < sensors.Length + 1)
            {
                guard++;
                SensorState sender = sensors[current];
                int parent = sender.ParentId;
                if (parent < 0 || parent >= sensors.Length)
                {
                    routingFailed = true;
                    delivered = false;
                    break;
                }

                double txEnergy = TxEnergyJ(sender, packetEvent.PacketBits);
                sender.EnergyJ -= txEnergy;
                RefreshBprSTableEntry(sender.Id, "packet_tx", false);
                summary.PacketSent++;
                if (sender.EnergyJ <= Epsilon)
                {
                    MarkDead(sender.Id, currentTime, current == packetEvent.SourceId ? "packet_tx" : "packet_forward");
                    delivered = false;
                    break;
                }

                if (parent != 0)
                {
                    SensorState receiver = sensors[parent];
                    if (!receiver.Alive)
                    {
                        delivered = false;
                        break;
                    }
                    double rxEnergy = RxEnergyJ(receiver, packetEvent.PacketBits);
                    receiver.EnergyJ -= rxEnergy;
                    RefreshBprSTableEntry(receiver.Id, "packet_rx", false);
                    if (receiver.EnergyJ <= Epsilon)
                    {
                        MarkDead(receiver.Id, currentTime, "packet_rx");
                        delivered = false;
                        break;
                    }
                }

                current = parent;
            }

            if (delivered && current != 0)
            {
                routingFailed = true;
                delivered = false;
            }

            if (delivered && current == 0)
                summary.PacketReceived++;
            else
            {
                summary.PacketLost++;
                if (routingFailed)
                    summary.RoutingFailedPacketLost++;
            }
        }

        private double TxEnergyJ(SensorState sensor, double bits)
        {
            double unitNj = settings.ReceiverEnergyNjPerBit +
                Math.Pow(settings.RadioRangeMeters, settings.PowerExponent) * settings.AmplifierEnergyNjPerBitM2;
            return unitNj * bits * sensor.RateScale * 1e-9;
        }

        private double RxEnergyJ(SensorState sensor, double bits)
        {
            return settings.ReceiverEnergyNjPerBit * bits * sensor.RateScale * 1e-9;
        }

        private void CreateRequestsAtCurrentTime()
        {
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive || sensor.HasPendingRequest || IsNodeReservedForCurrentMission(id))
                    continue;
                if (HasActiveRequestForNode(id))
                {
                    sensor.HasPendingRequest = true;
                    RefreshBprSTableEntry(id, "natural_request", true);
                    continue;
                }

                double threshold = GetRequestThresholdJ(sensor);
                if (sensor.EnergyJ <= threshold + Epsilon)
                {
                    ChargingRequest request = new ChargingRequest();
                    request.RequestId = nextRequestId++;
                    request.NodeId = id;
                    request.RequestTimeSeconds = currentTime;
                    double effectiveRate = GetEffectiveConsumeRateJPerSecond(sensor);
                    request.DeadlineSeconds = currentTime + Math.Max(0.0, sensor.EnergyJ / Math.Max(effectiveRate, 1e-12));
                    request.RequestEnergyJ = sensor.EnergyJ;
                    PopulateChargingRequestRoutingFields(request, sensor);
                    request.CriticalDensity = ComputeCriticalNodeDensity(id);
                    request.IsProactive = false;
                    request.ProactiveReason = "";
                    sensor.HasPendingRequest = true;
                    activeRequests.Add(request);
                    RefreshBprSTableEntry(id, "natural_request", true);
                    summary.NaturalRequestCount++;
                }
            }
        }

        private double GetRequestThresholdJ(SensorState sensor)
        {
            if (settings.ThresholdMode == "TreqSeconds")
            {
                double serviceFloor = Math.Max(TxEnergyJ(sensor, settings.PacketBits) * 2.0, settings.InitialEnergyJ * 0.005);
                return Math.Min(sensor.CapacityJ * 0.95, sensor.ConsumeRateJPerSecond * settings.TreqSeconds + serviceFloor);
            }

            return sensor.CapacityJ * settings.RequestThresholdPercent / 100.0;
        }

        private void CheckDeadSensors(string directReason)
        {
            for (int id = 1; id < sensors.Length; id++)
            {
                if (sensors[id].Alive && sensors[id].EnergyJ <= Epsilon)
                {
                    MarkDead(id, currentTime, directReason);
                    return;
                }
            }
        }

        private void MarkDead(int nodeId, double time, string directReason)
        {
            if (nodeId <= 0 || nodeId >= sensors.Length || !sensors[nodeId].Alive)
                return;

            SensorState sensor = sensors[nodeId];
            string directCause = String.IsNullOrWhiteSpace(directReason) ? "unknown" : directReason;
            double energyBeforeDeathJ = sensor.EnergyJ;
            bool hasPendingRequestAtDeath = sensor.HasPendingRequest || HasActiveRequestForNode(nodeId);
            bool wasScheduledInCurrentMissionAtDeath = IsNodeReservedForCurrentMission(nodeId);
            int parentIdAtDeath = sensor.ParentId;
            bool schedulingRelated = hasPendingRequestAtDeath || wasScheduledInCurrentMissionAtDeath;
            string reason = directCause;
            string reasonZh = ReasonZh(reason);
            string directEnergyCauseZh = ReasonZh(directCause);
            string schedulingCause = schedulingRelated ? "scheduling_wait" : "";
            string schedulingCauseZh = schedulingRelated ? ReasonZh("scheduling_wait") : "";
            double routingTxLoadAtDeath = GetRoutingTxLoadJPerSecond(sensor);
            double routingRxLoadAtDeath = GetRoutingRxLoadJPerSecond(sensor);
            double routingLoadAtDeath = routingTxLoadAtDeath + routingRxLoadAtDeath;

            sensor.Alive = false;
            sensor.EnergyJ = 0.0;

            ExperimentDeathRecord death = new ExperimentDeathRecord();
            death.RunIndex = artifact.RunIndex;
            death.Seed = artifact.Seed;
            death.Algorithm = algorithm;
            death.ArtifactHash = artifact.ArtifactHash;
            death.TimeSeconds = time;
            death.NodeId = nodeId;
            death.Reason = reason;
            death.ReasonZh = reasonZh;
            death.DirectEnergyCause = directCause;
            death.DirectEnergyCauseZh = directEnergyCauseZh;
            death.SchedulingRelated = schedulingRelated;
            death.SchedulingCause = schedulingCause;
            death.SchedulingCauseZh = schedulingCauseZh;
            death.PendingRequest = hasPendingRequestAtDeath;
            death.HasPendingRequestAtDeath = hasPendingRequestAtDeath;
            death.WasScheduledInCurrentMissionAtDeath = wasScheduledInCurrentMissionAtDeath;
            death.ParentIdAtDeath = parentIdAtDeath;
            death.EnergyBeforeDeathJ = energyBeforeDeathJ;
            death.BaseConsumeRateJPerSecondAtDeath = sensor.ConsumeRateJPerSecond;
            death.RoutingTxLoadJPerSecondAtDeath = routingTxLoadAtDeath;
            death.RoutingRxLoadJPerSecondAtDeath = routingRxLoadAtDeath;
            death.RoutingLoadJPerSecondAtDeath = routingLoadAtDeath;
            death.EffectiveConsumeRateJPerSecondAtDeath = sensor.ConsumeRateJPerSecond + routingLoadAtDeath;
            death.RoutingSubtreeSize = GetRoutingSubtreeSize(nodeId);
            death.ExpectedRoutingForwardPacketsPerSecond = GetExpectedRoutingForwardPacketsPerSecond(nodeId);
            death.EnergyJ = sensor.EnergyJ;
            death.RequestTimeSeconds = FindRequestTimeForNode(nodeId);
            death.WaitSeconds = death.RequestTimeSeconds >= 0.0 ? Math.Max(0.0, time - death.RequestTimeSeconds) : 0.0;
            deaths.Add(death);

            if (summary.FirstDeadNodeId < 0)
            {
                summary.FirstDeadNodeId = nodeId;
                summary.FirstDeadTimeSeconds = time;
                summary.FirstDeadReason = reason;
                summary.FirstDeadReasonZh = reasonZh;
                summary.FirstDeadDirectEnergyCause = directCause;
                summary.FirstDeadDirectEnergyCauseZh = directEnergyCauseZh;
                summary.FirstDeadSchedulingRelated = schedulingRelated;
                summary.FirstDeadSchedulingCause = schedulingCause;
                summary.FirstDeadSchedulingCauseZh = schedulingCauseZh;
                summary.NetworkLifetimeSeconds = time;
                stopForFirstDeath = true;
            }

            sensor.HasPendingRequest = false;
            for (int i = activeRequests.Count - 1; i >= 0; i--)
            {
                if (activeRequests[i].NodeId == nodeId)
                    activeRequests.RemoveAt(i);
            }
            if (plannedMissionNodeIds != null)
                plannedMissionNodeIds.Remove(nodeId);
            RefreshBprSTableEntry(nodeId, "dead:" + directCause, true);
        }

        private double FindRequestTimeForNode(int nodeId)
        {
            for (int i = 0; i < activeRequests.Count; i++)
            {
                if (activeRequests[i].NodeId == nodeId)
                    return activeRequests[i].RequestTimeSeconds;
            }
            return -1.0;
        }

        private string ReasonZh(string reason)
        {
            if (reason == "scheduling_wait")
                return "排程等待中耗盡";
            if (reason == "packet_tx")
                return "封包傳送耗能耗盡";
            if (reason == "packet_forward")
                return "封包轉送耗能耗盡";
            if (reason == "packet_rx")
                return "封包接收耗能耗盡";
            if (reason == "charging")
                return "充電過程中耗能耗盡";
            if (reason == "continuous")
                return "連續耗能耗盡";
            if (reason == "unknown")
                return "未知";
            return reason;
        }

        private void RemoveCompletedOrDeadRequests()
        {
            for (int i = activeRequests.Count - 1; i >= 0; i--)
            {
                if (!sensors[activeRequests[i].NodeId].Alive)
                    activeRequests.RemoveAt(i);
            }
        }

        internal static void RunReservedNodeRequestSelfTest()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(),
                "wsn-reserved-node-test-" + Guid.NewGuid().ToString("N"));
            List<ExperimentSimulation> simulations = new List<ExperimentSimulation>();
            try
            {
                ExperimentSettings maintenanceSettings = CreateBprSelfTestSettings(tempDirectory);
                maintenanceSettings.BprDeadlineThresholdSeconds = 1000.0;
                maintenanceSettings.Normalize();
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_BPR") == "NJF_ZHENG_BPR",
                    "Legacy NJF_BPR key should map to NJF_ZHENG_BPR.");
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_BPR_ROUTE_SAFE_LIMITED") == "NJF_ROUTE_ZHENG_BPR_LIMITED",
                    "Legacy limited route-safe key should map to NJF_ROUTE_ZHENG_BPR_LIMITED.");
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_BPR_ROUTE_SAFE_EXTENDED") == "NJF_ROUTE_ZHENG_BPR_EXTENDED",
                    "Legacy extended route-safe key should map to NJF_ROUTE_ZHENG_BPR_EXTENDED.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_YU_BPR") >= 0,
                    "AllAlgorithms should include NJF_YU_BPR.");

                RunStandaloneDispatchSelfTest(tempDirectory, simulations);
                RunActiveRequestProactiveSelfTest(tempDirectory, simulations);
                RunProactiveCandidateFilterSelfTest(tempDirectory, simulations);
                RunSimulationEndBeforeFullSelfTest(tempDirectory, simulations);
                RunDeathReasonSelfTest(tempDirectory, simulations);
                RunRoutingLoadDeadlinePrioritySelfTest(tempDirectory, simulations);
                RunCompleteBprYuPredictionSelfTest(tempDirectory, simulations);

                ExperimentArtifact maintenanceArtifact = CreateBprSelfTestArtifact(new double[] { 100.0, 100.0, 100.0 });
                ExperimentSimulation maintenanceSimulation = new ExperimentSimulation(
                    maintenanceSettings,
                    maintenanceArtifact,
                    "NJF_ROUTE_ZHENG_BPR_LIMITED",
                    null);
                simulations.Add(maintenanceSimulation);

                AssertSelfTest(maintenanceSimulation.bprSTableByNodeId.Count == maintenanceSimulation.sensors.Length - 1,
                    "Every sensor node should have a persistent BP&R STable entry after initialization.");
                for (int nodeId = 1; nodeId < maintenanceSimulation.sensors.Length; nodeId++)
                {
                    AssertSelfTest(maintenanceSimulation.bprSTableByNodeId.ContainsKey(nodeId),
                        "Persistent BP&R STable is missing a sensor entry.");
                }

                BprSTableEntry node1Entry = maintenanceSimulation.bprSTableByNodeId[1];
                double initialDeadline = node1Entry.LatestReportedDeadlineSeconds;
                maintenanceSimulation.currentTime = 5.0;
                maintenanceSimulation.sensors[1].EnergyJ = 99.5;
                maintenanceSimulation.sensors[1].RateScale = 1.01;
                maintenanceSimulation.sensors[1].RefreshConsumeRate(maintenanceSettings);
                double smallChangeRate = maintenanceSimulation.sensors[1].ConsumeRateJPerSecond;
                maintenanceSimulation.RefreshBprDeadlineAfterRateChange(1);
                AssertNear(maintenanceSimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds,
                    initialDeadline,
                    1e-9,
                    "Small rate changes below BprDeadlineThresholdSeconds should not update the reported deadline.");
                AssertNear(maintenanceSimulation.bprSTableByNodeId[1].EnergyJ,
                    99.5,
                    1e-9,
                    "Small rate changes should still update the BP&R STable energy snapshot.");
                AssertNear(maintenanceSimulation.bprSTableByNodeId[1].ConsumeRateJPerSecond,
                    smallChangeRate,
                    1e-9,
                    "Small rate changes should still update the BP&R STable consume-rate snapshot.");
                AssertSelfTest(maintenanceSimulation.bprSTableByNodeId[1].LastUpdateReason == "rate_change_deadline_unchanged",
                    "Small rate-change STable update should record that the deadline was unchanged.");

                maintenanceSimulation.sensors[1].EnergyJ = 100.0;
                maintenanceSimulation.sensors[1].RateScale = 0.01;
                maintenanceSimulation.sensors[1].RefreshConsumeRate(maintenanceSettings);
                double expectedLargeChangeDeadline = maintenanceSimulation.ComputeBprRequestDeadlineSeconds(maintenanceSimulation.sensors[1]);
                maintenanceSimulation.RefreshBprDeadlineAfterRateChange(1);
                AssertNear(maintenanceSimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds,
                    expectedLargeChangeDeadline,
                    1e-9,
                    "Large rate changes at or above BprDeadlineThresholdSeconds should update the reported deadline.");
                AssertSelfTest(maintenanceSimulation.bprSTableByNodeId[1].LastUpdateReason == "rate_change_deadline_updated",
                    "Large rate-change STable update should record the rate_change_deadline_updated reason.");

                ExperimentSettings infinityToFiniteSettings = CreateBprSelfTestSettings(tempDirectory);
                infinityToFiniteSettings.BprDeadlineThresholdSeconds = 1000.0;
                infinityToFiniteSettings.SensorBackgroundLifetimeSeconds = 1e20;
                infinityToFiniteSettings.Normalize();
                ExperimentSimulation infinityToFiniteSimulation = new ExperimentSimulation(
                    infinityToFiniteSettings,
                    CreateBprSelfTestArtifact(new double[] { 100.0 }),
                    "NJF_ROUTE_ZHENG_BPR_LIMITED",
                    null);
                simulations.Add(infinityToFiniteSimulation);
                AssertSelfTest(Double.IsPositiveInfinity(infinityToFiniteSimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds),
                    "Near-zero consume rate should initialize the BP&R deadline as Infinity.");
                infinityToFiniteSettings.SensorBackgroundLifetimeSeconds = 100.0;
                infinityToFiniteSimulation.sensors[1].RateScale = 1.0;
                infinityToFiniteSimulation.sensors[1].RefreshConsumeRate(infinityToFiniteSettings);
                double expectedFiniteDeadline = infinityToFiniteSimulation.ComputeBprRequestDeadlineSeconds(infinityToFiniteSimulation.sensors[1]);
                infinityToFiniteSimulation.RefreshBprDeadlineAfterRateChange(1);
                AssertNear(infinityToFiniteSimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds,
                    expectedFiniteDeadline,
                    1e-9,
                    "BP&R deadline should update when rate-change moves deadline from Infinity to finite.");
                AssertSelfTest(infinityToFiniteSimulation.bprSTableByNodeId[1].LastUpdateReason == "rate_change_deadline_updated",
                    "Infinity-to-finite rate change should record the updated reason.");

                ExperimentSettings finiteToInfinitySettings = CreateBprSelfTestSettings(tempDirectory);
                finiteToInfinitySettings.BprDeadlineThresholdSeconds = 1000.0;
                finiteToInfinitySettings.Normalize();
                ExperimentSimulation finiteToInfinitySimulation = new ExperimentSimulation(
                    finiteToInfinitySettings,
                    CreateBprSelfTestArtifact(new double[] { 100.0 }),
                    "NJF_ROUTE_ZHENG_BPR_LIMITED",
                    null);
                simulations.Add(finiteToInfinitySimulation);
                AssertSelfTest(!Double.IsInfinity(finiteToInfinitySimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds),
                    "Self-test finite-to-Infinity setup should begin with a finite BP&R deadline.");
                finiteToInfinitySettings.SensorBackgroundLifetimeSeconds = 1e20;
                finiteToInfinitySimulation.sensors[1].RateScale = 1.0;
                finiteToInfinitySimulation.sensors[1].RefreshConsumeRate(finiteToInfinitySettings);
                finiteToInfinitySimulation.RefreshBprDeadlineAfterRateChange(1);
                AssertSelfTest(Double.IsPositiveInfinity(finiteToInfinitySimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds),
                    "BP&R deadline should update when rate-change moves deadline from finite to Infinity.");
                AssertSelfTest(finiteToInfinitySimulation.bprSTableByNodeId[1].LastUpdateReason == "rate_change_deadline_updated",
                    "Finite-to-Infinity rate change should record the updated reason.");

                ExperimentSettings packetSettings = CreateBprSelfTestSettings(tempDirectory);
                packetSettings.BprDeadlineThresholdSeconds = 1000.0;
                packetSettings.PacketBits = 1.0;
                packetSettings.Normalize();
                ExperimentSimulation packetSimulation = new ExperimentSimulation(
                    packetSettings,
                    CreateBprSelfTestArtifact(new double[] { 100.0 }),
                    "NJF_ROUTE_ZHENG_BPR_LIMITED",
                    null);
                simulations.Add(packetSimulation);
                double packetOldDeadline = packetSimulation.bprSTableByNodeId[1].LatestReportedDeadlineSeconds;
                double packetOldEnergy = packetSimulation.bprSTableByNodeId[1].EnergyJ;
                PacketEventTemplate packetEvent = new PacketEventTemplate();
                packetEvent.TimeSeconds = packetSimulation.currentTime;
                packetEvent.SourceId = 1;
                packetEvent.PacketBits = 1.0;
                packetSimulation.ProcessPacketEvent(packetEvent);
                BprSTableEntry packetEntry = packetSimulation.bprSTableByNodeId[1];
                AssertNear(packetEntry.LatestReportedDeadlineSeconds,
                    packetOldDeadline,
                    1e-9,
                    "Small packet_tx energy use should not update the reported BP&R deadline.");
                AssertSelfTest(packetEntry.EnergyJ < packetOldEnergy,
                    "Small packet_tx energy use should update the BP&R STable energy snapshot.");
                AssertNear(packetEntry.EnergyJ,
                    packetSimulation.sensors[1].EnergyJ,
                    1e-12,
                    "packet_tx STable energy snapshot should match the current sensor energy.");
                AssertSelfTest(packetEntry.LastUpdateReason == "packet_tx_snapshot_only",
                    "Small packet_tx energy use should record the packet_tx_snapshot_only reason.");

                ExperimentSettings njfSettings = CreateBprSelfTestSettings(tempDirectory);
                ExperimentSimulation njfSimulation = new ExperimentSimulation(
                    njfSettings,
                    CreateBprSelfTestArtifact(new double[] { 55.0, 90.0 }),
                    "NJF",
                    null);
                simulations.Add(njfSimulation);
                njfSimulation.Run();
                AssertSelfTest(njfSimulation.summary.ProactiveTaskCount == 0,
                    "NJF baseline must not create proactive prediction tasks.");

                ExperimentSettings yuSettings = CreateBprSelfTestSettings(tempDirectory);
                yuSettings.NmaxTask = 1;
                yuSettings.YuDangerWindowSeconds = 10.0;
                yuSettings.YuDangerThresholdK = 2;
                yuSettings.YuIntervalUncertaintySeconds = 10.0;
                yuSettings.Normalize();
                ExperimentArtifact yuArtifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0 });
                ExperimentSimulation yuLimitedSimulation = new ExperimentSimulation(
                    yuSettings,
                    yuArtifact,
                    "NJF_ROUTE_YU_BPR_LIMITED",
                    null);
                simulations.Add(yuLimitedSimulation);
                List<ChargingRequest> yuLimited = yuLimitedSimulation.BuildYuBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    false,
                    YuProactiveSelectionMode.RouteInsertionCost);
                AssertSelfTest(yuLimited.Count == 1,
                    "YU limited cplist must not exceed NmaxTask.");
                AssertSelfTest(yuLimited[0].ProactiveReason == YuBprDangerIntervalRemovalReason,
                    "YU limited proactive request should use the YU_BPR_DANGER_INTERVAL_REMOVAL reason.");

                ExperimentSimulation yuExtendedSimulation = new ExperimentSimulation(
                    yuSettings,
                    yuArtifact,
                    "NJF_ROUTE_YU_BPR_EXTENDED",
                    null);
                simulations.Add(yuExtendedSimulation);
                List<ChargingRequest> yuExtended = yuExtendedSimulation.BuildYuBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    true,
                    YuProactiveSelectionMode.RouteInsertionCost);
                AssertSelfTest(yuExtended.Count > 1,
                    "YU extended cplist should allow capacity overflow beyond NmaxTask.");
                AssertSelfTest(yuExtended[0].ProactiveReason == YuBprDangerIntervalRemovalReason,
                    "YU extended proactive request should use the YU_BPR_DANGER_INTERVAL_REMOVAL reason.");

                maintenanceSimulation.sensors[2].EnergyJ = 40.0;
                maintenanceSimulation.CreateRequestsAtCurrentTime();
                AssertSelfTest(maintenanceSimulation.bprSTableByNodeId[2].IsPendingRequest,
                    "Natural request creation should mark the STable entry pending.");
                AssertSelfTest(!ContainsBprEntry(maintenanceSimulation.GetEligibleBprSTableEntries(new HashSet<int>()), 2),
                    "Natural-request nodes must not be eligible for BP&R BottleList selection.");

                maintenanceSimulation.plannedMissionNodeIds = new HashSet<int>();
                maintenanceSimulation.plannedMissionNodeIds.Add(3);
                maintenanceSimulation.RefreshBprSTableEntry(3, "mission_scheduled", true);
                AssertSelfTest(!ContainsBprEntry(maintenanceSimulation.GetEligibleBprSTableEntries(new HashSet<int>()), 3),
                    "Scheduled mission nodes must not be eligible for BP&R BottleList selection in the same mission.");
                List<ChargingRequest> scheduledTrial = maintenanceSimulation.BuildZhengBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    BprProactiveSelectionMode.Deterministic,
                    false);
                AssertSelfTest(!ContainsChargingRequest(scheduledTrial, 3),
                    "Scheduled mission nodes must not be selected as proactive tasks in the same mission.");
                maintenanceSimulation.plannedMissionNodeIds = null;
                maintenanceSimulation.RefreshBprSTableEntry(3, "mission_released", true);

                maintenanceSimulation.currentTime = 10.0;
                maintenanceSimulation.sensors[3].EnergyJ = 100.0;
                maintenanceSimulation.sensors[3].RateScale = 1.5;
                maintenanceSimulation.sensors[3].RefreshConsumeRate(maintenanceSettings);
                maintenanceSimulation.RefreshBprSTableEntry(3, "charged", true);
                BprSTableEntry chargedEntry = maintenanceSimulation.bprSTableByNodeId[3];
                AssertNear(chargedEntry.EnergyJ, 100.0, 1e-9,
                    "Charged STable force update should refresh energy.");
                AssertNear(chargedEntry.ConsumeRateJPerSecond, 1.5, 1e-9,
                    "Charged STable force update should refresh consume rate.");
                AssertNear(chargedEntry.LatestReportedDeadlineSeconds, 10.0 + 50.0 / 1.5, 1e-9,
                    "Charged STable force update should refresh the reported deadline.");
                AssertSelfTest(chargedEntry.LastUpdateReason == "charged",
                    "Charged STable force update should record the charged reason.");

                ExperimentSettings missionSettings = CreateBprSelfTestSettings(tempDirectory);
                ExperimentArtifact missionArtifact = CreateBprSelfTestArtifact(new double[] { 55.0, 90.0 });
                ExperimentSimulation simulation = new ExperimentSimulation(
                    missionSettings,
                    missionArtifact,
                    "NJF_ROUTE_ZHENG_BPR_LIMITED",
                    tempDirectory);
                simulations.Add(simulation);
                simulation.CreateRequestsAtCurrentTime();
                AssertSelfTest(simulation.summary.NaturalRequestCount == 0,
                    "Self-test setup unexpectedly created a natural request before dispatch.");

                simulation.ExecuteMission();
                if (simulation.csvWriter != null)
                    simulation.csvWriter.Dispose();

                AssertSelfTest(simulation.summary.NaturalRequestCount == 0,
                    "Reserved proactive node produced a natural request during the same mission.");
                AssertSelfTest(simulation.summary.ProactiveTaskCount == 1,
                    "Proactive task count should be exactly one.");
                AssertSelfTest(simulation.summary.NaturalRequestCount + simulation.summary.ProactiveTaskCount == 1,
                    "Natural and proactive counts are not mutually exclusive.");
                AssertSelfTest(simulation.activeRequests.Count == 0,
                    "Reserved proactive node left a duplicate pending natural request.");
                AssertSelfTest(simulation.totalTaskRecordCount == 1,
                    "Mission should contain exactly one task record for the reserved node.");

                string taskPath = Path.Combine(tempDirectory,
                    "run001-NJF_ROUTE_ZHENG_BPR_LIMITED-task-records.csv");
                AssertSelfTest(File.Exists(taskPath), "Task-record CSV was not written by the self-test.");
                string[] taskLines = File.ReadAllLines(taskPath, Encoding.UTF8);
                AssertSelfTest(taskLines.Length == 2,
                    "Task-record CSV should contain one data row for the mission.");

                string[] fields = taskLines[1].Split(',');
                AssertSelfTest(fields.Length > 8 && fields[4] == "1" && fields[6] == "1",
                    "Task-record CSV row does not describe mission 1 / sensor 1.");
                AssertSelfTest(String.Equals(fields[7], "proactive", StringComparison.OrdinalIgnoreCase),
                    "Reserved node should remain a proactive task, not be relabeled as natural.");
                AssertSelfTest(fields.Length > 9 && fields[9] == ZhengBprWindowRemovalReason,
                    "ZHENG route proactive task should use the ZHENG_BPR_WINDOW_REMOVAL reason.");
            }
            finally
            {
                for (int i = 0; i < simulations.Count; i++)
                {
                    if (simulations[i] != null && simulations[i].csvWriter != null)
                        simulations[i].csvWriter.Dispose();
                }
                try
                {
                    if (Directory.Exists(tempDirectory))
                        Directory.Delete(tempDirectory, true);
                }
                catch
                {
                }
            }
        }

        private static ExperimentSettings CreateBprSelfTestSettings(string outputDirectory)
        {
            ExperimentSettings settings = ExperimentSettings.CreateDefault();
            settings.BaseSeed = 777;
            settings.RunCount = 1;
            settings.SensorCount = 3;
            settings.MapWidthMeters = 30.0;
            settings.MapHeightMeters = 30.0;
            settings.SimulationTimeSeconds = 60.0;
            settings.InitialEnergyJ = 100.0;
            settings.SensorBackgroundLifetimeSeconds = 100.0;
            settings.InitialResidualJitterPercent = 0.0;
            settings.EventRatePerSecond = 0.0;
            settings.PacketBits = 1.0;
            settings.RadioRangeMeters = 100.0;
            settings.WcvSpeedMetersPerSecond = 1.0;
            settings.WcvChargeRateJPerSecond = 10.0;
            settings.WcvCapacityJ = 10000.0;
            settings.WcvMoveCostJPerMeter = 0.0;
            settings.NmaxTask = 1;
            settings.DynamicNmaxTask = false;
            settings.ThresholdMode = "Percent";
            settings.RequestThresholdPercent = 50.0;
            settings.TreqSeconds = 10.0;
            settings.BprDeadlineThresholdSeconds = 10.0;
            settings.PrateChange = 0.0;
            settings.RateChangeVariationPercent = 0.0;
            settings.SelectedAlgorithmsCsv = "NJF_ROUTE_ZHENG_BPR_LIMITED";
            settings.OutputDirectory = outputDirectory;
            settings.Normalize();
            return settings;
        }

        private static ExperimentArtifact CreateBprSelfTestArtifact(double[] sensorEnergies)
        {
            ExperimentArtifact artifact = new ExperimentArtifact();
            artifact.RunIndex = 1;
            artifact.Seed = 777;
            artifact.ArtifactHash = "SELFTEST";
            artifact.BaseX = 0.0;
            artifact.BaseY = 0.0;

            SensorTemplate baseStation = new SensorTemplate();
            baseStation.Id = 0;
            baseStation.X = 0.0;
            baseStation.Y = 0.0;
            baseStation.InitialEnergyJ = Double.PositiveInfinity;
            baseStation.ParentId = -1;
            artifact.Sensors.Add(baseStation);

            for (int i = 0; i < sensorEnergies.Length; i++)
            {
                SensorTemplate sensor = new SensorTemplate();
                sensor.Id = i + 1;
                sensor.X = (i + 1) * 10.0;
                sensor.Y = 0.0;
                sensor.InitialEnergyJ = sensorEnergies[i];
                sensor.ParentId = 0;
                artifact.Sensors.Add(sensor);
            }

            return artifact;
        }

        private static void RunStandaloneDispatchSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            string[] proactiveAlgorithms = new string[]
            {
                "NJF_ZHENG_BPR",
                "NJF_YU_BPR",
                "NJF_ROUTE_ZHENG_BPR_LIMITED",
                "NJF_ROUTE_YU_BPR_LIMITED"
            };

            for (int i = 0; i < proactiveAlgorithms.Length; i++)
            {
                ExperimentSettings settings = CreateBprSelfTestSettings(tempDirectory);
                settings.SimulationTimeSeconds = 20.0;
                settings.NmaxTask = 1;
                settings.YuDangerWindowSeconds = 10.0;
                settings.YuDangerThresholdK = 2;
                settings.YuIntervalUncertaintySeconds = 10.0;
                settings.AllowStandaloneProactiveDispatch = false;
                settings.Normalize();
                ExperimentSimulation simulation = new ExperimentSimulation(
                    settings,
                    CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0 }),
                    proactiveAlgorithms[i],
                    null);
                simulations.Add(simulation);
                simulation.Run();
                AssertSelfTest(simulation.summary.MissionCount == 0,
                    proactiveAlgorithms[i] + " must not dispatch a standalone proactive mission when no natural request exists.");
                AssertSelfTest(simulation.summary.ProactiveTaskCount == 0,
                    proactiveAlgorithms[i] + " must not create proactive tasks without a natural-request mission when standalone dispatch is disabled.");
                AssertSelfTest(simulation.summary.NaturalRequestCount == 0,
                    proactiveAlgorithms[i] + " self-test should not create natural requests before the short simulation horizon.");
            }
        }

        private static void RunActiveRequestProactiveSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_ZHENG_BPR", ZhengBprWindowRemovalReason, 2);
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_YU_BPR", YuBprDangerIntervalRemovalReason, 2);
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_ROUTE_ZHENG_BPR_LIMITED", ZhengBprWindowRemovalReason, 2);
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_ROUTE_YU_BPR_LIMITED", YuBprDangerIntervalRemovalReason, 2);

            ExperimentSettings extendedSettings = CreateBprSelfTestSettings(tempDirectory);
            extendedSettings.NmaxTask = 1;
            extendedSettings.YuDangerWindowSeconds = 10.0;
            extendedSettings.YuDangerThresholdK = 2;
            extendedSettings.YuIntervalUncertaintySeconds = 10.0;
            extendedSettings.Normalize();
            ExperimentArtifact artifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0 });

            ExperimentSimulation zhengExtended = new ExperimentSimulation(extendedSettings, artifact, "NJF_ROUTE_ZHENG_BPR_EXTENDED", null);
            simulations.Add(zhengExtended);
            List<ChargingRequest> zhengCplist = zhengExtended.BuildZhengBprCplist(
                new List<ChargingRequest>(),
                1,
                BprProactiveSelectionMode.RouteInsertionCost,
                true);
            AssertSelfTest(zhengCplist.Count > 1,
                "NJF_ROUTE_ZHENG_BPR_EXTENDED should allow cplist.Count beyond NmaxTask.");

            ExperimentSimulation yuExtended = new ExperimentSimulation(extendedSettings, artifact, "NJF_ROUTE_YU_BPR_EXTENDED", null);
            simulations.Add(yuExtended);
            List<ChargingRequest> yuCplist = yuExtended.BuildYuBprCplist(
                new List<ChargingRequest>(),
                1,
                true,
                YuProactiveSelectionMode.RouteInsertionCost);
            AssertSelfTest(yuCplist.Count > 1,
                "NJF_ROUTE_YU_BPR_EXTENDED should allow cplist.Count beyond NmaxTask.");
        }

        private static void AssertActiveRequestProactiveRoute(
            string tempDirectory,
            List<ExperimentSimulation> simulations,
            string algorithm,
            string expectedReason,
            int maxTask)
        {
            ExperimentSettings settings = CreateBprSelfTestSettings(tempDirectory);
            settings.NmaxTask = maxTask;
            settings.WcvSpeedMetersPerSecond = 100.0;
            settings.WcvChargeRateJPerSecond = 100.0;
            settings.YuDangerWindowSeconds = 10.0;
            settings.YuDangerThresholdK = 3;
            settings.YuIntervalUncertaintySeconds = 10.0;
            settings.Normalize();
            ExperimentSimulation simulation = new ExperimentSimulation(
                settings,
                CreateBprSelfTestArtifact(new double[] { 40.0, 60.0, 60.0, 60.0 }),
                algorithm,
                null);
            simulations.Add(simulation);
            simulation.CreateRequestsAtCurrentTime();
            AssertSelfTest(simulation.activeRequests.Count > 0,
                algorithm + " self-test should begin with a natural request.");

            List<ChargingRequest> route = simulation.BuildMissionRoute();
            AssertSelfTest(ContainsProactiveReason(route, expectedReason),
                algorithm + " should insert a proactive task with reason " + expectedReason + " when a natural-request mission is open.");
            AssertSelfTest(route.Count <= maxTask,
                algorithm + " limited/default route must not exceed NmaxTask.");
        }

        private static void RunProactiveCandidateFilterSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings nearFullSettings = CreateBprSelfTestSettings(tempDirectory);
            nearFullSettings.NmaxTask = 1;
            nearFullSettings.YuDangerWindowSeconds = 10.0;
            nearFullSettings.YuDangerThresholdK = 2;
            nearFullSettings.YuIntervalUncertaintySeconds = 10.0;
            nearFullSettings.ProactiveCandidateMaxEnergyRatio = 0.95;
            nearFullSettings.Normalize();
            ExperimentArtifact nearFullArtifact = CreateBprSelfTestArtifact(new double[] { 96.0, 96.0, 96.0 });

            ExperimentSimulation zhengNearFull = new ExperimentSimulation(nearFullSettings, nearFullArtifact, "NJF_ZHENG_BPR", null);
            simulations.Add(zhengNearFull);
            AssertSelfTest(zhengNearFull.BuildZhengBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    BprProactiveSelectionMode.Deterministic,
                    false).Count == 0,
                "Near-full nodes must be excluded from ZHENG BP&R proactive candidates.");

            ExperimentSimulation yuNearFull = new ExperimentSimulation(nearFullSettings, nearFullArtifact, "NJF_YU_BPR", null);
            simulations.Add(yuNearFull);
            AssertSelfTest(yuNearFull.BuildYuBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    false,
                    YuProactiveSelectionMode.Deterministic).Count == 0,
                "Near-full nodes must be excluded from YU BP&R proactive candidates.");

            ExperimentSettings cooldownSettings = CreateBprSelfTestSettings(tempDirectory);
            cooldownSettings.NmaxTask = 1;
            cooldownSettings.YuDangerWindowSeconds = 10.0;
            cooldownSettings.YuDangerThresholdK = 2;
            cooldownSettings.YuIntervalUncertaintySeconds = 10.0;
            cooldownSettings.ProactiveCooldownSeconds = 10.0;
            cooldownSettings.Normalize();
            ExperimentArtifact cooldownArtifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0 });

            ExperimentSimulation cooldownSimulation = new ExperimentSimulation(cooldownSettings, cooldownArtifact, "NJF_ZHENG_BPR", null);
            simulations.Add(cooldownSimulation);
            for (int nodeId = 1; nodeId < cooldownSimulation.sensors.Length; nodeId++)
            {
                BprSTableEntry entry = cooldownSimulation.GetOrCreateBprSTableEntry(nodeId);
                entry.LastChargedTimeSeconds = cooldownSimulation.currentTime;
                entry.LastProactiveSelectedTimeSeconds = cooldownSimulation.currentTime;
            }
            AssertSelfTest(cooldownSimulation.BuildZhengBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    BprProactiveSelectionMode.Deterministic,
                    false).Count == 0,
                "Nodes inside proactive cooldown must not be selected by ZHENG BP&R.");
            AssertSelfTest(cooldownSimulation.BuildYuBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    false,
                    YuProactiveSelectionMode.Deterministic).Count == 0,
                "Nodes inside proactive cooldown must not be selected by YU BP&R.");
        }

        private static void RunSimulationEndBeforeFullSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings settings = CreateBprSelfTestSettings(tempDirectory);
            settings.SimulationTimeSeconds = 1.0;
            settings.WcvSpeedMetersPerSecond = 1000.0;
            settings.WcvChargeRateJPerSecond = 1.0;
            settings.SensorBackgroundLifetimeSeconds = 1000.0;
            settings.NmaxTask = 1;
            settings.Normalize();
            ExperimentSimulation simulation = new ExperimentSimulation(
                settings,
                CreateBprSelfTestArtifact(new double[] { 40.0 }),
                "NJF",
                null);
            simulations.Add(simulation);
            simulation.Run();
            AssertSelfTest(simulation.totalTaskRecordCount == 1,
                "Simulation-end-before-full self-test should record one attempted task.");
            AssertSelfTest(simulation.summary.SuccessfulCharges == 0,
                "A task that reaches simulation end before becoming full must not be counted as successful.");
        }

        private static void RunDeathReasonSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings settings = CreateBprSelfTestSettings(tempDirectory);

            ExperimentSimulation pendingPacketDeath = new ExperimentSimulation(
                settings,
                CreateBprSelfTestArtifact(new double[] { 10.0 }),
                "NJF",
                null);
            simulations.Add(pendingPacketDeath);
            pendingPacketDeath.sensors[1].HasPendingRequest = true;
            pendingPacketDeath.sensors[1].EnergyJ = -0.25;
            pendingPacketDeath.MarkDead(1, 12.0, "packet_forward");
            ExperimentDeathRecord pendingPacketRecord = pendingPacketDeath.deaths[0];
            AssertSelfTest(pendingPacketRecord.Reason == "packet_forward",
                "Scheduling-related packet death should preserve packet_forward as Reason.");
            AssertSelfTest(pendingPacketRecord.DirectEnergyCause == "packet_forward",
                "DirectEnergyCause should preserve the direct packet death reason.");
            AssertSelfTest(pendingPacketRecord.SchedulingRelated,
                "Pending-request death should be marked scheduling related.");
            AssertSelfTest(pendingPacketRecord.SchedulingCause == "scheduling_wait",
                "SchedulingCause should be scheduling_wait when a pending request exists.");
            AssertSelfTest(pendingPacketRecord.HasPendingRequestAtDeath,
                "HasPendingRequestAtDeath should capture the pending request snapshot.");
            AssertNear(pendingPacketRecord.EnergyBeforeDeathJ, -0.25, 1e-12,
                "EnergyBeforeDeathJ should be captured before MarkDead resets energy to zero.");

            ExperimentSimulation continuousDeath = new ExperimentSimulation(
                settings,
                CreateBprSelfTestArtifact(new double[] { -0.1 }),
                "NJF",
                null);
            simulations.Add(continuousDeath);
            continuousDeath.MarkDead(1, 3.0, "continuous");
            ExperimentDeathRecord continuousRecord = continuousDeath.deaths[0];
            AssertSelfTest(continuousRecord.Reason == "continuous",
                "Non-scheduling continuous death should preserve continuous as Reason.");
            AssertSelfTest(!continuousRecord.SchedulingRelated,
                "Continuous death without pending or scheduled mission should not be scheduling related.");
            AssertSelfTest(String.IsNullOrEmpty(continuousRecord.SchedulingCause),
                "SchedulingCause should be empty when SchedulingRelated is false.");

            ExperimentSimulation scheduledDeath = new ExperimentSimulation(
                settings,
                CreateBprSelfTestArtifact(new double[] { -0.1 }),
                "NJF",
                null);
            simulations.Add(scheduledDeath);
            scheduledDeath.plannedMissionNodeIds = new HashSet<int>();
            scheduledDeath.plannedMissionNodeIds.Add(1);
            scheduledDeath.MarkDead(1, 5.0, "continuous");
            ExperimentDeathRecord scheduledRecord = scheduledDeath.deaths[0];
            AssertSelfTest(scheduledRecord.WasScheduledInCurrentMissionAtDeath,
                "WasScheduledInCurrentMissionAtDeath should capture mission reservation at death.");
            AssertSelfTest(scheduledRecord.SchedulingRelated,
                "Scheduled mission death should be marked scheduling related.");
            AssertSelfTest(scheduledRecord.Reason == "continuous",
                "Scheduled mission death should still preserve the direct continuous Reason.");
            AssertSelfTest(scheduledDeath.summary.FirstDeadReason == "continuous",
                "FirstDeadReason should preserve direct death cause, not scheduling_wait.");
            AssertSelfTest(scheduledDeath.summary.FirstDeadSchedulingCause == "scheduling_wait",
                "FirstDeadSchedulingCause should record scheduling_wait separately.");
        }

        private static void RunRoutingLoadDeadlinePrioritySelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings settings = CreateBprSelfTestSettings(tempDirectory);
            settings.EventRatePerSecond = 3.0;
            settings.PacketBits = 81920.0;
            settings.RadioRangeMeters = 30.0;
            settings.SensorBackgroundLifetimeSeconds = 100.0;
            settings.ThresholdMode = "Percent";
            settings.RequestThresholdPercent = 50.0;
            settings.Normalize();

            ExperimentSimulation simulation = new ExperimentSimulation(
                settings,
                CreateChainRoutingSelfTestArtifact(new double[] { 100.0, 100.0, 100.0 }),
                "NJF",
                null);
            simulations.Add(simulation);

            AssertSelfTest(simulation.routingSubtreeSizeByNodeId[1] > simulation.routingSubtreeSizeByNodeId[2] &&
                simulation.routingSubtreeSizeByNodeId[2] > simulation.routingSubtreeSizeByNodeId[3],
                "Routing subtree size should be largest near the base station.");
            AssertSelfTest(simulation.GetRoutingLoadJPerSecond(simulation.sensors[1]) >
                simulation.GetRoutingLoadJPerSecond(simulation.sensors[2]) &&
                simulation.GetRoutingLoadJPerSecond(simulation.sensors[2]) >
                simulation.GetRoutingLoadJPerSecond(simulation.sensors[3]),
                "Routing load should be largest for forwarding bottleneck nodes.");
            AssertSelfTest(simulation.GetEffectiveConsumeRateJPerSecond(simulation.sensors[1]) >
                simulation.GetEffectiveConsumeRateJPerSecond(simulation.sensors[2]) &&
                simulation.GetEffectiveConsumeRateJPerSecond(simulation.sensors[2]) >
                simulation.GetEffectiveConsumeRateJPerSecond(simulation.sensors[3]),
                "Effective consume rate should include routing load.");

            double threshold = simulation.GetRequestThresholdJ(simulation.sensors[1]);
            double baseDeadline = simulation.currentTime +
                (simulation.sensors[1].EnergyJ - threshold) / simulation.sensors[1].ConsumeRateJPerSecond;
            double effectiveDeadline = simulation.ComputeBprRequestDeadlineSeconds(simulation.sensors[1]);
            AssertSelfTest(effectiveDeadline < baseDeadline,
                "Routing-aware BP&R deadline should be earlier than base-consume-rate deadline.");
            AssertNear(simulation.FindNextRequestTime(null), baseDeadline, 1e-9,
                "Natural request time advancement should use base consume rate, not routing-aware effective rate.");
            AssertNear(simulation.FindNextDeathTime(null),
                simulation.currentTime + simulation.sensors[1].EnergyJ / simulation.sensors[1].ConsumeRateJPerSecond,
                1e-9,
                "Natural death time advancement should use base consume rate, not routing-aware effective rate.");

            ExperimentSettings treqSettings = CreateBprSelfTestSettings(tempDirectory);
            treqSettings.EventRatePerSecond = 3.0;
            treqSettings.PacketBits = 81920.0;
            treqSettings.RadioRangeMeters = 30.0;
            treqSettings.SensorBackgroundLifetimeSeconds = 100.0;
            treqSettings.ThresholdMode = "TreqSeconds";
            treqSettings.TreqSeconds = 10.0;
            treqSettings.Normalize();
            ExperimentSimulation treqSimulation = new ExperimentSimulation(
                treqSettings,
                CreateChainRoutingSelfTestArtifact(new double[] { 100.0, 100.0, 100.0 }),
                "NJF",
                null);
            simulations.Add(treqSimulation);
            AssertNear(treqSimulation.GetRequestThresholdJ(treqSimulation.sensors[1]),
                treqSimulation.GetRequestThresholdJ(treqSimulation.sensors[3]),
                1e-9,
                "Natural TreqSeconds threshold should not include predicted routing load.");
            double predictedNode1Rate = treqSimulation.ComputePredictedEffectiveConsumeRateJPerSecond(1, treqSimulation.sensors[1].RateScale);
            double predictedNode3Rate = treqSimulation.ComputePredictedEffectiveConsumeRateJPerSecond(3, treqSimulation.sensors[3].RateScale);
            AssertSelfTest(treqSimulation.GetPredictedRequestThresholdJ(
                    treqSimulation.sensors[1],
                    predictedNode1Rate,
                    treqSimulation.sensors[1].RateScale) >
                treqSimulation.GetPredictedRequestThresholdJ(
                    treqSimulation.sensors[3],
                    predictedNode3Rate,
                    treqSimulation.sensors[3].RateScale),
                "BPR/YU prediction threshold should still include routing-aware effective load.");

            double beforeEnergy = simulation.sensors[1].EnergyJ;
            double baseRate = simulation.sensors[1].ConsumeRateJPerSecond;
            double effectiveRate = simulation.GetEffectiveConsumeRateJPerSecond(simulation.sensors[1]);
            simulation.ApplyContinuousEnergy(1.0, null);
            AssertNear(simulation.sensors[1].EnergyJ, beforeEnergy - baseRate, 1e-9,
                "ApplyContinuousEnergy should subtract only base continuous consume rate.");
            AssertSelfTest(effectiveRate > baseRate,
                "Routing load should not be double-counted into continuous energy consumption.");
        }

        private static void RunCompleteBprYuPredictionSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings windowSettings = CreateBprSelfTestSettings(tempDirectory);
            windowSettings.SimulationTimeSeconds = 240.0;
            windowSettings.NmaxTask = 2;
            windowSettings.YuDangerWindowSeconds = 80.0;
            windowSettings.YuDangerThresholdK = 3;
            windowSettings.Normalize();
            ExperimentSimulation windowSimulation = new ExperimentSimulation(
                windowSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0 }),
                "NJF_ZHENG_BPR",
                null);
            simulations.Add(windowSimulation);

            List<BprPredictedRequest> manualRequests = new List<BprPredictedRequest>();
            manualRequests.Add(CreateManualBprPredictedRequest(1, 100.0, 150.0, 1.0));
            manualRequests.Add(CreateManualBprPredictedRequest(2, 110.0, 160.0, 1.0));
            manualRequests.Add(CreateManualBprPredictedRequest(3, 120.0, 170.0, 1.0));
            List<BprWindow> bprWindows = windowSimulation.BuildBprSlidingWindows(manualRequests, 2);
            AssertSelfTest(bprWindows.Count > 0 && bprWindows[0].OverflowCount == 1,
                "ZHENG BP&R sliding window should detect one overflow from three predicted requests and NmaxTask=2.");
            List<BprRemovalDecision> bprDecisions = windowSimulation.SelectZhengBprRemovalNodes(
                bprWindows[0],
                1,
                new List<ChargingRequest>(),
                BprProactiveSelectionMode.Deterministic);
            AssertSelfTest(bprDecisions.Count == 1,
                "ZHENG BP&R window removal should select one deterministic removal candidate.");
            ChargingRequest zhengProactive = windowSimulation.CreateZhengBprProactiveRequest(bprDecisions[0]);
            AssertSelfTest(zhengProactive.ProactiveReason == ZhengBprWindowRemovalReason,
                "ZHENG BP&R proactive reason should be ZHENG_BPR_WINDOW_REMOVAL.");
            AssertNear(zhengProactive.DeadlineSeconds, bprDecisions[0].DeathTimeSeconds, 1e-9,
                "ZHENG BP&R proactive deadline should use the predicted death time.");

            List<YuPredictedInterval> manualIntervals = new List<YuPredictedInterval>();
            manualIntervals.Add(CreateManualYuPredictedInterval(1, 100.0, 100.0, 160.0, 150.0, 1.0, 30.0));
            manualIntervals.Add(CreateManualYuPredictedInterval(2, 110.0, 110.0, 170.0, 160.0, 1.0, 30.0));
            manualIntervals.Add(CreateManualYuPredictedInterval(3, 120.0, 120.0, 180.0, 170.0, 1.0, 30.0));
            List<YuDangerWindow> yuWindows = windowSimulation.BuildYuDangerWindows(manualIntervals, 2);
            AssertSelfTest(yuWindows.Count > 0 && yuWindows[0].RemovalNeededCount == 1,
                "YU BP&R danger window should detect one required removal from three overlapping intervals and K*=3.");
            List<YuRemovalDecision> yuDecisions = windowSimulation.SelectYuRemovalNodes(
                yuWindows[0],
                1,
                new List<ChargingRequest>(),
                YuProactiveSelectionMode.Deterministic);
            AssertSelfTest(yuDecisions.Count == 1,
                "YU BP&R danger interval removal should select one deterministic removal candidate.");
            ChargingRequest yuProactive = windowSimulation.CreateYuProactiveRequest(yuDecisions[0]);
            AssertSelfTest(yuProactive.ProactiveReason == YuBprDangerIntervalRemovalReason,
                "YU BP&R proactive reason should be YU_BPR_DANGER_INTERVAL_REMOVAL.");
            AssertNear(yuProactive.DeadlineSeconds, yuDecisions[0].LatestSafeServiceTimeSeconds, 1e-9,
                "YU BP&R proactive deadline should use the latest safe service time.");

            ExperimentSettings predictionSettings = CreateBprSelfTestSettings(tempDirectory);
            predictionSettings.SimulationTimeSeconds = 120.0;
            predictionSettings.RequestThresholdPercent = 20.0;
            predictionSettings.ProactiveCandidateMaxEnergyRatio = 1.0;
            predictionSettings.YuIntervalUncertaintySeconds = 10.0;
            predictionSettings.Normalize();
            ExperimentArtifact predictionArtifact = CreateBprSelfTestArtifact(new double[] { 99.0 });
            RateChangeTemplate rateChange = new RateChangeTemplate();
            rateChange.TimeSeconds = 50.0;
            rateChange.NodeId = 1;
            rateChange.Multiplier = 2.0;
            predictionArtifact.RateChanges.Add(rateChange);

            ExperimentSimulation predictionSimulation = new ExperimentSimulation(
                predictionSettings,
                predictionArtifact,
                "NJF_ZHENG_BPR",
                null);
            simulations.Add(predictionSimulation);
            List<BprPredictedRequest> predictedRequests = predictionSimulation.BuildBprPredictedRequests(1, new HashSet<int>());
            AssertSelfTest(predictedRequests.Count == 1,
                "ZHENG prediction timeline should produce one predicted request for the rate-change test node.");
            AssertNear(predictedRequests[0].RequestTimeSeconds, 64.5, 1e-9,
                "ZHENG prediction timeline should apply future rate changes when computing request time.");

            List<YuPredictedInterval> predictedIntervals = predictionSimulation.BuildYuPredictedIntervals(1, new HashSet<int>());
            AssertSelfTest(predictedIntervals.Count == 1,
                "YU prediction timeline should produce one predicted request interval for the rate-change test node.");
            AssertNear(predictedIntervals[0].CenterRequestTimeSeconds, 64.5, 1e-9,
                "YU prediction interval center should follow the rate-change-adjusted request time.");
            AssertNear(predictedIntervals[0].IntervalStartSeconds, 54.5, 1e-9,
                "YU prediction interval start should apply configured uncertainty around the center.");
            AssertNear(predictedIntervals[0].IntervalEndSeconds, 74.5, 1e-9,
                "YU prediction interval end should apply configured uncertainty around the center.");

            ExperimentSimulation zhengSideEffectSimulation = new ExperimentSimulation(
                predictionSettings,
                predictionArtifact,
                "NJF_ZHENG_BPR",
                null);
            simulations.Add(zhengSideEffectSimulation);
            AssertPredictionHelpersHaveNoSideEffects(zhengSideEffectSimulation, 1);

            ExperimentSimulation yuSideEffectSimulation = new ExperimentSimulation(
                predictionSettings,
                predictionArtifact,
                "NJF_ROUTE_YU_BPR_LIMITED",
                null);
            simulations.Add(yuSideEffectSimulation);
            AssertPredictionHelpersHaveNoSideEffects(yuSideEffectSimulation, 1);
        }

        private static void AssertPredictionHelpersHaveNoSideEffects(ExperimentSimulation simulation, int maxTask)
        {
            double energyBefore = simulation.sensors[1].EnergyJ;
            double rateScaleBefore = simulation.sensors[1].RateScale;
            double consumeRateBefore = simulation.sensors[1].ConsumeRateJPerSecond;
            double currentTimeBefore = simulation.currentTime;
            int activeRequestCountBefore = simulation.activeRequests.Count;
            int missionIdBefore = simulation.missionId;

            List<BprPredictedRequest> bprPredicted = simulation.BuildBprPredictedRequests(maxTask, new HashSet<int>());
            simulation.BuildBprSlidingWindows(bprPredicted, maxTask);
            simulation.BuildBprPredictionTimeline(1, simulation.GetBprPredictionHorizonEndSeconds(maxTask), new HashSet<int>());
            List<YuPredictedInterval> yuPredicted = simulation.BuildYuPredictedIntervals(maxTask, new HashSet<int>());
            simulation.BuildYuDangerWindows(yuPredicted, maxTask);
            simulation.BuildYuPredictionTimeline(1, simulation.GetYuPredictionHorizonEndSeconds(maxTask), new HashSet<int>());
            simulation.HasBprBottleneckCandidate();
            simulation.FindNextBprBottleneckCandidateTime();

            AssertNear(simulation.sensors[1].EnergyJ, energyBefore, 1e-12,
                "Prediction timeline helpers must not mutate sensor energy.");
            AssertNear(simulation.sensors[1].RateScale, rateScaleBefore, 1e-12,
                "Prediction timeline helpers must not mutate sensor rate scale.");
            AssertNear(simulation.sensors[1].ConsumeRateJPerSecond, consumeRateBefore, 1e-12,
                "Prediction timeline helpers must not mutate sensor consume rate.");
            AssertNear(simulation.currentTime, currentTimeBefore, 1e-12,
                "Prediction timeline helpers must not mutate current time.");
            AssertSelfTest(simulation.activeRequests.Count == activeRequestCountBefore,
                "Prediction timeline helpers must not mutate active requests.");
            AssertSelfTest(simulation.missionId == missionIdBefore,
                "Prediction timeline helpers must not mutate mission id.");
        }

        private static BprPredictedRequest CreateManualBprPredictedRequest(
            int nodeId,
            double requestTime,
            double deathTime,
            double effectiveRate)
        {
            BprPredictedRequest request = new BprPredictedRequest();
            request.NodeId = nodeId;
            request.RequestTimeSeconds = requestTime;
            request.DeathTimeSeconds = deathTime;
            request.EnergyAtPredictionStartJ = 100.0;
            request.EffectiveConsumeRateJPerSecond = effectiveRate;
            request.SlackSeconds = deathTime - requestTime;
            request.RouteInsertionCost = 0.0;
            return request;
        }

        private static YuPredictedInterval CreateManualYuPredictedInterval(
            int nodeId,
            double intervalStart,
            double center,
            double intervalEnd,
            double latestSafeServiceTime,
            double effectiveRate,
            double uncertainty)
        {
            YuPredictedInterval interval = new YuPredictedInterval();
            interval.NodeId = nodeId;
            interval.IntervalStartSeconds = intervalStart;
            interval.CenterRequestTimeSeconds = center;
            interval.IntervalEndSeconds = intervalEnd;
            interval.EarliestDeathTimeSeconds = latestSafeServiceTime;
            interval.LatestSafeServiceTimeSeconds = latestSafeServiceTime;
            interval.EnergyAtPredictionStartJ = 100.0;
            interval.EffectiveConsumeRateJPerSecond = effectiveRate;
            interval.UncertaintySeconds = uncertainty;
            interval.SlackSeconds = latestSafeServiceTime - center;
            interval.RouteInsertionCost = 0.0;
            interval.IsAlive = true;
            return interval;
        }

        private static ExperimentArtifact CreateChainRoutingSelfTestArtifact(double[] sensorEnergies)
        {
            ExperimentArtifact artifact = CreateBprSelfTestArtifact(sensorEnergies);
            for (int id = 1; id < artifact.Sensors.Count; id++)
                artifact.Sensors[id].ParentId = id - 1;
            return artifact;
        }

        private static bool ContainsBprEntry(List<BprSTableEntry> entries, int nodeId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].NodeId == nodeId)
                    return true;
            }
            return false;
        }

        private static bool ContainsChargingRequest(List<ChargingRequest> requests, int nodeId)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].NodeId == nodeId)
                    return true;
            }
            return false;
        }

        private static bool ContainsProactiveReason(List<ChargingRequest> requests, string reason)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                ChargingRequest request = requests[i];
                if (request.IsProactive && String.Equals(request.ProactiveReason, reason, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static void AssertSelfTest(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        private static void AssertNear(double actual, double expected, double tolerance, string message)
        {
            if (Math.Abs(actual - expected) > tolerance)
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture,
                    "{0} Expected {1}, got {2}.",
                    message,
                    expected,
                    actual));
        }

        private static int StableStringHash(string value)
        {
            unchecked
            {
                int hash = 23;
                for (int i = 0; i < value.Length; i++)
                    hash = hash * 31 + value[i];
                return hash;
            }
        }
    }

    internal class SensorState
    {
        public int Id;
        public double X;
        public double Y;
        public double EnergyJ;
        public double CapacityJ;
        public double RateScale;
        public double ConsumeRateJPerSecond;
        public int ParentId;
        public bool Alive;
        public bool HasPendingRequest;

        public SensorState(SensorTemplate template, ExperimentSettings settings)
        {
            Id = template.Id;
            X = template.X;
            Y = template.Y;
            EnergyJ = template.InitialEnergyJ;
            CapacityJ = settings.InitialEnergyJ;
            RateScale = 1.0;
            ParentId = template.ParentId;
            Alive = true;
            HasPendingRequest = false;
            RefreshConsumeRate(settings);
        }

        public void RefreshConsumeRate(ExperimentSettings settings)
        {
            ConsumeRateJPerSecond = settings.InitialEnergyJ * Math.Max(0.01, RateScale) /
                Math.Max(1.0, settings.SensorBackgroundLifetimeSeconds);
        }
    }

    internal class ChargingContext
    {
        public int NodeId;
        public double ChargeRateJPerSecond;
        public double WcvEnergyJ;
        public double DeliveredEnergyJ;
    }

    internal class ChargingRequest
    {
        public int RequestId;
        public int NodeId;
        public double RequestTimeSeconds;
        public double DeadlineSeconds;
        public double RequestEnergyJ;
        public double ConsumeRateJPerSecond;
        public double BaseConsumeRateJPerSecond;
        public double EffectiveConsumeRateJPerSecond;
        public double RoutingLoadJPerSecond;
        public double RoutingTxLoadJPerSecond;
        public double RoutingRxLoadJPerSecond;
        public int RoutingSubtreeSize;
        public double ExpectedRoutingForwardPacketsPerSecond;
        public double CriticalDensity;
        public bool IsProactive;
        public string ProactiveReason;

        public ChargingRequest Clone()
        {
            ChargingRequest clone = new ChargingRequest();
            clone.RequestId = RequestId;
            clone.NodeId = NodeId;
            clone.RequestTimeSeconds = RequestTimeSeconds;
            clone.DeadlineSeconds = DeadlineSeconds;
            clone.RequestEnergyJ = RequestEnergyJ;
            clone.ConsumeRateJPerSecond = ConsumeRateJPerSecond;
            clone.BaseConsumeRateJPerSecond = BaseConsumeRateJPerSecond;
            clone.EffectiveConsumeRateJPerSecond = EffectiveConsumeRateJPerSecond;
            clone.RoutingLoadJPerSecond = RoutingLoadJPerSecond;
            clone.RoutingTxLoadJPerSecond = RoutingTxLoadJPerSecond;
            clone.RoutingRxLoadJPerSecond = RoutingRxLoadJPerSecond;
            clone.RoutingSubtreeSize = RoutingSubtreeSize;
            clone.ExpectedRoutingForwardPacketsPerSecond = ExpectedRoutingForwardPacketsPerSecond;
            clone.CriticalDensity = CriticalDensity;
            clone.IsProactive = IsProactive;
            clone.ProactiveReason = ProactiveReason;
            return clone;
        }
    }

    public class ExperimentRunResult
    {
        public ExperimentRunSummary Summary { get; set; }
        public int TotalTaskRecordCount { get; set; }
        public List<ExperimentDeathRecord> Deaths { get; set; }
    }

    public class ExperimentRunSummary
    {
        public int RunIndex;
        public int Seed;
        public string Algorithm;
        public string ArtifactHash;
        public double PrateChange;
        public double RateChangeVariationPercent;
        public int RateChangeScheduleCount;
        public int AppliedRateChangeCount;
        public double NetworkLifetimeSeconds;
        public int FirstDeadNodeId;
        public double FirstDeadTimeSeconds;
        public string FirstDeadReason;
        public string FirstDeadReasonZh;
        public string FirstDeadDirectEnergyCause;
        public string FirstDeadDirectEnergyCauseZh;
        public bool FirstDeadSchedulingRelated;
        public string FirstDeadSchedulingCause;
        public string FirstDeadSchedulingCauseZh;
        public int SuccessfulCharges;
        public int FailedOrLateTasks;
        public int NaturalRequestCount;
        public int ProactiveTaskCount;
        public int UniqueServedNodeCount;
        public int RepeatChargeCount;
        public int ProactiveNearFullCount;
        public int MeaningfulProactiveCount;
        public int RequestCount;
        public int MissionCount;
        public double MovementDistanceMeters;
        public double MoveEnergyJ;
        public double DeliveredEnergyJ;
        public double AverageDeliveredEnergyPerTask;
        public double AverageDeliveredEnergyPerProactiveTask;
        public double ChargeEfficiency;
        public int PacketSent;
        public int PacketReceived;
        public int PacketLost;
        public int RoutingFailedPacketLost;
        public int RoutingParentMissingNodeCount;
        public double RoutingDisconnectedNodeRatio;
        public double TotalWaitSeconds;
        public double AverageWaitSeconds;
    }

    public class ExperimentTaskRecord
    {
        public int RunIndex;
        public int Seed;
        public string Algorithm;
        public string ArtifactHash;
        public int MissionId;
        public int TaskOrder;
        public int NodeId;
        public string TaskSource;
        public bool IsProactive;
        public string ProactiveReason;
        public double RequestTimeSeconds;
        public double DeadlineSeconds;
        public double DispatchTimeSeconds;
        public double ArrivalTimeSeconds;
        public double WaitSeconds;
        public double ChargeStartSeconds;
        public double ChargeEndSeconds;
        public double EnergyBeforeJ;
        public double EnergyAfterJ;
        public double InternalEnergyBeforeNj;
        public double InternalEnergyAfterNj;
        public double ConsumeRateJPerSecond;
        public double BaseConsumeRateJPerSecond;
        public double EffectiveConsumeRateJPerSecond;
        public double RoutingLoadJPerSecond;
        public double RoutingTxLoadJPerSecond;
        public double RoutingRxLoadJPerSecond;
        public int RoutingSubtreeSize;
        public double ExpectedRoutingForwardPacketsPerSecond;
        public double InternalRateNjPerTick;
        public double DeliveredEnergyJ;
        public double DistanceFromPreviousMeters;
        public bool Success;
        public string FailureReason;
        public double WcvEnergyAfterJ;
    }

    public class MissionRecord
    {
        public int RunIndex;
        public int Seed;
        public string Algorithm;
        public int MissionId;
        public double DispatchTimeSeconds;
        public double ReturnTimeSeconds;
        public int NodeCount;
        public int RequestCount;
        public int ProactiveCount;
        public int SuccessfulCharges;
        public int FailedCount;
        public double DistanceMeters;
        public double MoveEnergyJ;
        public double DeliveredEnergyJ;
        public int PacketSent;
        public int PacketReceived;
        public int PacketLost;
        public int RoutingFailedPacketLost;
        public double TotalWaitSeconds;
        public double AverageWaitSeconds;
        public int DeduplicatedTaskCount;
        public List<int> RouteNodeIds;
    }

    public class ExperimentDeathRecord
    {
        public int RunIndex;
        public int Seed;
        public string Algorithm;
        public string ArtifactHash;
        public double TimeSeconds;
        public int NodeId;
        public string Reason;
        public string ReasonZh;
        public string DirectEnergyCause;
        public string DirectEnergyCauseZh;
        public bool SchedulingRelated;
        public string SchedulingCause;
        public string SchedulingCauseZh;
        public bool PendingRequest;
        public bool HasPendingRequestAtDeath;
        public bool WasScheduledInCurrentMissionAtDeath;
        public int ParentIdAtDeath;
        public double EnergyBeforeDeathJ;
        public double BaseConsumeRateJPerSecondAtDeath;
        public double EffectiveConsumeRateJPerSecondAtDeath;
        public double RoutingLoadJPerSecondAtDeath;
        public double RoutingTxLoadJPerSecondAtDeath;
        public double RoutingRxLoadJPerSecondAtDeath;
        public int RoutingSubtreeSize;
        public double ExpectedRoutingForwardPacketsPerSecond;
        public double EnergyJ;
        public double RequestTimeSeconds;
        public double WaitSeconds;
    }

    internal sealed class MissionDetailCsvWriter : IDisposable
    {
        private readonly StreamWriter missionWriter;
        private readonly StreamWriter taskWriter;
        private readonly StreamWriter routingLoadWriter;
        private readonly StreamWriter bprDebugWriter;
        private readonly StreamWriter yuBprDebugWriter;
        private bool disposed;

        public MissionDetailCsvWriter(string directory, int runIndex, string algorithm)
        {
            Directory.CreateDirectory(directory);
            string safeAlgorithm = SanitizeFileName(algorithm);
            string missionPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-mission-details.csv", runIndex, safeAlgorithm));
            string taskPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-task-records.csv", runIndex, safeAlgorithm));
            string routingLoadPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-routing-load.csv", runIndex, safeAlgorithm));
            string bprDebugPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-bpr-debug.csv", runIndex, safeAlgorithm));
            string yuBprDebugPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-yu-bpr-debug.csv", runIndex, safeAlgorithm));

            Encoding utf8 = new UTF8Encoding(true);
            missionWriter = new StreamWriter(missionPath, false, utf8);
            taskWriter = new StreamWriter(taskPath, false, utf8);
            routingLoadWriter = new StreamWriter(routingLoadPath, false, utf8);
            bprDebugWriter = new StreamWriter(bprDebugPath, false, utf8);
            yuBprDebugWriter = new StreamWriter(yuBprDebugPath, false, utf8);
            WriteMissionHeader();
            WriteTaskHeader();
            WriteRoutingLoadHeader();
            WriteBprDebugHeader();
            WriteYuBprDebugHeader();
        }

        public static string PrepareTaskDetailsDirectory(string outputDirectory)
        {
            string root = String.IsNullOrWhiteSpace(outputDirectory)
                ? Path.Combine(ExperimentSettings.ResolveProjectRoot(), "outputs")
                : outputDirectory;
            string directory = Path.Combine(root, "task-details");
            Directory.CreateDirectory(directory);
            return directory;
        }

        public void WriteMission(MissionRecord record)
        {
            if (disposed || record == null)
                return;

            missionWriter.WriteLine(CsvRow(
                record.RunIndex,
                record.Seed,
                record.Algorithm,
                record.MissionId,
                record.DispatchTimeSeconds,
                record.ReturnTimeSeconds,
                record.NodeCount,
                record.RequestCount,
                record.ProactiveCount,
                record.SuccessfulCharges,
                record.FailedCount,
                record.DistanceMeters,
                record.MoveEnergyJ,
                record.DeliveredEnergyJ,
                record.PacketSent,
                record.PacketReceived,
                record.PacketLost,
                record.RoutingFailedPacketLost,
                record.AverageWaitSeconds,
                RouteText(record.RouteNodeIds),
                record.DeduplicatedTaskCount));
        }

        public void WriteTask(ExperimentTaskRecord record)
        {
            if (disposed || record == null)
                return;

            taskWriter.WriteLine(CsvRow(
                record.RunIndex,
                record.Seed,
                record.Algorithm,
                record.ArtifactHash,
                record.MissionId,
                record.TaskOrder,
                record.NodeId,
                record.TaskSource,
                record.IsProactive,
                record.ProactiveReason,
                record.RequestTimeSeconds,
                record.DeadlineSeconds,
                record.DispatchTimeSeconds,
                record.ArrivalTimeSeconds,
                record.WaitSeconds,
                record.ChargeStartSeconds,
                record.ChargeEndSeconds,
                record.EnergyBeforeJ,
                record.EnergyAfterJ,
                record.InternalEnergyBeforeNj,
                record.InternalEnergyAfterNj,
                record.ConsumeRateJPerSecond,
                record.BaseConsumeRateJPerSecond,
                record.EffectiveConsumeRateJPerSecond,
                record.RoutingLoadJPerSecond,
                record.RoutingTxLoadJPerSecond,
                record.RoutingRxLoadJPerSecond,
                record.RoutingSubtreeSize,
                record.ExpectedRoutingForwardPacketsPerSecond,
                record.InternalRateNjPerTick,
                record.DeliveredEnergyJ,
                record.DistanceFromPreviousMeters,
                record.Success,
                record.FailureReason,
                record.WcvEnergyAfterJ));
        }

        public void WriteRoutingLoad(
            ExperimentArtifact artifact,
            int[] subtreeSizeByNodeId,
            double[] expectedTxPacketsPerSecondByNodeId,
            double[] expectedRxPacketsPerSecondByNodeId,
            double[] expectedForwardPacketsPerSecondByNodeId,
            double[] estimatedTxLoadJPerSecondByNodeId,
            double[] estimatedRxLoadJPerSecondByNodeId,
            double[] estimatedRoutingLoadJPerSecondByNodeId)
        {
            if (disposed || artifact == null)
                return;

            for (int id = 1; id < artifact.Sensors.Count; id++)
            {
                routingLoadWriter.WriteLine(CsvRow(
                    artifact.RunIndex,
                    artifact.Seed,
                    id,
                    artifact.Sensors[id].ParentId,
                    SafeArrayValue(subtreeSizeByNodeId, id),
                    SafeArrayValue(expectedTxPacketsPerSecondByNodeId, id),
                    SafeArrayValue(expectedRxPacketsPerSecondByNodeId, id),
                    SafeArrayValue(expectedForwardPacketsPerSecondByNodeId, id),
                    SafeArrayValue(estimatedTxLoadJPerSecondByNodeId, id),
                    SafeArrayValue(estimatedRxLoadJPerSecondByNodeId, id),
                    SafeArrayValue(estimatedRoutingLoadJPerSecondByNodeId, id)));
            }
            routingLoadWriter.Flush();
        }

        public void WriteBprDebug(
            int runIndex,
            int seed,
            string algorithm,
            int missionId,
            double currentTimeSeconds,
            int iteration,
            double windowStartSeconds,
            double windowEndSeconds,
            int bottleneckCount,
            int overflowCount,
            int selectedNodeId,
            double selectedRequestTimeSeconds,
            double selectedDeathTimeSeconds,
            double selectedSlackSeconds,
            double selectedEffectiveRateJPerSecond,
            double selectedRouteInsertionCost,
            string selectionReason,
            int cplistCountBefore,
            int cplistCountAfter,
            int maxTask,
            bool allowCapacityOverflow)
        {
            if (disposed)
                return;

            bprDebugWriter.WriteLine(CsvRow(
                runIndex,
                seed,
                algorithm,
                missionId,
                currentTimeSeconds,
                iteration,
                windowStartSeconds,
                windowEndSeconds,
                bottleneckCount,
                overflowCount,
                selectedNodeId,
                selectedRequestTimeSeconds,
                selectedDeathTimeSeconds,
                selectedSlackSeconds,
                selectedEffectiveRateJPerSecond,
                selectedRouteInsertionCost,
                selectionReason,
                cplistCountBefore,
                cplistCountAfter,
                maxTask,
                allowCapacityOverflow));
        }

        public void WriteYuBprDebug(
            int runIndex,
            int seed,
            string algorithm,
            int missionId,
            double currentTimeSeconds,
            int iteration,
            double windowStartSeconds,
            double windowEndSeconds,
            int kStar,
            int dangerCount,
            int removalNeededCount,
            int selectedNodeId,
            double selectedIntervalStartSeconds,
            double selectedCenterRequestTimeSeconds,
            double selectedIntervalEndSeconds,
            double selectedEarliestDeathTimeSeconds,
            double selectedLatestSafeServiceTimeSeconds,
            double selectedSlackSeconds,
            double selectedUncertaintySeconds,
            double selectedEffectiveRateJPerSecond,
            double selectedRouteInsertionCost,
            string selectionReason,
            int cplistCountBefore,
            int cplistCountAfter,
            int maxTask,
            bool allowCapacityOverflow)
        {
            if (disposed)
                return;

            yuBprDebugWriter.WriteLine(CsvRow(
                runIndex,
                seed,
                algorithm,
                missionId,
                currentTimeSeconds,
                iteration,
                windowStartSeconds,
                windowEndSeconds,
                kStar,
                dangerCount,
                removalNeededCount,
                selectedNodeId,
                selectedIntervalStartSeconds,
                selectedCenterRequestTimeSeconds,
                selectedIntervalEndSeconds,
                selectedEarliestDeathTimeSeconds,
                selectedLatestSafeServiceTimeSeconds,
                selectedSlackSeconds,
                selectedUncertaintySeconds,
                selectedEffectiveRateJPerSecond,
                selectedRouteInsertionCost,
                selectionReason,
                cplistCountBefore,
                cplistCountAfter,
                maxTask,
                allowCapacityOverflow));
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            missionWriter.Dispose();
            taskWriter.Dispose();
            routingLoadWriter.Dispose();
            bprDebugWriter.Dispose();
            yuBprDebugWriter.Dispose();
        }

        private void WriteMissionHeader()
        {
            missionWriter.WriteLine(CsvRow("Run", "Seed", "Algorithm", "MissionId", "DispatchTime", "ReturnTime",
                "節點數", "Request數", "Proactive數", "成功充電數", "失敗數", "走的距離(m)", "移動耗能(J)",
                "充入能量(J)", "封包送出", "封包收到", "封包遺失", "RoutingFailed封包遺失", "平均等待時間",
                "路線節點序列", "DeduplicatedTaskCount"));
        }

        private void WriteTaskHeader()
        {
            taskWriter.WriteLine(CsvRow("Run", "Seed", "Algorithm", "ArtifactHash", "MissionId", "TaskOrder",
                "NodeId", "TaskSource", "IsProactive", "ProactiveReason", "RequestTimeSeconds", "DeadlineSeconds",
                "DispatchTimeSeconds", "ArrivalTimeSeconds", "WaitSeconds", "ChargeStartSeconds", "ChargeEndSeconds",
                "EnergyBeforeJ", "EnergyAfterJ", "InternalEnergyBeforeNj", "InternalEnergyAfterNj",
                "ConsumeRateJPerSecond", "BaseConsumeRateJPerSecond", "EffectiveConsumeRateJPerSecond",
                "RoutingLoadJPerSecond", "RoutingTxLoadJPerSecond", "RoutingRxLoadJPerSecond",
                "RoutingSubtreeSize", "ExpectedRoutingForwardPacketsPerSecond",
                "InternalRateNjPerTick", "DeliveredEnergyJ", "DistanceFromPreviousMeters",
                "Success", "FailureReason", "WcvEnergyAfterJ"));
        }

        private void WriteRoutingLoadHeader()
        {
            routingLoadWriter.WriteLine(CsvRow("runIndex", "seed", "nodeId", "parentId", "subtreeSize",
                "expectedTxPacketsPerSecond", "expectedRxPacketsPerSecond", "expectedForwardPacketsPerSecond",
                "estimatedTxLoadJPerSecond", "estimatedRxLoadJPerSecond", "estimatedRoutingLoadJPerSecond"));
        }

        private void WriteBprDebugHeader()
        {
            bprDebugWriter.WriteLine(CsvRow("RunIndex", "Seed", "Algorithm", "MissionId", "CurrentTimeSeconds",
                "Iteration", "WindowStartSeconds", "WindowEndSeconds", "BottleneckCount", "OverflowCount",
                "SelectedNodeId", "SelectedRequestTimeSeconds", "SelectedDeathTimeSeconds", "SelectedSlackSeconds",
                "SelectedEffectiveRateJPerSecond", "SelectedRouteInsertionCost", "SelectionReason",
                "CplistCountBefore", "CplistCountAfter", "MaxTask", "AllowCapacityOverflow"));
        }

        private void WriteYuBprDebugHeader()
        {
            yuBprDebugWriter.WriteLine(CsvRow("RunIndex", "Seed", "Algorithm", "MissionId", "CurrentTimeSeconds",
                "Iteration", "WindowStartSeconds", "WindowEndSeconds", "KStar", "DangerCount", "RemovalNeededCount",
                "SelectedNodeId", "SelectedIntervalStartSeconds", "SelectedCenterRequestTimeSeconds",
                "SelectedIntervalEndSeconds", "SelectedEarliestDeathTimeSeconds", "SelectedLatestSafeServiceTimeSeconds",
                "SelectedSlackSeconds", "SelectedUncertaintySeconds", "SelectedEffectiveRateJPerSecond",
                "SelectedRouteInsertionCost", "SelectionReason", "CplistCountBefore", "CplistCountAfter",
                "MaxTask", "AllowCapacityOverflow"));
        }

        private static object SafeArrayValue(int[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
                return "";
            return values[index];
        }

        private static object SafeArrayValue(double[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
                return "";
            return values[index];
        }

        private static string RouteText(List<int> routeNodeIds)
        {
            if (routeNodeIds == null || routeNodeIds.Count == 0)
                return "";

            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < routeNodeIds.Count; i++)
            {
                if (i > 0)
                    builder.Append(">");
                builder.Append(routeNodeIds[i].ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private static string CsvRow(params object[] values)
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (i > 0)
                    builder.Append(',');
                builder.Append(CsvValue(values[i]));
            }
            return builder.ToString();
        }

        private static string CsvValue(object value)
        {
            if (value == null)
                return "";

            string text;
            IFormattable formattable = value as IFormattable;
            if (formattable != null)
                text = formattable.ToString(null, CultureInfo.InvariantCulture);
            else
                text = value.ToString();

            if (text.IndexOf('"') >= 0)
                text = text.Replace("\"", "\"\"");
            if (text.IndexOf(',') >= 0 || text.IndexOf('"') >= 0 || text.IndexOf('\r') >= 0 || text.IndexOf('\n') >= 0)
                return "\"" + text + "\"";
            return text;
        }

        private static string SanitizeFileName(string value)
        {
            string text = String.IsNullOrWhiteSpace(value) ? "algorithm" : value.Trim();
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                bool bad = false;
                for (int j = 0; j < invalid.Length; j++)
                {
                    if (c == invalid[j])
                    {
                        bad = true;
                        break;
                    }
                }
                builder.Append(bad ? '_' : c);
            }
            return builder.ToString();
        }
    }

    internal static class ExperimentWorkbookWriter
    {
        public static void Write(string path, ExperimentBatchResult result)
        {
            SimpleXlsxWriter writer = new SimpleXlsxWriter();
            writer.AddSheet("參數設定", BuildSettingsRows(result));
            writer.AddSheet("執行比較", BuildRunRows(result));
            writer.AddSheet("彙總統計", BuildSummaryRows(result));
            writer.AddSheet("死亡原因", BuildDeathRows(result));
            writer.Save(path);
        }

        private static List<List<object>> BuildSettingsRows(ExperimentBatchResult result)
        {
            ExperimentSettings s = result.Settings;
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("欄位", "值", "說明"));
            rows.Add(Row("實作根目錄", s.ProjectRoot, "所有修改與輸出都在 WSN 目錄"));
            rows.Add(Row("基礎亂數種子", s.BaseSeed, "第 i 個 run 使用 seed+i-1"));
            rows.Add(Row("Run 次數", s.RunCount, ""));
            rows.Add(Row("最大平行工作數", s.MaxParallelJobs == 0 ? "自動" : s.MaxParallelJobs.ToString(CultureInfo.InvariantCulture), "0=自動使用 CPU 邏輯核心數；可手動調高或降低"));
            rows.Add(Row("感測器數量", s.SensorCount, ""));
            rows.Add(Row("地圖邊長(m)", s.MapWidthMeters, "地圖固定為 n x n 正方形"));
            rows.Add(Row("模擬時間(s)", s.SimulationTimeSeconds, ""));
            rows.Add(Row("初始能量(J)", s.InitialEnergyJ, ""));
            rows.Add(Row("初始能量(nJ)", s.InitialEnergyJ * 1000000000.0, "內部換算"));
            rows.Add(Row("背景壽命(s)", s.SensorBackgroundLifetimeSeconds, "滿電連續耗能耗盡時間"));
            rows.Add(Row("基礎連續耗能(J/s)", s.InitialEnergyJ / s.SensorBackgroundLifetimeSeconds, ""));
            rows.Add(Row("基礎連續耗能(nJ/tick)", s.InitialEnergyJ / s.SensorBackgroundLifetimeSeconds * 1000000000.0 * 0.01, "tick=0.01s"));
            rows.Add(Row("事件率(封包/s)", s.EventRatePerSecond, ""));
            rows.Add(Row("封包大小(bits)", s.PacketBits, "參考 MyWSN 預設 10KB"));
            rows.Add(Row("通訊半徑(m)", s.RadioRangeMeters, ""));
            rows.Add(Row("RX 能耗(nJ/bit)", s.ReceiverEnergyNjPerBit, ""));
            rows.Add(Row("放大器能耗(nJ/bit/m^2)", s.AmplifierEnergyNjPerBitM2, ""));
            rows.Add(Row("距離耗能次方", s.PowerExponent, ""));
            rows.Add(Row("WCV 速度(m/s)", s.WcvSpeedMetersPerSecond, ""));
            rows.Add(Row("WCV 充電速率(J/s)", s.WcvChargeRateJPerSecond, ""));
            rows.Add(Row("WCV 容量(J)", s.WcvCapacityJ, ""));
            rows.Add(Row("WCV 移動耗能(J/m)", s.WcvMoveCostJPerMeter, ""));
            rows.Add(Row("每趟任務上限", s.NmaxTask, s.DynamicNmaxTask ? "動態上限" : "固定上限"));
            rows.Add(Row("門檻模式", s.ThresholdMode == "TreqSeconds" ? "Treq 秒數門檻" : "百分比門檻", ""));
            rows.Add(Row("Treq 秒數", s.TreqSeconds, ""));
            rows.Add(Row("BP&R deadline threshold(s)", s.BprDeadlineThresholdSeconds, "Persistent STable deadline maintenance threshold"));
            rows.Add(Row("AllowStandaloneProactiveDispatch", s.AllowStandaloneProactiveDispatch, "false = BP&R/YU proactive tasks are inserted only into natural-request missions"));
            rows.Add(Row("ProactivePredictionHorizonSeconds", s.ProactivePredictionHorizonSeconds, "0 = TreqSeconds + EstimateBprTjobSeconds(NmaxTask)"));
            rows.Add(Row("ProactiveCandidateMaxEnergyRatio", s.ProactiveCandidateMaxEnergyRatio, "nodes at or above this capacity ratio are excluded from proactive candidates"));
            rows.Add(Row("ProactiveCooldownSeconds", s.ProactiveCooldownSeconds, "0 = TreqSeconds after charged or proactive-selected"));
            rows.Add(Row("YU danger window(s)", s.YuDangerWindowSeconds, "0 = use EstimateBprTjobSeconds(NmaxTask)"));
            rows.Add(Row("YU danger threshold K", s.YuDangerThresholdK, "0 = use NmaxTask + 1"));
            rows.Add(Row("YU interval uncertainty(s)", s.YuIntervalUncertaintySeconds, "0 = use BprDeadlineThresholdSeconds"));
            rows.Add(Row("剩餘能量門檻(%)", s.RequestThresholdPercent, ""));
            rows.Add(Row("Prate_change（耗能率變動機率）", s.PrateChange, "單次測試固定一個值，不跑預設列表"));
            rows.Add(Row("耗能變動幅度(%)", s.RateChangeVariationPercent, "變動時倍率範圍 = 1 ± 此百分比"));
            rows.Add(Row("演算法", s.SelectedAlgorithmsCsv, ""));
            rows.Add(Row("輸出目錄", s.OutputDirectory, ""));
            rows.Add(Row("任務明細 CSV 資料夾", result.TaskDetailsDirectory, "每個 run + algorithm 各自輸出 mission-details 與 task-records CSV"));
            rows.Add(Row("ZHENG-inspired 動態耗能週期(s)", 10000, "每 10000s 檢查一次；延伸實驗，不是原始 ZHENG 重現"));
            rows.Add(Row("ZHENG-inspired 耗能率倍率", RateMultiplierRangeText(s), "由耗能變動幅度決定"));
            rows.Add(Row("基地台", "(0,0) sink + 充電中心", "固定單台 WCV，每趟 mission 後回 BS"));
            rows.Add(Row("FUZZY", "Mamdani 模糊推論", "剩餘能量、距離、耗能率、臨界節點密度"));
            rows.Add(Row("BP&R 標註", "ZHENG BP&R sliding-window BottleList", "使用 STable deadline、TdeadlineThreshold、Tjob(NmaxTask) sliding window、BottleList 與 cplist；RouteSafe 只改 BottleList 內選點策略"));
            rows.Add(Row("GENE/PSO/Cuckoo 標註", "full route optimization baselines", "GA、random-key PSO、Cuckoo Search 共用 route fitness"));
            rows.Add(Row("任務明細總列數", result.TotalTaskRecordCount, "逐節點 task records 已改寫入 CSV，不再輸出到 Excel"));

            rows.Add(Row("", "", ""));
            rows.Add(Row("Run", "Seed", "共用資料 hash", "rate-change 次數", "ParentId=-1 節點數", "不連通節點比例"));
            for (int i = 0; i < result.Artifacts.Count; i++)
            {
                ExperimentArtifact artifact = result.Artifacts[i];
                rows.Add(Row(artifact.RunIndex, artifact.Seed, artifact.ArtifactHash, artifact.RateChanges.Count,
                    artifact.CountMissingRoutingParents(), artifact.MissingRoutingParentRatio()));
            }

            return rows;
        }

        private static List<List<object>> BuildRunRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("Run", "Seed", "演算法", "共用資料hash", "Prate_change", "耗能變動幅度(%)", "rate schedule數", "實際套用rate變動數",
                "網路生命週期(s)", "第一個死亡節點", "第一個死亡時間(s)", "死亡原因", "直接耗能源",
                "成功充電數", "失敗/逾期數", "request數", "proactive數", "mission數", "移動距離(m)",
                "封包送出", "封包收到", "封包遺失", "routing failed 遺失", "ParentId=-1 節點數",
                "不連通節點比例", "平均等待(s)", "充電效率"));

            AppendRunAntiInflationHeaders(rows[0]);
            AppendRunDeathDiagnosisHeaders(rows[0]);

            for (int i = 0; i < result.RunSummaries.Count; i++)
            {
                ExperimentRunSummary s = result.RunSummaries[i];
                rows.Add(Row(s.RunIndex, s.Seed, AlgorithmDisplayName(s.Algorithm), s.ArtifactHash, s.PrateChange, s.RateChangeVariationPercent, s.RateChangeScheduleCount,
                    s.AppliedRateChangeCount, s.NetworkLifetimeSeconds, s.FirstDeadNodeId, s.FirstDeadTimeSeconds,
                    s.FirstDeadReasonZh, s.FirstDeadDirectEnergyCause, s.SuccessfulCharges, s.FailedOrLateTasks,
                    s.NaturalRequestCount, s.ProactiveTaskCount, s.MissionCount, s.MovementDistanceMeters,
                    s.PacketSent, s.PacketReceived, s.PacketLost, s.RoutingFailedPacketLost,
                    s.RoutingParentMissingNodeCount, s.RoutingDisconnectedNodeRatio,
                    s.AverageWaitSeconds, s.ChargeEfficiency));
                AddRunAntiInflationValues(rows[rows.Count - 1], s);
                AddRunDeathDiagnosisValues(rows[rows.Count - 1], s);
            }

            return rows;
        }

        private static void AppendRunAntiInflationHeaders(List<object> row)
        {
            row.Add("UniqueServedNodeCount");
            row.Add("RepeatChargeCount");
            row.Add("ProactiveNearFullCount");
            row.Add("MeaningfulProactiveCount");
            row.Add("AverageDeliveredEnergyPerTask");
            row.Add("AverageDeliveredEnergyPerProactiveTask");
        }

        private static void AddRunAntiInflationValues(List<object> row, ExperimentRunSummary s)
        {
            row.Add(s.UniqueServedNodeCount);
            row.Add(s.RepeatChargeCount);
            row.Add(s.ProactiveNearFullCount);
            row.Add(s.MeaningfulProactiveCount);
            row.Add(s.AverageDeliveredEnergyPerTask);
            row.Add(s.AverageDeliveredEnergyPerProactiveTask);
        }

        private static void AppendRunDeathDiagnosisHeaders(List<object> row)
        {
            row.Add("FirstDeadReason");
            row.Add("FirstDeadDirectEnergyCauseZh");
            row.Add("FirstDeadSchedulingRelated");
            row.Add("FirstDeadSchedulingCause");
            row.Add("FirstDeadSchedulingCauseZh");
        }

        private static void AddRunDeathDiagnosisValues(List<object> row, ExperimentRunSummary s)
        {
            row.Add(s.FirstDeadReason);
            row.Add(s.FirstDeadDirectEnergyCauseZh);
            row.Add(s.FirstDeadSchedulingRelated);
            row.Add(s.FirstDeadSchedulingCause);
            row.Add(s.FirstDeadSchedulingCauseZh);
        }

        private static List<List<object>> BuildSummaryRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("演算法", "Run 次數", "平均生命週期(s)", "最小生命週期(s)", "最大生命週期(s)",
                "平均成功充電", "平均失敗/逾期", "平均request", "平均移動距離(m)", "平均等待(s)",
                "平均封包遺失", "平均routing failed遺失", "平均ParentId=-1節點數", "平均不連通比例", "平均充電效率"));

            AppendSummaryAntiInflationHeaders(rows[0]);

            Dictionary<string, List<ExperimentRunSummary>> groups = new Dictionary<string, List<ExperimentRunSummary>>();
            for (int i = 0; i < result.RunSummaries.Count; i++)
            {
                string algorithm = result.RunSummaries[i].Algorithm;
                if (!groups.ContainsKey(algorithm))
                    groups[algorithm] = new List<ExperimentRunSummary>();
                groups[algorithm].Add(result.RunSummaries[i]);
            }

            foreach (KeyValuePair<string, List<ExperimentRunSummary>> pair in groups)
            {
                List<ExperimentRunSummary> list = pair.Value;
                rows.Add(Row(AlgorithmDisplayName(pair.Key), list.Count,
                    Average(list, delegate (ExperimentRunSummary s) { return s.NetworkLifetimeSeconds; }),
                    Min(list, delegate (ExperimentRunSummary s) { return s.NetworkLifetimeSeconds; }),
                    Max(list, delegate (ExperimentRunSummary s) { return s.NetworkLifetimeSeconds; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.SuccessfulCharges; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.FailedOrLateTasks; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.NaturalRequestCount; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.MovementDistanceMeters; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.AverageWaitSeconds; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.PacketLost; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.RoutingFailedPacketLost; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.RoutingParentMissingNodeCount; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.RoutingDisconnectedNodeRatio; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.ChargeEfficiency; })));
                AddSummaryAntiInflationValues(rows[rows.Count - 1], list);
            }

            return rows;
        }

        private static void AppendSummaryAntiInflationHeaders(List<object> row)
        {
            row.Add("平均UniqueServedNodeCount");
            row.Add("平均RepeatChargeCount");
            row.Add("平均ProactiveNearFullCount");
            row.Add("平均MeaningfulProactiveCount");
            row.Add("平均DeliveredEnergyPerTask");
            row.Add("平均DeliveredEnergyPerProactiveTask");
        }

        private static void AddSummaryAntiInflationValues(List<object> row, List<ExperimentRunSummary> list)
        {
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.UniqueServedNodeCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.RepeatChargeCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.ProactiveNearFullCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.MeaningfulProactiveCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.AverageDeliveredEnergyPerTask; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.AverageDeliveredEnergyPerProactiveTask; }));
        }

        private static List<List<object>> BuildDeathRows(ExperimentBatchResult result)
        {
            return BuildDetailedDeathRows(result);
        }

        private static List<List<object>> BuildDeathRowsLegacy(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("Run", "Seed", "演算法", "死亡時間(s)", "節點", "死亡原因", "原因代碼",
                "直接耗能源", "是否排程相關", "是否已有request", "死亡時能量(J)", "request時間(s)",
                "等待(s)", "共用資料hash"));

            if (result.DeathRecords.Count == 0)
            {
                rows.Add(Row("無死亡", "", "", "", "", "", "", "", "", "", "", "", "", ""));
                return rows;
            }

            for (int i = 0; i < result.DeathRecords.Count; i++)
            {
                ExperimentDeathRecord d = result.DeathRecords[i];
                rows.Add(Row(d.RunIndex, d.Seed, AlgorithmDisplayName(d.Algorithm), d.TimeSeconds, d.NodeId, d.ReasonZh, d.Reason,
                    d.DirectEnergyCause, d.SchedulingRelated ? "是" : "否", d.PendingRequest ? "是" : "否",
                    d.EnergyJ, d.RequestTimeSeconds, d.WaitSeconds, d.ArtifactHash));
            }

            return rows;
        }

        private static List<List<object>> BuildDetailedDeathRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("RunIndex", "Seed", "Algorithm", "ArtifactHash", "TimeSeconds", "NodeId",
                "Reason", "ReasonZh", "DirectEnergyCause", "DirectEnergyCauseZh",
                "SchedulingRelated", "SchedulingCause", "SchedulingCauseZh",
                "PendingRequest", "HasPendingRequestAtDeath", "WasScheduledInCurrentMissionAtDeath",
                "ParentIdAtDeath", "EnergyBeforeDeathJ",
                "BaseConsumeRateJPerSecondAtDeath", "EffectiveConsumeRateJPerSecondAtDeath",
                "RoutingLoadJPerSecondAtDeath", "RoutingTxLoadJPerSecondAtDeath", "RoutingRxLoadJPerSecondAtDeath",
                "RoutingSubtreeSize", "ExpectedRoutingForwardPacketsPerSecond",
                "EnergyJ", "RequestTimeSeconds", "WaitSeconds"));

            if (result.DeathRecords.Count == 0)
            {
                rows.Add(Row("NO_DEATH", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""));
                return rows;
            }

            for (int i = 0; i < result.DeathRecords.Count; i++)
            {
                ExperimentDeathRecord d = result.DeathRecords[i];
                rows.Add(Row(d.RunIndex, d.Seed, AlgorithmDisplayName(d.Algorithm), d.ArtifactHash, d.TimeSeconds, d.NodeId,
                    d.Reason, d.ReasonZh, d.DirectEnergyCause, d.DirectEnergyCauseZh,
                    d.SchedulingRelated, d.SchedulingCause, d.SchedulingCauseZh,
                    d.PendingRequest, d.HasPendingRequestAtDeath, d.WasScheduledInCurrentMissionAtDeath,
                    d.ParentIdAtDeath, d.EnergyBeforeDeathJ,
                    d.BaseConsumeRateJPerSecondAtDeath, d.EffectiveConsumeRateJPerSecondAtDeath,
                    d.RoutingLoadJPerSecondAtDeath, d.RoutingTxLoadJPerSecondAtDeath, d.RoutingRxLoadJPerSecondAtDeath,
                    d.RoutingSubtreeSize, d.ExpectedRoutingForwardPacketsPerSecond,
                    d.EnergyJ, d.RequestTimeSeconds, d.WaitSeconds));
            }

            return rows;
        }

        private static string AlgorithmDisplayName(string key)
        {
            if (key == "EDF") return "EDF（最早期限優先）";
            if (key == "NJF") return "NJF（no prediction baseline）";
            if (key == "TADP_LIN") return "TADP/LIN（時間與距離優先）";
            if (key == "RCSS") return "RCSS（風險與耗能排序）";
            if (key == "NJF_ZHENG_BPR") return "NJF_ZHENG_BPR（ZHENG BP&R deterministic）";
            if (key == "NJF_YU_BPR") return "NJF_YU_BPR（YU interval BP&R deterministic）";
            if (key == "NJF_ROUTE_ZHENG_BPR_LIMITED") return "NJF_ROUTE_ZHENG_BPR_LIMITED（Route + ZHENG BP&R，<=NmaxTask）";
            if (key == "NJF_ROUTE_ZHENG_BPR_EXTENDED") return "NJF_ROUTE_ZHENG_BPR_EXTENDED（Route + ZHENG BP&R，可超過NmaxTask）";
            if (key == "NJF_ROUTE_YU_BPR_LIMITED") return "NJF_ROUTE_YU_BPR_LIMITED（Route + YU interval BP&R，<=NmaxTask）";
            if (key == "NJF_ROUTE_YU_BPR_EXTENDED") return "NJF_ROUTE_YU_BPR_EXTENDED（Route + YU interval BP&R，可超過NmaxTask）";
            if (key == "FUZZY") return "FUZZY（模糊推論排程）";
            if (key == "GENE") return "GENE（GA route optimization）";
            if (key == "PSO") return "PSO（random-key PSO route optimization）";
            if (key == "Cuckoo") return "Cuckoo（Cuckoo Search route optimization）";
            return key;
        }

        private static string RateMultiplierRangeText(ExperimentSettings settings)
        {
            double variationRatio = settings.RateChangeVariationPercent / 100.0;
            return (1.0 - variationRatio).ToString("0.###", CultureInfo.InvariantCulture) + "~" +
                (1.0 + variationRatio).ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static List<object> Row(params object[] values)
        {
            return new List<object>(values);
        }

        private static double Average(List<ExperimentRunSummary> list, Func<ExperimentRunSummary, double> selector)
        {
            if (list.Count == 0)
                return 0.0;
            double sum = 0.0;
            for (int i = 0; i < list.Count; i++)
                sum += selector(list[i]);
            return sum / list.Count;
        }

        private static double Min(List<ExperimentRunSummary> list, Func<ExperimentRunSummary, double> selector)
        {
            if (list.Count == 0)
                return 0.0;
            double value = selector(list[0]);
            for (int i = 1; i < list.Count; i++)
                value = Math.Min(value, selector(list[i]));
            return value;
        }

        private static double Max(List<ExperimentRunSummary> list, Func<ExperimentRunSummary, double> selector)
        {
            if (list.Count == 0)
                return 0.0;
            double value = selector(list[0]);
            for (int i = 1; i < list.Count; i++)
                value = Math.Max(value, selector(list[i]));
            return value;
        }
    }

    internal class SimpleXlsxWriter
    {
        private readonly List<SimpleSheet> sheets;

        public SimpleXlsxWriter()
        {
            sheets = new List<SimpleSheet>();
        }

        public void AddSheet(string name, List<List<object>> rows)
        {
            SimpleSheet sheet = new SimpleSheet();
            sheet.Name = name;
            sheet.Rows = rows;
            sheets.Add(sheet);
        }

        public void Save(string path)
        {
            string directory = Path.GetDirectoryName(path);
            if (!String.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);
            if (File.Exists(path))
                File.Delete(path);

            using (FileStream stream = File.Create(path))
            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                WriteEntry(archive, "[Content_Types].xml", BuildContentTypes());
                WriteEntry(archive, "_rels/.rels", BuildRootRelationships());
                WriteEntry(archive, "xl/workbook.xml", BuildWorkbook());
                WriteEntry(archive, "xl/_rels/workbook.xml.rels", BuildWorkbookRelationships());
                WriteEntry(archive, "xl/styles.xml", BuildStyles());
                for (int i = 0; i < sheets.Count; i++)
                    WriteEntry(archive, "xl/worksheets/sheet" + (i + 1).ToString(CultureInfo.InvariantCulture) + ".xml", BuildWorksheet(sheets[i]));
            }
        }

        private string BuildContentTypes()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            for (int i = 0; i < sheets.Count; i++)
                sb.Append("<Override PartName=\"/xl/worksheets/sheet").Append(i + 1).Append(".xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private string BuildRootRelationships()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";
        }

        private string BuildWorkbook()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
            sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><sheets>");
            for (int i = 0; i < sheets.Count; i++)
            {
                sb.Append("<sheet name=\"").Append(XmlEscape(sheets[i].Name)).Append("\" sheetId=\"").Append(i + 1);
                sb.Append("\" r:id=\"rId").Append(i + 1).Append("\"/>");
            }
            sb.Append("</sheets></workbook>");
            return sb.ToString();
        }

        private string BuildWorkbookRelationships()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 0; i < sheets.Count; i++)
            {
                sb.Append("<Relationship Id=\"rId").Append(i + 1);
                sb.Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet");
                sb.Append(i + 1).Append(".xml\"/>");
            }
            sb.Append("<Relationship Id=\"rId").Append(sheets.Count + 1);
            sb.Append("\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        private string BuildStyles()
        {
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
                "<borders count=\"1\"><border><left/><right/><top/><bottom/><diagonal/></border></borders>" +
                "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
                "<cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\"/></cellXfs>" +
                "<cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles>" +
                "</styleSheet>";
        }

        private string BuildWorksheet(SimpleSheet sheet)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
            sb.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetViews><sheetView workbookViewId=\"0\"><pane ySplit=\"1\" topLeftCell=\"A2\" activePane=\"bottomLeft\" state=\"frozen\"/></sheetView></sheetViews><sheetData>");
            for (int r = 0; r < sheet.Rows.Count; r++)
            {
                sb.Append("<row r=\"").Append(r + 1).Append("\">");
                List<object> row = sheet.Rows[r];
                for (int c = 0; c < row.Count; c++)
                    AppendCell(sb, r + 1, c + 1, row[c]);
                sb.Append("</row>");
            }
            sb.Append("</sheetData></worksheet>");
            return sb.ToString();
        }

        private void AppendCell(StringBuilder sb, int row, int col, object value)
        {
            string cellRef = ColumnName(col) + row.ToString(CultureInfo.InvariantCulture);
            if (value == null)
            {
                sb.Append("<c r=\"").Append(cellRef).Append("\"/>");
                return;
            }

            if (value is int || value is long || value is double || value is float || value is decimal)
            {
                double number = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (Double.IsNaN(number) || Double.IsInfinity(number))
                {
                    AppendInlineStringCell(sb, cellRef, "");
                    return;
                }
                sb.Append("<c r=\"").Append(cellRef).Append("\"><v>");
                sb.Append(number.ToString("G17", CultureInfo.InvariantCulture));
                sb.Append("</v></c>");
                return;
            }

            AppendInlineStringCell(sb, cellRef, Convert.ToString(value, CultureInfo.InvariantCulture));
        }

        private void AppendInlineStringCell(StringBuilder sb, string cellRef, string text)
        {
            sb.Append("<c r=\"").Append(cellRef).Append("\" t=\"inlineStr\"><is><t>");
            sb.Append(XmlEscape(text));
            sb.Append("</t></is></c>");
        }

        private static string ColumnName(int index)
        {
            StringBuilder name = new StringBuilder();
            while (index > 0)
            {
                int mod = (index - 1) % 26;
                name.Insert(0, (char)('A' + mod));
                index = (index - mod - 1) / 26;
            }
            return name.ToString();
        }

        private static void WriteEntry(ZipArchive archive, string path, string content)
        {
            ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
            using (Stream stream = entry.Open())
            using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
            {
                writer.Write(content);
            }
        }

        private static string XmlEscape(string value)
        {
            if (value == null)
                return "";
            return value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;")
                .Replace("\"", "&quot;").Replace("'", "&apos;");
        }

        private class SimpleSheet
        {
            public string Name;
            public List<List<object>> Rows;
        }
    }

    public class ExperimentSettingsDialog : Form
    {
        private readonly Dictionary<string, TextBox> boxes;
        private readonly CheckedListBox algorithmList;
        private ExperimentSettings settings;

        public ExperimentSettingsDialog(ExperimentSettings currentSettings)
        {
            settings = currentSettings;
            settings.Normalize();
            boxes = new Dictionary<string, TextBox>();
            algorithmList = new CheckedListBox();
            InitializeDialog();
            LoadSettingsToControls();
        }

        public ExperimentSettings Settings
        {
            get { return settings; }
        }

        private void InitializeDialog()
        {
            Text = "WSN 實驗設定";
            Width = 640;
            Height = 790;
            StartPosition = FormStartPosition.CenterParent;

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.AutoScroll = true;
            Controls.Add(panel);

            int y = 14;
            AddTextBox(panel, "BaseSeed", "基礎亂數種子", y); y += 30;
            AddTextBox(panel, "RunCount", "Run 次數", y); y += 30;
            AddTextBox(panel, "SensorCount", "感測器數量", y); y += 30;
            AddTextBox(panel, "MapWidthMeters", "地圖邊長(m)", y); y += 30;
            AddTextBox(panel, "SimulationTimeSeconds", "模擬時間(s)", y); y += 30;
            AddTextBox(panel, "MaxParallelJobs", "最大平行工作數", y); y += 30;
            AddTextBox(panel, "InitialEnergyJ", "初始能量(J)", y); y += 30;
            AddTextBox(panel, "SensorBackgroundLifetimeSeconds", "背景壽命(s)", y); y += 30;
            AddTextBox(panel, "InitialResidualJitterPercent", "初始能量擾動(%)", y); y += 30;
            AddTextBox(panel, "EventRatePerSecond", "事件率(封包/s)", y); y += 30;
            AddTextBox(panel, "PacketBits", "封包大小(bits)", y); y += 30;
            AddTextBox(panel, "RadioRangeMeters", "通訊半徑(m)", y); y += 30;
            AddTextBox(panel, "ReceiverEnergyNjPerBit", "接收能耗(nJ/bit)", y); y += 30;
            AddTextBox(panel, "AmplifierEnergyNjPerBitM2", "放大器能耗(nJ/bit/m^2)", y); y += 30;
            AddTextBox(panel, "PowerExponent", "距離耗能次方", y); y += 30;
            AddTextBox(panel, "WcvSpeedMetersPerSecond", "WCV 速度(m/s)", y); y += 30;
            AddTextBox(panel, "WcvChargeRateJPerSecond", "WCV 充電速率(J/s)", y); y += 30;
            AddTextBox(panel, "WcvCapacityJ", "WCV 容量(J)", y); y += 30;
            AddTextBox(panel, "WcvMoveCostJPerMeter", "WCV 移動耗能(J/m)", y); y += 30;
            AddTextBox(panel, "NmaxTask", "每趟任務上限", y); y += 30;
            AddTextBox(panel, "ThresholdMode", "門檻模式", y); y += 30;
            AddTextBox(panel, "RequestThresholdPercent", "剩餘能量門檻(%)", y); y += 30;
            AddTextBox(panel, "TreqSeconds", "Treq 秒數", y); y += 30;
            AddTextBox(panel, "BprDeadlineThresholdSeconds", "BP&R deadline threshold(s)", y); y += 30;
            AddTextBox(panel, "AllowStandaloneProactiveDispatch", "Allow standalone proactive", y); y += 30;
            AddTextBox(panel, "ProactivePredictionHorizonSeconds", "Proactive horizon(s)", y); y += 30;
            AddTextBox(panel, "ProactiveCandidateMaxEnergyRatio", "Proactive max energy ratio", y); y += 30;
            AddTextBox(panel, "ProactiveCooldownSeconds", "Proactive cooldown(s)", y); y += 30;
            AddTextBox(panel, "YuDangerWindowSeconds", "YU danger window(s)", y); y += 30;
            AddTextBox(panel, "YuDangerThresholdK", "YU danger threshold K", y); y += 30;
            AddTextBox(panel, "YuIntervalUncertaintySeconds", "YU interval uncertainty(s)", y); y += 30;
            AddTextBox(panel, "PrateChange", "Prate_change", y); y += 30;
            AddTextBox(panel, "RateChangeVariationPercent", "耗能變動幅度(%)", y); y += 30;
            AddTextBox(panel, "OutputDirectory", "輸出資料夾", y); y += 35;

            Label algorithmLabel = new Label();
            algorithmLabel.Text = "演算法";
            algorithmLabel.Location = new System.Drawing.Point(20, y);
            algorithmLabel.AutoSize = true;
            panel.Controls.Add(algorithmLabel);

            algorithmList.Location = new System.Drawing.Point(160, y);
            algorithmList.Size = new System.Drawing.Size(420, 130);
            algorithmList.CheckOnClick = true;
            panel.Controls.Add(algorithmList);
            y += 140;

            Button saveButton = new Button();
            saveButton.Text = "儲存";
            saveButton.Location = new System.Drawing.Point(160, y);
            saveButton.Click += delegate
            {
                ApplyControlsToSettings();
                settings.SaveLast();
                MessageBox.Show(this, "設定已儲存。", "WSN 實驗設定", MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            panel.Controls.Add(saveButton);

            Button okButton = new Button();
            okButton.Text = "確定";
            okButton.Location = new System.Drawing.Point(250, y);
            okButton.Click += delegate
            {
                ApplyControlsToSettings();
                DialogResult = DialogResult.OK;
                Close();
            };
            panel.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Location = new System.Drawing.Point(340, y);
            cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            panel.Controls.Add(cancelButton);
        }

        private void AddTextBox(Panel panel, string key, string label, int y)
        {
            Label l = new Label();
            l.Text = label;
            l.Location = new System.Drawing.Point(20, y + 4);
            l.AutoSize = true;
            panel.Controls.Add(l);

            TextBox box = new TextBox();
            box.Location = new System.Drawing.Point(160, y);
            box.Size = new System.Drawing.Size(420, 22);
            panel.Controls.Add(box);
            boxes[key] = box;
        }

        private void LoadSettingsToControls()
        {
            boxes["BaseSeed"].Text = settings.BaseSeed.ToString(CultureInfo.InvariantCulture);
            boxes["RunCount"].Text = settings.RunCount.ToString(CultureInfo.InvariantCulture);
            boxes["SensorCount"].Text = settings.SensorCount.ToString(CultureInfo.InvariantCulture);
            boxes["MapWidthMeters"].Text = settings.MapWidthMeters.ToString(CultureInfo.InvariantCulture);
            boxes["SimulationTimeSeconds"].Text = settings.SimulationTimeSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["MaxParallelJobs"].Text = settings.MaxParallelJobs.ToString(CultureInfo.InvariantCulture);
            boxes["InitialEnergyJ"].Text = settings.InitialEnergyJ.ToString(CultureInfo.InvariantCulture);
            boxes["SensorBackgroundLifetimeSeconds"].Text = settings.SensorBackgroundLifetimeSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["InitialResidualJitterPercent"].Text = settings.InitialResidualJitterPercent.ToString(CultureInfo.InvariantCulture);
            boxes["EventRatePerSecond"].Text = settings.EventRatePerSecond.ToString(CultureInfo.InvariantCulture);
            boxes["PacketBits"].Text = settings.PacketBits.ToString(CultureInfo.InvariantCulture);
            boxes["RadioRangeMeters"].Text = settings.RadioRangeMeters.ToString(CultureInfo.InvariantCulture);
            boxes["ReceiverEnergyNjPerBit"].Text = settings.ReceiverEnergyNjPerBit.ToString(CultureInfo.InvariantCulture);
            boxes["AmplifierEnergyNjPerBitM2"].Text = settings.AmplifierEnergyNjPerBitM2.ToString(CultureInfo.InvariantCulture);
            boxes["PowerExponent"].Text = settings.PowerExponent.ToString(CultureInfo.InvariantCulture);
            boxes["WcvSpeedMetersPerSecond"].Text = settings.WcvSpeedMetersPerSecond.ToString(CultureInfo.InvariantCulture);
            boxes["WcvChargeRateJPerSecond"].Text = settings.WcvChargeRateJPerSecond.ToString(CultureInfo.InvariantCulture);
            boxes["WcvCapacityJ"].Text = settings.WcvCapacityJ.ToString(CultureInfo.InvariantCulture);
            boxes["WcvMoveCostJPerMeter"].Text = settings.WcvMoveCostJPerMeter.ToString(CultureInfo.InvariantCulture);
            boxes["NmaxTask"].Text = settings.NmaxTask.ToString(CultureInfo.InvariantCulture);
            boxes["ThresholdMode"].Text = settings.ThresholdMode == "TreqSeconds" ? "Treq 秒數門檻" : "百分比門檻";
            boxes["RequestThresholdPercent"].Text = settings.RequestThresholdPercent.ToString(CultureInfo.InvariantCulture);
            boxes["TreqSeconds"].Text = settings.TreqSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["BprDeadlineThresholdSeconds"].Text = settings.BprDeadlineThresholdSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["AllowStandaloneProactiveDispatch"].Text = settings.AllowStandaloneProactiveDispatch.ToString(CultureInfo.InvariantCulture);
            boxes["ProactivePredictionHorizonSeconds"].Text = settings.ProactivePredictionHorizonSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["ProactiveCandidateMaxEnergyRatio"].Text = settings.ProactiveCandidateMaxEnergyRatio.ToString(CultureInfo.InvariantCulture);
            boxes["ProactiveCooldownSeconds"].Text = settings.ProactiveCooldownSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["YuDangerWindowSeconds"].Text = settings.YuDangerWindowSeconds.ToString(CultureInfo.InvariantCulture);
            boxes["YuDangerThresholdK"].Text = settings.YuDangerThresholdK.ToString(CultureInfo.InvariantCulture);
            boxes["YuIntervalUncertaintySeconds"].Text = settings.YuIntervalUncertaintySeconds.ToString(CultureInfo.InvariantCulture);
            boxes["PrateChange"].Text = settings.PrateChange.ToString(CultureInfo.InvariantCulture);
            boxes["RateChangeVariationPercent"].Text = settings.RateChangeVariationPercent.ToString(CultureInfo.InvariantCulture);
            boxes["OutputDirectory"].Text = settings.OutputDirectory;

            algorithmList.Items.Clear();
            List<string> selected = settings.GetSelectedAlgorithms();
            string[] all = ExperimentSettings.AllAlgorithms();
            for (int i = 0; i < all.Length; i++)
                algorithmList.Items.Add(AlgorithmDisplayName(all[i]), selected.Contains(all[i]));
        }

        private void ApplyControlsToSettings()
        {
            settings.BaseSeed = ParseInt("BaseSeed", settings.BaseSeed);
            settings.RunCount = ParseInt("RunCount", settings.RunCount);
            settings.SensorCount = ParseInt("SensorCount", settings.SensorCount);
            double mapSizeMeters = ParseDouble("MapWidthMeters", settings.MapWidthMeters);
            settings.MapWidthMeters = mapSizeMeters;
            settings.MapHeightMeters = mapSizeMeters;
            settings.SimulationTimeSeconds = ParseDouble("SimulationTimeSeconds", settings.SimulationTimeSeconds);
            settings.MaxParallelJobs = ParseInt("MaxParallelJobs", settings.MaxParallelJobs);
            settings.InitialEnergyJ = ParseDouble("InitialEnergyJ", settings.InitialEnergyJ);
            settings.SensorBackgroundLifetimeSeconds = ParseDouble("SensorBackgroundLifetimeSeconds", settings.SensorBackgroundLifetimeSeconds);
            settings.InitialResidualJitterPercent = ParseDouble("InitialResidualJitterPercent", settings.InitialResidualJitterPercent);
            settings.EventRatePerSecond = ParseDouble("EventRatePerSecond", settings.EventRatePerSecond);
            settings.PacketBits = ParseDouble("PacketBits", settings.PacketBits);
            settings.RadioRangeMeters = ParseDouble("RadioRangeMeters", settings.RadioRangeMeters);
            settings.ReceiverEnergyNjPerBit = ParseDouble("ReceiverEnergyNjPerBit", settings.ReceiverEnergyNjPerBit);
            settings.AmplifierEnergyNjPerBitM2 = ParseDouble("AmplifierEnergyNjPerBitM2", settings.AmplifierEnergyNjPerBitM2);
            settings.PowerExponent = ParseDouble("PowerExponent", settings.PowerExponent);
            settings.WcvSpeedMetersPerSecond = ParseDouble("WcvSpeedMetersPerSecond", settings.WcvSpeedMetersPerSecond);
            settings.WcvChargeRateJPerSecond = ParseDouble("WcvChargeRateJPerSecond", settings.WcvChargeRateJPerSecond);
            settings.WcvCapacityJ = ParseDouble("WcvCapacityJ", settings.WcvCapacityJ);
            settings.WcvMoveCostJPerMeter = ParseDouble("WcvMoveCostJPerMeter", settings.WcvMoveCostJPerMeter);
            settings.NmaxTask = ParseInt("NmaxTask", settings.NmaxTask);
            settings.ThresholdMode = ParseThresholdMode(boxes["ThresholdMode"].Text.Trim(), settings.ThresholdMode);
            settings.RequestThresholdPercent = ParseDouble("RequestThresholdPercent", settings.RequestThresholdPercent);
            settings.TreqSeconds = ParseDouble("TreqSeconds", settings.TreqSeconds);
            settings.BprDeadlineThresholdSeconds = ParseDouble("BprDeadlineThresholdSeconds", settings.BprDeadlineThresholdSeconds);
            settings.AllowStandaloneProactiveDispatch = ParseBool("AllowStandaloneProactiveDispatch", settings.AllowStandaloneProactiveDispatch);
            settings.ProactivePredictionHorizonSeconds = ParseDouble("ProactivePredictionHorizonSeconds", settings.ProactivePredictionHorizonSeconds);
            settings.ProactiveCandidateMaxEnergyRatio = ParseDouble("ProactiveCandidateMaxEnergyRatio", settings.ProactiveCandidateMaxEnergyRatio);
            settings.ProactiveCooldownSeconds = ParseDouble("ProactiveCooldownSeconds", settings.ProactiveCooldownSeconds);
            settings.YuDangerWindowSeconds = ParseDouble("YuDangerWindowSeconds", settings.YuDangerWindowSeconds);
            settings.YuDangerThresholdK = ParseInt("YuDangerThresholdK", settings.YuDangerThresholdK);
            settings.YuIntervalUncertaintySeconds = ParseDouble("YuIntervalUncertaintySeconds", settings.YuIntervalUncertaintySeconds);
            settings.PrateChange = ParseDouble("PrateChange", settings.PrateChange);
            settings.RateChangeVariationPercent = ParseDouble("RateChangeVariationPercent", settings.RateChangeVariationPercent);
            settings.OutputDirectory = boxes["OutputDirectory"].Text.Trim();

            List<string> algorithms = new List<string>();
            for (int i = 0; i < algorithmList.CheckedItems.Count; i++)
                algorithms.Add(AlgorithmKeyFromDisplay(Convert.ToString(algorithmList.CheckedItems[i], CultureInfo.InvariantCulture)));
            settings.SetSelectedAlgorithms(algorithms);
            settings.Normalize();
        }

        private string ParseThresholdMode(string value, string fallback)
        {
            if (String.IsNullOrWhiteSpace(value))
                return fallback;
            if (value.IndexOf("Treq", StringComparison.OrdinalIgnoreCase) >= 0 || value.Contains("秒"))
                return "TreqSeconds";
            if (value.IndexOf("Percent", StringComparison.OrdinalIgnoreCase) >= 0 || value.Contains("百分") || value.Contains("%"))
                return "Percent";
            return fallback;
        }

        private string AlgorithmDisplayName(string key)
        {
            if (key == "EDF") return "EDF（最早期限優先）";
            if (key == "NJF") return "NJF（no prediction baseline）";
            if (key == "TADP_LIN") return "TADP/LIN（時間與距離優先）";
            if (key == "RCSS") return "RCSS（風險與耗能排序）";
            if (key == "NJF_ZHENG_BPR") return "NJF_ZHENG_BPR（ZHENG BP&R deterministic）";
            if (key == "NJF_YU_BPR") return "NJF_YU_BPR（YU interval BP&R deterministic）";
            if (key == "NJF_ROUTE_ZHENG_BPR_LIMITED") return "NJF_ROUTE_ZHENG_BPR_LIMITED（Route + ZHENG BP&R，<=NmaxTask）";
            if (key == "NJF_ROUTE_ZHENG_BPR_EXTENDED") return "NJF_ROUTE_ZHENG_BPR_EXTENDED（Route + ZHENG BP&R，可超過NmaxTask）";
            if (key == "NJF_ROUTE_YU_BPR_LIMITED") return "NJF_ROUTE_YU_BPR_LIMITED（Route + YU interval BP&R，<=NmaxTask）";
            if (key == "NJF_ROUTE_YU_BPR_EXTENDED") return "NJF_ROUTE_YU_BPR_EXTENDED（Route + YU interval BP&R，可超過NmaxTask）";
            if (key == "FUZZY") return "FUZZY（模糊推論排程）";
            if (key == "GENE") return "GENE（GA route optimization）";
            if (key == "PSO") return "PSO（random-key PSO route optimization）";
            if (key == "Cuckoo") return "Cuckoo（Cuckoo Search route optimization）";
            return key;
        }

        private string AlgorithmKeyFromDisplay(string display)
        {
            if (String.IsNullOrWhiteSpace(display))
                return "";
            int index = display.IndexOf('（');
            string key = index > 0 ? display.Substring(0, index) : display;
            return ExperimentSettings.CanonicalAlgorithmKey(key.Replace("TADP/LIN", "TADP_LIN").Replace("NJF+BP&R", "NJF_BPR").Trim());
        }

        private int ParseInt(string key, int fallback)
        {
            int value;
            if (Int32.TryParse(boxes[key].Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
                return value;
            return fallback;
        }

        private double ParseDouble(string key, double fallback)
        {
            double value;
            if (Double.TryParse(boxes[key].Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                return value;
            if (Double.TryParse(boxes[key].Text.Trim(), out value))
                return value;
            return fallback;
        }

        private bool ParseBool(string key, bool fallback)
        {
            string value = boxes[key].Text.Trim();
            bool parsed;
            if (Boolean.TryParse(value, out parsed))
                return parsed;
            if (String.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "yes", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "y", StringComparison.OrdinalIgnoreCase))
                return true;
            if (String.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "no", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(value, "n", StringComparison.OrdinalIgnoreCase))
                return false;
            return fallback;
        }
    }
}
