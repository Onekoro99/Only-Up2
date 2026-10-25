# Girls Fall ADVテキストデザイン・配置調整

## 実装内容
既存のDialogueUIController/DialogueManagerは変更せず、DialogueTextLayoutを追加しました。
本文を左へ寄せ、31から34へ拡大、行間8・濃いグレー #383838へ変更しました。
キャラクター名は28、白文字、背景 #FF70A5、中央揃えの角丸ラベルです。
日本語フォントは既存のGirlsFallJapaneseSDFを継続使用しています。新しいフォントや画像は追加していません。
ウィンドウ背景・縁取り・角丸Sprite・透明度は維持し、横位置・幅・下端位置を参考画像に寄せました。
立ち絵の位置・サイズ・補正・描画順、会話内容、表情切り替え、スキップ、セーブ連携を維持しています。

## どこを選んで調整するか
1. Playを停止します。
2. Projectで Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab をダブルクリックします。
3. Prefab ModeのHierarchyで次を選択します。
   DialogueCanvas → Presentation → ResponsiveDesign → DialogueBorder → DialogueWindow
4. Inspectorの DialogueTextLayout を調整します。
5. Prefabを保存します（Auto Save、またはCtrl+S）。
既存のゲームプレイSceneのPrefabインスタンスへ反映されます。Sceneファイルを個別に変更する必要はありません。
Scene固有の調整にする場合は、Scene内の同じGameObjectで値を変更し、そのSceneを保存します。OverrideはPrefab値より優先されます。
Play中の変更は停止で元に戻ります。永続化したい値は停止中にPrefab/Sceneへ保存してください。

停止中にも設定が反映されます。更新が見えない場合は「配置・文字設定を更新」を押してください。
「会話データの先頭をプレビュー」は、設定済みOpeningDialogueの先頭行を表示します。会話データ本体は変更しません。Play中は押せないようにしています。

## 本文の初期設定
|項目|初期値|説明|
|---|---:|---|
|Font Size|34|自動縮小しない文字サイズ|
|Text Position X|0|本文Rectを左右に移動|
|Text Position Y|0|本文Rectを上下に移動。正で上|
|Text Width|1330|本文Rectの幅|
|Text Height|300|本文Rectの高さ|
|Line Spacing|8|TextMeshProの追加行間|
|Character Spacing|0|追加文字間隔|
|Text Color|#383838|濃いグレー|
|Text Left Padding|82|本文Rect左端から文字まで|
|Text Right Padding|58|右側余白|
|Text Top Padding|84|名前ラベルの下へ本文を置く余白|
|Text Bottom Padding|58|操作案内・次へアイコンの領域|

基準は1920×1080のUI単位です。
本文Rectはウィンドウ内側の左上をAnchor/Pivotとして配置します。
Text PositionとPaddingを加えた位置が表示開始位置になります。文字列の先頭に空白は追加していません。
本文の表示開始Xは共通デザイン基準で約627、変更前は約676なので約49px左へ移動しました。
本文Rectは左上(0,0)、文字は左82・上84の余白から始まります。実際の字形にはフォント固有のベアリングが加わります。
Font Style=Normal、Alignment=Top Left、Word Wrapping=ON、Auto Size=OFFです。
文字送り中は全文を同じRectでレイアウトし、既存maxVisibleCharactersで可視文字数だけを変更します。

## 名前ラベルの初期設定
|項目|初期値|
|---|---:|
|Name Font Size|28|
|Name Position X|44|
|Name Position Y|−14|
|Name Label Width|250|
|Name Label Height|52|
|Name Text Color|#FFFFFF|
|Name Background Color|#FF70A5|

名前ラベルの位置はウィンドウ内側左上基準です。
名前ラベル左端はデザイン基準で約589、本文開始は約627で、差は38です。
中央揃え・角丸の背景を維持しています。名前そのものは従来どおりDialogueDataの話者名から表示されます。

