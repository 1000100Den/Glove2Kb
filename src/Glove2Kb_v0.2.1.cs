using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using uOSC;
using TMPro;
using System;

public class Glove2Kb : MonoBehaviour
{
    // デバッグUI スライダー指定用 LR別
    public Slider righthandYawRotationSlider; // スライダー 右手-z
    public Slider rightThumbRotationSlider; // スライダー 右親指
    public Slider rightIndexRotationSlider; // スライダー 右人差し指
    public Slider lefthandYawRotationSlider; // スライダー 左手-z
    public Slider rightGripForce; // スライダー 右手グリップ
    public RawImage pointer; // ポインターオブジェクト

    // 手首原点補正用パラメータ　調整可能 LR共用
    public float deadzoneThreshold = 10.0f;
    public float correctionTimeThreshold = 0.2f;
    public float correctionSpeed = 7.2f;
    
    // 指原点補正用パラメータ 調整可能 LR共用
    public float fingerdeadzoneThreshold = 14.0f;
    public float fingerCorrectionTimeThreshold = 0.2f;
    public float fingercorrectionSpeed = 7.2f;

    // 内部変数 LR別
    private float righthandcorrectionTimer = 0.0f;
    private float righthandRotateZ = 0.0f;
    private float lefthandcorrectionTimer = 0.0f;
    private float lefthandRotateZ = 0.0f;
    private float rWristZInput = 0.0f;
    private float rWristZOrigin = 0.0f;
    private float rWristZOriginPrev = 0.0f;
    private float lWristZInput = 0.0f;
    private float lWristZOrigin = 0.0f;
    private float lWristZOriginPrev = 0.0f;

    // 内部変数 指用 LR別
    private float rightThumbInput = 0.0f;
    private float rightThumbRotation = 0.0f;
    private float rightThumbOrigin = 0.0f;
    private float rightIndexInput = 0.0f;
    private float rightIndexRotation = 0.0f;
    private float rightIndexOrigin = 0.0f;
    private float rightMiddleInput = 0.0f;
    private float rightMiddleRotation = 0.0f;
    private float rightMiddleOrigin = 0.0f;
    private float rThumbCorrectionTimer = 0.0f;
    private float rIndexCorrectionTimer = 0.0f;
    private float rMiddleCorrectionTimer = 0.0f;
    private float rthumbOriginprev = 0.0f;
    private float rindexOriginprev = 0.0f;
    private float rmiddleOriginprev = 0.0f;

    // 状態管理用変数
    private bool flag_RWristMoved = false;
    private bool flag_RWristReset = true;
    private bool flag_LWristMoved = false;
    private bool flag_LWristReset = true;
    private bool flag_RightThumbMoved = false;
    private bool flag_RightIndexMoved = false;
    private bool bothFlagsSet = false;
    private bool isGripActive = false;

    // 入力判定用変数　調整可能 LR共用
    public float wristZThresholdMax = 0.32f;
    public float wristZThresholdMin = -0.32f;
    public float rThumbThreshold = 0.24f;
    public float rIndexThreshold = -0.3f;
    public float rGripThreshold = -0.5f;

    // 入力予定文字用変数
    private string charaPlan = "a";

    // 入力文字用変数
    private string testTextInput;
    public TMPro.TMP_Text testText;

    // ポインター移動用変数
    private Vector3 keyPointer = Vector3.zero;

    //状態管理用変数 LR別
    private sbyte rHandAngle; //右手首状態用変数、-1から1
    private sbyte lHandAngle; //左手首状態用変数、-1から1
    private sbyte touchFinger; //指状態用変数、0から2

