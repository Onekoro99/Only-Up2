# Girls Fall タイトル・オプション・オートセーブ実装報告

作成日：2026年10月20日

プロジェクト：`C:\Users\9shun\UnityProjects\Only-Up2`

Unity：6000.3.25f1／旧Input Manager／CharacterController。新しい外部パッケージは追加していません。

## 起動方法

`C:\Users\9shun\UnityProjects\Only-Up2\Assets\GirlsFall\Scenes\TitleScene.unity` を開いてPlayしてください。

Build SettingsはTitleSceneを先頭、GirlsFallGameplayを2番目に登録しました。元のmainシーンは残しています。既存のOnlyUp_Prototype、kiyotaなどのシーンは上書きしていません。

- 続きから：最新の有効な進行データから再開。データがない場合は無効です。
- はじめから：確認ダイアログの「はい」で新規開始。「いいえ」でキャンセルします。
- オプション：音量、画面モード、解像度、感度を変更。保存または戻るで設定を保存します。
- ゲーム終了：設定だけを保存して終了。タイトル画面から進行データを書き換えません。
- ゲーム中：Escapeで一時停止。再開、タイトルへ戻る、ゲーム終了が可能です。タイトルへ戻るときも自動保存します。手動保存・過去データのロードはありません。

## 事前調査と再利用

- タイトル用シーン、セーブ／ロード、オプション設定は存在しなかったため新規実装しました。
- 既存のCharacterController、PrototypePlayerMotor、FallTracker、StageRespawn、修正済みIdle／Run／Jump、三人称カメラを再利用しています。
- 既存のステージ管理・PVシステムは未実装でした。現在ステージ番号と所持PVは保存データ項目だけを用意し、新規ゲームではステージ1・PV0にします。
- 既存のゴール到達イベントを使い、ステージ突破フラグを保存・復元します。次ステージへの進行システムは追加していません。
- 背景画像は `Assets/background/キャンディの空から少女がダイブ.png` を使用しました。

## 作成したScene

以下の相対パスの基準は `C:\Users\9shun\UnityProjects\Only-Up2` です。

| Scene | 内容 |
|---|---|
| `Assets/GirlsFall/Scenes/TitleScene.unity` | タイトル背景、4ボタン、確認ダイアログ、エラーパネル、オプション画面、EventSystem |
| `Assets/GirlsFall/Scenes/GirlsFallGameplay.unity` | 元プロトタイプの複製。オートセーブとEscapeメニューを追加 |

## UIデザインと素材

- パステルピンク・水色・白、角丸ボタン、薄い影、ホバー・選択・押下・無効状態の色変化。
- ロゴ下の右下領域へメニューを配置し、背景に含まれるロゴを別テキストで重ねていません。
- Canvas ScalerはScale With Screen Size、1920×1080。デザインルートを画面内へ均等縮小し、背景はアスペクト比を維持して収めます。
- 16:9以外では淡いピンクの余白を許容し、イラスト全体・ロゴ・キャラクターが切れない構成です。
- 元画像のインポーターにNPOTサイズ変換があったため、UI用にバイト同一のコピー `Assets/GirlsFall/UI/TitleArtwork.png` を作成し、そのコピーだけNPOT None・非圧縮に設定しました。元画像と元画像の設定は変更していません。
- 日本語は同梱のNoto Sans CJK JP／TMP SDFを使用します。OFLライセンスを `Assets/GirlsFall/Fonts/OFL.txt` に同梱しました。
- 既存のuGUIパッケージ内のTextMeshPro必須リソースを `Assets/TextMesh Pro/` に導入しました。Examples & Extrasは導入していません。
- BGM・SEスライダーの効果を確認できるよう、簡易的な仮タイトルBGM・クリックSEをコードで生成しています。正式な楽曲・SE素材は未導入です。

## 新規スクリプト

ランタイム：`Assets/GirlsFall/Runtime/`

| スクリプト | 役割 |
|---|---|
| TitleMenuController.cs | 4ボタン、確認、読み込み中の多重操作防止、エラー表示、タイトル終了 |
| OptionsMenuController.cs | Slider、Toggle、TMP_Dropdown、既定値、保存、戻る |
| GameSettingsManager.cs | 進行と独立した設定保存・読み込み、音量、表示設定、カメラ感度、仮音源 |
| PlayerSaveState.cs | 進行・プレイヤー・落下状態のデータと検証 |
| SaveDataManager.cs | 1スロット保存・読み込み、保存順序保証、非同期更新、エラー管理 |
| AtomicSaveFile.cs | チェックサム、書き込み途中の復旧、原子的なファイル置換 |
| AutoSaveController.cs | プレイヤー状態取得／復元、定期・空中・落下開始・着地・ゴール・復帰・終了時の保存 |
| GameSession.cs | タイトルからゲームへの新規／再開要求、シーン間の状態受け渡し |
| GameplayMenuController.cs | Escapeメニュー、保存してタイトルへ戻る／終了 |
| ResponsiveTitleLayout.cs | 1920×1080デザインの画面内フィット |

