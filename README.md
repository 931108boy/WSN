# WSN / MyWSN / ZHENG-Inspired Extension Experiment System

本專案是一套 Wireless Rechargeable Sensor Network（WRSN）實驗系統，建置於 `C:\Users\931108boy\Desktop\WSN`。系統以 MyWSN rechargeable 版本的 WinForms 專案為基礎，加入參考 ZHENG single-WCV 架構的動態耗能、完整 BP&R sliding-window bottleneck proactive 機制，以及 FUZZY 排程方法，用來比較多個充電排程演算法在相同實驗條件下的表現。

重要定位：目前參數與實作是「參考 ZHENG single-WCV 架構的延伸實驗」，不是 ZHENG 原始實驗重現；sensor 數量、sensor energy、simulation time、packet/routing 設定都可由本系統自訂。

此版本的重點不是單純展示節點動畫，而是建立一個可以重複、可比較、可輸出 Excel 的實驗平台。每一次 run 都會先產生固定的地圖、事件、初始能量、routing parent 與耗能率變動排程，然後讓所有被選到的演算法使用同一份資料。這樣可以避免不同演算法其實是在不同網路條件下比較的問題。

批次實驗會以 run 為單位平行執行，以使用多核心 CPU；同一個 run 內的 algorithm 與 sensor 狀態推進仍維持序列，以避免破壞模擬時間順序。所有 run 完成後才由單一執行緒合併結果並寫出同一份 Excel。

大型實驗的 `任務明細` 工作表只保留固定上限內的 deterministic run/algorithm 配額資料，以避免 32-bit WinForms 程序在 Excel 寫出階段發生 OutOfMemory；`執行比較` 與 `彙總統計` 仍使用完整模擬結果。

新版雙擊啟動檔會建置/啟動 x64 `HighCpu` 版本。`MaxParallelJobs=0` 表示自動使用 CPU 邏輯核心數；若想更積極使用 CPU，可在 UI 填入較大的平行工作數，若電腦變頓則調低。

---

## 目錄

