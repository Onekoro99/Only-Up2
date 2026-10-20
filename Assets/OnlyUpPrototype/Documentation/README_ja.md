# Only-Up2 プロトタイプ実装報告

作成日：2026年10月20日

プロジェクト：`C:\Users\9shun\UnityProjects\Only-Up2`

プレイ用シーン：`C:\Users\9shun\UnityProjects\Only-Up2\Assets\OnlyUpPrototype\Scenes\OnlyUp_Prototype.unity`

## 起動と操作

1. Unity Hubで **6000.3.25f1** を指定してプロジェクトを開きます。この端末ではPackage Managerが揃っている `C:\Program Files\Unity\Hub\Editor\6000.3.25f1-x86_64\Editor\Unity.exe` を使用しました。
2. 上記のプレイ用シーンを開いてPlayを押し、Gameビューをクリックします。
3. WASDでカメラの水平方向を基準に移動、Spaceでジャンプします。
4. 右クリックを押しながらマウスを動かすとカメラが回転します。右クリックを離す、またはアプリのフォーカスを失うとカーソル制御を解除します。
5. 数字付き足場01〜12を順に登り、金色のGOALを目指します。ステージ外でY=-5を下回ると同じステージの開始地点へ復帰します。
6. GameビューのScaleが2xなどの場合は1xへ戻してください。HUDや画面全体を確認できます。

既存の `kiyota`、`main`、`kouguchi`、`yamazaki` シーンやBuild Settingsの既定シーンは変更していません。編集モードのキャラクターがTポーズでも、Play時には既存Animatorが動作します。

## 事前調査と環境復旧

- ディスクと起動中Editorが2021.3.19f1となっていましたが、パッケージ・シーンは以前のUnity 6向け設定でした。
- `packages-lock.json`にGitの競合マーカーがあり、Package Managerが起動できない状態でした。
- ユーザーの承認後に、両側の依存関係を残して競合マーカーを除去し、Unity 6000.3.25f1へ戻しました。
- Unityによる再解決後もlockファイルが有効なJSONであることを確認しました。不要なパッケージは追加していません。
- 利用可能な素材は既存のmodel_chara、Idle/Run/Jump/Walk。背景素材フォルダは実質空で、今回はPrimitiveを使用しました。
- 旧Input Managerのみ有効で、Horizontal・Vertical・Jump・Mouse X・Mouse Yを使用します。
- 既存プレイヤーにはAnimatorとCharacterControllerがあり、既存のCharacterMovementはワールド方向移動でした。
- 既存AnimatorのSpeed／IsGrounded、修正済みIdle・Run・Jumpと遷移を再利用しています。

## 新規スクリプト

以下の相対パスの基準は `C:\Users\9shun\UnityProjects\Only-Up2` です。

| ファイル | 役割 |
|---|---|
| `Assets/OnlyUpPrototype/Runtime/PrototypePlayerMotor.cs` | カメラ基準移動、斜め速度補正、接地ジャンプ、重力、空中操作、方向転換、Animator制御、Teleport |
| `Assets/OnlyUpPrototype/Runtime/ThirdPersonOrbitCamera.cs` | 右ドラッグ旋回、角度制限、キャラクター追従、SphereCastによる障害物回避、カーソル復帰 |
| `Assets/OnlyUpPrototype/Runtime/FallTracker.cs` | 離地・最高点・落下開始・着地の計測、落下レポートとイベント公開 |
| `Assets/OnlyUpPrototype/Runtime/StageRespawn.cs` | ステージ外判定、開始地点復帰、速度リセット、カメラ復帰 |
| `Assets/OnlyUpPrototype/Runtime/PrototypeGoal.cs` | Rigidbodyに依存しないゴール領域判定、到達イベント |
| `Assets/OnlyUpPrototype/Runtime/PrototypeHUD.cs` | 操作説明、高さ、最高到達点、落下量、復帰回数、ゴール表示 |
| `Assets/OnlyUpPrototype/Editor/PrototypeSceneBuilder.cs` | 元シーンを複製して仮ステージを生成するメニュー。既存プロトタイプの上書きは拒否 |
| `Assets/OnlyUpPrototype/Editor/PrototypePlaytestRunner.cs` | Unity Playモードでの再現可能なスモークテスト |

