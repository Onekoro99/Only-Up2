# Inspector 日本語Tooltip対応報告

## 対応結果
自作Runtimeスクリプト22ファイル・25種類のInspector用データ型にある173項目へ日本語Tooltipを設定しました。
新規追加137項目、既存説明の更新36項目です。
参照・配列・リストと、その要素の会話データ・表情設定・ルート目標も含めています。
変数名、型、初期値、ゲーム処理、SerializeField参照は変更していません。

## 対象スクリプトと項目数
|スクリプト|項目数|
|---|---:|
| `Assets/GirlsFall/Dialogue/Runtime/CharacterPortraitController.cs` | 9 |
| `Assets/GirlsFall/Dialogue/Runtime/CharacterPortraitLayout.cs` | 6 |
| `Assets/GirlsFall/Dialogue/Runtime/DialogueData.cs` | 8 |
| `Assets/GirlsFall/Dialogue/Runtime/DialogueManager.cs` | 7 |
| `Assets/GirlsFall/Dialogue/Runtime/DialogueTextLayout.cs` | 23 |
| `Assets/GirlsFall/Dialogue/Runtime/DialogueUIController.cs` | 12 |
| `Assets/GirlsFall/Runtime/AutoSaveController.cs` | 4 |
| `Assets/GirlsFall/Runtime/GameplayMenuController.cs` | 9 |
| `Assets/GirlsFall/Runtime/OptionsMenuController.cs` | 14 |
| `Assets/GirlsFall/Runtime/ResponsiveTitleLayout.cs` | 1 |
| `Assets/GirlsFall/Runtime/TitleMenuController.cs` | 12 |
| `Assets/OnlyUpPrototype/Runtime/FallTracker.cs` | 1 |
| `Assets/OnlyUpPrototype/Runtime/PrototypeGoal.cs` | 2 |
| `Assets/OnlyUpPrototype/Runtime/PrototypeHUD.cs` | 4 |
| `Assets/OnlyUpPrototype/Runtime/PrototypePlayerMotor.cs` | 6 |
| `Assets/OnlyUpPrototype/Runtime/StageRespawn.cs` | 3 |
| `Assets/OnlyUpPrototype/Runtime/ThirdPersonOrbitCamera.cs` | 22 |
| `Assets/CandyKingdom/Boundary/Runtime/CandyBoundaryMarker.cs` | 5 |
| `Assets/CandyKingdom/Motion/Runtime/CandyMotionDriver.cs` | 2 |
| `Assets/CandyKingdom/Runtime/CandyTownSettings.cs` | 8 |
| `Assets/CandyKingdom/Shortcake/Runtime/ShortcakeRouteLayout.cs` | 11 |
| `Assets/Characters/Scripts/CharacterMovement.cs` | 4 |
|合計|173|

CharacterPortraitController.csの件数にはPortraitExpression、DialogueData.csにはDialogueLine、ShortcakeRouteLayout.csにはSweetRouteNodeを含みます。

## 説明内容
- 数値の用途、増減による変化、単位、根拠のある初期値を記載しました。
- Gravityは負の値の絶対値、立ち絵のBottom Offsetは正で下、Text Position Yは正で上、という実際の向きを確認しました。
- カメラの補間は値が大きいほど速く追従すること、障害物退避とホイール距離が別であることを説明しました。
- Run Reference Speedは実移動速度ではなく再生倍率の基準で、倍率の制限も明記しました。
- 生成設定は再生成時に反映するもの、生成記録やルートの検証用データは編集しても地形が変わらないもの、と区別しました。
- オートセーブ間隔の説明では、正常終了時の保存と強制終了時の巻き戻りリスクを区別しました。
- 旧移動スクリプトと現行Motorの設定が別であることも明記しました。

## Inspectorの整理
既存の英語Headerを日本語化し、必要な箇所へ日本語Headerを追加しました。
主な区分：立ち絵の表示先・表情補正・フェード、会話内容、本文と名前ラベル、余白、自動保存の間隔、タイトルのボタンと確認、オプションのスライダーと表示先、カメラ構図/回転/ズーム/障害物、移動性能、落下・復帰、生成設定・記録です。
まとめて宣言されていた参照は項目ごとの宣言へ分け、個別のTooltipを付けました。名前・型・宣言順・初期値は維持しています。
表示名を日本語の変数名へ置き換える変更はしていません。

