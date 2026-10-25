# ADV立ち絵の配置修正・調整ガイド

## 修正結果
立ち絵を中央基準のResponsiveDesignから外し、Presentationの直下へ移動しました。
実画面左下をAnchor/Pivot=(0,0)として固定し、Imageの矩形をSprite比率に一致させることでImage内部のレターボックスもなくしました。
共通枠は表情切り替え時に変更しません。内側のImageだけを、保存済みアルファ範囲に合わせて拡大・移動します。
見えているキャラクターの範囲は左端0、下端−2（基準解像度のUI単位）で、画面の最下端に接します。下端2単位を画面外へ下げ、細い隙間を防止しています。
セリフウィンドウのデザイン・位置・文字送り・スキップ・セーブ処理は変更していません。

## どのGameObjectを選ぶか
1. Playを停止します。Projectで Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab をダブルクリックします。
2. Prefab ModeのHierarchyで DialogueCanvas → Presentation → PortraitFrame を選択します。
3. Inspectorの CharacterPortraitLayout を調整します。
4. Prefabを保存します（Auto Save有効、またはCtrl+S）。既存GirlsFallGameplayのPrefabインスタンスへ反映されます。
シーン固有に変更する場合も、シーン内の DialogueCanvas → Presentation → PortraitFrame が対象です。その場合はSceneを保存します。
Play中の調整は見た目の確認用です。Play終了で値が戻るため、永続化する値は停止中にPrefab/Sceneへ設定してください。

## 共通設定
|Inspector項目|初期値|用途|
|---|---:|---|
|Position X|0|左端から左右への移動|
|Position Y|0|下端から上下への移動。正にすると意図的に空白ができます|
|Scale|1|左下を基準とした全表情の拡大率|
|Bottom Offset|2|正の値で下に移動。透明余白の自動補正に加える微調整|
|Width|570|実描画範囲の幅。1920×1080でScale=1の基準値|
|Fit To Screen|有効|異なる画面比率で会話ウィンドウと基準倍率を合わせる|
|Fade Duration|0.16秒|表情切り替えのクロスフェード時間|

Fade Durationは同じGameObjectのCharacterPortraitControllerの値と連動します。
停止中もExecuteAlwaysの配置処理で更新します。更新が見えないときは「Scene / Gameビューの配置を更新」を押してください。
停止中の表示表情は同じGameObjectのCharacterPortraitController → Preview Expressionで選びます。会話データは変更しません。
停止中のPrefab表示用CanvasGroup.alphaを1にしました。実行時は既存DialogueManagerが表示・非表示を制御します。

## 表情ごとの補正
同じGameObjectの CharacterPortraitController → Expressions を開きます。
各要素のExpressionで表情、Spriteで画像、Offset X/Yで位置、Scaleでその表情の拡大率を指定します。
現在の手動補正は5表情とも Offset=(0,0)、Scale=1です。
表情を切り替える際、画像と補正値を同時に反映します。フェード元の画像にも元の補正を保持します。
共通の枠を固定し、実描画の幅をそろえているため、表情による透明余白の差で上下に浮くことを防ぎます。元イラストの顔の姿勢・構図差は、必要に応じて個別Offset/Scaleで調整できます。

## 自動アルファ補正
EditorでPNGを一時Textureに読み込み、アルファ32/255以上の最大連結成分をキャラクター本体として解析しました。
小さな独立した描画片やノイズに下端判定が引っ張られないようにしています。
解析済みの正規化描画範囲とSprite参照をPrefabへ保存しています。実行時は画像検索・画素解析・ファイル読み込みを行いません。
PNG本体のトリミング/上書き、インポート設定のRead/Write有効化は行っていません。
旧RectMask2Dの下端Padding=18による切り取りは除去しました。画像下端にある小さな描画片は本体の接地補正により画面外となります。

初期状態でImageへ適用される位置（共通位置0、Width570、Scale1、Bottom Offset2）：
|表情|自動X|自動Y|手動Offset|手動Scale|
|---|---:|---:|---|---:|
|Normal|0|−2.00|(0,0)|1|
|Surprised|−6.04|−20.11|(0,0)|1|
|Crying|−6.88|−20.91|(0,0)|1|
|Angry|−22.94|−20.53|(0,0)|1|
|Troubled|−13.92|−20.27|(0,0)|1|

