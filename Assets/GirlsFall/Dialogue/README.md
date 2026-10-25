> 立ち絵配置・表示枠・マスク・画像調整については、新しい[配置調整ガイド](PortraitLayoutGuide.md)を参照してください。以下の旧配置説明より優先します。

# Girls Fall 簡易ADV：編集・利用ガイド

## 使い始める
TitleSceneの「はじめから」→確認「はい」で既存のGirlsFallGameplayへ移動し、オープニングを開始します。
EditorのPlay開始シーンもTitleSceneに設定しました。現在編集中だった未保存のバックアップシーンは上書きせず保持しています。
今回の接続先では、タイトルの遷移先は以前の仮ステージGirlsFallGameplayでした。CandyTown_EnclosedWorldとは別で、今回マップの統合・置き換えは行っていません。

## プランナー向け：セリフを編集する
1. Playを停止します。
2. Projectで Assets/GirlsFall/Dialogue/Data/OpeningDialogue.asset を選択します。
3. Inspectorの Lines を展開します。表示順が会話順です。
4. 各要素の Text に複数行でセリフを入力します。
5. Expressionのドロップダウンから Normal / Surprised / Crying / Angry / Troubled を選択します。
6. Character Name は空欄なら Default Character Name（初期値「主人公」）を使います。個別に入力して別の話者名にもできます。
7. 追加はリストの「＋」、削除は対象を選択して「−」、並べ替えは左端のハンドルをドラッグします。Unityのリスト表示によってはSize欄でも件数を変更できます。
8. Default Characters Per Second が会話全体の文字/秒です。各要素の Characters Per Second は0なら既定速度、正の値ならその行だけ変更します。
9. Delay Before で本文を表示し始めるまでの待ち時間（秒）を指定します。
10. Playして「はじめから」を選び確認します。セリフの変更にC#編集は不要です。

完了済みの「続きから」ではオープニングが表示されません。「はじめから」は既存進行を初期化する確認付き操作です。テスト用の進行データを利用してください。

## 会話イベントを追加する
Projectの右クリック → Create → Girls Fall → Dialogue → Conversation で会話単位のDialogueDataを作成できます。
EndingDialogueなど別のデータも同じPrefab・表示クラスで利用可能です。
シーン内のDialogueManager.PlayEvent(data, completedCallback)で再生できます。PlayEventはオープニング完了フラグを変更しません。
Ending自体の到達条件・イベント発火処理は今回未実装です。将来の呼び出し元は安全な地点で再生してください。

## 表情画像を差し替える・位置を調整する
Prefab：Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab
Prefab Modeで Presentation → ResponsiveDesign → PortraitFrame を選びます。
CharacterPortraitControllerのExpressions配列で表情とSprite参照、Offset、Scaleを調整します。
共通のPortraitFrameのRectTransformで全体の位置・サイズを調整します。表情切り替えで共通Rectのサイズを自動変更しません。
ExpressionとPreviousExpressionのImageを重ねてフェードします。画面左下、比率維持、透過ありです。
指定PNGの一部には下端に小さな描画片があります。元PNGは変更せずRectMask2DのPaddingで表示下端を少し隠しています。必要ならPortraitFrameのPaddingを調整できます。
Prefab上の変更は利用シーンのインスタンスに反映されます。シーン固有のOverrideがあれば、その値が優先されます。

## 作成物
- Prefab：Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab
- 会話：Assets/GirlsFall/Dialogue/Data/OpeningDialogue.asset（仮セリフ5件、5表情使用）
- UI素材：Assets/GirlsFall/Dialogue/UI/DialogueRounded.png（既存UI素材の複製。ADV用9-slice設定）
- Runtime：CharacterExpression.cs / DialogueData.cs（DialogueLineを含む）/ CharacterPortraitController.cs / DialogueUIController.cs / DialogueManager.cs
- Editor：DialogueSetup.cs / DialogueTests.cs
- バックアップ：Assets/GirlsFall/Dialogue/Backups
- このガイド：Assets/GirlsFall/Dialogue/README.md