Editor：`Assets/GirlsFall/Editor/`

- GirlsFallTitleBuilder.cs：シーン・UI・フォント生成とコントロールの調整。既存の同名シーンの上書きを拒否します。
- GirlsFallTitleTests.cs：タイトル、設定、保存、破損、落下復元、実解像度の統合検証。
- GirlsFallRestartTests.cs：Editor停止／新しいPlayセッションをまたぐ終了時保存・状態復元・設定再読み込みの検証。

## 変更した既存ファイル

| ファイル | 変更内容 |
|---|---|
| `Assets/OnlyUpPrototype/Runtime/PrototypePlayerMotor.cs` | 水平速度の取得と、位置・向き・水平／鉛直速度・接地状態の復元APIを追加（既存の移動計算は維持） |
| `Assets/OnlyUpPrototype/Runtime/FallTracker.cs` | 離地高さ・最高点・空中状態の取得と復元APIを追加 |
| `Assets/OnlyUpPrototype/Runtime/ThirdPersonOrbitCamera.cs` | オプションのマウス感度を反映するプロパティを追加 |
| `Assets/OnlyUpPrototype/Runtime/PrototypeGoal.cs` | 保存されたゴール到達フラグを復元するAPIを追加 |
| `ProjectSettings/EditorBuildSettings.asset` | 新しいタイトル／ゲームシーンを追加し、タイトルを起動シーンへ設定。元mainは保持 |

元のOnlyUp_Prototypeシーン、既存生成／検証スクリプト、StageRespawn、PrototypeHUDはバックアップとのハッシュ一致を確認しました。既存のFBX・Animation Clip・Animator Controllerに変更処理は行っていません。

作業開始時から存在したGit変更（Packages、ProjectVersion、ShaderGraphSettings、boot.configの削除、既存の未追跡アセットなど）は破棄していません。Gitのreset・checkout・clean・commitは行っていません。

実装前バックアップ：

`C:\Users\9shun\Documents\Codex\2026-10-18\unity-mcp-unity\work\BeforeGirlsFallTitle_20261020`

## 保存する情報と復元方式

保存先は `Application.persistentDataPath` 以下です。現在の会社名／製品名を変更していないため、この端末の通常の保存フォルダは `C:\Users\9shun\AppData\LocalLow\DefaultCompany\My project (3)` です。

- 進行：`girls-fall-progress.json`
- 設定：`girls-fall-options.json`
- 保存中のみ、それぞれに`.pending`トランザクションファイルを使用します。

進行は論理的に1スロットです。過去のスロットやバックアップを選択してロードするUIはありません。

保存情報：現在のシーン名、ステージ番号、ゴール到達、PV、プレイ時間、最終保存日時（UTC）、位置、Quaternionの向き、水平速度ベクトル、鉛直速度、接地／空中／下降状態、離地高さ、空中最高点。

ロード時はCharacterControllerを一時的に無効化して位置・向きを復元し、有効化後に速度と接地フラグを設定します。落下中のデータは接地状態として復元しません。FallTrackerにも離地高さ・最高点・空中／下降状態を渡すため、落下距離の計測を継続できます。

保存された水平速度は復元します。その後、入力がなければ既存の空中制御による減速が通常どおり進みます。

## オートセーブと破損対策

- 地上での通常間隔：初期値3秒。InspectorのSave Intervalで変更できます。
- 空中での間隔：初期値0.15秒。Airborne Save Intervalで変更できます。
- 落下開始、着地、ゴール、場外復帰、フォーカス喪失／一時中断、正常終了時は即時保存します。
- 空中の定期更新はバックグラウンドIOにし、連続した不要な要求は最新のものへまとめます。UnityのJSON生成・状態取得はメインスレッド上で行います。
- 落下開始・着地・正常終了などの重要点は同期保存し、古い非同期要求が後から新しい状態を上書きしないようリビジョン番号で防ぎます。
- データは一時ファイルへ書き、Flush(true)後にFile.Replaceで置換します。最初の保存ではFile.Moveを使用します。
- SHA-256チェックサムと形式番号を検証します。書き込み途中で止まっても、完全に書けた最新の本体／一時トランザクションを自動採用します。
- 破損ファイルしかない場合は読み込みエラーを表示し、古い高所のデータへ自動的に戻す処理は行いません。
- タイトルで「はじめから」を承認しても、ゲームシーンの読み込みが失敗した場合には進行を初期化しません。ゲーム側が正常に初期化された時点で新しい状態を保存します。
- オプションは別ファイルなので、新規ゲームでリセットされません。

