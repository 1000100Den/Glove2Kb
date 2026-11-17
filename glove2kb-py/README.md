# Glove2Kb-Py

A hand-gesture keyboard for low-step, low-movement-cost text input,
implemented in pure Python (standard library only).

> **Note:** Glove2Kb-Py is an *original reimplementation of the concept*
> of Glove2Kb, written from scratch. It is not a port of the original
> code; no original source code was used. Only the concept — gesture
> keyboard input with a glove controller — is shared.
>
> **注記:** Glove2Kb-Py は Glove2Kb の*コンセプトを独自に再実装*したもので、
> ゼロから書かれています。元のコードの移植ではなく、流用もしていません。
> 共有しているのは「グローブ型コントローラによるジェスチャキーボード」
> というコンセプトのみです。

## Overview / 概要

Text entry in VR and similar settings is slow when every character costs
a large arm movement. Glove2Kb-Py instead uses only:

* **Wrist rotation** (roll) of both hands — sensed from the hand bones,
* **Finger flexion** of the right hand — thumb and index select within a
  group, middle/ring/little together form the "grip",

with a ContactGlove-style glove controller whose tracking data is
forwarded by DivingStation over the VirtualMotionCapture protocol
(VMC: OSC messages over UDP).

VR などでは、1文字ごとに大きな腕の移動が必要だと入力が遅くなります。
Glove2Kb-Py は代わりに、

* 両手の**手首の回転（ロール）** — 手のボーンから取得
* 右手の**指の屈曲** — 親指と人差し指でグループ内のキーを選び、
  中指・薬指・小指で「グリップ」を構成

だけを使います。センサデータは ContactGlove のようなグローブ型
コントローラから DivingStation を経由し、VirtualMotionCapture
プロトコル（VMC: UDP 上の OSC メッセージ）で届きます。

## Setup / セットアップ

Requirements: Python 3.10+ (standard library only, nothing to install).
Tkinter is needed for the GUI; the core engine works without it.

動作環境: Python 3.10 以上（標準ライブラリのみ、追加インストール不要）。
GUI には Tkinter が必要ですが、エンジン本体は無くても動きます。

### DivingStation / VMC side（送信側の設定）

1. Connect the glove controller and start DivingStation.
2. Enable VMC protocol output in DivingStation and set the destination
   to `127.0.0.1` and the port you will use in this app (default
   **39539**). Bone poses (`/VMC/Ext/Bone/Pos`) must be included.
3. Start this app, enter the same port, and press **Connect**.

1. グローブ型コントローラを接続し、DivingStation を起動します。
2. DivingStation で VMC プロトコルの出力を有効にし、送信先を
   `127.0.0.1`、ポートをこのアプリで使う番号（既定 **39539**）に
   設定します。ボーンポーズ（`/VMC/Ext/Bone/Pos`）を含めてください。
3. 本アプリを起動し、同じポートを入力して **Connect** を押します。

### Running / 起動

```sh
cd glove2kb-py
python3 run.py
# or:
PYTHONPATH=src python3 -m glove2kb_py
```

## Operation / 操作方法（本実装）

1. **Grip** (bend middle + ring + little fingers) to enter input mode.
2. **Right wrist roll** chooses the row band, **left wrist roll**
   chooses the column band — together a 3×3 grid of key groups.
   Counterclockwise = first band, neutral = middle band,
   clockwise = last band. Zones use ~25° to enter and ~15° to leave
   (hysteresis), so borderline angles do not flicker.
3. While gripping, the **thumb/index combination** chooses the key
   inside the group, counted from the group's first key:
   grip only → key 1, grip+index → key 2, grip+thumb → key 3,
   grip+index+thumb → key 4.
4. **Commit** the character by releasing the grip, *or* by returning
   both wrists to neutral while still gripping. A selection must be
   held steady for a few frames (debounce) before it can be committed,
   so groups you merely pass through are not typed. After a
   neutral-return commit, the centre group stays locked out until the
   wrists move again, so one gesture never types two characters.

1. **グリップ**（中指・薬指・小指を曲げる）で入力モードに入ります。
2. **右手首の回転**で行バンド、**左手首の回転**で列バンドを選び、
   合わせて 3×3 のキーグループを決めます。反時計回り＝1番目、
   ニュートラル＝中央、時計回り＝3番目です。ゾーンは入る時 約25°、
   出る時 約15° のヒステリシスがあり、境界でちらつきません。
3. グリップ中、**親指・人差し指の組み合わせ**でグループ内のキーを
   選びます。グリップのみ→1番目、+人差し指→2番目、+親指→3番目、
   +両方→4番目です。
4. **確定**は、グリップを離すか、グリップしたまま両手首を
   ニュートラルに戻すか、どちらかで行います。選択が数フレーム
   安定するまでは確定できない（デバウンス）ため、通過しただけの
   グループは入力されません。ニュートラル復帰で確定した後は、
   手首を動かすまで中央グループがロックされ、1回のジェスチャで
   2文字入ることはありません。

### Layout / レイアウト

Two pages; the `123` / `ABC` key toggles between them. Rows are chosen
by the right wrist (top row = counterclockwise, middle = neutral,
bottom = clockwise), columns by the left wrist (left = counterclockwise,
middle = neutral, right = clockwise). Keys inside a group are listed in
selection order 1→4.

2つのページがあり、`123` / `ABC` キーで切り替えます。行は右手首
（上段＝反時計回り、中段＝ニュートラル、下段＝時計回り）、列は
左手首（左＝反時計回り、中＝ニュートラル、右＝時計回り）で選びます。
グループ内のキーは選択順 1→4 の順に並んでいます。

**Letters page / 文字ページ**

