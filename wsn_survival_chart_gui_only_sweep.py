# -*- coding: utf-8 -*-
"""
WSN Sweep 平均生命週期折線圖 GUI 產生器（只在 UI 顯示，不輸出 PNG/CSV）

功能：
1. 在 VS Code 直接執行後開啟 GUI。
2. 用「瀏覽...」選擇 WSN sweep Excel 結果檔。
3. 預設讀取「彙總統計」sheet，也可改讀「執行比較」sheet。
4. 勾選要畫的演算法。
5. 直接在 UI 內顯示折線圖：
   - x 軸：SweepValue
   - y 軸：平均生命週期(s)
   - 點位順序：依 SweepIndex 排序，SweepIndex 0 為第一個點、1 為第二個點，以此類推。

需求：
    pip install matplotlib

執行：
    python wsn_survival_chart_gui_only.py
"""

import sys
import tkinter as tk
from tkinter import filedialog, messagebox, ttk
import zipfile
import xml.etree.ElementTree as ET
from pathlib import Path
from collections import defaultdict

import matplotlib
try:
    matplotlib.use("TkAgg")
except ImportError:
    # 讓沒有桌面環境的測試環境仍可匯入；一般 Windows / VS Code GUI 執行時會使用 TkAgg。
    matplotlib.use("Agg")
import matplotlib.pyplot as plt
from matplotlib.figure import Figure
from matplotlib import font_manager
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk


DEFAULT_SHEET_NAME = "彙總統計"
FALLBACK_SHEET_NAME = "執行比較"
DEFAULT_SWEEP_INDEX_COL = "SweepIndex"
DEFAULT_SWEEP_VALUE_COL = "SweepValue"
DEFAULT_ALGORITHM_COL = "演算法"
DEFAULT_AVERAGE_LIFETIME_COL = "平均生命週期(s)"
DEFAULT_LIFETIME_COL = "網路生命週期(s)"


def setup_chinese_font():
    preferred_fonts = [
        "Microsoft JhengHei",
        "Microsoft YaHei",
        "SimHei",
        "Noto Sans CJK TC",
        "Noto Sans CJK SC",
        "Arial Unicode MS",
    ]

    available = set(f.name for f in font_manager.fontManager.ttflist)
    selected = None

    for font_name in preferred_fonts:
        if font_name in available:
            selected = font_name
            break

    if selected:
        plt.rcParams["font.sans-serif"] = [selected]

    plt.rcParams["axes.unicode_minus"] = False


def excel_col_to_index(cell_ref):
    letters = ""

    for ch in cell_ref:
        if ch.isalpha():
            letters += ch
        else:
            break

    index = 0
    for ch in letters:
        index = index * 26 + (ord(ch.upper()) - ord("A") + 1)

    return index - 1


def read_xlsx_sheet_values(path, sheet_name):
    """
    只用 Python 標準庫讀 xlsx，不需要 openpyxl。
    """
    ns_main = "http://schemas.openxmlformats.org/spreadsheetml/2006/main"
    ns_rel = "http://schemas.openxmlformats.org/officeDocument/2006/relationships"
    ns = {"a": ns_main}

    with zipfile.ZipFile(path) as z:
        file_list = set(z.namelist())

        shared_strings = []
        if "xl/sharedStrings.xml" in file_list:
            shared_root = ET.fromstring(z.read("xl/sharedStrings.xml"))
            for si in shared_root.findall(f"{{{ns_main}}}si"):
                text = "".join(t.text or "" for t in si.iter(f"{{{ns_main}}}t"))
                shared_strings.append(text)

        workbook = ET.fromstring(z.read("xl/workbook.xml"))
        workbook_rels = ET.fromstring(z.read("xl/_rels/workbook.xml.rels"))
        rel_map = {rel.attrib["Id"]: rel.attrib["Target"] for rel in workbook_rels}

        sheet_target = None
        available_sheets = []

        for sh in workbook.find("a:sheets", ns):
            name = sh.attrib.get("name")
            available_sheets.append(name)

            if name == sheet_name:
                rid = sh.attrib[f"{{{ns_rel}}}id"]
                target = rel_map[rid]
                sheet_target = "xl/" + target.lstrip("/")
                break

        if sheet_target is None:
            raise ValueError(
                "找不到工作表：{0}\n目前 Excel 內的工作表：{1}".format(
                    sheet_name,
                    ", ".join(available_sheets)
                )
            )

        sheet_xml = ET.fromstring(z.read(sheet_target))
        sheet_data = sheet_xml.find("a:sheetData", ns)

        rows = []
        if sheet_data is None:
            return rows

        for row in sheet_data:
            cells = []
            max_col = -1

            for cell in row:
                cell_ref = cell.attrib.get("r", "A1")
                col_idx = excel_col_to_index(cell_ref)
                cell_type = cell.attrib.get("t")
                value = ""

                if cell_type == "inlineStr":
                    value = "".join(t.text or "" for t in cell.iter(f"{{{ns_main}}}t"))
                else:
                    v = cell.find("a:v", ns)
                    if v is not None:
                        raw = v.text or ""
                        if cell_type == "s":
                            value = shared_strings[int(raw)]
                        else:
                            value = raw

                cells.append((col_idx, value))
                max_col = max(max_col, col_idx)

            values = [""] * (max_col + 1)
            for col_idx, value in cells:
                values[col_idx] = value

            rows.append(values)

        return rows


