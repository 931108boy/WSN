# -*- coding: utf-8 -*-
"""
WSN Sweep 平均生命週期折線圖 GUI 產生器（只在 UI 顯示，不輸出 PNG/CSV）

功能：
1. 在 VS Code 直接執行後開啟 GUI。
2. 用「瀏覽...」選擇 WSN sweep Excel 結果檔。
3. 預設讀取「彙總統計」sheet，也可改讀「執行比較」sheet。
4. 勾選要畫的演算法。
5. 直接在 UI 內顯示三種折線圖：
   - 一般折線圖：Excel 演算法名稱會移除小括號內容。
   - 論文風格圖：參考 CHENG paper Fig. 12 的樣式。
   - 圖例分離版：圖表和 legend 分成上下兩塊，避免圖例蓋住折線。
6. 可勾選顯示/隱藏圖例，也可將論文風格圖另存為 PNG。
7. x 軸名稱會自動讀取 Excel 的 SweepParameterName，不再固定寫成 p。
8. 輸出 PNG 時，預設檔名格式：
   SweepParameterName + SweepValue[0] + "~" + SweepValue[-1]

需求：
    pip install matplotlib

執行：
    python wsn_survival_chart_gui_only_sweep_paper_style.py
"""

import sys
import math
import re
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
    matplotlib.use("Agg")

import matplotlib.pyplot as plt
from matplotlib.figure import Figure
from matplotlib import font_manager
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg, NavigationToolbar2Tk


DEFAULT_SHEET_NAME = "彙總統計"
FALLBACK_SHEET_NAME = "執行比較"

