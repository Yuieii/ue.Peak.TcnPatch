<img alt="Icon" width="150" src="https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/Docs/Icon.png" />

# PeakTcnPatch 
<!-- shields.io: 為什麼他不給我用 JSON 的 endpoint :( -->
<!-- @placeholder/version -->
![支援的 PEAK 版本](https://img.shields.io/badge/dynamic/regex?url=https%3A%2F%2Fraw.githubusercontent.com%2FYuieii%2Fue.Peak.TcnPatch%2Frefs%2Fheads%2Fmaster%2FDocs%2FMetadata.json&search=%22GameVersion%22%3A%20%22(.%2B)%22&replace=%241&label=PEAK&color=red) \
這是悠依的 PEAK 繁體中文化模組～

## 關於模組
模組本身只有修改繁體中文的翻譯文本， \
以及為了增強 PEAK 對繁體中文的支持所做的變更。 \
安裝時會額外安裝繁體中文的翻譯資料。

> [!IMPORTANT]
> 模組設定可以更改是否要下載最新的翻譯資料。 <!-- @start/thunderstore-only {{\
> 翻譯資料連結：[這裡](https://github.com/Yuieii/ue.Peak.TcnPatch/blob/master/TcnTranslations.json) }} @end/thunderstore-only --><!-- @start/thunderstore-omit -->\
> 翻譯資料格式請參考[這裡](https://github.com/Yuieii/ue.Peak.TcnPatch/blob/master/TcnTranslations.json)。 <!-- @end/thunderstore-omit -->

## 關於翻譯資料
您可以自己選擇想要使用的翻譯資料！ \
請在安裝完模組後至少開過一次 PEAK，之後在模組設定檔 `BepInEx/config/ue.Peak.TcnPatch.cfg` 更改翻譯資料的來源。

若翻譯資料有問題歡迎到 [GitHub](https://github.com/Yuieii/ue.Peak.TcnPatch/issues) 提出～

## 給模組包 (modpack) 作者們
如果想要自己增加翻譯資料的話，請務必：
- 將 `ue.Peak.TcnPatch.cfg` 的 `DownloadFromRemote` 設定為 `false`，並自行更改 `TcnTranslations.json` 檔案
- 或者，自己利用其他網站或服務上傳你自己的 `TcnTranslations.json`，然後將 `ue.Peak.TcnPatch.cfg` 的 `DownloadUrl` 設定為您的網址
  - 舉例來說，可以上傳至 GitHub 並將設定檔的下載 URL 更改為對應的網址（請參考本模組的預設設定）

---

目前這裡提供數種翻譯資料：

### 官方繁中 + 官方簡中 + ue繁中
`https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/TcnTranslations.json`
- 目前預設提供的翻譯資料
- 由官方的繁體中文結合部分先前的官方簡體中文翻譯成繁體中文，附加一點翻譯修正。
  - 可能會有與簡中、繁中原文不同的措辭
  - 舊有生態域名稱、部分物品名稱採用簡中翻譯，提供給已經習慣簡中名稱的大家

### 夜芷冰繁體中文
`https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/Translations/Vocaloid2048.json`
- 由夜芷冰從英文翻譯成繁體中文。
  - 如果您比較習慣夜芷冰的翻譯的話，可以使用這個翻譯資料 
  - 不定時從[上游](https://github.com/Vocaloid2048/PEAK-zh-tw-Translation)更新

歡迎提供更多翻譯資料！或者您也可以自己 fork 一份自己的翻譯資料！ \
~~想要弄成文言文當然是沒問題的！（比讚）~~