def normalize_algorithm_name(display_name):
    """
    把 Excel 裡較長的演算法名稱縮短，避免圖例太長。
    """
    key = str(display_name).split("（")[0].strip()

    mapping = {
        "NJF_ROUTE_ZHENG_BPR_LIMITED": "ZHENG_LIMITED",
        "NJF_ROUTE_ZHENG_BPR_EXTENDED": "ZHENG_EXTENDED",
        "NJF_ROUTE_YU_BPR_LIMITED": "YU_LIMITED",
        "NJF_ROUTE_YU_BPR_EXTENDED": "YU_EXTENDED",
        "NJF_ZHENG_BPR": "ZHENG_BPR",
        "NJF_YU_BPR": "YU_BPR",
        "TADP/LIN": "TADP_LIN",
    }

    return mapping.get(key, key)


def find_column(headers, requested_name, fallback_keywords):
    for i, header in enumerate(headers):
        if str(header).strip() == requested_name:
            return i

    for i, header in enumerate(headers):
        text = str(header).strip()

        for keyword in fallback_keywords:
            if keyword in text:
                return i

    raise ValueError(
        "找不到欄位：{0}\n目前欄位：{1}".format(
            requested_name,
            ", ".join(map(str, headers))
        )
    )


def find_column_optional(headers, requested_name, fallback_keywords):
    try:
        return find_column(headers, requested_name, fallback_keywords)
    except ValueError:
        return None


def format_sweep_value(value):
    text = str(value).strip()

    try:
        number = float(text)
        if number.is_integer():
            return str(int(number))
        return "{0:g}".format(number)
    except (ValueError, TypeError):
        return text


def parse_records(rows, sheet_name):
    if not rows:
        raise ValueError("工作表沒有資料：" + sheet_name)

    headers = rows[0]

    sweep_index_col = find_column(
        headers,
        DEFAULT_SWEEP_INDEX_COL,
        ["SweepIndex", "Sweep Index", "sweep index", "掃描序號"]
    )
    sweep_value_col = find_column(
        headers,
        DEFAULT_SWEEP_VALUE_COL,
        ["SweepValue", "Sweep Value", "sweep value", "掃描值"]
    )
    algo_col = find_column(headers, DEFAULT_ALGORITHM_COL, ["演算法", "Algorithm", "algorithm"])

    # 「彙總統計」sheet 通常已經有平均生命週期；若使用「執行比較」sheet，則改讀每次 run 的生命週期再自行平均。
    lifetime_col = find_column_optional(
        headers,
        DEFAULT_AVERAGE_LIFETIME_COL,
        ["平均生命週期", "AverageLifetime", "average lifetime"]
    )
    if lifetime_col is None:
        lifetime_col = find_column_optional(
            headers,
            DEFAULT_LIFETIME_COL,
            ["網路生命週期", "生命週期", "Lifetime", "lifetime"]
        )

    if lifetime_col is None:
        raise ValueError(
            "找不到生命週期欄位：{0} 或 {1}\n目前欄位：{2}".format(
                DEFAULT_AVERAGE_LIFETIME_COL,
                DEFAULT_LIFETIME_COL,
                ", ".join(map(str, headers))
            )
        )

    records = []

    for row in rows[1:]:
        if len(row) <= max(sweep_index_col, sweep_value_col, algo_col, lifetime_col):
            continue

        try:
            sweep_index = int(float(row[sweep_index_col]))
            lifetime = float(row[lifetime_col])
        except (ValueError, TypeError):
            continue

        records.append(
            {
                "SweepIndex": sweep_index,
                "SweepValue": format_sweep_value(row[sweep_value_col]),
                "Algorithm": normalize_algorithm_name(row[algo_col]),
                "LifetimeSeconds": lifetime,
            }
        )

    if not records:
        raise ValueError(
            "沒有讀到可用資料，請確認 sheet 內有 SweepIndex、SweepValue、演算法，以及 平均生命週期(s) 或 網路生命週期(s)。"
        )

    return records


