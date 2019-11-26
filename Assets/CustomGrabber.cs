using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomGrabber : MonoBehaviour
{
    public int buttonID = 0;        //!< index of the button assigned to grabbing.  Defaults to the first button
    public bool ButtonActsAsToggle = false; //!< Toggle button? as opposed to a press-and-hold setup?  Defaults to off.
    public enum PhysicsToggleStyle { none, onTouch, onGrab };
    public PhysicsToggleStyle physicsToggleStyle = PhysicsToggleStyle.none;   //!< Should the grabber script toggle the physics forces on the stylus? 

    public bool DisableUnityCollisionsWithTouchableObjects = true;

    private GameObject hapticDevice = null;   //!< Reference to the GameObject representing the Haptic Device
    private bool buttonStatus = false;          //!< Is the button currently pressed?
    private GameObject touching = null;         //!< Reference to the object currently touched
    private GameObject grabbing = null;         //!< Reference to the object currently grabbed
    private FixedJoint joint = null;            //!< The Unity physics joint created between the stylus and the object being grabbed.

    public int latestObjectIndex=0; //This is the index for the number of objects currently on the plate


    //! Automatically called for initialization
    void Start()
    {
        if (hapticDevice == null)
        {

            HapticPlugin[] HPs = (HapticPlugin[])Object.FindObjectsOfType(typeof(HapticPlugin));
            foreach (HapticPlugin HP in HPs)
            {
                if (HP.hapticManipulator == this.gameObject)
                {
                    hapticDevice = HP.gameObject;
                }
            }

        }

        if (physicsToggleStyle != PhysicsToggleStyle.none)
            hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = false;

        if (DisableUnityCollisionsWithTouchableObjects)
            disableUnityCollisions();
    }

    void disableUnityCollisions()
    {
        GameObject[] touchableObjects;
        touchableObjects = GameObject.FindGameObjectsWithTag("Touchable") as GameObject[];  //FIXME  Does this fail gracefully?

        // Ignore my collider
        Collider myC = gameObject.GetComponent<Collider>();
        if (myC != null)
            foreach (GameObject T in touchableObjects)
            {
                Collider CT = T.GetComponent<Collider>();
                if (CT != null)
                    Physics.IgnoreCollision(myC, CT);
            }

        // Ignore colliders in children.
        Collider[] colliders = gameObject.GetComponentsInChildren<Collider>();
        foreach (Collider C in colliders)
            foreach (GameObject T in touchableObjects)
            {
                Collider CT = T.GetComponent<Collider>();
                if (CT != null)
                    Physics.IgnoreCollision(C, CT);
            }

    }

    private bool was_object_selected = false;
    public string[] sandwichStack = new string[6];
    void FixedUpdate()
    {
        if (physicsToggleStyle == PhysicsToggleStyle.onTouch)
        {
            hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = true;
            GetComponentInParent<Rigidbody>().velocity = Vector3.zero;
            GetComponentInParent<Rigidbody>().angularVelocity = Vector3.zero;
        }
        bool newButtonStatus = hapticDevice.GetComponent<HapticPlugin>().Buttons[buttonID] == 1;
        bool oldButtonStatus = buttonStatus;
        buttonStatus = newButtonStatus;
        if (oldButtonStatus == false && newButtonStatus == true)
            pressedOnce = true;
        if (oldButtonStatus == true && newButtonStatus == false)
        {
            pressedOnce = false;
            SelectedObject = null;
            joint.connectedBody = null;
            Destroy(joint);
            grabbing = null;
        }
        if (physicsToggleStyle != PhysicsToggleStyle.none)
        { 
        hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = false;
        }
        if (pressedOnce && SelectedObject!=null)
        {
            was_object_selected = true;
            pressedOnce = false;
            grabbing = SelectedObject;
            Rigidbody body = grabbing.GetComponent<Rigidbody>();
            while (body == null)
            {
                //Debug.logger.Log("Grabbing : " + grabbing.name + " Has no body. Finding Parent. ");
                if (grabbing.transform.parent == null)
                {
                    grabbing = null;
                    return;
                }
                GameObject parent = grabbing.transform.parent.gameObject;
                if (parent == null)
                {
                    grabbing = null;
                    return;
                }
                grabbing = parent;
                body = grabbing.GetComponent<Rigidbody>();
            }

            joint = (FixedJoint)gameObject.AddComponent(typeof(FixedJoint));
            joint.connectedBody = body;
        }

        if (grabbing && physicsToggleStyle != PhysicsToggleStyle.none)
            hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = true;
        if (!grabbing && physicsToggleStyle == PhysicsToggleStyle.onGrab)
            hapticDevice.GetComponent<HapticPlugin>().PhysicsManipulationEnabled = false;
    }
    public GameObject SelectedObject;
    public GameObject PreviousObject;
    public bool pressedOnce = false;
    void OnCollisionEnter(Collision collision)
    {
        SelectedObject = collision.collider.gameObject;
        if(SelectedObject.name.Equals("Plate"))
        {
            SelectedObject = null;
        }
        PreviousObject = SelectedObject;

    }
    void OnCollisionExit(Collision collision)
    {
        SelectedObject = null;
        if(!was_object_selected||sandwichStack[0]==PreviousObject.name|| sandwichStack[1] == PreviousObject.name ||
            sandwichStack[2] == PreviousObject.name || sandwichStack[3] == PreviousObject.name ||
            sandwichStack[4] == PreviousObject.name || sandwichStack[5] == PreviousObject.name ||
            latestObjectIndex==6)
        {
            PreviousObject = null;
            return;
        }
        else if (was_object_selected)
        {
            Transform myTransform = PreviousObject.GetComponent<Transform>();
            float xpos = myTransform.position.x;
            float ypos = myTransform.position.y;
            float zpos = myTransform.position.z;
            if (ypos < 7 && ypos > -2.5 && xpos < 7 && xpos > -4 && zpos > -12 && zpos < -3)
            {
                sandwichStack[latestObjectIndex] = PreviousObject.name;
                PreviousObject.transform.SetPositionAndRotation(new Vector3((float)2, (float)latestObjectIndex / 4 + 0, (float)-9), Quaternion.Euler(new Vector3(0, 90, 0)));
                latestObjectIndex++;
                Destroy(PreviousObject.GetComponent<Rigidbody>());
                was_object_selected = false;
            }
        }
        PreviousObject = null;
    }

}
