using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PlayerMovement : MonoBehaviour {

   
    public float walkSpeed;
    public float runSpeed;
    public float jumpForce;
    public float negativeJumpForce;
    public float rollForce;
    public float tapTime;
    public float slideStartAngel;
    public float slideMultiplyer;
    public float slideMultiAdditon;
    public float burm;
    public float chargetimer;
    public float chargetime;
    public float jumpCharger;
    public float Maxjumpcharge;
    public int rolldistance;
    public float gravity;
    public float plusGravity;
    public float minusGravity;
    public float succRunClickMax;
    public float succRunfulClickMin;
    public float failRunClickMax;
    public float failRunClickMin;
    public float succSlideClickMax;
    public float succSlidefulClickMin;
    public float failSlideClickMax;
    public float failSlideClickMin;

    public float succGrindClickMax;
    public float succGrindfulClickMin;
    public float failGrindClickMax;
    public float failGrindClickMin;
    public bool sliding;
    public bool grind;
    [SerializeField]
    public float speed;
     Camera cam;

    Rigidbody rb;
    Vector3 moveDir;
    Vector3 slidingForce;
    Vector3 globalForce;
    Vector3 directionOfRoll;
    Vector3 oldPos;
    private Animator animator;
    private RaycastHit hit;

    Quaternion currentRot;
    private float movementSpeed;
    private float _doubleTapTimeA;
    private float _doubleTapTimeD;
    private float worldForwardAngle;
    private float worldRightAngle;

    Animator playerAnim;
    private int jump = 0;

    private bool running;
    
    private bool clicked;
    private bool negativeGravity;
    private bool m_grounded;
    private bool doubleTapA = false;
    private bool doubleTapD = false;
    


    void Start() {

        globalForce = Vector3.zero;
        rb = GetComponent<Rigidbody>();
        playerAnim = GameObject.FindGameObjectWithTag("kid").GetComponent<Animator>();
        gravity = .87f;
        cam = Camera.main;
     
    }

    void Update() {
       
        Jump();
        Landing();
        if (Input.GetMouseButton(0) && m_grounded || Input.GetButton("Slide") && m_grounded) {
            sliding = true;
            running = false;
        }
        else {
            sliding = false;
        }
       

    }

    void FixedUpdate() {


        ///Jump();
       
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
      

       
      
        m_grounded = IsGrounded();
        
        checkForDoubleTap(h, v);
        checkForSlopes();
        Movement(h, v);
      //  Landing();
        //checkForSlopes();
        calculatingAngles();
        calculatingMultiplyer();

        speed = Vector3.Distance( oldPos, transform.position);
        oldPos = transform.position;

        playerAnim.SetBool("jumpIsTrue", !m_grounded);
        
    }


    bool IsGrounded() {
        
        Ray ray = new Ray(transform.position, -transform.up);

        //Raycast to check if the player is grounded or not
        if (Physics.Raycast(ray, out hit, .8f)) {
         
            if (hit.collider.tag == "Map" || hit.collider.tag == "Grind") {
                chargetimer = 0;
                negativeGravity = false;
               
                //Physics.gravity = new Vector3(0, -9.87f, 0);
                return true;
            }
        }
       // sliding = false;
        return false;
    }

    void Movement(float x, float v) {

        if (Input.GetKey(KeyCode.LeftShift) || Input.GetButton("Run") && m_grounded) {
            running = true;
            sliding = false;
           
           
        }
        else {
            running = false;
        }


        if (running) {
            movementSpeed = runSpeed + (slideMultiplyer * .5f); ;
       
           
        }
        else {
            movementSpeed = walkSpeed + (slideMultiplyer * .25f); ;
            
        }
        
        //if (sliding) {
        //    //The Player is sliding
        //    float xProj = animator.GetFloat("xProj");
        //    moveDir = transform.TransformDirection(xProj * burm, 0, v * movementSpeed);
          
        //    rb.MovePosition(transform.position + (moveDir * Time.deltaTime));
        //    rb.AddForce(moveDir * Time.deltaTime, ForceMode.VelocityChange);
        //}

       if(!sliding) {

            //The Player is running
            // Vector3 _input = new Vector3(Input.GetAxis("Horizontal") * 3, 0, Input.GetAxis("Horizontal") * 3);

            //Vector3 _moveDir = cam.transform.TransformDirection(_input);
            // moveDir = transform.TransformDirection(0, 0, Mathf.Clamp(Mathf.Abs(v + x),-1,1)   /*Mathf.Abs*x)/*-1,1)*/ * movementSpeed);
            if (v != 0 || x != 0) 
                //rb.MovePosition(transform.position + (transform.forward* movementSpeed * Time.deltaTime));
                rb.MovePosition(transform.position + (transform.forward * movementSpeed * Time.deltaTime));

            playerAnim.SetFloat("walkSpeed", Mathf.Abs(v + x));

        }

    }

    void checkForDoubleTap(float h, float v) {

        
        if (Input.GetKeyDown(KeyCode.A)) {
            if (Time.time < _doubleTapTimeA + tapTime) {
                doubleTapA = true;
               
                Dodge(h, v);
            }
            _doubleTapTimeA = Time.time;

        }

        if (Input.GetKeyDown(KeyCode.D)) {
            if (Time.time < _doubleTapTimeD + tapTime) {
                doubleTapD = true;
                
                Dodge(h, v);
            }
            _doubleTapTimeD = Time.time;

        }

    }
    
    void Jump() {
        //Player jumps up
        if (Input.GetButtonDown("Jump") && m_grounded == true) {


            playerAnim.SetBool("failedLanding", false);
         
                gravity = 0.87f;
            rb.AddForce(transform.up * 10, ForceMode.VelocityChange);
        }

        if (Input.GetButton("Jump")) {
            jumpCharger += Time.deltaTime;
            if (jumpCharger < Maxjumpcharge) {
                rb.AddForce(transform.up * 10, ForceMode.Acceleration);
            }
            if (jumpCharger > Maxjumpcharge)
                negativeGravity = true;
        }
       if (Input.GetButtonUp("Jump")) {


            m_grounded = false;
            running = false;
            sliding = false;
            jump = 1;
            gravity = 0.87f;
            jumpCharger = 0;
            negativeGravity = true;
           
        }
        if (m_grounded == false) {
            chargetimer += Time.deltaTime;

            //Code for player to  glide.
            //Gravity gets lighter
            if (Input.GetKey(KeyCode.E) && chargetimer < chargetime || Input.GetButton("Glide") && chargetimer < chargetime) {
                gravity += plusGravity;
                //Physics.gravity = new Vector3(0, gravity, 0);
                rb.velocity = 20 * transform.forward;
               // rb.AddForce(transform.forward * 10, ForceMode.VelocityChange);
               
            }
            //Code that brings player back down
            //Gravity gets heavier
            if (Input.GetKeyUp(KeyCode.E) || Input.GetButtonUp("Glide") || chargetimer >= 2 ) {
                gravity -= minusGravity;
            }

           
        }
        if (m_grounded) {
            gravity = -9.87f;
        }
        
        Physics.gravity = new Vector3(0, gravity, 0);
        if(negativeGravity)
        rb.AddForce(-transform.up * negativeJumpForce, ForceMode.VelocityChange);
    }

    void Dodge(float h, float v) {


        if (doubleTapA) {
            if (Input.GetKeyDown(KeyCode.A)) {
                if (rb.velocity.magnitude <= 0.5f && rb.velocity.magnitude >= -0.5f)
                    rb.AddForce(directionOfRoll * rollForce, ForceMode.Impulse - rolldistance);
               
            }
        }

        if (doubleTapD) {
            if (Input.GetKeyDown(KeyCode.D)) {
                if (rb.velocity.magnitude <= 0.5f && rb.velocity.magnitude >= -0.5f)
                    rb.AddForce(directionOfRoll * rollForce, ForceMode.Impulse - rolldistance);
               
            }
        }
    }

    void checkForSlopes() {

        float v = Input.GetAxis("Vertical");
        float h = Input.GetAxis("Horizontal");
       

        //Move player around based on the players mouse
        //float mouseInput = Input.GetAxis("Mouse X");
        Vector3 lookhere1 = cam.transform.TransformDirection(new Vector3(Input.GetAxis("Horizontal") * 3, 0, Input.GetAxis("Vertical") * 3));
        
        Quaternion lookRot = Quaternion.LookRotation(lookhere1);
        if (lookRot.y != 0) {
            currentRot = lookRot;

        }
        
        //If player is grounded slerp the player with the terrains normal
        if (m_grounded) {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;

            targetRotation = Quaternion.Euler(targetRotation.eulerAngles.x, currentRot.eulerAngles.y, targetRotation.eulerAngles.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, (Time.deltaTime * 20) /* (Mathf.Clamp(v + Mathf.Abs(h), 0f, 1))*/);
        }

        ////If the player isn't grounded rotate them so there transform up is the same as the worlds up
        else {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up) * transform.rotation;
            targetRotation = Quaternion.Euler(targetRotation.eulerAngles.x, currentRot.eulerAngles.y, targetRotation.eulerAngles.z);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime /** (Mathf.Clamp(v + Mathf.Abs(h), 0f, 1))*/ * 10);
        }
    }

    void calculatingAngles() {

        //Turns on/off the sliding mechanic
        

        //Sliding turned on will calculate global force to apply to player 
            if (sliding) {
                float forwardAngle = Vector3.Angle(transform.up, Vector3.forward);
                worldForwardAngle = forwardAngle - 90;


                float Rightangle = Vector3.Angle(transform.up, Vector3.right);
                worldRightAngle = Rightangle - 90;


                if (worldForwardAngle > slideStartAngel)
                slidingForce.z = -(worldForwardAngle - slideStartAngel) * slideMultiplyer;

                else if (worldForwardAngle < -slideStartAngel)
                slidingForce.z = Mathf.Abs((worldForwardAngle + slideStartAngel) * slideMultiplyer);

                else slidingForce.z = 0;

                if (worldRightAngle > slideStartAngel)
                slidingForce.x = -(worldRightAngle - slideStartAngel) * slideMultiplyer;
                else if (worldRightAngle < -slideStartAngel)
                slidingForce.x = Mathf.Abs((worldRightAngle + slideStartAngel) * slideMultiplyer);
                else
                slidingForce.x = 0;
            globalForce = slidingForce;
          
        }
        //the Player is running 
        if (!sliding) {
            globalForce = Vector3.zero;
        }
        rb.AddForce(globalForce, ForceMode.Force);

    }

    void Landing() {

        
            RaycastHit hitForLanding;
            Vector3 down = transform.TransformDirection(Vector3.down);

            if (!m_grounded && !clicked) {
            
                if (Physics.Raycast(transform.position, down, out hitForLanding)) {
                //Range in which the player can preform a time click to land correctly
                    if (hitForLanding.distance <= succRunClickMax && hitForLanding.distance >= succRunfulClickMin && Input.GetMouseButtonDown(0) || hitForLanding.distance <= succRunClickMax && hitForLanding.distance >= succRunfulClickMin && Input.GetButtonDown("LandRun")) {
                        jump = 2;
                        clicked = true;
                        running = true;
                    
                    //  print("landed");
                }
                    //Outside of above range is a failed click and landing
                    if (hitForLanding.distance > failRunClickMax && Input.GetMouseButtonDown(0) || hitForLanding.distance < failRunClickMin && Input.GetMouseButtonDown(0)|| hitForLanding.distance > failRunClickMax && Input.GetButtonDown("LandRun") || hitForLanding.distance < failRunClickMin && Input.GetButtonDown("LandRun")) {
                        jump = 1;
                        clicked = true;
                    //  print("didnt land");
                    playerAnim.SetBool("failedLanding", true);
                    }

                if (hitForLanding.distance <= succSlideClickMax && hitForLanding.distance >= succRunfulClickMin && Input.GetMouseButtonDown(0) || hitForLanding.distance <= succSlideClickMax && hitForLanding.distance >= succSlidefulClickMin && Input.GetButtonDown("LandSlide") && hitForLanding.collider.tag == "Map") {
                    jump = 2;
                    clicked = true;
                    sliding = true;
                  //  print("landed");
                }
                //Outside of above range is a failed click and landing
                if (hitForLanding.distance > failSlideClickMax && Input.GetMouseButtonDown(0) || hitForLanding.distance < failSlideClickMin && Input.GetMouseButtonDown(0) || hitForLanding.distance > failSlideClickMax && Input.GetButtonDown("LandSlide") && hitForLanding.collider.tag == "Map" || hitForLanding.distance < failSlideClickMin && Input.GetButtonDown("LandSlide") && hitForLanding.collider.tag == "Map") {
                    jump = 1;
                    clicked = true;
                    //print("didnt land");
                    playerAnim.SetBool("failedLanding", true);
                }

                if (hitForLanding.distance <= succGrindClickMax && hitForLanding.distance >= succGrindfulClickMin && Input.GetButtonDown("LandGrind") && hitForLanding.collider.tag == "Grind") {
                    jump = 2;
                    clicked = true;
                    grind = true;
                    //  print("landed");
                }
                //Outside of above range is a failed click and landing
                if (hitForLanding.distance > failGrindClickMax && Input.GetButtonDown("LandGrind") && hit.collider.tag == "Grind" || hitForLanding.distance < failGrindClickMin && Input.GetButtonDown("LandGrind") && hit.collider.tag == "Grind") {
                    jump = 1;
                    clicked = true;
                    
                    playerAnim.SetBool("failedLanding", true);
                }
            }
            }

        }
    
    void calculatingMultiplyer() {

        if (m_grounded && clicked) {
            //failed click
            if (jump == 1) {
                jump = 0;
                slideMultiplyer = 1;
                // runSpeed = 15 + (slideMultiplyer * .25f);
                clicked = false;
            }
           // successfulClickMin clicked
            if (jump == 2) {
                jump = 0;
                slideMultiplyer += slideMultiAdditon;
               // runSpeed = 15 + (slideMultiplyer * .25f);
                clicked = false;
            }
        }
        //Didnt click
        if (m_grounded && !clicked)
            if (jump == 1) {
                jump = 0;
                slideMultiplyer = 1;
                // runSpeed = 15 + (slideMultiplyer * .25f);
                playerAnim.SetBool("failedLanding", true);
            }
    }
} 

