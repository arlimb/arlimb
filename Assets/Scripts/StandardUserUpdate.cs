using UnityEngine;
using System.Collections;
using AssemblyCSharp;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UI;

public delegate void EventMotionHand(HandMotion e);

public class StandardUserUpdate : MonoBehaviour
{

    public byte manualMovementSpeed = 50;

    //Server
    public string Ip = "127.0.0.1";
    public int Port = 4;

    // Main arm.
    public GameObject MainArm;
    private Animator animArm;
    private bool RotateArm = false;
    //private GameObject TACArm;
    public static TCPServer serverInstance = null;

    //Main arm state
    public static List<DOF> DOF_ArmList = new List<DOF>();

    //List of different close movment, grasp, close, key....
    private static List<Byte> CloseMovement = new List<Byte>();

    //Bloked movment
    public static List<HandMovement.Movement> BlockedMovment = new List<HandMovement.Movement>();

    //Movment Range
    private static int Range = 200; // 500

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

    //current movement and current force
    public float currentForce = 0f;
    public HandMovement.Movement currentMovement = HandMovement.Movement.Error;
    public bool currentlyGraspingObject = false;
    public bool usePalmarGrasp = false;

    [Tooltip("Switches the manual control between moving the main-limb and the TAC-limb")]
    public bool MoveTACLimbManually = false;

    public bool UseManualControl = false;

    public static event EventMotionHand OnMotion;

    public Text counterTextLeft, counterTextRight;
    private float seconds, minutes;


    /// <summary>
    /// initialization.
    /// </summary>
    void Start()
    {
        UseManualControl = false;

        //Get Server Instance 
        if (serverInstance == null)
        {
            serverInstance = TCPServer.getInstance(Ip, Port);
        }

        //register to IncomingMessage event
        serverInstance.incomingMessage += new EventIncomingMessage(HandleIncomingMessage);

        //Get Animator
        if (MainArm == null)
        {
            MainArm = GameObject.FindGameObjectWithTag("MainLimbAnimatorObject");
        }

        animArm = MainArm.GetComponent<Animator>();

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

        //Set ID-list of secondary-close movments
        CloseMovement.Add(0x0C);
        CloseMovement.Add(0x0D);
        CloseMovement.Add(0x0A);
    }

    private void HandleIncomingMessage(TCPServer.ByteCast msg, bool manualControl=false)
    {
        DOF CloseControl = DOF_ArmList.Where(x => x.ID == 0x09).ElementAt(0);
        DOF PincerControl = DOF_ArmList.Where(x => x.ID == 0x0C).ElementAt(0);
        DOF IndexControl = DOF_ArmList.Where(x => x.ID == 0x0A).ElementAt(0);
        DOF KeyControl = DOF_ArmList.Where(x => x.ID == 0x0D).ElementAt(0);
       
       if (!UseManualControl || manualControl || msg.Operation != 0x01)
        { 
            switch (msg.Operation)
            {
                //Control main arm
                case 0x01:

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
                    throw new Exception("TAC-Test not supported");
                // Control the training phase
                case 0x03:
                    //TODO
                    break;
                //Reset the main arm
                case 0x72:

                    switch (msg.val1)
                    {
                        case 0x74:
                            throw new Exception("TAC-Test not supported");
                        default:
                            foreach (var dof in DOF_ArmList)
                            {
                                dof.RestValue();
                            }
                            break;
                    }
                    break;
                // Configurations
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
                            throw new Exception("TAC-Test not supported");
                        case 0x03:
                            throw new Exception("TAC-Test not supported");
                        case 0x05:
                            // Switch between right and left limb
                            RotateArm = true;
                            break;
                        default://TODO
                            break;
                    }
                    break;

                default://TODO
                    break;
            }
        }
        serverInstance.SendAck(0x01);

        //set current movement and current movement's velocity
        currentMovement = serverInstance.movement;
        currentForce = msg.val3 / 100f; //maybe this is a source for errors. Needs to be tested!
    }

    /// <summary>
    /// Check for new movement and Update the animation using Animator triggers.
    /// Update is called once per frame
    /// </summary>
    void Update()
    {
        TimeCount();

        if (MoveTACLimbManually == true)
        {
            ManualControl(false);
        }
        else
        {
            ManualControl(true);
        }
        

        if (!MuscularActivityMap && Mam != null && Mam.isVisible)
        {
            Mam.Deactivate();
        }

        //update heatmap only if heat is true to prevent errors
        if (UpdateMuscularActivityMap && CurrentMuscularActivityMap != null)
        {
            if (Mam == null)
            {
                Mam = new Heatmap(colormap, alpha, cubeheight, map3D, MainArm);
            }

            Mam.UpdateHeatMap(CurrentMuscularActivityMap, cubeheight, map3D, MuscularActivityMap, innerGrowing);
            UpdateMuscularActivityMap = false;
        }

        // use left or right arm
        if (RotateArm)
        {
            GameObject.Find("lower_arm").transform.localScale = new Vector3(-GameObject.Find("lower_arm").transform.localScale.x, GameObject.Find("lower_arm").transform.localScale.y, GameObject.Find("lower_arm").transform.localScale.z);
            GameObject.Find("lower_arm").transform.Rotate(GameObject.Find("lower_arm").transform.up, 180, Space.World);
            RotateArm = false;
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

    void TimeCount()
    {
    //    minutes = (int)(Time.timeSinceLevelLoad / 60f);
    //    seconds = (int)(Time.timeSinceLevelLoad % 60f);
    //    SetRightTopPosition(counterTextLeft.GetComponent<RectTransform>(),new Vector2(100,200));
    //    SetRightTopPosition(counterTextRight.GetComponent<RectTransform>(), new Vector2(200, 200));
    //    counterTextLeft.text = "Close arme in\n" + minutes.ToString("00") + ":" + seconds.ToString("00");
    //    counterTextRight.text = counterTextLeft.text;
    }
    public void SetRightTopPosition(RectTransform trans, Vector2 newPos)
    {
        trans.localPosition = new Vector3(newPos.x - ((1f - trans.pivot.x) * trans.rect.width) , newPos.y - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
    }

    private void ManualControl(bool MainArm = true)
    {
        if (Input.GetKey(KeyCode.Escape)) //Reset Arms DOFs
        {
            for (int i = 0; i < DOF_ArmList.Count(); i++)
            {
                DOF_ArmList[i].RestValue();
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            serverInstance.Message = "DummyValues";
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
            print(manualMsgByte);
            HandleIncomingMessage(manualMsgByte, true);
        }
    }
}
