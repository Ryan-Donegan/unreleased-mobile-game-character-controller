using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float velocityIncrease;

    [Header("Obj Refs")]
    public GameObject cameraControlPoint, target;
    public GameObject[] wings;
    private CameraController camControl;

    [Header("Runtime")]
    private Rigidbody rb;
    private SphereCollider colliderRef;
    public int state;    
    public float speed, rotBound, thrustEfficiency, liftEfficiency, lateralEfficiency; // drag;
    private float roll, pitch, yaw, verticalVelocity, lateralVelocity, radius, targetMagnitude;
    private float joystickRadius;

    private Vector3 liftThrust, lateralThrust;
    private Vector3 inputVector, vertical, horizontal, targetForce, thrust;
    private Vector2 joystickCentre, joystickPosition, joystickValueRaw;
    private Quaternion currentRotation, targetRotation;
    public bool onGround, joystickCurrentlyActive = false;
    

    // Start is called before the first frame update
    void Start()
    {
        state = 0;
        rb = GetComponent<Rigidbody>();
        colliderRef = GetComponent<SphereCollider>();
        camControl = cameraControlPoint.GetComponent<CameraController>();
        radius =  colliderRef.radius;

        joystickRadius = Screen.width / 4;

        //lift = (Physics.gravity.magnitude * Vector3.up * liftEfficiency);// + (drag * Vector3.forward * liftEfficiency);
        //dragForce = Vector3.back * drag;
    }

    // Update is called once per frame
    void Update()
    {
        inputVector = new Vector3(Input.GetAxis("Horizontal"),0 , Input.GetAxis("Vertical"));

        inputVector=GetInputVector();

        if (state > -1)
        {
            camControl.SetInputAxes(inputVector.x);
        }
        

        if (Input.GetButtonDown("Fire2"))
        {

            if (state == 0)
            {
                OpenGlider();
            }
            else if (state == 1)
            {
                CloseGlider();
            }
        }
    }

    void FixedUpdate()
    {
        if (transform.position.y <= 3 && state != 2)
        {
            state = 2;
            AddGravity();
        }

        onGround = GroundedCheck();

        if (state == 0 && onGround )
        {
            Roll();
        }
        else if(state == 0 && inputVector.x >= 0.5)
        {
            OpenGlider();
        }
        else if (state == 1)
        {
            Glide();

            if (joystickCurrentlyActive==false)
            {
                CloseGlider();
            }
        }

        //onGround = false;


    }

    //Old ground check code, no longer fit for purpose
    /*
    private void OnCollisionStay(Collision collision)
    {
        onGround = true;
    }
    */

    private bool GroundedCheck()
    {
        RaycastHit raycastHit;
        if (Physics.Raycast(transform.position, -Vector3.up, out raycastHit, radius + 0.5f))
        {
            if (raycastHit.collider.gameObject.tag == "NoFloor")
            {
                return false;
            }
            return true;
        }
        return false;
    }

    private void Roll()
    {
        //Force Roll System
        vertical = cameraControlPoint.transform.forward * inputVector.z * speed;
        horizontal = cameraControlPoint.transform.right * inputVector.x * speed;

        //targetForce = vertical + horizontal;
        //targetMagnitude = targetForce.magnitude;

        //rb.AddForce(vertical + horizontal);
        rb.velocity = new Vector3(rb.velocity.x * velocityIncrease, rb.velocity.y * velocityIncrease, rb.velocity.z * velocityIncrease);

        //Torque Roll System
        //vertical = cameraControlPoint.transform.right * inputVector.z * speed;
        //horizontal = cameraControlPoint.transform.forward * inputVector.x * speed * -1;
        //rb.AddTorque(vertical + horizontal);
    }

    private void OpenGlider()
    {
        float velocityStrength;
        Vector3 currentPosition = transform.position;
        Quaternion targetRotation = cameraControlPoint.transform.rotation; 

        transform.SetPositionAndRotation(currentPosition, targetRotation);
        rb.freezeRotation = true;

        wings[0].transform.Rotate(new Vector3(0, 0, 90));
        wings[1].transform.Rotate(new Vector3(0, 0, 90));

        velocityStrength = rb.velocity.magnitude;
        rb.velocity = transform.forward * velocityStrength;

        //Disable gravity and drag for manual control in glider controls
        rb.useGravity = false;
        rb.drag = 0;
        state = 1;
    }

    private void CloseGlider()
    {
        float dragFactor = 0.5f;
        Vector3 currentPosition = transform.position;
        Quaternion targetRotation = cameraControlPoint.transform.rotation;

        transform.SetPositionAndRotation(currentPosition, targetRotation);

        wings[0].transform.Rotate(new Vector3(0, 0, -90));
        wings[1].transform.Rotate(new Vector3(0, 0, -90));

        rb.freezeRotation = false;

        //Reduce XY (Horizontal Plane) Components of velocity
        rb.velocity = new Vector3(rb.velocity.x * dragFactor, rb.velocity.y, rb.velocity.z * dragFactor);

        AddGravity();
        state = 2;
    }

    public void ResetGlider()
    {
        wings[0].transform.rotation =Quaternion.Euler(0,0,0);
        wings[1].transform.rotation =Quaternion.Euler(0, 180, 0);
        state = 0;
        rb.useGravity = true;
        rb.drag = 0.05f;
        rb.angularDrag = 0.0f;
    }

    private void AddGravity()
    {
        //Re-enable gravity and drag 
        rb.useGravity = true;
        rb.drag = 0.05f;

        //rolling friction
        rb.angularDrag = 4;        
    }

    private void Glide()
    {
        //Undo Previous step rotation
        //transform.Rotate(transform.forward, roll, Space.World);
        //transform.Rotate(transform.right, -pitch, Space.World);

        //Get target roll and pitch for current step
        roll = inputVector.x * rotBound;
        pitch = inputVector.z * rotBound;

        //Avoid gimbal lock with setting yaw by correcting local transforms based on camera
        //target.transform.SetPositionAndRotation(cameraControlPoint.transform.position, cameraControlPoint.transform.rotation);
        //target.transform.Translate(Vector3.forward * 10, Space.Self);
        //transform.LookAt(target.transform);

        //Set Y rotation to desired world space rotation
        currentRotation = Quaternion.LookRotation(cameraControlPoint.transform.forward);
        targetRotation = Quaternion.Euler(pitch, 0, -roll);
        targetRotation = currentRotation * targetRotation;
        transform.rotation = targetRotation;

        //Set roll and pitch for current step, yaw for current step
        //transform.Rotate(transform.forward, -roll, Space.Self);
        //transform.Rotate(transform.right, pitch, Space.Self);

        //Apply Gravity manually
        rb.velocity = rb.velocity - Vector3.up *(Physics.gravity.magnitude * Time.fixedDeltaTime);

        //Apply Lift to local up
        rb.velocity = rb.velocity + transform.up * (Physics.gravity.magnitude * Time.fixedDeltaTime);

        //Remove Local Vertical Velocity to re add as thrust along the forward axis
        liftThrust = rb.velocity - Vector3.ProjectOnPlane(rb.velocity, transform.up);
        verticalVelocity = liftThrust.magnitude;
        rb.velocity = rb.velocity - (liftThrust );
        rb.velocity = rb.velocity + (transform.forward * verticalVelocity * Time.fixedDeltaTime * liftEfficiency);

        //Remove Local Lateral Velocity to re add as thrust along the foraward axis
        lateralThrust = rb.velocity - Vector3.ProjectOnPlane(rb.velocity, transform.right);
        lateralVelocity = lateralThrust.magnitude;
        rb.velocity = rb.velocity - (lateralThrust);
        rb.velocity = rb.velocity + (transform.forward * lateralVelocity * Time.fixedDeltaTime * lateralEfficiency);


        //Debug.Break();
    }

    private Vector3 GetInputVector()
    {
        float xInput = 0, yInput = 0;
        joystickCurrentlyActive = false;

        try
        {
            //Nested in try catch to catch exception on this line from no touchscreen event
            Touch touch = Input.touches[0];
            joystickCurrentlyActive = true;

            if (touch.phase == TouchPhase.Began)
            {
                joystickCentre = touch.position;
                joystickPosition = joystickCentre;
            }
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                joystickPosition = touch.position - joystickCentre;
            }
            else
            {
                joystickCentre = new Vector2(0, 0);
                joystickPosition = joystickCentre;
            }

            joystickValueRaw = joystickCentre - joystickPosition;

            xInput = JoystickValueRemap(joystickValueRaw.x, joystickCentre.x - joystickRadius, joystickCentre.x + joystickRadius, -1f, 1f);
            yInput = JoystickValueRemap(joystickValueRaw.y, joystickCentre.y - joystickRadius, joystickCentre.y + joystickRadius, -1f, 1f);
        }
        catch (System.IndexOutOfRangeException e)
        {

        }

        return new Vector3(xInput, 0, yInput);
    }


    private float JoystickValueRemap(float value, float lowerBound, float upperBound, float targetLowerBound, float targetUpperBound)
    {
        value = Mathf.Clamp(value, lowerBound, upperBound);

        value = targetLowerBound + (value - lowerBound) * (targetUpperBound - targetLowerBound) / (upperBound - lowerBound);

        value = value * -1;

        return value;
    }


    private void OnCollisionEnter(Collision collision)
    {
        if (state == 1)
        {
            CloseGlider();
        }
    }


    public void StartRolling()
    {
        state = 0;
    }

    public void LockControls()
    {
        state = -1;
    }
}




//Old horizontal drag
/*
    //Apply local Horizontal Drag components calculated in same manner
        forwardDrag = rb.velocity - Vector3.ProjectOnPlane(rb.velocity, transform.forward);
        forwardVelocity = forwardDrag.magnitude;
        rb.AddForce(-forwardDrag.normalized * forwardVelocity * Time.fixedDeltaTime);

        strafeDrag = rb.velocity - Vector3.ProjectOnPlane(rb.velocity, transform.right);
        strafeVelocity = strafeDrag.magnitude;
        rb.AddForce(-strafeDrag.normalized * strafeVelocity * Time.fixedDeltaTime);

*/
