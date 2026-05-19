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
                "NJF_BPR",
                "NJF_BPR_ROUTE_SAFE_LIMITED",
                "NJF_BPR_ROUTE_SAFE_EXTENDED",
                "FUZZY",
                "GENE",
                "PSO",
                "Cuckoo"
            };
        }

        public static string DefaultAlgorithmSelectionCsv()
        {
            return "EDF,NJF,TADP_LIN,RCSS,NJF_BPR,NJF_BPR_ROUTE_SAFE_LIMITED,FUZZY";
        }

        public static string CanonicalAlgorithmKey(string algorithm)
        {
            if (String.IsNullOrWhiteSpace(algorithm))
                return "";

            string key = algorithm.Trim();
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE", StringComparison.OrdinalIgnoreCase))
                return "NJF_BPR_ROUTE_SAFE_EXTENDED";
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
        private readonly ExperimentSettings settings;
        private readonly ExperimentArtifact artifact;
        private readonly string algorithm;
        private readonly Random algorithmRandom;
        private readonly SensorState[] sensors;
        private readonly List<ChargingRequest> activeRequests;
        private readonly List<ExperimentDeathRecord> deaths;
        private readonly ExperimentRunSummary summary;
        private readonly MissionDetailCsvWriter csvWriter;
        private int totalTaskRecordCount;
        private int nextEventIndex;
        private int nextRateChangeIndex;
        private int nextRequestId;
        private int missionId;
        private double currentTime;
        private bool stopForFirstDeath;
        private HashSet<int> plannedMissionNodeIds;

        private enum BprProactiveSelectionMode
        {
            Deterministic,
            RouteInsertionCost
        }

        private sealed class BprSTableEntry
        {
            public int NodeId;
            public double RemainingWorkSeconds;
            public double RequestDeadlineSeconds;
            public double DepletionDeadlineSeconds;
            public double EnergyJ;
            public double ConsumeRateJPerSecond;
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

            for (int i = 0; i < artifact.Sensors.Count; i++)
                sensors[i] = new SensorState(artifact.Sensors[i], settings);

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

                if (UsesBprBottleneckCandidates() && HasBprBottleneckCandidate())
                {
                    ExecuteMission();
                    continue;
                }

                double nextTime = FindNextInterestingTime(settings.SimulationTimeSeconds, null);
                if (UsesBprBottleneckCandidates())
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

            ExperimentRunResult result = new ExperimentRunResult();
            result.Summary = summary;
            result.TotalTaskRecordCount = totalTaskRecordCount;
            result.Deaths = deaths;
            if (csvWriter != null)
                csvWriter.Dispose();
            return result;
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
                record.InternalRateNjPerTick = sensor.ConsumeRateJPerSecond * 1000000000.0 * 0.01;
                record.DeliveredEnergyJ = context.DeliveredEnergyJ;
                record.DistanceFromPreviousMeters = distance;
                record.Success = success && sensor.Alive;
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
            plannedMissionNodeIds = null;
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
            if (csvWriter != null)
                csvWriter.WriteTask(record);
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

            if (algorithm == "NJF_BPR_ROUTE_SAFE_LIMITED")
                return BuildRouteSafeNjfBpr(pool, maxTask, true);
            if (algorithm == "NJF_BPR_ROUTE_SAFE_EXTENDED")
                return BuildRouteSafeNjfBpr(pool, maxTask, false);

            if (algorithm == "NJF_BPR")
            {
                List<ChargingRequest> cplist = BuildZhengBprCplist(
                    pool,
                    maxTask,
                    BprProactiveSelectionMode.Deterministic,
                    false);
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
            return algorithm == "NJF_BPR" ||
                algorithm == "NJF_BPR_ROUTE_SAFE_LIMITED" ||
                algorithm == "NJF_BPR_ROUTE_SAFE_EXTENDED";
        }

        private bool HasBprBottleneckCandidate()
        {
            if (!UsesBprBottleneckCandidates())
                return false;

            int maxTask = GetMissionTaskLimit();
            bool allowCapacityOverflow = algorithm == "NJF_BPR_ROUTE_SAFE_EXTENDED";
            BprProactiveSelectionMode selectionMode = algorithm == "NJF_BPR"
                ? BprProactiveSelectionMode.Deterministic
                : BprProactiveSelectionMode.RouteInsertionCost;
            List<ChargingRequest> cplist = BuildZhengBprCplist(
                new List<ChargingRequest>(),
                maxTask,
                selectionMode,
                allowCapacityOverflow);
            return cplist.Count > 0;
        }

        private double FindNextBprBottleneckCandidateTime()
        {
            return HasBprBottleneckCandidate() ? currentTime : Double.PositiveInfinity;
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
                if (sensor.ConsumeRateJPerSecond <= 0.0)
                    continue;

                double timeToRequest = Math.Max(0.0, (sensor.EnergyJ - threshold) / sensor.ConsumeRateJPerSecond);
                double timeToDeath = Math.Max(0.0, sensor.EnergyJ / sensor.ConsumeRateJPerSecond);
                if (timeToRequest > settings.TreqSeconds * 1.5 && sensor.EnergyJ > settings.InitialEnergyJ * 0.35)
                    continue;

                ChargingRequest proactive = new ChargingRequest();
                proactive.RequestId = -id;
                proactive.NodeId = id;
                proactive.RequestTimeSeconds = currentTime;
                proactive.DeadlineSeconds = currentTime + timeToDeath;
                proactive.RequestEnergyJ = sensor.EnergyJ;
                proactive.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
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

        private List<ChargingRequest> BuildRouteSafeNjfBpr(List<ChargingRequest> requiredRequests, int maxTask, bool enforceTaskLimit)
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

            List<BprSTableEntry> sTable = BuildBprSTable(reservedNodeIds);
            if (sTable.Count == 0)
                return cplist;

            double tjob = EstimateBprTjobSeconds(maxTask);
            double tdeadlineThreshold = GetBprDeadlineThresholdSeconds();
            int scanIndex = 0;
            int safety = 0;
            while (scanIndex < sTable.Count && safety < sensors.Length * sensors.Length + sensors.Length)
            {
                safety++;
                BprSTableEntry anchor = sTable[scanIndex];
                double windowStart = anchor.RequestDeadlineSeconds;
                double windowEnd = windowStart + tjob;
                List<BprSTableEntry> bottleList = BuildBprBottleList(
                    sTable,
                    windowStart,
                    windowEnd,
                    tdeadlineThreshold);

                if (bottleList.Count <= maxTask)
                {
                    scanIndex++;
                    continue;
                }

                int removalCount = bottleList.Count - maxTask;
                int capacityLeft = allowCapacityOverflow ? removalCount : Math.Max(0, maxTask - cplist.Count);
                int addCount = Math.Min(removalCount, capacityLeft);
                if (addCount <= 0)
                    return cplist;

                List<BprSTableEntry> addList = SelectBprProactiveEntries(
                    bottleList,
                    addCount,
                    cplist,
                    selectionMode);
                if (addList.Count == 0)
                {
                    scanIndex++;
                    continue;
                }

                for (int i = 0; i < addList.Count; i++)
                {
                    BprSTableEntry entry = addList[i];
                    cplist.Add(CreateBprProactiveRequest(entry));
                    reservedNodeIds.Add(entry.NodeId);
                }

                sTable.RemoveAll(delegate (BprSTableEntry entry)
                {
                    return reservedNodeIds.Contains(entry.NodeId);
                });
                scanIndex = 0;
            }

            return cplist;
        }

        private List<BprSTableEntry> BuildBprSTable(HashSet<int> reservedNodeIds)
        {
            List<BprSTableEntry> sTable = new List<BprSTableEntry>();
            for (int id = 1; id < sensors.Length; id++)
            {
                if (reservedNodeIds != null && reservedNodeIds.Contains(id))
                    continue;

                SensorState sensor = sensors[id];
                if (!sensor.Alive || sensor.HasPendingRequest || HasActiveRequestForNode(id) ||
                    sensor.ConsumeRateJPerSecond <= 1e-12)
                    continue;

                double threshold = GetRequestThresholdJ(sensor);
                if (sensor.EnergyJ <= threshold + Epsilon)
                    continue;

                double remainingWork = (sensor.EnergyJ - threshold) / sensor.ConsumeRateJPerSecond;
                if (Double.IsNaN(remainingWork) || Double.IsInfinity(remainingWork) || remainingWork < 0.0)
                    continue;

                BprSTableEntry entry = new BprSTableEntry();
                entry.NodeId = id;
                entry.RemainingWorkSeconds = remainingWork;
                entry.RequestDeadlineSeconds = currentTime + remainingWork;
                entry.DepletionDeadlineSeconds = currentTime + sensor.EnergyJ / sensor.ConsumeRateJPerSecond;
                entry.EnergyJ = sensor.EnergyJ;
                entry.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
                sTable.Add(entry);
            }

            sTable.Sort(CompareBprSTableByDeadline);
            return sTable;
        }

        private static int CompareBprSTableByDeadline(BprSTableEntry a, BprSTableEntry b)
        {
            int compare = a.RequestDeadlineSeconds.CompareTo(b.RequestDeadlineSeconds);
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
                double intervalStart = entry.RequestDeadlineSeconds - tdeadlineThreshold;
                double intervalEnd = entry.RequestDeadlineSeconds + tdeadlineThreshold;
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
                    compare = a.RequestDeadlineSeconds.CompareTo(b.RequestDeadlineSeconds);
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
            proactive.DeadlineSeconds = entry.RequestDeadlineSeconds;
            proactive.RequestEnergyJ = entry.EnergyJ;
            proactive.ConsumeRateJPerSecond = entry.ConsumeRateJPerSecond;
            proactive.CriticalDensity = 0.0;
            proactive.IsProactive = true;
            proactive.ProactiveReason = "ZHENG_BPR_BOTTLENECK";
            return proactive;
        }

        private double GetBprDeadlineThresholdSeconds()
        {
            return Math.Max(1.0, settings.TreqSeconds);
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
                    maxRate = Math.Max(maxRate, remaining[i].ConsumeRateJPerSecond);
                }

                int bestIndex = 0;
                double bestScore = Double.MaxValue;
                for (int i = 0; i < remaining.Count; i++)
                {
                    double urgency = Math.Max(0.0, remaining[i].DeadlineSeconds - currentTime) / maxSlack;
                    double distance = DistanceFrom(x, y, remaining[i].NodeId) / maxDistance;
                    double rate = 1.0 - remaining[i].ConsumeRateJPerSecond / maxRate;
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
                    double rateRatio = sensor.ConsumeRateJPerSecond / Math.Max(1e-9, settings.InitialEnergyJ / settings.SensorBackgroundLifetimeSeconds);
                    double density = ComputeCriticalNodeDensity(request.NodeId);
                    double priority = FuzzyPriority(residualRatio, distanceRatio, rateRatio, density);
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
                pendingEnergy[nodeId] = pendingEnergy[nodeId] - sensors[nodeId].ConsumeRateJPerSecond * deltaSeconds;
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

        private double FuzzyPriority(double residualRatio, double distanceRatio, double rateRatio, double densityRatio)
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

            double analyticBias = (1.0 - residualRatio) * 0.42 + (1.0 - distanceRatio) * 0.18 +
                Math.Min(1.0, rateRatio / 2.0) * 0.24 + densityRatio * 0.16;
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

                double effectiveRate = sensor.ConsumeRateJPerSecond;
                if (charging != null && charging.NodeId == id)
                    effectiveRate -= charging.ChargeRateJPerSecond;
                if (effectiveRate <= 1e-12)
                    continue;

                best = Math.Min(best, currentTime + (sensor.EnergyJ - threshold) / effectiveRate);
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

                double effectiveRate = sensor.ConsumeRateJPerSecond;
                if (charging != null && charging.NodeId == id)
                    effectiveRate -= charging.ChargeRateJPerSecond;
                if (effectiveRate <= 1e-12)
                    continue;

                best = Math.Min(best, currentTime + sensor.EnergyJ / effectiveRate);
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
                    sensors[change.NodeId].RateScale *= change.Multiplier;
                    sensors[change.NodeId].RefreshConsumeRate(settings);
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
                    continue;
                }

                double threshold = GetRequestThresholdJ(sensor);
                if (sensor.EnergyJ <= threshold + Epsilon)
                {
                    ChargingRequest request = new ChargingRequest();
                    request.RequestId = nextRequestId++;
                    request.NodeId = id;
                    request.RequestTimeSeconds = currentTime;
                    request.DeadlineSeconds = currentTime + Math.Max(0.0, sensor.EnergyJ / Math.Max(sensor.ConsumeRateJPerSecond, 1e-12));
                    request.RequestEnergyJ = sensor.EnergyJ;
                    request.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
                    request.CriticalDensity = ComputeCriticalNodeDensity(id);
                    request.IsProactive = false;
                    request.ProactiveReason = "";
                    sensor.HasPendingRequest = true;
                    activeRequests.Add(request);
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
            sensor.Alive = false;
            sensor.EnergyJ = 0.0;

            bool schedulingRelated = sensor.HasPendingRequest || activeRequests.Exists(delegate (ChargingRequest rq) { return rq.NodeId == nodeId; });
            string reason = schedulingRelated ? "scheduling_wait" : directReason;
            string reasonZh = ReasonZh(reason);

            ExperimentDeathRecord death = new ExperimentDeathRecord();
            death.RunIndex = artifact.RunIndex;
            death.Seed = artifact.Seed;
            death.Algorithm = algorithm;
            death.ArtifactHash = artifact.ArtifactHash;
            death.TimeSeconds = time;
            death.NodeId = nodeId;
            death.Reason = reason;
            death.ReasonZh = reasonZh;
            death.DirectEnergyCause = directReason;
            death.SchedulingRelated = schedulingRelated;
            death.PendingRequest = sensor.HasPendingRequest;
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
                summary.FirstDeadDirectEnergyCause = directReason;
                summary.FirstDeadSchedulingRelated = schedulingRelated;
                summary.NetworkLifetimeSeconds = time;
                stopForFirstDeath = true;
            }
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
            if (reason == "continuous")
                return "連續耗能耗盡";
            return "未知";
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
            ExperimentSimulation simulation = null;
            try
            {
                ExperimentSettings settings = ExperimentSettings.CreateDefault();
                settings.BaseSeed = 777;
                settings.RunCount = 1;
                settings.SensorCount = 2;
                settings.MapWidthMeters = 30.0;
                settings.MapHeightMeters = 1.0;
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
                settings.PrateChange = 0.0;
                settings.RateChangeVariationPercent = 0.0;
                settings.SelectedAlgorithmsCsv = "NJF_BPR_ROUTE_SAFE_LIMITED";
                settings.OutputDirectory = tempDirectory;
                settings.Normalize();

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

                SensorTemplate sensor = new SensorTemplate();
                sensor.Id = 1;
                sensor.X = 10.0;
                sensor.Y = 0.0;
                sensor.InitialEnergyJ = 55.0;
                sensor.ParentId = 0;
                artifact.Sensors.Add(sensor);

                SensorTemplate secondSensor = new SensorTemplate();
                secondSensor.Id = 2;
                secondSensor.X = 20.0;
                secondSensor.Y = 0.0;
                secondSensor.InitialEnergyJ = 90.0;
                secondSensor.ParentId = 0;
                artifact.Sensors.Add(secondSensor);

                simulation = new ExperimentSimulation(settings, artifact, "NJF_BPR_ROUTE_SAFE_LIMITED", tempDirectory);
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
                    "run001-NJF_BPR_ROUTE_SAFE_LIMITED-task-records.csv");
                AssertSelfTest(File.Exists(taskPath), "Task-record CSV was not written by the self-test.");
                string[] taskLines = File.ReadAllLines(taskPath, Encoding.UTF8);
                AssertSelfTest(taskLines.Length == 2,
                    "Task-record CSV should contain one data row for the mission.");

                string[] fields = taskLines[1].Split(',');
                AssertSelfTest(fields.Length > 8 && fields[4] == "1" && fields[6] == "1",
                    "Task-record CSV row does not describe mission 1 / sensor 1.");
                AssertSelfTest(String.Equals(fields[7], "proactive", StringComparison.OrdinalIgnoreCase),
                    "Reserved node should remain a proactive task, not be relabeled as natural.");
            }
            finally
            {
                if (simulation != null && simulation.csvWriter != null)
                    simulation.csvWriter.Dispose();
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

        private static void AssertSelfTest(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
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
        public bool FirstDeadSchedulingRelated;
        public int SuccessfulCharges;
        public int FailedOrLateTasks;
        public int NaturalRequestCount;
        public int ProactiveTaskCount;
        public int RequestCount;
        public int MissionCount;
        public double MovementDistanceMeters;
        public double MoveEnergyJ;
        public double DeliveredEnergyJ;
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
        public bool SchedulingRelated;
        public bool PendingRequest;
        public double EnergyJ;
        public double RequestTimeSeconds;
        public double WaitSeconds;
    }

    internal sealed class MissionDetailCsvWriter : IDisposable
    {
        private readonly StreamWriter missionWriter;
        private readonly StreamWriter taskWriter;
        private bool disposed;

        public MissionDetailCsvWriter(string directory, int runIndex, string algorithm)
        {
            Directory.CreateDirectory(directory);
            string safeAlgorithm = SanitizeFileName(algorithm);
            string missionPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-mission-details.csv", runIndex, safeAlgorithm));
            string taskPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "run{0:D3}-{1}-task-records.csv", runIndex, safeAlgorithm));

            Encoding utf8 = new UTF8Encoding(true);
            missionWriter = new StreamWriter(missionPath, false, utf8);
            taskWriter = new StreamWriter(taskPath, false, utf8);
            WriteMissionHeader();
            WriteTaskHeader();
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
                record.InternalRateNjPerTick,
                record.DeliveredEnergyJ,
                record.DistanceFromPreviousMeters,
                record.Success,
                record.FailureReason,
                record.WcvEnergyAfterJ));
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            missionWriter.Dispose();
            taskWriter.Dispose();
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
                "ConsumeRateJPerSecond", "InternalRateNjPerTick", "DeliveredEnergyJ", "DistanceFromPreviousMeters",
                "Success", "FailureReason", "WcvEnergyAfterJ"));
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
            }

            return rows;
        }

        private static List<List<object>> BuildSummaryRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("演算法", "Run 次數", "平均生命週期(s)", "最小生命週期(s)", "最大生命週期(s)",
                "平均成功充電", "平均失敗/逾期", "平均request", "平均移動距離(m)", "平均等待(s)",
                "平均封包遺失", "平均routing failed遺失", "平均ParentId=-1節點數", "平均不連通比例", "平均充電效率"));

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
            }

            return rows;
        }

        private static List<List<object>> BuildDeathRows(ExperimentBatchResult result)
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

        private static string AlgorithmDisplayName(string key)
        {
            if (key == "EDF") return "EDF（最早期限優先）";
            if (key == "NJF") return "NJF（最近工作優先）";
            if (key == "TADP_LIN") return "TADP/LIN（時間與距離優先）";
            if (key == "RCSS") return "RCSS（風險與耗能排序）";
            if (key == "NJF_BPR") return "NJF_BPR（ZHENG BP&R deterministic）";
            if (key == "NJF_BPR_ROUTE_SAFE_LIMITED") return "NJF_BPR_ROUTE_SAFE_LIMITED（ZHENG BP&R route-cost，<=NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_EXTENDED") return "NJF_BPR_ROUTE_SAFE_EXTENDED（ZHENG BP&R route-cost，可超過NmaxTask）";
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
            if (key == "NJF") return "NJF（最近工作優先）";
            if (key == "TADP_LIN") return "TADP/LIN（時間與距離優先）";
            if (key == "RCSS") return "RCSS（風險與耗能排序）";
            if (key == "NJF_BPR") return "NJF_BPR（ZHENG BP&R deterministic）";
            if (key == "NJF_BPR_ROUTE_SAFE_LIMITED") return "NJF_BPR_ROUTE_SAFE_LIMITED（ZHENG BP&R route-cost，<=NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_EXTENDED") return "NJF_BPR_ROUTE_SAFE_EXTENDED（ZHENG BP&R route-cost，可超過NmaxTask）";
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
    }
}
