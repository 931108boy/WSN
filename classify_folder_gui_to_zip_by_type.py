# -*- coding: utf-8 -*-
import os
import re
import csv
import io
import zipfile
import tempfile
import shutil
import threading
import queue
import tkinter as tk
from tkinter import filedialog, messagebox
from tkinter import ttk

OUTPUT_FOLDER_NAME = "scheduler_zips_by_type"

FILE_TYPES = [
    "summary",
    "mission-details",
    "task-records",
    "bpr-debug",
    "yu-bpr-debug",
]


def safe_name(name):
    bad_chars = r'\\/:*?"<>|'
    for ch in bad_chars:
        name = name.replace(ch, "_")
    return name


def normalize_algorithm(alg):
    if alg.endswith("-yu"):
        alg = alg[:-3]
    return alg


def extract_algorithm_from_filename(filename):
    base = os.path.basename(filename)
    patterns = [
        r"-seed\d+-(.+)-yu-bpr-debug\.csv$",
        r"-seed\d+-(.+)-bpr-debug\.csv$",
        r"-seed\d+-(.+)-mission-details\.csv$",
        r"-seed\d+-(.+)-task-records\.csv$",
    ]
    for p in patterns:
        m = re.search(p, base)
        if m:
            return normalize_algorithm(m.group(1))
    return None


def classify_file_type(filename):
    if filename.endswith("-mission-details.csv"):
        return "mission-details"
    if filename.endswith("-task-records.csv"):
        return "task-records"
    if filename.endswith("-yu-bpr-debug.csv"):
        return "yu-bpr-debug"
    if filename.endswith("-bpr-debug.csv"):
        return "bpr-debug"
    return "other"


def find_task_details_folder(root_folder):
    direct = os.path.join(root_folder, "task-details")
    if os.path.exists(direct):
        return direct
    for dirpath, dirnames, filenames in os.walk(root_folder):
        if os.path.basename(dirpath) == "task-details":
            return dirpath
    return None


def prepare_input_path(input_path):
    temp_dir = None
    if os.path.isdir(input_path):
        return find_task_details_folder(input_path), temp_dir
    if os.path.isfile(input_path) and input_path.lower().endswith(".zip"):
        temp_dir = tempfile.mkdtemp(prefix="wsn_result_")
        with zipfile.ZipFile(input_path, "r") as z:
            z.extractall(temp_dir)
        return find_task_details_folder(temp_dir), temp_dir
    return None, temp_dir


def collect_algorithm_files_by_type(task_dir):
    # result[alg][file_type] = [path, ...]
    result = {}
    for dirpath, dirnames, filenames in os.walk(task_dir):
        for filename in filenames:
            if not filename.lower().endswith(".csv"):
                continue
            if filename == "summary.csv":
                continue
            alg = extract_algorithm_from_filename(filename)
            if alg is None:
                continue
            file_type = classify_file_type(filename)
            if file_type == "other":
                continue
            src = os.path.join(dirpath, filename)
            result.setdefault(alg, {}).setdefault(file_type, []).append(src)
    return result


def read_summary_by_algorithm(task_dir):
    summary_path = os.path.join(task_dir, "summary.csv")
    if not os.path.exists(summary_path):
        return None, {}
    summary_groups = {}
    with open(summary_path, "r", encoding="utf-8-sig", newline="") as f:
        reader = csv.DictReader(f)
        fieldnames = reader.fieldnames
        if fieldnames is None:
            return None, {}
        for row in reader:
            alg = normalize_algorithm(row.get("Algorithm", ""))
            if alg == "":
                continue
            summary_groups.setdefault(alg, []).append(row)
    return fieldnames, summary_groups


def make_csv_text(fieldnames, rows):
    output = io.StringIO()
    writer = csv.DictWriter(output, fieldnames=fieldnames)
    writer.writeheader()
    writer.writerows(rows)
    return "\ufeff" + output.getvalue()


def make_file_index_text(alg, file_type, files):
    output = io.StringIO()
    fieldnames = ["Algorithm", "FileType", "FileName", "SourcePath"]
    writer = csv.DictWriter(output, fieldnames=fieldnames)
    writer.writeheader()
    for src in files:
        writer.writerow({
            "Algorithm": alg,
            "FileType": file_type,
            "FileName": os.path.basename(src),
            "SourcePath": src,
        })
    return "\ufeff" + output.getvalue()


def write_algorithm_type_zip(zip_path, alg, file_type, files, summary_fieldnames=None, summary_rows=None):
    with zipfile.ZipFile(zip_path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=1) as z:
        z.writestr("file_index.csv", make_file_index_text(alg, file_type, files))
        if file_type == "summary" and summary_fieldnames is not None and summary_rows is not None:
            z.writestr("summary_" + safe_name(alg) + ".csv", make_csv_text(summary_fieldnames, summary_rows))
        else:
            for src in files:
                z.write(src, os.path.basename(src))