    void Start()
    {
        // uOSCのメッセージ受信イベントにリスナーを追加
        var uOscServer = FindObjectOfType<uOscServer>();
        if (uOscServer != null)
        {
            uOscServer.onDataReceived.AddListener(OnDataReceived);
        }
        else
        {
            UnityEngine.Debug.LogError("uOscServer not found!");
        }
    }
    void Update()
    {
        //グリップ状態か確認
        isActivatedGrip();
        indexAndThumb();

        if (isGripActive)
        {
            // 左手から入力されているか確認
            isCrossingL();

            if (lHandAngle == 0)// もし左手からの入力が無かった場合以下の処理を行う
            {
                isCrossingR();// 右手から入力されているか確認
            }
            // ポインターを移動させる
            keyPointerMove();
            kbInput();


        }
    }

    // OSCアドレスからボーンの角度情報を受信
    void OnDataReceived(Message message)
    {
        // OSCアドレスがボーンの角度情報であることを確認
        if (message.address == "/VMC/Ext/Bone/Pos")
        {
            // メッセージデータからボーンの名前と角度を取得
            string boneName = (string)message.values[0];
            float rotX = (float)message.values[4];
            float rotY = (float)message.values[5];
            float rotZ = (float)message.values[6];
            float rotW = (float)message.values[7];

            switch (boneName)
            {
                case "RightHand":
                    righthandRotateZ = rotZ * 2.6f;
                    rInputCorrection();
                    righthandYawRotationSlider.value = rWristZInput;
                    break;
                case "RightThumbProximal":
                    rightThumbRotation = rotY * 12;
                    rThumbCorrection();
                    rightThumbRotationSlider.value = rightThumbInput;
                    break;
                case "RightIndexIntermediate":
                    rightIndexRotation = rotY * 64;
                    rIndexCorrection();
                    rightIndexRotationSlider.value = rightIndexInput;
                    break;
                case "RightRingProximal":
                    rightMiddleRotation = rotY * 32;
                    rGripCorrection();
                    rightGripForce.value = rightMiddleInput;
                    break;
                case "LeftHand":
                    lefthandRotateZ = rotX * 2.1f;
                    lInputCorrection();
                    lefthandYawRotationSlider.value = lWristZInput;
                    break;
                default:
                    break;
            }
        }
    }

    // グリップ判定
    void isActivatedGrip()
    {
        if (rightMiddleInput <= rGripThreshold)// 右手の中指の角度が一定以上の場合
        {
            isGripActive = true;// グリップ判定有り
        }
        else if (rightMiddleInput > rGripThreshold)// 右手の中指の角度が一定より小さい場合
        {
            isGripActive = false;// グリップ判定無し
        }
    }

    // 右手首原点補正
    void rInputCorrection()
    {
        if (isGripActive == false)
        {
            // 入力の取得
            rWristZInput = righthandRotateZ;

            // 原点補正を適用
            rWristZInput -= rWristZOrigin;

            // 入力の絶対値がデッドゾーン内か確認
            if (Mathf.Abs(rWristZInput) < deadzoneThreshold)
            {
                // タイマーを更新
                righthandcorrectionTimer += Time.deltaTime;

                // 一定時間範囲内に留まった場合、原点を補正
                if (righthandcorrectionTimer >= correctionTimeThreshold)
                {
                    rWristZOrigin = Mathf.Lerp(rWristZOrigin, rWristZOrigin + rWristZInput, correctionSpeed * Time.deltaTime);
                    righthandcorrectionTimer = 0.0f;  // タイマーをリセット
                }
            }
            else
            {
                // 入力が範囲外ならタイマーをリセット
                righthandcorrectionTimer = 0.0f;
            }
            rWristZOriginPrev = rWristZOrigin;
        }
        else
        {
            rWristZInput = righthandRotateZ;
            rWristZInput -= rWristZOriginPrev;
        }
    }