ランタイム6本、Editor用2本です。外部パッケージへの依存は増やしていません。

## 作成したアセットと既存ファイルの変更

新規アセットは `Assets/OnlyUpPrototype/` 内と同フォルダの `.meta` に集約しています。

- `Scenes/OnlyUp_Prototype.unity`
- `Materials/Start.mat`、`Platforms.mat`、`Landings.mat`、`Goal.mat`
- 上記スクリプト8本とUnity生成の `.meta`
- `Documentation/BuildReport.txt`、`PlaytestReport.txt`、`README_ja.md`

既存ファイルの変更は、承認された環境修復に伴う以下の2ファイルです。

- `Packages/packages-lock.json`：競合解消とUnityの依存関係再解決
- `ProjectSettings/ProjectVersion.txt`：6000.3.25f1へ復旧（実際のリビジョンe1dba0a9aba4）

元の `Assets/Scenes/kiyota.unity`、`Assets/Characters/Scripts/CharacterMovement.cs`、`Assets/Characters/PlayerAnimator.controller` はバックアップとのSHA-256一致を確認しました。元FBX・既存Animation Clipへの変更処理は行っていません。Gitのリセット・チェックアウト・変更破棄・コミットは行っていません。

実装前のProjectSettings・Packages・既存Scenes・移動スクリプト・Controllerのバックアップは次に保存しています。

`C:\Users\9shun\Documents\Codex\2026-10-18\unity-mcp-unity\work\BeforePrototype_20261020`

## シーン構成と既存実装の再利用

- `model_chara`：既存のモデル、Animator、CharacterControllerを再利用。
- 既存 `CharacterMovement` は新しいシーンのインスタンスだけ無効化。元シーンやスクリプトは保持し、二重移動を防止。
- `PrototypePlayerMotor`、`FallTracker`、`StageRespawn`を追加。
- キャラクターモデルのメッシュ・マテリアル・Rig・Avatar・アニメーションは変更していません。
- Main Cameraに `ThirdPersonOrbitCamera` を追加。既存ライトは再利用。
- 既存TestGroundは新しいシーンでのみ非アクティブ化し、8m四方のStartPlatformに置き換え。
- 0.6m刻みの足場12段、最高部7.8mのゴール、カメラ衝突確認用の壁を追加。
- ゴールは到達表示のみで、次ステージには遷移しません。

## Inspectorの調整項目

| コンポーネント | 項目と初期値 |
|---|---|
| PrototypePlayerMotor | Move Speed=3.5、Rotation Speed=540度/秒、Jump Speed=7、Gravity=-20、Air Control=0.75、Movement Camera参照 |
| ThirdPersonOrbitCamera | Target Height=1.1、Distance=5.5、Sensitivity=3、Min Pitch=-15度、Max Pitch=70度、初期Pitch=22度、Yaw、Collision Radius=0.22、Padding=0.1、Return Smoothness=10、Collision Mask |
| FallTracker | Significant Fall Distance=3m |
| StageRespawn | Start Point、Kill Height=-5、Orbit Camera参照 |
| PrototypeGoal | Player参照、Half Extents=(1.3, 1.6, 1.3) |

CharacterControllerの高さ約1.889、半径約0.264、中心Y約0.869は既存のモデル寸法に合わせた設定を維持しました。Root Motionは無効、ルートカーブによる移動上書きはモーターのOnAnimatorMoveで抑えています。

## 落下量を他システムから利用する方法

