# Girls Fall タイトル・ゲームプレイ統合 完了報告

## 使用Sceneと起動方法
- 起動：Assets/GirlsFall/Scenes/TitleScene.unity
- 正式な遷移先：Assets/CandyKingdom/Scenes/CandyTown_EnclosedWorld.unity
- 旧仮ステージ：Assets/GirlsFall/Scenes/GirlsFallGameplay.unity（削除せず、単体テスト用に維持）
- 既存main.unityも削除していません。

Unity 6000.3.25f1、旧Input Manager（activeInputHandler=0）、uGUI/TextMeshProです。
Build Settingsの有効SceneはTitleSceneを先頭、CandyTown_EnclosedWorldを2番目に設定し、旧Scene登録も保持しました。独自BuildProfileアセットは見つかりませんでした。
EditorのPlay開始先はTitleSceneです。現在別のSceneを編集中でも、通常のPlayでタイトルから始まります。
メニュー Tools → Girls Fall → Play Full Game From Title からも全体起動できます。
Scene単体でテストする場合は対象Sceneを開き、Tools → Girls Fall → Play Current Scene Only を選びます。Play終了後は元の起動設定へ戻ります。
作業開始時の未保存Sceneは上書き・破棄していません。コピーもBackupsに保存しました。

## ゲーム全体の使い方
1. Playで既存のGirls Fallタイトル背景と4つのボタンが表示されます。
2. 「はじめから」：セーブなしなら直接お菓子マップへ。セーブありなら既存の上書き確認を表示し、「はい」で新規開始、「いいえ」で中止します。
3. 新規開始時は既存の初期位置へ配置し、安全に接地してからADVを開始します。ADV終了/スキップの完了保存後に操作を復帰します。
4. WASD、Space、自由マウス回転、ホイールズームは既存実装を使用します。
5. ゲームプレイ中にEsc：ポーズメニュー。「ゲームに戻る」「オプション」「セーブしてタイトルへ戻る」を使用できます。
6. 「セーブしてタイトルへ戻る」は最新状態の保存・書き込み待機に成功してからTitleSceneへ移動します。失敗時はエラーを表示して遷移しません。
7. タイトルの「続きから」で、最後に保存された位置・向き・速度・落下状態・進行情報を復元します。
8. タイトルの「ゲーム終了」はオプション設定を保存して終了します。EditorではPlay終了、ビルドでは既存Application.Quitを使用します。タイトルから進行データは上書きしません。

## 既存システムとの連携
GameSessionの正式遷移先をCandyTown_EnclosedWorldへ変更しました。
このSceneの既存model_charaへAutoSaveControllerを接続し、既存の有効なCandyTown_Goalを登録しました。
既存DialogueCanvas.prefabをそのままインスタンス化し、Motor・AutoSave・入力停止対象を配線しました。
旧SceneにあったポーズCanvasをお菓子マップでも利用し、タイトルのOptionsMenuController付きUIを再利用してポーズ中のオプションへ接続しました。不要な新規Prefabは作成していません。
操作停止対象はMotor、カメラ、落下追跡、リスポーン、モーションドライバー等です。再開時には元のenabled状態とTime.timeScaleを復元します。
Escでポーズを解除した際は、同フレームのEscによってカメラが再びカーソルを解除しないよう、次フレームで再固定を要求します。
設定は既存のDontDestroyOnLoad GameSettingsManagerを共有します。音量3種、フルスクリーン、解像度、マウス感度を両方のオプション画面で変更できます。
新規ゲームは進行だけを初期化し、オプションは維持します。

## セーブとADV
既存の単一スロット、オートセーブ、原子的書き込み、古い非同期書き込みを排除するリビジョン管理を継続使用しています。
現在Scene名をCaptureへ記録し、旧GirlsFallGameplay名の既存セーブも有効として読み込みます。タイトルのContinue先は正式なお菓子マップです。
位置・回転・水平速度・垂直速度・接地/空中/落下フラグ・落下開始/最高地点・ステージ・PV・プレイ時間・ADV完了フラグを維持します。
旧仮ステージの保存座標もそのまま引き継ぎます。地形が異なるため、旧足場と同じ接地状態までは保証できません。自動で位置や進行を初期化する移行は行っていません。
ADV完了済みなら再表示しません。未完了なら最初から表示します。落下中/空中の未完了セーブは既存仕様どおり落下を継続し、安全に着地してからADVを開始します。
ポーズ中はゲーム時間と周期保存を停止し、ポーズ開始・タイトル復帰の重要タイミングでは最新状態を保存します。
落下中にタイトルへ戻る操作で、落下前の地点へ戻ることはありません。今回、落下中の位置・速度を保存して再開する経路を検証済みです。
強制終了は最後の確定保存以降の短い巻き戻しを完全には防げません。既存の空中保存間隔は0.15秒、地上は3秒で、I/O遅延・強制終了・データ破損に由来するリスクは残ります。実プロセス強制終了テストは今回未実施です。

## 維持したADV
立ち絵の左下固定、下端補正、表情別補正、フェード、Inspector調整、ScriptableObjectによる会話編集を維持しました。
OpeningDialogue.assetのセリフは変更していません。
指定された新フォルダ Assets/Art/UI/ADV/Characters/Heroine/heroine_*.png は引き続き見つからないため、既存設定の Assets/Art/ADV/Heroine/{normal,surprised,crying,angry,troubled}.png を使用しています。画像の生成・変更は行っていません。