    // 左手首原点補正
    void lInputCorrection()
    {
        if (isGripActive == false)
        {
            lWristZInput = lefthandRotateZ;

            lWristZInput -= lWristZOrigin;

            if (Mathf.Abs(lWristZInput) < deadzoneThreshold)
            {
                lefthandcorrectionTimer += Time.deltaTime;

                if (lefthandcorrectionTimer >= correctionTimeThreshold)
                {
                    lWristZOrigin = Mathf.Lerp(lWristZOrigin, lWristZOrigin + lWristZInput, correctionSpeed * Time.deltaTime);
                    lefthandcorrectionTimer = 0.0f;
                }
            }
            else
            {
                lefthandcorrectionTimer = 0.0f;
            }
            lWristZOriginPrev = lWristZOrigin;
        }
        else
        {
            lWristZInput = lefthandRotateZ;
            lWristZInput -= lWristZOriginPrev;
        }
    }

    // 右人差し指原点補正
    void rIndexCorrection()
    {
        if (isGripActive == false)
        {
            rightIndexInput = rightIndexRotation;

            rightIndexInput -= rightIndexOrigin;

            if (Mathf.Abs(rightIndexInput) < fingerdeadzoneThreshold)
            {
                rIndexCorrectionTimer += Time.deltaTime;

                if (rIndexCorrectionTimer >= fingerCorrectionTimeThreshold)
                {
                    rightIndexOrigin = Mathf.Lerp(rightIndexOrigin, rightIndexOrigin + rightIndexInput, fingercorrectionSpeed * Time.deltaTime);
                    rIndexCorrectionTimer = 0.0f;
                }
            }
            else
            {
                rIndexCorrectionTimer = 0.0f;
            }
            rindexOriginprev = rightIndexOrigin;
        }
        else
        {
            rightIndexInput = rightIndexRotation;
            rightIndexInput -= rindexOriginprev;
        }
    }

    // 右親指原点補正
    void rThumbCorrection()
    {
        if (isGripActive == false)
        {
            rightThumbInput = rightThumbRotation;

            rightThumbInput -= rightThumbOrigin;

            if (Mathf.Abs(rightThumbInput) < fingerdeadzoneThreshold)
            {
                rThumbCorrectionTimer += Time.deltaTime;

                if (rThumbCorrectionTimer >= fingerCorrectionTimeThreshold)
                {
                    rightThumbOrigin = Mathf.Lerp(rightThumbOrigin, rightThumbOrigin + rightThumbInput, fingercorrectionSpeed * Time.deltaTime);
                    rThumbCorrectionTimer = 0.0f;
                }
            }
            else
            {
                rThumbCorrectionTimer = 0.0f;
            }
            rthumbOriginprev = rightThumbOrigin;
        }
        else
        {
            rightThumbInput = rightThumbRotation;
            rightThumbInput -= rthumbOriginprev;
        }
    }

    // 右グリップ原点補正
    void rGripCorrection()
    {
        if (isGripActive == false)
        {
            rightMiddleInput = rightMiddleRotation;

            rightMiddleInput -= rightMiddleOrigin;

            if (Mathf.Abs(rightMiddleInput) < fingerdeadzoneThreshold)
            {
                rMiddleCorrectionTimer += Time.deltaTime;

                if (rMiddleCorrectionTimer >= fingerCorrectionTimeThreshold)
                {
                    rightMiddleOrigin = Mathf.Lerp(rightMiddleOrigin, rightMiddleOrigin + rightMiddleInput, fingercorrectionSpeed * Time.deltaTime);
                    rMiddleCorrectionTimer = 0.0f;
                }
            }
            else
            {
                rMiddleCorrectionTimer = 0.0f;
            }
            rmiddleOriginprev = rightMiddleOrigin;
        }
        else
        {
            rightMiddleInput = rightMiddleRotation;
            rightMiddleInput -= rmiddleOriginprev;
        }
    }

    // 入力ポインタ移動用
    void keyPointerMove()
    {
        if (lHandAngle != 0)
        {
            keyPointer.x = (-234.0f + (lHandAngle * 156.0f));
            keyPointer.y = -155.42f;
        }
        else if (rHandAngle != 0)
        {
            keyPointer.x = -234.0f;
            keyPointer.y = (-155.42f + (rHandAngle * 156.0f));
        }
        else if (rHandAngle == 0 && lHandAngle == 0)
        {
            keyPointer.x = -234.0f;
            keyPointer.y = -155.42f;
        }
        else if (rHandAngle != 0 && lHandAngle != 0)
        {
            keyPointer.x = -234.0f;
            keyPointer.y = -155.42f;
        }

        pointer.rectTransform.anchoredPosition = new Vector3(keyPointer.x, keyPointer.y,  0.0f);
    }

