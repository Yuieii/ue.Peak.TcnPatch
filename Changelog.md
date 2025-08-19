# 1.1.0
## 新增
- 現在會在主畫面的版本號小字後面加上「繁中支援by悠依」的字樣
    - 這個改變可以在設定檔中取消
    - 預設會與「繁中翻譯by: *(翻譯資料作者)*」交替顯示
    - 這個改變可以在設定檔中取消
- 為了支援以上更動，翻譯資料格式也改變了
    - 模組會繼續支援舊的格式
    - GitHub 上的翻譯資料會持續使用舊格式一段時間

# 1.0.3
## 修正
- 修正使用 `Append` 模式時無法加入繁體中文選項（變成兩個英文選項）
    - ~~沒有測試就丟上來了，2ㄏ~~

# 1.0.2
## 新增
- 設定檔新增了可以調整「在語言選項內加入繁體中文選項的方式」的模組設定
- 設定檔新增了可以調整「自動輸出官方參考翻譯時選擇的原始語言」的模組設定
- 現在 [GitHub](https://github.com/Yuieii/ue.Peak.TcnPatch/) 上提供了兩種翻譯資料，可以在模組設定檔案更改翻譯資料來源
    - 預設將採用[接近原始文字的翻譯](https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/TcnTranslations.json)
    - 可以選用[另一個翻譯](https://raw.githubusercontent.com/Yuieii/ue.Peak.TcnPatch/refs/heads/master/TcnTranslations-ue.json)，我有做過比較多更改

# 1.0.1
## 修正
- 修正檔案存取會打架的問題
- 修正寫入新的翻譯時沒有先清空檔案所導致的讀取錯誤

# 1.0.0
## 更新
- 模組在 Thunderstore 上面釋出啦～