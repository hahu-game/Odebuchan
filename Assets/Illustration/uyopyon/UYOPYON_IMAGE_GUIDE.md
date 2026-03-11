# うーぴょん画像適用ガイド

このドキュメントは、`Assets/Illustration/uyopyon` フォルダ内の画像がゲーム内でどのような場面で適用されるかを整理したものです。

---

## 基本ルール

### 進化状態による分類
- **before**: 進化前（体重が進化条件の値に達していないとき）
- **after**: 進化後（体重が進化条件の値に達したとき）

### 画像の種類
- **normal**: デフォルト画像。行動を行っていないときや、行動完了後に戻る画像
- **abnormal**: ケガ・病気状態のときのデフォルト画像。normal の代わりに表示される
- **progress**: 行動実行中の画像
- **complete**: 行動完了時の画像

### 画像表示時間
- 行動の画像遷移は**合計2秒**で完了する
- 画像が複数枚ある場合は、2秒を均等に割り振る
- 行動完了後は normal（またはケガ・病気時は abnormal）に戻る

---

## 進化前（before）画像一覧：14枚

### デフォルト画像

| ファイル名 | 用途 | 表示条件 |
|-----------|------|---------|
| 1_uyopyon_before_normal.png | デフォルト画像 | 行動を行っていないとき、行動完了後（健康時） |
| 2_uyopyon_before_abnormal.png | ケガ・病気時画像 | ケガまたは病気状態のとき（normalの代わり） |

### たべる（Eat）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 3_uyopyon_before_Eat_progress.png | 食べている途中 | 1.0秒 |
| 4_uyopyon_before_Eat_complete.png | 食べ終わり | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### ねむる（Sleep）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 5_uyopyon_before_Sleep_progress.png | 眠っている途中 | 1.0秒 |
| 6_uyopyon_before_Sleep_complete.png | 眠り終わり | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### あそぶ（Play）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 7_uyopyon_before_Play_progress_1.png | 遊んでいる途中1 | 0.67秒 |
| 7_uyopyon_before_Play_progress_2.png | 遊んでいる途中2 | 0.67秒 |
| 8_uyopyon_before_Play_complete.png | 遊び終わり | 0.67秒 |

**遷移フロー**: 効果音再生 → progress_1(0.67秒) → progress_2(0.67秒) → complete(0.67秒) → normal/abnormalに戻る

### つういん（Clinic）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 9_uyopyon_before_Clinic_progress_1.png | 通院中1 | 0.4秒 |
| 9_uyopyon_before_Clinic_progress_2.png | 通院中2 | 0.4秒 |
| 10_uyopyon_before_Clinic_complete_1.png | 通院完了1 | 0.4秒 |
| 10_uyopyon_before_Clinic_complete_2.png | 通院完了2 | 0.4秒 |
| 10_uyopyon_before_Clinic_complete_3.png | 通院完了3 | 0.4秒 |

**遷移フロー**: 効果音再生 → progress_1(0.4秒) → progress_2(0.4秒) → complete_1(0.4秒) → complete_2(0.4秒) → complete_3(0.4秒) → normal/abnormalに戻る

---

## 進化後（after）画像一覧：24枚

### デフォルト画像

| ファイル名 | 用途 | 表示条件 |
|-----------|------|---------|
| 11_uyopyon_after_normal.png | デフォルト画像 | 行動を行っていないとき、行動完了後（健康時） |
| 12_uyopyon_after_abnormal.png | ケガ・病気時画像 | ケガまたは病気状態のとき（normalの代わり） |

### たべる（Eat）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 13_uyopyon_after_Eat_progress.png | 食べている途中 | 1.0秒 |
| 14_uyopyon_after_Eat_complete.png | 食べ終わり | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### ねむる（Sleep）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 15_uyopyon_after_Sleep_progress.png | 眠っている途中 | 1.0秒 |
| 16_uyopyon_after_Sleep_complete.png | 眠り終わり | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### あそぶ（Play）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 17_uyopyon_after_Play_progress_1.png | 遊んでいる途中1 | 0.67秒 |
| 17_uyopyon_after_Play_progress_2.png | 遊んでいる途中2 | 0.67秒 |
| 18_uyopyon_after_Play_complete.png | 遊び終わり | 0.67秒 |

**遷移フロー**: 効果音再生 → progress_1(0.67秒) → progress_2(0.67秒) → complete(0.67秒) → normal/abnormalに戻る