def build_sweep_lifetime_tables(records, selected_algorithms):
    filtered = [r for r in records if r["Algorithm"] in selected_algorithms]
    sweep_indices = sorted(set(r["SweepIndex"] for r in filtered))
    algorithms = [a for a in selected_algorithms if any(r["Algorithm"] == a for r in filtered)]

    sweep_value_by_index = {}
    values_by_algo_index = defaultdict(list)

    for r in filtered:
        sweep_value_by_index.setdefault(r["SweepIndex"], r["SweepValue"])
        values_by_algo_index[(r["Algorithm"], r["SweepIndex"])].append(r["LifetimeSeconds"])

    average_by_algo_index = defaultdict(dict)
    for key, values in values_by_algo_index.items():
        algo, sweep_index = key
        if values:
            average_by_algo_index[algo][sweep_index] = sum(values) / len(values)

    sweep_values = [sweep_value_by_index.get(i, str(i)) for i in sweep_indices]

    return sweep_indices, sweep_values, algorithms, average_by_algo_index


def draw_line_on_figure(fig, sweep_indices, sweep_values, algorithms, average_by_algo_index):
    fig.clear()
    ax = fig.add_subplot(111)

    x_positions = list(range(len(sweep_indices)))

    for algo in algorithms:
        y_values = [average_by_algo_index[algo].get(sweep_index, None) for sweep_index in sweep_indices]
        ax.plot(x_positions, y_values, marker="o", linewidth=1.8, markersize=4.5, label=algo)

    ax.set_title("SweepValue 與平均生命週期")
    ax.set_xlabel("SweepValue")
    ax.set_ylabel("平均生命週期（秒）")
    ax.set_xticks(x_positions)
    ax.set_xticklabels(sweep_values)
    ax.grid(True, alpha=0.3)
    ax.legend(loc="best", fontsize=8)
    fig.tight_layout()



class ScrollableCheckFrame(ttk.Frame):
    def __init__(self, parent):
        super().__init__(parent)

        self.canvas = tk.Canvas(self, height=170, highlightthickness=1, highlightbackground="#cccccc")
        self.scrollbar = ttk.Scrollbar(self, orient="vertical", command=self.canvas.yview)
        self.inner = ttk.Frame(self.canvas)

        self.inner.bind(
            "<Configure>",
            lambda event: self.canvas.configure(scrollregion=self.canvas.bbox("all"))
        )

        self.canvas_window = self.canvas.create_window((0, 0), window=self.inner, anchor="nw")
        self.canvas.configure(yscrollcommand=self.scrollbar.set)

        self.canvas.bind(
            "<Configure>",
            lambda event: self.canvas.itemconfig(self.canvas_window, width=event.width)
        )

        self.canvas.pack(side="left", fill="both", expand=True)
        self.scrollbar.pack(side="right", fill="y")