    // 人差し指と親指の入力判定用
    void indexAndThumb()
    {
        //右人差し指の入力が閾値より大きいかチェック
        if (rightIndexInput > rIndexThreshold)
        {
            flag_RightIndexMoved = true;
            //閾値以下の場合にリセットフラグを立てるフラグをリセット
        }
        else if ((rightIndexInput <= rIndexThreshold))
        {
            flag_RightIndexMoved = false;
        }

        //右親指の入力が閾値より大きいかチェック
        if (rightThumbInput < rThumbThreshold)
        {
            flag_RightThumbMoved = true;
            //閾値以下の場合にリセットフラグを立てるフラグをリセット
        }
        else if (rightThumbInput >= rThumbThreshold)
        {
            flag_RightThumbMoved = false;
        }
    }

    //左手首デコード
    void isCrossingL()
    {
        if (lWristZInput > wristZThresholdMax) //左手の角度を取得し一定より大きいかチェック
        {
            lHandAngle = -1; //左手状態変数に1を代入
            flag_LWristMoved = true; //左手入力フラグにtrueを代入
            flag_LWristReset = false; //左手リセットフラグにfalseを代入
        }
        else if (lWristZInput <= wristZThresholdMax && lWristZInput >= wristZThresholdMin) //左手の角度を取得し一定領域内かチェック
        {
            lHandAngle = 0; //左手状態変数に0を代入
            flag_LWristMoved = false; //左手入力フラグにfalseを代入
            flag_LWristReset = false; //左手リセットフラグにfalseを代入
        }
        else if (lWristZInput < wristZThresholdMin) //左手の角度を取得し一定より小さいかチェック
        {
            lHandAngle = 1; //左手状態変数に-1を代入
            flag_LWristMoved = true; //左手入力フラグにtrueを代入
            flag_LWristReset = false; //左手リセットフラグにfalseを代入
        }

        if (lHandAngle != 0)
        {
            interpreter_gloveToKb();
        }
    }

    //右手首デコード
    void isCrossingR()
    {
        if (rWristZInput > wristZThresholdMax) //右手の角度を取得し一定より大きいかチェック
        {
            rHandAngle = -1; //右手状態変数に1を代入
            flag_RWristMoved = true; //右手入力フラグをtrueに
            flag_RWristReset = false; //右手リセットフラグをfalseに
        }
        else if (rWristZInput <= wristZThresholdMax && rWristZInput >= wristZThresholdMin) //右手の角度を取得し一定領域内かチェック
        {
            rHandAngle = 0; //右手状態変数に0を代入
            flag_RWristMoved = false; //右入力フラグをfalseに
            flag_RWristReset = false; //右手リセットフラグをfalseに
        }
        else if (rWristZInput < wristZThresholdMin) //右手の角度を取得し一定より小さいかチェック
        {
            rHandAngle = 1; //右手状態変数に1を代入
            flag_RWristMoved = true; //右手入力フラグをtrueに
            flag_RWristReset = false; //右手リセットフラグをfalseに
        }

        if (rHandAngle != 0)
        {
            interpreter_gloveToKb();
        }
    }