| Right \ Left | CCW (left) | Neutral | CW (right) |
|---|---|---|---|
| **CCW (top)** | a b c d | e f g h | i j k l |
| **Neutral** | m n o p | q r s t | u v w x |
| **CW (bottom)** | y z Space BS | . , ! ? | Enter 123 - ' |

**Numbers page / 数字ページ**

| Right \ Left | CCW (left) | Neutral | CW (right) |
|---|---|---|---|
| **CCW (top)** | 1 2 3 4 | 5 6 7 8 | 9 0 + - |
| **Neutral** | . , ! ? | @ # % & | * ( ) / |
| **CW (bottom)** | Space BS Enter : | ABC _ = ; | " < > $ |

(BS = backspace)

### Simulator / シミュレータ

No hardware is needed to try the whole flow: while disconnected, the
GUI's simulator panel drives the engine. Sliders set both wrist rolls
(-90°..90°) and the five finger flexions (0..1); the buttons apply
quick gestures — **Grip**, **Release**, **Neutral**, and the **Key 1–4**
finger combinations. Grip, rotate the wrist sliders, pick a combo,
then Release (or return the rolls to 0) to commit.

ハードウェアがなくても全フローが試せます。未接続時は GUI の
シミュレータパネルがエンジンを駆動します。スライダで両手首の
ロール（-90°..90°）と5指の屈曲（0..1）を設定でき、ボタンでは
**Grip** / **Release** / **Neutral** と **Key 1–4** の指組み合わせを
すぐ適用できます。グリップ→スライダで手首を回転→組み合わせ選択→
Release（またはロールを 0 に戻す）で確定、という流れです。

## Architecture / 構成

```
run.py                        convenience launcher / 簡易ランチャ
src/glove2kb_py/
  layout.py                   3x3 group grid, key definitions (pure data)
  gestures.py                 GestureFrame, zone/grip/combo logic + hysteresis
  engine.py                   input state machine (IDLE / SELECTING, debounce, commit)
  osc.py                      strict OSC 1.0 parser/encoder (bounds-checked)
  vmc_receiver.py             UDP receiver (localhost), VMC bone -> GestureFrame
  simulator.py                GUI-driven and scripted frame transports
  gui.py                      Tkinter app (grid view, output, simulator panel)
  app.py / __main__.py        entry points (`python -m glove2kb_py`)
tests/                        unittest suites (layout, gestures, engine, OSC, VMC/sim)
```

The engine, layout, gestures and OSC modules are pure logic and fully
unit-testable headless; only `gui.py` imports Tkinter.

エンジン・レイアウト・ジェスチャ・OSC の各モジュールは純粋な
ロジックで、画面なしで単体テストできます。Tkinter を import するのは
`gui.py` だけです。

### Conversion approximation / 変換の近似について

VMC bone quaternions are converted with a deliberately simple,
documented approximation: wrist roll is the hand bone's rotation about
its local X axis; finger flexion is the sum of the Proximal /
Intermediate / Distal joint bend angles relative to the hand bone,
normalised by a per-finger maximum (thumb 150°, others 240°) and
clamped to 0..1. Real-world accuracy depends on the sender's
calibration; treat the thresholds in `gestures.py` as tunable.

VMC のボーン四元数からの変換は、意図的に単純化した近似です。
手首ロールは手ボーンのローカル X 軸まわりの回転、指の屈曲は
手ボーンに対する Proximal / Intermediate / Distal 各関節の
曲げ角の合計を、指ごとの最大値（親指 150°、他 240°）で
正規化して 0..1 に丸めたものを使います。実機での精度は送信側の
キャリブレーションに依存するため、`gestures.py` の閾値は
調整前提の値です。

## Security / セキュリティ

* The UDP listener binds to **127.0.0.1 only**; VMC traffic is local.
* The OSC parser is strict and bounds-checked: maximum packet size,
  length/alignment checks on every field, malformed packets are
  dropped and counted, never executed.
* **No dynamic code execution** (`eval`/`exec`) anywhere, and no
  network access other than the VMC UDP listener.
* Configuration is validated: the port must be an integer in 1..65535.

* UDP リスナは **127.0.0.1 のみ**に bind します（VMC はローカル前提）。
* OSC パーサは厳格で境界チェック付きです。最大パケットサイズ、
  全フィールドの長さ・整列チェックを行い、不正なパケットは
  実行せず破棄して数えます。
* `eval` / `exec` などの**動的コード実行は一切ありません**。
  ネットワークアクセスは VMC の UDP リスナのみです。
* 設定値は検証されます。ポートは 1..65535 の整数のみ有効です。

## Tests / テスト

```sh
python3 -m unittest discover -s tests -v
```

Covers layout lookup, gesture thresholds/hysteresis, the engine commit
flow (including typing "hi" with a backspace correction), the OSC
parser (round-trip plus malformed inputs), and VMC conversion /
simulator smoke tests.

レイアウト検索、ジェスチャの閾値とヒステリシス、エンジンの確定
フロー（"hi" の入力とバックスペース修正を含む）、OSC パーサ
（往復変換と不正入力の拒否）、VMC 変換とシミュレータの
スモークテストを含みます。

## References / 参考文献

Research and prior art on low-cost VR text entry that informed the
concept (titles/topics):

* PinchType: Text Entry for Virtual and Augmented Reality
* Flick input with pinch gestures (ピンチによるフリック入力)
* Quikwriting (Ken Perlin)
* DaisywheelJS / the Daisywheel radial text entry interface
* Virtual keyboard input using wrist flexion (手首の屈曲を用いた仮想キーボード)
* 1D crossing-based keyboard input (1次元クロッシング型キーボード)
* Marking menus and pie-menu text entry
* Text Entry in Virtual Reality: A Survey

## License

MIT License — Copyright (c) 2026 Paratap. See [LICENSE](LICENSE).