### つういん（Clinic）

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 19_uyopyon_after_Clinic_progress_1.png | 通院中1 | 0.67秒 |
| 19_uyopyon_after_Clinic_progress_2.png | 通院中2 | 0.67秒 |
| 20_uyopyon_after_Clinic_complete.png | 通院完了 | 0.67秒 |

**遷移フロー**: 効果音再生 → progress_1(0.67秒) → progress_2(0.67秒) → complete(0.67秒) → normal/abnormalに戻る

### がいしょく（Gaishoku）※進化後専用

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 21_uyopyon_after_Gaishoku_progress.png | 外食中 | 1.0秒 |
| 22_uyopyon_after_Gaishoku_complete.png | 外食完了 | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### きんとれ（Kintre）※進化後専用

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 23_uyopyon_after_Kintre_progress.png | きんとれ中 | 1.0秒 |
| 24_uyopyon_after_Kintre_complete.png | きんとれ完了 | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### がむしゃら（Gamushara）※進化後専用

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 25_uyopyon_after_Gamushara_progress.png | がむしゃら中 | 1.0秒 |
| 26_uyopyon_after_Gamushara_complete.png | がむしゃら完了 | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### べんきょう（Benkyou）※進化後専用

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 27_uyopyon_after_Benkyou_progress.png | 勉強中 | 1.0秒 |
| 28_uyopyon_after_Benkyou_complete.png | 勉強完了 | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### じゅくすい（Jukusui）※進化後専用

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 29_uyopyon_after_Jukusui_progress.png | じゅくすい中 | 1.0秒 |
| 30_uyopyon_after_Jukusui_complete.png | じゅくすい完了 | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

### どかぐい（Dokagui）※進化後専用

| ファイル名 | 用途 | 表示時間 |
|-----------|------|---------|
| 31_uyopyon_after_Dokagui_progress.png | どかぐい中 | 1.0秒 |
| 32_uyopyon_after_Dokagui_complete.png | どかぐい完了 | 1.0秒 |

**遷移フロー**: 効果音再生 → progress(1秒) → complete(1秒) → normal/abnormalに戻る

---

## リザルト画面専用画像一覧：2枚

これらの画像は他の画像と異なり、Uyopyon オブジェクトの画像を切り替える処理では使用しない。
リザルト画面にそれぞれ専用のオブジェクトを配置し、表示・非表示を切り替えることで実装する。

| ファイル名 | 用途 | 表示条件 |
|-----------|------|---------|
| 33_uyopyon_win.png | 勝利画像 | リザルト画面で勝利したプレイヤーの名前の横に表示 |
| 34_uyopyon_lose.png | 敗北画像 | リザルト画面で敗北したプレイヤーの名前の横に表示 |

### 実装方針

- Uyopyon オブジェクトの Image を差し替えるのではなく、各画像に対応した**専用の Image オブジェクト**をリザルト画面に配置する
- 勝利プレイヤーの名前表示欄の横に `33_uyopyon_win` を割り当てたオブジェクト、敗北プレイヤーの名前表示欄の横に `34_uyopyon_lose` を割り当てたオブジェクトを置く
- リザルト表示時にスクリプトから該当オブジェクトを `SetActive(true/false)` で制御する

---

## 行動と画像の対応サマリー

| 行動 | 進化前 | 進化後 | 備考 |
|------|--------|--------|------|
| たべる (Eat) | ○ | ○ | 共通行動 |
| ねむる (Sleep) | ○ | ○ | 共通行動 |
| あそぶ (Play) | ○ | ○ | 共通行動 |
| つういん (Clinic) | ○ | ○ | 共通行動 |
| がいしょく (Gaishoku) | - | ○ | 進化後専用 |
| きんとれ (Kintre) | - | ○ | 進化後専用 |
| がむしゃら (Gamushara) | - | ○ | 進化後専用 |
| べんきょう (Benkyou) | - | ○ | 進化後専用 |
| じゅくすい (Jukusui) | - | ○ | 進化後専用 |
| どかぐい (Dokagui) | - | ○ | 進化後専用 |
| 勝利（Win） | - | - | リザルト画面専用（専用オブジェクトで表示） |
| 敗北（Lose） | - | - | リザルト画面専用（専用オブジェクトで表示） |

---

## 実装時の注意点

