# Glove2Kb

## Overview - 概要

Demonstration of a hand gesture keyboard aimed at inputting with fewer steps/lower input costs. It uses ContactGlove's angular velocity sensor and finger tracking.

[here for the pre-built demonstration app](https://github.com/1000100Den/Glove2Kb/releases)

省ステップ/省移動コストによる入力を目標としたハンドジェスチャキーボードのデモ。ContactGloveの角速度センサとフィンガートラッキングを用いています。

[リリースページ(ビルド済みデモンストレーションアプリのダウンロードはこちらから)](https://github.com/1000100Den/Glove2Kb/releases)

- Glove2Kb v0.1.2

![Glove2Kb_Operation_0001](https://github.com/1000100Den/Glove2Kb/assets/52491146/92d56092-18d7-4669-8abb-758cd297f3f1)

- Glove2Kb v0.2.0

![Glove2Kb_Operation_0002](https://github.com/1000100Den/Glove2Kb/assets/52491146/561c08ec-fc06-47b1-a95e-315a8f8771e9)

- Glove2Kb v0.4.0

![Glove2Kb_Operation_0003](https://github.com/1000100Den/Glove2Kb/assets/52491146/a1b03b6a-eca8-4752-92e5-4345ea7e1f63)

- Glove2Kb v0.5.0

![Glove2Kb_Operation_0006](https://github.com/1000100Den/Glove2Kb/assets/52491146/33a993da-4b40-46bd-ab36-31a4493fc458)


## Introduction - 導入

1. DivingStationを起動し、トップの設定（歯車のマーク）をクリックします。

2. (ポート設定)の(VirtualMotionCaptureポート)の横の数字を控えておきます。

3. (VirtualMotionCaptureProtocolで送信)のチェックボックスにチェックを入れます。

4. Glove2Kb.exeを起動後、画面上部のPort Numberと書かれているテキストフィードに2.で控えた数字を入力し、エンターキーを押します。

## Operation instructions - 操作説明

![Glove2Kb_Operation (1)](https://github.com/1000100Den/Glove2Kb/assets/52491146/a89b49ac-38ce-4130-9ed7-ff741b81f40f)

There are a total of five types of hand gestures motion used for operation in Glove2Kb: rotation of the left hand/right hand, flexion of the right thumb/right index finger, and flexion (grip) of the right middle finger, ring finger, and little finger.

I will explain using the image above.

For example, if you want to input (p), 1. Bend (grip) the middle finger, ring finger, and little finger of your right hand, 2. Then rotate your right wrist clockwise, your left wrist "small clockwise", and 3. While holding the p/b block, bend and straighten your thumb to input p to the text feed.

Glove2Kbにおいて操作に用いるハンドジェスチャは、左手/右手の回転、右親指/右人差し指の屈曲、右中指-薬指-小指の屈曲（グリップ）の合計5種類となります。

The rough operating procedures for v0.1.2 and v0.2.1 are as follows.

v0.1.2とv0.2.1の大まかな操作手順はそれぞれ以下の様になります

- v0.1.2

[English]

1. Bend your right middle finger, ring finger, and little finger  (grip gesture)  to enter input mode.
2. Rotate your right wrist to select the top and bottom rows, and rotate your left wrist to select the left and right columns.
3. Select input layer by flexing or non-flexing right index finger
4. Enter characters with the flexion of your right thumb.

[日本語]

1. 右中指-薬指-小指の屈曲（グリップ）を行い入力モードに入ります
2. 右手首の回転で上下の行を、左手首の回転で左右の列を選択します
3. 右人差し指の屈曲/非屈曲で入力レイヤーを選択します
4. 右親指の屈曲で入力を行います。

上記の画像を例に説明いたします。

例えば(p)を入力したい場合、まず右手中指-薬指-小指の屈曲（グリップ）を行い、その後右手首を右回りに、左手首を右回りに"小さく"回転させ、ポインターをp/bのブロックに持って行った状態で、親指を曲げ伸ばしする事でpの入力がテキストフィードへと行われます。

- v0.2.1

[English]

1. Bend your right middle finger, ring finger, and little finger (grip gesture) to enter input mode.
2. Rotate your right wrist to select the top and bottom keys group, and rotate your left wrist to select the left and right keys group.
3. Select a key in a group of keys using a combination of right thumb/right index finger flexion and right middle finger-ring finger-little finger flexion (grip gesture)
(From the keys at the bottom of the key group: grip, grip + index finger, grip + thumb, grip + index finger + thumb.)
4. Undo wrist rotation and enter character.

[日本語]

1. 右中指-薬指-小指の屈曲（グリップ）を行い入力モードに入ります
2. 右手首の回転で上下のキー群を、左手首の回転で左右のキー群を選択します
3. 右親指/右人差し指の屈曲、右中指-薬指-小指の屈曲（グリップ）の組み合わせでキー群の中のキーを選びます
（キー群の下のキーからグリップ、グリップ+人差し指、グリップ+親指、グリップ+人差し指+親指となっています）
4. 手首の回転を戻して入力を行います

## Developers - 開発者向け

## References - 参考文献

We would like to introduce the previous research that we used as reference during development.

参考とさせていただいた文献、サイトをご紹介いたします。

- VRでの画面占有を考慮した文字入力高速化の研究
  
- VRにおけるピンチ動作を用いたフリック入力手法に関する研究
  
- MR環境における文字入力手法の開発
  
- 透過型HMDにおけるフリック入力の検討
  
- 仮想現実空間でのピンチ動作を用いた覚えやすい文字入力手法
  
- HMD前面を用いたVR用フリック文字入力手法
  
- HMD前面を用いたVR用フリック文字入力手法の予備評価
  
- クロッシングによる選択を用いた仮想現実向けの1次元キーボード
  
- クロッシングによる選択を用いた表示面積の小さい仮想現実向け1次元キーボード
  
- 手首の屈曲および伸展により操作される仮想キーボードの提案
  
- 仮想キーボードの姿勢および形状が入力性能および主観評価に与える影響の調査
  
- 日常利用の拡張現実感環境におけるタッチタイピング可能な文字入力システム
  
- 立体キーボードを用いたVR向け文字入力手法
  
- Quikwriting: Continuous Stylus-based Text Entry
  
- PinchType: Text Entry for Virtual and Augmented Reality Using Comfortable Thumb to Fingertip Pinches
  
- [「どうぶつの森」の文字入力方式から学ぶUX改善の重要性 - kininarium.](https://kininarium.hateblo.jp/entry/animal_crossing_input_method)
  
- [【VR開発】VRならではの日本語入力を作ってみた - Mogura VR News](https://www.moguravr.com/jpn-vr/)
  
- [DaisywheelJS, a UI tool for web developers that support the Gamepad API](https://likethemammal.github.io/daisywheeljs/)
  
- [Touch typing on a gamepad | krisfris.github.io](https://krisfris.com/2020/07/07/touch-typing-on-a-gamepad.html)
  
- [VR中の入力手法について調査してみた【31/31記事目】 - 机上の空論主義者](https://umeboshi-lab.com/entry/2021/05/31/232752)

- [HandTrackingでのキーボード入力集め - トマシープが学ぶ](https://bibinbaleo.hatenablog.com/entry/2019/12/30/230156)

## license - ライセンス

Distributed under the MIT License. Please see LICENSE for details.

MITライセンスに基づいて配布されます。詳細につきましては、LICENSEを参照してください。

## Contact - 問い合わせ先

For support or questions, please contact us below.

サポートやご質問については、下記までお問い合わせください。

- Author Xaccount: https://x.com/Paratap_VRC
- Author e-mail: nebuibuibui@gmail.com