DEFAULT_SWEEP_INDEX_COL = "SweepIndex"
DEFAULT_SWEEP_VALUE_COL = "SweepValue"
DEFAULT_SWEEP_PARAMETER_NAME_COL = "SweepParameterName"
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
    圖例名稱照 Excel 的演算法欄位，只移除小括號內容，不再做自訂縮名 mapping。
    """
    text = str(display_name).strip()
    text = re.sub(r"\s*[（(][^（）()]*[）)]\s*", "", text).strip()
    return text


def try_float(value):
    try:
        number = float(str(value).strip())
        if math.isfinite(number):
            return number
    except (TypeError, ValueError):
        pass
    return None


def make_paper_label(algorithm):
    """
    論文風格圖用短圖例。
    CHENG Fig. 12 使用 NJF/P、TADP/P、EDF/P 表示有 BP&R proactive mechanism。
    不符合這些基本方法的演算法，保留 Excel 原名。
    """
    name = normalize_algorithm_name(algorithm)
    upper = name.upper().replace("-", "_").replace("/", "_")

    if upper == "NJF":
        return "NJF"
    if upper == "EDF":
        return "EDF"
    if upper in {"TADP", "TADP_LIN"}:
        return "TADP"

    if upper in {"NJF_CHENG_BPR", "NJF_BPR", "NJF_P"}:
        return "NJF/P"
    if upper in {"EDF_CHENG_BPR", "EDF_BPR", "EDF_P"}:
        return "EDF/P"
    if upper in {"TADP_CHENG_BPR", "TADP_BPR", "TADP_P", "TADP_LIN_BPR"}:
        return "TADP/P"

    if "BPR" in upper:
        if upper.startswith("NJF"):
            return name
        if upper.startswith("EDF"):
            return name
        if upper.startswith("TADP"):
            return name

    return name


def build_paper_style(algorithms):
    """
    給每條線固定 marker / linestyle，讓圖看起來比較接近論文。
    """
    marker_cycle = ["x", "o", "^", "v", "s", "*", "D", "P", "h", "+"]
    line_cycle = ["-", "-", "-", "-", "-", "-", "--", "--", "--", "--"]

    preferred = {
        "NJF": {"marker": "x", "linestyle": "-"},
        "NJF/P": {"marker": "o", "linestyle": "-"},
        "TADP": {"marker": "*", "linestyle": "-"},
        "TADP/P": {"marker": "s", "linestyle": "-"},
        "EDF": {"marker": "_", "linestyle": "-"},
        "EDF/P": {"marker": "^", "linestyle": "-"},
    }

    styles = {}
    for i, algo in enumerate(algorithms):
        short_label = make_paper_label(algo)
        style = preferred.get(short_label, {})
        styles[algo] = {
            "label": short_label,
            "marker": style.get("marker", marker_cycle[i % len(marker_cycle)]),
            "linestyle": style.get("linestyle", line_cycle[i % len(line_cycle)]),
        }
    return styles


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


def sanitize_filename(text):
    """
    清掉 Windows 檔名不能用的字元
    """
    text = str(text).strip()
    text = re.sub(r'[\\/:*?"<>|]', "_", text)
    return text


def build_export_filename(sweep_parameter_name, sweep_values):
    """
    檔名格式：
    SweepParameterName + SweepValue[0] + "~" + SweepValue[-1]
    例如：
        p2~10
        WCV 容量(J)80000~240000
    """
    param_name = str(sweep_parameter_name).strip() if sweep_parameter_name else "p"

    if sweep_values and len(sweep_values) >= 1:
        first_value = str(sweep_values[0]).strip()
        last_value = str(sweep_values[-1]).strip()
        base_name = f"{param_name}{first_value}~{last_value}"
    else:
        base_name = param_name

    return sanitize_filename(base_name)


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
    sweep_parameter_name_col = find_column_optional(
        headers,
        DEFAULT_SWEEP_PARAMETER_NAME_COL,
        ["SweepParameterName", "Sweep Parameter Name", "sweep parameter name", "掃描參數名稱", "參數名稱"]
    )
    algo_col = find_column(
        headers,
        DEFAULT_ALGORITHM_COL,
        ["演算法", "Algorithm", "algorithm"]
    )

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
        required_cols = [sweep_index_col, sweep_value_col, algo_col, lifetime_col]
        if any(c is None for c in required_cols):
            continue

        if len(row) <= max(required_cols):
            continue

        try:
            sweep_index = int(float(row[sweep_index_col]))
            lifetime = float(row[lifetime_col])
        except (ValueError, TypeError):
            continue

        sweep_parameter_name = "p"
        if sweep_parameter_name_col is not None and len(row) > sweep_parameter_name_col:
            candidate = str(row[sweep_parameter_name_col]).strip()
            if candidate:
                sweep_parameter_name = candidate

        records.append(
            {
                "SweepIndex": sweep_index,
                "SweepValue": format_sweep_value(row[sweep_value_col]),
                "SweepParameterName": sweep_parameter_name,
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
    if not filtered:
        return "p", [], [], [], defaultdict(dict)

    sweep_indices = sorted(set(r["SweepIndex"] for r in filtered))
    algorithms = [a for a in selected_algorithms if any(r["Algorithm"] == a for r in filtered)]

    sweep_value_by_index = {}
    values_by_algo_index = defaultdict(list)

    sweep_parameter_name = "p"
    for r in filtered:
        candidate_name = str(r.get("SweepParameterName", "")).strip()
        if candidate_name:
            sweep_parameter_name = candidate_name
            break

    for r in filtered:
        sweep_value_by_index.setdefault(r["SweepIndex"], r["SweepValue"])
        values_by_algo_index[(r["Algorithm"], r["SweepIndex"])].append(r["LifetimeSeconds"])

    average_by_algo_index = defaultdict(dict)
    for key, values in values_by_algo_index.items():
        algo, sweep_index = key
        if values:
            average_by_algo_index[algo][sweep_index] = sum(values) / len(values)

    sweep_values = [sweep_value_by_index.get(i, str(i)) for i in sweep_indices]

    return sweep_parameter_name, sweep_indices, sweep_values, algorithms, average_by_algo_index


def draw_line_on_figure(fig, x_axis_label, sweep_indices, sweep_values, algorithms, average_by_algo_index, show_legend=True):
    fig.clear()
    ax = fig.add_subplot(111)

    x_positions = list(range(len(sweep_indices)))

    for algo in algorithms:
        y_values = [average_by_algo_index[algo].get(sweep_index, None) for sweep_index in sweep_indices]
        ax.plot(x_positions, y_values, marker="o", linewidth=1.8, markersize=4.5, label=algo)

    ax.set_title("SweepValue 與平均生命週期")
    ax.set_xlabel(x_axis_label)
    ax.set_ylabel("平均生命週期（秒）")
    ax.set_xticks(x_positions)
    ax.set_xticklabels(sweep_values, rotation=45, ha="right")
    ax.grid(True, alpha=0.3)

    if show_legend:
        ax.legend(loc="center left", bbox_to_anchor=(1.02, 0.5), fontsize=8, borderaxespad=0)
        fig.tight_layout(rect=(0, 0, 0.78, 1))
    else:
        fig.tight_layout()


def draw_paper_style_figure(fig, x_axis_label, sweep_indices, sweep_values, algorithms, average_by_algo_index, show_legend=True):
    """
    另外產生一張接近 CHENG paper Fig. 12 的圖。
    """
    fig.clear()
    ax = fig.add_subplot(111)

    numeric_x = [try_float(v) for v in sweep_values]
    use_numeric_x = all(v is not None for v in numeric_x)

    if use_numeric_x:
        x_values = numeric_x
    else:
        x_values = list(range(len(sweep_indices)))

    styles = build_paper_style(algorithms)

    for algo in algorithms:
        y_values = [average_by_algo_index[algo].get(sweep_index, None) for sweep_index in sweep_indices]
        style = styles[algo]
        ax.plot(
            x_values,
            y_values,
            linestyle=style["linestyle"],
            marker=style["marker"],
            linewidth=1.0,
            markersize=4.0,
            markevery=1,
            label=style["label"],
        )

    ax.set_xlabel(x_axis_label)
    ax.set_ylabel("Network lifetime (s)")

    if use_numeric_x:
        ax.set_xticks(x_values)
        ax.set_xticklabels(["{0:g}".format(v) for v in x_values], rotation=45, ha="right")
    else:
        ax.set_xticks(x_values)
        ax.set_xticklabels(sweep_values, rotation=45, ha="right")

    ax.minorticks_on()
    ax.grid(True, which="major", linestyle=":", linewidth=0.6, alpha=0.75)
    ax.grid(True, which="minor", linestyle=":", linewidth=0.35, alpha=0.45)

    all_y = []
    for algo in algorithms:
        for sweep_index in sweep_indices:
            y = average_by_algo_index[algo].get(sweep_index, None)
            if y is not None and math.isfinite(y):
                all_y.append(y)

    if all_y:
        ymin = min(all_y)
        ymax = max(all_y)
        padding = max((ymax - ymin) * 0.08, 1.0)
        ax.set_ylim(max(0, ymin - padding), ymax + padding)

    if show_legend:
        handles, labels = ax.get_legend_handles_labels()
        seen = set()
        unique_handles = []
        unique_labels = []
        for handle, label in zip(handles, labels):
            if label not in seen:
                seen.add(label)
                unique_handles.append(handle)
                unique_labels.append(label)

        ax.legend(
            unique_handles,
            unique_labels,
            loc="upper center",
            bbox_to_anchor=(0.5, 1.16),
            ncol=min(3, max(1, len(unique_labels))),
            fontsize=8,
            frameon=True,
            fancybox=False,
            edgecolor="black",
        )
        fig.tight_layout(rect=(0, 0, 1, 0.92))
    else:
        fig.tight_layout()


def draw_paper_style_split_legend_figure(fig, x_axis_label, sweep_indices, sweep_values, algorithms, average_by_algo_index, show_legend=True):
    """
    圖表與圖例分開放。
    """
    fig.clear()

    if show_legend:
        grid = fig.add_gridspec(2, 1, height_ratios=[4.2, 1.15], hspace=0.12)
        ax = fig.add_subplot(grid[0, 0])
        legend_ax = fig.add_subplot(grid[1, 0])
        legend_ax.axis("off")
    else:
        ax = fig.add_subplot(111)
        legend_ax = None

    numeric_x = [try_float(v) for v in sweep_values]
    use_numeric_x = all(v is not None for v in numeric_x)

    if use_numeric_x:
        x_values = numeric_x
    else:
        x_values = list(range(len(sweep_indices)))

    styles = build_paper_style(algorithms)

    for algo in algorithms:
        y_values = [average_by_algo_index[algo].get(sweep_index, None) for sweep_index in sweep_indices]
        style = styles[algo]
        ax.plot(
            x_values,
            y_values,
            linestyle=style["linestyle"],
            marker=style["marker"],
            linewidth=1.0,
            markersize=4.0,
            markevery=1,
            label=style["label"],
        )

    ax.set_xlabel(x_axis_label)
    ax.set_ylabel("Network lifetime (s)")

    if use_numeric_x:
        ax.set_xticks(x_values)
        ax.set_xticklabels(["{0:g}".format(v) for v in x_values], rotation=45, ha="right")
    else:
        ax.set_xticks(x_values)
        ax.set_xticklabels(sweep_values, rotation=45, ha="right")

    ax.minorticks_on()
    ax.grid(True, which="major", linestyle=":", linewidth=0.6, alpha=0.75)
    ax.grid(True, which="minor", linestyle=":", linewidth=0.35, alpha=0.45)

    all_y = []
    for algo in algorithms:
        for sweep_index in sweep_indices:
            y = average_by_algo_index[algo].get(sweep_index, None)
            if y is not None and math.isfinite(y):
                all_y.append(y)

    if all_y:
        ymin = min(all_y)
        ymax = max(all_y)
        padding = max((ymax - ymin) * 0.08, 1.0)
        ax.set_ylim(max(0, ymin - padding), ymax + padding)

    if show_legend and legend_ax is not None:
        handles, labels = ax.get_legend_handles_labels()
        seen = set()
        unique_handles = []
        unique_labels = []
        for handle, label in zip(handles, labels):
            if label not in seen:
                seen.add(label)
                unique_handles.append(handle)
                unique_labels.append(label)

        legend_ax.legend(
            unique_handles,
            unique_labels,
            loc="center",
            ncol=min(4, max(1, len(unique_labels))),
            fontsize=8,
            frameon=True,
            fancybox=False,
            edgecolor="black",
        )

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
        self.show_legend_var = tk.BooleanVar(value=True)

        self.latest_chart_data = None

        self.build_ui()

    def build_ui(self):
        root = ttk.Frame(self, padding=12)
        root.pack(fill="both", expand=True)

        title = ttk.Label(root, text="WSN Sweep 平均生命週期折線圖產生器", font=("Microsoft JhengHei UI", 16, "bold"))
        title.pack(anchor="w")

        subtitle = ttk.Label(
            root,
            text="選 Excel、勾選演算法，產生一般折線圖、CHENG 論文風格圖、圖例分離版；x 軸名稱自動讀 SweepParameterName。"
        )
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

        legend_frame = ttk.Frame(algo_frame)
        legend_frame.pack(fill="x", pady=(8, 0))
        ttk.Checkbutton(
            legend_frame,
            text="顯示圖例（取消勾選＝收合圖上的名字）",
            variable=self.show_legend_var,
            command=self.generate_charts_if_ready,
        ).pack(side="left")
        ttk.Button(legend_frame, text="另存論文風格 PNG", command=self.export_paper_png).pack(side="right")
        ttk.Button(legend_frame, text="另存圖例分離 PNG", command=self.export_split_legend_png).pack(side="right", padx=(0, 8))

        output_frame = ttk.LabelFrame(left_panel, text="狀態", padding=10)
        output_frame.pack(fill="both", expand=True, pady=(10, 0))

        ttk.Label(output_frame, textvariable=self.status_var).pack(anchor="w", pady=(0, 4))

        self.log_text = tk.Text(output_frame, height=15, wrap="word")
        self.log_text.pack(fill="both", expand=True)

        self.notebook = ttk.Notebook(right_panel)
        self.notebook.pack(fill="both", expand=True)

        self.line_preview = ChartPreviewFrame(self.notebook, "Sweep 折線圖")
        self.paper_preview = ChartPreviewFrame(self.notebook, "CHENG 論文風格圖")
        self.split_legend_preview = ChartPreviewFrame(self.notebook, "論文風格圖：圖例分離版")

        self.notebook.add(self.line_preview, text="一般折線圖")
        self.notebook.add(self.paper_preview, text="論文風格圖")
        self.notebook.add(self.split_legend_preview, text="圖例分離版")

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

            example_param_name = "p"
            for r in self.records:
                candidate = str(r.get("SweepParameterName", "")).strip()
                if candidate:
                    example_param_name = candidate
                    break

            self.log("讀取完成：{0} 筆資料，{1} 個 Sweep 點，{2} 個演算法。".format(
                len(self.records),
                len(sweep_indices),
                len(algorithms)
            ))
            self.log("偵測到 SweepParameterName：{0}".format(example_param_name))

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

    def generate_charts_if_ready(self):
        if self.records and self.selected_algorithms():
            self.generate_charts(select_tab=False)

    def generate_charts(self, select_tab=True):
        if not self.records:
            messagebox.showwarning("尚未讀取 Excel", "請先選擇並讀取 Excel 檔案。")
            return

        selected = self.selected_algorithms()
        if not selected:
            messagebox.showwarning("尚未選擇演算法", "請至少勾選一個演算法。")
            return

        try:
            self.log("開始產生圖表...")

            sweep_parameter_name, sweep_indices, sweep_values, algorithms, average_by_algo_index = build_sweep_lifetime_tables(
                self.records,
                selected
            )

            if not algorithms:
                raise ValueError("沒有可繪圖的演算法資料。")

            self.latest_chart_data = (
                sweep_parameter_name,
                sweep_indices,
                sweep_values,
                algorithms,
                average_by_algo_index
            )
            show_legend = self.show_legend_var.get()

            draw_line_on_figure(
                self.line_preview.figure,
                sweep_parameter_name,
                sweep_indices,
                sweep_values,
                algorithms,
                average_by_algo_index,
                show_legend=show_legend,
            )

            draw_paper_style_figure(
                self.paper_preview.figure,
                sweep_parameter_name,
                sweep_indices,
                sweep_values,
                algorithms,
                average_by_algo_index,
                show_legend=show_legend,
            )

            draw_paper_style_split_legend_figure(
                self.split_legend_preview.figure,
                sweep_parameter_name,
                sweep_indices,
                sweep_values,
                algorithms,
                average_by_algo_index,
                show_legend=show_legend,
            )

            self.line_preview.refresh()
            self.paper_preview.refresh()
            self.split_legend_preview.refresh()

            self.log("完成：一般折線圖、論文風格圖、圖例分離版已更新在 UI 上。")
            self.log("目前顯示參數：{0}，{1} 個 Sweep 點、{2} 個演算法。".format(
                sweep_parameter_name,
                len(sweep_indices),
                len(algorithms)
            ))

            if select_tab:
                self.notebook.select(self.split_legend_preview)

        except Exception as ex:
            messagebox.showerror("產生失敗", str(ex))
            self.log("產生失敗：" + str(ex))

    def export_paper_png(self):
        if self.latest_chart_data is None:
            if not self.records:
                messagebox.showwarning("尚未讀取 Excel", "請先選擇並讀取 Excel 檔案。")
                return
            self.generate_charts(select_tab=False)

        if self.latest_chart_data is None:
            return

        sweep_parameter_name, sweep_indices, sweep_values, algorithms, average_by_algo_index = self.latest_chart_data
        default_filename = build_export_filename(sweep_parameter_name, sweep_values) + ".png"

        save_path = filedialog.asksaveasfilename(
            title="另存論文風格圖",
            initialfile=default_filename,
            defaultextension=".png",
            filetypes=[("PNG 圖片", "*.png"), ("所有檔案", "*.*")],
        )

        if not save_path:
            return

        try:
            export_fig = Figure(figsize=(4.8, 3.8), dpi=220)
            draw_paper_style_figure(
                export_fig,
                sweep_parameter_name,
                sweep_indices,
                sweep_values,
                algorithms,
                average_by_algo_index,
                show_legend=self.show_legend_var.get(),
            )
            export_fig.savefig(save_path, dpi=220, bbox_inches="tight")
            self.log("已輸出論文風格圖：" + save_path)
        except Exception as ex:
            messagebox.showerror("輸出失敗", str(ex))
            self.log("輸出失敗：" + str(ex))

    def export_split_legend_png(self):
        if self.latest_chart_data is None:
            if not self.records:
                messagebox.showwarning("尚未讀取 Excel", "請先選擇並讀取 Excel 檔案。")
                return
            self.generate_charts(select_tab=False)

        if self.latest_chart_data is None:
            return

        sweep_parameter_name, sweep_indices, sweep_values, algorithms, average_by_algo_index = self.latest_chart_data
        default_filename = build_export_filename(sweep_parameter_name, sweep_values) + ".png"

        save_path = filedialog.asksaveasfilename(
            title="另存圖例分離版",
            initialfile=default_filename,
            defaultextension=".png",
            filetypes=[("PNG 圖片", "*.png"), ("所有檔案", "*.*")],
        )

        if not save_path:
            return

        try:
            algo_count = len(algorithms)
            extra_legend_rows = max(0, math.ceil(algo_count / 4) - 1)
            fig_height = 4.2 + extra_legend_rows * 0.45

            export_fig = Figure(figsize=(5.2, fig_height), dpi=220)
            draw_paper_style_split_legend_figure(
                export_fig,
                sweep_parameter_name,
                sweep_indices,
                sweep_values,
                algorithms,
                average_by_algo_index,
                show_legend=self.show_legend_var.get(),
            )
            export_fig.savefig(save_path, dpi=220, bbox_inches="tight")
            self.log("已輸出圖例分離版：" + save_path)
        except Exception as ex:
            messagebox.showerror("輸出失敗", str(ex))
            self.log("輸出失敗：" + str(ex))


def main():
    setup_chinese_font()
    app = WsnChartGui()
    app.mainloop()


if __name__ == "__main__":
    main()