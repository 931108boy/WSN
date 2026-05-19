using System;
using System.Collections.Generic;
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
        internal const int MaxTaskRecordsInWorkbook = 50000;
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

            int totalWork = settings.RunCount * algorithms.Count;
            int completedWork = 0;
            int maxParallelJobs = Math.Max(1, Math.Min(totalWork, Environment.ProcessorCount));
            ExperimentRunBatchResult[] runResults = new ExperimentRunBatchResult[settings.RunCount];
            Report(String.Format(CultureInfo.InvariantCulture,
                "平行批次啟動：runs={0}, algorithms={1}, max parallel jobs={2}",
                settings.RunCount, algorithms.Count, maxParallelJobs));

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

                int taskRecordCaptureLimit = GetTaskRecordCaptureLimit(settings.RunCount, algorithms.Count, runIndex, algorithmIndex);
                ExperimentSimulation simulation = new ExperimentSimulation(settings, runBatch.Artifact, algorithm, taskRecordCaptureLimit);
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
                    int remainingTaskSlots = MaxTaskRecordsInWorkbook - result.TaskRecords.Count;
                    if (remainingTaskSlots > 0)
                    {
                        int copyCount = Math.Min(remainingTaskSlots, run.Tasks.Count);
                        for (int t = 0; t < copyCount; t++)
                            result.TaskRecords.Add(run.Tasks[t]);
                    }
                    if (result.TaskRecords.Count < result.TotalTaskRecordCount)
                        result.TaskRecordsTruncated = true;
                    result.DeathRecords.AddRange(run.Deaths);
                }
            }
        }

        private static int GetTaskRecordCaptureLimit(int runCount, int algorithmCount, int runIndex, int algorithmIndex)
        {
            int totalCells = Math.Max(1, runCount * algorithmCount);
            int ordinal = Math.Max(0, (runIndex - 1) * algorithmCount + algorithmIndex);
            int baseQuota = MaxTaskRecordsInWorkbook / totalCells;
            int remainder = MaxTaskRecordsInWorkbook % totalCells;
            return baseQuota + (ordinal < remainder ? 1 : 0);
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
        public List<ExperimentTaskRecord> TaskRecords { get; private set; }
        public List<ExperimentDeathRecord> DeathRecords { get; private set; }
        public string WorkbookPath { get; set; }
        public int TotalTaskRecordCount { get; set; }
        public bool TaskRecordsTruncated { get; set; }

        public ExperimentBatchResult()
        {
            Artifacts = new List<ExperimentArtifact>();
            RunSummaries = new List<ExperimentRunSummary>();
            TaskRecords = new List<ExperimentTaskRecord>();
            DeathRecords = new List<ExperimentDeathRecord>();
            WorkbookPath = "";
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
            artifact.BaseX = settings.MapWidthMeters / 2.0;
            artifact.BaseY = settings.MapHeightMeters / 2.0;

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

            AssignRoutingParents(settings, artifact);

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

        private static void AssignRoutingParents(ExperimentSettings settings, ExperimentArtifact artifact)
        {
            double[] distanceToBase = new double[artifact.Sensors.Count];
            for (int i = 0; i < artifact.Sensors.Count; i++)
                distanceToBase[i] = Distance(artifact.Sensors[i].X, artifact.Sensors[i].Y, artifact.BaseX, artifact.BaseY);

            for (int i = 1; i < artifact.Sensors.Count; i++)
            {
                SensorTemplate sensor = artifact.Sensors[i];
                if (distanceToBase[i] <= settings.RadioRangeMeters)
                {
                    sensor.ParentId = 0;
                    continue;
                }

                int bestParent = -1;
                double bestDistanceToBase = distanceToBase[i];
                for (int j = 1; j < artifact.Sensors.Count; j++)
                {
                    if (i == j)
                        continue;
                    if (distanceToBase[j] >= distanceToBase[i])
                        continue;
                    double linkDistance = Distance(sensor.X, sensor.Y, artifact.Sensors[j].X, artifact.Sensors[j].Y);
                    if (linkDistance <= settings.RadioRangeMeters && distanceToBase[j] < bestDistanceToBase)
                    {
                        bestDistanceToBase = distanceToBase[j];
                        bestParent = j;
                    }
                }

                sensor.ParentId = bestParent;
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
        private readonly List<ExperimentTaskRecord> tasks;
        private readonly List<ExperimentDeathRecord> deaths;
        private readonly ExperimentRunSummary summary;
        private readonly int taskRecordCaptureLimit;
        private int totalTaskRecordCount;
        private int nextEventIndex;
        private int nextRateChangeIndex;
        private int nextRequestId;
        private int missionId;
        private double currentTime;
        private bool stopForFirstDeath;

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName)
            : this(experimentSettings, sharedArtifact, schedulerName, Int32.MaxValue)
        {
        }

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName, int maxTaskRecordsToKeep)
        {
            settings = experimentSettings;
            artifact = sharedArtifact;
            algorithm = ExperimentSettings.CanonicalAlgorithmKey(schedulerName);
            algorithmRandom = new Random(sharedArtifact.Seed * 397 + StableStringHash(algorithm));
            sensors = new SensorState[artifact.Sensors.Count];
            activeRequests = new List<ChargingRequest>();
            tasks = new List<ExperimentTaskRecord>();
            deaths = new List<ExperimentDeathRecord>();
            nextEventIndex = 0;
            nextRateChangeIndex = 0;
            nextRequestId = 1;
            missionId = 0;
            currentTime = 0.0;
            stopForFirstDeath = false;
            taskRecordCaptureLimit = Math.Max(0, maxTaskRecordsToKeep);
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

                double nextTime = FindNextInterestingTime(settings.SimulationTimeSeconds, null);
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
            result.Tasks = tasks;
            result.TotalTaskRecordCount = totalTaskRecordCount;
            result.Deaths = deaths;
            return result;
        }

        private void ExecuteMission()
        {
            missionId++;
            double dispatchTime = currentTime;
            List<ChargingRequest> route = BuildMissionRoute();
            if (route.Count == 0)
            {
                currentTime = Math.Min(settings.SimulationTimeSeconds, currentTime + 1.0);
                return;
            }

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
                    RecordSkippedTask(request, dispatchTime, currentTime, "節點已死亡");
                    continue;
                }

                double distance = ExperimentArtifact.Distance(posX, posY, sensor.X, sensor.Y);
                double returnDistance = ExperimentArtifact.Distance(sensor.X, sensor.Y, artifact.BaseX, artifact.BaseY);
                double moveEnergy = distance * settings.WcvMoveCostJPerMeter;
                double returnEnergy = returnDistance * settings.WcvMoveCostJPerMeter;
                if (wcvEnergy < moveEnergy + returnEnergy)
                {
                    RecordSkippedTask(request, dispatchTime, currentTime, "WCV 能量不足");
                    summary.FailedOrLateTasks++;
                    break;
                }

                wcvEnergy -= moveEnergy;
                summary.MoveEnergyJ += moveEnergy;
                summary.MovementDistanceMeters += distance;
                AdvanceTo(currentTime + distance / settings.WcvSpeedMetersPerSecond, null);
                if (stopForFirstDeath)
                    break;

                order++;
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

                summary.DeliveredEnergyJ += context.DeliveredEnergyJ;
                if (record.IsProactive)
                    summary.ProactiveTaskCount++;
                if (record.Success)
                {
                    summary.SuccessfulCharges++;
                    summary.TotalWaitSeconds += record.WaitSeconds;
                }
                else if (String.IsNullOrWhiteSpace(failReason))
                {
                    summary.FailedOrLateTasks++;
                }

                CompleteRequestForNode(request.NodeId);
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
                AdvanceTo(currentTime + backDistance / settings.WcvSpeedMetersPerSecond, null);
            }
            else
            {
                summary.FailedOrLateTasks++;
            }
        }

        private void RecordSkippedTask(ChargingRequest request, double dispatchTime, double time, string reason)
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
        }

        private void AddTaskRecord(ExperimentTaskRecord record)
        {
            totalTaskRecordCount++;
            if (tasks.Count < taskRecordCaptureLimit)
                tasks.Add(record);
        }

        private void CompleteRequestForNode(int nodeId)
        {
            for (int i = activeRequests.Count - 1; i >= 0; i--)
            {
                if (activeRequests[i].NodeId == nodeId)
                    activeRequests.RemoveAt(i);
            }
            sensors[nodeId].HasPendingRequest = false;
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

            if (algorithm == "NJF_BPR" || algorithm == "FUZZY")
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
            if (algorithm == "NJF_BPR")
                return BuildNearestRoute(pool, maxTask);
            if (algorithm == "FUZZY")
                return BuildFuzzyRoute(pool, maxTask);
            if (algorithm == "GENE")
                return ImproveRouteByTwoOpt(BuildCompositeRoute(pool, maxTask, 0.45, 0.35, 0.20));
            if (algorithm == "PSO")
                return BuildCompositeRoute(pool, maxTask, 0.35, 0.25, 0.40);
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

        private void AddProactiveCandidates(List<ChargingRequest> pool, int maxTask)
        {
            if (pool.Count >= maxTask)
                return;

            // BP&R-inspired risk selection, not the full ZHENG Algorithm 3 sliding-window bottleneck scan.
            HashSet<int> used = new HashSet<int>();
            for (int i = 0; i < pool.Count; i++)
                used.Add(pool[i].NodeId);

            List<ChargingRequest> candidates = new List<ChargingRequest>();
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive || sensor.HasPendingRequest || used.Contains(id))
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
            if (requiredRequests.Count == 0)
                return requiredRequests;

            List<ChargingRequest> selected = enforceTaskLimit
                ? BuildNearestRoute(requiredRequests, maxTask)
                : new List<ChargingRequest>();
            HashSet<int> used = new HashSet<int>();
            if (!enforceTaskLimit)
            {
                for (int i = 0; i < requiredRequests.Count; i++)
                    selected.Add(requiredRequests[i].Clone());
            }
            for (int i = 0; i < selected.Count; i++)
                used.Add(selected[i].NodeId);

            List<ChargingRequest> currentRoute = BuildNearestRoute(selected, selected.Count);
            List<ChargingRequest> candidates = BuildBprRiskCandidates(used);
            int proactiveAddLimit = enforceTaskLimit ? Math.Max(0, maxTask - selected.Count) : GetRouteSafeProactiveLimit();
            int proactiveAdded = 0;

            while (candidates.Count > 0 && proactiveAdded < proactiveAddLimit &&
                (!enforceTaskLimit || selected.Count < maxTask))
            {
                candidates.Sort(delegate (ChargingRequest a, ChargingRequest b)
                {
                    double da = DistanceToRouteSegments(a.NodeId, currentRoute);
                    double db = DistanceToRouteSegments(b.NodeId, currentRoute);
                    int compare = da.CompareTo(db);
                    if (compare != 0)
                        return compare;
                    compare = BprRiskScore(b).CompareTo(BprRiskScore(a));
                    if (compare != 0)
                        return compare;
                    return a.NodeId.CompareTo(b.NodeId);
                });

                bool accepted = false;
                for (int i = 0; i < candidates.Count; i++)
                {
                    ChargingRequest candidate = candidates[i];
                    List<ChargingRequest> trialPool = new List<ChargingRequest>(selected);
                    trialPool.Add(candidate);
                    List<ChargingRequest> trialRoute = BuildNearestRoute(trialPool, trialPool.Count);

                    candidates.RemoveAt(i);
                    if (!IsTrialRouteSafe(trialRoute))
                    {
                        i--;
                        continue;
                    }

                    selected.Add(candidate);
                    used.Add(candidate.NodeId);
                    proactiveAdded++;
                    currentRoute = BuildNearestRoute(selected, selected.Count);
                    accepted = true;
                    break;
                }

                if (!accepted)
                    break;
            }

            return BuildNearestRoute(selected, enforceTaskLimit ? maxTask : selected.Count);
        }

        private int GetRouteSafeProactiveLimit()
        {
            return Math.Max(20, Math.Min(120, Math.Max(1, sensors.Length - 1) / 10));
        }

        private List<ChargingRequest> BuildBprRiskCandidates(HashSet<int> used)
        {
            // Risk-based proactive shortlist inspired by BP&R terminology. It does not implement
            // ZHENG's sliding future-window bottleneck prediction/removal procedure.
            List<ChargingRequest> candidates = new List<ChargingRequest>();
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive || sensor.HasPendingRequest || used.Contains(id) || sensor.ConsumeRateJPerSecond <= 0.0)
                    continue;

                double threshold = GetRequestThresholdJ(sensor);
                double timeToRequest = Math.Max(0.0, (sensor.EnergyJ - threshold) / sensor.ConsumeRateJPerSecond);
                double timeToDeath = Math.Max(0.0, sensor.EnergyJ / sensor.ConsumeRateJPerSecond);
                double energyRatio = sensor.CapacityJ <= 0.0 ? 0.0 : sensor.EnergyJ / sensor.CapacityJ;
                double density = ComputeCriticalNodeDensity(id);
                double thresholdRatio = sensor.CapacityJ <= 0.0 ? 0.0 : threshold / sensor.CapacityJ;
                bool directlyDangerous = timeToRequest <= settings.TreqSeconds ||
                    timeToDeath <= settings.TreqSeconds * 2.0 ||
                    energyRatio <= Math.Max(0.20, thresholdRatio * 1.8);
                bool bottleneckDangerous = density >= 0.35 && timeToRequest <= settings.TreqSeconds * 2.5;
                bool dangerous = directlyDangerous || bottleneckDangerous;
                if (!dangerous)
                    continue;

                ChargingRequest proactive = new ChargingRequest();
                proactive.RequestId = -id;
                proactive.NodeId = id;
                proactive.RequestTimeSeconds = currentTime;
                proactive.DeadlineSeconds = currentTime + timeToDeath;
                proactive.RequestEnergyJ = sensor.EnergyJ;
                proactive.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
                proactive.IsProactive = true;
                proactive.CriticalDensity = density;
                candidates.Add(proactive);
            }

            candidates.Sort(delegate (ChargingRequest a, ChargingRequest b)
            {
                int compare = BprRiskScore(b).CompareTo(BprRiskScore(a));
                if (compare != 0)
                    return compare;
                return a.DeadlineSeconds.CompareTo(b.DeadlineSeconds);
            });
            int shortlistLimit = Math.Max(GetRouteSafeProactiveLimit() * 3, GetRouteSafeProactiveLimit() + 20);
            if (candidates.Count > shortlistLimit)
                candidates.RemoveRange(shortlistLimit, candidates.Count - shortlistLimit);
            return candidates;
        }

        private bool IsTrialRouteSafe(List<ChargingRequest> route)
        {
            if (route.Count == 0)
                return true;

            double[] energy = new double[sensors.Length];
            for (int id = 1; id < sensors.Length; id++)
                energy[id] = sensors[id].EnergyJ;

            double trialTime = currentTime;
            double wcvEnergy = settings.WcvCapacityJ;
            double x = artifact.BaseX;
            double y = artifact.BaseY;

            for (int i = 0; i < route.Count; i++)
            {
                ChargingRequest request = route[i];
                if (request.NodeId <= 0 || request.NodeId >= sensors.Length || !sensors[request.NodeId].Alive)
                    return false;

                SensorState sensor = sensors[request.NodeId];
                double distance = ExperimentArtifact.Distance(x, y, sensor.X, sensor.Y);
                double returnDistance = ExperimentArtifact.Distance(sensor.X, sensor.Y, artifact.BaseX, artifact.BaseY);
                double moveEnergy = distance * settings.WcvMoveCostJPerMeter;
                double returnEnergy = returnDistance * settings.WcvMoveCostJPerMeter;
                if (wcvEnergy < moveEnergy + returnEnergy)
                    return false;

                wcvEnergy -= moveEnergy;
                if (!ApplyTrialTravel(energy, distance / settings.WcvSpeedMetersPerSecond, ref trialTime))
                    return false;
                if (trialTime > request.DeadlineSeconds + Epsilon || energy[request.NodeId] <= Epsilon)
                    return false;
                if (!ApplyTrialCharge(energy, request.NodeId, ref trialTime, ref wcvEnergy))
                    return false;

                x = sensor.X;
                y = sensor.Y;
            }

            double backDistance = ExperimentArtifact.Distance(x, y, artifact.BaseX, artifact.BaseY);
            double backMoveEnergy = backDistance * settings.WcvMoveCostJPerMeter;
            if (wcvEnergy < backMoveEnergy)
                return false;
            wcvEnergy -= backMoveEnergy;
            return ApplyTrialTravel(energy, backDistance / settings.WcvSpeedMetersPerSecond, ref trialTime);
        }

        private bool ApplyTrialTravel(double[] energy, double deltaSeconds, ref double trialTime)
        {
            if (deltaSeconds <= 0.0)
                return true;

            trialTime += deltaSeconds;
            for (int id = 1; id < sensors.Length; id++)
            {
                if (!sensors[id].Alive)
                    continue;
                energy[id] -= sensors[id].ConsumeRateJPerSecond * deltaSeconds;
                if (energy[id] <= Epsilon)
                    return false;
            }
            return true;
        }

        private bool ApplyTrialCharge(double[] energy, int nodeId, ref double trialTime, ref double wcvEnergy)
        {
            SensorState target = sensors[nodeId];
            int safety = 0;
            while (energy[nodeId] < target.CapacityJ - 1e-6 && wcvEnergy > 1e-9 && safety < 10000)
            {
                safety++;
                double netRate = settings.WcvChargeRateJPerSecond - target.ConsumeRateJPerSecond;
                if (netRate <= 1e-9)
                    return false;

                double timeToFull = (target.CapacityJ - energy[nodeId]) / netRate;
                double timeToEmptyWcv = wcvEnergy / settings.WcvChargeRateJPerSecond;
                double deltaSeconds = Math.Min(timeToFull, timeToEmptyWcv);
                if (deltaSeconds <= 1e-9)
                    return false;

                trialTime += deltaSeconds;
                for (int id = 1; id < sensors.Length; id++)
                {
                    if (!sensors[id].Alive)
                        continue;
                    if (id == nodeId)
                        continue;
                    energy[id] -= sensors[id].ConsumeRateJPerSecond * deltaSeconds;
                    if (energy[id] <= Epsilon)
                        return false;
                }

                energy[nodeId] += netRate * deltaSeconds;
                wcvEnergy -= settings.WcvChargeRateJPerSecond * deltaSeconds;
                if (energy[nodeId] <= Epsilon)
                    return false;
            }

            return energy[nodeId] >= target.CapacityJ - 1e-5;
        }

        private double DistanceToRouteSegments(int nodeId, List<ChargingRequest> route)
        {
            if (route.Count == 0)
                return DistanceFrom(artifact.BaseX, artifact.BaseY, nodeId);

            SensorState target = sensors[nodeId];
            if (route.Count == 1)
                return DistanceFrom(sensors[route[0].NodeId].X, sensors[route[0].NodeId].Y, nodeId);

            double best = Double.MaxValue;
            for (int i = 1; i < route.Count; i++)
            {
                SensorState a = sensors[route[i - 1].NodeId];
                SensorState b = sensors[route[i].NodeId];
                best = Math.Min(best, DistancePointToSegment(target.X, target.Y, a.X, a.Y, b.X, b.Y));
            }
            return best;
        }

        private static double DistancePointToSegment(double px, double py, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax;
            double dy = by - ay;
            double lengthSquared = dx * dx + dy * dy;
            if (lengthSquared <= 1e-9)
                return ExperimentArtifact.Distance(px, py, ax, ay);

            double t = ((px - ax) * dx + (py - ay) * dy) / lengthSquared;
            t = ExperimentSettings.Clamp(t, 0.0, 1.0);
            return ExperimentArtifact.Distance(px, py, ax + t * dx, ay + t * dy);
        }

        private double BprRiskScore(ChargingRequest request)
        {
            SensorState sensor = sensors[request.NodeId];
            double energyRisk = 1.0 - (sensor.CapacityJ <= 0.0 ? 0.0 : sensor.EnergyJ / sensor.CapacityJ);
            double deadlineRisk = 1.0 / Math.Max(1.0, request.DeadlineSeconds - currentTime);
            return energyRisk + request.CriticalDensity + deadlineRisk * settings.TreqSeconds;
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

        private List<ChargingRequest> BuildCuckooRoute(List<ChargingRequest> pool, int maxTask)
        {
            List<ChargingRequest> remaining = TakeSorted(pool, pool.Count, CompareByDeadline);
            List<ChargingRequest> route = new List<ChargingRequest>();
            double x = artifact.BaseX;
            double y = artifact.BaseY;

            while (remaining.Count > 0 && route.Count < maxTask)
            {
                remaining.Sort(delegate (ChargingRequest a, ChargingRequest b)
                {
                    double da = DistanceFrom(x, y, a.NodeId) + Math.Max(0.0, a.DeadlineSeconds - currentTime) * 0.01;
                    double db = DistanceFrom(x, y, b.NodeId) + Math.Max(0.0, b.DeadlineSeconds - currentTime) * 0.01;
                    return da.CompareTo(db);
                });

                int candidateSpan = Math.Max(1, Math.Min(3, remaining.Count));
                int chosenIndex = algorithmRandom.Next(candidateSpan);
                ChargingRequest next = remaining[chosenIndex];
                route.Add(next);
                remaining.RemoveAt(chosenIndex);
                x = sensors[next.NodeId].X;
                y = sensors[next.NodeId].Y;
            }

            return route;
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
                if (!sensor.Alive || sensor.HasPendingRequest)
                    continue;

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
                if (!sensor.Alive || sensor.HasPendingRequest)
                    continue;

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
            return clone;
        }
    }

    public class ExperimentRunResult
    {
        public ExperimentRunSummary Summary { get; set; }
        public List<ExperimentTaskRecord> Tasks { get; set; }
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

    internal static class ExperimentWorkbookWriter
    {
        public static void Write(string path, ExperimentBatchResult result)
        {
            SimpleXlsxWriter writer = new SimpleXlsxWriter();
            writer.AddSheet("參數設定", BuildSettingsRows(result));
            writer.AddSheet("執行比較", BuildRunRows(result));
            writer.AddSheet("彙總統計", BuildSummaryRows(result));
            writer.AddSheet("任務明細", BuildTaskRows(result));
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
            rows.Add(Row("ZHENG-inspired 動態耗能週期(s)", 10000, "每 10000s 檢查一次；延伸實驗，不是原始 ZHENG 重現"));
            rows.Add(Row("ZHENG-inspired 耗能率倍率", RateMultiplierRangeText(s), "由耗能變動幅度決定"));
            rows.Add(Row("基地台", "sink + 充電中心", "固定單台 WCV，每趟 mission 後回 BS"));
            rows.Add(Row("FUZZY", "Mamdani 模糊推論", "剩餘能量、距離、耗能率、臨界節點密度"));
            rows.Add(Row("BP&R 標註", "BP&R-inspired risk-based proactive", "目前未實作 ZHENG Algorithm 3 sliding-window bottleneck removal"));
            rows.Add(Row("GENE/PSO/Cuckoo 標註", "simplified wrapper baselines", "不是完整移植舊版最佳化流程"));
            rows.Add(Row("任務明細總列數", result.TotalTaskRecordCount, result.TaskRecordsTruncated ? "Excel 任務明細過大，已用 deterministic run/algorithm quota 保留部分資料" : "完整輸出"));
            rows.Add(Row("任務明細保留列數", result.TaskRecords.Count, "目前記憶體保護上限 " + ExperimentBatchRunner.MaxTaskRecordsInWorkbook.ToString(CultureInfo.InvariantCulture) + " 列"));

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

        private static List<List<object>> BuildTaskRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("Run", "Seed", "演算法", "Mission", "順序", "節點", "來源", "proactive",
                "request時間(s)", "deadline(s)", "出發時間(s)", "抵達(s)", "等待(s)", "開始充電(s)",
                "結束充電(s)", "充電前(J)", "充電後(J)", "充電前(nJ)", "充電後(nJ)", "耗能率(J/s)",
                "耗能率(nJ/tick)", "充入能量(J)", "前段距離(m)", "成功", "失敗原因", "WCV剩餘能量(J)",
                "共用資料hash"));
            if (result.TaskRecordsTruncated)
            {
                rows.Add(Row("注意", "", "", "", "", "", "", "",
                    "任務明細總列數 " + result.TotalTaskRecordCount.ToString(CultureInfo.InvariantCulture) +
                    " 超過 Excel 記憶體保護上限，本工作表以 run/algorithm 固定配額保留 " +
                    result.TaskRecords.Count.ToString(CultureInfo.InvariantCulture) + " 列；彙總統計仍使用完整模擬結果。",
                    "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""));
            }

            for (int i = 0; i < result.TaskRecords.Count; i++)
            {
                ExperimentTaskRecord t = result.TaskRecords[i];
                rows.Add(Row(t.RunIndex, t.Seed, AlgorithmDisplayName(t.Algorithm), t.MissionId, t.TaskOrder, t.NodeId, t.TaskSource,
                    t.IsProactive ? "是" : "否", t.RequestTimeSeconds, t.DeadlineSeconds, t.DispatchTimeSeconds,
                    t.ArrivalTimeSeconds, t.WaitSeconds, t.ChargeStartSeconds, t.ChargeEndSeconds, t.EnergyBeforeJ,
                    t.EnergyAfterJ, t.InternalEnergyBeforeNj, t.InternalEnergyAfterNj, t.ConsumeRateJPerSecond,
                    t.InternalRateNjPerTick, t.DeliveredEnergyJ, t.DistanceFromPreviousMeters,
                    t.Success ? "是" : "否", t.FailureReason, t.WcvEnergyAfterJ, t.ArtifactHash));
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
            if (key == "NJF_BPR") return "NJF_BPR（BP&R-inspired risk-based proactive）";
            if (key == "NJF_BPR_ROUTE_SAFE_LIMITED") return "NJF_BPR_ROUTE_SAFE_LIMITED（公平版，<=NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_EXTENDED") return "NJF_BPR_ROUTE_SAFE_EXTENDED（延伸版，可超過NmaxTask）";
            if (key == "FUZZY") return "FUZZY（模糊推論排程）";
            if (key == "GENE") return "GENE（簡化 wrapper baseline，非完整舊版 GA）";
            if (key == "PSO") return "PSO（簡化 wrapper baseline，非完整舊版 PSO）";
            if (key == "Cuckoo") return "Cuckoo（簡化 wrapper baseline，非完整舊版 Cuckoo）";
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
            if (key == "NJF_BPR") return "NJF_BPR（BP&R-inspired risk-based proactive）";
            if (key == "NJF_BPR_ROUTE_SAFE_LIMITED") return "NJF_BPR_ROUTE_SAFE_LIMITED（公平版，<=NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_EXTENDED") return "NJF_BPR_ROUTE_SAFE_EXTENDED（延伸版，可超過NmaxTask）";
            if (key == "FUZZY") return "FUZZY（模糊推論排程）";
            if (key == "GENE") return "GENE（簡化 wrapper baseline，非完整舊版 GA）";
            if (key == "PSO") return "PSO（簡化 wrapper baseline，非完整舊版 PSO）";
            if (key == "Cuckoo") return "Cuckoo（簡化 wrapper baseline，非完整舊版 Cuckoo）";
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
