# ADVウィンドウ：参考画像に合わせた配置・デザイン

## 今回の修正
- ウィンドウXを540から500へ変更し、左へ40 UI単位移動しました。
- ウィンドウを中央基準のResponsiveDesignから外し、画面左下基準のPresentation直下へ移しました。
  横長画面で中央余白が加わって右へずれる原因も解消しています。
- 右端は自動で広げ、画面右側の余白を維持します。
- 本文は34から38、名前は28から32へ拡大しました。
- ピンクの大きな角丸枠、白〜ごく淡いピンクの背景、花付きの名前ラベル、淡い星と雲、マウス案内、ピンクの次へアイコン、スキップ矢印を再現しました。
- 飾りはUnityのUIメッシュで描画し、画像の引き伸ばしやクリック判定を追加しません。
参考画像の雰囲気・配置を基準にした再現で、イラスト素材そのものを複製したものではありません。

## 主な初期値（1920×1080基準）
|項目|値|
|---|---:|
|Window Position X / Y|500 / 30|
|Window Width / Height|1380 / 320|
|Fit Window To Screen|ON|
|Auto Window Width|ON|
|Window Right Margin|40|
|Corner Density|0.4|
|本文 Font Size|38|
|Line Spacing|10|
|Text Width / Height|1364 / 304|
|本文 Padding 左 / 右 / 上 / 下|102 / 200 / 88 / 56|
|名前 Font Size|32|
|名前 Position X / Y|76 / −10|
|名前 Label Width / Height|250 / 52|

Window Widthは自動拡張ONでは最小幅です。横長画面では右余白まで広がります。本文の幅はText Widthで別に調整できます。
本文の開始Xは基準画面で約610です。立ち絵の表示幅570より右に置き、本文が隠れないようにしています。
名前はピンク #FF70A5・白文字、本文は濃いグレー #383838です。
角丸の大きさはCorner Densityを小さくすると大きくなります。

## Unity Editorで調整する場所
1. Projectの Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab を開きます。
2. Hierarchyで DialogueCanvas → Presentation → DialogueBorder → DialogueWindow を選びます。
   ※以前のResponsiveDesignの下ではなく、Presentationの直下へ移動しています。
3. Inspectorの DialogueTextLayout を調整します。
4. 「会話ウィンドウ全体」欄で枠全体の位置・幅・高さ・色を変更できます。
5. Font Size、Padding、Name Position等で本文・名前を調整します。
6. Play停止中にPrefabを保存すると、各ゲームプレイSceneへ反映されます。
追加・変更したInspector項目にも日本語Tooltipを付けています。

飾りの濃さを変える場合：
- ウィンドウ内のCandyWindowArtworkを選び、CandyDialogueDecorationのStrengthを変更
- 名前の花：NameRibbon内のNameFlower
- マウス：MouseIcon
- スキップ矢印：Skipボタン内のSkipArrows
すべてraycastTarget=falseで、文字送りやスキップのクリックを妨げません。

## 変更・作成ファイル
変更：
- Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab
- Assets/GirlsFall/Dialogue/Runtime/DialogueTextLayout.cs
- Assets/GirlsFall/Dialogue/TextDesign/Editor/DialogueTextDesignSetup.cs
- Assets/GirlsFall/Dialogue/TextDesign/Editor/DialogueTextDesignTests.cs

新規：
- Assets/GirlsFall/Dialogue/Runtime/CandyDialogueDecoration.cs
- Assets/GirlsFall/Dialogue/ReferenceStyle/Editor/CandyDialogueStyle.cs
- ReferenceStyle/Backups、README、対応meta

既存の日本語SDFフォントAssets/GirlsFall/Fonts/GirlsFallJapaneseSDF.assetにもUnityによる保存データの更新がありました。同じフォントを引き続き使用し、別フォントの導入はしていません。
Sceneファイル、キャラクター画像・モデル、立ち絵の補正、会話データ、タイトル、マップ、移動/カメラ/セーブ処理は変更していません。
再生成時もDialogueTextDesignSetupから新スタイルを設定する構成です。

## 確認結果
- レイアウト・文字設定115項目：通過
  1920×1080、1600×900、1280×720、1200×900（4:3）、1920×900（横長）で会話5件を確認
- 既存アプリ動作51項目：通過
- 立ち絵47項目：通過
- 合計213項目：通過
- 実際のScreen Space Overlayの画面も撮影し、白い背景・飾り・左下固定を確認
- 飾り4種類すべてのCanvasRendererと、クリック判定を受けない設定を確認
- 立ち絵より後ろ、保存エラー画面より後ろの描画順を確認
- Unity Console：最終エラー・警告0
- 非対象951ファイルの照合：950一致、日本語SDFフォント1件の保存データ更新
テストは専用保存先で行い、実ユーザーの進行データを初期化していません。

未検証：ビルド版、物理マウス・キーによる全操作、任意の全画面比率。
セリフ送り・スキップは既存のButtonイベントと処理を用いて検証しています。

## 証跡
- Library/ReferenceStyle/FinalOverlay.png：実際のOverlay表示
- Library/DialogueTextDesign/Results.json：115項目
- Library/GirlsFallIntegration/Results.json：51項目
- Library/PortraitLayout/Results.json：47項目
- Library/ReferenceStyle/Preservation.json：保護照合

実画面のキャプチャは、その時点のGameビューの比率（1092×504）です。
UIの3D背景は現在のお菓子マップのままです。参考画像に含まれる旧仮ステージへの置き換えは行っていません。
