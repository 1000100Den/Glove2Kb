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
    // �f�o�b�OUI �X���C�_�[�w��p LR��
    public Slider righthandYawRotationSlider; // �X���C�_�[ �E��-z
    public Slider rightThumbRotationSlider; // �X���C�_�[ �E�e�w
    public Slider rightIndexRotationSlider; // �X���C�_�[ �E�l�����w
    public Slider lefthandYawRotationSlider; // �X���C�_�[ ����-z
    public Slider rightGripForce; // �X���C�_�[ �E��O���b�v
    public RawImage pointer; // �|�C���^�[�I�u�W�F�N�g

    // ��񌴓_�␳�p�p�����[�^�@�����\ LR���p
    public float deadzoneThreshold = 10.0f;
    public float correctionTimeThreshold = 0.2f;
    public float correctionSpeed = 7.2f;
    
    // �w���_�␳�p�p�����[�^ �����\ LR���p
    public float fingerdeadzoneThreshold = 14.0f;
    public float fingerCorrectionTimeThreshold = 0.2f;
    public float fingercorrectionSpeed = 7.2f;

    // �����ϐ� LR��
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

    // �����ϐ� �w�p LR��
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

    // ���͔���p�ϐ��@�����\ LR���p
    public float wristZThresholdMax = 0.18f;
    public float wristZThresholdMiddleMax = 0.06f;
    public float wristZThresholdMin = -0.18f;
    public float wristZThresholdMiddleMin = -0.06f;
    public float rThumbThreshold = 0.24f;
    public float rIndexThreshold = -0.3f;
    public float rGripThreshold = -0.5f;

    // ���͗\�蕶���p�ϐ�
    private string charaPlan = "a";

    // ���͕����p�ϐ�
    private string testTextInput;
    public TMPro.TMP_Text testText;

    // �|�C���^�[�ړ��p�ϐ�
    private Vector3 keyPointer = Vector3.zero;

    //��ԊǗ��p�ϐ� LR��
    private sbyte rHandAngle; //�E����ԗp�ϐ��A-1����1
    private sbyte lHandAngle; //������ԗp�ϐ��A-2����2
    private sbyte touchFinger; //�w��ԗp�ϐ��A0����2

    void Start()
    {
        // uOSC�̃��b�Z�[�W��M�C�x���g�Ƀ��X�i�[��ǉ�
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

    // OSC�A�h���X����{�[���̊p�x������M
    void OnDataReceived(Message message)
    {
        // OSC�A�h���X���{�[���̊p�x���ł��邱�Ƃ��m�F
        if (message.address == "/VMC/Ext/Bone/Pos")
        {
            // ���b�Z�[�W�f�[�^����{�[���̖��O�Ɗp�x���擾
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

    // �O���b�v����
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

    // �E��񌴓_�␳
    void rInputCorrection()
    {
        if (isGripActive == false)
        {
            // ���͂̎擾
            rWristZInput = righthandRotateZ;

            // ���_�␳��K�p
            rWristZInput -= rWristZOrigin;

            // ���͂̐�Βl���f�b�h�]�[�������m�F
            if (Mathf.Abs(rWristZInput) < deadzoneThreshold)
            {
                // �^�C�}�[���X�V
                righthandcorrectionTimer += Time.deltaTime;

                // ��莞�Ԕ͈͓��ɗ��܂����ꍇ�A���_��␳
                if (righthandcorrectionTimer >= correctionTimeThreshold)
                {
                    rWristZOrigin = Mathf.Lerp(rWristZOrigin, rWristZOrigin + rWristZInput, correctionSpeed * Time.deltaTime);
                    righthandcorrectionTimer = 0.0f;  // �^�C�}�[�����Z�b�g
                }
            }
            else
            {
                // ���͂��͈͊O�Ȃ�^�C�}�[�����Z�b�g
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

    // ����񌴓_�␳
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

    // �E�l�����w���_�␳
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

    // �E�e�w���_�␳
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

    // �E�O���b�v���_�␳
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

    // ���̓|�C���^�ړ��p
    void keyPointerMove()
    {
        keyPointer.x = (-260.0f + (lHandAngle * 104.0f));
        keyPointer.y = (-231.98f + (rHandAngle * 104.0f));
        pointer.rectTransform.anchoredPosition = new Vector3(keyPointer.x, keyPointer.y,  0.0f);
    }

    // ���p�x�G���R�[�h
    void wristPosEncoder()
    {
        if (lWristZInput > wristZThresholdMax)
        {
            lHandAngle = 2;
        }
        else if (lWristZInput > wristZThresholdMiddleMax && lWristZInput <= wristZThresholdMax)
        {
            lHandAngle = 1;
        }
        else if (lWristZInput <= wristZThresholdMiddleMax && lWristZInput >= wristZThresholdMiddleMin)
        {
            lHandAngle = 0;
        }
        else if (lWristZInput < wristZThresholdMiddleMin && lWristZInput >= wristZThresholdMin)
        {
            lHandAngle = -1;
        }
        else if (lWristZInput < wristZThresholdMin)
        {
            lHandAngle = -2;
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

    // ���̊p�x�Ǝw�̋Ȃ��𕶎��ɕϊ�
    void interpreter_Hand2chara()
    {
        //�E�l�����w�̓��͂�臒l���傫�����`�F�b�N
        if (rightIndexInput > rIndexThreshold)
        {
            flag_RightIndexMoved = true;
            //臒l�ȉ��̏ꍇ�Ƀ��Z�b�g�t���O�𗧂Ă�t���O�����Z�b�g
            flag_RightIndexReset = false;
        }
        //�E�e�w�̓��͂�臒l���傫�����`�F�b�N
        if (rightThumbInput > rThumbThreshold)
        {
            flag_RightThumbMoved = true;
            //臒l�ȉ��̏ꍇ�Ƀ��Z�b�g�t���O�𗧂Ă�t���O�����Z�b�g
            flag_RightThumbReset = false;
        }
        //Index���C��
        //�e�w�Ɛl�����w�̃t���O�������Ă���ꍇ�A�ȉ��̏������s��
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
                // �X�y�[�X�L�[
                charaPlan = " ";
                bothFlagsSet = true;
            }
        }
        //�e�w�Ɛl�����w�̃t���O�������Ă���ꍇ�A�ȉ��̏������s��
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
                //�n�C�t��
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
                //�G���^�[
                //���s
                testTextInput = testTextInput + Environment.NewLine;
                bothFlagsSet = true;
            }
        }
        //�l�����w�ƃO���b�v�̃t���O���ǂ���������Ă��Ȃ��ꍇ�A�ȉ��̏������s��
        if (flag_RightIndexMoved = false && flag_RightThumbMoved == false)
        {
            //�S�t���O��������
            //flag_RightIndexReset = true;
            //flag_RightThumbReset = true;
            //�����s��Ȃ�
        }
    }

    // �e�L�X�g�t�B�[�h�ɓ���
    void character_Input()
    {
        //�E�e�w�̓��͂�臒l�ȉ����`�F�b�N
        if (rightThumbInput <= rThumbThreshold)
        {
        flag_RightThumbReset = true;
        }

        //�E�l�����w�̓��͂�臒l�ȉ����`�F�b�N
        if (rightIndexInput <= rIndexThreshold)
        {
            flag_RightIndexReset = true;
        }

        // �����̃t���O����������ԂŁA�E�e�w�Ɛl�����w��臒l�ȉ��̏ꍇ�A�ȉ��̏������s��
        if (bothFlagsSet && flag_RightThumbReset && flag_RightIndexReset == false)
        {
            testTextInput += charaPlan;
            testText.SetText(testTextInput);
            bothFlagsSet = false; // �o�͂�����A�t���O�����Z�b�g
        }

        // �����̃t���O����������ԂŁA�e�w�ƃO���b�v��臒l�ȉ��̏ꍇ�A�ȉ��̏������s��
        if (bothFlagsSet && flag_RightThumbReset && flag_RightIndexReset)
        {
            testTextInput += charaPlan;
            testText.SetText(testTextInput);
            bothFlagsSet = false; // �o�͂�����A�t���O�����Z�b�g
        }

        flag_RightIndexMoved = false;
        flag_RightThumbMoved = false;
    }
}