1. **画像遷移の総時間は必ず2秒**とする
2. 画像枚数に応じて表示時間を均等に割り振る（例：5枚なら各0.4秒）
3. 行動完了後は、ケガ・病気状態なら `abnormal` に、そうでなければ `normal` に戻る
4. 進化後専用の行動は進化前では使用不可（対応する画像が存在しない）
5. 効果音は画像遷移開始と同時に再生する

---

## Unityでの設定手順

### 1. UyopyonImageData ScriptableObjectの作成

1. Projectウィンドウで右クリック → **Create** → **Odebuchan** → **Uyopyon Image Data**
2. 作成したアセットに名前を付ける（例: `UyopyonImageData`）
3. Inspectorで以下の画像をアサイン:

#### 進化前（Before）デフォルト画像
| フィールド | ファイル |
|-----------|---------|
| Before Normal | 1_uyopyon_before_normal.png |
| Before Abnormal | 2_uyopyon_before_abnormal.png |

#### 進化後（After）デフォルト画像
| フィールド | ファイル |
|-----------|---------|
| After Normal | 11_uyopyon_after_normal.png |
| After Abnormal | 12_uyopyon_after_abnormal.png |

#### 進化前（Before）行動画像
各ActionImageSetで、Progress SpritesとComplete Spritesに画像を順番にアサイン:

- **Before Eat**: 3_uyopyon_before_Eat_progress → 4_uyopyon_before_Eat_complete
- **Before Sleep**: 5_uyopyon_before_Sleep_progress → 6_uyopyon_before_Sleep_complete
- **Before Play**: 7_uyopyon_before_Play_progress_1 → 7_uyopyon_before_Play_progress_2 → 8_uyopyon_before_Play_complete
- **Before Clinic**: 9_uyopyon_before_Clinic_progress_1 → 9_uyopyon_before_Clinic_progress_2 → 10_uyopyon_before_Clinic_complete_1 → 10_uyopyon_before_Clinic_complete_2 → 10_uyopyon_before_Clinic_complete_3

#### 進化後（After）基本行動画像
- **After Eat**: 13_uyopyon_after_Eat_progress → 14_uyopyon_after_Eat_complete
- **After Sleep**: 15_uyopyon_after_Sleep_progress → 16_uyopyon_after_Sleep_complete
- **After Play**: 17_uyopyon_after_Play_progress_1 → 17_uyopyon_after_Play_progress_2 → 18_uyopyon_after_Play_complete
- **After Clinic**: 19_uyopyon_after_Clinic_progress_1 → 19_uyopyon_after_Clinic_progress_2 → 20_uyopyon_after_Clinic_complete

#### 進化後（After）特殊能力画像
- **After Gaishoku**: 21_uyopyon_after_Gaishoku_progress → 22_uyopyon_after_Gaishoku_complete
- **After Kintre**: 23_uyopyon_after_Kintre_progress → 24_uyopyon_after_Kintre_complete
- **After Gamushara**: 25_uyopyon_after_Gamushara_progress → 26_uyopyon_after_Gamushara_complete
- **After Benkyou**: 27_uyopyon_after_Benkyou_progress → 28_uyopyon_after_Benkyou_complete
- **After Jukusui**: 29_uyopyon_after_Jukusui_progress → 30_uyopyon_after_Jukusui_complete
- **After Dokagui**: 31_uyopyon_after_Dokagui_progress → 32_uyopyon_after_Dokagui_complete

### 2. UyopyonPrefabへのImageコンポーネント追加

1. `Assets/Prefabs/UyopyonPrefab`を開く
2. UyopyonPrefab内にImageコンポーネントを持つ子オブジェクトを追加（または既存のImageを使用）
3. UyopyonStateコンポーネントのInspectorで:
   - **Uyopyon Image**: 追加したImageコンポーネントをアサイン

### 3. UIControllerへの設定

1. GameSceneのUIController GameObjectを選択
2. Inspectorで以下を設定:
   - **Uyopyon Image Data**: 手順1で作成したScriptableObjectをアサイン

※ うーぴょん画像のImage参照は、ゲーム開始時にUyopyonPrefabがSpawnされた際に自動的にUIControllerに登録されます。

### 4. 動作確認

- **選択フェーズ**: デフォルト画像（normal）が表示される
- **行動フェーズ**: 行動実行時に画像が順番に切り替わり、2秒後にデフォルト画像に戻る
- **状態異常時**: abnormal画像がデフォルトとして表示される
- **進化後**: 進化後用の画像セットが使用される
