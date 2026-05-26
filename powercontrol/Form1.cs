using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace WindowsFormsApplication1
{

    public partial class Form1 : Form
    {
        Button experimentSaveButton;
        Button experimentRunButton;
        Button experimentSweepButton;
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
        ToolTip thresholdModeToolTip;
        CheckBox expWriteMissionDetailsBox;
        CheckBox expWriteTaskRecordsBox;
        CheckBox expWriteBprDebugBox;
        CheckBox expWriteYuBprDebugBox;
        CheckBox expFastSchedulingBox;
        CheckedListBox expAlgorithmList;
        TextBox expLogBox;
        Label expLastOutputLabel;
        Label expProgressLabel;
        Label expSweepSummaryLabel;
        
        public Form1()
        {
            InitializeComponent();
            initialize_experiment_panel();

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

            experimentSweepButton = new Button();
            experimentSweepButton.Text = "參數迭代設定";
            experimentSweepButton.Location = new Point(130, 233);
            experimentSweepButton.Size = new Size(130, 28);
            experimentSweepButton.Click += experimentSweepButton_Click;
            dataGroup.Controls.Add(experimentSweepButton);

            expSweepSummaryLabel = new Label();
            expSweepSummaryLabel.Text = "";
            expSweepSummaryLabel.Location = new Point(18, 264);
            expSweepSummaryLabel.Size = new Size(320, 20);
            dataGroup.Controls.Add(expSweepSummaryLabel);

            GroupBox energyGroup = create_group_box("能量、需求與動態耗能", 404, 92, 360, 290);
            expInitialEnergyBox = add_labeled_textbox(energyGroup, "初始能量(J)", "感測器滿電容量", 18, 30, "");
            expBackgroundLifetimeBox = add_labeled_textbox(energyGroup, "背景壽命(s)", "只靠連續耗能時的滿電壽命", 18, 64, "");
            expEventRateBox = add_labeled_textbox(energyGroup, "需求頻率 p(次/s)", "CHENG 啟動/充電需求頻率", 18, 98, "");
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
            expThresholdModeBox.Items.Add("CHENG Treq 自動門檻");
            expThresholdModeBox.Items.Add("手動 Treq 秒數門檻");
            expThresholdModeBox.Location = new Point(130, 261);
            expThresholdModeBox.Size = new Size(180, 22);
            expThresholdModeBox.SelectedIndexChanged += expThresholdModeBox_SelectedIndexChanged;
            energyGroup.Controls.Add(expThresholdModeBox);
            thresholdModeToolTip = new ToolTip();
            thresholdModeToolTip.SetToolTip(modeLabel, "Percent 模式只控制自然請求百分比門檻。BP&R 的預測範圍與 cooldown 需要時間參數。執行 BP&R 時請使用 ChengTreq，或明確設定 ProactivePredictionHorizonSeconds / ProactiveCooldownSeconds / BprDeadlineThresholdSeconds。");
            thresholdModeToolTip.SetToolTip(expThresholdModeBox, "Percent 模式只控制自然請求百分比門檻。BP&R 的預測範圍與 cooldown 需要時間參數。執行 BP&R 時請使用 ChengTreq，或明確設定 ProactivePredictionHorizonSeconds / ProactiveCooldownSeconds / BprDeadlineThresholdSeconds。");

            GroupBox wcvGroup = create_group_box("WCV 與任務流程", 784, 92, 360, 290);
            expWcvSpeedBox = add_labeled_textbox(wcvGroup, "WCV 速度(m/s)", "單台 WCV", 18, 30, "");
            expWcvChargeRateBox = add_labeled_textbox(wcvGroup, "充電速率(J/s)", "WCV 對節點充電", 18, 64, "");
            expWcvCapacityBox = add_labeled_textbox(wcvGroup, "WCV 容量(J)", "WCV 可用能量", 18, 98, "");
            expWcvMoveCostBox = add_labeled_textbox(wcvGroup, "移動耗能(J/m)", "WCV 每公尺消耗能量", 18, 132, "");
            expNmaxTaskBox = add_labeled_textbox(wcvGroup, "任務上限", "每趟最多服務節點數", 18, 166, "NmaxTask");
            expMapSizeBox.TextChanged += auto_treq_parameter_changed;
            expInitialEnergyBox.TextChanged += auto_treq_parameter_changed;
            expBackgroundLifetimeBox.TextChanged += auto_treq_parameter_changed;
            expRequestThresholdBox.TextChanged += auto_treq_parameter_changed;
            expWcvSpeedBox.TextChanged += auto_treq_parameter_changed;
            expWcvChargeRateBox.TextChanged += auto_treq_parameter_changed;
            expNmaxTaskBox.TextChanged += auto_treq_parameter_changed;
            Label bsLabel = new Label();
            bsLabel.Text = "基地台 = (0,0) sink + 充電中心；每趟任務後 WCV 返回基地台。";
            bsLabel.Location = new Point(18, 236);
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

            GroupBox outputGroup = create_group_box("執行與輸出", 404, 398, 740, 290);
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

            expFastSchedulingBox = new CheckBox();
            expFastSchedulingBox.Text = "高速平行排程";
            expFastSchedulingBox.Location = new Point(430, 70);
            expFastSchedulingBox.Size = new Size(120, 22);
            expFastSchedulingBox.Checked = true;
            outputGroup.Controls.Add(expFastSchedulingBox);

            Label csvOutputLabel = new Label();
            csvOutputLabel.Text = "明細 CSV";
            csvOutputLabel.Location = new Point(18, 108);
            csvOutputLabel.Size = new Size(90, 22);
            outputGroup.Controls.Add(csvOutputLabel);

            expWriteMissionDetailsBox = add_output_csv_checkbox(outputGroup, "mission-details", 110, 104, 130);
            expWriteTaskRecordsBox = add_output_csv_checkbox(outputGroup, "task-records", 245, 104, 125);
            expWriteBprDebugBox = add_output_csv_checkbox(outputGroup, "bpr-debug", 375, 104, 105);
            expWriteYuBprDebugBox = add_output_csv_checkbox(outputGroup, "yu-bpr-debug", 485, 104, 118);

            expLastOutputLabel = new Label();
            expLastOutputLabel.Text = "";
            expLastOutputLabel.Location = new Point(18, 138);
            expLastOutputLabel.Size = new Size(700, 22);
            outputGroup.Controls.Add(expLastOutputLabel);

            expProgressLabel = new Label();
            expProgressLabel.Text = "進度：尚未執行";
            expProgressLabel.Location = new Point(18, 160);
            expProgressLabel.Size = new Size(700, 22);
            outputGroup.Controls.Add(expProgressLabel);

            expLogBox = new TextBox();
            expLogBox.Multiline = true;
            expLogBox.ReadOnly = true;
            expLogBox.ScrollBars = ScrollBars.Vertical;
            expLogBox.Location = new Point(18, 184);
            expLogBox.Size = new Size(700, 86);
            outputGroup.Controls.Add(expLogBox);

            GroupBox noteGroup = create_group_box("資料產生說明", 24, 704, 1120, 120);
            Label note = new Label();
            note.Text = "本系統不使用舊 MyWSN 的單一節點排程介面。資料由亂數種子產生共用地圖、CHENG 啟動需求與耗能率變動時間表，所有演算法共用同一資料雜湊碼。";
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

        CheckBox add_output_csv_checkbox(Control parent, string text, int x, int y, int width)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Location = new Point(x, y);
            box.Size = new Size(width, 22);
            box.Checked = true;
            parent.Controls.Add(box);
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
            if (expWriteMissionDetailsBox != null)
                expWriteMissionDetailsBox.Checked = experimentSettings.WriteMissionDetailsCsv;
            if (expWriteTaskRecordsBox != null)
                expWriteTaskRecordsBox.Checked = experimentSettings.WriteTaskRecordsCsv;
            if (expWriteBprDebugBox != null)
                expWriteBprDebugBox.Checked = experimentSettings.WriteBprDebugCsv;
            if (expWriteYuBprDebugBox != null)
                expWriteYuBprDebugBox.Checked = experimentSettings.WriteYuBprDebugCsv;
            if (expFastSchedulingBox != null)
                expFastSchedulingBox.Checked = experimentSettings.UseFastSimulationScheduling;
            if (ChengTreqCalculator.IsChengTreqMode(experimentSettings.ThresholdMode))
                expThresholdModeBox.SelectedIndex = 1;
            else if (String.Equals(experimentSettings.ThresholdMode, "TreqSeconds", StringComparison.OrdinalIgnoreCase))
                expThresholdModeBox.SelectedIndex = 2;
            else
                expThresholdModeBox.SelectedIndex = 0;

            expAlgorithmList.Items.Clear();
            List<string> selected = experimentSettings.GetSelectedAlgorithms();
            string[] all = ExperimentSettings.AllAlgorithms();
            for (int i = 0; i < all.Length; i++)
            {
                string display = get_algorithm_display_name(all[i]);
                expAlgorithmList.Items.Add(display, selected.Contains(all[i]));
            }

            update_threshold_mode_ui();
            update_sweep_summary_label();
            update_last_output_label();
        }

        void apply_experiment_controls_to_settings()
        {
            apply_experiment_controls_to_settings(false);
        }

        void apply_experiment_controls_to_settings(bool validateWcvFeasibility)
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
            experimentSettings.WcvSpeedMetersPerSecond = parse_double(expWcvSpeedBox, experimentSettings.WcvSpeedMetersPerSecond);
            experimentSettings.WcvChargeRateJPerSecond = parse_double(expWcvChargeRateBox, experimentSettings.WcvChargeRateJPerSecond);
            experimentSettings.WcvCapacityJ = parse_double(expWcvCapacityBox, experimentSettings.WcvCapacityJ);
            experimentSettings.WcvMoveCostJPerMeter = parse_double(expWcvMoveCostBox, experimentSettings.WcvMoveCostJPerMeter);
            experimentSettings.NmaxTask = parse_int(expNmaxTaskBox, experimentSettings.NmaxTask);
            experimentSettings.OutputDirectory = expOutputDirectoryBox.Text.Trim();
            if (expWriteMissionDetailsBox != null)
                experimentSettings.WriteMissionDetailsCsv = expWriteMissionDetailsBox.Checked;
            if (expWriteTaskRecordsBox != null)
                experimentSettings.WriteTaskRecordsCsv = expWriteTaskRecordsBox.Checked;
            if (expWriteBprDebugBox != null)
                experimentSettings.WriteBprDebugCsv = expWriteBprDebugBox.Checked;
            if (expWriteYuBprDebugBox != null)
                experimentSettings.WriteYuBprDebugCsv = expWriteYuBprDebugBox.Checked;
            experimentSettings.WriteTaskDetailCsv = experimentSettings.HasAnyTaskDetailCsvOutput();
            if (expFastSchedulingBox != null)
                experimentSettings.UseFastSimulationScheduling = expFastSchedulingBox.Checked;
            if (expThresholdModeBox.SelectedIndex == 1)
            {
                experimentSettings.ThresholdMode = "ChengTreq";
                double treq = experimentSettings.ComputeAutoTreqSeconds();
                experimentSettings.TreqSeconds = treq;
                experimentSettings.BprDeadlineThresholdSeconds = treq;
            }
            else if (expThresholdModeBox.SelectedIndex == 2)
            {
                experimentSettings.ThresholdMode = "TreqSeconds";
                experimentSettings.TreqSeconds = parse_double(expTreqBox, experimentSettings.TreqSeconds);
            }
            else
            {
                experimentSettings.ThresholdMode = "Percent";
                experimentSettings.RequestThresholdPercent = parse_double(expRequestThresholdBox, experimentSettings.RequestThresholdPercent);
            }

            List<string> algorithms = new List<string>();
            for (int i = 0; i < expAlgorithmList.CheckedItems.Count; i++)
                algorithms.Add(get_algorithm_key_from_display(Convert.ToString(expAlgorithmList.CheckedItems[i])));
            experimentSettings.SetSelectedAlgorithms(algorithms);
            if (validateWcvFeasibility)
            {
                BprTimingValidator.ThrowIfInvalid(experimentSettings);
                WcvMaxTaskFeasibilityValidator.ThrowIfInvalid(experimentSettings);
            }
            experimentSettings.Normalize();
            populate_experiment_controls_from_settings();
        }

        void expThresholdModeBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            update_threshold_mode_ui();
        }

        void auto_treq_parameter_changed(object sender, EventArgs e)
        {
            if (expThresholdModeBox != null && expThresholdModeBox.SelectedIndex == 1)
                update_threshold_mode_ui();
        }

        void set_task_detail_csv_checkboxes_enabled(bool enabled)
        {
            CheckBox[] boxes = new CheckBox[] {
                expWriteMissionDetailsBox,
                expWriteTaskRecordsBox,
                expWriteBprDebugBox,
                expWriteYuBprDebugBox
            };
            for (int i = 0; i < boxes.Length; i++)
            {
                if (boxes[i] != null)
                {
                    boxes[i].Enabled = enabled;
                }
            }
        }

        void update_threshold_mode_ui()
        {
            if (expThresholdModeBox == null || expRequestThresholdBox == null || expTreqBox == null)
                return;

            if (expThresholdModeBox.SelectedIndex == 1)
            {
                update_treq_preview_settings_from_controls();
                experimentSettings.ThresholdMode = "ChengTreq";
                try
                {
                    double treq = experimentSettings.ComputeAutoTreqSeconds();
                    experimentSettings.TreqSeconds = treq;
                    experimentSettings.BprDeadlineThresholdSeconds = treq;
                    expTreqBox.Text = treq.ToString(System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (InvalidOperationException ex)
                {
                    expTreqBox.Text = ex.Message;
                }
                expTreqBox.ReadOnly = true;
                expTreqBox.Enabled = true;
                expRequestThresholdBox.ReadOnly = true;
                expRequestThresholdBox.Enabled = false;
            }
            else if (expThresholdModeBox.SelectedIndex == 2)
            {
                experimentSettings.ThresholdMode = "TreqSeconds";
                expTreqBox.ReadOnly = false;
                expTreqBox.Enabled = true;
                expRequestThresholdBox.ReadOnly = true;
                expRequestThresholdBox.Enabled = false;
            }
            else
            {
                experimentSettings.ThresholdMode = "Percent";
                expRequestThresholdBox.ReadOnly = false;
                expRequestThresholdBox.Enabled = true;
                expTreqBox.ReadOnly = true;
                expTreqBox.Enabled = false;
            }
        }

        void update_treq_preview_settings_from_controls()
        {
            double mapSize = parse_double(expMapSizeBox, experimentSettings.MapWidthMeters);
            experimentSettings.MapWidthMeters = mapSize;
            experimentSettings.MapHeightMeters = mapSize;
            experimentSettings.InitialEnergyJ = parse_double(expInitialEnergyBox, experimentSettings.InitialEnergyJ);
            experimentSettings.SensorBackgroundLifetimeSeconds = parse_double(expBackgroundLifetimeBox, experimentSettings.SensorBackgroundLifetimeSeconds);
            experimentSettings.RequestThresholdPercent = parse_double(expRequestThresholdBox, experimentSettings.RequestThresholdPercent);
            experimentSettings.WcvSpeedMetersPerSecond = parse_double(expWcvSpeedBox, experimentSettings.WcvSpeedMetersPerSecond);
            experimentSettings.WcvChargeRateJPerSecond = parse_double(expWcvChargeRateBox, experimentSettings.WcvChargeRateJPerSecond);
            experimentSettings.NmaxTask = parse_int(expNmaxTaskBox, experimentSettings.NmaxTask);
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
            if (key == "NJF_CHENG_BPR") return "NJF_CHENG_BPR（CHENG原文BP&R，seeded random）";
            if (key == "TADP_CHENG_BPR") return "TADP_CHENG_BPR（CHENG原文BP&R，seeded random）";
            if (key == "EDF_CHENG_BPR") return "EDF_CHENG_BPR（CHENG原文BP&R，seeded random）";
            if (key == "NJF_ZHENG_BPR") return "NJF_ZHENG_BPR（ZHENG BP&R deterministic extension）";
            if (key == "NJF_YU_BPR") return "NJF_YU_BPR（YU interval BP&R extension，非CHENG原文）";
            if (key == "NJF_ROUTE_ZHENG_BPR_LIMITED") return "NJF_ROUTE_ZHENG_BPR_LIMITED（WCV route-aware extension，<=NmaxTask）";
            if (key == "NJF_ROUTE_ZHENG_BPR_EXTENDED") return "NJF_ROUTE_ZHENG_BPR_EXTENDED（WCV route-aware extension，可超過NmaxTask）";
            if (key == "NJF_ROUTE_YU_BPR_LIMITED") return "NJF_ROUTE_YU_BPR_LIMITED（WCV route-aware YU extension，<=NmaxTask）";
            if (key == "NJF_ROUTE_YU_BPR_EXTENDED") return "NJF_ROUTE_YU_BPR_EXTENDED（WCV route-aware YU extension，可超過NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_LIMITED") return "NJF_BPR_ROUTE_SAFE_LIMITED（ZHENG BP&R route-cost，<=NmaxTask）";
            if (key == "NJF_BPR_ROUTE_SAFE_EXTENDED") return "NJF_BPR_ROUTE_SAFE_EXTENDED（ZHENG BP&R route-cost，可超過NmaxTask）";
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

        void update_sweep_summary_label()
        {
            if (expSweepSummaryLabel == null || experimentSettings == null)
                return;

            if (!experimentSettings.SweepEnabled)
            {
                expSweepSummaryLabel.Text = "參數迭代：停用";
                return;
            }

            ExperimentSweepParameterDefinition definition = ExperimentSweepParameterCatalog.Find(experimentSettings.SweepParameterKey);
            if (definition == null)
            {
                expSweepSummaryLabel.Text = "參數迭代：設定錯誤";
                return;
            }

            double startValue = definition.GetValue(experimentSettings);
            double endValue = startValue + experimentSettings.SweepStepValue * experimentSettings.SweepIterationCount;
            expSweepSummaryLabel.Text = String.Format(System.Globalization.CultureInfo.InvariantCulture,
                "參數迭代：{0} {1} -> {2}，共 {3} 組",
                definition.DisplayName,
                definition.FormatValue(startValue),
                definition.FormatValue(endValue),
                experimentSettings.SweepIterationCount + 1);
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

        void experimentSweepButton_Click(object sender, EventArgs e)
        {
            apply_experiment_controls_to_settings();
            using (ExperimentSweepSettingsDialog dialog = new ExperimentSweepSettingsDialog(experimentSettings))
            {
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    update_sweep_summary_label();
                    log_experiment_message(experimentSettings.SweepEnabled ? "參數迭代已啟用。" : "參數迭代已停用。");
                }
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
            try
            {
                apply_experiment_controls_to_settings(true);
                experimentSettings.Normalize();
                List<WcvMaxTaskFeasibilityResult> feasibilityResults =
                    WcvMaxTaskFeasibilityValidator.ValidateSelectedAlgorithms(experimentSettings);
                for (int i = 0; i < feasibilityResults.Count; i++)
                {
                    if (!feasibilityResults[i].IsValid)
                    {
                        log_experiment_message(feasibilityResults[i].ErrorMessage);
                        MessageBox.Show(this, feasibilityResults[i].ErrorMessage, "WSN 實驗設定錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                log_experiment_message("WCV feasibility validation passed.");
                List<BprTimingValidationResult> bprTimingResults =
                    BprTimingValidator.ValidateSelectedAlgorithms(experimentSettings);
                for (int i = 0; i < bprTimingResults.Count; i++)
                {
                    if (!bprTimingResults[i].IsValid)
                    {
                        log_experiment_message(bprTimingResults[i].ErrorMessage);
                        MessageBox.Show(this, bprTimingResults[i].ErrorMessage, "WSN 實驗設定錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }
                log_experiment_message("BP&R timing validation passed.");
                experimentSettings.SaveLast();
            }
            catch (Exception ex)
            {
                log_experiment_message("設定錯誤：" + ex.Message);
                MessageBox.Show(this, ex.Message, "WSN 實驗設定錯誤", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            experimentRunButton.Enabled = false;
            experimentSaveButton.Enabled = false;
            experimentSweepButton.Enabled = false;
            set_task_detail_csv_checkboxes_enabled(false);
            if (expFastSchedulingBox != null)
                expFastSchedulingBox.Enabled = false;
            expProgressLabel.Text = "進度：準備開始";
            log_experiment_message("批次比較執行中...");

            ExperimentSettings settingsToRun = experimentSettings.Copy();
            Thread worker = new Thread(delegate()
            {
                try
                {
                    Action<string> progressCallback = delegate(string message)
                    {
                        begin_update_experiment_status(message);
                    };
                    ExperimentBatchResult result;
                    if (settingsToRun.SweepEnabled)
                    {
                        ExperimentSweepBatchRunner runner = new ExperimentSweepBatchRunner(progressCallback, true);
                        result = runner.Run(settingsToRun);
                    }
                    else
                    {
                        ExperimentBatchRunner runner = new ExperimentBatchRunner(progressCallback);
                        result = runner.Run(settingsToRun);
                    }
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
                    experimentSweepButton.Enabled = true;
                    set_task_detail_csv_checkboxes_enabled(true);
                    if (expFastSchedulingBox != null)
                        expFastSchedulingBox.Enabled = true;
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


    }
}