    // 手首の角度と指の曲げを文字に変換
    void interpreter_gloveToKb()
    {
        //Gripレイヤ
        //Grip=true,Index=false,Thumb=falseの場合、以下の処理を行う
        if (isGripActive && flag_RightIndexMoved && flag_RightThumbMoved)
        {
            // 左手を回転させた場合
            if (lHandAngle == -1)
            {
                charaPlan = "e";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1)
            {
                charaPlan = "t";
                bothFlagsSet = true;
            }
            // 右手を回転させた場合
            if (rHandAngle == -1)
            {
                charaPlan = "a";
                bothFlagsSet = true;
            }
            else if (rHandAngle == 1)
            {
                charaPlan = "o";
                bothFlagsSet = true;
            }
        }
        //Grip+Indexレイヤ
        //Grip=true,Index=true,Thumb=falseの場合、以下の処理を行う
        if (isGripActive && flag_RightIndexMoved != true && flag_RightThumbMoved)
        {
            // 左手を回転させた場合
            if (lHandAngle == -1)
            {
                charaPlan = "i";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1)
            {
                charaPlan = "n";
                bothFlagsSet = true;
            }
            // 右手を回転させた場合
            if (rHandAngle == -1)
            {
                charaPlan = "s";
                bothFlagsSet = true;
            }
            else if (rHandAngle == 1)
            {
                charaPlan = "h";
                bothFlagsSet = true;
            }
        }
        //Grip+Thumbレイヤ
        //Grip=true,Index=false,Thumb=trueの場合、以下の処理を行う
        if (isGripActive && flag_RightIndexMoved && flag_RightThumbMoved != true)
        {
            // 左手を回転させた場合
            if (lHandAngle == -1)
            {
                charaPlan = "r";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1)
            {
                charaPlan = "d";
                bothFlagsSet = true;
            }
            // 右手を回転させた場合
            if (rHandAngle == -1)
            {
                charaPlan = "l";
                bothFlagsSet = true;
            }
            else if (rHandAngle == 1)
            {
                charaPlan = "c";
                bothFlagsSet = true;
            }
        }
        //Grip+Index+Thumbレイヤ
        //Grip=true,Index=true,Thumb=trueの場合、以下の処理を行う
        if (isGripActive && flag_RightIndexMoved == false && flag_RightThumbMoved == false)
        {
            // 左手を回転させた場合
            if (lHandAngle == -1)
            {
                charaPlan = "u";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1)
            {
                charaPlan = "m";
                bothFlagsSet = true;
            }
            //右手を回転させた場合
            if (rHandAngle == -1)
            {
                charaPlan = "w";
                bothFlagsSet = true;
            }
            else if (rHandAngle == 1)
            {
                charaPlan = "f";
                bothFlagsSet = true;
            }
        }
        //人差し指とグリップのフラグがどちらも立っていない場合、以下の処理を行う
        if (isGripActive != true && flag_RightIndexMoved != true && flag_RightThumbMoved != true)
        {
            //何も行わない
        }
    }

    // クロッシング判定およびテキストフィードへの入力
    void kbInput()
    {
        if (lWristZInput <= wristZThresholdMax && lWristZInput >= wristZThresholdMin) //左手の角度を取得し一定領域内かチェック
        {
            flag_LWristReset = true; //左手リセットフラグをtrueに
        }


        if (rWristZInput <= wristZThresholdMax && rWristZInput >= wristZThresholdMin) //右手の角度を取得し一定領域内かチェック
        {
            flag_RWristReset = true; //右手リセットフラグをtrueに
        }

        if (bothFlagsSet)
        {
            if (flag_LWristReset || flag_RWristReset) //左右の手のどちらかのリセットフラグがtrueの場合以下の処理を行う
            {
                if (flag_LWristReset) //左手のリセットフラグがtrueの場合以下の処理を行う
                    if (rHandAngle == 0)
                    {
                        testTextInput += charaPlan;
                        testText.SetText(testTextInput);
                        bothFlagsSet = false;
                    }
                    else
                    {

                    }
                else //右手のリセットフラグがtrueの場合以下の処理を行う
                {
                    if (lHandAngle == 0)
                    {
                        testTextInput += charaPlan;
                        testText.SetText(testTextInput);
                        bothFlagsSet = false;
                    }
                    else
                    {

                    }
                }
            }
        }
        flag_LWristMoved = false;
        flag_RWristMoved = false;
    }
}