## カスタムInspector
既存の2か所を調査しました。
- DialogueTextDesignSetup.cs：DrawDefaultInspectorを使用しているため、標準Tooltipをそのまま利用できます。変更不要でした。
- PortraitLayoutSetup.cs：DrawDefaultInspectorに加え、独自のFade Duration欄のGUIContentにも、時間・大小の効果・初期値を含む日本語説明を設定しました。
Tooltipだけのために新しいCustomEditorやPropertyDrawerは追加していません。

## 追加・変更ファイル
- 上表の22スクリプト：Tooltip・Header・宣言の表示整理
- Assets/GirlsFall/Dialogue/Editor/PortraitLayoutSetup.cs：既存カスタム欄の説明文字列を更新
- 新規 Assets/GirlsFall/InspectorHelp/Editor/TooltipAudit.cs：属性・Inspector・実行コード・保護ファイルの監査用
- 新規 Assets/GirlsFall/InspectorHelp/Backups 以下：元スクリプト、未保存作業Sceneのコピー、保護対象のハッシュ
- 新規 Assets/GirlsFall/InspectorHelp/README.md：この報告
- 新規 プロジェクト直下のAGENTS.md：今後のInspector項目にも日本語Tooltipを付ける開発方針
Unityが作成する対応metaファイルも含みます。

Scene、Prefab、会話アセット、画像、モデル、Animator、Animation Clip、入力設定、セーブ形式は変更していません。
作業前にPlay中の進行を保存して停止しました。以後の動作テストはLibrary内の専用保存先へ分離し、実ユーザーの進行/設定データは初期化していません。

## 動作確認
- 全173項目のTooltip属性が存在し、日本語を含むことを監査：不足0
- ADV Prefabと会話アセットの62項目でSerializedProperty.tooltipを取得：不足0
- Unity InspectorでOpeningDialogueのDefault Character Nameへ実際にマウスを合わせ、日本語の説明表示を確認
- 確認用に一時変更したInspector幅は元へ戻しました
- 変更前後のAssembly-CSharpの実行コードとフィールド定義を比較：一致
  メソッドのIL、初期化処理、フィールド名・型・修飾子、Tooltip/Header/Space以外のフィールド属性を照合しています
- 非対象861ファイルのSHA256：すべて一致
  Scene/Prefabの参照や設定、フォント、画像、外部コード、既存ユーザー保存ファイルがある場合はそれらも含みます
- アプリ全体51項目、テキスト75項目、立ち絵47項目の既存Playモード検証：計173項目通過
  ADV表示・表情・文字送り・スキップ・操作復帰・ポーズ・タイトル遷移・保存/再開・落下状態・保存失敗等を確認
- 最終Unity Console：新しいエラー・警告0

画面でのホバーは代表項目の確認です。全173項目を1つずつ実マウスでホバーした検証は行っていません。
カスタムInspectorのFade Duration欄はGUIContentを確認済みですが、その欄の実ホバーは未検証です。
ビルド版の実行は今回未検証です。

## 対象外と制約
Unity標準コンポーネント、Packages、外部アセット、UnityのTutorialInfoテンプレートは変更していません。
SaveDataManager、AtomicSaveFile、GameSession、PlayerSaveState、GameSettingsのうちInspectorへ公開されていない内部状態・純粋なセーブ用データには、表示されないTooltipを無理に追加していません。Inspectorに実際に出る自作設定項目の漏れはありません。
Array SizeやScript欄などのUnity標準表示は対象外です。

## 利用方法
Inspectorの項目名へカーソルを合わせ、少し待つと日本語の説明が表示されます。
ラベルが省略されて読みにくい場合はInspectorを横へ広げて確認してください。狭い表示ではUnityが省略前の項目名を先に表示する場合があります。
項目の意味を読むだけなら、値をクリック・編集する必要はありません。

今後追加する自作Inspector設定にも、用途・増減の効果・単位を記した日本語Tooltipを付けます。
この方針はプロジェクト直下のAGENTS.mdへ保存済みです。

## 証跡
- Library/InspectorTooltip/Inventory.json：変更前の対象一覧
- Library/InspectorTooltip/Coverage.json：173項目の現在の説明全文
- Library/InspectorTooltip/Audit.json：属性・実行コード・保護確認
- Library/InspectorTooltip/BeforeBehavior.json / AfterBehavior.json：実行コード・フィールド比較
- Library/GirlsFallIntegration/Results.json：アプリ51項目
- Library/DialogueTextDesign/Results.json：テキスト75項目
- Library/PortraitLayout/Results.json：立ち絵47項目

Inspectorの実画面確認にはcomputer-useスキルを使用しました。
