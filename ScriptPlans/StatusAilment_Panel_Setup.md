# 状態異常パネルの設定ガイド (9.3修正)

## 概要
状態異常表示システムを、病気とケガを別々のパネルに分けて表示するように変更しました。

## 変更内容

### 旧実装
- `myStatusAilmentTexts[4]`：自分の状態異常を順番に表示
- `oppStatusAilmentTexts[4]`：相手の状態異常を順番に表示

### 新実装（9.3修正）
4つのパネルに分割：

1. **MySicknessPanel**：自分の病気パネル
2. **MyInjuryPanel**：自分のケガパネル
3. **OppSicknessPanel**：相手の病気パネル
4. **OppInjuryPanel**：相手のケガパネル

## Unity Editorでの設定手順

### 1. UIControllerコンポーネントの設定

#### パネルの割り当て
以下の4つのGameObjectをUIControllerの該当フィールドに割り当ててください：

- **mySicknessPanel**：自分の病気を表示するパネル（GameObject）
- **myInjuryPanel**：自分のケガを表示するパネル（GameObject）
- **oppSicknessPanel**：相手の病気を表示するパネル（GameObject）
- **oppInjuryPanel**：相手のケガを表示するパネル（GameObject）

#### テキストフィールドの割り当て
各パネル内に2つのTextMeshProUGUIコンポーネントを作成し、以下のように割り当ててください：

**mySicknessTexts[2]**：
- `[0]`：睡眠時無呼吸症候群の表示用テキスト
- `[1]`：糖尿病の表示用テキスト

**myInjuryTexts[2]**：
- `[0]`：腰痛の表示用テキスト
- `[1]`：熱中症の表示用テキスト

**oppSicknessTexts[2]**：
- `[0]`：相手の睡眠時無呼吸症候群の表示用テキスト
- `[1]`：相手の糖尿病の表示用テキスト

**oppInjuryTexts[2]**：
- `[0]`：相手の腰痛の表示用テキスト
- `[1]`：相手の熱中症の表示用テキスト

## 状態異常の種類とインデックス対応

### 病気（Sickness）
- インデックス 0：睡眠時無呼吸症候群（StatusAilment.SleepApnea）→ 表示テキスト「無呼吸」
- インデックス 1：糖尿病（StatusAilment.Diabetes）→ 表示テキスト「糖尿病」

### ケガ（Injury）
- インデックス 2：腰痛（StatusAilment.BackPain）→ 表示テキスト「腰痛」
- インデックス 3：熱中症（StatusAilment.Heatstroke）→ 表示テキスト「熱中症」

## 動作仕様

### パネルの表示/非表示
- 該当する状態異常がある場合のみ、パネルが表示されます
- 病気がない場合、SicknessPanelは非表示になります
- ケガがない場合、InjuryPanelは非表示になります

### 複数の状態異常
- 同じカテゴリ（病気またはケガ）で複数の状態異常がある場合：
  - 1つ目の状態異常：`texts[0]`に表示
  - 2つ目の状態異常：`texts[1]`に表示
- 例：睡眠時無呼吸症候群と糖尿病の両方にかかった場合
  - `mySicknessTexts[0].text = "無呼吸"`
  - `mySicknessTexts[1].text = "糖尿病"`
  - MySicknessPanelが表示される

### 初期状態
- ゲーム開始時、すべてのパネルは非表示です
- 状態異常が発生すると、該当するパネルが自動的に表示されます

## レイアウト例

```
[自分のエリア]
┌─────────────────┐
│ 病気: 無呼吸    │  ← MySicknessPanel（病気がある場合のみ表示）
└─────────────────┘
┌─────────────────┐
│ ケガ: 腰痛      │  ← MyInjuryPanel（ケガがある場合のみ表示）
└─────────────────┘

[相手のエリア]
┌─────────────────┐
│ 病気: 糖尿病    │  ← OppSicknessPanel（病気がある場合のみ表示）
└─────────────────┘
┌─────────────────┐
│ ケガ: 熱中症    │  ← OppInjuryPanel（ケガがある場合のみ表示）
└─────────────────┘
```

## チェックリスト

### 設定確認
- [ ] MySicknessPanelを作成し、UIControllerに割り当て
- [ ] MyInjuryPanelを作成し、UIControllerに割り当て
- [ ] OppSicknessPanelを作成し、UIControllerに割り当て
- [ ] OppInjuryPanelを作成し、UIControllerに割り当て
- [ ] 各パネル内にTextMeshProUGUIを2つずつ作成
- [ ] mySicknessTexts[2]を正しく割り当て
- [ ] myInjuryTexts[2]を正しく割り当て
- [ ] oppSicknessTexts[2]を正しく割り当て
- [ ] oppInjuryTexts[2]を正しく割り当て

### 動作確認
- [ ] ゲーム開始時、すべてのパネルが非表示になっている
- [ ] 病気発症時、該当するSicknessPanelが表示される
- [ ] ケガ発症時、該当するInjuryPanelが表示される
- [ ] 状態異常が治癒したとき、パネルが非表示になる
- [ ] 複数の状態異常（例：無呼吸と糖尿病）が正しく表示される

## 実装済みのスクリプト

以下のスクリプトは実装済みです：
- `Assets/Scripts/UI/UIController.cs`：状態異常パネル管理

UIの配置・割り当てが完了したら、`UpdateStatusAilmentDisplay()`メソッドが自動的に呼び出され、状態異常が表示されます。