## 既存ファイルの変更
- Assets/GirlsFall/Scenes/GirlsFallGameplay.unity：ADV Prefabを追加し、既存Motor・カメラ・ポーズメニュー等の参照を設定。
- Assets/GirlsFall/Runtime/PlayerSaveState.cs：openingDialogueCompleted boolを追加。旧JSONに項目がなければfalse。既存schemaVersion=1の互換性を維持。
- Assets/GirlsFall/Runtime/AutoSaveController.cs：完了フラグのCapture/Restore、完了保存と失敗時のフラグ復元、ADV中の周期保存・プレイ時間カウント停止を追加。保存・復元方式や位置・PV・速度の項目は継続利用。
- Assets/Art/ADV/Heroine/*.png.meta：指定5枚をSprite/Single/Full Rect/Alpha Is Transparency有効に設定。NPOTを維持し、元の解像度・縦横比を復元するため最大サイズ2048。PNG本体は変更なし。
- Assets/GirlsFall/Fonts/GirlsFallJapaneseSDF.asset：新しい日本語セリフ表示に伴い、既存の動的SDFに文字データを追加。
- EditorのPlay開始シーン：TitleScene（既存の未保存シーンを閉じず、タイトルからテストできるように設定）。

プレイヤー操作・カメラスクリプト・CharacterController・Animator・Animation Clip・地形・攻略ルート・Input Managerは変更していません。

## 画像参照
すべて指定された Assets/Art/ADV/Heroine 以下をEditorで割り当て済みです。実行時にファイル名検索は行いません。
- Normal → normal.png
- Surprised → surprised.png
- Crying → crying.png
- Angry → angry.png
- Troubled → troubled.png

## 入力と画面
uGUI / TextMeshPro / CanvasGroup / Scale With Screen Size、基準1920×1080。
16:9と4:3で画像を確認しました。会話UIの共通デザインを画面内に収める縮小を行います。
マウスクリックはAdvanceClickAreaのButtonに一本化し、Updateで同じクリックを読み取りません。
Space・Enter・テンキーEnterにも対応し、同フレームの重複した送り処理を除外します。
入力を受けた最初のフレームは、文字送り中なら全文表示、全文表示後なら次の行へ進みます。
スキップも通常終了と同じ保存処理を使用します。
ADV中は操作用コンポーネントを一時無効化し、元のenabled状態とTime.timeScale・カーソル状態を終了時に復元します。
既存ポーズメニューも一時停止するため、EscでADV中にポーズメニューが割り込むことはありません。

## 進行データ・安全性
新規ゲームでは既存の床へ接地してから会話を表示します。移動・ジャンプ入力は接地準備中も受け付けません。
旧セーブが空中/落下中かつ未完了なら、既存の位置・速度を復元してゲームを継続し、着地後に会話を最初から表示します。空中で停止させたりスタートへ戻したりしません。
完了時にはAutoSaveControllerの最新状態を保存し、ほかの進行項目を初期化しません。
保存失敗時は完了フラグを元に戻し、操作停止を維持したまま「保存を再試行」「保存せずタイトルへ」を表示します。
再試行に成功すれば会話を終了します。保存せずタイトルへ戻った場合は完了が残らず、次回再表示されることを画面に明示します。
既存の単一スロット・原子的書き込み・リビジョンによる古い書き込みの排除を利用しています。
強制終了前に完了保存が確定していなければ、次回会話が再表示される可能性があります。保存不能のまま完了を永続化することはできません。
PlayerPrefsによる独立フラグ、手動セーブ/ロード、過去スロットは追加していません。

## 動作確認
Playモードの自動検証39項目すべて通過：
- タイトルの新規開始確認 → ゲームシーン → 初回ADV
- 指定Sprite5枚、インポート設定、表情切り替えと共通Rect維持
- 文字送り、全文表示、次行送り、同フレーム二重送り抑止
- 操作停止・カーソル表示・終了後復帰
- スキップ/最終行による通常完了と完了フラグ保存
- 保存前後の最新位置・向き・速度・PV・ステージ情報の維持
- 完了済みContinueの非表示、New Gameでの再表示
- 落下中旧セーブの復元と着地後再生
- 実際のI/O失敗による保存エラーと再試行成功
- フラグのない旧JSONの互換性
- 別会話データの再生・終了
- 16:9/4:3の描画画像

自動検証ではUIの実Buttonイベントと既存Motor・セーブクラスを実行し、保存先をLibrary/DialogueTestsの専用領域に切り替えました。既存のユーザー進行データに新規ゲーム操作は行っていません。
750保護対象ファイルを照合し、変更は上記の動的日本語SDFフォント1ファイルのみでした。他の749ファイル（PNG本体・操作/カメラコード・モデル・Clip・Controller・他シーン等）は一致しました。
最終Consoleのエラー・警告は0件です。

未検証：物理マウスによるUIヒット、実際のSpace/Enterキー入力、Inspectorでの手作業によるドラッグ並べ替え、ビルド版、実プロセス強制終了、保存失敗画面からタイトルへ戻るボタンの通し操作、超横長・縦長の全解像度。
Inspectorの追加・削除・並べ替えはUnity標準のシリアライズ可能Listを使用し、編集後のデータ差し替え再生をコードから確認済みです。
エンディングイベント本体は未実装です。ボイス、選択肢、バックログも今回の対象外です。