`FallTracker.FallStarted` は下降開始時の高さを通知します。`FallCompleted` は `FallReport` を通知します。

```csharp
tracker.FallCompleted += report =>
{
    // report.Distance を将来のPV計算などへ渡す。
    // 実際のPV加算は今回実装していない。
};
```

レポートには次を保持します。

- `DepartureHeight`：離地前の接地高さ
- `StartHeight`：空中での最高点＝下降開始高さ
- `EndHeight`：着地高さ、または場外復帰が発生した高さ
- `Distance`：StartHeight - EndHeight（最低0）
- `IsSignificant`：閾値以上の落下か
- `EndedByRespawn`：実際の着地か、場外復帰で計測が終了したか

高さはキャラクタールートのY座標を使います。ジャンプを含む場合は離地からの差ではなく最高点からの下降量です。場外復帰は空中での打ち切りなので、着地とは明確に区別できます。

## Unity上の確認結果

Unity 6000.3.25f1で実行し、最終の **31項目がPASS、失敗0件** でした。結果は `Assets/OnlyUpPrototype/Documentation/PlaytestReport.txt` に保存されています。

- スタート地点に接地し、Idleを再生。
- 直進と斜め移動がともに0.4秒で1.4m。
- Run切り替え、ジャンプ約1.156m、Jump切り替え。
- 停止着地はIdle、移動着地はRunへ復帰。
- カメラYaw90度でW入力がワールドX方向へ移動。
- 上下角度の下限-15度／上限70度。
- 壁があるとカメラ距離が約1.658mまで短縮。
- 約5.904mの落下でSignificant判定。
- 場外からStageStartへ復帰し、場外終了フラグが設定される。
- 実際のCharacterController移動とジャンプで、12段すべてとゴール足場へ連続踏破。
- ゴール領域の到達判定。
- GameビューでHUD・キャラクター・足場の表示を確認。
- 実際のSpaceキーによるジャンプを確認。
- 最終のConsole表示はエラー0件。

自動テストは入力値をモーターへ直接渡し、UnityのCharacterController・Physics・Animatorを実際に評価しています。人がWASDと右ドラッグを組み合わせて操作したときの感度・酔い・遊びやすさは、継続的な手動プレイテストが必要です。

再実行：編集モードで `Tools > Only Up > Run Prototype Smoke Test`。テストはPlayモードへ入り、一時的な操作・移動を行って、結果を保存して編集モードへ戻ります。保存済みシーンの配置を変更しません。

## 今回実装していない機能と残る調整

- PV獲得・管理・交換、アイテム、複数ステージ進行、会話、立ち絵、エンディング、衣装・リアクション演出は未実装。
- 正式な美少女モデルはこのプロジェクト内では検出されず、既存の白いmodel_charaを維持。
- 仮ステージは静的足場のみ。動く床、チェックポイント、セーブ、音声、正式UI、ビルド版は今回の対象外。
- 常時カメラ回転・ズーム操作・カメラ自動復帰・着地ダメージは追加していません。
- カメラの壁回避は現在のPrimitive環境で確認。複雑な天井・狭い通路での調整は正式ステージに合わせて実施してください。
- MCPのHTTPサーバーは起動しましたが、確認時にEditorインスタンス登録が0件となる場面がありました。今回はEditorスクリプトとWindows操作で生成・実行検証を完了しています。MCPの常時接続は別途接続状態を確認してください。

## 次のフェーズ

1. 実際のWASD・右ドラッグで移動速度、ジャンプ距離、空中制御、カメラ距離・感度を調整する。
2. 正式キャラクターを導入する場合は、AnimatorとRig・ボーンパスの互換性を検証する。
3. ステージ境界、復帰地点、チェックポイントのルールを決める。
4. FallCompletedのレポートを購読するPV計算システムを別コンポーネントとして追加する。
5. 仮足場を正式ステージへ置き換え、カメラ衝突とジャンプ到達性を再確認する。
