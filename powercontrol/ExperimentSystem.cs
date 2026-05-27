using System;
using System.Collections.Concurrent;
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
    public sealed class MissionDetailCsvOutputOptions
    {
        public bool WriteMissionDetails { get; set; }
        public bool WriteTaskRecords { get; set; }
        public bool WriteBprDebug { get; set; }
        public bool WriteYuBprDebug { get; set; }

        public MissionDetailCsvOutputOptions()
        {
            WriteMissionDetails = true;
            WriteTaskRecords = true;
            WriteBprDebug = true;
            WriteYuBprDebug = true;
        }

        public bool HasAnyOutput()
        {
            return WriteMissionDetails || WriteTaskRecords ||
                WriteBprDebug || WriteYuBprDebug;
        }

        public static MissionDetailCsvOutputOptions All()
        {
            return new MissionDetailCsvOutputOptions();
        }

        public static MissionDetailCsvOutputOptions FromSettings(ExperimentSettings settings)
        {
            MissionDetailCsvOutputOptions options = new MissionDetailCsvOutputOptions();
            if (settings == null)
                return options;

            options.WriteMissionDetails = settings.WriteMissionDetailsCsv;
            options.WriteTaskRecords = settings.WriteTaskRecordsCsv;
            options.WriteBprDebug = settings.WriteBprDebugCsv;
            options.WriteYuBprDebug = settings.WriteYuBprDebugCsv;
            return options;
        }

        public string DescribeSelectedFiles()
        {
            List<string> names = new List<string>();
            if (WriteMissionDetails) names.Add("mission-details.csv");
            if (WriteTaskRecords) names.Add("task-records.csv");
            if (WriteBprDebug) names.Add("bpr-debug.csv");
            if (WriteYuBprDebug) names.Add("yu-bpr-debug.csv");
            return names.Count == 0 ? "none" : String.Join(", ", names.ToArray());
        }
    }

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
        public double CriticalDensityRadiusMeters { get; set; }
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
        // Legacy/debug only: formal YU BP&R uses CenterRequestTimeSeconds + EstimateBprTjobSeconds(maxTask).
        public double YuDangerWindowSeconds { get; set; }
        // Legacy/debug only: formal YU BP&R danger detection is fixed at overlap count > maxTask.
        public int YuDangerThresholdK { get; set; }
        public double YuIntervalUncertaintySeconds { get; set; }
        public double PrateChange { get; set; }
        public double RateChangeVariationPercent { get; set; }
        public string SelectedAlgorithmsCsv { get; set; }
        public string OutputDirectory { get; set; }
        public string LastOutputWorkbookPath { get; set; }
        public bool WriteTaskDetailCsv { get; set; }
        public bool WriteMissionDetailsCsv { get; set; }
        public bool WriteTaskRecordsCsv { get; set; }
        public bool WriteBprDebugCsv { get; set; }
        public bool WriteYuBprDebugCsv { get; set; }
        public bool UseFastSimulationScheduling { get; set; }
        public int MaxParallelJobs { get; set; }
        public bool SweepEnabled { get; set; }
        public string SweepParameterKey { get; set; }
        public int SweepIterationCount { get; set; }
        public double SweepStepValue { get; set; }

        [XmlIgnore]
        public int CurrentSweepIndex { get; set; }

        [XmlIgnore]
        public double CurrentSweepValue { get; set; }

        public ExperimentSettings()
        {
            ProjectRoot = ResolveProjectRoot();
            BaseSeed = 42;
            RunCount = 1;
            SensorCount = 2000;
            MapWidthMeters = 500.0;
            MapHeightMeters = 500.0;
            SimulationTimeSeconds = 500000.0;
            InitialEnergyJ = 100.0;
            SensorBackgroundLifetimeSeconds = 100000.0;
            InitialResidualJitterPercent = 0.0;
            EventRatePerSecond = 0.02;
            CriticalDensityRadiusMeters = 90.0;
            WcvSpeedMetersPerSecond = 5.0;
            WcvChargeRateJPerSecond = 5.0;
            WcvCapacityJ = 200000.0;
            WcvMoveCostJPerMeter = 10.0;
            NmaxTask = 30;
            DynamicNmaxTask = false;
            ThresholdMode = "ChengTreq";
            RequestThresholdPercent = 10.0;
            TreqSeconds = 2900.0;
            BprDeadlineThresholdSeconds = 2900.0;
            AllowStandaloneProactiveDispatch = false;
            ProactivePredictionHorizonSeconds = 0.0;
            ProactiveCandidateMaxEnergyRatio = 0.95;
            ProactiveCooldownSeconds = 0.0;
            YuDangerWindowSeconds = 0.0;
            YuDangerThresholdK = 0;
            YuIntervalUncertaintySeconds = 0.0;
            PrateChange = 0.0;
            RateChangeVariationPercent = 12.5;
            SelectedAlgorithmsCsv = DefaultAlgorithmSelectionCsv();
            OutputDirectory = Path.Combine(ProjectRoot, "outputs");
            LastOutputWorkbookPath = "";
            WriteTaskDetailCsv = true;
            WriteMissionDetailsCsv = true;
            WriteTaskRecordsCsv = true;
            WriteBprDebugCsv = true;
            WriteYuBprDebugCsv = true;
            UseFastSimulationScheduling = true;
            MaxParallelJobs = 0;
            SweepEnabled = false;
            SweepParameterKey = "EventRatePerSecond";
            SweepIterationCount = 4;
            SweepStepValue = 0.005;
            CurrentSweepIndex = 0;
            CurrentSweepValue = 0.0;
        }

        public static string[] AllAlgorithms()
        {
            return new string[] {
                "EDF",
                "NJF",
                "TADP_LIN",
                "NJF_CHENG_BPR",
                "TADP_CHENG_BPR",
                "EDF_CHENG_BPR",
                "NJF_YU_BPR",
                "NJF_ROUTE_CHENG_BPR_LIMITED",
                "NJF_ROUTE_CHENG_BPR_EXTENDED",
                "NJF_ROUTE_YU_BPR_LIMITED",
                "NJF_ROUTE_YU_BPR_EXTENDED"
            };
        }

        public static string DefaultAlgorithmSelectionCsv()
        {
            return "EDF,NJF,TADP_LIN,NJF_CHENG_BPR,TADP_CHENG_BPR,EDF_CHENG_BPR,NJF_YU_BPR,NJF_ROUTE_CHENG_BPR_LIMITED,NJF_ROUTE_CHENG_BPR_EXTENDED,NJF_ROUTE_YU_BPR_LIMITED,NJF_ROUTE_YU_BPR_EXTENDED";
        }

        public static string CanonicalAlgorithmKey(string algorithm)
        {
            return NormalizeAlgorithmKey(algorithm);
        }

        public static string NormalizeAlgorithmKey(string algorithm)
        {
            if (String.IsNullOrWhiteSpace(algorithm))
                return "";

            string key = algorithm.Trim();
            if (String.Equals(key, "NJF_BPR", StringComparison.OrdinalIgnoreCase))
                return "NJF_CHENG_BPR";
            if (String.Equals(key, "TADP_BPR", StringComparison.OrdinalIgnoreCase))
                return "TADP_CHENG_BPR";
            if (String.Equals(key, "EDF_BPR", StringComparison.OrdinalIgnoreCase))
                return "EDF_CHENG_BPR";
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE_LIMITED", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_CHENG_BPR_LIMITED";
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE_EXTENDED", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_CHENG_BPR_EXTENDED";
            if (String.Equals(key, "NJF_BPR_ROUTE_SAFE", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_CHENG_BPR_EXTENDED";
            if (String.Equals(key, "NJF_ROUTE_ZHENG_BPR_LIMITED", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_CHENG_BPR_LIMITED";
            if (String.Equals(key, "NJF_ROUTE_ZHENG_BPR_EXTENDED", StringComparison.OrdinalIgnoreCase))
                return "NJF_ROUTE_CHENG_BPR_EXTENDED";
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
            settings.SelectedAlgorithmsCsv = "EDF,NJF";
            settings.OutputDirectory = Path.Combine(settings.ProjectRoot, "outputs");
            settings.Normalize();
            return settings;
        }

        public ExperimentSettings Copy()
        {
            return (ExperimentSettings)MemberwiseClone();
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
            ExperimentSettings settings = LoadRaw(path);
            settings.Normalize();
            return settings;
        }

        public static ExperimentSettings LoadRaw(string path)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ExperimentSettings));
            using (FileStream stream = File.OpenRead(path))
            {
                return (ExperimentSettings)serializer.Deserialize(stream);
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
            CriticalDensityRadiusMeters = Math.Max(1.0, CriticalDensityRadiusMeters);
            if (WcvSpeedMetersPerSecond <= 0.0)
                throw new InvalidOperationException("目前設定不可行：WCV 移動速度必須大於 0。");
            if (WcvChargeRateJPerSecond <= 0.0)
                throw new InvalidOperationException("目前設定不可行：WCV 充電速率必須大於 0。");
            WcvCapacityJ = Math.Max(0.001, WcvCapacityJ);
            WcvMoveCostJPerMeter = Math.Max(0.0, WcvMoveCostJPerMeter);
            NmaxTask = Math.Max(1, NmaxTask);
            if (String.IsNullOrWhiteSpace(ThresholdMode))
                ThresholdMode = "Percent";
            if (String.Equals(ThresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase))
                ThresholdMode = "TreqSeconds";
            else if (String.Equals(ThresholdMode, "ChengTreq", StringComparison.OrdinalIgnoreCase))
                ThresholdMode = "ChengTreq";
            else
                ThresholdMode = "Percent";
            RequestThresholdPercent = Clamp(RequestThresholdPercent, 1.0, 90.0);
            TreqSeconds = Math.Max(1.0, TreqSeconds);
            BprDeadlineThresholdSeconds = Math.Max(1.0, BprDeadlineThresholdSeconds);
            if (ChengTreqCalculator.IsChengTreqMode(ThresholdMode))
            {
                double autoTreq = ComputeAutoTreqSeconds();
                TreqSeconds = autoTreq;
                BprDeadlineThresholdSeconds = autoTreq;
            }
            ProactivePredictionHorizonSeconds = Math.Max(0.0, ProactivePredictionHorizonSeconds);
            ProactiveCandidateMaxEnergyRatio = Clamp(ProactiveCandidateMaxEnergyRatio, 0.1, 1.0);
            ProactiveCooldownSeconds = Math.Max(0.0, ProactiveCooldownSeconds);
            YuDangerWindowSeconds = Math.Max(0.0, YuDangerWindowSeconds);
            YuDangerThresholdK = Math.Max(0, YuDangerThresholdK);
            YuIntervalUncertaintySeconds = Math.Max(0.0, YuIntervalUncertaintySeconds);
            PrateChange = Clamp(PrateChange, 0.0, 1.0);
            RateChangeVariationPercent = Clamp(RateChangeVariationPercent, 0.0, 99.0);
            if (!WriteTaskDetailCsv)
            {
                WriteMissionDetailsCsv = false;
                WriteTaskRecordsCsv = false;
                WriteBprDebugCsv = false;
                WriteYuBprDebugCsv = false;
            }
            WriteTaskDetailCsv = HasAnyTaskDetailCsvOutput();
            MaxParallelJobs = Math.Max(0, MaxParallelJobs);
            if (String.IsNullOrWhiteSpace(SweepParameterKey) || ExperimentSweepParameterCatalog.Find(SweepParameterKey) == null)
                SweepParameterKey = "SensorCount";
            SweepIterationCount = Math.Max(0, SweepIterationCount);
            if (SweepStepValue == 0.0)
                SweepStepValue = ExperimentSweepParameterCatalog.Find(SweepParameterKey).IntegerOnly ? 1.0 : 0.1;
            if (ExperimentSweepParameterCatalog.Find(SweepParameterKey).IntegerOnly)
                SweepStepValue = Math.Round(SweepStepValue);
            CurrentSweepIndex = Math.Max(0, CurrentSweepIndex);
            List<string> selectedAlgorithms = GetSelectedAlgorithms();
            if (selectedAlgorithms.Count == 0)
                SelectedAlgorithmsCsv = DefaultAlgorithmSelectionCsv();
            else
                SelectedAlgorithmsCsv = String.Join(",", selectedAlgorithms.ToArray());
            ClearStaleLastOutputWorkbookPath();
        }

        public bool HasAnyTaskDetailCsvOutput()
        {
            return WriteMissionDetailsCsv || WriteTaskRecordsCsv ||
                WriteBprDebugCsv || WriteYuBprDebugCsv;
        }

        public MissionDetailCsvOutputOptions CreateMissionDetailCsvOutputOptions()
        {
            return MissionDetailCsvOutputOptions.FromSettings(this);
        }

        public double ComputeAutoTreqSeconds()
        {
            return ChengTreqCalculator.Compute(this, NmaxTask).TreqSeconds;
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
            string executionDirectory = CreateExecutionOutputDirectory();
            return CreateOutputWorkbookPath(executionDirectory);
        }

        public string CreateOutputWorkbookPath(string executionDirectory)
        {
            string directory = String.IsNullOrWhiteSpace(executionDirectory) ? OutputDirectory : executionDirectory;
            Directory.CreateDirectory(directory);
            return Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "comparison-seed{0}-runs{1}.xlsx", BaseSeed, RunCount));
        }

        public string CreateExecutionOutputDirectory()
        {
            Normalize();
            Directory.CreateDirectory(OutputDirectory);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
            string name;
            if (SweepEnabled)
            {
                string safeSweepKey = MissionDetailCsvWriter.SanitizeFileNameForPath(SweepParameterKey);
                int sweepValueCount = Math.Max(1, SweepIterationCount + 1);
                string startValue = FormatPathNumber(GetSweepStartValue());
                string stepValue = FormatPathNumber(SweepStepValue);
                name = String.Format(CultureInfo.InvariantCulture,
                    "{0}-sweep{1}-{2}-start{3}-step{4}-seed{5}-runs{6}",
                    timestamp, sweepValueCount, safeSweepKey, startValue, stepValue, BaseSeed, RunCount);
            }
            else
            {
                name = String.Format(CultureInfo.InvariantCulture,
                    "{0}-comparison-seed{1}-runs{2}", timestamp, BaseSeed, RunCount);
            }
            return CreateUniqueDirectory(OutputDirectory, name);
        }

        private double GetSweepStartValue()
        {
            ExperimentSweepParameterDefinition definition = ExperimentSweepParameterCatalog.Find(SweepParameterKey);
            return definition == null ? 0.0 : definition.GetValue(this);
        }

        private static string FormatPathNumber(double value)
        {
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }

        private static string CreateUniqueDirectory(string parentDirectory, string baseName)
        {
            string safeBaseName = MissionDetailCsvWriter.SanitizeFileNameForPath(baseName);
            for (int i = 0; i < 1000; i++)
            {
                string suffix = i == 0 ? "" : "-" + i.ToString("D3", CultureInfo.InvariantCulture);
                string candidate = Path.Combine(parentDirectory, safeBaseName + suffix);
                if (Directory.Exists(candidate))
                    continue;
                Directory.CreateDirectory(candidate);
                return candidate;
            }
            throw new IOException("Unable to create a unique output directory under " + parentDirectory);
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

    internal sealed class ExperimentSweepParameterDefinition
    {
        public readonly string Key;
        public readonly string DisplayName;
        public readonly bool IntegerOnly;
        private readonly Func<ExperimentSettings, double> getter;
        private readonly Action<ExperimentSettings, double> setter;

        public ExperimentSweepParameterDefinition(string key, string displayName, bool integerOnly,
            Func<ExperimentSettings, double> valueGetter, Action<ExperimentSettings, double> valueSetter)
        {
            Key = key;
            DisplayName = displayName;
            IntegerOnly = integerOnly;
            getter = valueGetter;
            setter = valueSetter;
        }

        public double GetValue(ExperimentSettings settings)
        {
            return getter(settings);
        }

        public void SetValue(ExperimentSettings settings, double value)
        {
            if (IntegerOnly)
                value = Math.Round(value);
            setter(settings, value);
        }

        public string FormatValue(double value)
        {
            if (IntegerOnly)
                return ((int)Math.Round(value)).ToString(CultureInfo.InvariantCulture);
            return value.ToString("0.########", CultureInfo.InvariantCulture);
        }
    }

    internal static class ExperimentSweepParameterCatalog
    {
        private static readonly ExperimentSweepParameterDefinition[] definitions = new ExperimentSweepParameterDefinition[]
        {
            new ExperimentSweepParameterDefinition("SensorCount", "感測器數量", true,
                delegate(ExperimentSettings s) { return s.SensorCount; },
                delegate(ExperimentSettings s, double v) { s.SensorCount = (int)Math.Round(v); }),
            new ExperimentSweepParameterDefinition("MapSizeMeters", "地圖邊長(m)", false,
                delegate(ExperimentSettings s) { return s.MapWidthMeters; },
                delegate(ExperimentSettings s, double v) { s.MapWidthMeters = v; s.MapHeightMeters = v; }),
            new ExperimentSweepParameterDefinition("SimulationTimeSeconds", "模擬時間(s)", false,
                delegate(ExperimentSettings s) { return s.SimulationTimeSeconds; },
                delegate(ExperimentSettings s, double v) { s.SimulationTimeSeconds = v; }),
            new ExperimentSweepParameterDefinition("InitialEnergyJ", "初始能量(J)", false,
                delegate(ExperimentSettings s) { return s.InitialEnergyJ; },
                delegate(ExperimentSettings s, double v) { s.InitialEnergyJ = v; }),
            new ExperimentSweepParameterDefinition("SensorBackgroundLifetimeSeconds", "背景壽命(s)", false,
                delegate(ExperimentSettings s) { return s.SensorBackgroundLifetimeSeconds; },
                delegate(ExperimentSettings s, double v) { s.SensorBackgroundLifetimeSeconds = v; }),
            new ExperimentSweepParameterDefinition("EventRatePerSecond", "需求頻率 p(次/s)", false,
                delegate(ExperimentSettings s) { return s.EventRatePerSecond; },
                delegate(ExperimentSettings s, double v) { s.EventRatePerSecond = v; }),
            new ExperimentSweepParameterDefinition("PrateChange", "耗能變動機率", false,
                delegate(ExperimentSettings s) { return s.PrateChange; },
                delegate(ExperimentSettings s, double v) { s.PrateChange = v; }),
            new ExperimentSweepParameterDefinition("RateChangeVariationPercent", "變動幅度(%)", false,
                delegate(ExperimentSettings s) { return s.RateChangeVariationPercent; },
                delegate(ExperimentSettings s, double v) { s.RateChangeVariationPercent = v; }),
            new ExperimentSweepParameterDefinition("RequestThresholdPercent", "需求門檻(%)", false,
                delegate(ExperimentSettings s) { return s.RequestThresholdPercent; },
                delegate(ExperimentSettings s, double v) { s.RequestThresholdPercent = v; }),
            new ExperimentSweepParameterDefinition("TreqSeconds", "Treq 秒數", false,
                delegate(ExperimentSettings s) { return s.TreqSeconds; },
                delegate(ExperimentSettings s, double v) { s.TreqSeconds = v; }),
            new ExperimentSweepParameterDefinition("WcvSpeedMetersPerSecond", "WCV 速度(m/s)", false,
                delegate(ExperimentSettings s) { return s.WcvSpeedMetersPerSecond; },
                delegate(ExperimentSettings s, double v) { s.WcvSpeedMetersPerSecond = v; }),
            new ExperimentSweepParameterDefinition("WcvChargeRateJPerSecond", "充電速率(J/s)", false,
                delegate(ExperimentSettings s) { return s.WcvChargeRateJPerSecond; },
                delegate(ExperimentSettings s, double v) { s.WcvChargeRateJPerSecond = v; }),
            new ExperimentSweepParameterDefinition("WcvCapacityJ", "WCV 容量(J)", false,
                delegate(ExperimentSettings s) { return s.WcvCapacityJ; },
                delegate(ExperimentSettings s, double v) { s.WcvCapacityJ = v; }),
            new ExperimentSweepParameterDefinition("WcvMoveCostJPerMeter", "移動耗能(J/m)", false,
                delegate(ExperimentSettings s) { return s.WcvMoveCostJPerMeter; },
                delegate(ExperimentSettings s, double v) { s.WcvMoveCostJPerMeter = v; }),
            new ExperimentSweepParameterDefinition("NmaxTask", "任務上限", true,
                delegate(ExperimentSettings s) { return s.NmaxTask; },
                delegate(ExperimentSettings s, double v) { s.NmaxTask = (int)Math.Round(v); })
        };

        public static ExperimentSweepParameterDefinition[] All()
        {
            return definitions;
        }

        public static ExperimentSweepParameterDefinition Find(string key)
        {
            if (String.IsNullOrWhiteSpace(key))
                return null;
            for (int i = 0; i < definitions.Length; i++)
            {
                if (String.Equals(definitions[i].Key, key, StringComparison.OrdinalIgnoreCase))
                    return definitions[i];
            }
            return null;
        }

        public static string DisplayName(string key)
        {
            ExperimentSweepParameterDefinition definition = Find(key);
            return definition == null ? "" : definition.DisplayName;
        }
    }

    internal sealed class ExperimentSweepStep
    {
        public int Index;
        public string ParameterKey;
        public string ParameterName;
        public double Value;
        public ExperimentSettings Settings;
    }

    internal sealed class ChengTreqMetrics
    {
        public int MaxTask;
        public double SensorFullChargeSeconds;
        public double LpathMeters;
        public double TjobSeconds;
        public double LmaxStepMeters;
        public double TreqSeconds;
    }

    internal static class ChengTreqCalculator
    {
        public static bool IsChengTreqMode(string thresholdMode)
        {
            return String.Equals(thresholdMode, "ChengTreq", StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsTimeThresholdMode(string thresholdMode)
        {
            return String.Equals(thresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase) ||
                IsChengTreqMode(thresholdMode);
        }

        public static bool IsTreqSecondsMode(string thresholdMode)
        {
            return String.Equals(thresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase);
        }

        public static double GetEffectiveRequestThresholdSeconds(ExperimentSettings settings, int maxTask)
        {
            if (settings == null)
                return 0.0;
            if (IsChengTreqMode(settings.ThresholdMode))
                return Compute(settings, maxTask).TreqSeconds;
            if (IsTreqSecondsMode(settings.ThresholdMode))
                return Math.Max(0.0, settings.TreqSeconds);
            return 0.0;
        }

        public static double GetEffectiveTreqSeconds(ExperimentSettings settings, int maxTask)
        {
            return GetEffectiveRequestThresholdSeconds(settings, maxTask);
        }

        public static ChengTreqMetrics Compute(ExperimentSettings settings, int maxTask)
        {
            if (settings == null)
                throw new InvalidOperationException("ExperimentSettings is required for CHENG Treq calculation.");
            if (settings.WcvSpeedMetersPerSecond <= 0.0)
                throw new InvalidOperationException("目前設定不可行：WCV 移動速度必須大於 0。");
            if (settings.WcvChargeRateJPerSecond <= 0.0)
                throw new InvalidOperationException("目前設定不可行：WCV 充電速率必須大於 0。");

            int k = Math.Max(1, maxTask);
            double width = Math.Max(1.0, settings.MapWidthMeters);
            double height = Math.Max(1.0, settings.MapHeightMeters);
            double side = Math.Max(width, height);
            double lmaxStep = Math.Sqrt(width * width + height * height);
            double sensorFullChargeSeconds = Math.Max(0.0, settings.InitialEnergyJ) /
                settings.WcvChargeRateJPerSecond;

            double lpath;
            double tjob;
            double treq;
            if (k <= 1)
            {
                lpath = 2.0 * lmaxStep;
                tjob = lpath / settings.WcvSpeedMetersPerSecond + sensorFullChargeSeconds;
                treq = tjob;
            }
            else
            {
                double denominator = Math.Sqrt(k) - 1.0;
                if (denominator <= 1e-12)
                    lpath = 2.0 * lmaxStep;
                else
                    lpath = ((k - 1) * side) / denominator + 2.0 * Math.Sqrt(2.0) * side;

                tjob = lpath / settings.WcvSpeedMetersPerSecond + k * sensorFullChargeSeconds;
                treq = tjob +
                    (lpath - lmaxStep) / settings.WcvSpeedMetersPerSecond +
                    (k - 1) * sensorFullChargeSeconds;
            }

            ChengTreqMetrics metrics = new ChengTreqMetrics();
            metrics.MaxTask = k;
            metrics.SensorFullChargeSeconds = sensorFullChargeSeconds;
            metrics.LpathMeters = lpath;
            metrics.TjobSeconds = Math.Max(0.0, tjob);
            metrics.LmaxStepMeters = lmaxStep;
            metrics.TreqSeconds = Math.Max(0.0, treq);
            return metrics;
        }
    }

    internal sealed class WcvMaxTaskFeasibilityResult
    {
        public bool IsValid;
        public string Algorithm;
        public int ValidationTaskLimit;
        public string ErrorMessage;
        public double EstimatedMaxTaskMissionPathLengthMeters;
        public double EstimatedMaxTaskMoveEnergyJ;
        public double EstimatedMaxTaskChargeEnergyJ;
        public double EstimatedMaxTaskMissionEnergyJ;
        public double EstimatedFullChargeSeconds;
        public double EstimatedMaxTaskMoveSeconds;
        public double EstimatedMaxTaskChargeSeconds;
        public double EstimatedMaxTaskMissionSeconds;
    }

    internal static class WcvMaxTaskFeasibilityValidator
    {
        public static double EstimateMaxTaskMissionPathLengthMeters(ExperimentSettings settings)
        {
            return EstimateMaxTaskMissionPathLengthMeters(settings, settings == null ? 0 : settings.NmaxTask);
        }

        public static double EstimateMaxTaskMissionPathLengthMeters(ExperimentSettings settings, int validationTaskLimit)
        {
            if (settings == null)
                return 0.0;
            return ChengTreqCalculator.Compute(settings, validationTaskLimit).LpathMeters;
        }

        public static double EstimateMaxTaskMissionEnergyJ(ExperimentSettings settings)
        {
            WcvMaxTaskFeasibilityResult result = ValidateWcvCapacityForMaxTask(settings);
            return result.EstimatedMaxTaskMissionEnergyJ;
        }

        public static double EstimateMaxTaskMissionSeconds(ExperimentSettings settings)
        {
            WcvMaxTaskFeasibilityResult result = ValidateWcvCapacityForMaxTask(settings);
            return result.EstimatedMaxTaskMissionSeconds;
        }

        public static WcvMaxTaskFeasibilityResult ValidateWcvCapacityForMaxTask(ExperimentSettings settings)
        {
            return ValidateWcvCapacityForAlgorithm(settings, "");
        }

        public static WcvMaxTaskFeasibilityResult ValidateWcvCapacityForAlgorithm(ExperimentSettings settings, string algorithm)
        {
            WcvMaxTaskFeasibilityResult result = new WcvMaxTaskFeasibilityResult();
            result.IsValid = false;
            result.Algorithm = String.IsNullOrWhiteSpace(algorithm) ? "" : ExperimentSettings.CanonicalAlgorithmKey(algorithm);

            if (settings == null)
            {
                result.ErrorMessage = "目前設定不可行：缺少實驗設定。";
                return result;
            }

            if (settings.NmaxTask <= 0)
                return Fail(result, settings, "目前設定不可行：NmaxTask 必須大於 0。");
            if (settings.WcvCapacityJ <= 0.0)
                return Fail(result, settings, "目前設定不可行：WCV 容量必須大於 0。");
            if (settings.MapWidthMeters <= 0.0 || settings.MapHeightMeters <= 0.0)
                return Fail(result, settings, "目前設定不可行：地圖邊長必須大於 0。");
            if (settings.InitialEnergyJ <= 0.0)
                return Fail(result, settings, "目前設定不可行：Sensor 滿電容量必須大於 0。");
            if (settings.WcvChargeRateJPerSecond <= 0.0)
                return Fail(result, settings, "目前設定不可行：WCV 充電速率必須大於 0。");
            if (settings.WcvSpeedMetersPerSecond <= 0.0)
                return Fail(result, settings, "目前設定不可行：WCV 移動速度必須大於 0。");
            if (settings.WcvMoveCostJPerMeter < 0.0)
                return Fail(result, settings, "目前設定不可行：WCV 移動耗能必須大於或等於 0。");

            result.ValidationTaskLimit = settings.NmaxTask;

            FillEstimates(result, settings);
            if (result.EstimatedMaxTaskMissionEnergyJ > settings.WcvCapacityJ)
            {
                result.ErrorMessage = BuildErrorMessage(settings, result,
                    "目前設定不可行：WCV 容量不足以完成一趟最大任務數。");
                return result;
            }

            result.IsValid = true;
            result.ErrorMessage = "";
            return result;
        }

        public static void ThrowIfInvalid(ExperimentSettings settings)
        {
            List<WcvMaxTaskFeasibilityResult> results = ValidateSelectedAlgorithms(settings);
            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i].IsValid)
                    throw new InvalidOperationException(results[i].ErrorMessage);
            }
        }

        public static List<WcvMaxTaskFeasibilityResult> ValidateSelectedAlgorithms(ExperimentSettings settings)
        {
            List<WcvMaxTaskFeasibilityResult> results = new List<WcvMaxTaskFeasibilityResult>();
            if (settings == null)
            {
                results.Add(ValidateWcvCapacityForAlgorithm(settings, ""));
                return results;
            }

            List<string> algorithms = settings.GetSelectedAlgorithms();
            if (algorithms.Count == 0)
                algorithms.Add("");
            for (int i = 0; i < algorithms.Count; i++)
                results.Add(ValidateWcvCapacityForAlgorithm(settings, algorithms[i]));
            return results;
        }

        private static WcvMaxTaskFeasibilityResult Fail(
            WcvMaxTaskFeasibilityResult result,
            ExperimentSettings settings,
            string reason)
        {
            if (settings != null &&
                settings.NmaxTask > 0 &&
                settings.MapWidthMeters > 0.0 &&
                settings.MapHeightMeters > 0.0 &&
                settings.InitialEnergyJ > 0.0 &&
                settings.WcvChargeRateJPerSecond > 0.0 &&
                settings.WcvSpeedMetersPerSecond > 0.0 &&
                settings.WcvMoveCostJPerMeter >= 0.0)
            {
                if (result.ValidationTaskLimit <= 0)
                    result.ValidationTaskLimit = settings.NmaxTask;
                FillEstimates(result, settings);
            }
            result.ErrorMessage = BuildErrorMessage(settings, result, reason);
            return result;
        }

        private static void FillEstimates(WcvMaxTaskFeasibilityResult result, ExperimentSettings settings)
        {
            int maxTask = Math.Max(1, result.ValidationTaskLimit <= 0 ? settings.NmaxTask : result.ValidationTaskLimit);
            result.ValidationTaskLimit = maxTask;
            result.EstimatedFullChargeSeconds = settings.InitialEnergyJ / settings.WcvChargeRateJPerSecond;
            result.EstimatedMaxTaskChargeEnergyJ = maxTask * settings.InitialEnergyJ;
            result.EstimatedMaxTaskMissionPathLengthMeters = EstimateMaxTaskMissionPathLengthMeters(settings, maxTask);
            result.EstimatedMaxTaskMoveEnergyJ =
                result.EstimatedMaxTaskMissionPathLengthMeters * settings.WcvMoveCostJPerMeter;
            result.EstimatedMaxTaskMissionEnergyJ =
                result.EstimatedMaxTaskMoveEnergyJ + result.EstimatedMaxTaskChargeEnergyJ;
            result.EstimatedMaxTaskMoveSeconds =
                result.EstimatedMaxTaskMissionPathLengthMeters / settings.WcvSpeedMetersPerSecond;
            result.EstimatedMaxTaskChargeSeconds = maxTask * result.EstimatedFullChargeSeconds;
            result.EstimatedMaxTaskMissionSeconds =
                result.EstimatedMaxTaskMoveSeconds + result.EstimatedMaxTaskChargeSeconds;
        }

        private static string BuildErrorMessage(
            ExperimentSettings settings,
            WcvMaxTaskFeasibilityResult result,
            string reason)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(reason);
            builder.AppendLine();
            if (settings != null)
            {
                if (!String.IsNullOrWhiteSpace(result.Algorithm))
                    builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "演算法：{0}", result.Algorithm));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "ValidationTaskLimit：{0}", result.ValidationTaskLimit));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "NmaxTask：{0}", settings.NmaxTask));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "WCV 容量：{0} J", resultFormat(settings.WcvCapacityJ)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "估計一趟最大任務能耗：{0} J", resultFormat(result.EstimatedMaxTaskMissionEnergyJ)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "估計路徑長度：{0} m", resultFormat(result.EstimatedMaxTaskMissionPathLengthMeters)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "估計移動能耗：{0} J", resultFormat(result.EstimatedMaxTaskMoveEnergyJ)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "估計充電能量：{0} J", resultFormat(result.EstimatedMaxTaskChargeEnergyJ)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "Sensor 滿電容量：{0} J", resultFormat(settings.InitialEnergyJ)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "WCV 移動耗能：{0} J/m", resultFormat(settings.WcvMoveCostJPerMeter)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "WCV 充電速率：{0} J/s", resultFormat(settings.WcvChargeRateJPerSecond)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "WCV 移動速度：{0} m/s", resultFormat(settings.WcvSpeedMetersPerSecond)));
                builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "地圖邊長：{0} m", resultFormat(Math.Max(settings.MapWidthMeters, settings.MapHeightMeters))));
            }
            builder.AppendLine();
            builder.AppendLine("請降低 NmaxTask，或提高 WCV 容量，或檢查地圖大小、移動耗能、sensor 滿電容量與充電速率設定。");
            return builder.ToString().TrimEnd();
        }

        private static string resultFormat(double value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }

    internal sealed class BprTimingValidationResult
    {
        public bool IsValid;
        public string Algorithm;
        public string ErrorMessage;
    }

    internal static class BprTimingValidator
    {
        public static List<BprTimingValidationResult> ValidateSelectedAlgorithms(ExperimentSettings settings)
        {
            List<BprTimingValidationResult> results = new List<BprTimingValidationResult>();
            if (settings == null)
            {
                BprTimingValidationResult missing = new BprTimingValidationResult();
                missing.IsValid = false;
                missing.Algorithm = "";
                missing.ErrorMessage = "目前設定不可行：缺少實驗設定。";
                results.Add(missing);
                return results;
            }

            List<string> algorithms = settings.GetSelectedAlgorithms();
            if (algorithms.Count == 0)
                algorithms.Add("");
            for (int i = 0; i < algorithms.Count; i++)
                results.Add(ValidateAlgorithm(settings, algorithms[i]));
            return results;
        }

        public static void ThrowIfInvalid(ExperimentSettings settings)
        {
            List<BprTimingValidationResult> results = ValidateSelectedAlgorithms(settings);
            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i].IsValid)
                    throw new InvalidOperationException(results[i].ErrorMessage);
            }
        }

        public static BprTimingValidationResult ValidateAlgorithm(ExperimentSettings settings, string algorithm)
        {
            BprTimingValidationResult result = new BprTimingValidationResult();
            result.Algorithm = String.IsNullOrWhiteSpace(algorithm) ? "" : ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            result.IsValid = true;
            result.ErrorMessage = "";

            if (settings == null)
            {
                result.IsValid = false;
                result.ErrorMessage = "目前設定不可行：缺少實驗設定。";
                return result;
            }

            if (!IsBprAlgorithm(result.Algorithm))
                return result;
            if (!String.Equals(settings.ThresholdMode, "Percent", StringComparison.OrdinalIgnoreCase))
                return result;
            if (settings.ProactivePredictionHorizonSeconds > 0.0 &&
                settings.ProactiveCooldownSeconds > 0.0 &&
                settings.BprDeadlineThresholdSeconds > 0.0)
                return result;

            result.IsValid = false;
            result.ErrorMessage = BuildPercentModeErrorMessage(settings, result.Algorithm);
            return result;
        }

        public static bool IsBprAlgorithm(string algorithm)
        {
            string key = ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            return String.Equals(key, "NJF_CHENG_BPR", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "TADP_CHENG_BPR", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "EDF_CHENG_BPR", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "NJF_YU_BPR", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "NJF_ROUTE_CHENG_BPR_LIMITED", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "NJF_ROUTE_CHENG_BPR_EXTENDED", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "NJF_ROUTE_YU_BPR_LIMITED", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(key, "NJF_ROUTE_YU_BPR_EXTENDED", StringComparison.OrdinalIgnoreCase);
        }

        public static string ResolvePredictionHorizonSource(ExperimentSettings settings)
        {
            if (settings == null)
                return "InvalidPercentMode";
            if (settings.ProactivePredictionHorizonSeconds > 0.0)
                return "Explicit";
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode))
                return "ChengTreq";
            if (ChengTreqCalculator.IsTreqSecondsMode(settings.ThresholdMode))
                return "TreqSeconds";
            return "InvalidPercentMode";
        }

        public static string ResolveCooldownSource(ExperimentSettings settings)
        {
            if (settings == null)
                return "InvalidPercentMode";
            if (settings.ProactiveCooldownSeconds > 0.0)
                return "Explicit";
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode))
                return "ChengTreq";
            if (ChengTreqCalculator.IsTreqSecondsMode(settings.ThresholdMode))
                return "TreqSeconds";
            return "InvalidPercentMode";
        }

        public static string ResolveDeadlineThresholdSource(ExperimentSettings settings)
        {
            if (settings == null)
                return "InvalidPercentMode";
            if (settings.BprDeadlineThresholdSeconds > 0.0)
                return "Explicit";
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode))
                return "ChengTreq";
            if (ChengTreqCalculator.IsTreqSecondsMode(settings.ThresholdMode))
                return "TreqSeconds";
            return "InvalidPercentMode";
        }

        private static string BuildPercentModeErrorMessage(ExperimentSettings settings, string algorithm)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("目前設定不可行：ThresholdMode = Percent 時，BP&R 需要明確設定時間參數。");
            builder.AppendLine();
            builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "演算法：{0}", algorithm));
            builder.AppendLine("目前 ThresholdMode：Percent");
            builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "ProactivePredictionHorizonSeconds：{0}", settings.ProactivePredictionHorizonSeconds));
            builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "ProactiveCooldownSeconds：{0}", settings.ProactiveCooldownSeconds));
            builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "BprDeadlineThresholdSeconds：{0}", settings.BprDeadlineThresholdSeconds));
            builder.AppendLine(String.Format(CultureInfo.InvariantCulture, "TreqSeconds：{0}", settings.TreqSeconds));
            builder.AppendLine();
            builder.AppendLine("Percent 模式的自然請求門檻使用 RequestThresholdPercent，但 BP&R 的預測範圍與 cooldown 是時間參數，不能隱性使用 TreqSeconds。");
            builder.AppendLine("請明確設定 ProactivePredictionHorizonSeconds、ProactiveCooldownSeconds、BprDeadlineThresholdSeconds，或改用 ChengTreq / TreqSeconds 模式。");
            return builder.ToString();
        }
    }

    internal sealed class ExperimentSimulationQueueWorkItem
    {
        public ExperimentSettings Settings;
        public ExperimentArtifact Artifact;
        public string Algorithm;
        public int AlgorithmIndex;
        public ExperimentRunResult[] AlgorithmResults;
        public bool SweepEnabled;
        public int SweepIndex;
        public int SweepValueCount;
        public string SweepParameterName;
        public string SweepValueText;
        public int RunIndex;
        public int RunCount;
    }

    internal static class ExperimentSimulationQueueRunner
    {
        public static int ResolveProducerCount(int maxParallelJobs, int artifactWork)
        {
            int requested = Math.Max(1, maxParallelJobs / 4);
            requested = Math.Min(4, requested);
            return Math.Max(1, Math.Min(Math.Max(1, artifactWork), requested));
        }

        public static void Run(
            int totalWork,
            int maxParallelJobs,
            int algorithmCount,
            string taskDetailsDirectory,
            MissionDetailCsvOutputOptions csvOutputOptions,
            Action<BlockingCollection<ExperimentSimulationQueueWorkItem>> produceItems,
            Action<int, int, string> reportProgress)
        {
            int queueCapacity = Math.Max(Math.Max(1, algorithmCount), maxParallelJobs);
            BlockingCollection<ExperimentSimulationQueueWorkItem> queue =
                new BlockingCollection<ExperimentSimulationQueueWorkItem>(queueCapacity);
            ExperimentSimulationQueueExceptionState exceptionState = new ExperimentSimulationQueueExceptionState();
            int completedWork = 0;
            Task[] workers = new Task[Math.Max(1, maxParallelJobs)];

            for (int i = 0; i < workers.Length; i++)
            {
                workers[i] = Task.Factory.StartNew(delegate
                {
                    foreach (ExperimentSimulationQueueWorkItem work in queue.GetConsumingEnumerable())
                    {
                        if (exceptionState.HasException())
                            continue;

                        try
                        {
                            ExperimentSimulation simulation = new ExperimentSimulation(
                                work.Settings, work.Artifact, work.Algorithm, taskDetailsDirectory, csvOutputOptions);
                            ExperimentRunResult run = simulation.Run();
                            work.AlgorithmResults[work.AlgorithmIndex] = run;

                            int done = Interlocked.Increment(ref completedWork);
                            if (reportProgress != null)
                                reportProgress(done, totalWork, BuildProgressDetail(work, done, totalWork));
                        }
                        catch (Exception ex)
                        {
                            exceptionState.Record(ex);
                        }
                    }
                }, CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
            }

            try
            {
                produceItems(queue);
            }
            catch (Exception ex)
            {
                exceptionState.Record(ex);
            }
            finally
            {
                queue.CompleteAdding();
            }

            try
            {
                Task.WaitAll(workers);
            }
            catch (AggregateException ex)
            {
                exceptionState.Record(ex);
            }

            exceptionState.ThrowIfNeeded();
        }

        private static string BuildProgressDetail(ExperimentSimulationQueueWorkItem work, int done, int totalWork)
        {
            if (work.SweepEnabled)
            {
                return String.Format(CultureInfo.InvariantCulture,
                    "完成 value {0}/{1} {2}={3}, run {4}/{5}, algorithm {6} ({7}/{8})",
                    work.SweepIndex + 1,
                    work.SweepValueCount,
                    work.SweepParameterName,
                    work.SweepValueText,
                    work.RunIndex,
                    work.RunCount,
                    work.Algorithm,
                    done,
                    totalWork);
            }

            return String.Format(CultureInfo.InvariantCulture,
                "完成 run {0}/{1} {2} ({3}/{4})",
                work.RunIndex,
                work.RunCount,
                work.Algorithm,
                done,
                totalWork);
        }

        private sealed class ExperimentSimulationQueueExceptionState
        {
            private readonly object syncRoot = new object();
            private Exception firstException;

            public bool HasException()
            {
                lock (syncRoot)
                {
                    return firstException != null;
                }
            }

            public void Record(Exception ex)
            {
                lock (syncRoot)
                {
                    if (firstException == null)
                        firstException = ex;
                }
            }

            public void ThrowIfNeeded()
            {
                lock (syncRoot)
                {
                    if (firstException != null)
                        throw new AggregateException(firstException);
                }
            }
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
            BprTimingValidator.ThrowIfInvalid(settings);
            Report("BP&R timing validation passed.");
            WcvMaxTaskFeasibilityValidator.ThrowIfInvalid(settings);
            List<string> algorithms = settings.GetSelectedAlgorithms();
            if (algorithms.Count == 0)
                throw new InvalidOperationException("至少需要選擇一個演算法。");

            List<WcvMaxTaskFeasibilityResult> feasibilityResults =
                WcvMaxTaskFeasibilityValidator.ValidateSelectedAlgorithms(settings);
            for (int i = 0; i < feasibilityResults.Count; i++)
            {
                if (!feasibilityResults[i].IsValid)
                {
                    Report(feasibilityResults[i].ErrorMessage);
                    throw new InvalidOperationException(feasibilityResults[i].ErrorMessage);
                }
                Report(String.Format(CultureInfo.InvariantCulture,
                    "WCV feasibility validation passed: Algorithm={0}, ValidationTaskLimit={1}, EstimatedMaxTaskMissionEnergyJ={2}, WcvCapacityJ={3}, NmaxTask={4}",
                    feasibilityResults[i].Algorithm,
                    feasibilityResults[i].ValidationTaskLimit,
                    feasibilityResults[i].EstimatedMaxTaskMissionEnergyJ,
                    settings.WcvCapacityJ,
                    settings.NmaxTask));
            }
            ExperimentBatchResult result = new ExperimentBatchResult();
            result.Settings = settings;
            string executionDirectory = settings.CreateExecutionOutputDirectory();
            result.OutputDirectory = executionDirectory;
            MissionDetailCsvOutputOptions csvOutputOptions = settings.CreateMissionDetailCsvOutputOptions();
            result.TaskDetailsDirectory = MissionDetailCsvWriter.PrepareTaskDetailsDirectory(executionDirectory);
            string taskDetailsDirectory = csvOutputOptions.HasAnyOutput() ? result.TaskDetailsDirectory : null;

            int totalWork = settings.RunCount * algorithms.Count;
            int completedWork = 0;
            int maxParallelJobs = ResolveMaxParallelJobs(settings, totalWork);
            RaiseThreadPoolMinimum(maxParallelJobs);
            ExperimentRunBatchResult[] runResults = new ExperimentRunBatchResult[settings.RunCount];
            Report(String.Format(CultureInfo.InvariantCulture,
                "平行批次啟動：runs={0}, algorithms={1}, max parallel jobs={2}{3}",
                settings.RunCount, algorithms.Count, maxParallelJobs,
                settings.MaxParallelJobs > 0 ? " (manual)" : " (auto)"));

            ChengTreqMetrics chengMetrics = ChengTreqCalculator.Compute(settings, settings.NmaxTask);
            string treqSource = ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode)
                ? "ChengTreq"
                : (String.Equals(settings.ThresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase) ? "TreqSeconds" : "Percent");
            Report(String.Format(CultureInfo.InvariantCulture,
                "ThresholdMode={0}, RequestThresholdPercent={1}, ConfiguredTreqSeconds={2}, EffectiveTreqSeconds={3}, TreqSource={4}, NmaxTask={5}, MapWidthMeters={6}, MapHeightMeters={7}, ComputedLpathMeters={8}, ComputedTjobSeconds={9}, ComputedLmaxStepMeters={10}",
                settings.ThresholdMode,
                settings.RequestThresholdPercent,
                settings.TreqSeconds,
                ChengTreqCalculator.GetEffectiveRequestThresholdSeconds(settings, settings.NmaxTask),
                treqSource,
                settings.NmaxTask,
                settings.MapWidthMeters,
                settings.MapHeightMeters,
                chengMetrics.LpathMeters,
                chengMetrics.TjobSeconds,
                chengMetrics.LmaxStepMeters));

            if (settings.UseFastSimulationScheduling && totalWork > 1)
            {
                int producerCount = ExperimentSimulationQueueRunner.ResolveProducerCount(maxParallelJobs, settings.RunCount);
                RaiseThreadPoolMinimum(maxParallelJobs + producerCount);
                Report(String.Format(CultureInfo.InvariantCulture,
                    "高速排程啟用：simulation workers={0}, artifact producers={1}, queue capacity={2}",
                    maxParallelJobs, producerCount, Math.Max(algorithms.Count, maxParallelJobs)));

                ParallelOptions producerOptions = new ParallelOptions();
                producerOptions.MaxDegreeOfParallelism = producerCount;
                ExperimentSimulationQueueRunner.Run(totalWork, maxParallelJobs, algorithms.Count, taskDetailsDirectory,
                    csvOutputOptions,
                    delegate(BlockingCollection<ExperimentSimulationQueueWorkItem> queue)
                    {
                        Parallel.For(1, settings.RunCount + 1, producerOptions, delegate(int runIndex)
                        {
                            int seed = settings.BaseSeed + runIndex - 1;
                            ExperimentRunBatchResult runBatch = new ExperimentRunBatchResult(algorithms.Count);
                            ExperimentArtifact artifact = ExperimentArtifact.Generate(settings, runIndex, seed);
                            runBatch.ArtifactSummary = artifact.CreateSummary();
                            runResults[runIndex - 1] = runBatch;

                            for (int algorithmIndex = 0; algorithmIndex < algorithms.Count; algorithmIndex++)
                            {
                                ExperimentSimulationQueueWorkItem work = new ExperimentSimulationQueueWorkItem();
                                work.Settings = settings;
                                work.Artifact = artifact;
                                work.Algorithm = algorithms[algorithmIndex];
                                work.AlgorithmIndex = algorithmIndex;
                                work.AlgorithmResults = runBatch.AlgorithmResults;
                                work.RunIndex = runIndex;
                                work.RunCount = settings.RunCount;
                                queue.Add(work);
                            }
                        });
                    },
                    delegate(int done, int count, string detail)
                    {
                        ReportProgress(done, count, detail);
                    });
            }
            else
            {
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = maxParallelJobs;
            Parallel.For(1, settings.RunCount + 1, options, delegate (int runIndex)
            {
                int seed = settings.BaseSeed + runIndex - 1;
                ExperimentRunBatchResult runBatch = new ExperimentRunBatchResult(algorithms.Count);
                ExperimentArtifact artifact = ExperimentArtifact.Generate(settings, runIndex, seed);
                runBatch.ArtifactSummary = artifact.CreateSummary();

                for (int algorithmIndex = 0; algorithmIndex < algorithms.Count; algorithmIndex++)
                {
                    string algorithm = algorithms[algorithmIndex];
                    ExperimentSimulation simulation = new ExperimentSimulation(settings, artifact, algorithm, taskDetailsDirectory, csvOutputOptions);
                    ExperimentRunResult run = simulation.Run();
                    runBatch.AlgorithmResults[algorithmIndex] = run;

                    int done = Interlocked.Increment(ref completedWork);
                    ReportProgress(done, totalWork,
                        String.Format(CultureInfo.InvariantCulture, "完成 run {0}/{1} {2} ({3}/{4})",
                            runIndex, settings.RunCount, algorithm, done, totalWork));
                }

                runResults[runIndex - 1] = runBatch;
            });
            }
            Report(String.Format(CultureInfo.InvariantCulture,
                "共用資料產生完成：runs={0}, max parallel jobs={1}", settings.RunCount, maxParallelJobs));

            Report("所有 run 已完成，正在由單一執行緒合併結果並寫出 Excel。");
            MergeRunResults(result, runResults);

            string workbookPath = settings.CreateOutputWorkbookPath(executionDirectory);
            ExperimentWorkbookWriter.Write(workbookPath, result);
            settings.LastOutputWorkbookPath = workbookPath;
            if (persistSettings)
                settings.SaveLast();
            result.WorkbookPath = workbookPath;
            MissionDetailCsvWriter.WriteSummaryCsv(result.TaskDetailsDirectory, result);
            if (!csvOutputOptions.HasAnyOutput())
            {
                Report("Excel written: " + workbookPath);
                Report("Summary CSV written: " + Path.Combine(result.TaskDetailsDirectory, "summary.csv"));
                Report("Task detail CSV output is disabled.");
                return result;
            }
            Report("Task detail CSV files: " + csvOutputOptions.DescribeSelectedFiles());
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

                if (runBatch.ArtifactSummary != null)
                    result.ArtifactSummaries.Add(runBatch.ArtifactSummary);
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
            public ExperimentArtifactSummary ArtifactSummary;
            public ExperimentRunResult[] AlgorithmResults;

            public ExperimentRunBatchResult(int algorithmCount)
            {
                AlgorithmResults = new ExperimentRunResult[Math.Max(0, algorithmCount)];
            }
        }
    }

    public class ExperimentSweepBatchRunner
    {
        private readonly Action<string> progress;
        private readonly bool persistSettings;
        private readonly object progressLock;

        public ExperimentSweepBatchRunner(Action<string> progressCallback, bool saveSettingsAfterRun)
        {
            progress = progressCallback;
            persistSettings = saveSettingsAfterRun;
            progressLock = new object();
        }

        public ExperimentBatchResult Run(ExperimentSettings baseSettings)
        {
            baseSettings.Normalize();
            BprTimingValidator.ThrowIfInvalid(baseSettings);
            WcvMaxTaskFeasibilityValidator.ThrowIfInvalid(baseSettings);
            List<string> algorithms = baseSettings.GetSelectedAlgorithms();
            if (algorithms.Count == 0)
                throw new InvalidOperationException("請至少勾選一個排程演算法。");

            List<ExperimentSweepStep> steps = BuildSweepSteps(baseSettings);
            if (steps.Count == 0)
                throw new InvalidOperationException("請先設定參數迭代。");

            for (int i = 0; i < steps.Count; i++)
                BprTimingValidator.ThrowIfInvalid(steps[i].Settings);
            for (int i = 0; i < steps.Count; i++)
            {
                List<WcvMaxTaskFeasibilityResult> stepFeasibilityResults =
                    WcvMaxTaskFeasibilityValidator.ValidateSelectedAlgorithms(steps[i].Settings);
                for (int j = 0; j < stepFeasibilityResults.Count; j++)
                {
                    if (!stepFeasibilityResults[j].IsValid)
                    {
                        string message = String.Format(CultureInfo.InvariantCulture,
                            "Sweep value {0}/{1} ({2}={3}) failed WCV feasibility validation.{4}{5}",
                            i + 1,
                            steps.Count,
                            steps[i].ParameterKey,
                            FormatSweepValue(steps[i]),
                            Environment.NewLine,
                            stepFeasibilityResults[j].ErrorMessage);
                        Report(message);
                        throw new InvalidOperationException(message);
                    }
                }
            }
            Report(String.Format(CultureInfo.InvariantCulture,
                "BP&R timing validation passed for {0} sweep value(s).", steps.Count));
            Report(String.Format(CultureInfo.InvariantCulture,
                "WCV feasibility validation passed for {0} sweep value(s).", steps.Count));

            ExperimentBatchResult result = new ExperimentBatchResult();
            result.Settings = baseSettings.Copy();
            result.Settings.Normalize();
            string executionDirectory = baseSettings.CreateExecutionOutputDirectory();
            result.OutputDirectory = executionDirectory;
            MissionDetailCsvOutputOptions csvOutputOptions = baseSettings.CreateMissionDetailCsvOutputOptions();
            result.TaskDetailsDirectory = MissionDetailCsvWriter.PrepareTaskDetailsDirectory(executionDirectory);
            string taskDetailsDirectory = csvOutputOptions.HasAnyOutput() ? result.TaskDetailsDirectory : null;

            int artifactWork = steps.Count * baseSettings.RunCount;
            int totalWork = artifactWork * algorithms.Count;
            int completedWork = 0;
            int maxParallelJobs = ResolveMaxParallelJobs(baseSettings, totalWork);
            RaiseThreadPoolMinimum(maxParallelJobs);

            SweepRunBatchResult[] runResults = new SweepRunBatchResult[artifactWork];
            Report(String.Format(CultureInfo.InvariantCulture,
                "參數迭代開始：parameter={0}, values={1}, runsPerValue={2}, algorithms={3}, total simulations={4}, max parallel jobs={5}{6}",
                steps[0].ParameterName,
                steps.Count,
                baseSettings.RunCount,
                algorithms.Count,
                totalWork,
                maxParallelJobs,
                baseSettings.MaxParallelJobs > 0 ? " (manual)" : " (auto)"));

            if (baseSettings.UseFastSimulationScheduling && totalWork > 1)
            {
                int producerCount = ExperimentSimulationQueueRunner.ResolveProducerCount(maxParallelJobs, artifactWork);
                RaiseThreadPoolMinimum(maxParallelJobs + producerCount);
                Report(String.Format(CultureInfo.InvariantCulture,
                    "高速排程啟用：simulation workers={0}, artifact producers={1}, queue capacity={2}",
                    maxParallelJobs, producerCount, Math.Max(algorithms.Count, maxParallelJobs)));

                ParallelOptions producerOptions = new ParallelOptions();
                producerOptions.MaxDegreeOfParallelism = producerCount;
                ExperimentSimulationQueueRunner.Run(totalWork, maxParallelJobs, algorithms.Count, taskDetailsDirectory,
                    csvOutputOptions,
                    delegate(BlockingCollection<ExperimentSimulationQueueWorkItem> queue)
                    {
                        Parallel.For(0, artifactWork, producerOptions, delegate(int artifactIndex)
                        {
                            int stepIndex = artifactIndex / baseSettings.RunCount;
                            int runIndex = artifactIndex % baseSettings.RunCount + 1;
                            ExperimentSweepStep step = steps[stepIndex];
                            int seed = baseSettings.BaseSeed + runIndex - 1;

                            SweepRunBatchResult runBatch = new SweepRunBatchResult(algorithms.Count);
                            ExperimentArtifact artifact = ExperimentArtifact.Generate(step.Settings, runIndex, seed);
                            runBatch.ArtifactSummary = artifact.CreateSummary();
                            runResults[artifactIndex] = runBatch;

                            for (int algorithmIndex = 0; algorithmIndex < algorithms.Count; algorithmIndex++)
                            {
                                ExperimentSimulationQueueWorkItem work = new ExperimentSimulationQueueWorkItem();
                                work.Settings = step.Settings;
                                work.Artifact = artifact;
                                work.Algorithm = algorithms[algorithmIndex];
                                work.AlgorithmIndex = algorithmIndex;
                                work.AlgorithmResults = runBatch.AlgorithmResults;
                                work.SweepEnabled = true;
                                work.SweepIndex = stepIndex;
                                work.SweepValueCount = steps.Count;
                                work.SweepParameterName = step.ParameterName;
                                work.SweepValueText = FormatSweepValue(step);
                                work.RunIndex = runIndex;
                                work.RunCount = baseSettings.RunCount;
                                queue.Add(work);
                            }
                        });
                    },
                    delegate(int done, int count, string detail)
                    {
                        ReportProgress(done, count, detail);
                    });
            }
            else
            {
            ParallelOptions options = new ParallelOptions();
            options.MaxDegreeOfParallelism = maxParallelJobs;
            Parallel.For(0, artifactWork, options, delegate(int artifactIndex)
            {
                int stepIndex = artifactIndex / baseSettings.RunCount;
                int runIndex = artifactIndex % baseSettings.RunCount + 1;
                ExperimentSweepStep step = steps[stepIndex];
                int seed = baseSettings.BaseSeed + runIndex - 1;

                SweepRunBatchResult runBatch = new SweepRunBatchResult(algorithms.Count);
                ExperimentArtifact artifact = ExperimentArtifact.Generate(step.Settings, runIndex, seed);
                runBatch.ArtifactSummary = artifact.CreateSummary();

                for (int algorithmIndex = 0; algorithmIndex < algorithms.Count; algorithmIndex++)
                {
                    string algorithm = algorithms[algorithmIndex];
                    ExperimentSimulation simulation = new ExperimentSimulation(step.Settings, artifact, algorithm, taskDetailsDirectory, csvOutputOptions);
                    ExperimentRunResult run = simulation.Run();
                    runBatch.AlgorithmResults[algorithmIndex] = run;

                    int done = Interlocked.Increment(ref completedWork);
                    ReportProgress(done, totalWork,
                        String.Format(CultureInfo.InvariantCulture, "完成 value {0}/{1} {2}={3}, run {4}/{5}, algorithm {6} ({7}/{8})",
                            stepIndex + 1,
                            steps.Count,
                            step.ParameterName,
                            FormatSweepValue(step),
                            runIndex,
                            baseSettings.RunCount,
                            algorithm,
                            done,
                            totalWork));
                }

                runResults[artifactIndex] = runBatch;
            });
            }
            Report(String.Format(CultureInfo.InvariantCulture,
                "共用資料產生完成：values={0}, runsPerValue={1}, max parallel jobs={2}",
                steps.Count, baseSettings.RunCount, maxParallelJobs));

            MergeSweepRunResults(result, runResults);

            string workbookPath = baseSettings.CreateOutputWorkbookPath(executionDirectory);
            ExperimentWorkbookWriter.Write(workbookPath, result);
            baseSettings.LastOutputWorkbookPath = workbookPath;
            if (persistSettings)
                baseSettings.SaveLast();
            result.WorkbookPath = workbookPath;
            MissionDetailCsvWriter.WriteSummaryCsv(result.TaskDetailsDirectory, result);
            if (!csvOutputOptions.HasAnyOutput())
            {
                Report("Excel written: " + workbookPath);
                Report("Summary CSV written: " + Path.Combine(result.TaskDetailsDirectory, "summary.csv"));
                Report("Task detail CSV output is disabled.");
                return result;
            }
            Report("Task detail CSV files: " + csvOutputOptions.DescribeSelectedFiles());
            Report("參數迭代 Excel 已輸出：" + workbookPath);
            Report("任務明細 CSV 已輸出：" + result.TaskDetailsDirectory);
            return result;
        }

        private static List<ExperimentSweepStep> BuildSweepSteps(ExperimentSettings baseSettings)
        {
            ExperimentSweepParameterDefinition definition = ExperimentSweepParameterCatalog.Find(baseSettings.SweepParameterKey);
            if (definition == null)
                throw new InvalidOperationException("找不到可迭代參數：" + baseSettings.SweepParameterKey);

            List<ExperimentSweepStep> steps = new List<ExperimentSweepStep>();
            double startValue = definition.GetValue(baseSettings);
            int maxIndex = Math.Max(0, baseSettings.SweepIterationCount);
            for (int i = 0; i <= maxIndex; i++)
            {
                double value = startValue + baseSettings.SweepStepValue * i;
                ExperimentSettings settings = baseSettings.Copy();
                settings.CurrentSweepIndex = i;
                settings.SweepEnabled = true;
                settings.SweepParameterKey = definition.Key;
                settings.CurrentSweepValue = value;
                definition.SetValue(settings, value);
                if (String.Equals(definition.Key, "TreqSeconds", StringComparison.OrdinalIgnoreCase))
                {
                    double stepTreq = Math.Max(1.0, value);
                    settings.ThresholdMode = "TreqSeconds";
                    settings.TreqSeconds = stepTreq;
                    settings.BprDeadlineThresholdSeconds = stepTreq;
                }
                settings.Normalize();
                BprTimingValidator.ThrowIfInvalid(settings);
                WcvMaxTaskFeasibilityValidator.ThrowIfInvalid(settings);
                steps.Add(new ExperimentSweepStep
                {
                    Index = i,
                    ParameterKey = definition.Key,
                    ParameterName = definition.DisplayName,
                    Value = definition.GetValue(settings),
                    Settings = settings
                });
            }
            return steps;
        }

        private static string FormatSweepValue(ExperimentSweepStep step)
        {
            ExperimentSweepParameterDefinition definition = ExperimentSweepParameterCatalog.Find(step.ParameterKey);
            if (definition == null)
                return step.Value.ToString(CultureInfo.InvariantCulture);
            return definition.FormatValue(step.Value);
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

        private static void MergeSweepRunResults(ExperimentBatchResult result, SweepRunBatchResult[] runResults)
        {
            for (int i = 0; i < runResults.Length; i++)
            {
                SweepRunBatchResult runBatch = runResults[i];
                if (runBatch == null)
                    continue;

                if (runBatch.ArtifactSummary != null)
                    result.ArtifactSummaries.Add(runBatch.ArtifactSummary);
                for (int j = 0; j < runBatch.AlgorithmResults.Length; j++)
                {
                    ExperimentRunResult run = runBatch.AlgorithmResults[j];
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

        private class SweepRunBatchResult
        {
            public ExperimentArtifactSummary ArtifactSummary;
            public ExperimentRunResult[] AlgorithmResults;

            public SweepRunBatchResult(int algorithmCount)
            {
                AlgorithmResults = new ExperimentRunResult[Math.Max(0, algorithmCount)];
            }
        }
    }

    public class ExperimentBatchResult
    {
        public ExperimentSettings Settings { get; set; }
        public List<ExperimentArtifactSummary> ArtifactSummaries { get; private set; }
        public List<ExperimentRunSummary> RunSummaries { get; private set; }
        public List<ExperimentDeathRecord> DeathRecords { get; private set; }
        public string WorkbookPath { get; set; }
        public string OutputDirectory { get; set; }
        public string TaskDetailsDirectory { get; set; }
        public int TotalTaskRecordCount { get; set; }

        public ExperimentBatchResult()
        {
            ArtifactSummaries = new List<ExperimentArtifactSummary>();
            RunSummaries = new List<ExperimentRunSummary>();
            DeathRecords = new List<ExperimentDeathRecord>();
            WorkbookPath = "";
            OutputDirectory = "";
            TaskDetailsDirectory = "";
        }
    }

    public class ExperimentArtifactSummary
    {
        public int RunIndex;
        public int Seed;
        public string ArtifactHash;
        public int RateChangeScheduleCount;
        public bool SweepEnabled;
        public int SweepIndex;
        public string SweepParameterKey;
        public string SweepParameterName;
        public double SweepValue;
    }

    public class ExperimentArtifact
    {
        private static readonly List<RateChangeTemplate> EmptyRateChanges = new List<RateChangeTemplate>();
        public int RunIndex;
        public int Seed;
        public string ArtifactHash;
        public List<SensorTemplate> Sensors;
        public List<ActivationEventTemplate> ActivationEvents;
        public List<RateChangeTemplate> RateChanges;
        public Dictionary<int, List<RateChangeTemplate>> RateChangesByNodeId;
        public double BaseX;
        public double BaseY;
        public bool UsesActivationSchedule;
        public bool SweepEnabled;
        public int SweepIndex;
        public string SweepParameterKey;
        public string SweepParameterName;
        public double SweepValue;
        private int rateChangeIndexSourceCount;

        public ExperimentArtifact()
        {
            Sensors = new List<SensorTemplate>();
            ActivationEvents = new List<ActivationEventTemplate>();
            RateChanges = new List<RateChangeTemplate>();
            RateChangesByNodeId = new Dictionary<int, List<RateChangeTemplate>>();
            rateChangeIndexSourceCount = -1;
            ArtifactHash = "";
            SweepParameterKey = "";
            SweepParameterName = "";
        }

        public static ExperimentArtifact Generate(ExperimentSettings settings, int runIndex, int seed)
        {
            Random random = new Random(seed);
            ExperimentArtifact artifact = new ExperimentArtifact();
            artifact.RunIndex = runIndex;
            artifact.Seed = seed;
            artifact.BaseX = 0.0;
            artifact.BaseY = 0.0;
            ExperimentSweepParameterDefinition sweepDefinition = settings.SweepEnabled
                ? ExperimentSweepParameterCatalog.Find(settings.SweepParameterKey)
                : null;
            artifact.SweepEnabled = sweepDefinition != null;
            artifact.SweepIndex = artifact.SweepEnabled ? settings.CurrentSweepIndex : 0;
            artifact.SweepParameterKey = artifact.SweepEnabled ? sweepDefinition.Key : "";
            artifact.SweepParameterName = artifact.SweepEnabled ? sweepDefinition.DisplayName : "";
            artifact.SweepValue = artifact.SweepEnabled ? sweepDefinition.GetValue(settings) : 0.0;

            BuildRandomSensors(settings, artifact, random, seed);
            artifact.UsesActivationSchedule = true;
            GenerateChengActivationEvents(settings, artifact, seed);

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

            artifact.BuildRateChangesByNodeId();
            artifact.ArtifactHash = artifact.ComputeHash(settings);
            return artifact;
        }

        private static void GenerateChengActivationEvents(
            ExperimentSettings settings,
            ExperimentArtifact artifact,
            int seed)
        {
            artifact.ActivationEvents.Clear();

            int targetActivationCount = ResolveTargetActivationCount(settings);
            if (targetActivationCount <= 0)
                return;

            List<int> nodeIds = new List<int>();
            for (int id = 1; id <= settings.SensorCount; id++)
                nodeIds.Add(id);

            Random activationRandom = new Random(StableActivationSeed(seed));
            for (int i = nodeIds.Count - 1; i > 0; i--)
            {
                int j = activationRandom.Next(i + 1);
                int temp = nodeIds[i];
                nodeIds[i] = nodeIds[j];
                nodeIds[j] = temp;
            }

            double currentActivationTime = 0.0;
            double rate = Math.Max(1e-12, settings.EventRatePerSecond);
            for (int i = 0; i < targetActivationCount; i++)
            {
                double u = Math.Max(1e-12, activationRandom.NextDouble());
                currentActivationTime += -Math.Log(1.0 - u) / rate;

                ActivationEventTemplate activationEvent = new ActivationEventTemplate();
                activationEvent.TimeSeconds = currentActivationTime;
                activationEvent.NodeId = nodeIds[i];
                artifact.ActivationEvents.Add(activationEvent);
            }

            artifact.ActivationEvents.Sort(delegate (ActivationEventTemplate a, ActivationEventTemplate b)
            {
                int compare = a.TimeSeconds.CompareTo(b.TimeSeconds);
                if (compare != 0)
                    return compare;
                return a.NodeId.CompareTo(b.NodeId);
            });
        }

        private static int ResolveTargetActivationCount(ExperimentSettings settings)
        {
            if (settings == null)
                return 0;
            double expected = Math.Max(0.0, settings.EventRatePerSecond) *
                Math.Max(1.0, settings.SensorBackgroundLifetimeSeconds);
            int target = Math.Max(0, (int)Math.Round(expected));
            return Math.Min(Math.Max(0, settings.SensorCount), target);
        }

        private static int StableActivationSeed(int runSeed)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + runSeed;
                hash = hash * 31 + 2000003;
                return hash & 0x7fffffff;
            }
        }

        public void BuildRateChangesByNodeId()
        {
            RateChangesByNodeId = new Dictionary<int, List<RateChangeTemplate>>();
            if (RateChanges == null)
            {
                rateChangeIndexSourceCount = 0;
                return;
            }

            for (int i = 0; i < RateChanges.Count; i++)
            {
                RateChangeTemplate change = RateChanges[i];
                List<RateChangeTemplate> nodeChanges;
                if (!RateChangesByNodeId.TryGetValue(change.NodeId, out nodeChanges))
                {
                    nodeChanges = new List<RateChangeTemplate>();
                    RateChangesByNodeId[change.NodeId] = nodeChanges;
                }
                nodeChanges.Add(change);
            }

            foreach (KeyValuePair<int, List<RateChangeTemplate>> pair in RateChangesByNodeId)
            {
                pair.Value.Sort(delegate (RateChangeTemplate a, RateChangeTemplate b)
                {
                    int compare = a.TimeSeconds.CompareTo(b.TimeSeconds);
                    if (compare != 0)
                        return compare;
                    return a.NodeId.CompareTo(b.NodeId);
                });
            }
            rateChangeIndexSourceCount = RateChanges.Count;
        }

        public List<RateChangeTemplate> GetRateChangesForNode(int nodeId)
        {
            if (RateChangesByNodeId == null ||
                RateChanges == null ||
                rateChangeIndexSourceCount != RateChanges.Count)
            {
                BuildRateChangesByNodeId();
            }

            List<RateChangeTemplate> nodeChanges;
            if (RateChangesByNodeId.TryGetValue(nodeId, out nodeChanges))
                return nodeChanges;
            return EmptyRateChanges;
        }

        private static void BuildRandomSensors(ExperimentSettings settings, ExperimentArtifact artifact, Random random, int seed)
        {
            artifact.Sensors.Clear();

            SensorTemplate baseStation = new SensorTemplate();
            baseStation.Id = 0;
            baseStation.X = artifact.BaseX;
            baseStation.Y = artifact.BaseY;
            baseStation.InitialEnergyJ = Double.PositiveInfinity;
            baseStation.InitialResidualEnergyJ = Double.PositiveInfinity;
            baseStation.ParentId = -1;
            baseStation.InitiallyActive = true;
            artifact.Sensors.Add(baseStation);

            Random residualRandom = settings.InitialResidualJitterPercent > 0.0
                ? new Random(StableInitialResidualSeed(seed))
                : null;
            double minimumResidualRatio = 1.0 - settings.InitialResidualJitterPercent / 100.0;

            for (int id = 1; id <= settings.SensorCount; id++)
            {
                SensorTemplate sensor = new SensorTemplate();
                sensor.Id = id;
                sensor.X = random.NextDouble() * settings.MapWidthMeters;
                sensor.Y = random.NextDouble() * settings.MapHeightMeters;
                sensor.InitialEnergyJ = settings.InitialEnergyJ;
                sensor.InitialResidualEnergyJ = settings.InitialEnergyJ;
                if (residualRandom != null)
                {
                    double residualRatio = minimumResidualRatio +
                        residualRandom.NextDouble() * (1.0 - minimumResidualRatio);
                    sensor.InitialResidualEnergyJ = settings.InitialEnergyJ * residualRatio;
                }
                sensor.ParentId = -1;
                sensor.InitiallyActive = false;
                artifact.Sensors.Add(sensor);
            }
        }

        private static int StableInitialResidualSeed(int runSeed)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + runSeed;
                hash = hash * 31 + 3000017;
                return hash & 0x7fffffff;
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
                    AddHash(ref hash, Sensors[i].InitialResidualEnergyJ);
                    AddHash(ref hash, Sensors[i].ParentId);
                    AddHash(ref hash, Sensors[i].InitiallyActive ? 1 : 0);
                }
                for (int i = 0; i < ActivationEvents.Count; i++)
                {
                    AddHash(ref hash, ActivationEvents[i].TimeSeconds);
                    AddHash(ref hash, ActivationEvents[i].NodeId);
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

        public ExperimentArtifactSummary CreateSummary()
        {
            ExperimentArtifactSummary summary = new ExperimentArtifactSummary();
            summary.RunIndex = RunIndex;
            summary.Seed = Seed;
            summary.ArtifactHash = ArtifactHash;
            summary.RateChangeScheduleCount = RateChanges == null ? 0 : RateChanges.Count;
            summary.SweepEnabled = SweepEnabled;
            summary.SweepIndex = SweepIndex;
            summary.SweepParameterKey = SweepParameterKey;
            summary.SweepParameterName = SweepParameterName;
            summary.SweepValue = SweepValue;
            return summary;
        }
    }

    public class SensorTemplate
    {
        public int Id;
        public double X;
        public double Y;
        public double InitialEnergyJ;
        public double InitialResidualEnergyJ;
        public int ParentId;
        public bool InitiallyActive;
    }

    public class ActivationEventTemplate
    {
        public double TimeSeconds;
        public int NodeId;
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
        private const string ChengBprPaperRandomReason = "CHENG_BPR_PAPER_RANDOM";
        private const string ChengBprRouteInsertionReason = "CHENG_BPR_ROUTE_INSERTION";
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
        private int nextActivationIndex;
        private int nextRateChangeIndex;
        private int nextRequestId;
        private int missionId;
        private double currentTime;
        private bool stopForFirstDeath;
        private HashSet<int> plannedMissionNodeIds;
        private long predictionCacheVersion;
        private BprPredictedRequestCache bprPredictedRequestCache;
        private YuPredictedIntervalCache yuPredictedIntervalCache;

        // BP&R variants:
        // CHENG_BPR: fixed CHENG deadline interval + CHENG bottleneck window + random selection.
        // YU_BPR: dynamic YU request interval + CHENG bottleneck window + random selection.
        // ROUTE_CHENG_BPR: fixed CHENG deadline interval + CHENG bottleneck window + route insertion cost selection.
        // ROUTE_YU_BPR: dynamic YU request interval + CHENG bottleneck window + route insertion cost selection.
        private enum BprProactiveSelectionMode
        {
            Random,
            RouteInsertionCost
        }

        private enum YuProactiveSelectionMode
        {
            Random,
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
            public bool IsPendingRequest;
            public bool IsScheduledInCurrentMission;
            public bool IsAlive;
            public string LastUpdateReason;
            public double LastChargedTimeSeconds;
            public double LastProactiveSelectedTimeSeconds;
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

        private sealed class BprPredictedRequestCache
        {
            public double CurrentTimeSeconds;
            public int MissionId;
            public int MaxTask;
            public string ReservedSignature;
            public long Version;
            public List<BprPredictedRequest> Requests;
        }

        private sealed class YuPredictedIntervalCache
        {
            public double CurrentTimeSeconds;
            public int MissionId;
            public int MaxTask;
            public string ReservedSignature;
            public long Version;
            public List<YuPredictedInterval> Intervals;
        }

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName)
            : this(experimentSettings, sharedArtifact, schedulerName, null)
        {
        }

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName, string taskDetailsDirectory)
            : this(experimentSettings, sharedArtifact, schedulerName, taskDetailsDirectory, MissionDetailCsvOutputOptions.All())
        {
        }

        public ExperimentSimulation(ExperimentSettings experimentSettings, ExperimentArtifact sharedArtifact, string schedulerName, string taskDetailsDirectory, MissionDetailCsvOutputOptions csvOutputOptions)
        {
            settings = experimentSettings;
            artifact = sharedArtifact;
            algorithm = ExperimentSettings.CanonicalAlgorithmKey(schedulerName);
            algorithmRandom = new Random(sharedArtifact.Seed * 397 + StableStringHash(algorithm));
            sensors = new SensorState[artifact.Sensors.Count];
            activeRequests = new List<ChargingRequest>();
            deaths = new List<ExperimentDeathRecord>();
            servedNodeIds = new HashSet<int>();
            MissionDetailCsvOutputOptions effectiveCsvOptions = csvOutputOptions ?? MissionDetailCsvOutputOptions.All();
            csvWriter = String.IsNullOrWhiteSpace(taskDetailsDirectory) || !effectiveCsvOptions.HasAnyOutput()
                ? null
                : new MissionDetailCsvWriter(taskDetailsDirectory, artifact, algorithm, effectiveCsvOptions);
            nextActivationIndex = 0;
            nextRateChangeIndex = 0;
            nextRequestId = 1;
            missionId = 0;
            currentTime = 0.0;
            stopForFirstDeath = false;
            totalTaskRecordCount = 0;
            totalDeliveredEnergyForTasks = 0.0;
            totalDeliveredEnergyForProactiveTasks = 0.0;
            proactiveTaskRecordCount = 0;
            predictionCacheVersion = 0;
            bprPredictedRequestCache = null;
            yuPredictedIntervalCache = null;

            for (int i = 0; i < artifact.Sensors.Count; i++)
                sensors[i] = new SensorState(artifact.Sensors[i], settings, artifact.UsesActivationSchedule);
            artifact.GetRateChangesForNode(0);
            InitializeBprSTable();

            summary = new ExperimentRunSummary();
            CopySweepFieldsTo(summary);
            summary.RunIndex = artifact.RunIndex;
            summary.Seed = artifact.Seed;
            summary.Algorithm = algorithm;
            summary.ArtifactHash = artifact.ArtifactHash;
            summary.PrateChange = settings.PrateChange;
            summary.RateChangeVariationPercent = settings.RateChangeVariationPercent;
            summary.ThresholdMode = settings.ThresholdMode;
            summary.RequestThresholdPercent = settings.RequestThresholdPercent;
            summary.EffectiveTreqSeconds = ChengTreqCalculator.GetEffectiveRequestThresholdSeconds(settings, settings.NmaxTask);
            summary.TreqSource = ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode)
                ? "ChengTreq"
                : (String.Equals(settings.ThresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase) ? "TreqSeconds" : "Percent");
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
        }

        private void CopySweepFieldsTo(ExperimentRunSummary record)
        {
            if (record == null)
                return;
            record.SweepEnabled = artifact.SweepEnabled;
            record.SweepIndex = artifact.SweepIndex;
            record.SweepParameterKey = artifact.SweepParameterKey;
            record.SweepParameterName = artifact.SweepParameterName;
            record.SweepValue = artifact.SweepValue;
        }

        private void CopySweepFieldsTo(ExperimentTaskRecord record)
        {
            if (record == null)
                return;
            record.SweepEnabled = artifact.SweepEnabled;
            record.SweepIndex = artifact.SweepIndex;
            record.SweepParameterKey = artifact.SweepParameterKey;
            record.SweepParameterName = artifact.SweepParameterName;
            record.SweepValue = artifact.SweepValue;
        }

        private void CopySweepFieldsTo(MissionRecord record)
        {
            if (record == null)
                return;
            record.SweepEnabled = artifact.SweepEnabled;
            record.SweepIndex = artifact.SweepIndex;
            record.SweepParameterKey = artifact.SweepParameterKey;
            record.SweepParameterName = artifact.SweepParameterName;
            record.SweepValue = artifact.SweepValue;
        }

        private void CopySweepFieldsTo(ExperimentDeathRecord record)
        {
            if (record == null)
                return;
            record.SweepEnabled = artifact.SweepEnabled;
            record.SweepIndex = artifact.SweepIndex;
            record.SweepParameterKey = artifact.SweepParameterKey;
            record.SweepParameterName = artifact.SweepParameterName;
            record.SweepValue = artifact.SweepValue;
        }

        public ExperimentRunResult Run()
        {
            ApplyActivationsAtCurrentTime();
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
            summary.TotalChargingTaskCount = summary.NaturalRequestCount + summary.ProactiveTaskCount;
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
            {
                if (sensors[id].IsActive)
                    RefreshBprSTableEntry(id, "initialize", true);
            }
        }

        private void InvalidatePredictionCache()
        {
            predictionCacheVersion++;
            bprPredictedRequestCache = null;
            yuPredictedIntervalCache = null;
        }

        private static string BuildReservedNodeSignature(HashSet<int> reservedNodeIds)
        {
            if (reservedNodeIds == null || reservedNodeIds.Count == 0)
                return "";

            List<int> reserved = new List<int>(reservedNodeIds);
            reserved.Sort();
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < reserved.Count; i++)
            {
                if (i > 0)
                    builder.Append('|');
                builder.Append(reserved[i].ToString(CultureInfo.InvariantCulture));
            }
            return builder.ToString();
        }

        private double GetEffectiveConsumeRateJPerSecond(SensorState sensor)
        {
            if (sensor == null)
                return 0.0;
            return sensor.ConsumeRateJPerSecond;
        }

        private double ComputePredictedBaseConsumeRateJPerSecond(double rateScale)
        {
            return settings.InitialEnergyJ * Math.Max(0.01, rateScale) /
                Math.Max(1.0, settings.SensorBackgroundLifetimeSeconds);
        }

        private double ComputePredictedEffectiveConsumeRateJPerSecond(int nodeId, double rateScale)
        {
            return ComputePredictedBaseConsumeRateJPerSecond(rateScale);
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
            List<RateChangeTemplate> nodeRateChanges = artifact.GetRateChangesForNode(nodeId);
            for (int i = 0; i < nodeRateChanges.Count; i++)
            {
                RateChangeTemplate change = nodeRateChanges[i];
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

            if (ChengTreqCalculator.IsTimeThresholdMode(settings.ThresholdMode))
            {
                return Math.Min(sensor.CapacityJ * 0.95, effectiveRate * GetEffectiveTreqSeconds());
            }

            return Math.Min(sensor.CapacityJ * 0.95,
                sensor.CapacityJ * settings.RequestThresholdPercent / 100.0);
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

        private void PopulateChargingRequestEnergyFields(ChargingRequest request, SensorState sensor)
        {
            if (request == null || sensor == null)
                return;
            request.ConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            request.BaseConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            request.RequestNodeConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            request.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
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
                InvalidatePredictionCache();
                return;
            }

            if (ShouldRefreshBprDeadline(entry.LatestReportedDeadlineSeconds, newDeadline))
            {
                UpdateBprSTableEntrySnapshot(entry, sensor, newDeadline, reason);
                InvalidatePredictionCache();
                return;
            }

            string snapshotReason = String.IsNullOrEmpty(reason) ? "snapshot_only" : reason + "_snapshot_only";
            UpdateBprSTableEntryStateFields(entry, sensor, snapshotReason);
            InvalidatePredictionCache();
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
                InvalidatePredictionCache();
                return;
            }

            double oldDeadline = entry.LatestReportedDeadlineSeconds;
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
            entry.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            entry.IsAlive = sensor.Alive;
            entry.IsPendingRequest = sensor.HasPendingRequest || HasActiveRequestForNode(nodeId);
            entry.IsScheduledInCurrentMission = IsNodeReservedForCurrentMission(nodeId);
            InvalidatePredictionCache();
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
            entry.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
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
                if (!sensors[nodeId].Alive || !sensors[nodeId].IsActive ||
                    sensors[nodeId].HasPendingRequest || HasActiveRequestForNode(nodeId))
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
            AccumulatePlannedProactiveTasks(route);

            MissionRecord mission = new MissionRecord();
            CopySweepFieldsTo(mission);
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
                double serviceNodeConsumeRateSnapshot = sensor.ConsumeRateJPerSecond;
                string failReason = "";
                bool deadlineOk = arrivalTime <= request.DeadlineSeconds + Epsilon;
                bool success = sensor.Alive && deadlineOk;
                bool taskFailureCounted = false;

                if (!deadlineOk)
                {
                    failReason = "逾期抵達";
                    summary.FailedOrLateTasks++;
                }

                if (!deadlineOk)
                    taskFailureCounted = true;

                ChargingContext context = new ChargingContext();
                double reservedReturnEnergy = Math.Min(wcvEnergy, returnEnergy);
                context.NodeId = request.NodeId;
                context.ChargeRateJPerSecond = settings.WcvChargeRateJPerSecond;
                context.WcvEnergyJ = Math.Max(0.0, wcvEnergy - reservedReturnEnergy);
                context.ReservedReturnEnergyJ = reservedReturnEnergy;
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

                wcvEnergy = context.TotalWcvEnergyJ();
                double afterEnergy = sensor.EnergyJ;
                double chargeEndTime = currentTime;
                if (!sensor.Alive)
                {
                    failReason = "充電前或充電中死亡";
                    success = false;
                }
                if (sensor.Alive &&
                    sensor.EnergyJ < sensor.CapacityJ - 1e-5 &&
                    String.IsNullOrWhiteSpace(failReason))
                {
                    success = false;
                    failReason = "WCV charge energy insufficient after reserving return energy";
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
                    InvalidatePredictionCache();
                }

                ExperimentTaskRecord record = new ExperimentTaskRecord();
                CopySweepFieldsTo(record);
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
                PopulateTaskRecordEnergyFields(record, request.NodeId, request);
                PopulateNodeConsumeRateSnapshotFields(record, request, serviceNodeConsumeRateSnapshot, true);
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
                else if (!taskFailureCounted)
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
                summary.ExecutedProactiveTaskCount++;
            }
            else
            {
                mission.OnDemandRequestCount++;
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
            CopySweepFieldsTo(record);
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
            PopulateTaskRecordEnergyFields(record, request.NodeId, request);
            PopulateNodeConsumeRateSnapshotFields(record, request, 0.0, false);
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

        private void PopulateTaskRecordEnergyFields(ExperimentTaskRecord record, int nodeId, ChargingRequest request)
        {
            if (record == null)
                return;

            if (request != null && request.EffectiveConsumeRateJPerSecond > 0.0)
            {
                record.BaseConsumeRateJPerSecond = request.BaseConsumeRateJPerSecond;
                record.EffectiveConsumeRateJPerSecond = request.EffectiveConsumeRateJPerSecond;
                return;
            }

            if (nodeId <= 0 || nodeId >= sensors.Length)
                return;

            SensorState sensor = sensors[nodeId];
            record.BaseConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
            record.EffectiveConsumeRateJPerSecond = sensor.ConsumeRateJPerSecond;
        }

        private static void PopulateNodeConsumeRateSnapshotFields(
            ExperimentTaskRecord record,
            ChargingRequest request,
            double serviceNodeConsumeRateJPerSecond,
            bool hasServiceSnapshot)
        {
            if (record == null)
                return;

            if (request == null || !hasServiceSnapshot)
            {
                record.RequestNodeConsumeRateJPerSecond = 0.0;
                record.ServiceNodeConsumeRateJPerSecond = 0.0;
                record.NodeConsumeRatePredictionErrorJPerSecond = 0.0;
                return;
            }

            record.RequestNodeConsumeRateJPerSecond = ResolveRequestNodeConsumeRateSnapshot(request);
            record.ServiceNodeConsumeRateJPerSecond = Math.Max(0.0, serviceNodeConsumeRateJPerSecond);
            record.NodeConsumeRatePredictionErrorJPerSecond =
                record.ServiceNodeConsumeRateJPerSecond -
                record.RequestNodeConsumeRateJPerSecond;
        }

        private static double ResolveRequestNodeConsumeRateSnapshot(ChargingRequest request)
        {
            if (request == null)
                return 0.0;
            if (request.RequestNodeConsumeRateJPerSecond > 0.0)
                return request.RequestNodeConsumeRateJPerSecond;
            return Math.Max(0.0, request.ConsumeRateJPerSecond);
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
                InvalidatePredictionCache();
            }
        }

        private void AccumulatePlannedProactiveTasks(List<ChargingRequest> route)
        {
            if (route == null)
                return;

            for (int i = 0; i < route.Count; i++)
            {
                ChargingRequest request = route[i];
                if (request == null || !request.IsProactive)
                    continue;
                summary.PlannedProactiveTaskCount++;
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
                if (sensors[activeRequests[i].NodeId].Alive &&
                    sensors[activeRequests[i].NodeId].IsActive)
                    pool.Add(activeRequests[i].Clone());
            }

            if (algorithm == "NJF_ROUTE_CHENG_BPR_LIMITED")
                return BuildRouteAwareChengBpr(pool, maxTask, true);
            if (algorithm == "NJF_ROUTE_CHENG_BPR_EXTENDED")
                return BuildRouteAwareChengBpr(pool, maxTask, false);
            if (algorithm == "NJF_ROUTE_YU_BPR_LIMITED")
                return BuildRouteAwareYuBpr(pool, maxTask, true);
            if (algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
                return BuildRouteAwareYuBpr(pool, maxTask, false);

            if (algorithm == "NJF_CHENG_BPR")
            {
                List<ChargingRequest> cplist = BuildChengBprPaperCplist(pool, maxTask);
                return BuildNearestRoute(cplist, maxTask);
            }
            if (algorithm == "TADP_CHENG_BPR")
            {
                List<ChargingRequest> cplist = BuildChengBprPaperCplist(pool, maxTask);
                return BuildCompositeRoute(cplist, maxTask, 0.50, 0.50, 0.00);
            }
            if (algorithm == "EDF_CHENG_BPR")
            {
                List<ChargingRequest> cplist = BuildChengBprPaperCplist(pool, maxTask);
                return TakeSorted(cplist, maxTask, CompareByDeadline);
            }

            if (algorithm == "NJF_YU_BPR")
            {
                List<ChargingRequest> cplist = BuildYuBprCplist(
                    pool,
                    maxTask,
                    false,
                    YuProactiveSelectionMode.Random);
                return BuildNearestRoute(cplist, maxTask);
            }

            if (pool.Count == 0)
                return pool;

            if (algorithm == "EDF")
                return TakeSorted(pool, maxTask, CompareByDeadline);
            if (algorithm == "NJF")
                return BuildNearestRoute(pool, maxTask);
            if (algorithm == "TADP_LIN")
                return BuildCompositeRoute(pool, maxTask, 0.50, 0.50, 0.00);

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
            return algorithm == "NJF_CHENG_BPR" ||
                algorithm == "TADP_CHENG_BPR" ||
                algorithm == "EDF_CHENG_BPR" ||
                algorithm == "NJF_YU_BPR" ||
                algorithm == "NJF_ROUTE_CHENG_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_CHENG_BPR_EXTENDED" ||
                algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_YU_BPR_EXTENDED";
        }

        private bool HasBprBottleneckCandidate()
        {
            if (!UsesBprBottleneckCandidates())
                return false;

            int maxTask = GetMissionTaskLimit();
            if (IsChengBprWrapperAlgorithm())
            {
                List<BprPredictedRequest> candidates = BuildChengBprPaperCandidates(new HashSet<int>());
                return BuildChengBprPaperWindows(candidates, maxTask).Count > 0;
            }
            if (algorithm == "NJF_YU_BPR" ||
                algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
            {
                List<YuPredictedInterval> intervals = BuildYuPredictedIntervals(maxTask, new HashSet<int>());
                return BuildYuDangerWindows(intervals, maxTask).Count > 0;
            }

            return false;
        }

        private double FindNextBprBottleneckCandidateTime()
        {
            if (!UsesBprBottleneckCandidates())
                return Double.PositiveInfinity;

            int maxTask = GetMissionTaskLimit();
            if (IsChengBprWrapperAlgorithm())
            {
                double chengWindowTime = Double.PositiveInfinity;
                List<BprPredictedRequest> candidates = BuildChengBprPaperCandidates(new HashSet<int>());
                List<BprWindow> chengWindows = BuildChengBprPaperWindows(candidates, maxTask);
                if (chengWindows.Count > 0)
                    chengWindowTime = chengWindows[0].WindowStartSeconds - EstimateBprTjobSeconds(maxTask);

                if (Double.IsPositiveInfinity(chengWindowTime))
                    return chengWindowTime;
                return Math.Max(currentTime, chengWindowTime);
            }
            if (algorithm == "NJF_YU_BPR" ||
                algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
            {
                return FindNextYuBprBottleneckCandidateTime();
            }

            return Double.PositiveInfinity;
        }

        private bool IsChengBprWrapperAlgorithm()
        {
            return algorithm == "NJF_CHENG_BPR" ||
                algorithm == "TADP_CHENG_BPR" ||
                algorithm == "EDF_CHENG_BPR" ||
                algorithm == "NJF_ROUTE_CHENG_BPR_LIMITED" ||
                algorithm == "NJF_ROUTE_CHENG_BPR_EXTENDED";
        }

        private double FindNextYuBprBottleneckCandidateTime()
        {
            int maxTask = GetMissionTaskLimit();
            double windowTime = Double.PositiveInfinity;
            List<YuPredictedInterval> intervals = BuildYuPredictedIntervals(maxTask, new HashSet<int>());
            List<YuDangerWindow> windows = BuildYuDangerWindows(intervals, maxTask);
            if (windows.Count > 0)
                windowTime = windows[0].WindowStartSeconds - EstimateBprTjobSeconds(maxTask);

            if (Double.IsPositiveInfinity(windowTime))
                return windowTime;
            return Math.Max(currentTime, windowTime);
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
                if (!sensor.Alive || !sensor.IsActive ||
                    sensor.HasPendingRequest || used.Contains(id) || HasActiveRequestForNode(id))
                    continue;

                double threshold = GetRequestThresholdJ(sensor);
                double effectiveRate = GetEffectiveConsumeRateJPerSecond(sensor);
                if (effectiveRate <= 1e-12)
                    continue;
                double timeToRequest = Math.Max(0.0, (sensor.EnergyJ - threshold) / effectiveRate);
                double timeToDeath = Math.Max(0.0, sensor.EnergyJ / effectiveRate);
                if (timeToRequest > GetEffectiveTreqSeconds(maxTask) * 1.5 && sensor.EnergyJ > settings.InitialEnergyJ * 0.35)
                    continue;

                ChargingRequest proactive = new ChargingRequest();
                proactive.RequestId = -id;
                proactive.NodeId = id;
                proactive.RequestTimeSeconds = currentTime;
                proactive.DeadlineSeconds = currentTime + timeToDeath;
                proactive.RequestEnergyJ = sensor.EnergyJ;
                PopulateChargingRequestEnergyFields(proactive, sensor);
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

        private List<ChargingRequest> BuildRouteAwareChengBpr(List<ChargingRequest> requiredRequests, int maxTask, bool enforceTaskLimit)
        {
            List<ChargingRequest> clist = enforceTaskLimit
                ? BuildNearestRoute(requiredRequests, maxTask)
                : new List<ChargingRequest>();
            if (!enforceTaskLimit)
            {
                for (int i = 0; i < requiredRequests.Count; i++)
                    clist.Add(requiredRequests[i].Clone());
            }

            List<ChargingRequest> cplist = BuildChengBprPaperCplist(
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
            if (sensor == null || !sensor.Alive || !sensor.IsActive ||
                sensor.HasPendingRequest || HasActiveRequestForNode(nodeId))
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
            BprSTableEntry entry;
            if (!IsPredictionEligibleNode(nodeId, reservedNodeIds, out entry))
                return new List<BprPredictionSegment>();
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return new List<BprPredictionSegment>();

            SensorState sensor = sensors[nodeId];
            double startEnergy = Double.IsNaN(entry.EnergyJ) ? sensor.EnergyJ : entry.EnergyJ;
            return BuildBprPredictionTimelineFromState(nodeId, horizonEnd, startEnergy, sensor.RateScale);
        }

        private List<BprPredictionSegment> BuildBprPredictionTimelineFromState(
            int nodeId,
            double horizonEnd,
            double startEnergyJ,
            double startRateScale)
        {
            List<BprPredictionSegment> timeline = new List<BprPredictionSegment>();
            if (nodeId <= 0 || nodeId >= sensors.Length)
                return timeline;
            if (horizonEnd <= currentTime + Epsilon)
                return timeline;

            SensorState sensor = sensors[nodeId];
            List<RateChangeTemplate> futureRateChanges = new List<RateChangeTemplate>();
            List<double> breakpoints = new List<double>();
            AddUniqueBreakpoint(breakpoints, currentTime);
            AddUniqueBreakpoint(breakpoints, horizonEnd);
            List<RateChangeTemplate> nodeRateChanges = artifact.GetRateChangesForNode(nodeId);
            for (int i = 0; i < nodeRateChanges.Count; i++)
            {
                RateChangeTemplate change = nodeRateChanges[i];
                if (change.TimeSeconds <= currentTime + Epsilon ||
                    change.TimeSeconds > horizonEnd + Epsilon)
                    continue;
                futureRateChanges.Add(change);
                AddUniqueBreakpoint(breakpoints, change.TimeSeconds);
            }
            breakpoints.Sort();

            double predictedEnergy = startEnergyJ;
            double predictedRateScale = startRateScale;
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
            string reservedSignature = BuildReservedNodeSignature(reservedNodeIds);
            if (bprPredictedRequestCache != null &&
                Math.Abs(bprPredictedRequestCache.CurrentTimeSeconds - currentTime) <= Epsilon &&
                bprPredictedRequestCache.MissionId == missionId &&
                bprPredictedRequestCache.MaxTask == maxTask &&
                bprPredictedRequestCache.Version == predictionCacheVersion &&
                String.Equals(bprPredictedRequestCache.ReservedSignature, reservedSignature, StringComparison.Ordinal))
            {
                return new List<BprPredictedRequest>(bprPredictedRequestCache.Requests);
            }

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
            bprPredictedRequestCache = new BprPredictedRequestCache();
            bprPredictedRequestCache.CurrentTimeSeconds = currentTime;
            bprPredictedRequestCache.MissionId = missionId;
            bprPredictedRequestCache.MaxTask = maxTask;
            bprPredictedRequestCache.ReservedSignature = reservedSignature;
            bprPredictedRequestCache.Version = predictionCacheVersion;
            bprPredictedRequestCache.Requests = new List<BprPredictedRequest>(predictedRequests);
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

        private List<ChargingRequest> BuildChengBprPaperCplist(List<ChargingRequest> clist, int maxTask)
        {
            return BuildChengBprPaperCplist(clist, maxTask, BprProactiveSelectionMode.Random, false);
        }

        private List<ChargingRequest> BuildChengBprPaperCplist(List<ChargingRequest> clist, int maxTask, bool allowCapacityOverflow)
        {
            return BuildChengBprPaperCplist(clist, maxTask, BprProactiveSelectionMode.Random, allowCapacityOverflow);
        }

        private List<ChargingRequest> BuildChengBprPaperCplist(
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

            List<BprPredictedRequest> slist = BuildChengBprPaperCandidates(reservedNodeIds);
            Random paperRandom = CreateChengBprPaperRandom();
            int safety = 0;
            while ((allowCapacityOverflow || cplist.Count < maxTask) && slist.Count > 0 && safety < sensors.Length * 2 + 4)
            {
                safety++;
                List<BprWindow> windows = BuildChengBprPaperWindows(slist, maxTask);
                if (windows.Count == 0)
                {
                    WriteBprDebug(safety, null, null, "NO_CHENG_PAPER_OVERFLOW_WINDOW", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                BprWindow window = windows[0];
                int capacityLeft = allowCapacityOverflow
                    ? window.OverflowCount
                    : Math.Max(0, maxTask - cplist.Count);
                int addCount = Math.Min(capacityLeft, window.OverflowCount);
                if (addCount <= 0)
                {
                    WriteBprDebug(safety, window, null, "CHENG_PAPER_CAPACITY_FULL", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                List<BprPredictedRequest> selected = selectionMode == BprProactiveSelectionMode.RouteInsertionCost
                    ? SelectChengBprRouteInsertionNodes(window.Requests, addCount, cplist)
                    : SelectChengBprPaperRandomNodes(window.Requests, addCount, paperRandom);
                if (selected.Count == 0)
                {
                    WriteBprDebug(safety, window, null, "NO_CHENG_PAPER_RANDOM_NODE", cplist.Count, cplist.Count, maxTask, allowCapacityOverflow);
                    break;
                }

                for (int i = 0; i < selected.Count && (allowCapacityOverflow || cplist.Count < maxTask); i++)
                {
                    BprPredictedRequest request = selected[i];
                    BprRemovalDecision decision = CreateChengRemovalDecision(
                        request,
                        request.RouteInsertionCost,
                        selectionMode == BprProactiveSelectionMode.RouteInsertionCost
                            ? ChengBprRouteInsertionReason
                            : ChengBprPaperRandomReason);
                    int before = cplist.Count;
                    cplist.Add(CreateChengBprPaperProactiveRequest(request, decision.Reason));
                    reservedNodeIds.Add(request.NodeId);
                    RemoveBprPredictedRequestByNodeId(slist, request.NodeId);
                    WriteBprDebug(safety, window, decision, decision.Reason, before, cplist.Count, maxTask, allowCapacityOverflow);
                }
            }

            return cplist;
        }

        private List<BprPredictedRequest> BuildChengBprPaperCandidates(HashSet<int> reservedNodeIds)
        {
            List<BprPredictedRequest> candidates = new List<BprPredictedRequest>();
            if (bprSTableByNodeId == null)
                return candidates;

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

                SensorState sensor = sensors[nodeId];
                if (sensor == null || !sensor.Alive || !sensor.IsActive ||
                    sensor.HasPendingRequest || HasActiveRequestForNode(nodeId))
                    continue;
                if (!entry.IsAlive || entry.IsPendingRequest || entry.IsScheduledInCurrentMission)
                    continue;
                if (IsNodeReservedForCurrentMission(nodeId))
                    continue;
                if (Double.IsNaN(entry.LatestReportedDeadlineSeconds) ||
                    Double.IsInfinity(entry.LatestReportedDeadlineSeconds))
                    continue;

                double effectiveRate = sensor.ConsumeRateJPerSecond;
                if (effectiveRate <= 1e-12)
                    continue;

                BprPredictedRequest request = new BprPredictedRequest();
                request.NodeId = nodeId;
                request.RequestTimeSeconds = entry.LatestReportedDeadlineSeconds;
                request.DeathTimeSeconds = ComputeSensorDeathTimeSeconds(sensor);
                request.EnergyAtPredictionStartJ = sensor.EnergyJ;
                request.EffectiveConsumeRateJPerSecond = effectiveRate;
                request.SlackSeconds = request.DeathTimeSeconds - request.RequestTimeSeconds;
                request.RouteInsertionCost = 0.0;
                request.IsReserved = false;
                request.IsPendingRequest = false;
                request.IsScheduledInCurrentMission = false;
                candidates.Add(request);
            }

            candidates.Sort(CompareBprPredictedRequestByRequestTime);
            return candidates;
        }

        private List<BprWindow> BuildChengBprPaperWindows(List<BprPredictedRequest> slist, int maxTask)
        {
            List<BprWindow> windows = new List<BprWindow>();
            if (slist == null || slist.Count == 0)
                return windows;

            maxTask = Math.Max(1, maxTask);
            double tjob = EstimateBprTjobSeconds(maxTask);
            double threshold = GetBprDeadlineThresholdSeconds();
            for (int i = 0; i < slist.Count; i++)
            {
                BprPredictedRequest x = slist[i];
                double windowStart = x.RequestTimeSeconds;
                double windowEnd = windowStart + tjob;
                BprWindow window = new BprWindow();
                window.WindowStartSeconds = windowStart;
                window.WindowEndSeconds = windowEnd;
                window.Requests = new List<BprPredictedRequest>();

                for (int j = 0; j < slist.Count; j++)
                {
                    BprPredictedRequest y = slist[j];
                    double intervalStart = y.RequestTimeSeconds - threshold;
                    double intervalEnd = y.RequestTimeSeconds + threshold;
                    if (windowStart <= intervalEnd + Epsilon &&
                        windowEnd >= intervalStart - Epsilon)
                    {
                        window.Requests.Add(y);
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

        private Random CreateChengBprPaperRandom()
        {
            unchecked
            {
                int seed = artifact.Seed * 397 + missionId * 17 + StableStringHash(ChengBprPaperRandomReason);
                return new Random(seed & 0x7fffffff);
            }
        }

        private List<BprPredictedRequest> SelectChengBprPaperRandomNodes(
            List<BprPredictedRequest> bottleList,
            int addCount,
            Random random)
        {
            List<BprPredictedRequest> selected = new List<BprPredictedRequest>();
            if (bottleList == null || addCount <= 0)
                return selected;

            List<BprPredictedRequest> selectable = new List<BprPredictedRequest>(bottleList);
            Random effectiveRandom = random ?? algorithmRandom;
            while (selectable.Count > 0 && selected.Count < addCount)
            {
                int index = effectiveRandom.Next(selectable.Count);
                BprPredictedRequest picked = selectable[index];
                selectable.RemoveAt(index);
                selected.Add(picked);
            }

            return selected;
        }

        private List<BprPredictedRequest> SelectChengBprRouteInsertionNodes(
            List<BprPredictedRequest> bottleList,
            int addCount,
            List<ChargingRequest> cplist)
        {
            List<BprPredictedRequest> selected = new List<BprPredictedRequest>();
            if (bottleList == null || addCount <= 0)
                return selected;

            List<BprPredictedRequest> selectable = new List<BprPredictedRequest>(bottleList);
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
                    compare = a.RequestTimeSeconds.CompareTo(b.RequestTimeSeconds);
                    if (compare != 0)
                        return compare;
                    compare = a.DeathTimeSeconds.CompareTo(b.DeathTimeSeconds);
                    if (compare != 0)
                        return compare;
                    return a.NodeId.CompareTo(b.NodeId);
                });

                BprPredictedRequest picked = selectable[0];
                selectable.RemoveAt(0);
                picked.RouteInsertionCost = ComputeRouteInsertionCost(picked.NodeId, currentRoute);
                selected.Add(picked);
                previewCplist.Add(CreateChengBprPaperProactiveRequest(picked, ChengBprRouteInsertionReason));
            }

            return selected;
        }

        private void RemoveBprPredictedRequestByNodeId(List<BprPredictedRequest> requests, int nodeId)
        {
            if (requests == null)
                return;
            for (int i = requests.Count - 1; i >= 0; i--)
            {
                if (requests[i].NodeId == nodeId)
                    requests.RemoveAt(i);
            }
        }

        private ChargingRequest CreateChengBprPaperProactiveRequest(BprPredictedRequest predicted)
        {
            return CreateChengBprPaperProactiveRequest(predicted, ChengBprPaperRandomReason);
        }

        private ChargingRequest CreateChengBprPaperProactiveRequest(BprPredictedRequest predicted, string proactiveReason)
        {
            ChargingRequest proactive = new ChargingRequest();
            proactive.RequestId = -predicted.NodeId;
            proactive.NodeId = predicted.NodeId;
            proactive.RequestTimeSeconds = currentTime;
            proactive.DeadlineSeconds = predicted.DeathTimeSeconds;
            if (predicted.NodeId > 0 && predicted.NodeId < sensors.Length)
            {
                SensorState sensor = sensors[predicted.NodeId];
                proactive.RequestEnergyJ = sensor.EnergyJ;
                PopulateChargingRequestEnergyFields(proactive, sensor);
                if (Double.IsNaN(proactive.DeadlineSeconds) ||
                    Double.IsInfinity(proactive.DeadlineSeconds))
                {
                    proactive.DeadlineSeconds = ComputeSensorDeathTimeSeconds(sensor);
                }
            }
            proactive.EffectiveConsumeRateJPerSecond = predicted.EffectiveConsumeRateJPerSecond;
            proactive.CriticalDensity = 0.0;
            proactive.IsProactive = true;
            proactive.ProactiveReason = proactiveReason;
            return proactive;
        }

        private double ComputeSensorDeathTimeSeconds(SensorState sensor)
        {
            if (sensor == null || sensor.ConsumeRateJPerSecond <= 1e-12)
                return Double.PositiveInfinity;
            if (sensor.EnergyJ <= Epsilon)
                return currentTime;
            return currentTime + sensor.EnergyJ / sensor.ConsumeRateJPerSecond;
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
            string reservedSignature = BuildReservedNodeSignature(reservedNodeIds);
            if (yuPredictedIntervalCache != null &&
                Math.Abs(yuPredictedIntervalCache.CurrentTimeSeconds - currentTime) <= Epsilon &&
                yuPredictedIntervalCache.MissionId == missionId &&
                yuPredictedIntervalCache.MaxTask == maxTask &&
                yuPredictedIntervalCache.Version == predictionCacheVersion &&
                String.Equals(yuPredictedIntervalCache.ReservedSignature, reservedSignature, StringComparison.Ordinal))
            {
                return new List<YuPredictedInterval>(yuPredictedIntervalCache.Intervals);
            }

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
            yuPredictedIntervalCache = new YuPredictedIntervalCache();
            yuPredictedIntervalCache.CurrentTimeSeconds = currentTime;
            yuPredictedIntervalCache.MissionId = missionId;
            yuPredictedIntervalCache.MaxTask = maxTask;
            yuPredictedIntervalCache.ReservedSignature = reservedSignature;
            yuPredictedIntervalCache.Version = predictionCacheVersion;
            yuPredictedIntervalCache.Intervals = new List<YuPredictedInterval>(intervals);
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

            maxTask = Math.Max(1, maxTask);
            double windowSize = EstimateBprTjobSeconds(maxTask);
            List<double> windowStarts = new List<double>();
            for (int i = 0; i < intervals.Count; i++)
            {
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
                window.KStar = maxTask + 1;
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
                window.RemovalNeededCount = Math.Max(0, window.DangerCount - maxTask);
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
            compare = a.WindowStartSeconds.CompareTo(b.WindowStartSeconds);
            if (compare != 0)
                return compare;
            compare = b.DangerCount.CompareTo(a.DangerCount);
            if (compare != 0)
                return compare;
            return a.WindowEndSeconds.CompareTo(b.WindowEndSeconds);
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

        private BprRemovalDecision CreateChengRemovalDecision(
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

        private static bool IsFiniteTime(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
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
            List<ChargingRequest> previewCplist = CloneRequestList(cplist);
            while (selectable.Count > 0 && selected.Count < addCount)
            {
                List<ChargingRequest> currentRoute = BuildNearestRoute(previewCplist, previewCplist.Count);
                YuPredictedInterval picked;
                if (selectionMode == YuProactiveSelectionMode.RouteInsertionCost)
                {
                    selectable.Sort(delegate (YuPredictedInterval a, YuPredictedInterval b)
                    {
                        double da = ComputeRouteInsertionCost(a.NodeId, currentRoute);
                        double db = ComputeRouteInsertionCost(b.NodeId, currentRoute);
                        int compare = da.CompareTo(db);
                        if (compare != 0)
                            return compare;
                        compare = a.CenterRequestTimeSeconds.CompareTo(b.CenterRequestTimeSeconds);
                        if (compare != 0)
                            return compare;
                        compare = a.IntervalStartSeconds.CompareTo(b.IntervalStartSeconds);
                        if (compare != 0)
                            return compare;
                        compare = a.IntervalEndSeconds.CompareTo(b.IntervalEndSeconds);
                        if (compare != 0)
                            return compare;
                        return a.NodeId.CompareTo(b.NodeId);
                    });
                    picked = selectable[0];
                    selectable.RemoveAt(0);
                }
                else
                {
                    int index = algorithmRandom.Next(selectable.Count);
                    picked = selectable[index];
                    selectable.RemoveAt(index);
                }
                double routeInsertionCost = ComputeRouteInsertionCost(picked.NodeId, currentRoute);
                YuRemovalDecision decision = CreateYuRemovalDecision(
                    picked,
                    routeInsertionCost,
                    selectionMode == YuProactiveSelectionMode.RouteInsertionCost
                        ? "YU_ROUTE_COST_INTERVAL_REMOVAL"
                        : "YU_RANDOM_INTERVAL_REMOVAL");
                selected.Add(decision);
                previewCplist.Add(CreateYuProactiveRequest(decision));
            }

            return selected;
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
                PopulateChargingRequestEnergyFields(proactive, sensor);
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
            return ResolveBprTimeBaseSeconds(maxTask, "目前設定不可行：ThresholdMode = Percent 時，BP&R 預測範圍不可隱性使用 TreqSeconds。" + Environment.NewLine +
                "請明確設定 ProactivePredictionHorizonSeconds，或改用 ChengTreq / TreqSeconds 模式。") +
                EstimateBprTjobSeconds(maxTask);
        }

        private double ResolveProactiveCooldownSeconds()
        {
            if (settings.ProactiveCooldownSeconds > 0.0)
                return settings.ProactiveCooldownSeconds;
            return ResolveBprTimeBaseSeconds(GetMissionTaskLimit(),
                "目前設定不可行：ThresholdMode = Percent 時，BP&R cooldown 不可隱性使用 TreqSeconds。" + Environment.NewLine +
                "請明確設定 ProactiveCooldownSeconds，或改用 ChengTreq / TreqSeconds 模式。");
        }

        private double ResolveBprTimeBaseSeconds(int maxTask, string percentModeErrorMessage)
        {
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode))
                return ComputeChengTreqSeconds(maxTask);
            if (ChengTreqCalculator.IsTreqSecondsMode(settings.ThresholdMode))
                return Math.Max(0.0, settings.TreqSeconds);
            throw new InvalidOperationException(percentModeErrorMessage);
        }

        private double GetEffectiveTreqSeconds()
        {
            return GetEffectiveTreqSeconds(GetMissionTaskLimit());
        }

        private double GetEffectiveTreqSeconds(int maxTask)
        {
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode))
                return ComputeChengTreqSeconds(maxTask);
            if (ChengTreqCalculator.IsTreqSecondsMode(settings.ThresholdMode))
                return Math.Max(0.0, settings.TreqSeconds);
            return 0.0;
        }

        private ChengTreqMetrics ComputeChengTreqMetrics(int maxTask)
        {
            return ChengTreqCalculator.Compute(settings, maxTask);
        }

        private double ComputeChengTreqSeconds(int maxTask)
        {
            return ComputeChengTreqMetrics(maxTask).TreqSeconds;
        }

        private static int CompareBprSTableByDeadline(BprSTableEntry a, BprSTableEntry b)
        {
            int compare = a.LatestReportedDeadlineSeconds.CompareTo(b.LatestReportedDeadlineSeconds);
            if (compare != 0)
                return compare;
            return a.NodeId.CompareTo(b.NodeId);
        }

        private double GetBprDeadlineThresholdSeconds()
        {
            if (settings.BprDeadlineThresholdSeconds <= 0.0)
                return ResolveBprTimeBaseSeconds(GetMissionTaskLimit(),
                    "目前設定不可行：ThresholdMode = Percent 時，BP&R deadline threshold 不可隱性使用 TreqSeconds。" + Environment.NewLine +
                    "請明確設定 BprDeadlineThresholdSeconds，或改用 ChengTreq / TreqSeconds 模式。");
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode) &&
                settings.BprDeadlineThresholdSeconds <= 1.0 + Epsilon)
                return GetEffectiveTreqSeconds();
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
            return Math.Max(1.0, ComputeChengTreqMetrics(maxTask).TjobSeconds);
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
                    double routingRatio = 0.0;
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
            double radius = settings.CriticalDensityRadiusMeters;
            int total = 0;
            int critical = 0;
            for (int i = 1; i < sensors.Length; i++)
            {
                if (i == nodeId || !sensors[i].Alive || !sensors[i].IsActive)
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

                ApplyActivationsAtCurrentTime();
                ApplyRateChangesAtCurrentTime();
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
            if (nextActivationIndex < artifact.ActivationEvents.Count)
                next = Math.Min(next, artifact.ActivationEvents[nextActivationIndex].TimeSeconds);
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
                if (!sensor.Alive || !sensor.IsActive ||
                    sensor.HasPendingRequest || IsNodeReservedForCurrentMission(id))
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
                if (!sensor.Alive || !sensor.IsActive)
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
                if (!sensor.Alive || !sensor.IsActive)
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

        private void ApplyActivationsAtCurrentTime()
        {
            while (nextActivationIndex < artifact.ActivationEvents.Count &&
                artifact.ActivationEvents[nextActivationIndex].TimeSeconds <= currentTime + Epsilon)
            {
                ActivationEventTemplate activation = artifact.ActivationEvents[nextActivationIndex];
                if (activation.NodeId > 0 && activation.NodeId < sensors.Length)
                {
                    SensorState sensor = sensors[activation.NodeId];
                    if (!sensor.IsActive && sensor.Alive)
                    {
                        sensor.Activate(currentTime, settings);
                        RefreshBprSTableEntry(activation.NodeId, "activation", true);
                    }
                }
                nextActivationIndex++;
            }
        }

        private void ApplyRateChangesAtCurrentTime()
        {
            while (nextRateChangeIndex < artifact.RateChanges.Count &&
                artifact.RateChanges[nextRateChangeIndex].TimeSeconds <= currentTime + Epsilon)
            {
                RateChangeTemplate change = artifact.RateChanges[nextRateChangeIndex];
                if (change.NodeId > 0 && change.NodeId < sensors.Length &&
                    sensors[change.NodeId].Alive && sensors[change.NodeId].IsActive)
                {
                    SensorState sensor = sensors[change.NodeId];
                    double oldRate = sensor.ConsumeRateJPerSecond;
                    BprSTableEntry entry = GetOrCreateBprSTableEntry(change.NodeId);
                    double oldDeadline = entry.LatestReportedDeadlineSeconds;
                    sensor.RateScale *= change.Multiplier;
                    sensor.RefreshConsumeRate(settings);
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

        private void CreateRequestsAtCurrentTime()
        {
            for (int id = 1; id < sensors.Length; id++)
            {
                SensorState sensor = sensors[id];
                if (!sensor.Alive || !sensor.IsActive ||
                    sensor.HasPendingRequest || IsNodeReservedForCurrentMission(id))
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
                    PopulateChargingRequestEnergyFields(request, sensor);
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
            if (sensor == null)
                return 0.0;

            if (ChengTreqCalculator.IsTimeThresholdMode(settings.ThresholdMode))
            {
                return Math.Min(sensor.CapacityJ * 0.95,
                    sensor.ConsumeRateJPerSecond * GetEffectiveTreqSeconds());
            }

            return Math.Min(sensor.CapacityJ * 0.95,
                sensor.CapacityJ * settings.RequestThresholdPercent / 100.0);
        }

        private void CheckDeadSensors(string directReason)
        {
            for (int id = 1; id < sensors.Length; id++)
            {
                if (sensors[id].Alive && sensors[id].IsActive && sensors[id].EnergyJ <= Epsilon)
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
            bool schedulingRelated = hasPendingRequestAtDeath || wasScheduledInCurrentMissionAtDeath;
            string reason = directCause;
            string reasonZh = ReasonZh(reason);
            string directEnergyCauseZh = ReasonZh(directCause);
            string schedulingCause = schedulingRelated ? "scheduling_wait" : "";
            string schedulingCauseZh = schedulingRelated ? ReasonZh("scheduling_wait") : "";

            sensor.Alive = false;
            sensor.EnergyJ = 0.0;

            ExperimentDeathRecord death = new ExperimentDeathRecord();
            CopySweepFieldsTo(death);
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
            death.EnergyBeforeDeathJ = energyBeforeDeathJ;
            death.BaseConsumeRateJPerSecondAtDeath = sensor.ConsumeRateJPerSecond;
            death.EffectiveConsumeRateJPerSecondAtDeath = sensor.ConsumeRateJPerSecond;
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
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_BPR") == "NJF_CHENG_BPR",
                    "Legacy NJF_BPR key should map to the paper-compatible NJF_CHENG_BPR wrapper.");
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_BPR_ROUTE_SAFE_LIMITED") == "NJF_ROUTE_CHENG_BPR_LIMITED",
                    "Legacy limited route-safe key should map to NJF_ROUTE_CHENG_BPR_LIMITED.");
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_BPR_ROUTE_SAFE_EXTENDED") == "NJF_ROUTE_CHENG_BPR_EXTENDED",
                    "Legacy extended route-safe key should map to NJF_ROUTE_CHENG_BPR_EXTENDED.");
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_ROUTE_ZHENG_BPR_LIMITED") == "NJF_ROUTE_CHENG_BPR_LIMITED",
                    "Legacy route zheng limited key should normalize to NJF_ROUTE_CHENG_BPR_LIMITED.");
                AssertSelfTest(ExperimentSettings.CanonicalAlgorithmKey("NJF_ROUTE_ZHENG_BPR_EXTENDED") == "NJF_ROUTE_CHENG_BPR_EXTENDED",
                    "Legacy route zheng extended key should normalize to NJF_ROUTE_CHENG_BPR_EXTENDED.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_CHENG_BPR") >= 0,
                    "AllAlgorithms should include the paper-compatible NJF_CHENG_BPR wrapper.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "TADP_CHENG_BPR") >= 0,
                    "AllAlgorithms should include the paper-compatible TADP_CHENG_BPR wrapper.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "EDF_CHENG_BPR") >= 0,
                    "AllAlgorithms should include the paper-compatible EDF_CHENG_BPR wrapper.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_YU_BPR") >= 0,
                    "AllAlgorithms should include NJF_YU_BPR.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ROUTE_CHENG_BPR_LIMITED") >= 0 &&
                    Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ROUTE_CHENG_BPR_EXTENDED") >= 0 &&
                    Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ROUTE_YU_BPR_LIMITED") >= 0 &&
                    Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ROUTE_YU_BPR_EXTENDED") >= 0,
                    "AllAlgorithms should include the official ROUTE_CHENG_BPR and ROUTE_YU_BPR variants.");
                AssertSelfTest(Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ZHENG_BPR") < 0 &&
                    Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ROUTE_ZHENG_BPR_LIMITED") < 0 &&
                    Array.IndexOf(ExperimentSettings.AllAlgorithms(), "NJF_ROUTE_ZHENG_BPR_EXTENDED") < 0,
                    "AllAlgorithms should not expose deprecated ZHENG BP&R algorithm keys.");

                RunNormalizeThenValidateOrderingSelfTest(tempDirectory);
                RunSweepTreqSecondsSelfTest(tempDirectory);
                RunWcvReturnReserveSelfTest(tempDirectory, simulations);
                RunWcvMaxTaskFeasibilitySelfTest();
                RunChengPaperBprWrapperSelfTest(tempDirectory, simulations);
                RunChengPaperRandomPoolSelfTest(tempDirectory, simulations);
                RunChengActivationGenerationSelfTest(tempDirectory);
                RunCsvOutputSelectionSelfTest(tempDirectory);
                RunStandaloneDispatchSelfTest(tempDirectory, simulations);
                RunActiveRequestProactiveSelfTest(tempDirectory, simulations);
                RunProactiveCandidateFilterSelfTest(tempDirectory, simulations);
                RunSimulationEndBeforeFullSelfTest(tempDirectory, simulations);
                RunDeathReasonSelfTest(tempDirectory, simulations);
                RunCompleteBprYuPredictionSelfTest(tempDirectory, simulations);

                ExperimentArtifact maintenanceArtifact = CreateBprSelfTestArtifact(new double[] { 100.0, 100.0, 100.0 });
                ExperimentSimulation maintenanceSimulation = new ExperimentSimulation(
                    maintenanceSettings,
                    maintenanceArtifact,
                    "NJF_ROUTE_CHENG_BPR_LIMITED",
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
                    "NJF_ROUTE_CHENG_BPR_LIMITED",
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
                    "NJF_ROUTE_CHENG_BPR_LIMITED",
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
                    YuProactiveSelectionMode.Random);
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
                    YuProactiveSelectionMode.Random);
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
                List<ChargingRequest> scheduledTrial = maintenanceSimulation.BuildChengBprPaperCplist(
                    new List<ChargingRequest>(),
                    1);
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
                    "NJF_ROUTE_CHENG_BPR_LIMITED",
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
                    "run001-seed777-NJF_ROUTE_CHENG_BPR_LIMITED-task-records.csv");
                AssertSelfTest(File.Exists(taskPath), "Task-record CSV was not written by the self-test.");
                string[] taskLines = File.ReadAllLines(taskPath, Encoding.UTF8);
                AssertSelfTest(taskLines.Length == 2,
                    "Task-record CSV should contain one data row for the mission.");

                string[] taskHeaders = taskLines[0].Split(',');
                Dictionary<string, int> taskColumns = BuildCsvHeaderMap(taskHeaders);
                string[] fields = taskLines[1].Split(',');
                int missionColumn = taskColumns["MissionId"];
                int nodeColumn = taskColumns["NodeId"];
                int proactiveColumn = taskColumns["IsProactive"];
                AssertSelfTest(fields.Length > proactiveColumn && fields[missionColumn] == "1" && fields[nodeColumn] == "1",
                    "Task-record CSV row does not describe mission 1 / sensor 1.");
                AssertSelfTest(String.Equals(fields[proactiveColumn], "True", StringComparison.OrdinalIgnoreCase),
                    "Reserved node should remain a proactive task, not be relabeled as natural.");
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

        private static void RunCsvOutputSelectionSelfTest(string tempDirectory)
        {
            ExperimentSettings legacyDisabled = CreateBprSelfTestSettings(tempDirectory);
            legacyDisabled.WriteTaskDetailCsv = false;
            legacyDisabled.Normalize();
            AssertSelfTest(!legacyDisabled.HasAnyTaskDetailCsvOutput(),
                "Legacy disabled CSV setting should disable every detail CSV output.");

            ExperimentSettings taskOnly = CreateBprSelfTestSettings(tempDirectory);
            taskOnly.WriteMissionDetailsCsv = false;
            taskOnly.WriteTaskRecordsCsv = true;
            taskOnly.WriteBprDebugCsv = false;
            taskOnly.WriteYuBprDebugCsv = false;
            taskOnly.WriteTaskDetailCsv = true;
            taskOnly.Normalize();
            AssertSelfTest(taskOnly.HasAnyTaskDetailCsvOutput(),
                "A single selected detail CSV should keep CSV output enabled.");
            AssertSelfTest(!taskOnly.WriteMissionDetailsCsv && taskOnly.WriteTaskRecordsCsv &&
                !taskOnly.WriteBprDebugCsv && !taskOnly.WriteYuBprDebugCsv,
                "Normalize should preserve individual CSV selections when at least one output is enabled.");

            string selectionDirectory = Path.Combine(tempDirectory, "csv-selection");
            MissionDetailCsvOutputOptions options = new MissionDetailCsvOutputOptions();
            options.WriteMissionDetails = false;
            options.WriteTaskRecords = true;
            options.WriteBprDebug = false;
            options.WriteYuBprDebug = false;

            ExperimentArtifact artifact = CreateBprSelfTestArtifact(new double[] { 80.0 });
            using (MissionDetailCsvWriter writer = new MissionDetailCsvWriter(
                selectionDirectory, artifact, "EDF", options))
            {
                MissionRecord mission = new MissionRecord();
                mission.RunIndex = artifact.RunIndex;
                mission.Seed = artifact.Seed;
                mission.Algorithm = "EDF";
                mission.MissionId = 1;
                mission.RouteNodeIds = new List<int>();
                writer.WriteMission(mission);

                ExperimentTaskRecord task = new ExperimentTaskRecord();
                task.RunIndex = artifact.RunIndex;
                task.Seed = artifact.Seed;
                task.Algorithm = "EDF";
                task.ArtifactHash = artifact.ArtifactHash;
                task.MissionId = 1;
                task.NodeId = 1;
                task.TaskSource = "request";
                task.FailureReason = "";
                writer.WriteTask(task);

                writer.WriteBprDebug(artifact.RunIndex, artifact.Seed, "EDF", 1, 0.0, 0,
                    0.0, 0.0, 0, 0, -1, Double.NaN, Double.NaN, Double.NaN,
                    Double.NaN, Double.NaN, "test", 0, 0, 1, false);
                writer.WriteYuBprDebug(artifact.RunIndex, artifact.Seed, "EDF", 1, 0.0, 0,
                    0.0, 0.0, 0, 0, 0, -1, Double.NaN, Double.NaN, Double.NaN,
                    Double.NaN, Double.NaN, Double.NaN, Double.NaN, Double.NaN,
                    Double.NaN, "test", 0, 0, 1, false);
            }

            string taskOnlyPath = Path.Combine(selectionDirectory, "run001-seed777-EDF-task-records.csv");
            AssertSelfTest(!File.Exists(Path.Combine(selectionDirectory, "run001-seed777-EDF-mission-details.csv")),
                "Mission detail CSV should not be written when disabled.");
            AssertSelfTest(File.Exists(taskOnlyPath),
                "Task record CSV should be written when selected.");
            AssertCsvHeaderEquals(taskOnlyPath, ExpectedTaskRecordHeader(),
                "Task-record CSV should use the requested Chinese columns.");
            AssertCsvDataColumnCounts(taskOnlyPath, ExpectedTaskRecordHeader().Length);
            AssertSelfTest(!File.Exists(Path.Combine(selectionDirectory, "run001-seed777-EDF-bpr-debug.csv")),
                "BP&R debug CSV should not be written when disabled.");
            AssertSelfTest(!File.Exists(Path.Combine(selectionDirectory, "run001-seed777-EDF-yu-bpr-debug.csv")),
                "YU BP&R debug CSV should not be written when disabled.");

            RunCsvHeaderAndBprRuleSelfTest(tempDirectory);
        }

        private static void RunCsvHeaderAndBprRuleSelfTest(string tempDirectory)
        {
            MissionDetailCsvOutputOptions allOptions = MissionDetailCsvOutputOptions.All();
            ExperimentArtifact artifact = CreateBprSelfTestArtifact(new double[] { 80.0, 70.0 });

            string edfDirectory = Path.Combine(tempDirectory, "csv-edf");
            using (MissionDetailCsvWriter writer = new MissionDetailCsvWriter(edfDirectory, artifact, "EDF", allOptions))
            {
                WriteSampleCsvRows(writer, artifact);
                WriteSampleBprRows(writer, artifact, "EDF");
            }
            AssertCsvHeaderEquals(Path.Combine(edfDirectory, "run001-seed777-EDF-mission-details.csv"),
                ExpectedMissionDetailHeader(), "Mission detail CSV should use the requested Chinese columns.");
            AssertCsvDataColumnCounts(Path.Combine(edfDirectory, "run001-seed777-EDF-mission-details.csv"),
                ExpectedMissionDetailHeader().Length);
            AssertCsvHeaderEquals(Path.Combine(edfDirectory, "run001-seed777-EDF-task-records.csv"),
                ExpectedTaskRecordHeader(), "Task record CSV should use the requested Chinese columns.");
            AssertCsvDataColumnCounts(Path.Combine(edfDirectory, "run001-seed777-EDF-task-records.csv"),
                ExpectedTaskRecordHeader().Length);
            AssertSelfTest(!File.Exists(Path.Combine(edfDirectory, "run001-seed777-EDF-bpr-debug.csv")),
                "Non-BP&R algorithms must not write bpr-debug CSV.");
            AssertSelfTest(!File.Exists(Path.Combine(edfDirectory, "run001-seed777-EDF-yu-bpr-debug.csv")),
                "Non-BP&R algorithms must not write yu-bpr-debug CSV.");

            string chengDirectory = Path.Combine(tempDirectory, "csv-cheng");
            using (MissionDetailCsvWriter writer = new MissionDetailCsvWriter(
                chengDirectory, artifact, "NJF_ROUTE_CHENG_BPR_LIMITED", allOptions))
            {
                WriteSampleBprRows(writer, artifact, "NJF_ROUTE_CHENG_BPR_LIMITED");
            }
            string chengBprPath = Path.Combine(chengDirectory,
                "run001-seed777-NJF_ROUTE_CHENG_BPR_LIMITED-bpr-debug.csv");
            AssertCsvHeaderEquals(chengBprPath, ExpectedBprDebugHeader(),
                "CHENG BP&R debug CSV should use the requested Chinese columns.");
            AssertCsvDataColumnCounts(chengBprPath, ExpectedBprDebugHeader().Length);
            AssertSelfTest(!File.Exists(Path.Combine(chengDirectory,
                "run001-seed777-NJF_ROUTE_CHENG_BPR_LIMITED-yu-bpr-debug.csv")),
                "CHENG BP&R algorithms must not write yu-bpr-debug CSV.");

            string yuDirectory = Path.Combine(tempDirectory, "csv-yu");
            using (MissionDetailCsvWriter writer = new MissionDetailCsvWriter(
                yuDirectory, artifact, "NJF_ROUTE_YU_BPR_LIMITED", allOptions))
            {
                WriteSampleBprRows(writer, artifact, "NJF_ROUTE_YU_BPR_LIMITED");
            }
            string yuBprPath = Path.Combine(yuDirectory,
                "run001-seed777-NJF_ROUTE_YU_BPR_LIMITED-yu-bpr-debug.csv");
            AssertCsvHeaderEquals(yuBprPath, ExpectedYuBprDebugHeader(),
                "YU BP&R debug CSV should use the requested Chinese columns.");
            AssertCsvDataColumnCounts(yuBprPath, ExpectedYuBprDebugHeader().Length);
            AssertSelfTest(!File.Exists(Path.Combine(yuDirectory,
                "run001-seed777-NJF_ROUTE_YU_BPR_LIMITED-bpr-debug.csv")),
                "YU BP&R algorithms must not write bpr-debug CSV.");

            string chengNoRowsDirectory = Path.Combine(tempDirectory, "csv-cheng-no-rows");
            using (MissionDetailCsvWriter writer = new MissionDetailCsvWriter(
                chengNoRowsDirectory, artifact, "NJF_ROUTE_CHENG_BPR_LIMITED", allOptions))
            {
                WriteSampleCsvRows(writer, artifact);
            }
            AssertSelfTest(!File.Exists(Path.Combine(chengNoRowsDirectory,
                "run001-seed777-NJF_ROUTE_CHENG_BPR_LIMITED-bpr-debug.csv")),
                "BP&R debug CSV should not be created with only a header and no data rows.");
        }

        private static void WriteSampleCsvRows(MissionDetailCsvWriter writer, ExperimentArtifact artifact)
        {
            MissionRecord mission = new MissionRecord();
            mission.SweepParameterName = "";
            mission.RunIndex = artifact.RunIndex;
            mission.Seed = artifact.Seed;
            mission.Algorithm = "EDF";
            mission.MissionId = 1;
            mission.DispatchTimeSeconds = 1.0;
            mission.ReturnTimeSeconds = 2.0;
            mission.NodeCount = 1;
            mission.OnDemandRequestCount = 1;
            mission.RouteNodeIds = new List<int>();
            mission.RouteNodeIds.Add(1);
            writer.WriteMission(mission);

            ExperimentTaskRecord task = new ExperimentTaskRecord();
            task.RunIndex = artifact.RunIndex;
            task.Seed = artifact.Seed;
            task.Algorithm = "EDF";
            task.ArtifactHash = artifact.ArtifactHash;
            task.MissionId = 1;
            task.TaskOrder = 1;
            task.NodeId = 1;
            task.IsProactive = true;
            task.RequestTimeSeconds = 1.0;
            task.DeadlineSeconds = 5.0;
            task.DispatchTimeSeconds = 1.0;
            task.ArrivalTimeSeconds = 2.0;
            task.WaitSeconds = 1.0;
            task.ChargeStartSeconds = 2.0;
            task.ChargeEndSeconds = 3.0;
            task.EnergyBeforeJ = 10.0;
            task.ConsumeRateJPerSecond = 0.1;
            task.EffectiveConsumeRateJPerSecond = 0.1;
            task.InternalRateNjPerTick = 1000.0;
            task.DistanceFromPreviousMeters = 10.0;
            task.Success = true;
            task.FailureReason = "";
            task.WcvEnergyAfterJ = 999.0;
            writer.WriteTask(task);

        }

        private static void WriteSampleBprRows(MissionDetailCsvWriter writer, ExperimentArtifact artifact, string algorithm)
        {
            writer.WriteBprDebug(artifact.RunIndex, artifact.Seed, algorithm, 1, 1.0, 1,
                1.0, 2.0, 3, 1, 1, 1.0, 5.0, 4.0, 0.2, 10.0,
                "test", 1, 2, 3, false);
            writer.WriteYuBprDebug(artifact.RunIndex, artifact.Seed, algorithm, 1, 1.0, 1,
                1.0, 2.0, 3, 4, 1, 1, 1.0, 1.5, 2.0, 4.0, 3.5,
                2.5, 0.5, 0.2, 10.0, "test", 1, 2, 3, true);
        }

        private static void AssertCsvHeaderEquals(string path, string[] expected, string message)
        {
            AssertSelfTest(File.Exists(path), "Expected CSV was not written: " + path);
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            AssertSelfTest(lines.Length > 0, "CSV should contain a header: " + path);
            string[] actual = lines[0].Split(',');
            AssertSelfTest(actual.Length == expected.Length, message + " Header length mismatch.");
            for (int i = 0; i < expected.Length; i++)
                AssertSelfTest(actual[i] == expected[i], message + " Header mismatch at column " + i.ToString(CultureInfo.InvariantCulture) + ".");
        }

        private static void AssertCsvDataColumnCounts(string path, int expectedCount)
        {
            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            AssertSelfTest(lines.Length > 1, "CSV should contain at least one data row: " + path);
            for (int i = 1; i < lines.Length; i++)
            {
                string[] fields = lines[i].Split(',');
                AssertSelfTest(fields.Length == expectedCount,
                    "CSV row column count does not match the header in " + path + " line " + (i + 1).ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private static string[] ExpectedMissionDetailHeader()
        {
            return new string[] { "SweepParameterName", "MissionId", "DispatchTimeSeconds", "ReturnTimeSeconds",
                "NodeCount", "OnDemandRequestCount", "ProactiveCount", "SuccessfulCharges", "FailedCount",
                "DistanceMeters", "AverageWaitSeconds", "RouteNodeIds", "DeduplicatedTaskCount" };
        }

        private static string[] ExpectedTaskRecordHeader()
        {
            return new string[] { "MissionId", "TaskOrder", "NodeId", "IsProactive", "RequestTimeSeconds",
                "DeadlineSeconds", "DispatchTimeSeconds", "ArrivalTimeSeconds", "WaitSeconds",
                "ChargeStartSeconds", "ChargeEndSeconds", "EnergyBeforeJ", "ConsumeRateJPerSecond",
                "EffectiveConsumeRateJPerSecond", "RequestNodeConsumeRateJPerSecond",
                "ServiceNodeConsumeRateJPerSecond", "NodeConsumeRatePredictionErrorJPerSecond",
                "InternalRateNjPerTick", "DistanceFromPreviousMeters", "Success", "FailureReason",
                "WcvEnergyAfterJ" };
        }

        private static string[] ExpectedBprDebugHeader()
        {
            return new string[] { "任務編號", "目前時間秒", "迭代次數", "視窗開始時間秒", "視窗結束時間秒",
                "瓶頸數量", "超出數量", "選中節點編號", "選中節點請求時間秒", "選中節點死亡時間秒",
                "選中節點安全餘裕秒", "選中節點有效耗電率J每秒", "選中節點路線插入成本",
                "加入前候選任務數", "加入後候選任務數", "單趟任務上限" };
        }

        private static string[] ExpectedYuBprDebugHeader()
        {
            return new string[] { "任務編號", "目前時間秒", "迭代次數", "視窗開始時間秒", "視窗結束時間秒",
                "危險門檻", "危險數量", "需要移除數量", "選中節點編號", "選中區間開始時間秒",
                "選中中心請求時間秒", "選中區間結束時間秒", "選中最早死亡時間秒",
                "選中最晚安全服務時間秒", "選中安全餘裕秒", "選中不確定範圍秒",
                "選中有效耗電率J每秒", "選中路線插入成本", "加入前候選任務數",
                "加入後候選任務數", "單趟任務上限", "是否允許超出容量" };
        }

        private static void RunNormalizeThenValidateOrderingSelfTest(string tempDirectory)
        {
            ExperimentSettings batchSettings = CreateBprSelfTestSettings(Path.Combine(tempDirectory, "normalize-batch"));
            DisableTaskDetailCsv(batchSettings);
            batchSettings.SelectedAlgorithmsCsv = "NJF";
            batchSettings.SensorCount = 2;
            batchSettings.SimulationTimeSeconds = 1.0;
            batchSettings.EventRatePerSecond = 0.0;
            batchSettings.NmaxTask = 0;
            new ExperimentBatchRunner(null, false).Run(batchSettings);
            AssertSelfTest(batchSettings.NmaxTask == 1,
                "ExperimentBatchRunner.Run must normalize raw settings before validation.");

            ExperimentSettings sweepSettings = CreateBprSelfTestSettings(Path.Combine(tempDirectory, "normalize-sweep"));
            DisableTaskDetailCsv(sweepSettings);
            sweepSettings.SelectedAlgorithmsCsv = "NJF";
            sweepSettings.SensorCount = 2;
            sweepSettings.SimulationTimeSeconds = 1.0;
            sweepSettings.EventRatePerSecond = 0.0;
            sweepSettings.SweepEnabled = true;
            sweepSettings.SweepParameterKey = "SensorCount";
            sweepSettings.SweepIterationCount = 0;
            sweepSettings.SweepStepValue = 1.0;
            sweepSettings.NmaxTask = 0;
            new ExperimentSweepBatchRunner(null, false).Run(sweepSettings);
            AssertSelfTest(sweepSettings.NmaxTask == 1,
                "ExperimentSweepBatchRunner.Run must normalize raw settings before validation.");
        }

        private static void RunSweepTreqSecondsSelfTest(string tempDirectory)
        {
            ExperimentSettings settings = CreateBprSelfTestSettings(Path.Combine(tempDirectory, "sweep-treq"));
            DisableTaskDetailCsv(settings);
            settings.SelectedAlgorithmsCsv = "NJF";
            settings.SensorCount = 2;
            settings.SimulationTimeSeconds = 1.0;
            settings.EventRatePerSecond = 0.0;
            settings.ThresholdMode = "ChengTreq";
            settings.Normalize();

            double startTreq = settings.TreqSeconds;
            double step = 7.0;
            settings.SweepEnabled = true;
            settings.SweepParameterKey = "TreqSeconds";
            settings.SweepIterationCount = 2;
            settings.SweepStepValue = step;

            ExperimentBatchResult result = new ExperimentSweepBatchRunner(null, false).Run(settings);
            AssertSelfTest(result.RunSummaries.Count == 3,
                "TreqSeconds sweep self-test should produce one summary per sweep step.");
            for (int i = 0; i < result.RunSummaries.Count; i++)
            {
                double expected = startTreq + step * i;
                ExperimentRunSummary summary = result.RunSummaries[i];
                AssertNear(summary.SweepValue, expected, 1e-9,
                    "TreqSeconds sweep value should reflect the requested step value.");
                AssertNear(summary.EffectiveTreqSeconds, expected, 1e-9,
                    "TreqSeconds sweep step should use the requested TreqSeconds at runtime.");
                AssertSelfTest(String.Equals(summary.TreqSource, "TreqSeconds", StringComparison.Ordinal),
                    "TreqSeconds sweep step should switch to manual TreqSeconds mode.");
            }
        }

        private static void DisableTaskDetailCsv(ExperimentSettings settings)
        {
            settings.WriteTaskDetailCsv = false;
            settings.WriteMissionDetailsCsv = false;
            settings.WriteTaskRecordsCsv = false;
            settings.WriteBprDebugCsv = false;
            settings.WriteYuBprDebugCsv = false;
        }

        private static void RunWcvReturnReserveSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            string reserveDirectory = Path.Combine(tempDirectory, "wcv-return-reserve");
            Directory.CreateDirectory(reserveDirectory);

            ExperimentSettings settings = CreateBprSelfTestSettings(reserveDirectory);
            settings.SensorCount = 1;
            settings.SimulationTimeSeconds = 100.0;
            settings.SensorBackgroundLifetimeSeconds = 1000000000.0;
            settings.WcvSpeedMetersPerSecond = 10.0;
            settings.WcvChargeRateJPerSecond = 10.0;
            settings.WcvCapacityJ = 25.0;
            settings.WcvMoveCostJPerMeter = 1.0;
            settings.NmaxTask = 1;
            settings.SelectedAlgorithmsCsv = "NJF";
            settings.Normalize();

            ExperimentArtifact artifact = CreateBprSelfTestArtifact(new double[] { 90.0 });
            artifact.Sensors[1].X = 10.0;
            artifact.Sensors[1].Y = 0.0;

            MissionDetailCsvOutputOptions options = new MissionDetailCsvOutputOptions();
            options.WriteMissionDetails = false;
            options.WriteTaskRecords = true;
            options.WriteBprDebug = false;
            options.WriteYuBprDebug = false;

            ExperimentSimulation simulation = new ExperimentSimulation(
                settings,
                artifact,
                "NJF",
                reserveDirectory,
                options);
            simulations.Add(simulation);

            ChargingRequest request = new ChargingRequest();
            request.RequestId = 1;
            request.NodeId = 1;
            request.RequestTimeSeconds = 0.0;
            request.DeadlineSeconds = 1000.0;
            request.RequestEnergyJ = simulation.sensors[1].EnergyJ;
            request.ConsumeRateJPerSecond = simulation.sensors[1].ConsumeRateJPerSecond;
            request.BaseConsumeRateJPerSecond = simulation.sensors[1].ConsumeRateJPerSecond;
            request.RequestNodeConsumeRateJPerSecond = simulation.sensors[1].ConsumeRateJPerSecond;
            request.EffectiveConsumeRateJPerSecond = simulation.sensors[1].ConsumeRateJPerSecond;
            simulation.activeRequests.Add(request);
            simulation.sensors[1].HasPendingRequest = true;

            simulation.ExecuteMission();
            if (simulation.csvWriter != null)
                simulation.csvWriter.Dispose();

            AssertSelfTest(simulation.summary.SuccessfulCharges == 0,
                "A partial charge that preserves return energy must not be counted as a successful full charge.");
            AssertSelfTest(simulation.summary.FailedOrLateTasks == 1,
                "A partial charge caused by return-energy reservation should be counted as a failed task.");

            string taskPath = Path.Combine(reserveDirectory, "run001-seed777-NJF-task-records.csv");
            AssertSelfTest(File.Exists(taskPath), "Return-reserve task-record CSV was not written.");
            string[] lines = File.ReadAllLines(taskPath, Encoding.UTF8);
            AssertSelfTest(lines.Length == 2,
                "Return-reserve self-test should write exactly one task-record row.");

            Dictionary<string, int> columns = BuildCsvHeaderMap(lines[0].Split(','));
            string[] fields = lines[1].Split(',');
            bool success = Boolean.Parse(fields[columns["Success"]]);
            double wcvEnergyAfter = Double.Parse(fields[columns["WcvEnergyAfterJ"]], CultureInfo.InvariantCulture);
            double returnEnergy = ExperimentArtifact.Distance(artifact.Sensors[1].X, artifact.Sensors[1].Y,
                artifact.BaseX, artifact.BaseY) * settings.WcvMoveCostJPerMeter;

            AssertSelfTest(!success,
                "Return-reserve self-test task should be recorded as failed/partial, not successful.");
            AssertSelfTest(wcvEnergyAfter >= returnEnergy - 1e-6,
                "WCV task record should retain enough energy to return to base after charging.");
        }

        private static void RunWcvMaxTaskFeasibilitySelfTest()
        {
            ExperimentSettings feasible = ExperimentSettings.CreateDefault();
            feasible.NmaxTask = 30;
            feasible.WcvCapacityJ = 200000.0;
            feasible.MapWidthMeters = 500.0;
            feasible.MapHeightMeters = 500.0;
            feasible.InitialEnergyJ = 100.0;
            feasible.WcvMoveCostJPerMeter = 10.0;
            feasible.WcvChargeRateJPerSecond = 5.0;
            feasible.WcvSpeedMetersPerSecond = 5.0;
            feasible.Normalize();
            WcvMaxTaskFeasibilityResult feasibleResult =
                WcvMaxTaskFeasibilityValidator.ValidateWcvCapacityForAlgorithm(feasible, "NJF_CHENG_BPR");
            AssertSelfTest(feasibleResult.IsValid,
                "Feasible NmaxTask/WCV capacity settings should pass validation.");
            AssertSelfTest(feasibleResult.EstimatedMaxTaskMissionEnergyJ <= feasible.WcvCapacityJ,
                "Feasible validation estimate should not exceed WCV capacity.");

            ExperimentSettings infeasible = feasible.Copy();
            infeasible.WcvCapacityJ = 1000.0;
            infeasible.Normalize();
            WcvMaxTaskFeasibilityResult infeasibleResult =
                WcvMaxTaskFeasibilityValidator.ValidateWcvCapacityForAlgorithm(infeasible, "NJF_CHENG_BPR");
            AssertSelfTest(!infeasibleResult.IsValid,
                "Insufficient WCV capacity should fail validation.");
            AssertSelfTest(infeasibleResult.ErrorMessage.IndexOf("估計一趟最大任務能耗", StringComparison.Ordinal) >= 0 &&
                infeasibleResult.ErrorMessage.IndexOf("WCV 容量", StringComparison.Ordinal) >= 0,
                "WCV feasibility error should include the estimated mission energy and capacity in Chinese.");

            ExperimentSettings extendedFeasible = feasible.Copy();
            extendedFeasible.SelectedAlgorithmsCsv = "NJF_ROUTE_YU_BPR_EXTENDED";
            WcvMaxTaskFeasibilityResult extendedFeasibleResult =
                WcvMaxTaskFeasibilityValidator.ValidateWcvCapacityForAlgorithm(extendedFeasible, "NJF_ROUTE_YU_BPR_EXTENDED");
            AssertSelfTest(extendedFeasibleResult.IsValid &&
                extendedFeasibleResult.ValidationTaskLimit == extendedFeasible.NmaxTask,
                "EXTENDED algorithms should use NmaxTask for coarse preflight validation.");

            ExperimentSettings extendedInsufficient = extendedFeasible.Copy();
            extendedInsufficient.WcvCapacityJ = 1000.0;
            WcvMaxTaskFeasibilityResult extendedInsufficientResult =
                WcvMaxTaskFeasibilityValidator.ValidateWcvCapacityForAlgorithm(extendedInsufficient, "NJF_ROUTE_YU_BPR_EXTENDED");
            AssertSelfTest(!extendedInsufficientResult.IsValid &&
                extendedInsufficientResult.ValidationTaskLimit == extendedInsufficient.NmaxTask,
                "EXTENDED insufficient capacity should fail using NmaxTask coarse preflight validation.");

            ExperimentSettings invalidChargeRate = feasible.Copy();
            invalidChargeRate.WcvChargeRateJPerSecond = 0.0;
            WcvMaxTaskFeasibilityResult invalidChargeRateResult =
                WcvMaxTaskFeasibilityValidator.ValidateWcvCapacityForAlgorithm(invalidChargeRate, "NJF");
            AssertSelfTest(!invalidChargeRateResult.IsValid &&
                invalidChargeRateResult.ErrorMessage.IndexOf("WCV 充電速率必須大於 0", StringComparison.Ordinal) >= 0,
                "Invalid WCV charge rate should fail with a Chinese error message.");
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
            settings.CriticalDensityRadiusMeters = 150.0;
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
            settings.ProactivePredictionHorizonSeconds = 1000.0;
            settings.ProactiveCooldownSeconds = 10.0;
            settings.PrateChange = 0.0;
            settings.RateChangeVariationPercent = 0.0;
            settings.SelectedAlgorithmsCsv = "NJF_ROUTE_CHENG_BPR_LIMITED";
            settings.OutputDirectory = outputDirectory;
            settings.Normalize();
            return settings;
        }

        private static void RunChengActivationGenerationSelfTest(string tempDirectory)
        {
            ExperimentSettings settings = CreateBprSelfTestSettings(tempDirectory);
            settings.SensorCount = 10;
            settings.SimulationTimeSeconds = 200.0;
            settings.SensorBackgroundLifetimeSeconds = 100.0;
            settings.EventRatePerSecond = 0.03;
            settings.Normalize();

            ExperimentArtifact artifact = ExperimentArtifact.Generate(settings, 1, 42);
            int expectedActivationCount = (int)Math.Round(
                settings.EventRatePerSecond * settings.SensorBackgroundLifetimeSeconds);
            AssertSelfTest(artifact.UsesActivationSchedule,
                "Generated experiment artifacts should use CHENG activation scheduling.");
            AssertSelfTest(artifact.ActivationEvents.Count == expectedActivationCount,
                "EventRatePerSecond should produce p * SensorBackgroundLifetimeSeconds activation events.");
            AssertSelfTest(artifact.ActivationEvents.Count <= settings.SensorCount,
                "Activation event count must not exceed SensorCount.");
            for (int i = 1; i < artifact.ActivationEvents.Count; i++)
            {
                AssertSelfTest(artifact.ActivationEvents[i - 1].TimeSeconds <= artifact.ActivationEvents[i].TimeSeconds,
                    "Activation events should be sorted by time.");
            }
            for (int i = 0; i < artifact.Sensors.Count; i++)
            {
                AssertSelfTest(artifact.Sensors[i].ParentId < 0,
                    "Experiment artifacts should not assign parent links.");
            }

            ExperimentSettings zeroSettings = CreateBprSelfTestSettings(tempDirectory);
            zeroSettings.SensorCount = 5;
            zeroSettings.EventRatePerSecond = 0.0;
            zeroSettings.SimulationTimeSeconds = 10.0;
            zeroSettings.Normalize();
            ExperimentSimulation zeroSimulation = new ExperimentSimulation(
                zeroSettings,
                ExperimentArtifact.Generate(zeroSettings, 1, 777),
                "NJF",
                null);
            zeroSimulation.Run();
            AssertSelfTest(zeroSimulation.summary.NaturalRequestCount == 0 &&
                zeroSimulation.summary.FirstDeadNodeId < 0,
                "Inactive sensors must not consume energy, die, or create charging requests.");
        }

        private static ExperimentArtifact CreateBprSelfTestArtifact(double[] sensorEnergies)
        {
            ExperimentArtifact artifact = new ExperimentArtifact();
            artifact.RunIndex = 1;
            artifact.Seed = 777;
            artifact.ArtifactHash = "SELFTEST";
            artifact.BaseX = 0.0;
            artifact.BaseY = 0.0;
            artifact.UsesActivationSchedule = false;

            SensorTemplate baseStation = new SensorTemplate();
            baseStation.Id = 0;
            baseStation.X = 0.0;
            baseStation.Y = 0.0;
            baseStation.InitialEnergyJ = Double.PositiveInfinity;
            baseStation.ParentId = -1;
            baseStation.InitiallyActive = true;
            artifact.Sensors.Add(baseStation);

            for (int i = 0; i < sensorEnergies.Length; i++)
            {
                SensorTemplate sensor = new SensorTemplate();
                sensor.Id = i + 1;
                sensor.X = (i + 1) * 10.0;
                sensor.Y = 0.0;
                sensor.InitialEnergyJ = sensorEnergies[i];
                sensor.ParentId = 0;
                sensor.InitiallyActive = true;
                artifact.Sensors.Add(sensor);
            }

            return artifact;
        }

        private static void RunStandaloneDispatchSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            string[] proactiveAlgorithms = new string[]
            {
                "NJF_CHENG_BPR",
                "TADP_CHENG_BPR",
                "EDF_CHENG_BPR",
                "NJF_YU_BPR",
                "NJF_ROUTE_CHENG_BPR_LIMITED",
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

        private static void RunChengPaperBprWrapperSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            string[] paperAlgorithms = new string[]
            {
                "NJF_CHENG_BPR",
                "TADP_CHENG_BPR",
                "EDF_CHENG_BPR"
            };

            for (int i = 0; i < paperAlgorithms.Length; i++)
                AssertActiveRequestProactiveRoute(tempDirectory, simulations, paperAlgorithms[i], "CHENG_BPR_PAPER_RANDOM", 2);

            ExperimentSettings finiteDeadlineSettings = CreateBprSelfTestSettings(tempDirectory);
            finiteDeadlineSettings.NmaxTask = 2;
            finiteDeadlineSettings.ProactiveCandidateMaxEnergyRatio = 0.95;
            finiteDeadlineSettings.Normalize();
            ExperimentArtifact artifact = CreateBprSelfTestArtifact(new double[] { 40.0, 80.0, 80.0, 80.0, 80.0 });
            artifact.UsesActivationSchedule = true;
            artifact.Sensors[5].InitiallyActive = false;

            ExperimentSimulation simulation = new ExperimentSimulation(
                finiteDeadlineSettings,
                artifact,
                "NJF_CHENG_BPR",
                null);
            simulations.Add(simulation);
            simulation.CreateRequestsAtCurrentTime();
            List<ChargingRequest> route = simulation.BuildMissionRoute();
            ChargingRequest proactive = null;
            for (int i = 0; i < route.Count; i++)
            {
                if (route[i].IsProactive)
                {
                    proactive = route[i];
                    break;
                }
            }

            AssertSelfTest(proactive != null,
                "NJF_CHENG_BPR should add a paper-random proactive request when the STable deadline window overflows.");
            AssertSelfTest(!Double.IsInfinity(proactive.DeadlineSeconds) && !Double.IsNaN(proactive.DeadlineSeconds),
                "CHENG paper proactive request deadline must be finite.");
            AssertSelfTest(proactive.DeadlineSeconds > simulation.currentTime,
                "CHENG paper proactive request deadline should be the predicted death time, not the current request time.");
            AssertSelfTest(!ContainsChargingRequest(route, 5),
                "Inactive sensors must not enter the CHENG BP&R paper Slist or cplist.");
        }

        private static void RunChengPaperRandomPoolSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings scheduleSettings = CreateBprSelfTestSettings(tempDirectory);
            scheduleSettings.NmaxTask = 5;
            scheduleSettings.ProactiveCandidateMaxEnergyRatio = 0.95;
            scheduleSettings.ProactiveCooldownSeconds = 10.0;
            scheduleSettings.Normalize();

            ExperimentSimulation dangerSimulation = new ExperimentSimulation(
                scheduleSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0, 80.0, 80.0, 80.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(dangerSimulation);

            List<ChargingRequest> partialClist = new List<ChargingRequest>();
            partialClist.Add(CreateManualChargingRequest(1, 100.0));
            partialClist.Add(CreateManualChargingRequest(2, 110.0));
            partialClist.Add(CreateManualChargingRequest(3, 120.0));
            List<ChargingRequest> dangerCplist = dangerSimulation.BuildChengBprPaperCplist(partialClist, scheduleSettings.NmaxTask);
            AssertSelfTest(dangerCplist.Count <= scheduleSettings.NmaxTask,
                "CHENG paper-random cplist must not exceed NmaxTask.");
            AssertSelfTest(CountProactiveChargingRequests(dangerCplist) > 0,
                "CHENG paper-random should add proactive tasks when clist is not full and the BottleList overflows.");
            AssertSelfTest(ProactiveNodesAreWithinRange(dangerCplist, 4, 9),
                "CHENG paper-random proactive tasks must come from the danger-window BottleList, not from arbitrary sensors.");

            List<ChargingRequest> fullClist = new List<ChargingRequest>();
            for (int nodeId = 1; nodeId <= scheduleSettings.NmaxTask; nodeId++)
                fullClist.Add(CreateManualChargingRequest(nodeId, 100.0 + nodeId));
            List<ChargingRequest> fullCplist = dangerSimulation.BuildChengBprPaperCplist(fullClist, scheduleSettings.NmaxTask);
            AssertSelfTest(CountProactiveChargingRequests(fullCplist) == 0,
                "CHENG paper-random must not add proactive tasks when clist is already full.");

            ExperimentSimulation noDangerSimulation = new ExperimentSimulation(
                scheduleSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0, 80.0, 80.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(noDangerSimulation);
            List<ChargingRequest> noDangerClist = new List<ChargingRequest>();
            noDangerClist.Add(CreateManualChargingRequest(1, 100.0));
            noDangerClist.Add(CreateManualChargingRequest(2, 110.0));
            noDangerClist.Add(CreateManualChargingRequest(3, 120.0));
            List<ChargingRequest> noDangerCplist = noDangerSimulation.BuildChengBprPaperCplist(noDangerClist, scheduleSettings.NmaxTask);
            AssertSelfTest(noDangerCplist.Count == noDangerClist.Count &&
                CountProactiveChargingRequests(noDangerCplist) == 0,
                "CHENG paper-random must not add proactive tasks when there is no overflowing danger interval.");

            ExperimentSettings candidateSettings = CreateBprSelfTestSettings(tempDirectory);
            candidateSettings.NmaxTask = 1;
            candidateSettings.ProactiveCandidateMaxEnergyRatio = 0.95;
            candidateSettings.ProactiveCooldownSeconds = 10.0;
            candidateSettings.Normalize();

            ExperimentSimulation nearFullSimulation = new ExperimentSimulation(
                candidateSettings,
                CreateBprSelfTestArtifact(new double[] { 96.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(nearFullSimulation);
            SetBprReportedDeadline(nearFullSimulation, 1, 10.0);
            SetBprReportedDeadline(nearFullSimulation, 2, 11.0);
            SetBprReportedDeadline(nearFullSimulation, 3, 12.0);
            AssertChengCandidateCanBeRandomlySelected(nearFullSimulation, 1,
                "Near-full sensors inside the CHENG BottleList must remain selectable by paper-random selection.");

            ExperimentSimulation recentlyChargedSimulation = new ExperimentSimulation(
                candidateSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(recentlyChargedSimulation);
            SetBprReportedDeadline(recentlyChargedSimulation, 1, 10.0);
            SetBprReportedDeadline(recentlyChargedSimulation, 2, 11.0);
            SetBprReportedDeadline(recentlyChargedSimulation, 3, 12.0);
            recentlyChargedSimulation.GetOrCreateBprSTableEntry(1).LastChargedTimeSeconds = recentlyChargedSimulation.currentTime;
            AssertChengCandidateCanBeRandomlySelected(recentlyChargedSimulation, 1,
                "Recently charged sensors inside the CHENG BottleList must remain selectable by paper-random selection.");

            ExperimentSimulation recentlyProactiveSimulation = new ExperimentSimulation(
                candidateSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(recentlyProactiveSimulation);
            SetBprReportedDeadline(recentlyProactiveSimulation, 1, 10.0);
            SetBprReportedDeadline(recentlyProactiveSimulation, 2, 11.0);
            SetBprReportedDeadline(recentlyProactiveSimulation, 3, 12.0);
            recentlyProactiveSimulation.GetOrCreateBprSTableEntry(1).LastProactiveSelectedTimeSeconds = recentlyProactiveSimulation.currentTime;
            AssertChengCandidateCanBeRandomlySelected(recentlyProactiveSimulation, 1,
                "Recently proactive-selected sensors inside the CHENG BottleList must remain selectable by paper-random selection.");

            ExperimentArtifact legalityArtifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0, 80.0, 80.0 });
            legalityArtifact.UsesActivationSchedule = true;
            legalityArtifact.Sensors[2].InitiallyActive = false;
            ExperimentSimulation legalitySimulation = new ExperimentSimulation(
                candidateSettings,
                legalityArtifact,
                "NJF_CHENG_BPR",
                null);
            simulations.Add(legalitySimulation);
            legalitySimulation.GetOrCreateBprSTableEntry(0).LatestReportedDeadlineSeconds = 10.0;
            legalitySimulation.sensors[1].Alive = false;
            legalitySimulation.sensors[5].HasPendingRequest = true;
            legalitySimulation.GetOrCreateBprSTableEntry(6).IsScheduledInCurrentMission = true;
            HashSet<int> reservedNodeIds = new HashSet<int>();
            reservedNodeIds.Add(3);
            List<BprPredictedRequest> legalityCandidates = legalitySimulation.BuildChengBprPaperCandidates(reservedNodeIds);
            AssertSelfTest(!ContainsBprPredictedRequest(legalityCandidates, 0) &&
                !ContainsBprPredictedRequest(legalityCandidates, 1) &&
                !ContainsBprPredictedRequest(legalityCandidates, 2) &&
                !ContainsBprPredictedRequest(legalityCandidates, 3) &&
                !ContainsBprPredictedRequest(legalityCandidates, 5) &&
                !ContainsBprPredictedRequest(legalityCandidates, 6),
                "CHENG paper-random must still exclude BS, dead, inactive, duplicate, pending, and mission-scheduled nodes.");
            AssertSelfTest(ContainsBprPredictedRequest(legalityCandidates, 4),
                "CHENG paper-random should keep valid BottleList candidates after legality filtering.");

            ExperimentArtifact sharedArtifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0, 80.0, 80.0 });
            ExperimentSimulation njfChengSimulation = new ExperimentSimulation(candidateSettings, sharedArtifact, "NJF_CHENG_BPR", null);
            ExperimentSimulation edfChengSimulation = new ExperimentSimulation(candidateSettings, sharedArtifact, "EDF_CHENG_BPR", null);
            simulations.Add(njfChengSimulation);
            simulations.Add(edfChengSimulation);
            List<ChargingRequest> njfChengCplist = njfChengSimulation.BuildChengBprPaperCplist(new List<ChargingRequest>(), candidateSettings.NmaxTask);
            List<ChargingRequest> edfChengCplist = edfChengSimulation.BuildChengBprPaperCplist(new List<ChargingRequest>(), candidateSettings.NmaxTask);
            AssertSelfTest(SameProactiveChargingNodeSet(njfChengCplist, edfChengCplist),
                "NJF_CHENG_BPR and EDF_CHENG_BPR must use the same CHENG paper-random proactive node set for the same pool, seed, and danger window.");

            List<ChargingRequest> sharedCplist = new List<ChargingRequest>();
            sharedCplist.Add(CreateManualChargingRequest(1, 500.0));
            sharedCplist.Add(CreateManualChargingRequest(5, 50.0));
            List<ChargingRequest> njfRoute = njfChengSimulation.BuildNearestRoute(sharedCplist, 2);
            List<ChargingRequest> edfRoute = njfChengSimulation.TakeSorted(sharedCplist, 2, CompareByDeadline);
            AssertSelfTest(SameChargingNodeSet(njfRoute, edfRoute) && njfRoute[0].NodeId != edfRoute[0].NodeId,
                "NJF_CHENG_BPR and EDF_CHENG_BPR should differ only in final route ordering after the shared CHENG cplist is built.");

            ExperimentSimulation windowSimulation = new ExperimentSimulation(
                candidateSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 10.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(windowSimulation);

            List<BprPredictedRequest> manualRequests = new List<BprPredictedRequest>();
            manualRequests.Add(CreateManualBprPredictedRequest(1, 100.0, 200.0, 0.1));
            manualRequests.Add(CreateManualBprPredictedRequest(2, 105.0, 205.0, 0.1));
            manualRequests.Add(CreateManualBprPredictedRequest(3, 1000.0, 1010.0, 0.1));
            List<BprWindow> windows = windowSimulation.BuildChengBprPaperWindows(manualRequests, 1);
            AssertSelfTest(windows.Count > 0,
                "Manual CHENG danger interval setup should create an overflow window.");
            AssertSelfTest(ContainsBprPredictedRequest(windows[0].Requests, 1) &&
                ContainsBprPredictedRequest(windows[0].Requests, 2),
                "CHENG danger interval candidate pool should include only sensors related to the selected danger window.");
            AssertSelfTest(!ContainsBprPredictedRequest(windows[0].Requests, 3),
                "A low-energy sensor outside the selected CHENG danger interval must not enter the random pool.");

            List<BprPredictedRequest> randomPool = new List<BprPredictedRequest>();
            for (int nodeId = 1; nodeId <= 5; nodeId++)
                randomPool.Add(CreateManualBprPredictedRequest(nodeId, 100.0 + nodeId, 200.0 + nodeId, 0.1));
            List<BprPredictedRequest> seed11 = windowSimulation.SelectChengBprPaperRandomNodes(randomPool, 2, new Random(11));
            List<BprPredictedRequest> seed12 = windowSimulation.SelectChengBprPaperRandomNodes(randomPool, 2, new Random(12));
            AssertSelfTest(!SameBprPredictedNodeSet(seed11, seed12),
                "CHENG paper candidate selection should remain random within the danger interval pool, not sorted by EDF/NJF/energy.");
        }

        private static void RunActiveRequestProactiveSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_CHENG_BPR", ChengBprPaperRandomReason, 2);
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_YU_BPR", YuBprDangerIntervalRemovalReason, 2);
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_ROUTE_CHENG_BPR_LIMITED", ChengBprRouteInsertionReason, 2);
            AssertActiveRequestProactiveRoute(tempDirectory, simulations, "NJF_ROUTE_YU_BPR_LIMITED", YuBprDangerIntervalRemovalReason, 2);

            ExperimentSettings extendedSettings = CreateBprSelfTestSettings(tempDirectory);
            extendedSettings.NmaxTask = 1;
            extendedSettings.YuDangerWindowSeconds = 10.0;
            extendedSettings.YuDangerThresholdK = 2;
            extendedSettings.YuIntervalUncertaintySeconds = 10.0;
            extendedSettings.Normalize();
            ExperimentArtifact artifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0, 80.0 });

            ExperimentSimulation chengExtended = new ExperimentSimulation(extendedSettings, artifact, "NJF_ROUTE_CHENG_BPR_EXTENDED", null);
            simulations.Add(chengExtended);
            List<ChargingRequest> chengCplist = chengExtended.BuildChengBprPaperCplist(
                new List<ChargingRequest>(),
                1,
                BprProactiveSelectionMode.RouteInsertionCost,
                true);
            AssertSelfTest(chengCplist.Count > 1,
                "NJF_ROUTE_CHENG_BPR_EXTENDED should allow cplist.Count beyond NmaxTask.");

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

            ExperimentSimulation yuNearFull = new ExperimentSimulation(nearFullSettings, nearFullArtifact, "NJF_YU_BPR", null);
            simulations.Add(yuNearFull);
            AssertSelfTest(yuNearFull.BuildYuBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    false,
                    YuProactiveSelectionMode.Random).Count == 0,
                "Near-full nodes must be excluded from YU BP&R proactive candidates.");

            ExperimentSettings cooldownSettings = CreateBprSelfTestSettings(tempDirectory);
            cooldownSettings.NmaxTask = 1;
            cooldownSettings.YuDangerWindowSeconds = 10.0;
            cooldownSettings.YuDangerThresholdK = 2;
            cooldownSettings.YuIntervalUncertaintySeconds = 10.0;
            cooldownSettings.ProactiveCooldownSeconds = 10.0;
            cooldownSettings.Normalize();
            ExperimentArtifact cooldownArtifact = CreateBprSelfTestArtifact(new double[] { 80.0, 80.0 });

            ExperimentSimulation cooldownSimulation = new ExperimentSimulation(cooldownSettings, cooldownArtifact, "NJF_YU_BPR", null);
            simulations.Add(cooldownSimulation);
            for (int nodeId = 1; nodeId < cooldownSimulation.sensors.Length; nodeId++)
            {
                BprSTableEntry entry = cooldownSimulation.GetOrCreateBprSTableEntry(nodeId);
                entry.LastChargedTimeSeconds = cooldownSimulation.currentTime;
                entry.LastProactiveSelectedTimeSeconds = cooldownSimulation.currentTime;
            }
            AssertSelfTest(cooldownSimulation.BuildYuBprCplist(
                    new List<ChargingRequest>(),
                    1,
                    false,
                    YuProactiveSelectionMode.Random).Count == 0,
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

            ExperimentSimulation pendingContinuousDeath = new ExperimentSimulation(
                settings,
                CreateBprSelfTestArtifact(new double[] { 10.0 }),
                "NJF",
                null);
            simulations.Add(pendingContinuousDeath);
            pendingContinuousDeath.sensors[1].HasPendingRequest = true;
            pendingContinuousDeath.sensors[1].EnergyJ = -0.25;
            pendingContinuousDeath.MarkDead(1, 12.0, "continuous");
            ExperimentDeathRecord pendingContinuousRecord = pendingContinuousDeath.deaths[0];
            AssertSelfTest(pendingContinuousRecord.Reason == "continuous",
                "Scheduling-related death should preserve continuous as Reason in the CHENG flow.");
            AssertSelfTest(pendingContinuousRecord.DirectEnergyCause == "continuous",
                "DirectEnergyCause should preserve the direct continuous death reason.");
            AssertSelfTest(pendingContinuousRecord.SchedulingRelated,
                "Pending-request death should be marked scheduling related.");
            AssertSelfTest(pendingContinuousRecord.SchedulingCause == "scheduling_wait",
                "SchedulingCause should be scheduling_wait when a pending request exists.");
            AssertSelfTest(pendingContinuousRecord.HasPendingRequestAtDeath,
                "HasPendingRequestAtDeath should capture the pending request snapshot.");
            AssertNear(pendingContinuousRecord.EnergyBeforeDeathJ, -0.25, 1e-12,
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

        private static void RunCompleteBprYuPredictionSelfTest(string tempDirectory, List<ExperimentSimulation> simulations)
        {
            ExperimentSettings windowSettings = CreateBprSelfTestSettings(tempDirectory);
            windowSettings.SimulationTimeSeconds = 240.0;
            windowSettings.NmaxTask = 2;
            windowSettings.YuDangerWindowSeconds = 1.0;
            windowSettings.YuDangerThresholdK = 99;
            windowSettings.Normalize();
            ExperimentSimulation windowSimulation = new ExperimentSimulation(
                windowSettings,
                CreateBprSelfTestArtifact(new double[] { 80.0, 80.0, 80.0 }),
                "NJF_CHENG_BPR",
                null);
            simulations.Add(windowSimulation);

            List<BprPredictedRequest> manualRequests = new List<BprPredictedRequest>();
            manualRequests.Add(CreateManualBprPredictedRequest(1, 100.0, 150.0, 1.0));
            manualRequests.Add(CreateManualBprPredictedRequest(2, 110.0, 160.0, 1.0));
            manualRequests.Add(CreateManualBprPredictedRequest(3, 120.0, 170.0, 1.0));
            List<BprWindow> bprWindows = windowSimulation.BuildChengBprPaperWindows(manualRequests, 2);
            AssertSelfTest(bprWindows.Count > 0 && bprWindows[0].OverflowCount == 1,
                "CHENG BP&R interval window should detect one overflow from three predicted requests and NmaxTask=2.");
            List<BprPredictedRequest> bprRandomSelected = windowSimulation.SelectChengBprPaperRandomNodes(
                bprWindows[0].Requests,
                1,
                new Random(1));
            AssertSelfTest(bprRandomSelected.Count == 1,
                "CHENG BP&R interval removal should randomly select one BottleList candidate.");
            List<BprPredictedRequest> bprRouteSelected = windowSimulation.SelectChengBprRouteInsertionNodes(
                bprWindows[0].Requests,
                1,
                new List<ChargingRequest>());
            AssertSelfTest(bprRouteSelected.Count == 1 && bprRouteSelected[0].NodeId == 1,
                "ROUTE_CHENG_BPR should select the lowest route insertion cost candidate before request-time tie-breakers.");

            List<YuPredictedInterval> manualIntervals = new List<YuPredictedInterval>();
            manualIntervals.Add(CreateManualYuPredictedInterval(1, 100.0, 100.0, 160.0, 150.0, 1.0, 30.0));
            manualIntervals.Add(CreateManualYuPredictedInterval(2, 110.0, 110.0, 170.0, 160.0, 1.0, 30.0));
            manualIntervals.Add(CreateManualYuPredictedInterval(3, 120.0, 120.0, 180.0, 170.0, 1.0, 30.0));
            List<YuDangerWindow> yuWindows = windowSimulation.BuildYuDangerWindows(manualIntervals, 2);
            double expectedYuWindowEnd = 100.0 + windowSimulation.EstimateBprTjobSeconds(2);
            AssertSelfTest(yuWindows.Count > 0 && yuWindows[0].WindowStartSeconds == 100.0,
                "YU BP&R danger window should start at CenterRequestTimeSeconds, not IntervalStartSeconds.");
            AssertNear(yuWindows[0].WindowEndSeconds, expectedYuWindowEnd, 1e-9,
                "YU BP&R danger window should end at CenterRequestTimeSeconds + EstimateBprTjobSeconds(maxTask).");
            AssertSelfTest(yuWindows[0].DangerCount == 3 && yuWindows[0].RemovalNeededCount == yuWindows[0].DangerCount - 2,
                "YU BP&R danger window should use CHENG-style overlap count > maxTask and removal count = DangerCount - maxTask.");
            AssertSelfTest(yuWindows[0].KStar == 3,
                "YuDangerThresholdK should not change formal YU BP&R danger detection; debug KStar should be maxTask + 1.");
            List<YuRemovalDecision> yuDecisions = windowSimulation.SelectYuRemovalNodes(
                yuWindows[0],
                1,
                new List<ChargingRequest>(),
                YuProactiveSelectionMode.Random);
            AssertSelfTest(yuDecisions.Count == 1 && yuDecisions[0].Reason == "YU_RANDOM_INTERVAL_REMOVAL",
                "NJF_YU_BPR should randomly select one danger-window candidate.");
            List<YuRemovalDecision> yuRouteDecisions = windowSimulation.SelectYuRemovalNodes(
                yuWindows[0],
                1,
                new List<ChargingRequest>(),
                YuProactiveSelectionMode.RouteInsertionCost);
            AssertSelfTest(yuRouteDecisions.Count == 1 &&
                yuRouteDecisions[0].NodeId == 1 &&
                yuRouteDecisions[0].Reason == "YU_ROUTE_COST_INTERVAL_REMOVAL",
                "ROUTE_YU_BPR should select the lowest route insertion cost candidate.");
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
                "NJF_ROUTE_YU_BPR_LIMITED",
                null);
            simulations.Add(predictionSimulation);
            AssertSelfTest(predictionArtifact.GetRateChangesForNode(1).Count == 1,
                "ExperimentArtifact should group future rate changes by node id for prediction timeline scans.");
            List<BprPredictedRequest> predictedRequests = predictionSimulation.BuildBprPredictedRequests(1, new HashSet<int>());
            AssertSelfTest(predictedRequests.Count == 1,
                "Shared prediction timeline should produce one predicted request for the rate-change test node.");
            AssertNear(predictedRequests[0].RequestTimeSeconds, 64.5, 1e-9,
                "Shared prediction timeline should apply future rate changes when computing request time.");

            List<YuPredictedInterval> predictedIntervals = predictionSimulation.BuildYuPredictedIntervals(1, new HashSet<int>());
            AssertSelfTest(predictedIntervals.Count == 1,
                "YU prediction timeline should produce one predicted request interval for the rate-change test node.");
            AssertNear(predictedIntervals[0].CenterRequestTimeSeconds, 64.5, 1e-9,
                "YU prediction interval center should follow the rate-change-adjusted request time.");
            AssertNear(predictedIntervals[0].IntervalStartSeconds, 54.5, 1e-9,
                "YU prediction interval start should apply configured uncertainty around the center.");
            AssertNear(predictedIntervals[0].IntervalEndSeconds, 74.5, 1e-9,
                "YU prediction interval end should apply configured uncertainty around the center.");

            ExperimentSimulation chengSideEffectSimulation = new ExperimentSimulation(
                predictionSettings,
                predictionArtifact,
                "NJF_ROUTE_CHENG_BPR_LIMITED",
                null);
            simulations.Add(chengSideEffectSimulation);
            AssertPredictionHelpersHaveNoSideEffects(chengSideEffectSimulation, 1);

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

            simulation.bprPredictedRequestCache = null;
            simulation.yuPredictedIntervalCache = null;
            simulation.HasBprBottleneckCandidate();
            BprPredictedRequestCache bprCacheAfterHas = simulation.bprPredictedRequestCache;
            YuPredictedIntervalCache yuCacheAfterHas = simulation.yuPredictedIntervalCache;
            simulation.FindNextBprBottleneckCandidateTime();
            if (simulation.algorithm == "NJF_YU_BPR" ||
                simulation.algorithm == "NJF_ROUTE_YU_BPR_LIMITED" ||
                simulation.algorithm == "NJF_ROUTE_YU_BPR_EXTENDED")
            {
                AssertSelfTest(yuCacheAfterHas != null &&
                    Object.ReferenceEquals(yuCacheAfterHas, simulation.yuPredictedIntervalCache),
                    "YU bottleneck preview and next-time lookup should reuse the same predicted interval cache.");
            }
            else if (simulation.UsesBprBottleneckCandidates() && !simulation.IsChengBprWrapperAlgorithm())
            {
                AssertSelfTest(bprCacheAfterHas != null &&
                    Object.ReferenceEquals(bprCacheAfterHas, simulation.bprPredictedRequestCache),
                    "Point-based bottleneck preview and next-time lookup should reuse the same predicted request cache.");
            }

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

        private static ChargingRequest CreateManualChargingRequest(int nodeId, double deadlineSeconds)
        {
            ChargingRequest request = new ChargingRequest();
            request.RequestId = nodeId;
            request.NodeId = nodeId;
            request.RequestTimeSeconds = 0.0;
            request.DeadlineSeconds = deadlineSeconds;
            request.RequestEnergyJ = 100.0;
            request.ConsumeRateJPerSecond = 1.0;
            request.BaseConsumeRateJPerSecond = 1.0;
            request.RequestNodeConsumeRateJPerSecond = 1.0;
            request.EffectiveConsumeRateJPerSecond = 1.0;
            request.CriticalDensity = 0.0;
            request.IsProactive = false;
            request.ProactiveReason = "";
            return request;
        }

        private static void SetBprReportedDeadline(ExperimentSimulation simulation, int nodeId, double deadlineSeconds)
        {
            BprSTableEntry entry = simulation.GetOrCreateBprSTableEntry(nodeId);
            entry.LatestReportedDeadlineSeconds = deadlineSeconds;
            entry.IsAlive = true;
            entry.IsPendingRequest = false;
            entry.IsScheduledInCurrentMission = false;
        }

        private static void AssertChengCandidateCanBeRandomlySelected(
            ExperimentSimulation simulation,
            int expectedNodeId,
            string message)
        {
            List<BprPredictedRequest> candidates = simulation.BuildChengBprPaperCandidates(new HashSet<int>());
            AssertSelfTest(ContainsBprPredictedRequest(candidates, expectedNodeId),
                message + " Candidate was removed before random selection.");
            List<BprPredictedRequest> selected = simulation.SelectChengBprPaperRandomNodes(candidates, 1, new Random(1));
            AssertSelfTest(selected.Count == 1 && selected[0].NodeId == expectedNodeId,
                message + " Fixed self-test seed should be able to select the candidate.");
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

        private static bool ContainsBprEntry(List<BprSTableEntry> entries, int nodeId)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                if (entries[i].NodeId == nodeId)
                    return true;
            }
            return false;
        }

        private static bool ContainsBprPredictedRequest(List<BprPredictedRequest> requests, int nodeId)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i].NodeId == nodeId)
                    return true;
            }
            return false;
        }

        private static bool SameBprPredictedNodeSet(List<BprPredictedRequest> left, List<BprPredictedRequest> right)
        {
            if (left == null || right == null)
                return left == right;
            if (left.Count != right.Count)
                return false;

            List<int> leftIds = new List<int>();
            List<int> rightIds = new List<int>();
            for (int i = 0; i < left.Count; i++)
                leftIds.Add(left[i].NodeId);
            for (int i = 0; i < right.Count; i++)
                rightIds.Add(right[i].NodeId);
            leftIds.Sort();
            rightIds.Sort();
            for (int i = 0; i < leftIds.Count; i++)
            {
                if (leftIds[i] != rightIds[i])
                    return false;
            }
            return true;
        }

        private static int CountProactiveChargingRequests(List<ChargingRequest> requests)
        {
            int count = 0;
            if (requests == null)
                return count;
            for (int i = 0; i < requests.Count; i++)
            {
                if (requests[i] != null && requests[i].IsProactive)
                    count++;
            }
            return count;
        }

        private static bool ProactiveNodesAreWithinRange(List<ChargingRequest> requests, int minNodeId, int maxNodeId)
        {
            if (requests == null)
                return true;
            for (int i = 0; i < requests.Count; i++)
            {
                ChargingRequest request = requests[i];
                if (request == null || !request.IsProactive)
                    continue;
                if (request.NodeId < minNodeId || request.NodeId > maxNodeId)
                    return false;
            }
            return true;
        }

        private static bool SameProactiveChargingNodeSet(List<ChargingRequest> left, List<ChargingRequest> right)
        {
            List<int> leftIds = GetChargingNodeIds(left, true);
            List<int> rightIds = GetChargingNodeIds(right, true);
            return SameSortedNodeIds(leftIds, rightIds);
        }

        private static bool SameChargingNodeSet(List<ChargingRequest> left, List<ChargingRequest> right)
        {
            List<int> leftIds = GetChargingNodeIds(left, false);
            List<int> rightIds = GetChargingNodeIds(right, false);
            return SameSortedNodeIds(leftIds, rightIds);
        }

        private static List<int> GetChargingNodeIds(List<ChargingRequest> requests, bool proactiveOnly)
        {
            List<int> nodeIds = new List<int>();
            if (requests == null)
                return nodeIds;
            for (int i = 0; i < requests.Count; i++)
            {
                ChargingRequest request = requests[i];
                if (request == null)
                    continue;
                if (proactiveOnly && !request.IsProactive)
                    continue;
                nodeIds.Add(request.NodeId);
            }
            nodeIds.Sort();
            return nodeIds;
        }

        private static bool SameSortedNodeIds(List<int> leftIds, List<int> rightIds)
        {
            if (leftIds == null || rightIds == null)
                return leftIds == rightIds;
            if (leftIds.Count != rightIds.Count)
                return false;
            for (int i = 0; i < leftIds.Count; i++)
            {
                if (leftIds[i] != rightIds[i])
                    return false;
            }
            return true;
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

        private static Dictionary<string, int> BuildCsvHeaderMap(string[] headers)
        {
            Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < headers.Length; i++)
            {
                string header = headers[i] == null ? "" : headers[i].Trim();
                if (!map.ContainsKey(header))
                    map[header] = i;
            }
            return map;
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
        public double InitialResidualEnergyJ;
        public double RateScale;
        public double ConsumeRateJPerSecond;
        public int ParentId;
        public bool IsActive;
        public bool Alive;
        public bool HasPendingRequest;
        public double ActivationTimeSeconds;

        public SensorState(SensorTemplate template, ExperimentSettings settings, bool usesActivationSchedule)
        {
            Id = template.Id;
            X = template.X;
            Y = template.Y;
            CapacityJ = settings.InitialEnergyJ;
            InitialResidualEnergyJ = template.InitialResidualEnergyJ > 0.0
                ? template.InitialResidualEnergyJ
                : template.InitialEnergyJ;
            RateScale = 1.0;
            ParentId = template.ParentId;
            IsActive = Id == 0 || template.InitiallyActive || !usesActivationSchedule;
            EnergyJ = IsActive ? InitialResidualEnergyJ : 0.0;
            Alive = true;
            HasPendingRequest = false;
            ActivationTimeSeconds = IsActive ? 0.0 : Double.PositiveInfinity;
            RefreshConsumeRate(settings);
        }

        public void Activate(double activationTimeSeconds, ExperimentSettings settings)
        {
            IsActive = true;
            ActivationTimeSeconds = activationTimeSeconds;
            CapacityJ = settings.InitialEnergyJ;
            EnergyJ = Math.Min(InitialResidualEnergyJ, CapacityJ);
            RateScale = 1.0;
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
        public double ReservedReturnEnergyJ;
        public double DeliveredEnergyJ;

        public double TotalWcvEnergyJ()
        {
            return Math.Max(0.0, WcvEnergyJ) + Math.Max(0.0, ReservedReturnEnergyJ);
        }
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
        public double RequestNodeConsumeRateJPerSecond;
        public double EffectiveConsumeRateJPerSecond;
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
            clone.RequestNodeConsumeRateJPerSecond = RequestNodeConsumeRateJPerSecond;
            clone.EffectiveConsumeRateJPerSecond = EffectiveConsumeRateJPerSecond;
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
        public bool SweepEnabled;
        public int SweepIndex;
        public string SweepParameterKey;
        public string SweepParameterName;
        public double SweepValue;
        public int RunIndex;
        public int Seed;
        public string Algorithm;
        public string ArtifactHash;
        public double PrateChange;
        public double RateChangeVariationPercent;
        public string ThresholdMode;
        public double RequestThresholdPercent;
        public double EffectiveTreqSeconds;
        public string TreqSource;
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
        public int PlannedProactiveTaskCount;
        public int ExecutedProactiveTaskCount;
        public int UniqueServedNodeCount;
        public int RepeatChargeCount;
        public int ProactiveNearFullCount;
        public int MeaningfulProactiveCount;
        public int TotalChargingTaskCount;
        public int MissionCount;
        public double MovementDistanceMeters;
        public double MoveEnergyJ;
        public double DeliveredEnergyJ;
        public double AverageDeliveredEnergyPerTask;
        public double AverageDeliveredEnergyPerProactiveTask;
        public double ChargeEfficiency;
        public double TotalWaitSeconds;
        public double AverageWaitSeconds;
    }

    public class ExperimentTaskRecord
    {
        public bool SweepEnabled;
        public int SweepIndex;
        public string SweepParameterKey;
        public string SweepParameterName;
        public double SweepValue;
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
        public double RequestNodeConsumeRateJPerSecond;
        public double ServiceNodeConsumeRateJPerSecond;
        public double NodeConsumeRatePredictionErrorJPerSecond;
        public double InternalRateNjPerTick;
        public double DeliveredEnergyJ;
        public double DistanceFromPreviousMeters;
        public bool Success;
        public string FailureReason;
        public double WcvEnergyAfterJ;
    }

    public class MissionRecord
    {
        public bool SweepEnabled;
        public int SweepIndex;
        public string SweepParameterKey;
        public string SweepParameterName;
        public double SweepValue;
        public int RunIndex;
        public int Seed;
        public string Algorithm;
        public int MissionId;
        public double DispatchTimeSeconds;
        public double ReturnTimeSeconds;
        public int NodeCount;
        public int OnDemandRequestCount;
        public int ProactiveCount;
        public int SuccessfulCharges;
        public int FailedCount;
        public double DistanceMeters;
        public double MoveEnergyJ;
        public double DeliveredEnergyJ;
        public double TotalWaitSeconds;
        public double AverageWaitSeconds;
        public int DeduplicatedTaskCount;
        public List<int> RouteNodeIds;
    }

    public class ExperimentDeathRecord
    {
        public bool SweepEnabled;
        public int SweepIndex;
        public string SweepParameterKey;
        public string SweepParameterName;
        public double SweepValue;
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
        public double EnergyBeforeDeathJ;
        public double BaseConsumeRateJPerSecondAtDeath;
        public double EffectiveConsumeRateJPerSecondAtDeath;
        public double EnergyJ;
        public double RequestTimeSeconds;
        public double WaitSeconds;
    }

    internal sealed class MissionDetailCsvWriter : IDisposable
    {
        private StreamWriter missionWriter;
        private StreamWriter taskWriter;
        private StreamWriter bprDebugWriter;
        private StreamWriter yuBprDebugWriter;
        private string bprDebugPath;
        private string yuBprDebugPath;
        private Encoding csvEncoding;
        private string algorithmKey;
        private bool disposed;

        public MissionDetailCsvWriter(string directory, ExperimentArtifact artifact, string algorithm)
            : this(directory, artifact, algorithm, MissionDetailCsvOutputOptions.All())
        {
        }

        public MissionDetailCsvWriter(string directory, ExperimentArtifact artifact, string algorithm, MissionDetailCsvOutputOptions outputOptions)
            : this(directory, artifact == null ? 0 : artifact.RunIndex, artifact == null ? 0 : artifact.Seed,
                algorithm, BuildSweepFilePrefix(artifact), outputOptions)
        {
        }

        public MissionDetailCsvWriter(string directory, int runIndex, string algorithm)
            : this(directory, runIndex, algorithm, MissionDetailCsvOutputOptions.All())
        {
        }

        public MissionDetailCsvWriter(string directory, int runIndex, string algorithm, MissionDetailCsvOutputOptions outputOptions)
            : this(directory, runIndex, 0, algorithm, "", outputOptions)
        {
        }

        private MissionDetailCsvWriter(string directory, int runIndex, int seed, string algorithm, string filePrefix, MissionDetailCsvOutputOptions outputOptions)
        {
            MissionDetailCsvOutputOptions effectiveOptions = outputOptions ?? MissionDetailCsvOutputOptions.All();
            if (!effectiveOptions.HasAnyOutput())
                return;

            Directory.CreateDirectory(directory);
            algorithmKey = ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            string safeAlgorithm = SanitizeFileName(algorithm);
            string runSeedPrefix = seed > 0
                ? String.Format(CultureInfo.InvariantCulture, "run{0:D3}-seed{1}-", runIndex, seed)
                : String.Format(CultureInfo.InvariantCulture, "run{0:D3}-", runIndex);
            string missionPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "{0}{1}{2}-mission-details.csv", filePrefix, runSeedPrefix, safeAlgorithm));
            string taskPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "{0}{1}{2}-task-records.csv", filePrefix, runSeedPrefix, safeAlgorithm));
            bprDebugPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "{0}{1}{2}-bpr-debug.csv", filePrefix, runSeedPrefix, safeAlgorithm));
            yuBprDebugPath = Path.Combine(directory, String.Format(CultureInfo.InvariantCulture,
                "{0}{1}{2}-yu-bpr-debug.csv", filePrefix, runSeedPrefix, safeAlgorithm));

            csvEncoding = new UTF8Encoding(true);
            if (effectiveOptions.WriteMissionDetails)
            {
                missionWriter = new StreamWriter(missionPath, false, csvEncoding);
                WriteMissionHeader();
            }
            if (effectiveOptions.WriteTaskRecords)
            {
                taskWriter = new StreamWriter(taskPath, false, csvEncoding);
                WriteTaskHeader();
            }
            if (!effectiveOptions.WriteBprDebug || !IsChengBprAlgorithm(algorithmKey))
                bprDebugPath = "";
            if (!effectiveOptions.WriteYuBprDebug || !IsYuBprAlgorithm(algorithmKey))
                yuBprDebugPath = "";
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

        public static void WriteSummaryCsv(string directory, ExperimentBatchResult result)
        {
            if (String.IsNullOrWhiteSpace(directory) || result == null)
                return;

            Directory.CreateDirectory(directory);
            string path = Path.Combine(directory, "summary.csv");
            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine(CsvRow("SweepParameterName", "SweepValue", "RunIndex", "Seed", "Algorithm",
                    "ThresholdMode", "TreqSeconds", "TreqSource", "NetworkLifetimeSeconds",
                    "SuccessfulCharges", "FailedOrLateTasks", "NaturalRequestCount", "ProactiveTaskCount",
                    "TotalChargingTaskCount", "MissionCount", "MovementDistanceMeters", "AverageWaitSeconds",
                    "ArtifactHash"));
                for (int i = 0; i < result.RunSummaries.Count; i++)
                {
                    ExperimentRunSummary s = result.RunSummaries[i];
                    writer.WriteLine(CsvRow(
                        s.SweepParameterName,
                        s.SweepEnabled ? (object)s.SweepValue : "",
                        s.RunIndex,
                        s.Seed,
                        s.Algorithm,
                        s.ThresholdMode,
                        s.EffectiveTreqSeconds,
                        s.TreqSource,
                        s.NetworkLifetimeSeconds,
                        s.SuccessfulCharges,
                        s.FailedOrLateTasks,
                        s.NaturalRequestCount,
                        s.ProactiveTaskCount,
                        s.TotalChargingTaskCount,
                        s.MissionCount,
                        s.MovementDistanceMeters,
                        s.AverageWaitSeconds,
                        s.ArtifactHash));
                }
            }
        }

        public void WriteMission(MissionRecord record)
        {
            if (disposed || missionWriter == null || record == null)
                return;

            missionWriter.WriteLine(CsvRow(
                record.SweepParameterName,
                record.MissionId,
                record.DispatchTimeSeconds,
                record.ReturnTimeSeconds,
                record.NodeCount,
                record.OnDemandRequestCount,
                record.ProactiveCount,
                record.SuccessfulCharges,
                record.FailedCount,
                record.DistanceMeters,
                record.AverageWaitSeconds,
                RouteText(record.RouteNodeIds),
                record.DeduplicatedTaskCount));
        }

        public void WriteTask(ExperimentTaskRecord record)
        {
            if (disposed || taskWriter == null || record == null)
                return;

            ValidateNodeConsumeRateSnapshot(record);

            taskWriter.WriteLine(CsvRow(
                record.MissionId,
                record.TaskOrder,
                record.NodeId,
                record.IsProactive,
                record.RequestTimeSeconds,
                record.DeadlineSeconds,
                record.DispatchTimeSeconds,
                record.ArrivalTimeSeconds,
                record.WaitSeconds,
                record.ChargeStartSeconds,
                record.ChargeEndSeconds,
                record.EnergyBeforeJ,
                record.ConsumeRateJPerSecond,
                record.EffectiveConsumeRateJPerSecond,
                record.RequestNodeConsumeRateJPerSecond,
                record.ServiceNodeConsumeRateJPerSecond,
                record.NodeConsumeRatePredictionErrorJPerSecond,
                record.InternalRateNjPerTick,
                record.DistanceFromPreviousMeters,
                record.Success,
                record.FailureReason,
                record.WcvEnergyAfterJ));
        }

        private static bool IsFinite(double value)
        {
            return !Double.IsNaN(value) && !Double.IsInfinity(value);
        }

        private static void ValidateNodeConsumeRateSnapshot(ExperimentTaskRecord record)
        {
            const double eps = 1e-12;

            if (!IsFinite(record.RequestNodeConsumeRateJPerSecond) ||
                !IsFinite(record.ServiceNodeConsumeRateJPerSecond) ||
                !IsFinite(record.NodeConsumeRatePredictionErrorJPerSecond))
            {
                throw new InvalidOperationException(
                    "Invalid node consume rate snapshot: NaN or Infinity. NodeId=" +
                    record.NodeId.ToString(CultureInfo.InvariantCulture));
            }

            if (record.RequestNodeConsumeRateJPerSecond < -eps ||
                record.ServiceNodeConsumeRateJPerSecond < -eps)
            {
                throw new InvalidOperationException(
                    "Invalid node consume rate snapshot: negative rate. NodeId=" +
                    record.NodeId.ToString(CultureInfo.InvariantCulture));
            }

            double expectedError =
                record.ServiceNodeConsumeRateJPerSecond -
                record.RequestNodeConsumeRateJPerSecond;
            double scale = Math.Max(1.0, Math.Max(
                Math.Abs(record.ServiceNodeConsumeRateJPerSecond),
                Math.Abs(record.RequestNodeConsumeRateJPerSecond)));

            if (Math.Abs(record.NodeConsumeRatePredictionErrorJPerSecond - expectedError) >
                1e-8 * scale)
            {
                throw new InvalidOperationException(
                    "Invalid node consume rate snapshot: prediction error mismatch. NodeId=" +
                    record.NodeId.ToString(CultureInfo.InvariantCulture));
            }
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
            string effectiveAlgorithm = String.IsNullOrWhiteSpace(algorithm) ? algorithmKey : ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            if (disposed || !IsChengBprAlgorithm(effectiveAlgorithm) || String.IsNullOrWhiteSpace(bprDebugPath))
                return;
            EnsureBprDebugWriter();

            bprDebugWriter.WriteLine(CsvRow(
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
                cplistCountBefore,
                cplistCountAfter,
                maxTask));
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
            string effectiveAlgorithm = String.IsNullOrWhiteSpace(algorithm) ? algorithmKey : ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            if (disposed || !IsYuBprAlgorithm(effectiveAlgorithm) || String.IsNullOrWhiteSpace(yuBprDebugPath))
                return;
            EnsureYuBprDebugWriter();

            yuBprDebugWriter.WriteLine(CsvRow(
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
                cplistCountBefore,
                cplistCountAfter,
                maxTask,
                allowCapacityOverflow));
        }

        private void EnsureBprDebugWriter()
        {
            if (bprDebugWriter != null)
                return;
            bprDebugWriter = new StreamWriter(bprDebugPath, false, csvEncoding ?? new UTF8Encoding(true));
            WriteBprDebugHeader();
        }

        private void EnsureYuBprDebugWriter()
        {
            if (yuBprDebugWriter != null)
                return;
            yuBprDebugWriter = new StreamWriter(yuBprDebugPath, false, csvEncoding ?? new UTF8Encoding(true));
            WriteYuBprDebugHeader();
        }

        internal static bool IsChengBprAlgorithm(string algorithm)
        {
            string key = ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            return key == "NJF_CHENG_BPR" ||
                key == "TADP_CHENG_BPR" ||
                key == "EDF_CHENG_BPR" ||
                key == "NJF_ROUTE_CHENG_BPR_LIMITED" ||
                key == "NJF_ROUTE_CHENG_BPR_EXTENDED";
        }

        internal static bool IsYuBprAlgorithm(string algorithm)
        {
            string key = ExperimentSettings.CanonicalAlgorithmKey(algorithm);
            return key == "NJF_YU_BPR" ||
                key == "NJF_ROUTE_YU_BPR_LIMITED" ||
                key == "NJF_ROUTE_YU_BPR_EXTENDED";
        }

        public void Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (missionWriter != null)
                missionWriter.Dispose();
            if (taskWriter != null)
                taskWriter.Dispose();
            if (bprDebugWriter != null)
                bprDebugWriter.Dispose();
            if (yuBprDebugWriter != null)
                yuBprDebugWriter.Dispose();
        }

        private static string[] ExpectedMissionDetailHeader()
        {
            return new string[] { "SweepParameterName", "MissionId", "DispatchTimeSeconds", "ReturnTimeSeconds",
                "NodeCount", "OnDemandRequestCount", "ProactiveCount", "SuccessfulCharges", "FailedCount",
                "DistanceMeters", "AverageWaitSeconds", "RouteNodeIds", "DeduplicatedTaskCount" };
        }

        private static string[] ExpectedTaskRecordHeader()
        {
            return new string[] { "MissionId", "TaskOrder", "NodeId", "IsProactive", "RequestTimeSeconds",
                "DeadlineSeconds", "DispatchTimeSeconds", "ArrivalTimeSeconds", "WaitSeconds",
                "ChargeStartSeconds", "ChargeEndSeconds", "EnergyBeforeJ", "ConsumeRateJPerSecond",
                "EffectiveConsumeRateJPerSecond", "RequestNodeConsumeRateJPerSecond",
                "ServiceNodeConsumeRateJPerSecond", "NodeConsumeRatePredictionErrorJPerSecond",
                "InternalRateNjPerTick", "DistanceFromPreviousMeters", "Success", "FailureReason",
                "WcvEnergyAfterJ" };
        }

        private void WriteMissionHeader()
        {
            missionWriter.WriteLine(CsvRow(ExpectedMissionDetailHeader()));
        }

        private void WriteTaskHeader()
        {
            taskWriter.WriteLine(CsvRow(ExpectedTaskRecordHeader()));
        }

        private void WriteBprDebugHeader()
        {
            bprDebugWriter.WriteLine(CsvRow("任務編號", "目前時間秒", "迭代次數", "視窗開始時間秒", "視窗結束時間秒",
                "瓶頸數量", "超出數量", "選中節點編號", "選中節點請求時間秒", "選中節點死亡時間秒",
                "選中節點安全餘裕秒", "選中節點有效耗電率J每秒", "選中節點路線插入成本",
                "加入前候選任務數", "加入後候選任務數", "單趟任務上限"));
        }

        private void WriteYuBprDebugHeader()
        {
            yuBprDebugWriter.WriteLine(CsvRow("任務編號", "目前時間秒", "迭代次數", "視窗開始時間秒", "視窗結束時間秒",
                "危險門檻", "危險數量", "需要移除數量", "選中節點編號", "選中區間開始時間秒",
                "選中中心請求時間秒", "選中區間結束時間秒", "選中最早死亡時間秒",
                "選中最晚安全服務時間秒", "選中安全餘裕秒", "選中不確定範圍秒",
                "選中有效耗電率J每秒", "選中路線插入成本", "加入前候選任務數",
                "加入後候選任務數", "單趟任務上限", "是否允許超出容量"));
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

        private static string BuildSweepFilePrefix(ExperimentArtifact artifact)
        {
            if (artifact == null || !artifact.SweepEnabled)
                return "";

            string valueText = artifact.SweepValue.ToString("0.########", CultureInfo.InvariantCulture);
            return String.Format(CultureInfo.InvariantCulture, "sweep{0:D3}-{1}-{2}-",
                artifact.SweepIndex,
                SanitizeFileName(artifact.SweepParameterKey),
                SanitizeFileName(valueText));
        }

        internal static string SanitizeFileNameForPath(string value)
        {
            return SanitizeFileName(value);
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

        private static string ResolveTreqSource(ExperimentSettings settings)
        {
            if (settings == null)
                return "NotUsed";
            if (ChengTreqCalculator.IsChengTreqMode(settings.ThresholdMode))
                return "ChengTreq";
            if (String.Equals(settings.ThresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase))
                return "TreqSeconds";
            return "Percent";
        }

        private static List<List<object>> BuildSettingsRows(ExperimentBatchResult result)
        {
            ExperimentSettings s = result.Settings;
            ChengTreqMetrics chengMetrics = ChengTreqCalculator.Compute(s, s.NmaxTask);
            double effectiveTreqSeconds = ChengTreqCalculator.GetEffectiveRequestThresholdSeconds(s, s.NmaxTask);
            string treqSource = ResolveTreqSource(s);
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("欄位", "值", "說明"));
            rows.Add(Row("實作根目錄", s.ProjectRoot, "所有修改與輸出都在 WSN 目錄"));
            rows.Add(Row("基礎亂數種子", s.BaseSeed, "第 i 個 run 使用 seed+i-1"));
            rows.Add(Row("Run 次數", s.RunCount, ""));
            rows.Add(Row("最大平行工作數", s.MaxParallelJobs == 0 ? "自動" : s.MaxParallelJobs.ToString(CultureInfo.InvariantCulture), "0=自動使用 CPU 邏輯核心數；可手動調高或降低"));
            rows.Add(Row("SweepEnabled", s.SweepEnabled, "true = run selected algorithms across iterated parameter values"));
            rows.Add(Row("SweepParameterKey", s.SweepEnabled ? s.SweepParameterKey : "", s.SweepEnabled ? ExperimentSweepParameterCatalog.DisplayName(s.SweepParameterKey) : ""));
            rows.Add(Row("SweepIterationCount", s.SweepEnabled ? (object)s.SweepIterationCount : "", "number of increments after the current UI value"));
            rows.Add(Row("SweepStepValue", s.SweepEnabled ? (object)s.SweepStepValue : "", "value added on each increment"));
            rows.Add(Row("WriteTaskDetailCsv", s.WriteTaskDetailCsv, "false = skip per-run debug CSV files to reduce IO and memory pressure"));
            rows.Add(Row("WriteMissionDetailsCsv", s.WriteMissionDetailsCsv, "controls mission-details.csv output"));
            rows.Add(Row("WriteTaskRecordsCsv", s.WriteTaskRecordsCsv, "controls task-records.csv output"));
            rows.Add(Row("WriteBprDebugCsv", s.WriteBprDebugCsv, "controls bpr-debug.csv output"));
            rows.Add(Row("WriteYuBprDebugCsv", s.WriteYuBprDebugCsv, "controls yu-bpr-debug.csv output"));
            rows.Add(Row("UseFastSimulationScheduling", s.UseFastSimulationScheduling, "true = bounded artifact queue with simulation-level parallelism"));
            rows.Add(Row("感測器數量", s.SensorCount, ""));
            rows.Add(Row("地圖邊長(m)", s.MapWidthMeters, "地圖固定為 n x n 正方形"));
            rows.Add(Row("模擬時間(s)", s.SimulationTimeSeconds, ""));
            rows.Add(Row("初始能量(J)", s.InitialEnergyJ, ""));
            rows.Add(Row("初始能量(nJ)", s.InitialEnergyJ * 1000000000.0, "內部換算"));
            rows.Add(Row("背景壽命(s)", s.SensorBackgroundLifetimeSeconds, "滿電連續耗能耗盡時間"));
            rows.Add(Row("基礎連續耗能(J/s)", s.InitialEnergyJ / s.SensorBackgroundLifetimeSeconds, ""));
            rows.Add(Row("基礎連續耗能(nJ/tick)", s.InitialEnergyJ / s.SensorBackgroundLifetimeSeconds * 1000000000.0 * 0.01, "tick=0.01s"));
            rows.Add(Row("需求頻率 p(次/s)", s.EventRatePerSecond, "CHENG charging requirement / activation frequency; not packet event rate"));
            rows.Add(Row("CriticalDensityRadiusMeters", s.CriticalDensityRadiusMeters, "critical density radius"));
            rows.Add(Row("WCV 速度(m/s)", s.WcvSpeedMetersPerSecond, ""));
            rows.Add(Row("WCV 充電速率(J/s)", s.WcvChargeRateJPerSecond, ""));
            rows.Add(Row("WCV 容量(J)", s.WcvCapacityJ, ""));
            rows.Add(Row("WCV 移動耗能(J/m)", s.WcvMoveCostJPerMeter, ""));
            rows.Add(Row("每趟任務上限", s.NmaxTask, s.DynamicNmaxTask ? "動態上限" : "固定上限"));
            rows.Add(Row("門檻模式", s.ThresholdMode == "TreqSeconds" ? "Treq 秒數門檻" : (s.ThresholdMode == "ChengTreq" ? "CHENG Treq 自動門檻" : "百分比門檻"), ""));
            List<WcvMaxTaskFeasibilityResult> feasibilityResults = WcvMaxTaskFeasibilityValidator.ValidateSelectedAlgorithms(s);
            for (int i = 0; i < feasibilityResults.Count; i++)
            {
                WcvMaxTaskFeasibilityResult feasibility = feasibilityResults[i];
                string prefix = String.Format(CultureInfo.InvariantCulture, "Validation[{0}]", i + 1);
                rows.Add(Row(prefix + ".SelectedAlgorithm", feasibility.Algorithm, "preflight WCV capacity validation"));
                rows.Add(Row(prefix + ".ValidationTaskLimit", feasibility.ValidationTaskLimit, "task limit used by validation"));
                rows.Add(Row(prefix + ".EstimatedMaxTaskMissionEnergyJ", feasibility.EstimatedMaxTaskMissionEnergyJ, "preflight WCV capacity validation"));
                rows.Add(Row(prefix + ".EstimatedMaxTaskMissionPathLengthMeters", feasibility.EstimatedMaxTaskMissionPathLengthMeters, "preflight WCV capacity validation"));
                rows.Add(Row(prefix + ".EstimatedMaxTaskMoveEnergyJ", feasibility.EstimatedMaxTaskMoveEnergyJ, "preflight WCV capacity validation"));
                rows.Add(Row(prefix + ".EstimatedMaxTaskChargeEnergyJ", feasibility.EstimatedMaxTaskChargeEnergyJ, "preflight WCV capacity validation"));
                rows.Add(Row(prefix + ".EstimatedFullChargeSeconds", feasibility.EstimatedFullChargeSeconds, "InitialEnergyJ / WcvChargeRateJPerSecond"));
                rows.Add(Row(prefix + ".EstimatedMaxTaskMissionSeconds", feasibility.EstimatedMaxTaskMissionSeconds, "move seconds + charge seconds"));
                rows.Add(Row(prefix + ".WcvCapacityJ", s.WcvCapacityJ, "WCV capacity used by validation"));
            }
            rows.Add(Row("ThresholdMode", s.ThresholdMode, "Percent / TreqSeconds / ChengTreq"));
            rows.Add(Row("RequestThresholdPercent", s.RequestThresholdPercent, "used when ThresholdMode = Percent"));
            rows.Add(Row("TreqSeconds", effectiveTreqSeconds, "effective Treq seconds used by time-threshold modes"));
            rows.Add(Row("TreqSource", treqSource, "Percent / TreqSeconds / ChengTreq"));
            rows.Add(Row("ConfiguredTreqSeconds", s.TreqSeconds, "manual TreqSeconds setting"));
            rows.Add(Row("EffectiveTreqSeconds", effectiveTreqSeconds, "actual Treq used by time-threshold modes"));
            rows.Add(Row("ComputedLpathMeters", chengMetrics.LpathMeters, "CHENG/NJF path length upper bound"));
            rows.Add(Row("ComputedTjobSeconds", chengMetrics.TjobSeconds, "CHENG mission time Tjob(NmaxTask)"));
            rows.Add(Row("ComputedLmaxStepMeters", chengMetrics.LmaxStepMeters, "map diagonal / farthest two-point step"));
            rows.Add(Row("BP&R deadline threshold(s)", s.BprDeadlineThresholdSeconds, "Persistent STable deadline maintenance threshold"));
            rows.Add(Row("BprPredictionHorizonSource", BprTimingValidator.ResolvePredictionHorizonSource(s), "Explicit / ChengTreq / TreqSeconds / InvalidPercentMode"));
            rows.Add(Row("BprCooldownSource", BprTimingValidator.ResolveCooldownSource(s), "Explicit / ChengTreq / TreqSeconds / InvalidPercentMode"));
            rows.Add(Row("BprDeadlineThresholdSource", BprTimingValidator.ResolveDeadlineThresholdSource(s), "Explicit / ChengTreq / TreqSeconds / InvalidPercentMode"));
            rows.Add(Row("AllowStandaloneProactiveDispatch", s.AllowStandaloneProactiveDispatch, "false = BP&R/YU proactive tasks are inserted only into natural-request missions"));
            rows.Add(Row("ProactivePredictionHorizonSeconds", s.ProactivePredictionHorizonSeconds, "0 = ChengTreq/TreqSeconds time base + EstimateBprTjobSeconds(NmaxTask)"));
            rows.Add(Row("ProactiveCandidateMaxEnergyRatio", s.ProactiveCandidateMaxEnergyRatio, "nodes at or above this capacity ratio are excluded from proactive candidates"));
            rows.Add(Row("ProactiveCooldownSeconds", s.ProactiveCooldownSeconds, "0 = ChengTreq/TreqSeconds time base after charged or proactive-selected"));
            rows.Add(Row("YU danger window(s)", s.YuDangerWindowSeconds, "legacy/debug only; formal YU uses EstimateBprTjobSeconds(NmaxTask)"));
            rows.Add(Row("YU danger threshold K", s.YuDangerThresholdK, "legacy/debug only; formal YU uses overlap count > NmaxTask"));
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
            rows.Add(Row("BP&R 標註", "CHENG/YU/ROUTE BP&R", "CHENG/YU 使用 CHENG-style danger window；非 ROUTE 版 random 選點，ROUTE 版 route insertion cost 選點；LIMITED / EXTENDED 只控制 cplist 容量"));
            rows.Add(Row("任務明細總列數", result.TotalTaskRecordCount, "逐節點 task records 已改寫入 CSV，不再輸出到 Excel"));

            rows.Add(Row("", "", ""));
            rows.Add(Row("Run", "Seed", "ArtifactHash", "RateChangeScheduleCount"));
            for (int i = 0; i < result.ArtifactSummaries.Count; i++)
            {
                ExperimentArtifactSummary artifact = result.ArtifactSummaries[i];
                rows.Add(Row(artifact.RunIndex, artifact.Seed, artifact.ArtifactHash, artifact.RateChangeScheduleCount));
            }

            return rows;
        }

        private static List<List<object>> BuildRunRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("Run", "Seed", "Algorithm", "ArtifactHash", "PrateChange", "RateChangeVariationPercent",
                "RateChangeScheduleCount", "AppliedRateChangeCount", "NetworkLifetimeSeconds",
                "FirstDeadNodeId", "FirstDeadTimeSeconds", "FirstDeadReason", "FirstDeadDirectEnergyCause",
                "SuccessfulCharges", "FailedOrLateTasks", "NaturalRequestCount", "ProactiveTaskCount",
                "TotalChargingTaskCount", "MissionCount", "MovementDistanceMeters",
                "AverageWaitSeconds", "ChargeEfficiency"));

            PrependSweepHeaders(rows[0]);
            AppendRunAntiInflationHeaders(rows[0]);
            AppendRunDeathDiagnosisHeaders(rows[0]);

            for (int i = 0; i < result.RunSummaries.Count; i++)
            {
                ExperimentRunSummary s = result.RunSummaries[i];
                rows.Add(Row(s.RunIndex, s.Seed, AlgorithmDisplayName(s.Algorithm), s.ArtifactHash, s.PrateChange, s.RateChangeVariationPercent, s.RateChangeScheduleCount,
                    s.AppliedRateChangeCount, s.NetworkLifetimeSeconds, s.FirstDeadNodeId, s.FirstDeadTimeSeconds,
                    s.FirstDeadReasonZh, s.FirstDeadDirectEnergyCause, s.SuccessfulCharges, s.FailedOrLateTasks,
                    s.NaturalRequestCount, s.ProactiveTaskCount, s.TotalChargingTaskCount, s.MissionCount, s.MovementDistanceMeters,
                    s.AverageWaitSeconds, s.ChargeEfficiency));
                PrependSweepValues(rows[rows.Count - 1], s);
                AddRunAntiInflationValues(rows[rows.Count - 1], s);
                AddRunDeathDiagnosisValues(rows[rows.Count - 1], s);
            }

            return rows;
        }

        private static void PrependSweepHeaders(List<object> row)
        {
            row.Insert(0, "SweepValue");
            row.Insert(0, "SweepParameterName");
            row.Insert(0, "SweepParameterKey");
            row.Insert(0, "SweepIndex");
        }

        private static void PrependSweepValues(List<object> row, ExperimentRunSummary s)
        {
            row.Insert(0, s.SweepEnabled ? (object)s.SweepValue : "");
            row.Insert(0, s.SweepParameterName ?? "");
            row.Insert(0, s.SweepParameterKey ?? "");
            row.Insert(0, s.SweepEnabled ? (object)s.SweepIndex : "");
        }

        private static void PrependSweepValues(List<object> row, ExperimentDeathRecord d)
        {
            row.Insert(0, d.SweepEnabled ? (object)d.SweepValue : "");
            row.Insert(0, d.SweepParameterName ?? "");
            row.Insert(0, d.SweepParameterKey ?? "");
            row.Insert(0, d.SweepEnabled ? (object)d.SweepIndex : "");
        }

        private static void PrependBlankSweepValues(List<object> row)
        {
            row.Insert(0, "");
            row.Insert(0, "");
            row.Insert(0, "");
            row.Insert(0, "");
        }

        private static void AppendRunAntiInflationHeaders(List<object> row)
        {
            row.Add("UniqueServedNodeCount");
            row.Add("RepeatChargeCount");
            row.Add("PlannedProactiveTaskCount");
            row.Add("ExecutedProactiveTaskCount");
            row.Add("ProactiveNearFullCount");
            row.Add("MeaningfulProactiveCount");
            row.Add("AverageDeliveredEnergyPerTask");
            row.Add("AverageDeliveredEnergyPerProactiveTask");
        }

        private static void AddRunAntiInflationValues(List<object> row, ExperimentRunSummary s)
        {
            row.Add(s.UniqueServedNodeCount);
            row.Add(s.RepeatChargeCount);
            row.Add(s.PlannedProactiveTaskCount);
            row.Add(s.ExecutedProactiveTaskCount);
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

        private static string SummaryGroupKey(ExperimentRunSummary summary)
        {
            if (summary == null)
                return "";
            if (!summary.SweepEnabled)
                return summary.Algorithm ?? "";
            return String.Format(CultureInfo.InvariantCulture, "{0}|{1}|{2}|{3}",
                summary.SweepIndex,
                summary.SweepParameterKey,
                summary.SweepValue,
                summary.Algorithm);
        }

        private static List<List<object>> BuildSummaryRows(ExperimentBatchResult result)
        {
            List<List<object>> rows = new List<List<object>>();
            rows.Add(Row("Algorithm", "RunCount", "AverageNetworkLifetimeSeconds",
                "MinNetworkLifetimeSeconds", "MaxNetworkLifetimeSeconds", "AverageSuccessfulCharges",
                "AverageFailedOrLateTasks", "AverageNaturalRequestCount", "AverageProactiveTaskCount",
                "AverageTotalChargingTaskCount", "AverageMissionCount", "AverageMovementDistanceMeters",
                "AverageWaitSeconds", "AverageChargeEfficiency"));

            PrependSweepHeaders(rows[0]);
            rows[0].Add("ThresholdMode");
            rows[0].Add("RequestThresholdPercent");
            rows[0].Add("TreqSeconds");
            rows[0].Add("TreqSource");

            AppendSummaryAntiInflationHeaders(rows[0]);

            Dictionary<string, List<ExperimentRunSummary>> groups = new Dictionary<string, List<ExperimentRunSummary>>();
            for (int i = 0; i < result.RunSummaries.Count; i++)
            {
                string groupKey = SummaryGroupKey(result.RunSummaries[i]);
                if (!groups.ContainsKey(groupKey))
                    groups[groupKey] = new List<ExperimentRunSummary>();
                groups[groupKey].Add(result.RunSummaries[i]);
            }

            foreach (KeyValuePair<string, List<ExperimentRunSummary>> pair in groups)
            {
                List<ExperimentRunSummary> list = pair.Value;
                ExperimentRunSummary first = list[0];
                rows.Add(Row(AlgorithmDisplayName(first.Algorithm), list.Count,
                    Average(list, delegate (ExperimentRunSummary s) { return s.NetworkLifetimeSeconds; }),
                    Min(list, delegate (ExperimentRunSummary s) { return s.NetworkLifetimeSeconds; }),
                    Max(list, delegate (ExperimentRunSummary s) { return s.NetworkLifetimeSeconds; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.SuccessfulCharges; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.FailedOrLateTasks; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.NaturalRequestCount; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.ProactiveTaskCount; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.TotalChargingTaskCount; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.MissionCount; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.MovementDistanceMeters; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.AverageWaitSeconds; }),
                    Average(list, delegate (ExperimentRunSummary s) { return s.ChargeEfficiency; })));
                PrependSweepValues(rows[rows.Count - 1], first);
                rows[rows.Count - 1].Add(first.ThresholdMode);
                rows[rows.Count - 1].Add(first.RequestThresholdPercent);
                rows[rows.Count - 1].Add(first.EffectiveTreqSeconds);
                rows[rows.Count - 1].Add(first.TreqSource);
                AddSummaryAntiInflationValues(rows[rows.Count - 1], list);
            }

            return rows;
        }

        private static void AppendSummaryAntiInflationHeaders(List<object> row)
        {
            row.Add("平均UniqueServedNodeCount");
            row.Add("平均RepeatChargeCount");
            row.Add("平均PlannedProactiveTaskCount");
            row.Add("平均ExecutedProactiveTaskCount");
            row.Add("平均ProactiveNearFullCount");
            row.Add("平均MeaningfulProactiveCount");
            row.Add("平均DeliveredEnergyPerTask");
            row.Add("平均DeliveredEnergyPerProactiveTask");
        }

        private static void AddSummaryAntiInflationValues(List<object> row, List<ExperimentRunSummary> list)
        {
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.UniqueServedNodeCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.RepeatChargeCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.PlannedProactiveTaskCount; }));
            row.Add(Average(list, delegate (ExperimentRunSummary s) { return s.ExecutedProactiveTaskCount; }));
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
                "EnergyBeforeDeathJ",
                "BaseConsumeRateJPerSecondAtDeath", "EffectiveConsumeRateJPerSecondAtDeath",
                "EnergyJ", "RequestTimeSeconds", "WaitSeconds"));
            PrependSweepHeaders(rows[0]);

            if (result.DeathRecords.Count == 0)
            {
                rows.Add(Row("NO_DEATH", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", "", ""));
                PrependBlankSweepValues(rows[rows.Count - 1]);
                return rows;
            }

            for (int i = 0; i < result.DeathRecords.Count; i++)
            {
                ExperimentDeathRecord d = result.DeathRecords[i];
                rows.Add(Row(d.RunIndex, d.Seed, AlgorithmDisplayName(d.Algorithm), d.ArtifactHash, d.TimeSeconds, d.NodeId,
                    d.Reason, d.ReasonZh, d.DirectEnergyCause, d.DirectEnergyCauseZh,
                    d.SchedulingRelated, d.SchedulingCause, d.SchedulingCauseZh,
                    d.PendingRequest, d.HasPendingRequestAtDeath, d.WasScheduledInCurrentMissionAtDeath,
                    d.EnergyBeforeDeathJ,
                    d.BaseConsumeRateJPerSecondAtDeath, d.EffectiveConsumeRateJPerSecondAtDeath,
                    d.EnergyJ, d.RequestTimeSeconds, d.WaitSeconds));
                PrependSweepValues(rows[rows.Count - 1], d);
            }

            return rows;
        }

        private static string AlgorithmDisplayName(string key)
        {
            if (key == "EDF") return "EDF（最早期限優先）";
            if (key == "NJF") return "NJF（no prediction baseline）";
            if (key == "TADP_LIN") return "TADP/LIN（時間與距離優先）";
            if (key == "NJF_CHENG_BPR") return "NJF_CHENG_BPR (CHENG paper BP&R, seeded random)";
            if (key == "TADP_CHENG_BPR") return "TADP_CHENG_BPR (CHENG paper BP&R, seeded random)";
            if (key == "EDF_CHENG_BPR") return "EDF_CHENG_BPR (CHENG paper BP&R, seeded random)";
            if (key == "NJF_YU_BPR") return "NJF_YU_BPR (YU interval BP&R, seeded random)";
            if (key == "NJF_ROUTE_CHENG_BPR_LIMITED") return "NJF_ROUTE_CHENG_BPR_LIMITED (CHENG interval BP&R, route insertion cost, <=NmaxTask)";
            if (key == "NJF_ROUTE_CHENG_BPR_EXTENDED") return "NJF_ROUTE_CHENG_BPR_EXTENDED (CHENG interval BP&R, route insertion cost, may exceed NmaxTask)";
            if (key == "NJF_ROUTE_YU_BPR_LIMITED") return "NJF_ROUTE_YU_BPR_LIMITED (YU interval BP&R, route insertion cost, <=NmaxTask)";
            if (key == "NJF_ROUTE_YU_BPR_EXTENDED") return "NJF_ROUTE_YU_BPR_EXTENDED (YU interval BP&R, route insertion cost, may exceed NmaxTask)";
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

    public class ExperimentSweepSettingsDialog : Form
    {
        private readonly ExperimentSettings settings;
        private readonly CheckedListBox parameterList;
        private readonly CheckBox enableSweepBox;
        private readonly TextBox iterationCountBox;
        private readonly TextBox stepValueBox;
        private readonly Label previewLabel;
        private bool updatingChecks;

        public ExperimentSweepSettingsDialog(ExperimentSettings currentSettings)
        {
            settings = currentSettings;
            settings.Normalize();
            parameterList = new CheckedListBox();
            enableSweepBox = new CheckBox();
            iterationCountBox = new TextBox();
            stepValueBox = new TextBox();
            previewLabel = new Label();
            InitializeDialog();
            LoadSettingsToControls();
        }

        private void InitializeDialog()
        {
            Text = "參數迭代設定";
            Width = 560;
            Height = 520;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(16);
            Controls.Add(panel);

            enableSweepBox.Text = "啟用參數迭代";
            enableSweepBox.Location = new System.Drawing.Point(18, 18);
            enableSweepBox.Size = new System.Drawing.Size(180, 24);
            enableSweepBox.CheckedChanged += delegate { UpdateEnabledState(); UpdatePreview(); };
            panel.Controls.Add(enableSweepBox);

            Label parameterLabel = new Label();
            parameterLabel.Text = "選擇要迭代的參數（一次一個）";
            parameterLabel.Location = new System.Drawing.Point(18, 54);
            parameterLabel.Size = new System.Drawing.Size(300, 22);
            panel.Controls.Add(parameterLabel);

            parameterList.CheckOnClick = true;
            parameterList.Location = new System.Drawing.Point(18, 80);
            parameterList.Size = new System.Drawing.Size(500, 210);
            parameterList.ItemCheck += parameterList_ItemCheck;
            panel.Controls.Add(parameterList);

            Label iterationLabel = new Label();
            iterationLabel.Text = "迭代次數（不含目前值）";
            iterationLabel.Location = new System.Drawing.Point(18, 308);
            iterationLabel.Size = new System.Drawing.Size(180, 22);
            panel.Controls.Add(iterationLabel);

            iterationCountBox.Location = new System.Drawing.Point(210, 305);
            iterationCountBox.Size = new System.Drawing.Size(110, 22);
            iterationCountBox.TextChanged += delegate { UpdatePreview(); };
            panel.Controls.Add(iterationCountBox);

            Label stepLabel = new Label();
            stepLabel.Text = "每次增加量";
            stepLabel.Location = new System.Drawing.Point(18, 340);
            stepLabel.Size = new System.Drawing.Size(180, 22);
            panel.Controls.Add(stepLabel);

            stepValueBox.Location = new System.Drawing.Point(210, 337);
            stepValueBox.Size = new System.Drawing.Size(110, 22);
            stepValueBox.TextChanged += delegate { UpdatePreview(); };
            panel.Controls.Add(stepValueBox);

            previewLabel.Location = new System.Drawing.Point(18, 376);
            previewLabel.Size = new System.Drawing.Size(500, 44);
            panel.Controls.Add(previewLabel);

            Button okButton = new Button();
            okButton.Text = "確定";
            okButton.Location = new System.Drawing.Point(330, 432);
            okButton.Size = new System.Drawing.Size(85, 28);
            okButton.Click += okButton_Click;
            panel.Controls.Add(okButton);

            Button cancelButton = new Button();
            cancelButton.Text = "取消";
            cancelButton.Location = new System.Drawing.Point(430, 432);
            cancelButton.Size = new System.Drawing.Size(85, 28);
            cancelButton.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };
            panel.Controls.Add(cancelButton);
        }

        private void LoadSettingsToControls()
        {
            ExperimentSweepParameterDefinition[] definitions = ExperimentSweepParameterCatalog.All();
            parameterList.Items.Clear();
            updatingChecks = true;
            for (int i = 0; i < definitions.Length; i++)
            {
                parameterList.Items.Add(definitions[i].DisplayName + " [" + definitions[i].Key + "]",
                    String.Equals(definitions[i].Key, settings.SweepParameterKey, StringComparison.OrdinalIgnoreCase));
            }
            updatingChecks = false;

            enableSweepBox.Checked = settings.SweepEnabled;
            iterationCountBox.Text = settings.SweepIterationCount.ToString(CultureInfo.InvariantCulture);
            stepValueBox.Text = settings.SweepStepValue.ToString(CultureInfo.InvariantCulture);
            UpdateEnabledState();
            UpdatePreview();
        }

        private void parameterList_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (updatingChecks || e.NewValue != CheckState.Checked)
                return;
            if (!IsHandleCreated)
                return;

            updatingChecks = true;
            BeginInvoke(new Action(delegate
            {
                for (int i = 0; i < parameterList.Items.Count; i++)
                {
                    if (i != e.Index)
                        parameterList.SetItemChecked(i, false);
                }
                updatingChecks = false;
                UpdatePreview();
            }));
        }

        private void UpdateEnabledState()
        {
            bool enabled = enableSweepBox.Checked;
            parameterList.Enabled = enabled;
            iterationCountBox.Enabled = enabled;
            stepValueBox.Enabled = enabled;
        }

        private void UpdatePreview()
        {
            if (!enableSweepBox.Checked)
            {
                previewLabel.Text = "停用時會照目前主畫面設定只跑一組參數。";
                return;
            }

            ExperimentSweepParameterDefinition definition = SelectedDefinition();
            if (definition == null)
            {
                previewLabel.Text = "請勾選一個要迭代的參數。";
                return;
            }

            int iterations;
            double step;
            if (!Int32.TryParse(iterationCountBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out iterations))
            {
                previewLabel.Text = "迭代次數必須是整數。";
                return;
            }
            if (!Double.TryParse(stepValueBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out step))
            {
                previewLabel.Text = "每次增加量必須是數字。";
                return;
            }

            double start = definition.GetValue(settings);
            double end = start + iterations * step;
            previewLabel.Text = String.Format(CultureInfo.InvariantCulture,
                "將執行目前值加上 {0} 次遞增，共 {1} 組：{2} {3} -> {4}",
                Math.Max(0, iterations),
                Math.Max(0, iterations) + 1,
                definition.DisplayName,
                definition.FormatValue(start),
                definition.FormatValue(end));
        }

        private ExperimentSweepParameterDefinition SelectedDefinition()
        {
            ExperimentSweepParameterDefinition[] definitions = ExperimentSweepParameterCatalog.All();
            for (int i = 0; i < parameterList.Items.Count && i < definitions.Length; i++)
            {
                if (parameterList.GetItemChecked(i))
                    return definitions[i];
            }
            return null;
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (!enableSweepBox.Checked)
            {
                settings.SweepEnabled = false;
                settings.Normalize();
                DialogResult = DialogResult.OK;
                Close();
                return;
            }

            ExperimentSweepParameterDefinition definition = SelectedDefinition();
            if (definition == null)
            {
                MessageBox.Show(this, "請勾選一個要迭代的參數。", "參數迭代設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            int iterations;
            if (!Int32.TryParse(iterationCountBox.Text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out iterations) || iterations < 1)
            {
                MessageBox.Show(this, "迭代次數必須是 1 以上的整數。", "參數迭代設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double step;
            if (!Double.TryParse(stepValueBox.Text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out step) || step == 0.0)
            {
                MessageBox.Show(this, "每次增加量必須是不等於 0 的數字。", "參數迭代設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (definition.IntegerOnly && Math.Abs(step - Math.Round(step)) > 0.000001)
            {
                MessageBox.Show(this, "這個參數是整數，增加量也必須是整數。", "參數迭代設定", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            settings.SweepEnabled = true;
            settings.SweepParameterKey = definition.Key;
            settings.SweepIterationCount = iterations;
            settings.SweepStepValue = step;
            settings.Normalize();
            DialogResult = DialogResult.OK;
            Close();
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
            AddTextBox(panel, "EventRatePerSecond", "需求頻率 p(次/s)", y); y += 30;
            AddTextBox(panel, "CriticalDensityRadiusMeters", "Critical density radius(m)", y); y += 30;
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
            AddTextBox(panel, "YuDangerWindowSeconds", "YU danger window(s, legacy)", y); y += 30;
            AddTextBox(panel, "YuDangerThresholdK", "YU danger threshold K (legacy)", y); y += 30;
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
            boxes["CriticalDensityRadiusMeters"].Text = settings.CriticalDensityRadiusMeters.ToString(CultureInfo.InvariantCulture);
            boxes["WcvSpeedMetersPerSecond"].Text = settings.WcvSpeedMetersPerSecond.ToString(CultureInfo.InvariantCulture);
            boxes["WcvChargeRateJPerSecond"].Text = settings.WcvChargeRateJPerSecond.ToString(CultureInfo.InvariantCulture);
            boxes["WcvCapacityJ"].Text = settings.WcvCapacityJ.ToString(CultureInfo.InvariantCulture);
            boxes["WcvMoveCostJPerMeter"].Text = settings.WcvMoveCostJPerMeter.ToString(CultureInfo.InvariantCulture);
            boxes["NmaxTask"].Text = settings.NmaxTask.ToString(CultureInfo.InvariantCulture);
            boxes["ThresholdMode"].Text = settings.ThresholdMode == "TreqSeconds" ? "Treq 秒數門檻" : "百分比門檻";
            if (settings.ThresholdMode == "ChengTreq")
                boxes["ThresholdMode"].Text = "ChengTreq";
            else if (settings.ThresholdMode == "TreqSeconds")
                boxes["ThresholdMode"].Text = "TreqSeconds";
            else
                boxes["ThresholdMode"].Text = "Percent";
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
            settings.CriticalDensityRadiusMeters = ParseDouble("CriticalDensityRadiusMeters", settings.CriticalDensityRadiusMeters);
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
            if (value.IndexOf("Cheng", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("Auto", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("自動", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return "ChengTreq";
            }
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
            if (key == "NJF_CHENG_BPR") return "NJF_CHENG_BPR (CHENG paper BP&R, seeded random)";
            if (key == "TADP_CHENG_BPR") return "TADP_CHENG_BPR (CHENG paper BP&R, seeded random)";
            if (key == "EDF_CHENG_BPR") return "EDF_CHENG_BPR (CHENG paper BP&R, seeded random)";
            if (key == "NJF_YU_BPR") return "NJF_YU_BPR (YU interval BP&R, seeded random)";
            if (key == "NJF_ROUTE_CHENG_BPR_LIMITED") return "NJF_ROUTE_CHENG_BPR_LIMITED (CHENG interval BP&R, route insertion cost, <=NmaxTask)";
            if (key == "NJF_ROUTE_CHENG_BPR_EXTENDED") return "NJF_ROUTE_CHENG_BPR_EXTENDED (CHENG interval BP&R, route insertion cost, may exceed NmaxTask)";
            if (key == "NJF_ROUTE_YU_BPR_LIMITED") return "NJF_ROUTE_YU_BPR_LIMITED (YU interval BP&R, route insertion cost, <=NmaxTask)";
            if (key == "NJF_ROUTE_YU_BPR_EXTENDED") return "NJF_ROUTE_YU_BPR_EXTENDED (YU interval BP&R, route insertion cost, may exceed NmaxTask)";
            return key;
        }

        private string AlgorithmKeyFromDisplay(string display)
        {
            if (String.IsNullOrWhiteSpace(display))
                return "";
            int index = display.IndexOf('（');
            if (index < 0)
                index = display.IndexOf('(');
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