class ChartPreviewFrame(ttk.Frame):
    def __init__(self, parent, title):
        super().__init__(parent)

        self.figure = Figure(figsize=(8, 4.5), dpi=100)
        self.canvas = FigureCanvasTkAgg(self.figure, master=self)
        self.toolbar = NavigationToolbar2Tk(self.canvas, self)
        self.toolbar.update()

        self.canvas_widget = self.canvas.get_tk_widget()
        self.canvas_widget.pack(fill="both", expand=True)

        ax = self.figure.add_subplot(111)
        ax.text(0.5, 0.5, title + "\n尚未產生圖表", ha="center", va="center", fontsize=14)
        ax.axis("off")
        self.figure.tight_layout()
        self.canvas.draw()

    def refresh(self):
        self.canvas.draw_idle()


class WsnChartGui(tk.Tk):
    def __init__(self):
        super().__init__()

        self.title("WSN Sweep 平均生命週期折線圖產生器")
        self.geometry("1180x820")
        self.minsize(1050, 720)

        self.records = []
        self.algorithm_vars = {}

        self.xlsx_path_var = tk.StringVar()
        self.sheet_name_var = tk.StringVar(value=DEFAULT_SHEET_NAME)
        self.status_var = tk.StringVar(value="請先選擇 Excel 檔案。")

        self.build_ui()

    def build_ui(self):
        root = ttk.Frame(self, padding=12)
        root.pack(fill="both", expand=True)

        title = ttk.Label(root, text="WSN Sweep 平均生命週期折線圖產生器", font=("Microsoft JhengHei UI", 16, "bold"))
        title.pack(anchor="w")

        subtitle = ttk.Label(root, text="選 Excel、勾選演算法，直接在此視窗顯示 SweepValue 對平均生命週期(s)的折線圖，不會輸出 PNG/CSV。")
        subtitle.pack(anchor="w", pady=(2, 10))

        top_pane = ttk.PanedWindow(root, orient="horizontal")
        top_pane.pack(fill="both", expand=True)

        left_panel = ttk.Frame(top_pane)
        right_panel = ttk.Frame(top_pane)

        top_pane.add(left_panel, weight=0)
        top_pane.add(right_panel, weight=1)

        file_frame = ttk.LabelFrame(left_panel, text="檔案選擇", padding=10)
        file_frame.pack(fill="x")

        ttk.Label(file_frame, text="Excel 檔案").grid(row=0, column=0, sticky="w", padx=(0, 8), pady=4)
        ttk.Entry(file_frame, textvariable=self.xlsx_path_var, width=46).grid(row=0, column=1, sticky="ew", pady=4)
        ttk.Button(file_frame, text="瀏覽...", command=self.browse_xlsx).grid(row=0, column=2, padx=(8, 0), pady=4)

        ttk.Label(file_frame, text="工作表名稱").grid(row=1, column=0, sticky="w", padx=(0, 8), pady=4)
        ttk.Entry(file_frame, textvariable=self.sheet_name_var, width=24).grid(row=1, column=1, sticky="w", pady=4)
        ttk.Button(file_frame, text="讀取 Excel", command=self.load_excel).grid(row=1, column=2, padx=(8, 0), pady=4)

        file_frame.columnconfigure(1, weight=1)

        algo_frame = ttk.LabelFrame(left_panel, text="演算法選擇", padding=10)
        algo_frame.pack(fill="x", pady=(10, 0))

        self.check_frame = ScrollableCheckFrame(algo_frame)
        self.check_frame.pack(fill="x")

        algo_button_frame = ttk.Frame(algo_frame)
        algo_button_frame.pack(fill="x", pady=(8, 0))

        ttk.Button(algo_button_frame, text="全選", command=self.select_all).pack(side="left")
        ttk.Button(algo_button_frame, text="全部取消", command=self.clear_all).pack(side="left", padx=(8, 0))
        ttk.Button(algo_button_frame, text="產生/更新圖表", command=self.generate_charts).pack(side="right")

        output_frame = ttk.LabelFrame(left_panel, text="狀態", padding=10)
        output_frame.pack(fill="both", expand=True, pady=(10, 0))

        ttk.Label(output_frame, textvariable=self.status_var).pack(anchor="w", pady=(0, 4))

        self.log_text = tk.Text(output_frame, height=15, wrap="word")
        self.log_text.pack(fill="both", expand=True)

        self.notebook = ttk.Notebook(right_panel)
        self.notebook.pack(fill="both", expand=True)

        self.line_preview = ChartPreviewFrame(self.notebook, "Sweep 折線圖")

        self.notebook.add(self.line_preview, text="折線圖：SweepValue / 平均生命週期")

    def log(self, text):
        self.log_text.insert("end", text + "\n")
        self.log_text.see("end")
        self.status_var.set(text)
        self.update_idletasks()

    def browse_xlsx(self):
        path = filedialog.askopenfilename(
            title="選擇 WSN 比較結果 Excel",
            filetypes=[
                ("Excel 檔案", "*.xlsx"),
                ("所有檔案", "*.*"),
            ],
        )

        if not path:
            return

        self.xlsx_path_var.set(path)
        self.load_excel()

    def load_excel(self):
        raw_path = self.xlsx_path_var.get().strip().strip('"')
        xlsx = Path(raw_path)
        sheet_name = self.sheet_name_var.get().strip() or DEFAULT_SHEET_NAME

        if not raw_path:
            messagebox.showwarning("尚未選擇檔案", "請先選擇 Excel 檔案。")
            return

        if not xlsx.exists():
            messagebox.showerror("錯誤", "找不到 Excel 檔案：\n" + str(xlsx))
            return

        try:
            self.log("讀取 Excel：" + str(xlsx))
            try:
                rows = read_xlsx_sheet_values(xlsx, sheet_name)
            except ValueError:
                if sheet_name == DEFAULT_SHEET_NAME:
                    self.log("找不到「{0}」，改讀「{1}」。".format(DEFAULT_SHEET_NAME, FALLBACK_SHEET_NAME))
                    sheet_name = FALLBACK_SHEET_NAME
                    self.sheet_name_var.set(sheet_name)
                    rows = read_xlsx_sheet_values(xlsx, sheet_name)
                else:
                    raise

            self.records = parse_records(rows, sheet_name)

            algorithms = sorted(set(r["Algorithm"] for r in self.records))
            sweep_indices = sorted(set(r["SweepIndex"] for r in self.records))

            self.populate_algorithms(algorithms)

            self.log("讀取完成：{0} 筆資料，{1} 個 Sweep 點，{2} 個演算法。".format(
                len(self.records),
                len(sweep_indices),
                len(algorithms)
            ))

        except Exception as ex:
            messagebox.showerror("讀取失敗", str(ex))
            self.log("讀取失敗：" + str(ex))

    def populate_algorithms(self, algorithms):
        for child in self.check_frame.inner.winfo_children():
            child.destroy()

        self.algorithm_vars.clear()

        for i, algo in enumerate(algorithms):
            var = tk.BooleanVar(value=True)
            self.algorithm_vars[algo] = var

            check = ttk.Checkbutton(self.check_frame.inner, text=algo, variable=var)
            check.grid(row=i, column=0, sticky="w", padx=6, pady=3)

    def select_all(self):
        for var in self.algorithm_vars.values():
            var.set(True)

    def clear_all(self):
        for var in self.algorithm_vars.values():
            var.set(False)

    def selected_algorithms(self):
        return [algo for algo, var in self.algorithm_vars.items() if var.get()]

    def generate_charts(self):
        if not self.records:
            messagebox.showwarning("尚未讀取 Excel", "請先選擇並讀取 Excel 檔案。")
            return

        selected = self.selected_algorithms()
        if not selected:
            messagebox.showwarning("尚未選擇演算法", "請至少勾選一個演算法。")
            return

        try:
            self.log("開始產生圖表...")

            sweep_indices, sweep_values, algorithms, average_by_algo_index = build_sweep_lifetime_tables(self.records, selected)

            if not algorithms:
                raise ValueError("沒有可繪圖的演算法資料。")

            draw_line_on_figure(self.line_preview.figure, sweep_indices, sweep_values, algorithms, average_by_algo_index)

            self.line_preview.refresh()

            self.log("完成：圖表已更新在 UI 上。")
            self.log("目前顯示 {0} 個 Sweep 點、{1} 個演算法。".format(len(sweep_indices), len(algorithms)))

            self.notebook.select(self.line_preview)

        except Exception as ex:
            messagebox.showerror("產生失敗", str(ex))
            self.log("產生失敗：" + str(ex))


def main():
    setup_chinese_font()
    app = WsnChartGui()
    app.mainloop()


if __name__ == "__main__":
    main()
