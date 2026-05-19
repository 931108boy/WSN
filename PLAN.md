# WSN 實驗系統實作計畫

## Summary
在 `C:\Users\931108boy\Desktop\WSN` 內建立實驗系統，使用提供的 MyWSN 程式作為基礎，進行參考 ZHENG single-WCV 架構的延伸實驗；目前 BP&R 部分是 BP&R-inspired bottleneck proactive，不是完整 ZHENG Algorithm 3 重現。

## Key Decisions
- MyWSN 原始碼已搬入 `C:\Users\931108boy\Desktop\WSN\powercontrol`；原始壓縮檔只作為匯入來源，不直接修改。
- BS 固定為 MyWSN sink + 充電中心；WCV 固定單台，每趟 mission 完成後回 BS。
- `Prate_change` 是單次測試固定值；同一 run 先產生固定 rate-change schedule，所有演算法共用，不自動跑 `0, 0.1, 0.2, 0.3`。
- 每個 run 的 map、event、initial residual、routing parent、rate-change schedule 皆由 seed 決定，並以 artifact hash 記錄在 Excel。
- 能耗模型採連續背景耗能 + MyWSN 封包 TX/RX/forward 耗能；每 `10000 s` 以 `Prate_change` 機率套用 `0.875~1.125` 倍耗能率變動。
- 所有演算法以完整 mission 流程服務多個需求，直到達到 `NmaxTask`、WCV 能量限制、沒有可服務需求或安全限制。
- 批次實驗以 run 為單位平行化；同一 run 內不平行化 sensor 狀態更新，Excel 在所有 run 完成後由單一執行緒輸出。
- 大型實驗的任務明細採 deterministic run/algorithm 配額保留，summary 使用完整結果，避免 Excel 寫出階段 OutOfMemory。
- `HighCpu` 啟動檔使用 x64 build，並支援 `MaxParallelJobs` 手動控制平行工作數；0 表示自動使用 CPU 邏輯核心數。

## Implemented Scope
- 新增 XML 設定保存：seed、run count、sensor 數、地圖大小、初始能量、WCV 參數、Treq/百分比 threshold、`Prate_change`、演算法多選、輸出目錄。
- 新增 WinForms「新實驗系統」區塊，可開設定視窗、儲存上次設定、執行批次比較。
- 新增 CLI：
  - `powercontrol.exe --experiment-smoke`
  - `powercontrol.exe --experiment [settings.xml]`
- 預設演算法：EDF、NJF、TADP/LIN、RCSS、NJF+BP&R、FUZZY。
- 可選 simplified wrapper baseline：GENE、PSO、Cuckoo；不是完整移植舊版最佳化流程。
- FUZZY 採單台 WCV 改寫版 Mamdani FLCSD-style：剩餘能量、距離、耗能率、critical node density 輸入，輸出排程優先度。

## Excel Output
輸出繁中 `.xlsx`，包含：
- `參數設定`
- `執行比較`
- `彙總統計`
- `任務明細`
- `死亡原因`

每個 run/演算法記錄 network lifetime、first dead node/time/reason、成功充電數、失敗/逾期數、request 數、移動距離、封包送收/遺失、routing failed 遺失、ParentId=-1 節點數、不連通比例、平均等待時間、充電效率。任務明細記錄 mission、節點順序、request time、deadline、抵達時間、充電前後能量、是否成功、是否 proactive、失敗原因，並同時保留 J、J/s、秒與 nJ/tick 等內部換算欄位。

## Test Plan
- 建置 `C:\Users\931108boy\Desktop\WSN\powercontrol.sln`。
- 執行 smoke test：固定 seed、EDF + FUZZY、2 次 run、短模擬時間。
- 驗證同一 run 中所有演算法共用相同 artifact hash。
- 驗證 `Prate_change` 固定且 rate-change schedule 可重現。
- 驗證任務明細可出現同一 mission 多節點。
- 開啟 Excel 檢查繁中欄位、summary、任務明細與死亡原因完整。
