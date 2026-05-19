using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Reflection;
using WindowsFormsApplication1;

// command line format : exefilename 地圖檔名 事件檔名 使用節點數 輸出檔名 POWER_VAL 初始電量 power_range
// genetic: rebuild_tree 不可清事件, Go 時 prange 不要重設 
namespace WindowsFormsApplication1
{

    public partial class Form1 : Form
    {
        int Aheight, Awidth, maxbase, maxnode, Powerrange, total_time, maxlayer;
        int start_time = 0;
        int ER;
        int last_event_time;

        int hop1_residual;
        int POWER_VAL = 2;  // 電力與距離的關係次方數
        int[,] dist_prange = new int[10, 2];
        int[] nodes_num = new int[20];
        int data_sent, data_loss, data_recv, first_dead_time, event_loss, rec_time, dead_node;
        int num_event;
        StreamWriter ofs;
        StreamWriter rateChangeOfs;
        StreamWriter lowResidualOfs;
        StreamWriter zeroResidualOfs;
        bool silent, autoload;
        bool[] lowResidualBelowState;
        bool[] zeroResidualReachedState;
        char[] recv = new char[35001];
        bool assign_prange = true;
        double mu;
        int predict_count = 0;
        int predict_out_window_count = 0;
        string rateChangeLogPath = "";
        int currentBatchRunIndex = 1;
        int currentBatchRunCount = 1;
        Bitmap latestNodeFrame;

        const double PREDICT_SPEED_BAND_RATIO = 0.25;   // 速度區間寬度比例
        const double PREDICT_SAFETY_FLOOR_RATIO = 0.25; // 安全電量下限比例
        const double PREDICT_SPEED_FLOOR = 1.0;         // 防止除以 0
        const double LOW_RESIDUAL_RATIO = 0.10;         // 剩餘 10% 電量門檻
        const double ZERO_RESIDUAL_RATIO = 0.0;         // 剩餘 0% 電量門檻
        const double REQUEST_STATUS_BAND_RATIO = 0.05;  // 充電請求基準上下 5%
        const int NORMAL_VISUAL_REFRESH_INTERVAL = 100;
        const int SILENT_VISUAL_REFRESH_INTERVAL = 20;
        const int CHARGING_HIGHLIGHT_RADIUS = 7;
        const string DEFAULT_STORAGE_DIRECTORY = @"C:\temp";
        const string DEFAULT_OUTPUT_PATH = @"C:\temp\result.csv";
        const string DEFAULT_OUTPUT_DIRECTORY = DEFAULT_STORAGE_DIRECTORY;
        Button experimentSaveButton;
        Button experimentRunButton;
        ExperimentSettings experimentSettings;
        Panel experimentWorkspace;
        TextBox expSeedBox;
        TextBox expRunCountBox;
        TextBox expSensorCountBox;
        TextBox expMapSizeBox;
        TextBox expSimulationTimeBox;
        TextBox expMaxParallelJobsBox;
        TextBox expInitialEnergyBox;
        TextBox expBackgroundLifetimeBox;
        TextBox expEventRateBox;
        TextBox expPrateChangeBox;
        TextBox expRateChangeVariationBox;
        TextBox expRequestThresholdBox;
        TextBox expTreqBox;
        TextBox expWcvSpeedBox;
        TextBox expWcvChargeRateBox;
        TextBox expWcvCapacityBox;
        TextBox expWcvMoveCostBox;
        TextBox expNmaxTaskBox;
        TextBox expOutputDirectoryBox;
        ComboBox expThresholdModeBox;
        CheckedListBox expAlgorithmList;
        TextBox expLogBox;
        Label expLastOutputLabel;
        Label expProgressLabel;
        
        public Form1()
        {
            InitializeComponent();
            initialize_visual_panel();
            initialize_experiment_panel();
            common.nmap = new nodemap(1,2000);

        }

        void initialize_visual_panel()
        {
            try
            {
                PropertyInfo doubleBufferedProperty = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
                if (doubleBufferedProperty != null)
                    doubleBufferedProperty.SetValue(panel2, true, null);
            }
            catch
            {
            }

            panel2.Paint += panel2_Paint;
        }

        void initialize_experiment_panel()
        {
            experimentSettings = ExperimentSettings.LoadLast();

            build_chinese_experiment_workspace();
        }

        void build_chinese_experiment_workspace()
        {
            foreach (Control control in Controls)
                control.Visible = false;

            Text = "WSN 新實驗系統";
            WindowState = FormWindowState.Normal;
            ClientSize = new Size(1180, 850);
            MinimumSize = new Size(1180, 850);

            experimentWorkspace = new Panel();
            experimentWorkspace.Dock = DockStyle.Fill;
            experimentWorkspace.AutoScroll = true;
            experimentWorkspace.BackColor = Color.FromArgb(248, 248, 248);
            experimentWorkspace.Visible = true;
            Controls.Add(experimentWorkspace);
            experimentWorkspace.BringToFront();

            Font titleFont = new Font("Microsoft JhengHei UI", 18, FontStyle.Bold);
            Font normalFont = new Font("Microsoft JhengHei UI", 9);
            Font smallFont = new Font("Microsoft JhengHei UI", 8);
            experimentWorkspace.Font = normalFont;

            Label title = new Label();
            title.Text = "WSN 充電排程實驗系統";
            title.Font = titleFont;
            title.Location = new Point(24, 18);
            title.Size = new Size(520, 36);
            experimentWorkspace.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "只使用 MyWSN 的資料產生概念與參數作為參考；實驗比較由本系統產生共用資料並執行。";
            subtitle.Location = new Point(26, 58);
            subtitle.Size = new Size(760, 24);
            experimentWorkspace.Controls.Add(subtitle);

            GroupBox dataGroup = create_group_box("資料產生與公平比較", 24, 92, 360, 290);
            expSeedBox = add_labeled_textbox(dataGroup, "亂數種子", "每次實驗的基準值", 18, 30, "");
            expRunCountBox = add_labeled_textbox(dataGroup, "重複次數", "批次實驗次數", 18, 64, "");
            expSensorCountBox = add_labeled_textbox(dataGroup, "感測器數量", "不含基地台 BS", 18, 98, "");
            expMapSizeBox = add_labeled_textbox(dataGroup, "地圖邊長(m)", "正方形 n x n", 18, 132, "");
            expSimulationTimeBox = add_labeled_textbox(dataGroup, "模擬時間(s)", "每個演算法跑到此時間或首次死亡", 18, 166, "");
            expMaxParallelJobsBox = add_labeled_textbox(dataGroup, "平行工作數", "0=自動；可手動加速/降載", 18, 200, "MaxParallelJobs");

            GroupBox energyGroup = create_group_box("能量、封包與動態耗能", 404, 92, 360, 290);
            expInitialEnergyBox = add_labeled_textbox(energyGroup, "初始能量(J)", "感測器滿電容量", 18, 30, "");
            expBackgroundLifetimeBox = add_labeled_textbox(energyGroup, "背景壽命(s)", "只靠連續耗能時的滿電壽命", 18, 64, "");
            expEventRateBox = add_labeled_textbox(energyGroup, "事件率(封包/s)", "由 seed 產生封包事件", 18, 98, "");
            expPrateChangeBox = add_labeled_textbox(energyGroup, "耗能變動機率", "每 10000s 檢查", 18, 132, "Prate_change");
            expRateChangeVariationBox = add_labeled_textbox(energyGroup, "變動幅度(%)", "倍率 = 1 ± 此百分比", 18, 166, "預設 12.5");
            expRequestThresholdBox = add_labeled_textbox(energyGroup, "需求門檻(%)", "百分比門檻使用", 18, 200, "");
            expTreqBox = add_labeled_textbox(energyGroup, "Treq 秒數", "秒數門檻使用", 18, 234, "");

            Label modeLabel = new Label();
            modeLabel.Text = "門檻模式";
            modeLabel.Location = new Point(18, 264);
            modeLabel.Size = new Size(96, 22);
            energyGroup.Controls.Add(modeLabel);

            expThresholdModeBox = new ComboBox();
            expThresholdModeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            expThresholdModeBox.Items.Add("百分比門檻");
            expThresholdModeBox.Items.Add("Treq 秒數門檻");
            expThresholdModeBox.Location = new Point(130, 261);
            expThresholdModeBox.Size = new Size(135, 22);
            energyGroup.Controls.Add(expThresholdModeBox);

            GroupBox wcvGroup = create_group_box("WCV 與任務流程", 784, 92, 360, 290);
            expWcvSpeedBox = add_labeled_textbox(wcvGroup, "WCV 速度(m/s)", "單台 WCV", 18, 30, "");
            expWcvChargeRateBox = add_labeled_textbox(wcvGroup, "充電速率(J/s)", "WCV 對節點充電", 18, 64, "");
            expWcvCapacityBox = add_labeled_textbox(wcvGroup, "WCV 容量(J)", "WCV 可用能量", 18, 98, "");
            expWcvMoveCostBox = add_labeled_textbox(wcvGroup, "移動耗能(J/m)", "WCV 每公尺消耗能量", 18, 132, "");
            expNmaxTaskBox = add_labeled_textbox(wcvGroup, "任務上限", "每趟最多服務節點數", 18, 166, "NmaxTask");
            Label bsLabel = new Label();
            bsLabel.Text = "基地台 = sink + 充電中心；每趟任務後 WCV 返回基地台。";
            bsLabel.Location = new Point(18, 204);
            bsLabel.Size = new Size(320, 42);
            wcvGroup.Controls.Add(bsLabel);

            GroupBox algoGroup = create_group_box("演算法選擇", 24, 398, 360, 275);
            expAlgorithmList = new CheckedListBox();
            expAlgorithmList.CheckOnClick = true;
            expAlgorithmList.Location = new Point(18, 28);
            expAlgorithmList.Size = new Size(320, 205);
            algoGroup.Controls.Add(expAlgorithmList);

            Button selectDefaultButton = new Button();
            selectDefaultButton.Text = "勾選預設";
            selectDefaultButton.Location = new Point(18, 238);
            selectDefaultButton.Size = new Size(95, 26);
            selectDefaultButton.Click += delegate { set_default_algorithm_checks(); };
            algoGroup.Controls.Add(selectDefaultButton);

            Button selectAllButton = new Button();
            selectAllButton.Text = "全部勾選";
            selectAllButton.Location = new Point(126, 238);
            selectAllButton.Size = new Size(95, 26);
            selectAllButton.Click += delegate
            {
                for (int i = 0; i < expAlgorithmList.Items.Count; i++)
                    expAlgorithmList.SetItemChecked(i, true);
            };
            algoGroup.Controls.Add(selectAllButton);

            Button clearAlgorithmButton = new Button();
            clearAlgorithmButton.Text = "全部取消";
            clearAlgorithmButton.Location = new Point(234, 238);
            clearAlgorithmButton.Size = new Size(95, 26);
            clearAlgorithmButton.Click += delegate
            {
                for (int i = 0; i < expAlgorithmList.Items.Count; i++)
                    expAlgorithmList.SetItemChecked(i, false);
            };
            algoGroup.Controls.Add(clearAlgorithmButton);

            GroupBox outputGroup = create_group_box("執行與輸出", 404, 398, 740, 275);
            Label outputLabel = new Label();
            outputLabel.Text = "輸出資料夾";
            outputLabel.Location = new Point(18, 31);
            outputLabel.Size = new Size(90, 22);
            outputGroup.Controls.Add(outputLabel);

            expOutputDirectoryBox = new TextBox();
            expOutputDirectoryBox.Location = new Point(110, 28);
            expOutputDirectoryBox.Size = new Size(495, 22);
            outputGroup.Controls.Add(expOutputDirectoryBox);

            Button browseOutputButton = new Button();
            browseOutputButton.Text = "瀏覽";
            browseOutputButton.Location = new Point(616, 27);
            browseOutputButton.Size = new Size(80, 25);
            browseOutputButton.Click += browseOutputButton_Click;
            outputGroup.Controls.Add(browseOutputButton);

            experimentSaveButton = new Button();
            experimentSaveButton.Text = "儲存設定";
            experimentSaveButton.Location = new Point(18, 65);
            experimentSaveButton.Size = new Size(105, 32);
            experimentSaveButton.Click += experimentSaveButton_Click;
            outputGroup.Controls.Add(experimentSaveButton);

            experimentRunButton = new Button();
            experimentRunButton.Text = "執行批次比較";
            experimentRunButton.Location = new Point(136, 65);
            experimentRunButton.Size = new Size(130, 32);
            experimentRunButton.Click += experimentRunButton_Click;
            outputGroup.Controls.Add(experimentRunButton);

            Button openOutputButton = new Button();
            openOutputButton.Text = "開啟輸出資料夾";
            openOutputButton.Location = new Point(282, 65);
            openOutputButton.Size = new Size(130, 32);
            openOutputButton.Click += openOutputButton_Click;
            outputGroup.Controls.Add(openOutputButton);

            expLastOutputLabel = new Label();
            expLastOutputLabel.Text = "";
            expLastOutputLabel.Location = new Point(18, 106);
            expLastOutputLabel.Size = new Size(700, 22);
            outputGroup.Controls.Add(expLastOutputLabel);

            expProgressLabel = new Label();
            expProgressLabel.Text = "進度：尚未執行";
            expProgressLabel.Location = new Point(18, 128);
            expProgressLabel.Size = new Size(700, 22);
            outputGroup.Controls.Add(expProgressLabel);

            expLogBox = new TextBox();
            expLogBox.Multiline = true;
            expLogBox.ReadOnly = true;
            expLogBox.ScrollBars = ScrollBars.Vertical;
            expLogBox.Location = new Point(18, 152);
            expLogBox.Size = new Size(700, 101);
            outputGroup.Controls.Add(expLogBox);

            GroupBox noteGroup = create_group_box("資料產生說明", 24, 690, 1120, 120);
            Label note = new Label();
            note.Text = "本系統不使用舊 MyWSN 的單一節點排程介面。資料由亂數種子產生共用地圖、事件、初始剩餘能量與耗能率變動時間表，所有演算法共用同一資料雜湊碼。MyWSN 僅作為封包能耗、資料格式與參數量級參考。";
            note.Location = new Point(18, 28);
            note.Size = new Size(1070, 50);
            noteGroup.Controls.Add(note);

            Label note2 = new Label();
            note2.Text = "若要比較不同耗能變動機率（Prate_change），請一次只設定一個值並分開執行，例如 0、0.1、0.2、0.3 各跑一份 Excel。";
            note2.Location = new Point(18, 74);
            note2.Size = new Size(1070, 28);
            noteGroup.Controls.Add(note2);

            populate_experiment_controls_from_settings();
            log_experiment_message("已載入新實驗系統。請設定參數後執行批次比較。");
        }

