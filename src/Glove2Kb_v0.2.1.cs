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
    private float rthumbOriginprev = 0.0f;
    private float rindexOriginprev = 0.0f;
    private float rmiddleOriginprev = 0.0f;

    // ��ԊǗ��p�ϐ�
    private bool flag_RWristMoved = false;
    private bool flag_RWristReset = true;
    private bool flag_LWristMoved = false;
    private bool flag_LWristReset = true;
    private bool flag_RightThumbMoved = false;
    private bool flag_RightIndexMoved = false;
    private bool bothFlagsSet = false;
    private bool isGripActive = false;

    // ���͔���p�ϐ��@�����\ LR���p
    public float wristZThresholdMax = 0.32f;
    public float wristZThresholdMin = -0.32f;
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
    private sbyte lHandAngle; //������ԗp�ϐ��A-1����1
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
        //�O���b�v��Ԃ��m�F
        isActivatedGrip();
        indexAndThumb();

        if (isGripActive)
        {
            // ���肩����͂���Ă��邩�m�F
            isCrossingL();

            if (lHandAngle == 0)// �������肩��̓��͂����������ꍇ�ȉ��̏������s��
            {
                isCrossingR();// �E�肩����͂���Ă��邩�m�F
            }
            // �|�C���^�[���ړ�������
            keyPointerMove();
            kbInput();


        }
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

    // �O���b�v����
    void isActivatedGrip()
    {
        if (rightMiddleInput <= rGripThreshold)// �E��̒��w�̊p�x�����ȏ�̏ꍇ
        {
            isGripActive = true;// �O���b�v����L��
        }
        else if (rightMiddleInput > rGripThreshold)// �E��̒��w�̊p�x������菬�����ꍇ
        {
            isGripActive = false;// �O���b�v���薳��
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

    // �l�����w�Ɛe�w�̓��͔���p
    void indexAndThumb()
    {
        //�E�l�����w�̓��͂�臒l���傫�����`�F�b�N
        if (rightIndexInput > rIndexThreshold)
        {
            flag_RightIndexMoved = true;
            //臒l�ȉ��̏ꍇ�Ƀ��Z�b�g�t���O�𗧂Ă�t���O�����Z�b�g
        }
        else if ((rightIndexInput <= rIndexThreshold))
        {
            flag_RightIndexMoved = false;
        }

        //�E�e�w�̓��͂�臒l���傫�����`�F�b�N
        if (rightThumbInput < rThumbThreshold)
        {
            flag_RightThumbMoved = true;
            //臒l�ȉ��̏ꍇ�Ƀ��Z�b�g�t���O�𗧂Ă�t���O�����Z�b�g
        }
        else if (rightThumbInput >= rThumbThreshold)
        {
            flag_RightThumbMoved = false;
        }
    }

    //�����f�R�[�h
    void isCrossingL()
    {
        if (lWristZInput > wristZThresholdMax) //����̊p�x���擾�������傫�����`�F�b�N
        {
            lHandAngle = -1; //�����ԕϐ���1����
            flag_LWristMoved = true; //������̓t���O��true����
            flag_LWristReset = false; //���胊�Z�b�g�t���O��false����
        }
        else if (lWristZInput <= wristZThresholdMax && lWristZInput >= wristZThresholdMin) //����̊p�x���擾�����̈�����`�F�b�N
        {
            lHandAngle = 0; //�����ԕϐ���0����
            flag_LWristMoved = false; //������̓t���O��false����
            flag_LWristReset = false; //���胊�Z�b�g�t���O��false����
        }
        else if (lWristZInput < wristZThresholdMin) //����̊p�x���擾������菬�������`�F�b�N
        {
            lHandAngle = 1; //�����ԕϐ���-1����
            flag_LWristMoved = true; //������̓t���O��true����
            flag_LWristReset = false; //���胊�Z�b�g�t���O��false����
        }

        if (lHandAngle != 0)
        {
            interpreter_gloveToKb();
        }
    }

    //�E���f�R�[�h
    void isCrossingR()
    {
        if (rWristZInput > wristZThresholdMax) //�E��̊p�x���擾�������傫�����`�F�b�N
        {
            rHandAngle = -1; //�E���ԕϐ���1����
            flag_RWristMoved = true; //�E����̓t���O��true��
            flag_RWristReset = false; //�E�胊�Z�b�g�t���O��false��
        }
        else if (rWristZInput <= wristZThresholdMax && rWristZInput >= wristZThresholdMin) //�E��̊p�x���擾�����̈�����`�F�b�N
        {
            rHandAngle = 0; //�E���ԕϐ���0����
            flag_RWristMoved = false; //�E���̓t���O��false��
            flag_RWristReset = false; //�E�胊�Z�b�g�t���O��false��
        }
        else if (rWristZInput < wristZThresholdMin) //�E��̊p�x���擾������菬�������`�F�b�N
        {
            rHandAngle = 1; //�E���ԕϐ���1����
            flag_RWristMoved = true; //�E����̓t���O��true��
            flag_RWristReset = false; //�E�胊�Z�b�g�t���O��false��
        }

        if (rHandAngle != 0)
        {
            interpreter_gloveToKb();
        }
    }

    // ���̊p�x�Ǝw�̋Ȃ��𕶎��ɕϊ�
    void interpreter_gloveToKb()
    {
        //Grip���C��
        //Grip=true,Index=false,Thumb=false�̏ꍇ�A�ȉ��̏������s��
        if (isGripActive && flag_RightIndexMoved && flag_RightThumbMoved)
        {
            // �������]�������ꍇ
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
            // �E�����]�������ꍇ
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
        //Grip+Index���C��
        //Grip=true,Index=true,Thumb=false�̏ꍇ�A�ȉ��̏������s��
        if (isGripActive && flag_RightIndexMoved != true && flag_RightThumbMoved)
        {
            // �������]�������ꍇ
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
            // �E�����]�������ꍇ
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
        //Grip+Thumb���C��
        //Grip=true,Index=false,Thumb=true�̏ꍇ�A�ȉ��̏������s��
        if (isGripActive && flag_RightIndexMoved && flag_RightThumbMoved != true)
        {
            // �������]�������ꍇ
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
            // �E�����]�������ꍇ
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
        //Grip+Index+Thumb���C��
        //Grip=true,Index=true,Thumb=true�̏ꍇ�A�ȉ��̏������s��
        if (isGripActive && flag_RightIndexMoved == false && flag_RightThumbMoved == false)
        {
            // �������]�������ꍇ
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
            //�E�����]�������ꍇ
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
        //�l�����w�ƃO���b�v�̃t���O���ǂ���������Ă��Ȃ��ꍇ�A�ȉ��̏������s��
        if (isGripActive != true && flag_RightIndexMoved != true && flag_RightThumbMoved != true)
        {
            //�����s��Ȃ�
        }
    }

    // �N���b�V���O���肨��уe�L�X�g�t�B�[�h�ւ̓���
    void kbInput()
    {
        if (lWristZInput <= wristZThresholdMax && lWristZInput >= wristZThresholdMin) //����̊p�x���擾�����̈�����`�F�b�N
        {
            flag_LWristReset = true; //���胊�Z�b�g�t���O��true��
        }


        if (rWristZInput <= wristZThresholdMax && rWristZInput >= wristZThresholdMin) //�E��̊p�x���擾�����̈�����`�F�b�N
        {
            flag_RWristReset = true; //�E�胊�Z�b�g�t���O��true��
        }

        if (bothFlagsSet)
        {
            if (flag_LWristReset || flag_RWristReset) //���E�̎�̂ǂ��炩�̃��Z�b�g�t���O��true�̏ꍇ�ȉ��̏������s��
            {
                if (flag_LWristReset) //����̃��Z�b�g�t���O��true�̏ꍇ�ȉ��̏������s��
                    if (rHandAngle == 0)
                    {
                        testTextInput += charaPlan;
                        testText.SetText(testTextInput);
                        bothFlagsSet = false;
                    }
                    else
                    {

                    }
                else //�E��̃��Z�b�g�t���O��true�̏ꍇ�ȉ��̏������s��
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