## 変更ファイル
### 既存スクリプト
- Assets/GirlsFall/Runtime/GameSession.cs：正式Scene名・旧Scene名の互換定義
- Assets/GirlsFall/Runtime/PlayerSaveState.cs：正式Scene名の既定値と旧名の受理
- Assets/GirlsFall/Runtime/AutoSaveController.cs：実際のScene名を保存、ポーズ中の周期処理停止
- Assets/GirlsFall/Runtime/TitleMenuController.cs：セーブなし時は確認なしで新規開始
- Assets/GirlsFall/Runtime/GameplayMenuController.cs：ポーズ入力停止/復帰、共通オプション、保存失敗時の遷移中止、復帰時のカーソル再固定

### 新規スクリプト
- Assets/GirlsFall/Integration/Editor/ApplicationSetup.cs：重複を避けたScene配線・Build登録・Editor全体/単体起動メニュー（ApplicationLaunchToolsを含む）
- Assets/GirlsFall/Integration/Editor/ApplicationFlowTests.cs：全体フロー検証
新しいRuntime管理シングルトンは追加していません。

### Scene/設定
- Assets/CandyKingdom/Scenes/CandyTown_EnclosedWorld.unity：AS、ADV、ポーズ/オプションUI、EventSystemを追加・参照設定
- Assets/GirlsFall/Scenes/GirlsFallGameplay.unity：旧Sceneでも新ポーズ/オプションが利用できるよう配線
- ProjectSettings/EditorBuildSettings.asset：正式マップをTitleSceneの次へ追加
- EditorのPlay開始先：TitleScene
- Assets/GirlsFall/Integration 以下にバックアップ、ドキュメント、検証用スクリプトとmeta
変更した既存Prefab：なし。TitleSceneアセット自体、背景画像、ADV Prefab、会話アセット、立ち絵、カメラ/操作コード、モデル、Animator、Clipは維持しています。

## 検証結果
Unity EditorのPlayモードで全体51項目、既存の立ち絵配置47項目、計98項目がすべて通過しました。
- Title起動、4メニュー、セーブなしContinue無効、オプション保存
- セーブなし新規開始、セーブあり確認/キャンセル/確定、新規時の進行リセットと設定維持
- 実際のお菓子マップへの遷移、初回ADV、5枚の参照、下端配置、文字送り、二重送り防止、スキップ/通常完了
- 既存Motorによる移動・ジャンプ・落下、カメラ回転とズームの入力処理
- Esc入力処理によるポーズ、操作停止、既存UIによるオプション、再開
- PV777、ステージ3、プレイ時間、位置/回転/速度/落下状態を持たせて保存→タイトル→Continueし、LoadedStateの全項目を照合
- 落下再開で垂直速度が負のまま、保存地点より高い位置へ戻らないこと
- 保存I/O失敗時の遷移中止、保存先復旧後の再試行
- 正式Sceneの直接読み込み、旧Scene名セーブの互換と次回保存名
- 破損セーブでエラー表示、遷移を中止してクラッシュしないこと
- Sceneを繰り返し往復して設定Manager、Motor、EventSystem、ASが重複しないこと
- 実際のタイトル終了ボタンでPlayが終了し、タイトルから進行データが上書きされないこと

テストでは実Buttonイベント・入力処理・既存Motorを呼び出しています。物理マウス・キーボードの通し操作ではありません。
セーブと設定のテスト先はLibrary/GirlsFallIntegration以下の専用領域に切り替え、実ユーザーのセーブを初期化する操作はしていません。
保護対象827ファイル（ユーザー保存ファイルが既存ならそれらも含む）がすべて変更前のSHA256と一致しました。
新UI/EventSystem/AutoSaveの追加分を除き、お菓子マップのTransform、Collider、既存コンポーネント、Animator設定、マテリアル参照もバックアップと一致しました。
最終Unity Consoleのエラー・警告は0件です。

## 未検証・未実装
- ビルド版の作成・起動、ビルド版での画像・I/O・終了：未検証
- 実マウス/キーでの操作感・Esc復帰時のOSカーソル挙動・ディスプレイモードの実機切替：未検証
- 実プロセス強制終了/電源断：未検証
- 既存の全旧セーブ位置に対する新マップ地形との衝突整合：未検証
- 旧仮ステージをEditor単体起動する経路は配線済みですが、今回の通しテストは正式マップが対象です。
- PV獲得システム、複数ステージ進行、エンディング等の既存未実装機能は追加していません。
今回のタイトル・ADV・正式マップ・ポーズ・セーブ間の連携機能は実装済みです。Editorで必要な追加手動参照設定はありません。

## 証跡
Library/GirlsFallIntegration/Results.json：全体51項目
Library/PortraitLayout/Results.json：立ち絵47項目
Library/GirlsFallIntegration/WorldPreservation.json：マップ構成照合
Library/GirlsFallIntegration/OpeningCandy.png：お菓子マップでのADV
Backups：変更前5スクリプト、3Scene、Build設定、未保存作業Scene