def worker_make_zips(input_path, output_parent, q):
    temp_dir = None
    try:
        q.put(("status", "正在讀取輸入資料..."))
        task_dir, temp_dir = prepare_input_path(input_path)
        if task_dir is None:
            raise Exception("找不到 task-details 資料夾。請確認你選的是模擬結果資料夾或模擬結果 zip。")

        output_dir = os.path.join(output_parent, OUTPUT_FOLDER_NAME)
        if os.path.exists(output_dir):
            shutil.rmtree(output_dir)
        os.makedirs(output_dir)

        q.put(("status", "正在掃描各排程檔案..."))
        alg_files_by_type = collect_algorithm_files_by_type(task_dir)
        summary_fieldnames, summary_groups = read_summary_by_algorithm(task_dir)

        all_algorithms = set(alg_files_by_type.keys()) | set(summary_groups.keys())
        if not all_algorithms:
            raise Exception("沒有找到任何排程演算法檔案。")

        # 建立工作清單：(alg, file_type, files, summary_rows)
        jobs = []
        for alg in sorted(all_algorithms):
            if alg in summary_groups:
                jobs.append((alg, "summary", [], summary_groups[alg]))
            for file_type in FILE_TYPES:
                if file_type == "summary":
                    continue
                files = alg_files_by_type.get(alg, {}).get(file_type, [])
                if files:
                    jobs.append((alg, file_type, files, None))

        total_jobs = max(1, len(jobs))
        created_zips = []

        for idx, (alg, file_type, files, summary_rows) in enumerate(jobs, start=1):
            alg_safe = safe_name(alg)
            type_safe = safe_name(file_type)
            zip_name = alg_safe + "__" + type_safe + ".zip"
            zip_path = os.path.join(output_dir, zip_name)
            if os.path.exists(zip_path):
                os.remove(zip_path)

            q.put(("status", "正在產生：" + zip_name))
            write_algorithm_type_zip(
                zip_path,
                alg,
                file_type,
                files,
                summary_fieldnames if file_type == "summary" else None,
                summary_rows if file_type == "summary" else None,
            )
            created_zips.append(zip_path)
            q.put(("progress", idx, total_jobs, "完成：" + zip_name))

        q.put(("done", output_dir, len(created_zips)))
    except Exception as e:
        q.put(("error", str(e)))
    finally:
        if temp_dir is not None and os.path.exists(temp_dir):
            shutil.rmtree(temp_dir)


def choose_input_path(root):
    result = messagebox.askyesnocancel(
        "選擇輸入來源",
        "請選擇輸入來源：\n\n"
        "按「是」：選擇模擬結果資料夾\n"
        "按「否」：選擇模擬結果 zip\n"
        "按「取消」：離開"
    )
    if result is None:
        return None
    if result is True:
        return filedialog.askdirectory(title="選擇模擬結果資料夾")
    return filedialog.askopenfilename(title="選擇模擬結果 zip", filetypes=[("ZIP files", "*.zip")])


def main():
    root = tk.Tk()
    root.withdraw()

    input_path = choose_input_path(root)
    if not input_path:
        messagebox.showwarning("取消", "你沒有選擇輸入來源。")
        root.destroy()
        return

    messagebox.showinfo("選擇輸出位置", "請選擇各類型 zip 要輸出的資料夾。")
    output_parent = filedialog.askdirectory(title="選擇輸出資料夾")
    if not output_parent:
        messagebox.showwarning("取消", "你沒有選擇輸出資料夾。")
        root.destroy()
        return

    root.deiconify()
    root.title("各排程 × 檔案類型 zip 產生中")
    root.geometry("620x170")
    root.resizable(False, False)

    label = tk.Label(root, text="準備中...", anchor="w")
    label.pack(fill="x", padx=20, pady=(20, 10))

    progress = ttk.Progressbar(root, orient="horizontal", length=560, mode="determinate")
    progress.pack(padx=20, pady=10)

    percent_label = tk.Label(root, text="0%")
    percent_label.pack(pady=(0, 10))

    q = queue.Queue()
    t = threading.Thread(target=worker_make_zips, args=(input_path, output_parent, q))
    t.daemon = True
    t.start()

    def check_queue():
        try:
            while True:
                msg = q.get_nowait()
                if msg[0] == "status":
                    label.config(text=msg[1])
                elif msg[0] == "progress":
                    done = msg[1]
                    total = msg[2]
                    text = msg[3]
                    value = int(done * 100 / total)
                    progress["value"] = value
                    label.config(text=text)
                    percent_label.config(text=str(value) + "%")
                elif msg[0] == "done":
                    output_dir = msg[1]
                    count = msg[2]
                    progress["value"] = 100
                    percent_label.config(text="100%")
                    label.config(text="完成")
                    messagebox.showinfo("完成", "完成。\n\n輸出資料夾：\n" + output_dir + "\n\n共產生 " + str(count) + " 個 zip。")
                    root.destroy()
                    return
                elif msg[0] == "error":
                    messagebox.showerror("執行錯誤", msg[1])
                    root.destroy()
                    return
        except queue.Empty:
            pass
        root.after(200, check_queue)

    root.after(200, check_queue)
    root.mainloop()


if __name__ == "__main__":
    main()
