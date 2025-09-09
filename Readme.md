# PeakTcnPatch
<!-- shields.io: 為什麼他不給我用 JSON 的 endpoint :( -->
![v1.3.0](https://img.shields.io/badge/v1.3.0-blue)
![支援的 PEAK 版本](https://img.shields.io/badge/dynamic/regex?url=https%3A%2F%2Fraw.githubusercontent.com%2FYuieii%2Fue.Peak.TcnPatch%2Frefs%2Fheads%2Fmaster%2FDocs%2FMetadata.json&search=%22GameVersion%22%3A%20%22(.%2B)%22&replace=%241&label=PEAK&color=red) \
這是悠依的 PEAK 繁體中文化模組～

## 關於模組
模組本身只有讓語言設定增加繁體中文的選項。 \
安裝時會額外安裝繁體中文的翻譯資料。

若啟動時遊戲已經有「繁體中文」的選項，這個模組就沒有任何效用了。 \
（所以如果官方真的有推出繁體中文，那這個模組將會毫無用處）

> 模組設定可以更改是否要下載最新的翻譯資料。 \
> 翻譯資料連結：[這裡](https://github.com/Yuieii/ue.Peak.TcnPatch/blob/master/TcnTranslations.json)

### 如何切換至繁體中文
就有如遊戲本身就支援繁體中文一般，到遊戲設定內選擇「繁體中文」的語言即可。
- 英文介面：主畫面/暫停選單 → `Settings` → `General` → `Language`，下拉選單中選擇「繁體中文」

> 依照模組的設定，繁體中文的選項可以有三種出現的方式：
> 1. 放在簡體中文後面；位於簡體中文和日文之間
> 2. 取代簡體中文的選項
> 3. 放在語言清單的最下面
>
> 三種方式都會各自影響設定的讀取方式：
> 1. 若選擇 **「放在簡體中文後面；位於簡體中文和日文之間」**：
>    - 若已安裝完模組並使用繁體中文選項，移除模組後語言會變為日文
>    - 原本使用日文、韓文、波蘭語、土耳其語的使用者會受此影響（但應該這裡沒有人會用吧）
> 2. 若選擇 **「取代簡體中文的選項」**：
>    - 若已安裝完模組並使用繁體中文選項，移除模組後語言會變為簡體中文
> 3. 若選擇 **「放在語言清單的最下面」**：
>    - 若已安裝完模組並使用繁體中文選項，移除模組後語言會變為英文
>
> 以上三種讀取方式透過設定更改後重啟之後，也會有語言設定跑掉的情況發生，屬於正常現象。

## 關於翻譯資料
您可以自己選擇想要使用的翻譯資料！ \
請在安裝完模組後至少開過一次 PEAK，之後在模組設定檔 `BepInEx/config/ue.Peak.TcnPatch.cfg` 更改翻譯資料的來源。

若翻譯資料有問題歡迎到 [GitHub](https://github.com/Yuieii/ue.Peak.TcnPatch/issues) 提出～

---

目前這裡提供數種翻譯資料：

### 官方簡中 → 繁中
`https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/TcnTranslations.json`
- 由官方簡體中文翻譯成繁體中文，附加一點翻譯修正。
  - 在原文無錯誤的情況下盡可能保留原文

### 官方簡中 → 繁中 (ue版)
`https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/TcnTranslations-ue.json`
- 由官方簡體中文翻譯成繁體中文，附加更多翻譯修改。
  - 可能會有與簡中原文不同的翻譯或措辭

### 夜芷冰繁體中文
`https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/Translations/Vocaloid2048.json`
- 由夜芷冰從英文翻譯成繁體中文。
  - 如果您比較習慣夜芷冰的翻譯的話，可以使用這個翻譯資料 
  - 不定時從[上游](https://github.com/Vocaloid2048/PEAK-zh-tw-Translation)更新

歡迎提供更多翻譯資料！或者您也可以自己 fork 一份自己的翻譯資料！ \
~~想要弄成文言文當然是沒問題的！（比讚）~~