- [專案位置與檔案結構](#專案位置與檔案結構)
- [系統目標](#系統目標)
- [核心模型](#核心模型)
- [演算法](#演算法)
- [FUZZY 說明](#fuzzy-說明)
- [Prate_change 說明](#prate_change-說明)
- [實驗設定](#實驗設定)
- [執行方法](#執行方法)
- [Excel 輸出](#excel-輸出)
- [驗證方法](#驗證方法)
- [目前已知限制](#目前已知限制)
- [常見問題](#常見問題)

---

## 專案位置與檔案結構

專案根目錄固定為：

```text
C:\Users\931108boy\Desktop\WSN
```

目前主要檔案：

```text
C:\Users\931108boy\Desktop\WSN
├─ README.md                         # 本文件
├─ PLAN.md                           # 實作計畫與目前設計決策
├─ ZHENG.pdf                         # 論文參考資料
├─ YU.pdf                            # 論文參考資料
├─ experiment-last-settings.xml      # UI/CLI 最近一次實驗設定
├─ outputs\                          # 實驗輸出資料夾
│  ├─ *.xlsx                         # 批次比較 Excel 報告
│  └─ ui-qa\                         # 前端視覺 QA 截圖
├─ powercontrol.sln                  # Visual Studio / dotnet build solution
└─ powercontrol\
   ├─ powercontrol.csproj            # .NET Framework 4.8 WinForms 專案
   ├─ Program.cs                     # 程式入口，支援 GUI 與 CLI
   ├─ Form1.cs                       # 原 MyWSN UI + 新實驗系統 UI hook
   ├─ ExperimentSystem.cs            # 新增實驗系統核心
   ├─ common.cs                      # MyWSN 全域參數與既有模型
   ├─ nodemap.cs                     # MyWSN 節點、排程與 routing 相關邏輯
   ├─ mynode.cs                      # MyWSN 節點能耗模型
   ├─ car.cs / charger.cs            # MyWSN 充電車與充電器
   ├─ gene.cs / pso.cs / CuckooSearch.cs
   └─ ...
```

原始 MyWSN 壓縮檔只作為匯入來源，不應直接修改。後續修改都在 `C:\Users\931108boy\Desktop\WSN` 內進行。

---

## 系統目標

本系統要解決的問題是：

> 在相同 WRSN 實驗環境中，比較多個 WCV 充電排程演算法的 network lifetime、充電成功率、等待時間、封包送收狀況、死亡原因與任務細節。

具體目標：

1. 固定同一 run 的實驗資料。
2. 讓所有演算法共享相同 map、event、initial residual、routing parent、rate-change schedule。
3. 不讓 `Prate_change` 自動跑多組值；單次測試只使用一個固定值。
4. 能耗模型同時包含：
   - 連續背景耗能。
   - MyWSN packet TX / RX / forward 耗能。
   - ZHENG-inspired 動態耗能率變動。
5. 所有演算法都走完整 mission 流程，而不是只選單一節點。
6. FUZZY 必須納入實作，不作為延後項目。
7. 產出繁體中文 Excel 報告，方便後續論文整理與實驗比較。

---

## 核心模型

### 網路

- 感測器節點部署在 2D 平面。
- BS（Base Station）固定在 `(0,0)`，同時作為 MyWSN sink 與 WCV 充電中心。
- 節點產生封包事件，封包沿 routing parent 往 BS 傳送。
- 若 routing parent 不存在或中途節點死亡，該封包視為遺失。
- Excel 會另外輸出 `ParentId = -1` 節點數、不連通節點比例，以及因 routing failed 造成的 lost packet 數量，方便區分 routing 連通性與充電排程造成的影響。

### BS

本系統將 BS 視為：

- 資料 sink。
- 充電 request 管理中心。
- WCV dispatch 起點與終點。

### WCV

目前固定單台 WCV。

一趟 mission 流程：

1. WCV 從 BS 出發。
2. 演算法根據目前 request queue 與可能的 proactive candidates 建立 mission route。
3. 一趟 mission 可服務多個節點。
4. 達到以下任一條件時停止加入任務：
   - 已達 `NmaxTask`。
   - WCV 能量不足以服務下一個節點並返回 BS。
   - 沒有可服務需求。
   - 安全限制觸發。
5. mission 結束後 WCV 回到 BS。

例外：`NJF_ROUTE_ZHENG_BPR_EXTENDED` 與 `NJF_ROUTE_YU_BPR_EXTENDED` 是容量放寬實驗版，允許 `cplist` 超過 `NmaxTask`，因此不應混入公平比較；公平比較請使用對應的 `*_LIMITED` 版本。

### 能量單位

內部與輸出同時保留可讀單位與 MyWSN 類型換算：

| 類型 | 單位 |
|---|---|
| 節點能量 | J |
| 內部能量換算 | nJ |
| 耗能率 | J/s |
| tick 耗能率 | nJ/tick |
| tick 長度 | 0.01 s |
| 時間 | seconds |
| 距離 | meters |

### 連續背景耗能

每個節點有一個基礎背景耗能率：

```text
base_rate_J_per_s = InitialEnergyJ / SensorBackgroundLifetimeSeconds
```

節點實際耗能率會乘上目前的 `RateScale`：

```text
consume_rate_J_per_s = base_rate_J_per_s * RateScale
```

### Packet 能耗

封包耗能使用 MyWSN 風格 TX/RX 模型：

```text
RX energy = ER * packet_bits
TX energy = (ER + Eamp * radio_range^power_exponent) * packet_bits
```

其中 Excel 報告同時列出 packet sent / received / lost。

### ZHENG-Inspired 動態耗能

每 `10000 s` 檢查一次是否改變節點耗能率。

對每個 sensor node：

```text
if random <= Prate_change:
    RateScale *= random value in [1 - RateChangeVariationPercent/100,
                                  1 + RateChangeVariationPercent/100]
```

此 schedule 先由 seed 產生，然後所有演算法共享同一份 schedule。
`RateChangeVariationPercent` 預設為 `12.5`，所以預設倍率範圍就是 `0.875 ~ 1.125`。

這裡只保留「固定週期、依機率調整耗能率」這個延伸實驗設定，並未宣稱重現 ZHENG 原始實驗參數。

---

## 演算法

目前預設比較演算法：

| 演算法 | 說明 |
|---|---|
| EDF | Earliest Deadline First，優先服務 deadline 最早的 request |
| NJF | Nearest Job First，依目前 WCV 位置選最近的下一個節點 |
| TADP_LIN | 用 deadline urgency 與距離做線性綜合排序 |
| RCSS | 加入耗能率因素，偏向高風險、高耗能節點 |
| NJF_ZHENG_BPR | ZHENG BP&R deterministic；使用 persistent STable、LatestReportedDeadlineSeconds、BprDeadlineThresholdSeconds、BottleList，BottleList 內以 deadline / NodeId deterministic 選點 |
| NJF_YU_BPR | YU interval BP&R deterministic；使用 persistent STable 建立 YU-inspired request interval / dangerous interval，dangerous interval 內以 deterministic selection 選點；`cplist.Count <= NmaxTask` |
| NJF_ROUTE_ZHENG_BPR_LIMITED | Route + ZHENG BP&R；沿用 ZHENG deadline interval 與 BottleList，只把 BottleList 內選點改成 route insertion cost；`cplist.Count <= NmaxTask` |
| NJF_ROUTE_ZHENG_BPR_EXTENDED | Route + ZHENG BP&R 容量放寬版；同上，但允許 `cplist.Count > NmaxTask` |
| NJF_ROUTE_YU_BPR_LIMITED | Route + YU interval BP&R；使用 YU-inspired request interval / dangerous interval detector，再以 route insertion cost 選點；`cplist.Count <= NmaxTask` |
| NJF_ROUTE_YU_BPR_EXTENDED | Route + YU interval BP&R 容量放寬版；同上，但允許 `cplist.Count > NmaxTask` |
| FUZZY | Mamdani fuzzy inference 排程優先度 |

可選演算法：

| 演算法 | 說明 |
|---|---|
| GENE | Genetic Algorithm route optimization：染色體為任務節點順序，含 EDF/NJF/composite/random 初始族群、tournament selection、ordered crossover、mutation 與 elitism |
| PSO | Random-key Particle Swarm Optimization：每個 task 對應 position/velocity，依 position 排序成 route，使用 inertia/cognitive/social 更新 |
| Cuckoo | Cuckoo Search route optimization：nest 為任務排列，使用 swap/insertion/inversion 擾動與 abandonment probability 淘汰較差 nests |

注意：`NJF` 是沒有 proactive prediction 的 baseline，只等自然 request 並用 nearest-job-first 排路線。`NJF_ZHENG_BPR` 使用 ZHENG-style persistent STable deadline interval；`NJF_YU_BPR` 使用 YU-inspired request interval / dangerous interval detector，並用 deterministic selection 選點；`NJF_ROUTE_ZHENG_BPR_*` 仍使用同一套 ZHENG deadline interval 與 BottleList，只把 BottleList 內選點改成 route insertion cost。`NJF_ROUTE_YU_BPR_*` 使用 YU-inspired request interval / dangerous interval detector，再以 route insertion cost 選點；此版本不是完整 YU WCV+WCD 系統。舊 key `NJF_BPR`、`NJF_BPR_ROUTE_SAFE_LIMITED`、`NJF_BPR_ROUTE_SAFE_EXTENDED` 仍可讀取，會分別對應到新的 ZHENG key。GENE、PSO、Cuckoo 目前已改為正式 route optimization baseline，三者共用同一套 route fitness。

主比較預設 `AllowStandaloneProactiveDispatch=false`：BP&R / YU proactive 只會插入已由 natural request 開啟的 mission，不會在 0 秒、沒有 request 時讓 WCV 主動巡邏。若要研究純 proactive 巡邏，請另外設定 `AllowStandaloneProactiveDispatch=true`，不要混入公平主比較。`*_EXTENDED` 是容量放寬實驗版，也不放進預設主比較清單。

---

## FUZZY 說明

FUZZY 是本系統的重要比較組之一，參考 Tomar et al. FLCSD 類型概念，改寫為單台 WCV、多需求 mission 版本。

### FUZZY 輸入

每個候選節點會計算四個 fuzzy input：

| Input | 意義 |
|---|---|
| Remaining Energy | 節點剩餘能量比例 |
| Distance | WCV 目前位置到節點的距離比例 |
| Energy Consumption Rate | 節點目前耗能率比例 |
| Critical Node Density | 節點附近 critical nodes 的密度 |

### FUZZY 輸出

輸出為：

```text
charging priority
```

priority 越高，越優先排入 mission。

### Membership 概念

目前使用簡化 Mamdani fuzzy inference：

- Remaining Energy:
  - Low
  - Medium
  - High
- Distance:
  - Near
  - Medium
  - Far
- Energy Consumption Rate:
  - Low
  - Medium
  - High
- Critical Node Density:
  - Low
  - Medium
  - High

### 直覺規則

FUZZY 會偏好：

- 剩餘能量低的節點。
- 距離 WCV 近的節點。
- 耗能率高的節點。
- 附近 critical node density 高的節點。

例如：

```text
IF remaining energy is low
AND distance is near
AND energy consumption rate is high
AND critical node density is high
THEN priority is very high
```

目前 FUZZY 是單台 WCV 版本，不做原論文多充電器分區。

---

## Prate_change 說明

`Prate_change` 是每次動態耗能檢查時，單一節點是否改變耗能率的機率。

例如：

```text
Prate_change = 0.2
```

代表每 `10000 s` 檢查一次時，每個節點有 20% 機率改變自己的耗能率。

若觸發改變：

```text
new_rate_scale = old_rate_scale * random(1 - RateChangeVariationPercent/100,
                                         1 + RateChangeVariationPercent/100)
```

若 `RateChangeVariationPercent = 12.5`，耗能率可能降低 12.5%，也可能增加 12.5%。此值可在 UI 的「變動幅度(%)」欄位修改。

重要設計決策：

- 單次實驗只使用一個固定 `Prate_change`。
- `Prate_change` 控制「會不會變」，`RateChangeVariationPercent` 控制「變多少」。
- 不會自動跑 `0, 0.1, 0.2, 0.3` 四組。
- 同一 run 的 `rate-change schedule` 由 seed 固定產生。
- 同一 run 中所有演算法共用完全相同的 `rate-change schedule`。
- Excel 會記錄：
  - `Prate_change`
  - `rate schedule數`
  - `實際套用rate變動數`
  - `artifact hash`

---

## 實驗設定

設定檔位置：

```text
C:\Users\931108boy\Desktop\WSN\experiment-last-settings.xml
```

主要參數：

| 參數 | 說明 | 預設值 |
|---|---|---|
| `BaseSeed` | 基礎 seed，run i 使用 `BaseSeed + i - 1` | 42 |
| `RunCount` | 重複執行次數 | 1 |
| `SensorCount` | 感測器節點數，不含 BS | 200 |
| `MapWidthMeters` / `MapHeightMeters` | 地圖邊長，固定同步成 `n x n` 正方形 | 500 |
| `SimulationTimeSeconds` | 模擬時間 | 50000 |
| `InitialEnergyJ` | 每個 sensor 初始能量 | 500 |
| `SensorBackgroundLifetimeSeconds` | 滿電時只靠背景耗能可存活秒數 | 100000 |
| `InitialResidualJitterPercent` | 初始能量隨機擾動百分比 | 0 |
| `EventRatePerSecond` | 封包事件產生率 | 0.05 |
| `PacketBits` | 封包大小 | 81920 |
| `RadioRangeMeters` | radio range | 60 |
| `ReceiverEnergyNjPerBit` | RX energy | 50 |
| `AmplifierEnergyNjPerBitM2` | TX amplifier energy | 0.01 |
| `PowerExponent` | power distance exponent | 2 |
| `WcvSpeedMetersPerSecond` | WCV 速度 | 5 |
| `WcvChargeRateJPerSecond` | WCV 充電速率 | 5 |
| `WcvCapacityJ` | WCV 能量容量 | 200000 |
| `WcvMoveCostJPerMeter` | WCV 每公尺移動耗能 | 10 |
| `NmaxTask` | 每趟 mission 最多任務數 | 30 |
| `ThresholdMode` | request threshold 模式 | Percent |
| `RequestThresholdPercent` | 低電量 request 門檻百分比 | 10 |
| `TreqSeconds` | Treq 模式使用的剩餘秒數 | 4620 |
| `BprDeadlineThresholdSeconds` | BP&R persistent STable deadline maintenance threshold；rate-change 會重新計算 request-threshold deadline，只有變化達到此秒數才更新 `LatestReportedDeadlineSeconds`；其他 snapshot 欄位仍會更新 | 4620 |
| `AllowStandaloneProactiveDispatch` | 主比較預設 false；false 時 BP&R / YU proactive 只能插入 natural request mission，不會沒有 request 就出車 | false |
| `ProactivePredictionHorizonSeconds` | proactive 預測 horizon；0 表示使用 `TreqSeconds + EstimateBprTjobSeconds(NmaxTask)` | 0 |
| `ProactiveCandidateMaxEnergyRatio` | proactive candidate 最大能量比例；高於或等於此比例的幾乎滿電節點會被排除 | 0.95 |
| `ProactiveCooldownSeconds` | 節點充電完成或剛被 proactive 選入後的 cooldown；0 表示使用 `TreqSeconds` | 0 |
| `YuDangerWindowSeconds` | YU-inspired dangerous interval 掃描視窗；0 表示使用 `EstimateBprTjobSeconds(NmaxTask)` | 0 |
| `YuDangerThresholdK` | YU-inspired dangerous interval 的重疊門檻；0 表示使用 `NmaxTask + 1` | 0 |
| `YuIntervalUncertaintySeconds` | YU-inspired request interval 半寬；0 表示使用 `BprDeadlineThresholdSeconds` | 0 |
| `PrateChange` | 動態耗能變動機率 | 0.2 |
| `RateChangeVariationPercent` | 動態耗能變動幅度百分比，倍率範圍為 `1 ± 此百分比` | 12.5 |
| `SelectedAlgorithmsCsv` | 選擇演算法 | EDF,NJF,TADP_LIN,RCSS,NJF_ZHENG_BPR,NJF_YU_BPR,NJF_ROUTE_ZHENG_BPR_LIMITED,NJF_ROUTE_YU_BPR_LIMITED,FUZZY |
| `OutputDirectory` | Excel 輸出資料夾 | `C:\Users\931108boy\Desktop\WSN\outputs` |

---

## 執行方法

### 方法 1：用 WinForms 圖形介面執行

1. 開啟 PowerShell。
2. 進入專案根目錄：

```powershell
cd C:\Users\931108boy\Desktop\WSN
```

3. 建置專案：

```powershell
dotnet build C:\Users\931108boy\Desktop\WSN\powercontrol.sln
```

4. 啟動程式：

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe
```

5. 在右下角找到「新實驗系統」區塊。

6. 按「設定」可修改：
   - seed
   - run count
   - sensor count
   - map size
   - energy model
   - WCV parameters
   - `Prate_change`
   - algorithm selection
   - output directory

7. 按「儲存」會將設定寫入：

```text
C:\Users\931108boy\Desktop\WSN\experiment-last-settings.xml
```

8. 按「批次比較」開始執行。

9. 完成後會跳出訊息框顯示 Excel 輸出路徑。

### 方法 2：用 CLI 跑目前設定

先建置：

```powershell
dotnet build C:\Users\931108boy\Desktop\WSN\powercontrol.sln
```

使用最近一次設定執行：

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe --experiment
```

指定設定檔執行：

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe --experiment C:\Users\931108boy\Desktop\WSN\experiment-last-settings.xml
```

CLI 執行會輸出類似：

```text
產生共用資料 run 1/1, seed=42
執行 EDF run 1/1
執行 NJF run 1/1
執行 TADP_LIN run 1/1
執行 RCSS run 1/1
執行 NJF_ZHENG_BPR run 1/1
執行 FUZZY run 1/1
Excel 已輸出：C:\Users\931108boy\Desktop\WSN\outputs\...
WORKBOOK=C:\Users\931108boy\Desktop\WSN\outputs\...
```

### 方法 3：跑 smoke test

smoke test 是一個快速驗證用的小型實驗，不會覆蓋 `experiment-last-settings.xml`。

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe --experiment-smoke
```

smoke test 目前設定：

| 參數 | 值 |
|---|---|
| seed | 42 |
| run count | 2 |
| sensor count | 40 |
| map size | 250 x 250 |
| simulation time | 14000 s |
| initial energy | 80 J |
| background lifetime | 12000 s |
| WCV speed | 20 m/s |
| WCV charge rate | 20 J/s |
| NmaxTask | 40 |
| Prate_change | 0.2 |
| algorithms | EDF, FUZZY |

smoke test 用途：

- 確認專案可執行。
- 確認 Excel 可產生。
- 確認同一 run 的 EDF / FUZZY 共用 artifact hash。
- 確認 10000s 後 rate-change 有實際套用。
- 確認 `任務明細` 有多節點 mission。

---

## Excel 輸出

輸出資料夾預設為：

```text
C:\Users\931108boy\Desktop\WSN\outputs
```

檔名格式：

```text
yyyyMMdd-HHmmss-fff-wsn-comparison-seed{BaseSeed}-runs{RunCount}.xlsx
```

例如：

```text
20260518-225207-557-wsn-comparison-seed42-runs1.xlsx
```

### 工作表

| 工作表 | 說明 |
|---|---|
| `參數設定` | 實驗參數、單位換算、BS/WCV/FUZZY/Prate 設計說明、每個 run 的 artifact hash |
| `執行比較` | 每個 run / algorithm 的整體結果 |
| `彙總統計` | 每個 algorithm 的平均值、最大值、最小值 |
| `任務明細` | 每個 mission、每個 task 的詳細記錄 |
| `死亡原因` | 節點死亡時間、原因、是否排程相關 |

### 執行比較欄位

包含：

- run
- seed
- 演算法
- 共用資料 hash
- `Prate_change`
- rate schedule 數
- 實際套用 rate 變動數
- network lifetime
- first dead node
- first dead time
- 死亡原因
- 直接耗能源
- 成功充電數
- 失敗 / 逾期數
- request 數
- proactive 數
- mission 數
- 移動距離
- 封包送出
- 封包收到
- 封包遺失
- routing failed 造成的封包遺失
- ParentId = -1 節點數量
- 不連通節點比例
- 平均等待時間
- 充電效率

### 任務明細欄位

包含：

- run
- seed
- 演算法
- mission id
- task order
- node id
- task source
- proactive
- request time
- deadline
- dispatch time
- arrival time
- wait time
- charge start
- charge end
- energy before / after in J
- energy before / after in nJ
- consume rate in J/s
- consume rate in nJ/tick
- delivered energy
- distance from previous node
- success
- failure reason
- WCV energy after task
- artifact hash

---

## 驗證方法

### 1. 建置驗證

```powershell
dotnet build C:\Users\931108boy\Desktop\WSN\powercontrol.sln
```

成功時應看到：

```text
建置成功。
0 個錯誤
```

### 2. CLI smoke 驗證

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe --experiment-smoke
```

檢查輸出 `.xlsx` 是否產生。

### 3. 前端視覺驗證

啟動 GUI：

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe
```

檢查：

- 右下「新實驗系統」面板是否完整顯示。
- seed / run / nodes / Prate / algorithms 是否和設定檔一致。
- 設定視窗上下捲動是否正常。
- 演算法 checkbox 是否正確勾選。
- 按「批次比較」後是否跳出完成訊息。
- Excel 是否可正常開啟。

### 4. UI 與 CLI 結果一致性驗證

前端跑完後，用同一份設定檔再跑 CLI：

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe --experiment C:\Users\931108boy\Desktop\WSN\experiment-last-settings.xml
```

若 seed、設定與演算法相同，UI 產生的 workbook 與 CLI 產生的 workbook 內容應一致。

目前已做過的 QA 結果：

- 預設 UI 批次與 CLI/internal runner 逐工作表完全一致。
- QA mission 批次與 CLI/internal runner 逐工作表完全一致。
- QA mission workbook 的 `任務明細` 有多節點 mission，並包含 proactive 任務。

---

## 目前已知限制

1. 目前新實驗系統固定單台 WCV。
2. FUZZY 是單台 WCV 改寫版，不做多充電器分區。
3. GENE / PSO / Cuckoo 已在新版 ExperimentSystem 中改為正式 route optimization baseline，但仍是針對目前單 WCV mission route 的最佳化實作，不是 YU WCV+WCD 模型。
4. 新實驗系統是獨立 batch runner，盡量不破壞舊 MyWSN 視覺模擬流程。
5. Excel 目前用內建 OpenXML `.xlsx` 寫出，未額外套用漂亮樣式；重點是資料完整與可重現。
6. 若模擬參數太保守，例如初始能量很高、模擬時間不足，可能不會產生 charging request。這不是錯誤，而是模型條件下節點尚未低電量。

---

## 常見問題

### Q1：為什麼預設執行時 `任務明細` 可能沒有任務？

因為預設設定為：

```text
InitialEnergyJ = 500
SensorBackgroundLifetimeSeconds = 100000
SimulationTimeSeconds = 50000
RequestThresholdPercent = 10
```

在這組設定下，節點可能在模擬結束前還沒低於 request threshold，因此不會產生 charging request。

若要快速看到多節點 mission，可使用：

```powershell
& C:\Users\931108boy\Desktop\WSN\powercontrol\bin\Debug\powercontrol.exe --experiment-smoke
```

或在 UI 設定中降低：

- initial energy
- background lifetime

或提高：

- simulation time
- event rate

### Q2：為什麼 `Prate_change=0.2` 但實際變動數不是固定 20%？

因為每 `10000 s` 對每個節點獨立抽樣一次。`0.2` 是機率，不是保證比例。節點數少時，實際比例可能有明顯波動。

### Q3：如何確認所有演算法真的用同一份資料？

看 Excel 的 `執行比較` 工作表：

- 同一 run。
- 不同 algorithm。
- `共用資料hash` 應相同。

如果同一 run 裡 EDF、NJF、FUZZY 等演算法的 artifact hash 相同，代表它們使用同一份 map/event/residual/rate-change schedule。

### Q4：如何確認 FUZZY 有跑？

看 Excel：

- `執行比較` 中是否有 `FUZZY` row。
- `任務明細` 中是否有 `演算法 = FUZZY`。
- 若有 proactive 任務，`來源` 會顯示 `proactive`，`proactive` 欄位會是 `Y`。

### Q5：如何避免 smoke test 覆蓋正式設定？

`--experiment-smoke` 不會保存到 `experiment-last-settings.xml`。它只會產生測試用 workbook。

正式 UI 或 `--experiment` 執行才會更新最近一次設定。

### Q6：Excel 開著時能不能重新比對檔案？

Excel 可能會鎖住正在開啟的 `.xlsx`。若要用工具直接讀 zip / OpenXML 內容，建議先關閉 Excel 中該 workbook。

---

## 建議實驗流程

正式跑實驗時建議流程：

1. 設定固定 base seed。
2. 設定 run count，例如 30。
3. 設定固定 `Prate_change`，例如 0.2。
4. 選擇要比較的演算法。
5. 按「批次比較」輸出 Excel。
6. 若要比較不同 `Prate_change`，手動改值後再跑一次，不要混在同一份實驗中。
7. 每一份 Excel 代表一個固定 `Prate_change` 條件。
8. 最後再把多份 Excel 做跨參數整理。

建議的 `Prate_change` 實驗組合可以是：

```text
0
0.1
0.2
0.3
```

但這四組應分開執行，因為目前設計要求單次測試固定一個 `Prate_change`。

---

## 重要實作檔案

### `ExperimentSystem.cs`

新增實驗系統核心，包含：

- `ExperimentSettings`
- `ExperimentBatchRunner`
- `ExperimentArtifact`
- `ExperimentSimulation`
- `ExperimentWorkbookWriter`
- `SimpleXlsxWriter`
- `ExperimentSettingsDialog`

### `Program.cs`

支援：

```text
GUI mode
--experiment
--experiment [settings.xml]
--experiment-smoke
```

### `Form1.cs`

在舊 MyWSN UI 右下新增：

- 設定
- 儲存
- 批次比較
- 狀態顯示

---

## 一句話總結

這個專案目前是一個以 MyWSN 為基礎、參考 ZHENG single-WCV 架構並加入 FUZZY 排程的 WRSN 批次延伸實驗平台。它的核心特色是同一 run 所有演算法共用同一份可重現資料，並輸出繁中 Excel，方便後續做論文實驗比較與結果整理。