        GroupBox create_group_box(string text, int x, int y, int width, int height)
        {
            GroupBox group = new GroupBox();
            group.Text = text;
            group.Location = new Point(x, y);
            group.Size = new Size(width, height);
            experimentWorkspace.Controls.Add(group);
            return group;
        }

        TextBox add_labeled_textbox(Control parent, string label, string hint, int x, int y, string extraHint)
        {
            Label l = new Label();
            l.Text = label;
            l.Location = new Point(x, y + 4);
            l.Size = new Size(108, 20);
            parent.Controls.Add(l);

            TextBox box = new TextBox();
            box.Location = new Point(x + 112, y);
            box.Size = new Size(112, 22);
            parent.Controls.Add(box);

            Label h = new Label();
            h.Text = String.IsNullOrWhiteSpace(extraHint) ? hint : hint + "；" + extraHint;
            h.Location = new Point(x + 232, y + 4);
            h.Size = new Size(105, 20);
            parent.Controls.Add(h);
            return box;
        }

        void populate_experiment_controls_from_settings()
        {
            experimentSettings.Normalize();
            expSeedBox.Text = experimentSettings.BaseSeed.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expRunCountBox.Text = experimentSettings.RunCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expSensorCountBox.Text = experimentSettings.SensorCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expMapSizeBox.Text = experimentSettings.MapWidthMeters.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expSimulationTimeBox.Text = experimentSettings.SimulationTimeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expMaxParallelJobsBox.Text = experimentSettings.MaxParallelJobs.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expInitialEnergyBox.Text = experimentSettings.InitialEnergyJ.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expBackgroundLifetimeBox.Text = experimentSettings.SensorBackgroundLifetimeSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expEventRateBox.Text = experimentSettings.EventRatePerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expPrateChangeBox.Text = experimentSettings.PrateChange.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expRateChangeVariationBox.Text = experimentSettings.RateChangeVariationPercent.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expRequestThresholdBox.Text = experimentSettings.RequestThresholdPercent.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expTreqBox.Text = experimentSettings.TreqSeconds.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expWcvSpeedBox.Text = experimentSettings.WcvSpeedMetersPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expWcvChargeRateBox.Text = experimentSettings.WcvChargeRateJPerSecond.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expWcvCapacityBox.Text = experimentSettings.WcvCapacityJ.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expWcvMoveCostBox.Text = experimentSettings.WcvMoveCostJPerMeter.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expNmaxTaskBox.Text = experimentSettings.NmaxTask.ToString(System.Globalization.CultureInfo.InvariantCulture);
            expOutputDirectoryBox.Text = experimentSettings.OutputDirectory;
            expThresholdModeBox.SelectedIndex = experimentSettings.ThresholdMode == "TreqSeconds" ? 1 : 0;

            expAlgorithmList.Items.Clear();
            List<string> selected = experimentSettings.GetSelectedAlgorithms();
            string[] all = ExperimentSettings.AllAlgorithms();
            for (int i = 0; i < all.Length; i++)
            {
                string display = get_algorithm_display_name(all[i]);
                expAlgorithmList.Items.Add(display, selected.Contains(all[i]));
            }

            update_last_output_label();
        }

        void apply_experiment_controls_to_settings()
        {
            experimentSettings.BaseSeed = parse_int(expSeedBox, experimentSettings.BaseSeed);
            experimentSettings.RunCount = parse_int(expRunCountBox, experimentSettings.RunCount);
            experimentSettings.SensorCount = parse_int(expSensorCountBox, experimentSettings.SensorCount);
            double mapSize = parse_double(expMapSizeBox, experimentSettings.MapWidthMeters);
            experimentSettings.MapWidthMeters = mapSize;
            experimentSettings.MapHeightMeters = mapSize;
            experimentSettings.SimulationTimeSeconds = parse_double(expSimulationTimeBox, experimentSettings.SimulationTimeSeconds);
            experimentSettings.MaxParallelJobs = parse_int(expMaxParallelJobsBox, experimentSettings.MaxParallelJobs);
            experimentSettings.InitialEnergyJ = parse_double(expInitialEnergyBox, experimentSettings.InitialEnergyJ);
            experimentSettings.SensorBackgroundLifetimeSeconds = parse_double(expBackgroundLifetimeBox, experimentSettings.SensorBackgroundLifetimeSeconds);
            experimentSettings.EventRatePerSecond = parse_double(expEventRateBox, experimentSettings.EventRatePerSecond);
            experimentSettings.PrateChange = parse_double(expPrateChangeBox, experimentSettings.PrateChange);
            experimentSettings.RateChangeVariationPercent = parse_double(expRateChangeVariationBox, experimentSettings.RateChangeVariationPercent);
            experimentSettings.RequestThresholdPercent = parse_double(expRequestThresholdBox, experimentSettings.RequestThresholdPercent);
            experimentSettings.TreqSeconds = parse_double(expTreqBox, experimentSettings.TreqSeconds);
            experimentSettings.WcvSpeedMetersPerSecond = parse_double(expWcvSpeedBox, experimentSettings.WcvSpeedMetersPerSecond);
            experimentSettings.WcvChargeRateJPerSecond = parse_double(expWcvChargeRateBox, experimentSettings.WcvChargeRateJPerSecond);
            experimentSettings.WcvCapacityJ = parse_double(expWcvCapacityBox, experimentSettings.WcvCapacityJ);
            experimentSettings.WcvMoveCostJPerMeter = parse_double(expWcvMoveCostBox, experimentSettings.WcvMoveCostJPerMeter);
            experimentSettings.NmaxTask = parse_int(expNmaxTaskBox, experimentSettings.NmaxTask);
            experimentSettings.OutputDirectory = expOutputDirectoryBox.Text.Trim();
            experimentSettings.ThresholdMode = expThresholdModeBox.SelectedIndex == 1 ? "TreqSeconds" : "Percent";

            List<string> algorithms = new List<string>();
            for (int i = 0; i < expAlgorithmList.CheckedItems.Count; i++)
                algorithms.Add(get_algorithm_key_from_display(Convert.ToString(expAlgorithmList.CheckedItems[i])));
            experimentSettings.SetSelectedAlgorithms(algorithms);
            experimentSettings.Normalize();
            populate_experiment_controls_from_settings();
        }

        int parse_int(TextBox box, int fallback)
        {
            int value;
            if (Int32.TryParse(box.Text.Trim(), System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out value))
                return value;
            return fallback;
        }

        double parse_double(TextBox box, double fallback)
        {
            double value;
            if (Double.TryParse(box.Text.Trim(), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value))
                return value;
            if (Double.TryParse(box.Text.Trim(), out value))
                return value;
            return fallback;
        }

        string get_algorithm_display_name(string key)
        {
            if (key == "EDF") return "EDF（最早期限優先）";
            if (key == "NJF") return "NJF（最近工作優先）";
            if (key == "TADP_LIN") return "TADP/LIN（期限距離線性排序）";
            if (key == "RCSS") return "RCSS（風險與耗能排序）";
            if (key == "NJF_BPR") return "NJF_BPR（BP&R-inspired bottleneck proactive）";
            if (key == "NJF_BPR_ROUTE_SAFE_LIMITED") return "NJF_BPR_ROUTE_SAFE_LIMITED（公平版，<=NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_EXTENDED") return "NJF_BPR_ROUTE_SAFE_EXTENDED（延伸版，可超過NmaxTask）";
            if (key == "FUZZY") return "FUZZY（模糊推論排程）";
            if (key == "GENE") return "GENE（簡化 wrapper baseline，非完整舊版 GA）";
            if (key == "PSO") return "PSO（簡化 wrapper baseline，非完整舊版 PSO）";
            if (key == "Cuckoo") return "Cuckoo（簡化 wrapper baseline，非完整舊版 Cuckoo）";
            return key;
        }

        string get_algorithm_key_from_display(string display)
        {
            if (String.IsNullOrWhiteSpace(display))
                return "";
            int index = display.IndexOf('（');
            if (index > 0)
                return ExperimentSettings.CanonicalAlgorithmKey(display.Substring(0, index).Replace("TADP/LIN", "TADP_LIN").Replace("NJF+BP&R", "NJF_BPR").Trim());
            return ExperimentSettings.CanonicalAlgorithmKey(display.Trim());
        }