自動値は余白を除去するための内部Image位置です。キャラクター本体は各表情とも左端0・下端−2となります。WidthやScaleを変えると自動値も変わります。
画像を差し替えたりPNG内容を更新した場合は、CharacterPortraitLayoutの「指定Spriteのアルファ余白を再解析」を押してPrefab/Sceneを保存してください。
再解析は明示操作です。画像変更後に再解析しなければ、新たな余白には正しく対応できない場合があります。
極端なWidth/Scaleや正のPosition Y、表情Offset Yでは意図的に隙間やウィンドウとの重なりを作れます。初期値での隙間解消を検証しています。

## 使用画像
今回の依頼で指定された Assets/Art/UI/ADV/Characters/Heroine/heroine_*.png は見つかりませんでした。
既に参照設定済みの次の5枚を継続使用しています。
- Assets/Art/ADV/Heroine/normal.png
- Assets/Art/ADV/Heroine/surprised.png
- Assets/Art/ADV/Heroine/crying.png
- Assets/Art/ADV/Heroine/angry.png
- Assets/Art/ADV/Heroine/troubled.png
代替キャラクターや仮画像は生成していません。新しいフォルダの画像を使う場合は上記の差し替え・再解析手順で設定できます。
今回の添付はテキストのみで参考画像がなかったため、依頼文の配置条件を基準にしました。

## 変更ファイル
- 拡張：Assets/GirlsFall/Dialogue/Runtime/CharacterPortraitController.cs
- 新規：Assets/GirlsFall/Dialogue/Runtime/CharacterPortraitLayout.cs
- 新規：Assets/GirlsFall/Dialogue/Editor/PortraitLayoutSetup.cs（Editor限定解析・Inspector・Prefab設定）
- 新規：Assets/GirlsFall/Dialogue/Editor/PortraitLayoutChecks.cs（解像度・調整項目検証）
- 変更：Assets/GirlsFall/Dialogue/Editor/DialogueSetup.cs（再生成でも今回の配置が維持されるよう統合）
- 変更：Assets/GirlsFall/Dialogue/Editor/DialogueTests.cs（共通Rectに対する検証・追加チェック・バックグラウンド実行）
- 変更：Assets/GirlsFall/Dialogue/Prefabs/DialogueCanvas.prefab
- 更新：Assets/GirlsFall/Dialogue/README.md（このガイドへの案内）
- 新規：このガイド・バックアップおよびUnityのmetaファイル
ゲームプレイSceneファイルは変更せず、既存Prefabインスタンスへ反映しています。セーブ関連クラス、会話データ、DialogueManager、DialogueUIController、UI素材、タイトル、キャラクター操作、マップも変更していません。

描画順は、会話ウィンドウを含むResponsiveDesign → PortraitFrame → SaveErrorです。既定幅では本文・操作案内に重なりません。
Canvas Scalerは既存のScale With Screen Size、1920×1080を維持しています。
バックアップは Assets/GirlsFall/Dialogue/PortraitUpgradeBackups にあります。元から未保存だったシーンは閉じずに保持し、別名コピーも保存しました。

## 確認結果
Playモードで配置・調整機能47項目、既存ADV39項目、計86項目がすべて通過しました。
- 1920×1080、1600×900、1280×720、1200×900の各解像度で5表情を確認
- 本体の下端・左端座標、共通枠の維持、縦横比、画面上の下端位置
- Position X/Y、Scale、Width、Bottom Offset、表情別Offset/Scale
- 表情クロスフェード、描画順
- 初回表示、文字送り、同フレーム二重送り抑止、スキップ、通常終了、操作復帰
- 完了保存、続きから/はじめから、落下中旧セーブの扱い、保存失敗と再試行

保存済みPrefabを再読み込み、初期値・5表情の解析結果が保存されていることを確認しました。
実描画画像も確認し、キャラクターの下にあった背景の隙間がなくなっています。
自動検証のセーブ先はLibrary内の専用領域で、ユーザー進行データを変更するテストは行っていません。
未検証：Inspectorを物理マウスで操作しての調整、ビルド版、任意の全解像度、未提供の参考画像との厳密な比較。
今回依頼された配置・調整機能に未実装項目はありません。指定された新フォルダの画像への切り替えは、画像未存在のため行っていません。