## ウィンドウと立ち絵
DialogueBorderの初期位置：(540,30)、サイズ：(1340,310)。
既存の(635,55)、(1230,315)から左へ広げ、参考画像に近い横長配置にしました。
ウィンドウの枠・背景のSpriteや色・透明度は変更していません。
立ち絵の共通幅570、左下位置(0,0)、下端補正2、表情別の余白補正は変更していません。
ウィンドウの一部は立ち絵の後ろに入りますが、本文・名前の表示領域は立ち絵に隠れません。
Canvas ScalerはScale With Screen Size / 1920×1080を維持しています。

## 長いセリフについて
指定文字サイズを維持し、自動縮小は行いません。
本文Rectを超えた文字はMaskingで外へ描画しないようにしています。
EditorのInspectorで表示範囲超過を検出した場合は警告します。会話を分割するか、Text Width/Height、Padding、Font Sizeを調整してください。
既存のOpeningDialogueの5件は全文が収まり、はみ出さないことを確認しました。
任意の長さの文を一定サイズの枠へ必ず全文表示する機能や自動ページ送りは追加していません。

## 変更ファイル
- 新規：Assets/GirlsFall/Dialogue/Runtime/DialogueTextLayout.cs
- 新規：Assets/GirlsFall/Dialogue/TextDesign/Editor/DialogueTextDesignSetup.cs
  （Editor設定、Inspector、会話データからのプレビュー）
- 新規：Assets/GirlsFall/Dialogue/TextDesign/Editor/DialogueTextDesignTests.cs
- 変更：Assets/GirlsFall/Dialogue/Editor/DialogueSetup.cs
  （ADV UI再生成時にも新レイアウトを設定）
- 変更：Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab
- 新規：このガイド、TextDesign/Backups以下の退避ファイル、およびUnityのmeta

Sceneファイル、DialogueUIController.cs、DialogueManager.cs、会話ScriptableObject、立ち絵画像、CharacterPortraitController/Layout、セーブ・操作・カメラスクリプトは変更していません。
作業開始時はPlay中だったため、最新の進行データを保存してから停止しました。その後のテストは専用保存先に分離しました。
未保存だった編集用Sceneは保持し、別名バックアップも作成しました。

## 動作確認
テキスト関連75項目、既存アプリフロー51項目、立ち絵配置47項目、計173項目が通過しました。
- 1920×1080、1600×900、1280×720それぞれで会話5件の文字領域・左寄せ・はみ出し・立ち絵との分離を確認
- フォント、サイズ、色、名前ラベル、行間、各余白を確認
- 位置・幅・高さ・文字サイズ・行間・文字間隔・色・名前ラベルの調整値が実コンポーネントへ反映されることを確認
- 保存されたPrefabを読み直し、設定の永続化を確認
- maxVisibleCharactersと本文をレイアウト更新が上書きしないことを確認
- 文字送り前後で行の割り当てとRect位置が変わらないことを確認
- 長文は自動縮小せずマスクすることを確認
- スキップ・通常終了、表情、操作復帰、完了保存、続きから、新規開始、タイトル往復、ポーズ、保存失敗を回帰確認

最終設定への余白微調整後はテキスト75項目を再実行しました。
保護対象863ファイルはすべて変更前のSHA256と一致しました。実ユーザーの進行/設定ファイルが既存ならそれらも保護対象に含めています。
立ち絵の位置・サイズ・余白補正・Sprite参照・フェード設定も変更前と一致しました。
最終Unity Consoleのエラー・警告は0件です。
描画画像はRenderTextureによる解像度検証用です。実際のUIは既存のScreen Space Overlayを維持しています。

未検証：物理マウスでのInspector操作、実キー入力による通し操作、ビルド版、指定外の全画面比率。
今回のテキスト配置・Inspector調整機能に未実装項目はありません。参考画像の装飾追加やフォント画像の再制作は行っていません。

## 証跡
Library/DialogueTextDesign/Results.json：75項目
Library/DialogueTextDesign/{1920x1080,1600x900,1280x720}.png：描画確認
Library/DialogueTextDesign/Preservation.json：保護確認
Library/GirlsFallIntegration/Results.json：アプリ51項目
Library/PortraitLayout/Results.json：立ち絵47項目