        void set_default_algorithm_checks()
        {
            string[] defaults = ExperimentSettings.DefaultAlgorithmSelectionCsv().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < expAlgorithmList.Items.Count; i++)
            {
                string key = get_algorithm_key_from_display(Convert.ToString(expAlgorithmList.Items[i]));
                expAlgorithmList.SetItemChecked(i, defaults.Contains(key));
            }
        }

        void update_last_output_label()
        {
            if (String.IsNullOrWhiteSpace(experimentSettings.LastOutputWorkbookPath))
                expLastOutputLabel.Text = "最後輸出：尚未執行";
            else
                expLastOutputLabel.Text = "最後輸出：" + experimentSettings.LastOutputWorkbookPath;
        }

        void log_experiment_message(string message)
        {
            if (expLogBox == null)
                return;
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            if (String.IsNullOrWhiteSpace(expLogBox.Text))
                expLogBox.Text = line;
            else
                expLogBox.AppendText(Environment.NewLine + line);
        }

        void browseOutputButton_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "選擇 Excel 輸出資料夾";
                dialog.SelectedPath = expOutputDirectoryBox.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                    expOutputDirectoryBox.Text = dialog.SelectedPath;
            }
        }

        void openOutputButton_Click(object sender, EventArgs e)
        {
            apply_experiment_controls_to_settings();
            Directory.CreateDirectory(experimentSettings.OutputDirectory);
            System.Diagnostics.Process.Start("explorer.exe", experimentSettings.OutputDirectory);
        }

        void experimentSaveButton_Click(object sender, EventArgs e)
        {
            apply_experiment_controls_to_settings();
            experimentSettings.SaveLast();
            update_last_output_label();
            log_experiment_message("設定已儲存。");
            MessageBox.Show(this, "新實驗設定已儲存。", "WSN 實驗系統", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        void experimentRunButton_Click(object sender, EventArgs e)
        {
            apply_experiment_controls_to_settings();
            experimentSettings.Normalize();
            experimentSettings.SaveLast();
            experimentRunButton.Enabled = false;
            experimentSaveButton.Enabled = false;
            expProgressLabel.Text = "進度：準備開始";
            log_experiment_message("批次比較執行中...");

            ExperimentSettings settingsToRun = experimentSettings;
            Thread worker = new Thread(delegate()
            {
                try
                {
                    ExperimentBatchRunner runner = new ExperimentBatchRunner(delegate(string message)
                    {
                        begin_update_experiment_status(message);
                    });
                    ExperimentBatchResult result = runner.Run(settingsToRun);
                    begin_experiment_completed(result.WorkbookPath, null);
                }
                catch (Exception ex)
                {
                    begin_experiment_completed("", ex);
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        void begin_update_experiment_status(string message)
        {
            if (IsDisposed)
                return;
            try
            {
                BeginInvoke(new Action(delegate()
                {
                    if (expProgressLabel != null && message.StartsWith("進度 ", StringComparison.Ordinal))
                        expProgressLabel.Text = message;
                    log_experiment_message(message);
                }));
            }
            catch
            {
            }
        }

        void begin_experiment_completed(string workbookPath, Exception error)
        {
            if (IsDisposed)
                return;
            try
            {
                BeginInvoke(new Action(delegate()
                {
                    experimentRunButton.Enabled = true;
                    experimentSaveButton.Enabled = true;
                    if (error != null)
                    {
                        if (expProgressLabel != null)
                            expProgressLabel.Text = "進度：執行失敗";
                        log_experiment_message("執行失敗：" + error.Message);
                        MessageBox.Show(this, error.Message, "WSN 實驗失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    else
                    {
                        experimentSettings.LastOutputWorkbookPath = workbookPath;
                        update_last_output_label();
                        if (expProgressLabel != null)
                            expProgressLabel.Text = "進度 100.0%：批次比較完成";
                        log_experiment_message("批次比較完成：" + workbookPath);
                        MessageBox.Show(this, "批次比較完成。\n" + workbookPath, "WSN 實驗系統", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }));
            }
            catch
            {
            }
        }

        void panel2_Paint(object sender, PaintEventArgs e)
        {
            if (latestNodeFrame != null)
                e.Graphics.DrawImageUnscaled(latestNodeFrame, 0, 0);
            else
                e.Graphics.Clear(Color.White);
        }

        void update_node_frame(Bitmap nextFrame)
        {
            Bitmap previousFrame = latestNodeFrame;
            latestNodeFrame = nextFrame;
            if (previousFrame != null)
                previousFrame.Dispose();

            panel2.Invalidate();
            panel2.Update();
        }

        void draw_point(Graphics g, int x, int y, Color c, int size = 1)
        {
            using (Pen p = new Pen(c))
            using (SolidBrush pointBrush = new SolidBrush(c))
            {
                p.Width = 1;
                if (size > 1)
                    g.FillEllipse(pointBrush, x - size, y - size, 2 * size + 1, 2 * size + 1);
                else
                    g.DrawEllipse(p, x - size, y - size, 2 * size + 1, 2 * size + 1);
            }
        }

        void build_neighbor(int id, bool init_used)
        {
            int i;
            double dist;
            neighbor_info temp;
            for (i = 0; i < maxnode; i++)
            {
                if (i == id) continue;
                dist = common.mydist(common.nmap.node[i].x, common.nmap.node[i].y, common.nmap.node[id].x, common.nmap.node[id].y);
                if (dist <= common.nmap.node[id].prange)
                {
                    temp.id = i;
                    temp.dist = dist;
                    //temp.dist = 1;  // using hop for counting cost
                    temp.used = init_used;
                    common.nmap.node[id].n_list.Add(temp);
                }
            }
        }
        void Init_GR(int stime)
        {
            int i;
            packet pkt;
            for (i = 0; i < maxbase; i++)
            {
                pkt = new packet();
                pkt.source_id = i;
                pkt.dest_id = -1;
                pkt.pre_id = i;
                pkt.fid = i;
                pkt.hop = common.nmap.node[i].hop;
                common.nmap.node[i].setEvent(common.SEND_UPDATE_GR, pkt, stime, i);
            }
        }

        void relocate(bool circlearea, bool dcontrol)
        {
            int i, j,k, x, y, res, ni;
            double dist, min_dis;
            bool dok;
            int [,] cnt = new int[maxbase, maxlayer]; 
            char[] str = new char[10];
 
            for (y = 0; y < maxlayer; y++)
            {
                res = nodes_num[y] % maxbase;
                for (x = 0; x < maxbase; x++)
                {
                    cnt[x, y] = nodes_num[y] / maxbase;
                    if (res > 0) { cnt[x, y]++; res--; }
                }
            }
            for (i = 0; i < maxbase; i++)
            {
                common.nmap.node[i].id = i;
                common.nmap.node[i].clear_info();
                common.nmap.node[i].hop = 0;
                common.nmap.node[i].base_station = true;
            }
            //    randomize();
            for (i = maxbase; i < maxnode; i++)
            {
                common.nmap.node[i].id = i;
                common.nmap.node[i].clear_info();
                do
                {
                    if (circlearea)
                    {
                        do
                        {
                            x = common.rand.Next(Aheight);
                            y = common.rand.Next(Awidth);
                        } while (common.mydist(x, y, Aheight / 2.0, Awidth / 2.0) > (Aheight / 2.0));
                    }
                    else
                    {
                        x = common.rand.Next(Aheight);
                        y = common.rand.Next(Awidth);
                    }
                    if (dcontrol)
                    {
                        ni = -1;
                        dist = 0;
                        min_dis = common.MYINFINITE;
                        for (j = 0; j < maxbase; j++)
                        {
                            dist = common.mydist(common.nmap.node[j].x, common.nmap.node[j].y, x, y);
                            if (dist < min_dis) { ni = j; min_dis = dist; }
                        }
                        k = (int)(min_dis / Powerrange);
                        if (cnt[ni, k] > 0)
                        {
                            cnt[ni, k]--;
                            dok = true;
                        }
                        else
                            dok = false;
                    }
                    else
                        dok = true;
                } while (dcontrol && !dok);

                common.nmap.node[i].x = x;
                common.nmap.node[i].y = y;
                common.nmap.node[i].base_station = false;
                common.nmap.node[i].hop = common.MYINFINITE;
            }

        }

        string ensure_trailing_directory_separator(string directoryPath)
        {
            if (String.IsNullOrWhiteSpace(directoryPath))
                return directoryPath;

            char lastChar = directoryPath[directoryPath.Length - 1];
            if (lastChar == Path.DirectorySeparatorChar || lastChar == Path.AltDirectorySeparatorChar)
                return directoryPath;

            return directoryPath + Path.DirectorySeparatorChar;
        }

        void ensure_directory_exists(string directoryPath)
        {
            if (String.IsNullOrWhiteSpace(directoryPath))
                return;

            Directory.CreateDirectory(directoryPath);
        }

        void ensure_parent_directory_exists(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!String.IsNullOrWhiteSpace(directoryPath))
                ensure_directory_exists(directoryPath);
        }

        string normalize_directory_path(string directoryPath)
        {
            string normalized = String.IsNullOrWhiteSpace(directoryPath) ? DEFAULT_STORAGE_DIRECTORY : directoryPath.Trim();
            if (!Path.IsPathRooted(normalized))
                normalized = Path.Combine(DEFAULT_STORAGE_DIRECTORY, normalized);

            ensure_directory_exists(normalized);
            return ensure_trailing_directory_separator(normalized);
        }

        string get_default_input_path(string extension)
        {
            ensure_directory_exists(DEFAULT_STORAGE_DIRECTORY);
            return Path.Combine(DEFAULT_STORAGE_DIRECTORY, "1" + extension);
        }

        string get_default_output_path()
        {
            return DEFAULT_OUTPUT_PATH;
        }

        string get_initial_directory(string currentPath)
        {
            string candidate = currentPath;

            if (String.IsNullOrWhiteSpace(candidate))
                candidate = DEFAULT_STORAGE_DIRECTORY;
            else if (File.Exists(candidate))
                candidate = Path.GetDirectoryName(candidate);
            else if (!Directory.Exists(candidate))
                candidate = Path.GetDirectoryName(candidate);

            if (String.IsNullOrWhiteSpace(candidate))
                candidate = DEFAULT_STORAGE_DIRECTORY;

            ensure_directory_exists(candidate);
            return candidate;
        }

        void apply_default_storage_paths()
        {
            DirI.Text = normalize_directory_path(DirI.Text);
            textBox1.Text = get_default_input_path(".map");
            textBox2.Text = get_default_input_path(".ev");
            residualFile.Text = get_default_input_path(".re");
            outfilename.Text = get_default_output_path();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            folderBrowserDialog1.SelectedPath = get_initial_directory(DirI.Text);
            if (folderBrowserDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                DirI.Text = normalize_directory_path(folderBrowserDialog1.SelectedPath);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            string outputDir = normalize_directory_path(DirI.Text);
            DirI.Text = outputDir;

            for (int fi = 1; fi <= Int32.Parse(FileNI.Text); fi++)
            {
                write_map_file(Path.Combine(outputDir, fi + ".map"));
            }
            MessageBox.Show("Done!");
        }

        void regenerate_current_map_file()
        {
            string mapPath = textBox1.Text;

            if (String.IsNullOrWhiteSpace(mapPath))
                mapPath = get_default_input_path(".map");
            else if (!Path.IsPathRooted(mapPath))
                mapPath = Path.Combine(normalize_directory_path(DirI.Text), mapPath.Trim());

            write_map_file(mapPath);
            textBox1.Text = mapPath;
        }

        void write_map_file(string mapPath)
        {
            Aheight = Int32.Parse(areaH.Text);
            Awidth = Int32.Parse(areaW.Text);
            maxbase = Int32.Parse(maxBaseI.Text);
            maxnode = Int32.Parse(maxNodeI.Text);
            Powerrange = Int32.Parse(PowerRI.Text);

            ensure_parent_directory_exists(mapPath);

            using (StreamWriter ofs = new StreamWriter(mapPath, false, new UTF8Encoding(true)))
            {
                ofs.WriteLine(String.Format("Height: {0} Width: {1}", Aheight, Awidth));
                ofs.WriteLine(String.Format("Base: {0} Node: {1}", maxbase, maxnode));
                for (int i = 0; i < maxbase; i++)
                {
                    ofs.WriteLine(String.Format("{0} {1}", Base_cor.Lines[2 * i].ToString(), Base_cor.Lines[2 * i + 1].ToString()));
                    common.nmap.node[i].x = Int32.Parse(Base_cor.Lines[2 * i].ToString());
                    common.nmap.node[i].y = Int32.Parse(Base_cor.Lines[2 * i + 1].ToString());
                }

                if (density_control.Checked)
                {
                    for (int i = 0; i < density_in.Lines.Count(); i++)
                    {
                        nodes_num[i] = int.Parse(density_in.Lines[i]);
                    }
                    maxlayer = density_in.Lines.Count();
                }
                else
                {
                    maxlayer = 1;
                    nodes_num[0] = maxnode - maxbase;
                }

                relocate(circleArea.Checked, density_control.Checked);
                for (int i = maxbase; i < maxnode; i++)
                {
                    ofs.WriteLine("{0} {1}", common.nmap.node[i].x, common.nmap.node[i].y);
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int i, j, fi, send_time, end_time;
            int cx = 0, cy = 0, nx = 0, ny = 0;
            int last_ev, gcnt;
            double ev_in_sec, acnt;
            string outputDir = normalize_directory_path(DirI.Text);
            //  packet pkt;
            int num_event;
            //           panel1.Hide();
            StreamWriter ofs;
            Aheight = Int32.Parse(areaH.Text);
            Awidth = Int32.Parse(areaW.Text);

            Powerrange = Int32.Parse(PowerRI.Text);
            num_event = Int32.Parse(ComPairI.Text);
            total_time = Int32.Parse(SimTI.Text);
            start_time = 20; // 前面20秒留給路由樹的建立
            end_time = Int32.Parse(SimTI.Text) - 5;
            DirI.Text = outputDir;
            for (fi = 1; fi <= Int32.Parse(FileNI.Text); fi++)
            {
                ofs = new StreamWriter(Path.Combine(outputDir, fi + ".ev"));
                ofs.WriteLine(String.Format("Height: {0} Width: {1}", Aheight, Awidth));
                ofs.WriteLine(String.Format("PowerRange: {0}", Powerrange));
                ofs.WriteLine(String.Format("NumEvents: {0}", num_event));
                ofs.WriteLine(String.Format("ExecT: {0}", total_time));

                ev_in_sec = (double)num_event / (end_time - start_time);
                //last_ev = num_event - (int)(ev_in_sec * (end_time - start_time));
                j = 1;
                foreach (RadioButton item in datatype.Controls)
                    if (item.Checked)
                    {
                        j = Int32.Parse(item.Tag.ToString());
                        break;
                    }
                // event generation

                switch (j)
                {
                    case 1: //global random
                        acnt = 0;
                        last_ev = 0;
                        for (send_time = start_time; send_time <= end_time; send_time++)
                        {
                            acnt += ev_in_sec;
                            if (send_time == end_time)
                                gcnt = num_event - last_ev;
                            else
                            {
                                gcnt = (int)(Math.Truncate(acnt));
                                last_ev += gcnt;
                                acnt -= gcnt;
                            }
                            for (j = 0; j < gcnt; j++)
                            {
                                generate_xy(circleArea.Checked, ref nx, ref ny, Aheight, Awidth);
                                ofs.WriteLine(String.Format("{0} {1} {2}", nx, ny, send_time));
                            }
                        }

                        break;
                    case 2: // regional random
                        
                        // select region center coordination
                        generate_xy(circleArea.Checked, ref cx, ref cy, Aheight, Awidth);
                        acnt = 0;
                        last_ev = 0;
                        for (send_time = start_time; send_time <= end_time; send_time++)
                        {
                            acnt += ev_in_sec;
                            if (send_time == end_time)
                                gcnt = num_event - last_ev;
                            else
                            {
                                gcnt = (int)(Math.Truncate(acnt));
                                last_ev += gcnt;
                                acnt -= gcnt;
                            }
                            for (j = 0; j < ev_in_sec; j++)
                            {
                                nx = common.rand.Next(2 * Powerrange) - Powerrange + cx;
                                ny = common.rand.Next(2 * Powerrange) - Powerrange + cy;
                                ofs.WriteLine("{0} {1} {2}", nx, ny, send_time);
                            }
                        }
                        break;
                    case 3: // R+G
                        send_time = start_time - 1;
                        for (i = 0; i < num_event; )
                        {
                            // select region center node
                            generate_xy(circleArea.Checked, ref cx, ref cy, Aheight, Awidth);

                            for (j = 0; j < 10; j++, i++)
                            {
                                if (i >= num_event) break;
                                if (i % ev_in_sec == 0) send_time++;
                                nx = common.rand.Next(2 * Powerrange) - Powerrange + cx;
                                ny = common.rand.Next(2 * Powerrange) - Powerrange + cy;
                                ofs.WriteLine("{0} {1} {2}", nx, ny, send_time);
                            }
                        }
                        break;
                }

                ofs.Close();
            }
            MessageBox.Show("Done!");
        }

        void generate_xy(bool circle, ref int x, ref int y, int h, int w)
        {
            do
            {
                x = common.rand.Next(h);
                y = common.rand.Next(w);
            } while (circle && (common.mydist(x, y, h / 2.0, w / 2.0) > h / 2.0));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (Int32.Parse(((Button)sender).Tag.ToString()) == 0)
            { panel1.Show(); ((Button)sender).Text = "關閉資料產生器"; ((Button)sender).Tag = "1"; }
            else
            { panel1.Hide(); ((Button)sender).Text = "開啟資料產生器"; ((Button)sender).Tag = "0"; }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "地圖檔(*.map)|*.map|All files(*.*)|*.*";
            openFileDialog1.InitialDirectory = get_initial_directory(textBox1.Text);
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox1.Text = openFileDialog1.FileName;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "事件檔(*.ev)|*.ev|All files(*.*)|*.*";
            openFileDialog1.InitialDirectory = get_initial_directory(textBox2.Text);
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                textBox2.Text = openFileDialog1.FileName;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            assign_prange = true;

            original_in.Text = (common.Origin_RESIDUAL.ToString("G"));
            if (!File.Exists(textBox1.Text))
                MessageBox.Show("地圖輸入檔不存在！");
            else
                load_map();

            //
            common.nmap.init();
            if (!File.Exists(textBox2.Text))
                MessageBox.Show("事件輸入檔不存在！");
            else
                load_event_head();
            //            c_run_time = 0;

            if (!File.Exists(residualFile.Text))
                MessageBox.Show("餘電設定檔不存在！");
            else
                load_residual();

            if (!autoload)
            {
                MessageBox.Show("Done!");
                draw_nodes();
            }

        }

        void draw_point(int x, int y, Color c, int size = 1)
        {
            using (Graphics g = panel2.CreateGraphics())
            {
                draw_point(g, x, y, c, size);
            }
        }

        void draw_power_range(int x, int y, int r, Color c)
        {
            Graphics g = panel2.CreateGraphics();
            Pen p = new Pen(c);
            p.Width = 1;
            g.DrawEllipse(p, x - r, y - r, 2 * r, 2 * r);
        }

        Color get_sensor_point_color(int nodeId)
        {
            if (is_node_visually_charging(nodeId))
                return Color.Blue;

            double requestReferenceResidual = common.nmap.node[nodeId].has_request_reference
                ? common.nmap.node[nodeId].request_reference_residual
                : common.Origin_RESIDUAL * common.request_threshold;
            double residualBand = Math.Max(0.0, common.Origin_RESIDUAL * REQUEST_STATUS_BAND_RATIO);
            if (common.nmap.node[nodeId].residual > requestReferenceResidual + residualBand)
                return Color.Green;
            if (common.nmap.node[nodeId].residual < requestReferenceResidual - residualBand)
                return Color.Red;
            return Color.Orange;
        }

        bool is_node_visually_charging(int nid)
        {
            if (is_node_actively_charging(nid))
                return true;

            return common.nmap.node[nid].charging_visual_until_time >= common.current_time;
        }

        bool has_sensor_status_highlight()
        {
            return maxnode > maxbase;
        }

        Bitmap create_node_frame()
        {
            int frameWidth = Math.Max(1, panel2.ClientSize.Width);
            int frameHeight = Math.Max(1, panel2.ClientSize.Height);
            Bitmap frame = new Bitmap(frameWidth, frameHeight);

            using (Graphics g = Graphics.FromImage(frame))
            using (Pen basePen = new Pen(Color.Red))
            {
                g.Clear(Color.White);
                basePen.Width = 2;

                for (int i = 0; i < maxnode; i++)
                {
                    if (common.nmap.node[i].base_station)
                    {
                        g.DrawRectangle(basePen, common.nmap.node[i].x - 2, common.nmap.node[i].y - 2, 5, 5);
                    }
                    else
                    {
                        Color pointColor = get_sensor_point_color(i);
                        bool showChargingHighlight = is_node_visually_charging(i);
                        int pointSize = showChargingHighlight ? 4 : 3;
                        if (showChargingHighlight)
                        {
                            using (SolidBrush glowBrush = new SolidBrush(Color.FromArgb(96, Color.LightSkyBlue)))
                            using (Pen ringPen = new Pen(Color.DeepSkyBlue, 2))
                            {
                                g.FillEllipse(glowBrush,
                                    common.nmap.node[i].x - CHARGING_HIGHLIGHT_RADIUS,
                                    common.nmap.node[i].y - CHARGING_HIGHLIGHT_RADIUS,
                                    CHARGING_HIGHLIGHT_RADIUS * 2 + 1,
                                    CHARGING_HIGHLIGHT_RADIUS * 2 + 1);
                                g.DrawEllipse(ringPen,
                                    common.nmap.node[i].x - CHARGING_HIGHLIGHT_RADIUS,
                                    common.nmap.node[i].y - CHARGING_HIGHLIGHT_RADIUS,
                                    CHARGING_HIGHLIGHT_RADIUS * 2 + 1,
                                    CHARGING_HIGHLIGHT_RADIUS * 2 + 1);
                            }
                        }
                        draw_point(g, common.nmap.node[i].x, common.nmap.node[i].y, pointColor, pointSize);
                    }
                }
            }

            return frame;
        }

        void draw_nodes()
        {
            update_node_frame(create_node_frame());
        }

        void link_node(int pid, int sid, Color line_color)
        { // 繪製兩點連線並清除，以產生閃動效果
            Graphics g = panel2.CreateGraphics();
            Pen p = new Pen(line_color);
            //            if (silent) return;
            p.Width = 1;
            g.DrawLine(p, common.nmap.node[pid].x, common.nmap.node[pid].y, common.nmap.node[sid].x, common.nmap.node[sid].y);
        }

        void load_map()
        {
            //data load
            node_angle_entry temp_item;
            int i;
            string str;
            int tx, ty;
            string[] words;
            char[] deli = { ' ' };

            StreamReader ifs;
            ifs = new StreamReader(textBox1.Text);

            //format: Base: #base Node: #node ExecT: #time
            str = ifs.ReadLine();
            words = str.Split(' ');
            Aheight = Int32.Parse(words[1]);
            Awidth = Int32.Parse(words[3]);
            str = ifs.ReadLine();
            words = str.Split(deli, StringSplitOptions.RemoveEmptyEntries);
            maxbase = Int32.Parse(words[1]);
            maxnode = Int32.Parse(words[3]);
            common.nmap.node_angle_list = new List<node_angle_entry>();
            if (Int32.Parse(nodenum.Text.ToString()) > maxnode)
            {
                MessageBox.Show("指定使用節點數大於地圖檔中可用節點數!");
            }
            else
                maxnode = Int32.Parse(nodenum.Text.ToString());
            common.nmap.max_node = maxnode;
            common.nmap.max_base = maxbase;
            // read initial position
            for (i = 0; i < maxbase; i++)
            {
                common.nmap.node[i].clear_info();
                str = ifs.ReadLine();
                words = str.Split(' ');
                common.nmap.node[i].x = Int32.Parse(words[0]);
                common.nmap.node[i].y = Int32.Parse(words[1]);
                common.nmap.node[i].base_station = true;
                common.nmap.node[i].hop = 0;
            }
            for (i = maxbase; i < maxnode; i++)
            {
                common.nmap.node[i].clear_info();
                str = ifs.ReadLine();
                words = str.Split(' ');
                tx = Int32.Parse(words[0]);
                ty = Int32.Parse(words[1]);
                common.nmap.node[i].x = tx;
                common.nmap.node[i].y = ty;
                //以下角度只適用於單一基地台
                common.nmap.node[i].angle = Math.Atan2(ty - common.nmap.node[0].y, tx - common.nmap.node[0].x);
                temp_item = new node_angle_entry();
                temp_item.node_id = i;
                temp_item.angle = common.nmap.node[i].angle;
                common.nmap.node_angle_list.Add(temp_item);
                common.nmap.node[i].base_station = false;
            }
            //節點依對應基地台的角度排序
            common.nmap.node_angle_list.Sort();
            ifs.Close();
        }
        void load_residual()
        {
            //data load

            int i, nid;
            string str;
            string[] words;
            char[] deli = { ' ' };

            StreamReader ifs;
            ifs = new StreamReader(residualFile.Text);

            //format: Base: #base Node: #node ExecT: #time
            str = ifs.ReadLine();
            words = str.Split(' ');
            Aheight = Int32.Parse(words[1]);
            Awidth = Int32.Parse(words[3]);
            str = ifs.ReadLine();
            words = str.Split(deli, StringSplitOptions.RemoveEmptyEntries);
            maxbase = Int32.Parse(words[1]);
            maxnode = Int32.Parse(words[3]);

            if (Int32.Parse(nodenum.Text.ToString()) > maxnode)
            {
                MessageBox.Show("指定使用節點數大於餘電檔中可用節點數!");
            }
            else
                maxnode = Int32.Parse(nodenum.Text.ToString());
            common.nmap.max_node = maxnode;
            common.nmap.max_base = maxbase;
            // read initial residual ratio

            for (i = maxbase; i < maxnode; i++)
            {
                common.nmap.node[i].clear_info();
                str = ifs.ReadLine();
                words = str.Split(' ');
                nid = Int32.Parse(words[0]);
                common.nmap.node[nid].residual = Double.Parse(words[1]);
            }

            ifs.Close();

        }
        void load_event_head()
        {
            //event load

            packet pkt = new packet();
            int mapheight, mapwidth;
            string str;
            string[] words;

            common.ifs = new StreamReader(textBox2.Text);
            //format: Base: #base Node: #node ExecT: #time

            str = common.ifs.ReadLine();
            words = str.Split(' ');
            mapheight = Int32.Parse(words[1]);
            mapwidth = Int32.Parse(words[3]);

            if (mapheight != Aheight || mapwidth != Awidth)
            {
                MessageBox.Show("地圖檔與事件檔區域大小不同!");
            }
            str = common.ifs.ReadLine();
            words = str.Split(' ');
            Powerrange = Int32.Parse(words[1]);
            if (!autoload) prange.Text = words[1];

            str = common.ifs.ReadLine();
            words = str.Split(' ');
            total_time = Int32.Parse(words[1]);
            if (!autoload) stop_time.Text = words[1];
            
        }

        bool event_stream_ready()
        {
            if (common.ifs == null) return false;
            try
            {
                common.ifs.Peek();
                return true;
            }
            catch (ObjectDisposedException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }
        }

        void load_event_sec(double readsec)
        {
            int nid, send_time;
            packet pkt = new packet();
            string str;
            string[] words;

            str = common.ifs.ReadLine();

            while (str != null)
            {
                words = str.Split(' ');
                nid = Int32.Parse(words[0]);
                send_time = Int32.Parse(words[1]);

                if (nid >= 0)
                {
                    pkt.source_id = nid;
                    pkt.dest_id = 0;
                    pkt.ev_x = -1;
                    pkt.ev_y = -1;
                    pkt.ev_id = -1;
                    pkt.leng = common.default_pkt_leng;
                    pkt.remain_work_time = -1;
                    common.nmap.node[nid].setEvent(common.SEND_DATA, pkt, (int)(send_time / common.TIME_UNIT), nid);
                }
                last_event_time = send_time;
                if (send_time > readsec) break;
                str = common.ifs.ReadLine();
            }            
        }
        void load_event2()
        {
            //event load
            int nid, i, send_time;
            packet pkt = new packet();
            int mapheight, mapwidth;
            string str;
            string[] words;

            StreamReader ifs;
            ifs = new StreamReader(textBox2.Text);

            str = ifs.ReadLine();
            words = str.Split(' ');
            mapheight = Int32.Parse(words[1]);
            mapwidth = Int32.Parse(words[3]);

            if (mapheight != Aheight || mapwidth != Awidth)
            {
                MessageBox.Show("地圖檔與事件檔區域大小不同!");
            }
            str = ifs.ReadLine();
            words = str.Split(' ');
            Powerrange = Int32.Parse(words[1]);
            if (!autoload) prange.Text = words[1];

            str = ifs.ReadLine();
            words = str.Split(' ');
            total_time = Int32.Parse(words[1]);
            stop_time.Text = words[1];
            str = ifs.ReadLine();
            i = 0;
            while (str != null)
            {                
                words = str.Split(' ');
                nid = Int32.Parse(words[0]);
                send_time = Int32.Parse(words[1]);

                if (nid >= 0)
                {
                    pkt.source_id = nid;
                    pkt.dest_id = 0;
                    pkt.ev_x = -1;
                    pkt.ev_y = -1;
                    pkt.ev_id = i + 1;
                    pkt.leng = common.default_pkt_leng;
                    pkt.remain_work_time = -1;
                    common.nmap.node[nid].setEvent(common.SEND_DATA, pkt, (int)(send_time / common.TIME_UNIT), nid);
                }
                i++;
                str = ifs.ReadLine();
            }
        }
        void load_event()
        {
            //event load

            int mx, my;
            int nid, i, j, send_time;
            packet pkt = new packet();
            double min_dist, tempdist;
            int mapheight, mapwidth;
            string str;
            string[] words;

            StreamReader ifs;
            ifs = new StreamReader(textBox2.Text);
            //format: Base: #base Node: #node ExecT: #time

            str = ifs.ReadLine();
            words = str.Split(' ');
            mapheight = Int32.Parse(words[1]);
            mapwidth = Int32.Parse(words[3]);

            if (mapheight != Aheight || mapwidth != Awidth)
            {
                MessageBox.Show("地圖檔與事件檔區域大小不同!");
            }
            str = ifs.ReadLine();
            words = str.Split(' ');
            Powerrange = Int32.Parse(words[1]);
            if (!autoload) prange.Text = words[1];
            str = ifs.ReadLine();
            words = str.Split(' ');
            num_event = Int32.Parse(words[1]);
       //     rectime_in.Text = words[1];
            str = ifs.ReadLine();
            words = str.Split(' ');
            total_time = Int32.Parse(words[1]);
            stop_time.Text = words[1];

            for (i = 0; i < num_event; i++)
            {
                str = ifs.ReadLine();
                words = str.Split(' ');
                mx = Int32.Parse(words[0]);
                my = Int32.Parse(words[1]);
                send_time = Int32.Parse(words[2]);

                min_dist = 1000000;
                nid = -1;
                for (j = maxbase; j < maxnode; j++)
                {
                    tempdist = common.mydist(common.nmap.node[j].x, common.nmap.node[j].y, mx, my);
                    if (tempdist <= Powerrange && tempdist <= min_dist)
                    {
                        min_dist = tempdist;
                        nid = j;
                    }
                }
                if (nid >= 0)
                {
                    pkt.source_id = nid;
                    pkt.dest_id = 0;
                    pkt.ev_x = mx;
                    pkt.ev_y = my;
                    pkt.ev_id = i + 1;
                    pkt.leng = common.default_pkt_leng;
                    pkt.remain_work_time = -1;
                    common.nmap.node[nid].setEvent(common.SEND_DATA, pkt, (int)(send_time / common.TIME_UNIT), nid);
                }
                else
                { // lost event
                    pkt.source_id = nid;
                    pkt.dest_id = 0;
                    pkt.ev_x = mx;
                    pkt.ev_y = my;
                    pkt.ev_id = i + 1;
                    pkt.leng = common.default_pkt_leng;
                    common.nmap.node[0].setEvent(common.LOST_EVENT, pkt, (int)(send_time / common.TIME_UNIT), nid);
                }

            }
        }

        private void button9_Click(object sender, EventArgs e)
        {
            draw_nodes();
        }

        int find_best_fid(int nid, Boolean use_succ)
        {
            int i, best_fid, tid;
            double best_residual, best_succ;
            double t_succ;
            double best_weight, prange, tweight;
            use_succ = false;
            if (common.nmap.node[nid].f_ind > 0)
            {
                prange = common.nmap.node[nid].prange;
                best_fid = common.nmap.node[nid].fid[0];

                best_succ = common.nmap.node[nid].succ[0];

                best_residual = common.nmap.node[best_fid].residual;
                if (use_succ)
                {
                    if (best_residual < common.nmap.node[best_fid].tx_energy_unit() / best_succ)
                        best_weight = 0;
                    else
                        best_weight = best_residual / common.Origin_RESIDUAL * 0.9 + best_succ * 0.1;
                }
                else
                    best_weight = best_residual;
                for (i = 1; i < common.nmap.node[nid].f_ind; i++)
                {
                    tid = common.nmap.node[nid].fid[i];
                    t_succ = common.nmap.node[nid].succ[i];
                    if (use_succ)
                    {
                        if (common.nmap.node[nid].residual < common.nmap.node[tid].tx_energy_unit() / t_succ)
                            tweight = 0;
                        else
                            tweight = common.nmap.node[tid].residual / common.Origin_RESIDUAL * 0.9 + t_succ * 0.1;
                    }
                    else
                        tweight = common.nmap.node[tid].residual;
          //          if (common.nmap.node[tid].residual > best_residual)
                    if (tweight > best_weight)
                    {
                        best_fid = tid;
                        best_residual = common.nmap.node[tid].residual;
                        best_weight = tweight;
                    }
                }
                return best_fid;
            }
            else
                return -1;
        }

        void checkout_timing(packet pkt, char result)
        {
            int i;
            if (!common.debug_output) return; // disable by default
            recv[pkt.ev_id] = result;
            if (pkt.ev_id % rec_time == 0)
            {
                dead_node = 0;
                for (i = 0; i < maxnode; i++)
                {
                    if (common.nmap.node[i].residual <= common.nmap.node[i].tx_energy(mu)) dead_node++;
                }

                ofs.WriteLine("<可能無法正常工作的節點數>, {0}, {1}", pkt.ev_id, dead_node);

                hop1_residual = 0;
                for (i = 0; i < maxnode; i++)
                {
                    if (common.nmap.node[i].hop == 1)
                        hop1_residual = (int)(common.nmap.node[i].residual + hop1_residual);
                }

            }
        }

        private void button20_Click(object sender, EventArgs e)
        {

        }

        private void button14_Click(object sender, EventArgs e)
        {
            int fi;
            int maxni = Int32.Parse(maxNodeI.Text);
            int generatedMaxBase = 1;
            string outputDir = normalize_directory_path(DirI.Text);

            Aheight = Int32.Parse(areaH.Text);
            Awidth = Int32.Parse(areaW.Text);
            Powerrange = Int32.Parse(PowerRI.Text);
            Int32.Parse(ComPairI.Text);
            total_time = Int32.Parse(SimTI.Text);
            start_time = 1;
            DirI.Text = outputDir;

            for (fi = 1; fi <= Int32.Parse(FileNI.Text); fi++)
            {
                write_sequential_event_file(Path.Combine(outputDir, fi + ".ev"), generatedMaxBase, maxni);
            }
            MessageBox.Show("Done!");

        }

        void regenerate_current_event_file()
        {
            int activeMaxBase = maxbase;
            int activeMaxNode = maxnode;
            string eventPath = textBox2.Text;
            int requestedExecTime;

            Int32.Parse(ComPairI.Text);
            Powerrange = Int32.Parse(PowerRI.Text);
            if (!Int32.TryParse(stop_time.Text, out requestedExecTime) || requestedExecTime <= 0)
                requestedExecTime = Int32.Parse(SimTI.Text);
            total_time = requestedExecTime;
            start_time = 1;

            if (String.IsNullOrWhiteSpace(eventPath))
                eventPath = get_default_input_path(".ev");
            else if (!Path.IsPathRooted(eventPath))
                eventPath = Path.Combine(normalize_directory_path(DirI.Text), eventPath.Trim());

            ensure_parent_directory_exists(eventPath);

            write_sequential_event_file(eventPath, activeMaxBase, activeMaxNode);
            textBox2.Text = eventPath;
        }

        void write_sequential_event_file(string eventPath, int activeMaxBase, int activeMaxNode)
        {
            int ni;
            int send_time;
            int end_time = total_time;

            ensure_parent_directory_exists(eventPath);

            using (StreamWriter ofs = new StreamWriter(eventPath, false, new UTF8Encoding(true)))
            {
                ofs.WriteLine(String.Format("Height: {0} Width: {1}", Aheight, Awidth));
                ofs.WriteLine(String.Format("PowerRange: {0}", Powerrange));
                ofs.WriteLine(String.Format("ExecT: {0}", total_time));

                if (activeMaxNode <= 0)
                    return;

                double[] lambda_mu = new double[activeMaxNode];
                for (ni = activeMaxBase; ni < activeMaxNode; ni++)
                {
                    do
                    {
                        lambda_mu[ni] = common.rand.NextDouble();
                    } while (lambda_mu[ni] > 100.0 / Math.Max(activeMaxNode, 1));
                }

                for (send_time = start_time; send_time <= end_time; send_time++)
                {
                    for (ni = activeMaxBase; ni < activeMaxNode; ni++)
                    {
                        if (common.rand.NextDouble() > lambda_mu[ni]) continue;
                        ofs.WriteLine(String.Format("{0} {1}", ni, send_time));
                    }
                }
            }
        }

        private void load_btn_Click(object sender, EventArgs e)
        {

        }

        void one_hop_broadcast(int nid, int PID, packet pkt, int stime)
        {
            int ni;
            double dist;
            int broad_range;

            if (PID == common.RECV_UPDATE_GR)
                broad_range = common.nmap.node[nid].next_prange;
            else
                broad_range = common.nmap.node[nid].prange;
            for (ni = 0; ni < maxnode; ni++)
            { // 廣播 pkt 給周圍節點
                if (ni == nid) continue;
                dist = common.mydist(common.nmap.node[nid].x, common.nmap.node[nid].y, common.nmap.node[ni].x, common.nmap.node[ni].y);
                if (dist <= broad_range)
                {
                    common.nmap.node[ni].setEvent(PID, pkt, stime, nid);
                }
            }
        }

        bool randomcheck(float prob)
        {
            return common.rand.NextDouble()<= prob;
        }

        string normalize_csv_output_path(string outputPath)
        {
            if (String.IsNullOrWhiteSpace(outputPath))
                outputPath = get_default_output_path();
            else if (!Path.IsPathRooted(outputPath))
                outputPath = Path.Combine(DEFAULT_STORAGE_DIRECTORY, outputPath.Trim());

            string ext = Path.GetExtension(outputPath);
            if (String.IsNullOrEmpty(ext) || ext.Equals(".txt", StringComparison.OrdinalIgnoreCase))
                outputPath = Path.ChangeExtension(outputPath, ".csv");

            string outputDir = Path.GetDirectoryName(outputPath);
            if (String.Equals(outputDir, @"C:\", StringComparison.OrdinalIgnoreCase))
                outputPath = Path.Combine(DEFAULT_STORAGE_DIRECTORY, Path.GetFileName(outputPath));

            ensure_parent_directory_exists(outputPath);
            return outputPath;
        }

        string build_rate_change_log_path(string outputPath)
        {
            string normalizedOutput = normalize_csv_output_path(outputPath);
            string dir = Path.GetDirectoryName(normalizedOutput);
            string name = Path.GetFileNameWithoutExtension(normalizedOutput);

            if (String.IsNullOrWhiteSpace(dir))
                dir = DEFAULT_OUTPUT_DIRECTORY;
            if (String.IsNullOrWhiteSpace(name))
                name = "result";

            ensure_directory_exists(dir);
            return Path.Combine(dir, name + "_rate_change.txt");
        }

        StreamWriter open_text_overwrite_writer(string outputPath)
        {
            ensure_parent_directory_exists(outputPath);
            return new StreamWriter(outputPath, false, new UTF8Encoding(true));
        }

        StreamWriter open_text_append_writer(string outputPath)
        {
            ensure_utf8_bom(outputPath);
            return new StreamWriter(outputPath, true, new UTF8Encoding(false));
        }

        bool should_append_batch_output()
        {
            return currentBatchRunCount > 1 && currentBatchRunIndex > 1;
        }

        bool should_append_repeat_output(string outputPath)
        {
            if (should_append_batch_output())
                return true;

            if (currentBatchRunCount <= 1 || String.IsNullOrWhiteSpace(outputPath) || !File.Exists(outputPath))
                return false;

            FileInfo info = new FileInfo(outputPath);
            return info.Length > 0;
        }

        void write_batch_run_marker(StreamWriter writer)
        {
            if (writer == null || currentBatchRunCount <= 1)
                return;

            writer.WriteLine();
            writer.WriteLine("Run,{0}/{1}", currentBatchRunIndex, currentBatchRunCount);
        }

        bool try_get_repeat_count(out int repeatCount)
        {
            repeatCount = 1;
            string text = repeatCountBox.Text.Trim();
            if (String.IsNullOrWhiteSpace(text))
                text = "1";

            if (!Int32.TryParse(text, out repeatCount) || repeatCount <= 0)
            {
                MessageBox.Show("執行次數請輸入大於 0 的整數。");
                repeatCountBox.Focus();
                return false;
            }

            repeatCountBox.Text = repeatCount.ToString();
            return true;
        }

        void close_event_stream()
        {
            if (common.ifs == null)
                return;

            common.ifs.Close();
            common.ifs = null;
        }

        void close_main_output()
        {
            if (common.ofs == null)
                return;

            common.ofs.Close();
            common.ofs = null;
        }

        void close_all_run_streams()
        {
            close_event_stream();
            close_main_output();
            stop_rate_change_log();
            stop_low_residual_log();
            stop_zero_residual_log();
        }

        void update_run_status_label(string text)
        {
            label16.Text = text;
            label16.Refresh();
            Application.DoEvents();
        }

        string format_run_stage(string stage, int runIndex, int repeatCount)
        {
            if (repeatCount > 1)
                return String.Format("{0} {1}/{2}", stage, runIndex, repeatCount);
            return stage;
        }

        string format_run_stage_progress(string stage, int runIndex, int repeatCount, int current, int total)
        {
            if (repeatCount > 1)
                return String.Format("{0} {1}/{2} ({3}/{4})", stage, runIndex, repeatCount, current, total);
            return String.Format("{0} ({1}/{2})", stage, current, total);
        }

        void reset_run_counters()
        {
            common.packet_id = 0;
            common.saveNum = 0;
            common.current_time = 0;
            common.first_dead_time = -1;
            common.current_node = 0;
            common.redraw = false;
            request_event.max_id = 0;
        }

        bool prepare_single_run_inputs()
        {
            string requestedStopTime = stop_time.Text.Trim();
            if (!File.Exists(residualFile.Text))
            {
                MessageBox.Show("餘電檔不存在，請先確認 residual 檔案路徑。");
                return false;
            }

            try
            {
                close_event_stream();
                regenerate_current_map_file();
                load_map();
                regenerate_current_event_file();
            }
            catch (Exception ex)
            {
                MessageBox.Show("重新產生地圖/事件或載入地圖失敗: " + ex.Message);
                return false;
            }

            common.nmap.init();
            load_residual();
            load_event_head();
            if (!String.IsNullOrWhiteSpace(requestedStopTime))
                stop_time.Text = requestedStopTime;
            last_event_time = 0;
            reset_run_counters();
            return event_stream_ready();
        }

        void start_rate_change_log()
        {
            stop_rate_change_log();
            rateChangeLogPath = build_rate_change_log_path(outfilename.Text);
            rateChangeOfs = should_append_repeat_output(rateChangeLogPath)
                ? open_text_append_writer(rateChangeLogPath)
                : open_text_overwrite_writer(rateChangeLogPath);
            write_batch_run_marker(rateChangeOfs);
            rateChangeOfs.WriteLine("時間(秒), 節點ID, 是否變動, 變動百分比, 新耗電倍率");
        }

        void stop_rate_change_log()
        {
            if (rateChangeOfs == null) return;
            rateChangeOfs.Close();
            rateChangeOfs = null;
        }

        void start_low_residual_log()
        {
            stop_low_residual_log();
            string lowResidualPath = build_low_residual_log_path(outfilename.Text);
            lowResidualOfs = should_append_repeat_output(lowResidualPath)
                ? open_csv_append_writer(lowResidualPath)
                : open_csv_overwrite_writer(lowResidualPath);
            write_batch_run_marker(lowResidualOfs);
            lowResidualOfs.WriteLine("模擬時間(秒),節點ID,剩餘電量(nJ),剩餘電量比例,剩餘電量(%),節點狀態");
        }

        void stop_low_residual_log()
        {
            if (lowResidualOfs == null) return;
            lowResidualOfs.Close();
            lowResidualOfs = null;
        }

        void start_zero_residual_log()
        {
            stop_zero_residual_log();
            string zeroResidualPath = build_zero_residual_log_path(outfilename.Text);
            zeroResidualOfs = should_append_repeat_output(zeroResidualPath)
                ? open_csv_append_writer(zeroResidualPath)
                : open_csv_overwrite_writer(zeroResidualPath);
            write_batch_run_marker(zeroResidualOfs);
            zeroResidualOfs.WriteLine("模擬時間(秒),節點ID,剩餘電量(nJ),剩餘電量比例,剩餘電量(%),節點狀態");
        }

        void stop_zero_residual_log()
        {
            if (zeroResidualOfs == null) return;
            zeroResidualOfs.Close();
            zeroResidualOfs = null;
        }

        void log_low_residual_crossing(int nid)
        {
            if (nid < maxbase || nid >= maxnode || lowResidualBelowState == null || nid >= lowResidualBelowState.Length)
                return;

            double residualRatio = common.Origin_RESIDUAL <= 0 ? 0 : common.nmap.node[nid].residual / common.Origin_RESIDUAL;
            bool isBelow = residualRatio <= LOW_RESIDUAL_RATIO;

            if (isBelow && !lowResidualBelowState[nid])
            {
                double simTimeSec = common.current_time * common.TIME_UNIT;
                double residualPercent = residualRatio * 100.0;

                if (lowResidualOfs != null)
                {
                    lowResidualOfs.WriteLine("{0:F2},{1},{2:E6},{3:F6},{4:F4},{5}",
                        simTimeSec,
                        nid,
                        common.nmap.node[nid].residual,
                        residualRatio,
                        residualPercent,
                        common.nmap.node[nid].status);
                }

                if (autoload)
                    Console.WriteLine("LOW10, {0:F2}, {1}, {2:F4}", simTimeSec, nid, residualPercent);
            }

            lowResidualBelowState[nid] = isBelow;
        }

        void log_zero_residual_crossing(int nid)
        {
            if (nid < maxbase || nid >= maxnode || zeroResidualReachedState == null || nid >= zeroResidualReachedState.Length)
                return;

            double residualRatio = common.Origin_RESIDUAL <= 0 ? 0 : common.nmap.node[nid].residual / common.Origin_RESIDUAL;
            bool isZero = residualRatio <= ZERO_RESIDUAL_RATIO;

            if (isZero && !zeroResidualReachedState[nid])
            {
                double simTimeSec = common.current_time * common.TIME_UNIT;
                double residualPercent = residualRatio * 100.0;

                if (zeroResidualOfs != null)
                {
                    zeroResidualOfs.WriteLine("{0:F2},{1},{2:E6},{3:F6},{4:F4},{5}",
                        simTimeSec,
                        nid,
                        common.nmap.node[nid].residual,
                        residualRatio,
                        residualPercent,
                        common.nmap.node[nid].status);
                }

                if (autoload)
                    Console.WriteLine("ZERO0, {0:F2}, {1}, {2:F4}", simTimeSec, nid, residualPercent);
            }

            zeroResidualReachedState[nid] = isZero;
        }

        void refresh_low_residual_state()
        {
            if (lowResidualBelowState == null) return;

            for (int nid = maxbase; nid < maxnode; nid++)
            {
                double residualRatio = common.Origin_RESIDUAL <= 0 ? 0 : common.nmap.node[nid].residual / common.Origin_RESIDUAL;
                lowResidualBelowState[nid] = residualRatio <= LOW_RESIDUAL_RATIO;
            }
        }

        void refresh_zero_residual_state()
        {
            if (zeroResidualReachedState == null) return;

            for (int nid = maxbase; nid < maxnode; nid++)
            {
                double residualRatio = common.Origin_RESIDUAL <= 0 ? 0 : common.nmap.node[nid].residual / common.Origin_RESIDUAL;
                zeroResidualReachedState[nid] = residualRatio <= ZERO_RESIDUAL_RATIO;
            }
        }

        void update_node_consuming_rates()
        {
            for (int nid = maxbase; nid < maxnode; nid++)
            {
                double oldScale = common.nmap.node[nid].consume_rate_scale;
                double newScale = oldScale;
                bool changed = false;

                if (common.rand.NextDouble() <= common.Prate_change)
                {
                    double delta = (common.rand.NextDouble() * 2.0 - 1.0) * common.RATE_CHANGE_BAND;
                    newScale = Math.Max(0.01, oldScale * (1.0 + delta));
                    common.nmap.node[nid].consume_rate_scale = newScale;
                    changed = true;
                }

                double changePercent = oldScale <= 0 ? 0 : (newScale / oldScale - 1.0) * 100.0;

                if (rateChangeOfs != null)
                {
                    rateChangeOfs.WriteLine("{0:F0}, {1}, {2}, {3:F2}, {4:F4}",
                        common.current_time * common.TIME_UNIT,
                        nid,
                        changed ? 1 : 0,
                        changePercent,
                        common.nmap.node[nid].consume_rate_scale);
                }

                // 速率切換後，從這一刻重新估計後續耗電斜率。
                common.nmap.node[nid].pre_residual = common.nmap.node[nid].residual;
                common.nmap.node[nid].pre_charged_time = common.current_time;
                if (common.nmap.node[nid].status == 0)
                    common.nmap.refresh_bpr_state(nid, true);
            }

            if (rateChangeOfs != null)
                rateChangeOfs.Flush();
        }

        bool is_node_actively_charging(int nid)
        {
            foreach (car chargeCar in common.nmap.car_list)
            {
                if (chargeCar == null || chargeCar.mycharger == null)
                    continue;

                foreach (charger chargerUnit in chargeCar.mycharger)
                {
                    if (chargerUnit != null && chargerUnit.status == 1 && chargerUnit.q_target == nid)
                        return true;
                }
            }

            return false;
        }

        void apply_background_consumption()
        {
            for (int nid = maxbase; nid < maxnode; nid++)
            {
                if (common.nmap.node[nid].residual <= 0)
                    continue;
                if (is_node_actively_charging(nid))
                    continue;

                common.nmap.node[nid].consume_background_power();
            }
        }

        void ensure_utf8_bom(string outputPath)
        {
            ensure_parent_directory_exists(outputPath);
            byte[] bom = Encoding.UTF8.GetPreamble();
            if (!File.Exists(outputPath))
            {
                File.WriteAllBytes(outputPath, bom);
                return;
            }

            byte[] content = File.ReadAllBytes(outputPath);
            bool hasBom = content.Length >= bom.Length
                && content[0] == bom[0]
                && content[1] == bom[1]
                && content[2] == bom[2];
            if (hasBom) return;

            byte[] merged = new byte[bom.Length + content.Length];
            Buffer.BlockCopy(bom, 0, merged, 0, bom.Length);
            Buffer.BlockCopy(content, 0, merged, bom.Length, content.Length);
            File.WriteAllBytes(outputPath, merged);
        }

        StreamWriter open_csv_append_writer(string outputPath)
        {
            ensure_utf8_bom(outputPath);
            return new StreamWriter(outputPath, true, new UTF8Encoding(false));
        }

        StreamWriter open_csv_overwrite_writer(string outputPath)
        {
            ensure_parent_directory_exists(outputPath);
            return new StreamWriter(outputPath, false, new UTF8Encoding(true));
        }

        string build_low_residual_log_path(string outputPath)
        {
            string normalizedOutput = normalize_csv_output_path(outputPath);
            string dir = Path.GetDirectoryName(normalizedOutput);
            string name = Path.GetFileNameWithoutExtension(normalizedOutput);

            if (String.IsNullOrWhiteSpace(dir))
                dir = DEFAULT_OUTPUT_DIRECTORY;
            if (String.IsNullOrWhiteSpace(name))
                name = "result";

            ensure_directory_exists(dir);
            return Path.Combine(dir, name + "_residual10.csv");
        }

        string build_zero_residual_log_path(string outputPath)
        {
            string normalizedOutput = normalize_csv_output_path(outputPath);
            string dir = Path.GetDirectoryName(normalizedOutput);
            string name = Path.GetFileNameWithoutExtension(normalizedOutput);

            if (String.IsNullOrWhiteSpace(dir))
                dir = DEFAULT_OUTPUT_DIRECTORY;
            if (String.IsNullOrWhiteSpace(name))
                name = "result";

            ensure_directory_exists(dir);
            return Path.Combine(dir, name + "_residual0.csv");
        }

        void calc_predict_interval(double residual, double consuming_speed,
            out double speed_low, out double speed_high,
            out double timeleft_low, out double timeleft_high,
            out double need_low, out double need_high, out bool out_of_window)
        {
            double safe_speed = consuming_speed;
            if (double.IsNaN(safe_speed) || double.IsInfinity(safe_speed) || safe_speed <= 0)
                safe_speed = PREDICT_SPEED_FLOOR;

            double speed_band = Math.Abs(safe_speed) * PREDICT_SPEED_BAND_RATIO;
            speed_low = Math.Max(PREDICT_SPEED_FLOOR, safe_speed - speed_band);
            speed_high = Math.Max(speed_low, safe_speed + speed_band);

            double residual_to_zero = Math.Max(0, residual);

            // 以「電量歸零」為終點，估計最保守/最樂觀生存時間區間。
            timeleft_low = residual_to_zero / speed_high;
            timeleft_high = residual_to_zero / speed_low;

            double horizon = Math.Max(1.0, common.fix_waiting_time);
            need_low = Math.Max(0, speed_low * horizon - residual_to_zero);
            need_high = Math.Max(need_low, speed_high * horizon - residual_to_zero);

            out_of_window = (timeleft_low <= horizon);
        }

        void write_predict_to_ui(int node_id, double speed_low, double speed_high, double timeleft_low, double timeleft_high, bool out_of_window)
        {
            label25.Text = String.Format("Pred N{0}: v[{1:E2},{2:E2}]", node_id, speed_low, speed_high);
            label24.Text = String.Format("TZ[{0:F2},{1:F2}]s Risk:{2}",
                timeleft_low * common.TIME_UNIT, timeleft_high * common.TIME_UNIT, out_of_window ? "Y" : "N");
        }

        void write_predict_to_log(int node_id, double speed_low, double speed_high, double timeleft_low, double timeleft_high,
            double need_low, double need_high, bool out_of_window)
        {
            if (common.ofs == null) return;
            common.ofs.WriteLine("預測請求, {0:F2}, {1}, {2:E6}, {3:E6}, {4:F2}, {5:F2}, {6:E6}, {7:E6}, {8}",
                common.current_time * common.TIME_UNIT, node_id, speed_low, speed_high,
                timeleft_low * common.TIME_UNIT, timeleft_high * common.TIME_UNIT,
                need_low, need_high, out_of_window ? 1 : 0);
        }

        private void button14_Click_1(object sender, EventArgs e)
        {
            int i, fi;
            string outputDir = normalize_directory_path(DirI.Text);

            StreamWriter ofs;
            Aheight = Int32.Parse(areaH.Text);
            Awidth = Int32.Parse(areaW.Text);
            maxbase = Int32.Parse(maxBaseI.Text);
            maxnode = Int32.Parse(maxNodeI.Text);
            DirI.Text = outputDir;

            for (fi = 1; fi <= Int32.Parse(FileNI.Text); fi++)
            {
                ofs = new StreamWriter(Path.Combine(outputDir, fi + ".re"));
                ofs.WriteLine(String.Format("Height: {0} Width: {1}", Aheight, Awidth));
                ofs.WriteLine(String.Format("Base: {0} Node: {1}", maxbase, maxnode));

                for (i = maxbase; i < maxnode; i++)
                {
                    ofs.WriteLine("{0} {1}", i, common.rand.NextDouble()*0.2+0.1);
                }

                ofs.Close();
            }
            MessageBox.Show("Done!");
        }

        private void button16_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "餘電檔(*.re)|*.re|All files(*.*)|*.*";
            openFileDialog1.InitialDirectory = get_initial_directory(residualFile.Text);
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                residualFile.Text = openFileDialog1.FileName;
        }

        void Do_process(int nid)
        {
            //時間格點 current_time，節點nid檢查並處理其event list中應該被處理的事件
            packet pkt;
            double dist, consuming_speed;
            request_event tmp;
            double p_speed_low, p_speed_high, p_timeleft_low, p_timeleft_high, p_need_low, p_need_high;
            bool p_out_of_window;
            if (nid >= maxbase && common.nmap.node[nid].status == 0)
                common.nmap.refresh_bpr_state(nid, false);

            //checking residual
            if (common.nmap.node[nid].residual <= common.Origin_RESIDUAL * common.request_threshold && common.nmap.node[nid].status==0)
            { //send charging request
                pkt = new packet();
                pkt.source_id = nid;
                pkt.dest_id = 0; //send request to base station
                pkt.ev_x = -1; //no use
                pkt.ev_y = -1; //no use
                pkt.ev_id = 0; //for debuging, no use
                pkt.leng = common.default_pkt_leng;
                pkt.residual = common.nmap.node[nid].residual;
                consuming_speed = common.nmap.get_estimated_consume_speed(nid);
                pkt.consuming_speed = consuming_speed;
                pkt.request_deadline = common.current_time;
                pkt.remain_work_time = Math.Max(0.0,
                    (common.nmap.node[nid].residual - common.nmap.get_service_floor(nid)) / Math.Max(consuming_speed, 1e-6));
                pkt.depletion_deadline = common.current_time + pkt.remain_work_time;
                calc_predict_interval(pkt.residual, consuming_speed,
                    out p_speed_low, out p_speed_high,
                    out p_timeleft_low, out p_timeleft_high,
                    out p_need_low, out p_need_high, out p_out_of_window);
                pkt.predict_speed_low = p_speed_low;
                pkt.predict_speed_high = p_speed_high;
                pkt.predict_timeleft_low = p_timeleft_low;
                pkt.predict_timeleft_high = p_timeleft_high;
                pkt.predict_charge_need_low = p_need_low;
                pkt.predict_charge_need_high = p_need_high;
                pkt.predict_out_of_window = p_out_of_window;
                pkt.sent_time = common.current_time;
                common.nmap.refresh_bpr_state(nid, true);
                common.nmap.node[nid].request_reference_residual = pkt.residual;
                common.nmap.node[nid].has_request_reference = true;
                common.nmap.node[nid].setEvent(common.SEND_DATA, pkt, common.current_time, nid);
                common.nmap.node[nid].status = 1; //已发出充电需求，在充电车完成充电后会reset为0
                common.redraw = true;
                common.request_task++;
                //common.ofs.WriteLine("Time: {0}  nid:{1} - {2}", common.current_time, nid, common.nmap.node[nid].residual);
            }

            //end of checking

            List<event_entry> torun;
            torun = common.nmap.node[nid].event_list.FindAll(delegate (event_entry e) { return e.T_time == common.current_time; });

            foreach (event_entry e in torun)
            {
                pkt = e.p;
                switch (e.P_Id)
                {
                    case common.LOST_EVENT:
                        event_loss++;
                        checkout_timing(pkt, (char)(common.DETECT_FAIL));
                        break;
                    case common.SEND_DATA:
                        if (common.nmap.node[nid].fid[0] >= 0)
                        {
                            pkt.leng = pkt.leng * mu; // 壓縮封包
                            pkt.fid = common.nmap.node[nid].fid[0];
                            //pkt.fid = find_best_fid(nid, false);
                            //succ = common.nmap.cal_succ(nid, pkt.fid, 1, 0.5);
                        }
                        if (pkt.source_id == nid && pkt.remain_work_time == -1)
                            common.sent_packet++;
                        if (common.nmap.node[nid].residual >= common.nmap.node[nid].tx_energy(mu * pkt.leng) && common.nmap.node[nid].fid[0] >= 0)
                        {
                            if (pkt.source_id == nid)
                            {
                                data_sent++;
                                
                                pkt.ev_id = common.packet_id;
                                common.packet_id++;
                            }
                            common.nmap.node[nid].consuming_power(common.TX, pkt.leng);
                            pkt.hop = common.nmap.node[nid].hop;
                            
                            //pkt.source_id = nid;
                            pkt.next_id = pkt.fid;

                            common.nmap.node[pkt.fid].setEvent(common.RECV_DATA, pkt, common.current_time + 1, nid);
                            //             one_hop_broadcast(nid, RECV_DATA, pkt,common.current_time+1);
                            if (!silent)
                            {
                                link_node(nid, pkt.next_id, Color.Red);
                                Thread.Sleep(200);
                                link_node(nid, pkt.next_id, Color.White);
                            }
                            
                        }
                        else // 沒有電源，改由其他最近節點偵測事件
                        {
                            //if (common.nmap.node[nid].residual < common.nmap.node[nid].ET * mu * pkt.leng)
                            // {
                            //     common.nmap.node[nid].residual = 0;
                            // }

                            common.dead_sentout++;
                            data_loss++;
                            checkout_timing(pkt, (char)(common.PKT_LOST));
                        }
                        break;
                    case common.RECV_DATA:
                        if (common.nmap.node[nid].residual >= common.nmap.node[nid].rx_energy(pkt.leng))
                        {
                            common.nmap.node[nid].consuming_power(common.RX, pkt.leng);
                            if (nid == pkt.next_id)
                            {
                                if (nid != pkt.dest_id && nid >= maxbase) //到達基地台即可算成功收到
                                {
                                    pkt.ev_x = -1;
                                    pkt.ev_y = -1;
                                    common.nmap.node[nid].setEvent(common.SEND_DATA, pkt, common.current_time + 1, nid);
                                    break;
                                }
                                else
                                { // 到達基地台，應判斷資料是否為charging request                                  
                                    
                                    if (pkt.remain_work_time > 0)
                                    {
                                        double transitTime = common.current_time - pkt.sent_time;
                                        double e_timeleft = Math.Max(0.0, pkt.depletion_deadline - common.current_time);
                                        double currentResidual = Math.Max(0.0, pkt.residual - transitTime * pkt.consuming_speed);
                                        tmp = new request_event(pkt.source_id, currentResidual, common.current_time, e_timeleft);
                                        tmp.consuming_speed = pkt.consuming_speed;
                                        calc_predict_interval(tmp.residual, tmp.consuming_speed,
                                            out p_speed_low, out p_speed_high,
                                            out p_timeleft_low, out p_timeleft_high,
                                            out p_need_low, out p_need_high, out p_out_of_window);
                                        tmp.predict_speed_low = p_speed_low;
                                        tmp.predict_speed_high = p_speed_high;
                                        tmp.predict_timeleft_low = p_timeleft_low;
                                        tmp.predict_timeleft_high = p_timeleft_high;
                                        tmp.predict_charge_need_low = p_need_low;
                                        tmp.predict_charge_need_high = p_need_high;
                                        tmp.predict_out_of_window = p_out_of_window;
                                        tmp.request_deadline = common.current_time;
                                        tmp.request_deadline_low = tmp.request_deadline;
                                        tmp.request_deadline_high = tmp.request_deadline;
                                        tmp.request_reference_residual = pkt.residual;
                                        common.nmap.node[tmp.node_id].request_reference_residual = pkt.residual;
                                        common.nmap.node[tmp.node_id].has_request_reference = true;
                                        tmp.depletion_deadline = Math.Max(common.current_time, pkt.depletion_deadline);
                                        tmp.deadline = tmp.depletion_deadline;
                                        tmp.deadline_low = Math.Max(common.current_time, tmp.request_time + tmp.predict_timeleft_low);
                                        tmp.deadline_high = Math.Max(tmp.deadline_low, tmp.request_time + tmp.predict_timeleft_high);
                                        tmp.is_proactive = false;
                                        tmp.event_id = request_event.max_id++;
                                        common.nmap.request_charging_list.Add(tmp);
                                        common.nmap.register_on_demand_request(tmp);
                                        common.nmap.node[tmp.node_id].status = 1;
                                        predict_count++;
                                        if (tmp.predict_out_of_window) predict_out_window_count++;
                                        write_predict_to_ui(tmp.node_id, tmp.predict_speed_low, tmp.predict_speed_high, tmp.predict_timeleft_low, tmp.predict_timeleft_high, tmp.predict_out_of_window);
                                        write_predict_to_log(tmp.node_id, tmp.predict_speed_low, tmp.predict_speed_high, tmp.predict_timeleft_low, tmp.predict_timeleft_high,
                                            tmp.predict_charge_need_low, tmp.predict_charge_need_high, tmp.predict_out_of_window);

                                       // common.nmap.node[nid].consume_speed = ((e.residual / e.timeleft) * common.TIME_UNIT);
                                        //基地台使用 calcMinRedundentTime 以計算目前需求何時之前該啟動充電程序
                                        if (common.available_num_car <= 0)
                                            common.min_next_charging_time = -2;
                                        else
                                            common.min_next_charging_time = common.nmap.calcMinRedundentTime(common.available_num_car);
                                    }
                                    else common.recv_packet++;
                                    data_recv++;
                                    checkout_timing(pkt, (char)(common.EVENT_RECV));
                                }
                            }
                        }
                        else
                        {
                            common.nmap.node[nid].residual = 0;
                            
                            data_loss++;                            
                            checkout_timing(pkt, (char)(common.PKT_LOST));
                        }

                        break;
                    case common.SEND_HELLO:

                        break;

                    case common.RECV_HELLO:
                        break;
                    case common.SEND_UPDATE_GR:
                        if (common.nmap.node[nid].residual >= common.nmap.node[nid].tx_energy_unit())
                        {
                            pkt.hop = common.nmap.node[nid].hop;
                            pkt.fid = common.nmap.node[nid].fid[0];
                            //pkt.fid = find_best_fid(nid, false);
                            pkt.source_id = nid;

                            //不考慮建立成本
                            //             node[nid].consuming_power(ET, pkt);

                            one_hop_broadcast(nid, common.RECV_UPDATE_GR, pkt, common.current_time + 1);
                        }
                        else
                            common.nmap.node[nid].residual = 0;
                        break;
                    case common.RECV_UPDATE_GR:
                        if (common.nmap.node[nid].residual >= common.nmap.node[nid].rx_energy_unit())
                        {
                            //不考慮建立成本
                            //             node[nid].consuming_power(ER, pkt);

                            //check if the distance(nid,pkt.source_id) < node[nid].p_range
                            dist = common.mydist(common.nmap.node[nid].x, common.nmap.node[nid].y, common.nmap.node[pkt.source_id].x, common.nmap.node[pkt.source_id].y);
                            if (dist <= common.nmap.node[nid].prange && (pkt.hop + 1 <=common.nmap. node[nid].hop))
                            {
                                if (pkt.hop + 1 < common.nmap.node[nid].hop)
                                    common.nmap.node[nid].setEvent(common.SEND_UPDATE_GR, pkt, common.current_time + 1, nid);
                                common.nmap.node[nid].update_info(pkt);
                            }
                        }
                        else
                        {
                            //             if (node[nid].residual > 0) dead_node++;
                            common.nmap.node[nid].residual = 0;
                        }
                        break;
                }

            }// end of for each
            common.nmap.node[nid].event_list.RemoveAll(delegate(event_entry e) { return e.T_time == common.current_time; });

            if (first_dead_time == -1 && common.nmap.node[nid].residual < common.nmap.node[nid].tx_energy(common.default_pkt_leng))
            {
                first_dead_time = common.current_time;
            }
        }
        private void init_values()
        {
            common.total_charged = 0;
            common.total_life = 0;
            common.total_move = 0;
            common.total_time_exec = 0;
            common.call_gene_count = 0;
            common.total_gene_iterations = 0;
            common.total_car_used = 0;
            common.total_sent = 0;
            common.total_recved = 0;
            common.total_deadsentout = 0;
            common.total_lost = 0;
            common.total_charged = common.total_charging_generated = common.total_late_charged = 0;
            //common.total_collected_charger = 0;
            //common.total_used_charger = 0;
            //common.sum_max_used_charger = 0;
        }
        private void out_first_part()
        {
            common.total_num_test = common.end_n - common.start_n + 1;
            outfilename.Text = normalize_csv_output_path(outfilename.Text);
            common.ofs = should_append_repeat_output(outfilename.Text)
                ? open_csv_append_writer(outfilename.Text)
                : open_csv_overwrite_writer(outfilename.Text);
            write_batch_run_marker(common.ofs);
            common.ofs.Write("測試檔, " + textBox1.Text + "/" + common.start_n + "~" + common.end_n + ".map");
            common.ofs.Write(", 排程方法, " + common.method_sel);
            //common.ofs.Write(", 排程方法, " + common.method_sel + "-" + common.assign_car_method);
            common.ofs.Write(", CarN, " + num_car.Text);
            //common.ofs.Write(", MaxI, " + common.max_iteration);
            //common.ofs.Write(", Population, " + common.population_size);
            //common.ofs.Write(", 調整視窗大小," + common.window_size);
            //common.ofs.Write(", 次數臨界值," + common.modify_cnt);
            //common.ofs.Write(", 回收距離臨界值," + common.collecting_distance_threshold);
        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void out_last_part()
        {
            if (common.ofs == null)
                return;

            common.ofs.Write(", 第一節點死亡時間, " + common.total_life / common.total_num_test);
            common.ofs.Write(", 計算時間, " + common.total_time_exec / common.total_num_test);
            //common.ofs.Write(", 平均每次gene計算時間, " + common.total_time_exec / common.call_gene_count);
            //common.ofs.Write(", 使用充電車數, " + common.total_car_used / (double)common.call_gene_count);
            //common.ofs.Write(", 平均累積移動距離, " + common.total_move / common.total_num_test);
            //common.ofs.Write(", 完成充電節點數," + common.total_charged / common.total_num_test);
            common.ofs.Write(", 充電距離," + common.total_move / common.total_charged);
            //common.ofs.Write(", 平均使用充電器數," + common.total_used_charger / common.total_num_test);
            //common.ofs.Write(", 平均最大同時使用充電器數," + common.sum_max_used_charger / common.total_num_test);
            //common.ofs.WriteLine(", 平均回收充電器數," + common.total_collected_charger / common.total_num_test);
            //common.ofs.Write(", 平均基因iteration數," + (double)common.total_gene_iterations);
            //common.ofs.Write(", 平均每次iteration數," + (double)common.total_gene_iterations / common.call_gene_count);
            common.ofs.Write(", dead_sentout," + (double)common.total_deadsentout / common.total_num_test);
            common.ofs.Write(", data_sent," + (double)common.total_sent / common.total_num_test);
            common.ofs.Write(", data_recv," + (double)common.total_recved / common.total_num_test);
            common.ofs.Write(", request_generated," + (double)common.total_charging_generated / common.total_num_test);
            common.ofs.Write(", in time charged," + (double)common.total_charged / common.total_num_test);
            common.ofs.Write(", late charged," + (double)common.total_late_charged / common.total_num_test);
            common.ofs.Write(", lost," + (double)common.total_lost / common.total_num_test);
            common.ofs.Write(", predict_generated," + predict_count);
            common.ofs.WriteLine(", predict_out_of_window," + predict_out_window_count);
            common.ofs.Close();
            common.ofs = null;
        }
        private void button11_Click(object sender, EventArgs e)
        { //GO 启动代码
            int ji, i;
            double max_waiting;
            int sttime, report_time;
            packet pkt = new packet();
            int repeatCount;
            button11.Text = "Running";
            button11.Refresh();
            label24.Text = "Go pressed";
            label25.Text = DateTime.Now.ToString("HH:mm:ss");
            Timelabel.Text = "Click";
            Timelabel.Refresh();
            update_run_status_label("Click");
            Application.DoEvents();
            string outputPath = normalize_csv_output_path(outfilename.Text);

            if (!try_get_repeat_count(out repeatCount))
                return;

            currentBatchRunCount = repeatCount;
            outfilename.Text = outputPath;

            for (int runIndex = 1; runIndex <= repeatCount; runIndex++)
            {
                currentBatchRunIndex = runIndex;
                Timelabel.Text = "0";
                Timelabel.Refresh();
                update_run_status_label(format_run_stage("Preparing", runIndex, repeatCount));

                if (!prepare_single_run_inputs())
                {
                    update_run_status_label("Prepare failed");
                    currentBatchRunIndex = 1;
                    currentBatchRunCount = 1;
                    return;
                }

                silent = smode.Checked;
                mu = double.Parse(mu_text.Text);
                update_run_status_label(format_run_stage("Config", runIndex, repeatCount));
                //設定 x, y軸比例，以適當顯示節點於螢幕上
                Powerrange = int.Parse(prange.Text);
                POWER_VAL = int.Parse(PVin.Text);
                ER = int.Parse(ERin.Text);
                common.request_threshold = Double.Parse(rq_ratio.Text);
                common.Origin_RESIDUAL = Double.Parse(original_in.Text);
                common.max_num_car = int.Parse(num_car.Text);
                common.angle_sorting = angleSorting.Checked;
                common.EDF_gene = EDF_gene.Checked;
                common.NJF_gene = NJF_gene.Checked;
                common.useMaxCar = useMaxCar.Checked;
                rec_time = int.Parse(rectime_in.Text);
                outfilename.Text = outputPath;
                ensure_utf8_bom(outfilename.Text);

                start_rate_change_log();

                if (!autoload)
                {
                    common.assign_car_method = 1;
                    init_values();
                    out_first_part();
                }
                // initialize variables
                data_sent = data_loss = data_recv = 0;
                first_dead_time = -1;

         //因不同方法设定部分参数
                common.car_no_return = true; //充电车完成任务后不返回基站
                common.fix_activate_condition = true;
                common.dynamic_schedule = false; //充电排程在充電車離開充電站後還會因新到的充電任務重排
                if (!autoload)
                {
                    common.ofs.WriteLine();
                    common.ofs.WriteLine("{0}  T:{1}", textBox1.Text, Powerrange);
                }

                if (EDF_btn.Checked)
                {
                    common.car_no_return = true;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = true;
                    common.method_sel = 1;
                    if (!autoload) common.ofs.WriteLine("Method: EDF");
                }
                else if (GENE_btn.Checked)
                {
                    common.car_no_return = false;
                    common.method_sel = 2;
                    common.fix_activate_condition = false;
                    if (!autoload) common.ofs.WriteLine("Method: GENE");
                }
                else if (LIN_btn.Checked)
                {
                    common.method_sel = 3;
                    common.car_no_return = true;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = true;
                    if (!autoload) common.ofs.WriteLine("Method: LIN");
                }
                else if (RCSS_btn.Checked)
                {
                    common.method_sel = 4;
                    common.car_no_return = true;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = true;
                    if (!autoload) common.ofs.WriteLine("Method: RCSS");
                }
                else if (PSO_btn.Checked)
                {
                    common.method_sel = 5;
                    common.car_no_return = false;
                    common.fix_activate_condition = false;
                    common.dynamic_schedule = false;
                    if (!autoload) common.ofs.WriteLine("Method: PSO");
                }
                else if (cuck_btn.Checked)
                {
                    common.method_sel = 6;
                    common.car_no_return = false;
                    common.fix_activate_condition = false;
                    common.dynamic_schedule = false;
                    if (!autoload) common.ofs.WriteLine("Method: Cuckoo");
                }
                else if (M_LIN.Checked)
                {
                    common.method_sel = 7;
                    common.car_no_return = true;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = true;
                    if (!autoload) common.ofs.WriteLine("Method: M_LIN");
                }
                else if (D_LIN.Checked)
                {
                    common.method_sel = 8;
                    common.car_no_return = true;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = true;
                    if (!autoload) common.ofs.WriteLine("Method: D_LIN");
                }
                else if (NEDF_btn.Checked)
                {
                    common.method_sel = 9;
                    common.car_no_return = true;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = true;
                    if (!autoload) common.ofs.WriteLine("Method: NEDF");
                }
                else if (NNJF_btn.Checked)
                {
                    common.method_sel = 10;
                    common.car_no_return = false;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = false;
                    if (!autoload) common.ofs.WriteLine("Method: BPR_NJF");
                }
                else if (Prevention_btn.Checked)
                {
                    common.method_sel = 11;
                    common.car_no_return = false;
                    common.fix_activate_condition = true;
                    common.dynamic_schedule = false;
                    if (!autoload) common.ofs.WriteLine("Method: Prevention");
                }
                if (!autoload)
                    common.ofs.WriteLine("時間, 移動距離, 已送封包, 已收封包, 準時完成任務, 延遲任務, 總請求數, 平均剩餘電量, 節點失電送失敗");
                if (!autoload)
                    common.ofs.WriteLine("預測請求, 模擬時間(秒), 節點ID, 耗電速率下界, 耗電速率上界, 歸零時間下界(秒), 歸零時間上界(秒), 充電缺口下界, 充電缺口上界, 超出安全區間");
         //结束方法设定

                //maxbase 为基站数量
                for (i = 0; i < maxbase; i++)
                { //设定各基站传送距离限制
                    common.nmap.node[i].prange = common.nmap.node[i].next_prange = Powerrange;
                    common.nmap.node[i].ET = 0;  // 基地台不計耗電
                }

                for (i = maxbase; i < maxnode; i++)
                {
                    common.nmap.node[i].residual = common.Origin_RESIDUAL * common.nmap.node[i].residual; //节点初始残電
                    common.nmap.node[i].pre_residual = common.nmap.node[i].residual; //节点之前残電，用于估计耗电速度
                    if (assign_prange) //no use
                        common.nmap.node[i].prange = common.nmap.node[i].next_prange = Powerrange;
                    common.nmap.node[i].ER = ER;  // nJ/bit
                    common.nmap.node[i].ET = Math.Pow(common.nmap.node[i].prange, POWER_VAL) * common.Eamp + common.nmap.node[i].ER; // nJ/bit
                }
                for (i = 0; i < 100000; i++)
                { //记录封包接收情况
                    common.recved[i] = false;
                }
                for (i = 0; i < maxnode; i++)
                {
                    build_neighbor(i, true);
                    if (i == 0 || (i + 1) % 100 == 0 || i == maxnode - 1)
                        update_run_status_label(format_run_stage_progress("Neighbors", runIndex, repeatCount, i + 1, maxnode));
                }
                //Init_GR(1);
                update_run_status_label(format_run_stage("Tree", runIndex, repeatCount));
                common.nmap.rebuild_tree();

                sttime = int.Parse(stop_time.Text);
                report_time = (int)(int.Parse(rectime_in.Text) / common.TIME_UNIT);
                rec_time = int.Parse(rectime_in.Text);
                report_time = (int)((sttime / 10) / common.TIME_UNIT);
                int rate_change_interval = (int)(common.RATE_CHANGE_INTERVAL_SEC / common.TIME_UNIT);

                bool start_charging;
                common.recalc_MRT = false;
                common.min_next_charging_time = common.MYINFINITE;
                common.moving_distance = 0;
                common.request_task = 0;
                common.missed_task = 0;
                common.done_task = 0;
                common.sent_packet = 0;
                common.recv_packet = 0;
                common.dead_sentout = 0;
                common.available_num_car = common.max_num_car;
                predict_count = 0;
                predict_out_window_count = 0;
                label25.Text = "Pred: waiting";
                label24.Text = "TZ: waiting";
                lowResidualBelowState = new bool[maxnode];
                zeroResidualReachedState = new bool[maxnode];
                update_run_status_label(format_run_stage("Cars", runIndex, repeatCount));
                //多辆充电车设定
                for (i = 0; i < common.max_num_car; i++)
                {
                    common.nmap.car_list.Add(new car(common.nmap.node[0].x, common.nmap.node[0].y, common.car_speed, common.num_charger_per_car));
                }
                common.nmap.refresh_all_bpr_states();
                last_event_time = 0;
                update_run_status_label(format_run_stage("Open logs", runIndex, repeatCount));
                start_low_residual_log();
                start_zero_residual_log();

                update_run_status_label(format_run_stage("Loop", runIndex, repeatCount));
                Timelabel.Text = common.TIME_UNIT.ToString("F2");
                Timelabel.Refresh();
                Application.DoEvents();
                for (common.current_time = 1; common.current_time <= (sttime) / common.TIME_UNIT; common.current_time++)
                {  // 主要時間迴圈
                    if (common.current_time == 1)
                    {
                        Timelabel.Text = "1";
                        Timelabel.Refresh();
                        Application.DoEvents();
                    }
                    if (common.current_time * common.TIME_UNIT >= last_event_time)
                        load_event_sec(common.current_time * common.TIME_UNIT);
                    if (rate_change_interval > 0 && common.current_time % rate_change_interval == 0)
                        update_node_consuming_rates();
                    apply_background_consumption();
                    if (common.current_time % 10000 == 0)
                    {
                        Timelabel.Text = (common.current_time * common.TIME_UNIT).ToString();
                        md.Text = common.moving_distance.ToString("F2");
                        missed.Text = common.missed_task.ToString();
                        taskdone.Text = common.done_task.ToString();
                        pktsent.Text = common.sent_packet.ToString();
                        pktrecv.Text = common.recv_packet.ToString();
                        Timelabel.Refresh();
                        md.Refresh();
                        missed.Refresh();
                        taskdone.Refresh();
                        pktsent.Refresh();
                        pktrecv.Refresh();
                        Application.DoEvents();
                    }

                    start_charging = false;
                    if (common.available_num_car > 0)
                    {
                        if (common.fix_activate_condition)
                            start_charging = common.nmap.request_charging_list.Count() > 0;
                        else
                        {
                            if (common.min_next_charging_time == -2)
                            {
                                common.min_next_charging_time = common.nmap.calcMinRedundentTime(common.available_num_car);
                            }
                            max_waiting = 9400 * Math.Min(common.nmap.request_charging_list.Count, common.num_charger_per_car * common.available_num_car);
                            start_charging = common.nmap.request_charging_list.Count() > 0 && (common.nmap.request_charging_list.Count() >= common.num_charger_per_car * common.available_num_car ||
                                 (common.min_next_charging_time > 0 && common.current_time >= common.min_next_charging_time - max_waiting) || common.min_next_charging_time <= 0);
                        }
                    }
                    if (start_charging)
                    { //啟動充電車
                        int car_ready = common.nmap.get_available_car();

                        if (car_ready > 0)
                        {
                            common.available_num_car = car_ready;

                            common.nmap.start_charging();
                            if (common.nmap.request_charging_list.Count() > 0 && !common.fix_activate_condition)
                                common.min_next_charging_time = common.nmap.calcMinRedundentTime(common.available_num_car);
                        }
                    }
                    int maxleng = 0;
                    for (ji = 0; ji < maxnode; ji++)
                    {
                        if (common.nmap.node[ji].event_list.Count > maxleng)
                            maxleng = common.nmap.node[ji].event_list.Count;
                        Do_process(ji);
                        log_low_residual_crossing(ji);
                        log_zero_residual_crossing(ji);
                    }

                    if (common.first_dead_time >= 0)
                    {
                        break;
                    }

                    for (ji = 0; ji < common.max_num_car; ji++)
                    { //充電車處理
                        common.nmap.car_list[ji].Do_process();
                    }
                    refresh_low_residual_state();
                    refresh_zero_residual_state();

                    int visualRefreshInterval = silent ? SILENT_VISUAL_REFRESH_INTERVAL : NORMAL_VISUAL_REFRESH_INTERVAL;
                    if (common.current_time % visualRefreshInterval == 0 && (common.redraw || has_sensor_status_highlight()))
                    {
                        if (!silent)
                            Thread.Sleep(100);

                        draw_nodes();

                        if (!silent)
                        { //充電車
                            for (ji = 0; ji < common.max_num_car; ji++)
                            {
                                draw_point((int)common.nmap.car_list[ji].x, (int)common.nmap.car_list[ji].y, Color.Red, 5);
                            }
                        }

                        common.redraw = false;
                    }

                    if (common.current_time % report_time == 0)
                    {
                        if (!autoload)
                            common.ofs.WriteLine("{0}, {1:f2}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", common.current_time * common.TIME_UNIT, common.moving_distance, common.sent_packet, common.recv_packet, common.done_task, common.missed_task, common.request_task, avg_residual(), common.dead_sentout);
                        else
                            Console.WriteLine("{0}, {1:f2}, {2}, {3}, {4}, {5}, {6}, {7}, {8}", common.current_time * common.TIME_UNIT, common.moving_distance, common.sent_packet, common.recv_packet, common.done_task, common.missed_task, common.request_task, avg_residual(), common.dead_sentout);
                    }
                }
                if (first_dead_time >= 0)
                    common.total_life += first_dead_time;
                else
                    common.total_life += common.current_time;

                common.total_move += common.moving_distance;
                common.total_deadsentout += common.dead_sentout;
                common.total_sent += data_sent;
                common.total_recved += data_recv;
                common.total_late_charged += common.missed_task;
                common.total_charging_generated += common.request_task;
                common.total_charged += common.done_task;
                common.total_lost += data_loss;
                update_run_status_label(repeatCount > 1 ? String.Format("Done {0}/{1}", runIndex, repeatCount) : "Done");

                close_event_stream();
                if (!autoload)
                {
                    out_last_part();
                }
                stop_rate_change_log();
                stop_low_residual_log();
                stop_zero_residual_log();
            }

            currentBatchRunIndex = 1;
            currentBatchRunCount = 1;
            outfilename.Text = outputPath;
            if (repeatCount > 1)
                label16.Text = String.Format("Done {0}/{0}", repeatCount);
        }

        double avg_residual()
        {
            double total_residual=0;
            for (int i=maxbase; i<maxnode; i++)
            {
                total_residual += common.nmap.node[i].residual;
            }
            return total_residual / (maxnode - maxbase);
        }
        private void button10_Click(object sender, EventArgs e)
        {
            draw_nodes();
            for (int i = maxbase; i < maxnode; i++)
                if (common.nmap.node[i].fid[0] >= 0)
                    link_node(i, common.nmap.node[i].fid[0], Color.Blue);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] words;
            //format: 1.map dir 2.start_n 3.end_n 4. num nodes 5. output 6.exec time 7.method 8. threshold 9.车数 10.车速(m/s) 11.充电速度 (J/sec) 12.population size 13.max_iteration 14. EDF_gene 15. NJF_gene 16. AngleSorting
            words = Environment.GetCommandLineArgs();
            if (words.Length > 1)
            {
                common.source_dir = words[1];
                common.start_n = Convert.ToInt16(words[2]);
                common.end_n = Convert.ToInt16(words[3]);
                nodenum.Text = words[4];
                outfilename.Text = words[5];
                stop_time.Text = words[6];
                switch (words[7])
                {
                    case "1": EDF_btn.Checked = true; break;
                    case "2": GENE_btn.Checked = true; break;
                    case "3": LIN_btn.Checked = true; break;
                    case "4": RCSS_btn.Checked = true; break;
                    case "5": cuck_btn.Checked = true; break;
                    case "6": PSO_btn.Checked = true; break;
                    case "7": M_LIN.Checked = true; break;
                    case "8": D_LIN.Checked = true; break;
                    case "9": NEDF_btn.Checked = true; break;
                    case "10": NNJF_btn.Checked = true; break;
                    case "11": Prevention_btn.Checked = true; break;
                }
                rq_ratio.Text = words[8];
                num_car.Text = words[9];
                common.car_speed = Int32.Parse(words[10]);
                common.charging_speed = Double.Parse(words[11])*Math.Pow(10,9);
                common.population_size= Int32.Parse(words[12]);
                common.max_iteration = Int32.Parse(words[13]);
                angleSorting.Checked = Int32.Parse(words[14])==1;
                EDF_gene.Checked = Int32.Parse(words[15]) == 1;
                NJF_gene.Checked = Int32.Parse(words[16]) == 1;
                useMaxCar.Checked = Int32.Parse(words[17]) == 1;
                common.assign_car_method = 1;
                autoload = true;
            }
            else
            {
                autoload = false;
                apply_default_storage_paths();
            }

        }

        private void Form1_Activated(object sender, EventArgs e)
        {
            if (autoload)
            {
                init_values();
                out_first_part();
                for (int i = common.start_n; i <= common.end_n; i++)
                {
                    textBox1.Text = common.source_dir + "/" + i + ".map";
                    textBox2.Text = common.source_dir + "/" + i + ".ev";
                    residualFile.Text = common.source_dir + "/" + i + ".re";
                    button8_Click(sender, e);
                    button11_Click(sender, e);
                }
                out_last_part();
                Environment.Exit(0);
            }
        }

        private void button12_Click(object sender, EventArgs e)
        {
            int nn;
            nn = int.Parse(shownode.Text);
            draw_nodes();
            draw_point(common.nmap.node[nn].x, common.nmap.node[nn].y, Color.Red);
        }

        private void button13_Click(object sender, EventArgs e)
        {
            label24.Text=common.nmap.check_node_load().ToString();
        }

        private void button15_Click(object sender, EventArgs e)
        {
            for (int i = 0; i < maxnode; i++)
                common.nmap.node[i].prange = int.Parse(prange.Text); ;
            common.nmap.rebuild_tree();
            button10_Click(sender, e);
        }      

        private void button17_Click(object sender, EventArgs e)
        {
            int nn;
            draw_nodes();
            for (nn = maxbase; nn < maxnode; nn++)
            {
                if (common.nmap.node[nn].residual < common.nmap.node[nn].tx_energy_unit())
                   draw_point(common.nmap.node[nn].x, common.nmap.node[nn].y, Color.Red);
            }
        }

        private double mytheda(int a1, int a2, int b1, int b2)
        {
            return Math.Acos((a1*b1+a2*b2)/(Math.Sqrt(a1*a1+a2*a2)*Math.Sqrt(b1*b1+b2*b2)));
        }
        private void button18_Click(object sender, EventArgs e)
        {
            int[] cnt = new int[36];
            double[] ecost = new double[36];
            Point[] center_line = new Point[maxbase];
            Point temp_line=new Point();
            int cx = Awidth / 2, cy = Aheight / 2; 
            double tdist, mindist, tangle; 
            int i, j, minbase=0, aclass;
            for (i=0; i<36; i++){
                cnt[i]=0;
                ecost[i]=0;
            }
            for (i = 0; i < maxbase; i++)
            {
                center_line[i].X = cy - common.nmap.node[i].y;
                center_line[i].Y = cx - common.nmap.node[i].x;
            }
            for (i = maxbase; i < maxnode; i++)
            {
                mindist = 10000000;
                for (j = 0; j < maxbase; j++)
                {
                    // find out the nearest sink
                    tdist = common.mydist(common.nmap.node[j].x, common.nmap.node[j].y, common.nmap.node[i].x, common.nmap.node[i].y);
                    if (tdist < mindist)
                    {
                        mindist = tdist;
                        minbase = j;
                    }
                }
                    temp_line.X=common.nmap.node[i].x - common.nmap.node[minbase].x;
                    temp_line.Y=common.nmap.node[i].y - common.nmap.node[minbase].y;
                    tangle = mytheda(temp_line.X, temp_line.Y, center_line[minbase].X, center_line[minbase].Y);
                    tangle = Math.Abs (tangle);                
                    tangle = tangle * 180.0 / 3.1415926;
                    if (tangle > 90) tangle = 180 - tangle;
                    aclass = (int)( Math.Truncate(tangle / 10));
                    cnt[aclass]++;
                    ecost[aclass] += (common.Origin_RESIDUAL - common.nmap.node[i].residual);
                
            }
            ofs = open_csv_append_writer(outfilename.Text);
            ofs.WriteLine();
            for (i=0; i<6; i++)
                ofs.WriteLine("{0}  E:{1}  C:{2}", i, (cnt[i]==0? 0:ecost[i]/cnt[i]), cnt[i]);
            ofs.Close();
            MessageBox.Show ("Done!");
        }
        private void button19_Click(object sender, EventArgs e)
        {

        }

    }
}