## 動作確認結果

Unity Editor 6000.3.25f1内で、最終の統合テスト48項目と停止／再開テスト12項目、合計60項目が成功しました。最終Console表示はエラー0件です。

確認済み：

- 4ボタンの接続、日本語フォント、データなしでの続きから無効化。
- マウスクリックでオプションが開くことと画面上の日本語表示。
- はじめからの確認、キャンセル時のデータ保持、承認後の初期化。
- 読み込み中の多重操作防止、読み込み破損エラー表示。
- 地上位置、ジャンプ中、落下中、着地、ゴール到達の保存・再開。
- ステージ／PV／時間データ、水平・鉛直速度、落下最高点の復元。
- Editorを停止して新しいPlayセッションを開始した場合も、地上・上昇中・下降中のスナップショットが復元されること。
- Editor停止・再Playをまたいだ音量とマウス感度の設定読み込み。
- オプションの独立保存、ゲーム側カメラへの感度反映。
- 実際のGameビューを1280×720、1024×768、2560×1080に変更し、メニューが画面内に収まること。
- タイトル終了ボタンでEditorのPlayモードが終了し、進行ファイルのバイト内容が変わらないこと。
- 保存中断を模した不完全な一時ファイル、置換前の新しい完全ファイル、破損した本体ファイルの処理。

検証データは `Library/GirlsFallTests/` と `Library/GirlsFallRestartTests/` に分離しました。ユーザーの実際の進行ファイルをテスト用データで上書きしていません。

検証ログ：

- `Assets/GirlsFall/Documentation/TitlePlaytestReport.txt`
- `Assets/GirlsFall/Documentation/RestartPlaytestReport.txt`

## 未検証の項目・制約

- ビルド版のApplication.Quit、OSのフルスクリーン切り替え、複数モニター、アプリプロセスの完全な終了／再起動は未検証です。Editor停止／再Playによるライフサイクル検証と、設定ファイルの再読み込みは実施しました。
- OSからUnityやビルド版を強制終了する実験、実際の電源断、ディスク故障、ディスク満杯の負荷試験は行っていません。ファイル中断／破損は専用領域で模擬しました。
- 落下開始の同期保存が完了した後は、最新の落下状態へ復帰できます。ただし記録処理が始まる前／最初の永続化前にプロセスが停止すると、以前の有効データが残る短い時間窓があります。
- 空中更新間隔の範囲で、最新の未保存部分へわずかに巻き戻る可能性があります。初期値は0.15秒ですが、フレーム間隔やディスク遅延・停止中の要求により長くなる場合があります。完全な巻き戻し防止を保証するものではありません。
- 原子的置換とFlushは破損を起こしにくくしますが、OS・ファイルシステム・ストレージ故障まで完全に保証できません。
- ローカルファイルのコピーや書き換えに対する完全な不正防止ではありません。チェックサムは破損検出であり、暗号化・署名・サーバー認証ではありません。
- 重要な保存は同期IOなので、遅いストレージでは短いフレーム停止が起こり得ます。性能プロファイリングは未実施です。

## 未実装・追加設定・今後の改善

- ステージ進行システムとPVの獲得／消費ロジックは追加していません。保存項目とゴール到達フラグのみです。
- 仮BGM・仮SEは正式音源へ差し替えることを推奨します。
- 基本動作のためにEditorで手動の参照設定を追加する必要はありません。新規シーン、Build Settings、フォント、UI参照は設定済みです。
- InspectorではAutoSaveControllerのSave Interval、Airborne Save Interval、stage、goalなどを調整できます。メニューの各参照・配置・色も新しいシーン内で編集できます。
- 将来、会社名／製品名をGirls Fallへ変更する場合は、persistentDataPathが変わるため保存データ移行を追加してください。
- 次はWindowsビルドで終了・再起動・画面モードを検証し、低速ディスク時の保存時間を計測することを推奨します。
- より厳しい巻き戻し対策が必要なら、短周期ジャーナル、サーバー側の進行記録、未完了落下セッションの保守的な復元方式を検討してください。

再テストメニュー：

- `Tools > Girls Fall > Test Title, Settings and Fall Saves`
- `Tools > Girls Fall > Test Editor Quit and Resume`

どちらも専用テスト領域を使用します。開始前はPlayを停止し、編集内容を保存してください。
