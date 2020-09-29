/*  <Handles arm and hand movements.>
    Copyright (C) 2020  Christian Kaltschmidt <c.kaltschmidt@gmx.de>

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using UnityEngine;
using System.Collections;
using AssemblyCSharp;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UI;

public class UserUpdate : MonoBehaviour
{

    public byte manualMovementSpeed = 10;

    //Server
    public string Ip = "127.0.0.1";
    public int Port = 4;

    // Main arm.
    public GameObject MainArm;
    private Animator animArm;
    private string MainArmLayer = "Default";
    public bool RotateArm = false;

    // TAC arm.
    public GameObject TACArm;
    private Animator animTACArm;
    private bool TACArmSkinR;
    //private GameObject TACArm;
    public static TCPServer serverInstance = null;

    //Bloked movment
    public static List<HandMovement.Movement> BlockedMovment = new List<HandMovement.Movement>();

    //Main arm state
    public static List<DOF> DOF_ArmList = new List<DOF>();
    //Main arm state
    private static List<DOF> DOF_TAC_ArmList = new List<DOF>();
    //List of different close movment, grasp, close, key....
    private static List<Byte> CloseMovement = new List<Byte>();

    //Movment Range
    private static int Range = 200; // 500

    //Error tolerance for the TAC test
    private static float Allowance = 0.1F;
    public  bool StartTAC = false; //was static before
    public static bool HideTACArm = true;
    private bool GlowArm = false;

    //MuscularActivityMap property
    private Heatmap Mam;
    public bool MuscularActivityMap = false;
    [Tooltip("Choose from {jet,hot,autumn,summer,winter,spring,cool,pink,hsv,copper,bone,gray,parula}")]
    private string colormap = "jet";
    private float alpha = 0.7f;
    private float cubeheight = 1.0f;
    public bool map3D = true;
    public bool innerGrowing = false;
    private bool UpdateMuscularActivityMap = true;
    private byte[] CurrentMuscularActivityMap;

    //movement of hand
    private float deltaBetweenMovements = 0.1f;
    private float deltaTimeFactor = 1f;
    private bool blockAnimation = false;

    //current movement and current force
    public float currentForce = 0;				   
    public HandMovement.Movement currentMovement = HandMovement.Movement.Error;
    public bool currentlyGraspingObject = false;
    public bool usePalmarGrasp = false;

    public bool UseManualControl = false;

    [Tooltip("Switches the manual control between moving the main-limb and the TAC-limb")]
    public bool MoveTACLimbManually = false;

    public Text counterTextLeft, counterTextRight;
    private float seconds, minutes;


    public static event EventMotionHand OnMotion;

    /// <summary>
    /// initialization.
    /// </summary>
    void Start()
    {
        //Get Server Instance 
        if (serverInstance == null)
        {
            serverInstance = TCPServer.getInstance(Ip, Port);
        }

        //register to IncomingMessage event
        serverInstance.incomingMessage += new EventIncomingMessage(HandleIncomingMessage);

        //Get Animator
        if (MainArm== null)
        {
            MainArm = GameObject.FindGameObjectWithTag("MainLimbAnimatorObject");
        }
            
        animArm = MainArm.GetComponent<Animator>();
        if (TACArm == null)
        {
            TACArm = GameObject.FindGameObjectWithTag("TACLimbAnimatorObject");
        }
        if(TACArm!=null)
        {
            animTACArm = TACArm.GetComponent<Animator>();

        }
        TACArm.GetComponentInChildren<SkinnedMeshRenderer>().enabled = !HideTACArm;
        TACArmSkinR = false;

        //Create Mam
        Mam = new Heatmap(colormap, alpha, cubeheight, map3D, MainArm);

        //Set DOF LIST for main Arm
        DOF_ArmList.Add(new DOF("f_Open", "f_Close", 0x09, -1.0F, 1.0F, Range));
        DOF_ArmList.Add(new DOF("f_Supination", "f_Pronation", 0x08, -1.0F, 1.0F, Range));
        DOF_ArmList.Add(new DOF("f_Extend", "f_Flex", 0x07, -1.0F, 1.0F, Range));
        DOF_ArmList.Add(new DOF("f_Ulnar", "f_Radial", 0x0F, 0.0F, 1.0F, Range)); //minValue is 0 because a fully extended arm is the initial position. The currentValue cannot go <0 and cause some delay when flexing again.
        DOF_ArmList.Add(new DOF("f_Pincer", "f_Rest", 0x0C, 0.0F, 1.0F, Range));
        DOF_ArmList.Add(new DOF("f_IndexFinger", "f_Rest", 0x0A, 0.0F, 1.0F, Range));
        DOF_ArmList.Add(new DOF("f_Key", "f_Rest", 0x0D, 0.0F, 1.0F, Range));
        DOF_ArmList.Add(new DOF("f_Rest", "f_Rest", 0x00, 0.0F, 0.0F, Range));

        //Set DOF LIST for the TAC Arm
        DOF_TAC_ArmList.Add(new DOF("f_Open", "f_Close", 0x09, -1.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_Supination", "f_Pronation", 0x08, -1.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_Extend", "f_Flex", 0x07, -1.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_Ulnar", "f_Radial", 0x0F, 0.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_Pincer", "f_Rest", 0x0C, 0.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_IndexFinger", "f_Rest", 0x0A, 0.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_Key", "f_Rest", 0x0D, 0.0F, 1.0F, Range));
        DOF_TAC_ArmList.Add(new DOF("f_Rest", "f_Rest", 0x00, 0.0F, 0.0F, Range));

        //Set ID-list of secondary-close movments
        CloseMovement.Add(0x0C);
        CloseMovement.Add(0x0D);
        CloseMovement.Add(0x0A);
        
    }

    private void HandleIncomingMessage(TCPServer.ByteCast msg, bool manualControl = false)
    {
        DOF CloseControl = DOF_ArmList.Where(x => x.ID == 0x09).ElementAt(0);
        DOF PincerControl = DOF_ArmList.Where(x => x.ID == 0x0C).ElementAt(0);
        DOF IndexControl = DOF_ArmList.Where(x => x.ID == 0x0A).ElementAt(0);
        DOF KeyControl = DOF_ArmList.Where(x => x.ID == 0x0D).ElementAt(0);
        print(msg.val1.ToString());
        if (!UseManualControl || manualControl || msg.Operation!=0x01)
        {
            switch (msg.Operation)
            {
                //Control main arm
                case 0x01:
                    if (StartTAC)
                    {
                        GlowArm = true;
                    }

                    //update heatmap only if heat is true to prevent errors
                    if (MuscularActivityMap && msg.heatmap != null)
                    {
                        UpdateMuscularActivityMap = true;
                        CurrentMuscularActivityMap = msg.heatmap;
                    }

                    DOF dofArm = DOF_ArmList.Where(x => x.ID == msg.val1).ElementAt(0);
                    if (!BlockedMovment.Contains(serverInstance.movement))
                    {
                        if (CloseMovement.Contains(msg.val1) && CloseControl.CurrentValue > 0)
                        {
                            CloseControl.Move(msg.val3, 0x0);
                        }
                        else if (msg.val1 == 0x09 && msg.val2 == 0x01 && (PincerControl.CurrentValue > 0 || CloseControl.CurrentValue < 0 || IndexControl.CurrentValue > 0 || KeyControl.CurrentValue > 0))
                        {
                            if (PincerControl.CurrentValue > 0)
                            {
                                PincerControl.Move(msg.val3, 0x0);
                            }

                            if (CloseControl.CurrentValue < 0)
                            {
                                CloseControl.Move(msg.val3, 0x01);
                            }
                            if (IndexControl.CurrentValue > 0)
                            {
                                IndexControl.Move(msg.val3, 0x0);
                            }

                            if (KeyControl.CurrentValue > 0)
                            {
                                KeyControl.Move(msg.val3, 0x0);
                            }
                        }
                        else
                        {
                            dofArm.Move(msg.val3, msg.val2);
                        }
                    }
                    //fire event OnMotion. Check if someone registered to this event. //this can be simplified by OnMotion?.Invoke(...); If performance drops try the simplification!
                    if (OnMotion != null)
                    {
                        OnMotion(new HandMotion(serverInstance.movement, dofArm.CurrentValue, msg.val3 / 100f));
                    }

                    break;
                //Control TAC arm
                case 0x02:

                    HideTACArm = false;
                    MainArmLayer = "Default";
                    TACArmSkinR = !HideTACArm;
                    DOF dofTAC = DOF_TAC_ArmList.Where(x => x.ID == msg.val1).ElementAt(0);
                    dofTAC.Move(msg.val3 * 4, msg.val2);
                    break;
                // Control the training phase
                case 0x03:
                    // TODO
                    break;
                //Reset the main arm
                case 0x72:

                    switch (msg.val1)
                    {
                        case 0x74:
                            foreach (var dof in DOF_TAC_ArmList)
                            {
                                dof.RestValue();
                            }
                            MainArmLayer = "Default";
                            break;
                        default:
                            foreach (var dof in DOF_ArmList)
                            {
                                dof.RestValue();
                            }
                            MainArmLayer = "Default";
                            break;
                    }
                    break;
                // Configuration
                case 0x63:
                    usePalmarGrasp = false;

                    switch (msg.val1)
                    {
                        case 0x01:
                            // Select upper limb or lower limb
                            //TODO
                            new NotImplementedException("Selecting upper limb or lower limb");
                            break;
                        case 0x02:
                            // Switch TAC test on/off
                            StartTAC = !StartTAC;
                            TACArmSkinR = StartTAC;
                            MainArmLayer = "Default";
                            break;
                        case 0x03:
                            //Error tolerance for the TAC test
                            Allowance = (float)(Convert.ToInt32(msg.val2)) / 50;
                            break;
                        case 0x05:
                            // Switch between right and left limb
                            RotateArm = true;
                            break;
                        default://TODO
                            break;
                    }
                    break;
                // Send text/Timer
                case 0x74:
                    string text = System.Text.Encoding.ASCII.GetString(msg.heatmap).Trim(new char[] { ' ', (char)0 });
                    var motion = TCPServer.GetMovement(msg).ToString();
                    var min = Convert.ToInt32(msg.val3);
                    var sec = Convert.ToInt32(msg.val4);

                    var tt = string.Format("{0} {1} in {2}:{3}", text, motion, min, sec);
                    print(tt);
                    break;

                default://TODO
                    break;
            }
        }
        serverInstance.Message = string.Empty;

        bool overPostion = false;
        for (int i = 0; i < DOF_ArmList.Count; i++)
        {
            if (DOF_ArmList[i] != null)
            {
                if (Mathf.Abs(DOF_ArmList[i].CurrentValue - DOF_TAC_ArmList[i].CurrentValue) > Allowance)
                {
                    overPostion = true;
                    break;
                }
            }
            else
            {
                break;
            }
        }
       
        if (!overPostion && StartTAC && DOF_ArmList.Any(x => Math.Abs(x.CurrentValue) > 0.1))
        {
            HideTACArm = true;
            if (GlowArm)
            {
                MainArmLayer = "Outline";
            }
            serverInstance.SendAck(0x74);
        }
        else
        {
            if (StartTAC)
            {
                MainArmLayer = "Default";
                HideTACArm = false;
            }
            serverInstance.SendAck(0x01);
        }

        TACArmSkinR = !HideTACArm;

        //set current movement and current movement's velocity
        currentMovement = serverInstance.movement;
        currentForce = msg.val3 / 100f;
    }

    public static void SetLayer(Transform root, int layer)
    {
        Stack<Transform> children = new Stack<Transform>();
        children.Push(root);
        while (children.Count > 0)
        {
            Transform currentTransform = children.Pop();
            currentTransform.gameObject.layer = layer;
            foreach (Transform child in currentTransform)
            {
                if (child.gameObject.GetComponent<Renderer>() != null)
                {
                    children.Push(child);
                }
            }
        }
    }

    /// <summary>
    /// Check for new movement and Update the animation using Animator triggers.
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        TimeCount();

        TACArm.GetComponentInChildren<SkinnedMeshRenderer>().enabled = TACArmSkinR;
        if (Input.GetKeyDown(KeyCode.J))
            MainArmLayer = "Outline";
        if (Input.GetKeyDown(KeyCode.K))
            MainArmLayer = "Default";
           
        SetLayer(MainArm.transform, LayerMask.NameToLayer(MainArmLayer));

        if (MoveTACLimbManually == true)
        {
            ManualControl(false);
        }
        else
        {
            ManualControl(true);
        }

        if (RotateArm)
        {
            GameObject.Find("lower_arm").transform.localScale = new Vector3(-GameObject.Find("lower_arm").transform.localScale.x, GameObject.Find("lower_arm").transform.localScale.y, GameObject.Find("lower_arm").transform.localScale.z);
            GameObject.Find("lower_arm").transform.Rotate(GameObject.Find("lower_arm").transform.up, 180, Space.World);
            RotateArm = false;
        }

        //update heatmap only if heat is true to prevent errors
        if (UpdateMuscularActivityMap && CurrentMuscularActivityMap != null)
        {
            if (Mam == null)
            {
                Mam = new Heatmap(colormap, alpha, cubeheight, map3D, MainArm);
            }

            Mam.UpdateHeatMap(CurrentMuscularActivityMap, cubeheight, map3D, MuscularActivityMap, innerGrowing);

        }

        if (!MuscularActivityMap && Mam != null && Mam.isVisible)
        {
            Mam.Deactivate();
        }

        

        //position thumb for palmar or lateral
        if (usePalmarGrasp)
        {
            animArm.SetFloat("f_GraspClose", 1f);
        }
        else
        {
            animArm.SetFloat("f_GraspClose", 0f);
        }
        if (Input.GetKeyDown(KeyCode.H))
        {
            blockAnimation = !blockAnimation;
        }
        //animation should be block if object is grasped
        if (!blockAnimation)
        {
            //play the animations
            foreach (DOF x in DOF_ArmList)
            {
                //Der Übergang von >0 zu <0 und umgekehrt wird wahrscheinlich Probleme bereiten, weil der Parameter der Gegenbewegung nicht automatisch 0 ist.
                if (x.CurrentValue > 0)
                {
                    animArm.SetFloat(x.NamePos, x.GetPostiveLimit(), deltaBetweenMovements, Time.deltaTime * deltaTimeFactor);
                    animArm.SetFloat(x.NameNeg, 0, deltaBetweenMovements, Time.deltaTime * deltaTimeFactor);
                }
                else
                {
                    animArm.SetFloat(x.NameNeg, x.GetNegativeLimit(), deltaBetweenMovements, Time.deltaTime * deltaTimeFactor);
                    animArm.SetFloat(x.NamePos, 0, deltaBetweenMovements, Time.deltaTime * deltaTimeFactor);

                }

            }
        }
        //play the animations
        foreach (DOF x in DOF_TAC_ArmList)
        {
            //Der Übergang von >0 zu <0 und umgekehrt wird wahrscheinlich Probleme bereiten, weil der Parameter der Gegenbewegung nicht automatisch 0 ist.
            if (x.CurrentValue > 0)
            {
                animTACArm.SetFloat(x.NamePos, x.GetPostiveLimit(),0f,0f);
                animTACArm.SetFloat(x.NameNeg, 0, 0f, 0f);
            }
            else
            {
                animTACArm.SetFloat(x.NameNeg, x.GetNegativeLimit(), 0f, 0f);
                animTACArm.SetFloat(x.NamePos, 0, 0f, 0f);
            }

        }

        
    }

    void TimeCount()
    {
        //minutes = (int)(Time.timeSinceLevelLoad / 60f);
        //seconds = (int)(Time.timeSinceLevelLoad % 60f);
        //SetRightTopPosition(counterTextLeft.GetComponent<RectTransform>(), new Vector2(100, 200));
        //SetRightTopPosition(counterTextRight.GetComponent<RectTransform>(), new Vector2(200, 200));
        //counterTextLeft.text = "Close arm in\n" + minutes.ToString("00") + ":" + seconds.ToString("00");
        //counterTextRight.text = counterTextLeft.text;
    }
    public void SetRightTopPosition(RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width), newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
    }

    public void SetBlockAnimation(bool block)
    {
        blockAnimation = block;
    }
    private void ManualControl(bool MainArm = true)
    {
        if (Input.GetKey(KeyCode.Escape)) //Reset Arms DOFs
        {
            for (int i = 0;i< DOF_ArmList.Count();i++)
            {
                if(!MoveTACLimbManually)
                    DOF_ArmList[i].RestValue();
                else
                    DOF_TAC_ArmList[i].RestValue();
            }
        }

        //create ByteCast object with dummy values
        TCPServer.ByteCast manualMsgByte;
        manualMsgByte.Operation = 0xFF;
        serverInstance.Message = "DummyValues";
        manualMsgByte.val3 = 42;
        manualMsgByte.val4 = 0xFF;
        manualMsgByte.val1 = 0xFF;
        manualMsgByte.val2 = 0xFF;
        manualMsgByte.heatmap = serverInstance.MsgByte.heatmap;

        //variable to verify if key stroke was performed
        bool keyStroke = false;

        //manually control movement
        if (Input.GetKey(KeyCode.Q)) //Pronation
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x08;
            manualMsgByte.val2 = 0x00;
			serverInstance.movement = HandMovement.Movement.Pronation;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.W)) //Supination
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x08;
            manualMsgByte.val2 = 0x01;
			serverInstance.movement = HandMovement.Movement.Supination;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.A)) //Close
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x09;
            manualMsgByte.val2 = 0x00;
			serverInstance.movement = HandMovement.Movement.Close;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.S)) //Open
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x09;
            manualMsgByte.val2 = 0x01;
			serverInstance.movement = HandMovement.Movement.Open;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.E)) //Point
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x0A;
            manualMsgByte.val2 = 0x01;
			serverInstance.movement = HandMovement.Movement.Index;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.X)) //Fine Grip
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x0C;
            manualMsgByte.val2 = 0x01;
			serverInstance.movement = HandMovement.Movement.Pincer;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.D)) //Flex Hand
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x07;
            manualMsgByte.val2 = 0x00;
			serverInstance.movement = HandMovement.Movement.Flex;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.F))//Ex Hand
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x07;
            manualMsgByte.val2 = 0x01;
			serverInstance.movement = HandMovement.Movement.Extend;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.C)) //Elbow Ex
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x0F;
            manualMsgByte.val2 = 0x00;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.V)) //Elbow Flex 
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x0F;
            manualMsgByte.val2 = 0x01;
            keyStroke = true;
        }
        if (Input.GetKey(KeyCode.T)) //Side Grip
        {
            if (MainArm)
            {
                manualMsgByte.Operation = 0x01;
            }
            else
            {
                manualMsgByte.Operation = 0x02;
            }
            serverInstance.Message = "FuckTheSystem";
            manualMsgByte.val3 = manualMovementSpeed;
            manualMsgByte.val4 = 0x01;
            manualMsgByte.val1 = 0x0D;
            manualMsgByte.val2 = 0x01;
			serverInstance.movement = HandMovement.Movement.Close;
            keyStroke = true;
        }

        //fires fake event only if a key is held down
        if (keyStroke)
        {
            HandleIncomingMessage(manualMsgByte, true);
        }
    }

}
