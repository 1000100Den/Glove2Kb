using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using uOSC;
using TMPro;
using System;

public class BoneAngleReceiver : MonoBehaviour
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
    private bool flag_RightThumbMoved = false;
    private bool flag_RightIndexMoved = false;
    private bool bothFlagsSet = false;
    private bool flag_RightThumbReset = true;
    private bool flag_RightIndexReset = true;
    private bool isGripActive = false;
    private float rthumbOriginprev = 0.0f;
    private float rindexOriginprev = 0.0f;
    private float rmiddleOriginprev = 0.0f;

    // 入力判定用変数　調整可能 LR共用
    public float wristZThresholdMax = 0.18f;
    public float wristZThresholdMiddleMax = 0.06f;
    public float wristZThresholdMin = -0.18f;
    public float wristZThresholdMiddleMin = -0.06f;
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
    private sbyte lHandAngle; //左手首状態用変数、-2から2
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
        wristPosEncoder();

        keyPointerMove();

        interpreter_Hand2chara();

        character_Input();

        isActivatedGrip();
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
                    righthandRotateZ = rotZ * 2.1f;
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
                    lefthandRotateZ = rotX * 1.7f;
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
        if (rightMiddleInput <= rGripThreshold)
        {
            isGripActive = true;
        }
        else if (rightMiddleInput > rGripThreshold)
        {
            isGripActive = false;
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
        keyPointer.x = (-260.0f + (lHandAngle * 104.0f));
        keyPointer.y = (-231.98f + (rHandAngle * 104.0f));
        pointer.rectTransform.anchoredPosition = new Vector3(keyPointer.x, keyPointer.y,  0.0f);
    }

    // 手首角度エンコード
    void wristPosEncoder()
    {
        if (lWristZInput > wristZThresholdMax)
        {
            //lHandAngle = 2;
            lHandAngle = -2;
        }
        else if (lWristZInput > wristZThresholdMiddleMax && lWristZInput <= wristZThresholdMax)
        {
            //lHandAngle = 1;
            lHandAngle = -1;
        }
        else if (lWristZInput <= wristZThresholdMiddleMax && lWristZInput >= wristZThresholdMiddleMin)
        {
            lHandAngle = 0;
        }
        else if (lWristZInput < wristZThresholdMiddleMin && lWristZInput >= wristZThresholdMin)
        {
            //lHandAngle = -1;
            lHandAngle = 1;
        }
        else if (lWristZInput < wristZThresholdMin)
        {
            //lHandAngle = -2;
            lHandAngle = 2;
        }

        if (rWristZInput > wristZThresholdMax)
        {
            rHandAngle = -1;
        }
        else if (rWristZInput <= wristZThresholdMax && rWristZInput >= wristZThresholdMin)
        {
            rHandAngle = 0;
        }
        else if (rWristZInput < wristZThresholdMin)
        {
            rHandAngle = 1;
        }
    }

    // 手首の角度と指の曲げを文字に変換
    void interpreter_Hand2chara()
    {
        //右人差し指の入力が閾値より大きいかチェック
        if (rightIndexInput > rIndexThreshold)
        {
            flag_RightIndexMoved = true;
            //閾値以下の場合にリセットフラグを立てるフラグをリセット
            flag_RightIndexReset = false;
        }
        //右親指の入力が閾値より大きいかチェック
        if (rightThumbInput > rThumbThreshold)
        {
            flag_RightThumbMoved = true;
            //閾値以下の場合にリセットフラグを立てるフラグをリセット
            flag_RightThumbReset = false;
        }
        //Indexレイヤ
        //親指と人差し指のフラグが立っている場合、以下の処理を行う
        if (flag_RightIndexMoved && flag_RightThumbMoved)
        {
            if (lHandAngle == -2 && rHandAngle == 1)
            {
                charaPlan = "v";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -1 && rHandAngle == 1)
            {
                charaPlan = "g";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 0 && rHandAngle == 1)
            {
                charaPlan = "s";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1 && rHandAngle == 1)
            {
                charaPlan = "p";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 2 && rHandAngle == 1)
            {
                charaPlan = "w";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -2 && rHandAngle == 0)
            {
                charaPlan = "u";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -1 && rHandAngle == 0)
            {
                charaPlan = "i";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 0 && rHandAngle == 0)
            {
                charaPlan = "e";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1 && rHandAngle == 0)
            {
                charaPlan = "a";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 2 && rHandAngle == 0)
            {
                charaPlan = "l";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -2 && rHandAngle == -1)
            {
                //BackSpace
                if (testTextInput.Length != 0)
                {
                    testTextInput = testTextInput.Substring(0, testTextInput.Length - 1);
                }
                bothFlagsSet = true;
            }
            else if (lHandAngle == -1 && rHandAngle == -1)
            {
                charaPlan = "j";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 0 && rHandAngle == -1)
            {
                charaPlan = "r";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1 && rHandAngle == -1)
            {
                charaPlan = "q";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 2 && rHandAngle == -1)
            {
                // スペースキー
                charaPlan = " ";
                bothFlagsSet = true;
            }
        }
        //親指と人差し指のフラグが立っている場合、以下の処理を行う
        if (flag_RightThumbMoved && flag_RightIndexMoved == false)
        {
            if (lHandAngle == -2 && rHandAngle == 1)
            {
                charaPlan = "k";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -1 && rHandAngle == 1)
            {
                charaPlan = "y";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 0 && rHandAngle == 1)
            {
                charaPlan = "h";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1 && rHandAngle == 1)
            {
                charaPlan = "b";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 2 && rHandAngle == 1)
            {
                charaPlan = "f";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -2 && rHandAngle == 0)
            {
                charaPlan = "m";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -1 && rHandAngle == 0)
            {
                charaPlan = "n";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 0 && rHandAngle == 0)
            {
                charaPlan = "t";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1 && rHandAngle == 0)
            {
                charaPlan = "o";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 2 && rHandAngle == 0)
            {
                charaPlan = "c";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -2 && rHandAngle == -1)
            {
                //ハイフン
                charaPlan = "-";
                bothFlagsSet = true;
            }
            else if (lHandAngle == -1 && rHandAngle == -1)
            {
                charaPlan = "x";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 0 && rHandAngle == -1)
            {
                charaPlan = "d";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 1 && rHandAngle == -1)
            {
                charaPlan = "z";
                bothFlagsSet = true;
            }
            else if (lHandAngle == 2 && rHandAngle == -1)
            {
                //エンター
                //改行
                testTextInput = testTextInput + Environment.NewLine;
                bothFlagsSet = true;
            }
        }
        //人差し指とグリップのフラグがどちらも立っていない場合、以下の処理を行う
        if (flag_RightIndexMoved = false && flag_RightThumbMoved == false)
        {
            //全フラグを初期化
            //flag_RightIndexReset = true;
            //flag_RightThumbReset = true;
            //何も行わない
        }
    }

    // テキストフィードに入力
    void character_Input()
    {
        //右親指の入力が閾値以下かチェック
        if (rightThumbInput <= rThumbThreshold)
        {
        flag_RightThumbReset = true;
        }

        //右人差し指の入力が閾値以下かチェック
        if (rightIndexInput <= rIndexThreshold)
        {
            flag_RightIndexReset = true;
        }

        // 両方のフラグが立った状態で、右親指と人差し指が閾値以下の場合、以下の処理を行う
        if (bothFlagsSet && flag_RightThumbReset && flag_RightIndexReset == false)
        {
            testTextInput += charaPlan;
            testText.SetText(testTextInput);
            bothFlagsSet = false; // 出力した後、フラグをリセット
        }

        // 両方のフラグが立った状態で、親指とグリップが閾値以下の場合、以下の処理を行う
        if (bothFlagsSet && flag_RightThumbReset && flag_RightIndexReset)
        {
            testTextInput += charaPlan;
            testText.SetText(testTextInput);
            bothFlagsSet = false; // 出力した後、フラグをリセット
        }

        flag_RightIndexMoved = false;
        flag_RightThumbMoved = false;
    }
}
