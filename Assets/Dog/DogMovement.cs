using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DogMovement : MonoBehaviour
{

    // Behaviour control variables
    [Range(0f, 3f)]
    public float speed = 1f;
    private float origSpeed = 1f;
    [Range(0f, 0.5f)]
    public float minWalk = 0.02f;
    [Range (0f, 6f)]
    public float secsToActivation = 6f;
    [Range(0f, 10f)]
    public float secsToRegainControlFromSmellingFloor = 3f;
    [Range(0f, 0.005f)]
    public float chanceOfSniffingFloor = 0.0005f;
    [Range(0f, 5f)]
    public float secsToStopForSniffingFloor = 2f;
    private float enterSniffModePcnt = 0f; // [0,1]
    private const float EPSILON = 0.025f;

    // References
    private GameManager gm = null;
    private Animator anim = null;
    private int sniffLayerIndex = 0;
    private float currSniffLayerWeight = 0f;
    public GameObject smellSeq;
    public Transform draggableTire = null;
    private Transform draggableTireOriginalParent = null;

    // State info
    private float axis = 0f;
    private bool isRight = true;
    private bool isWalking = false;
    private bool isDragging = false;
    private bool isCurrDirectionRight = true;
    private bool isSniffingFloor = false;
    private bool inTireScentArea = false;
    private bool isLosingCtrl = false;
    private bool hasControl = true;
    private bool approachedTire = false;
    private float timeToApproachTire = Mathf.Infinity;
    

    // Start is called before the first frame update
    void Awake()
    {
        Animator[] animators = GetComponentsInChildren<Animator>();
        foreach (Animator animator in animators)
        {
            if (animator.gameObject.GetInstanceID() != GetInstanceID())
            {
                this.anim = animator;
            }
        }
        sniffLayerIndex = anim.GetLayerIndex("SniffFloor");
        origSpeed = speed;
        this.gm = FindObjectOfType<GameManager>();
        
    }

    // Update is called once per frame
    void Update()
    {

        // Start Interaction. Freezes controls
        if (ShouldApproachTire())
            ApproachTire();

        // Randomly smell floor. Freezes control
        else if (ShouldStartSniffingFloor())
            StartSniffingFloor();

        // Player controled actions
        else if (hasControl)
            HandlePlayerInput();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Start sniffing
        if (other.tag.Equals("SmellActivate") && !approachedTire) {
            inTireScentArea = true;
            SetTimeToTire();
            anim.SetBool("isSniffing", true);
        }

        //Drop Tire
        else if (other.tag.Equals("DropTireSpot")) {
            OnTireDropped();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag.Equals("SmellActivate"))
        {
            inTireScentArea = false;
            StopSniffing();
        }
    }


    // Player Ctrl logic
    private void HandlePlayerInput() {
        axis = Input.GetAxis("Horizontal");
        bool isWalkingCurr = Mathf.Abs(axis) > minWalk;
        if (isWalkingCurr && !isWalking && !isLosingCtrl)
        {
            OnFinishedSniffingFloor();
            anim.SetTrigger("startWalking");
        }
        isWalking = isWalkingCurr;

        //TODO: shame to re-set these every update
        anim.SetBool("isWalking", isWalking); 
        bool isRegainingCtrl = hasControl && isWalking && speed < origSpeed;

        if (isLosingCtrl)
        {
            speed -= Time.deltaTime * (origSpeed / secsToStopForSniffingFloor);
            currSniffLayerWeight = anim.GetLayerWeight(sniffLayerIndex);
            anim.SetLayerWeight(sniffLayerIndex, currSniffLayerWeight + Time.deltaTime / secsToStopForSniffingFloor);
            if (speed <= EPSILON)
            {
                speed = 0;
                isLosingCtrl = false;
                hasControl = false;
            }
        }
        else if (isRegainingCtrl) {
            speed = Mathf.Clamp(speed + Time.deltaTime * (origSpeed / secsToStopForSniffingFloor), 0f, origSpeed);
            currSniffLayerWeight = anim.GetLayerWeight(sniffLayerIndex);
            anim.SetLayerWeight(sniffLayerIndex, currSniffLayerWeight - Time.deltaTime / secsToStopForSniffingFloor);
        }

        if (isWalking)
            HandleWalk();
    }

    // Walk logic
    private void HandleWalk() {
        //OnFinishedSniffingFloor(); // just in case
        isCurrDirectionRight = axis > 0;
        if (isCurrDirectionRight != isRight && !isDragging)
        {
            transform.Rotate(new Vector3(0, 180, 0));
            isRight = isCurrDirectionRight;
        }
        if (!isLosingCtrl && speed > 0) {
            anim.SetBool("isSniffingFloor", false);
        }

        anim.SetBool("isLeft", !isCurrDirectionRight);
        Vector3 direction = transform.forward * Mathf.Sign(axis);
        transform.Translate(direction * speed * Time.deltaTime);
    }

    // If player interrupted or left tire area. 
    private void StopSniffing() {
        ResetTimeToTire();
        anim.SetBool("isSniffing", false);
    }

    // Cancel plan to approach tire
    private void ResetTimeToTire() {
        timeToApproachTire = Mathf.Infinity;
    }

    // Schedule time to approach tire
    private void SetTimeToTire() {
        timeToApproachTire = Time.time + secsToActivation;
    }

    // Did the plaer wait enough? 
    private bool ShouldApproachTire() {
        return Time.time >= timeToApproachTire && !approachedTire && !isSniffingFloor;
    }

    // Randomly revoke control from player
    private bool ShouldStartSniffingFloor() {
        return 
            Random.Range(0f, 1f) <= chanceOfSniffingFloor // Random smell
            && !inTireScentArea // Not while in tire scent area
            && !isSniffingFloor // No self overlaps
            && !approachedTire; // Only before tire interaction
    }

    // Activate sequence, go deeper in z space
    private void ApproachTire() {
        approachedTire = true;
        Vector3 moveVector = smellSeq.transform.position - this.transform.position;
        if (!isRight)
            iTween.RotateBy(gameObject, iTween.Hash("y", .5, "easeType", "easeInOutBack", "delay", .75));
        iTween.MoveBy(gameObject, iTween.Hash("x", moveVector.x, "y", moveVector.y, "z", moveVector.z, "easeType", "easeInQuad", "delay", 1.25, "oncomplete", "BeginTireSequence"));
        
    }

    // Activate sequence, go deeper in z space
    private void BeginTireSequence() {
        gm.PlaySmellSeq();
        anim.SetBool("isDragging", true);
    }

    // This will fire by the gm once the sequence is over
    public void OnTirePickedUp() {
        isDragging = true;
        anim.SetBool("isDragging", true);
        speed = origSpeed / 4;
        if (draggableTire != null) {
            draggableTireOriginalParent = draggableTire.parent;
            draggableTire.parent = transform;
        }
    }

    // this will fire once the player droped the tire.
    private void OnTireDropped() {
        if (draggableTire != null && draggableTireOriginalParent != null)
        {
            draggableTire.parent = draggableTireOriginalParent;
            anim.SetBool("isDragging", false);
            speed = origSpeed;
            isDragging = false;
            
        }
    }

    // Freeze player control
    private void StartSniffingFloor() {
        anim.SetBool("isSniffingFloor", true);
        anim.SetTrigger("SniffFloor");
        isSniffingFloor = true;
        FreezeControl();
    }

    // When player interrupts floor sniffing
    public void OnFinishedSniffingFloor() {
        anim.SetBool("isSniffingFloor", false);
        isSniffingFloor = false;
    }

    // Re-nable movement after secsToRegainControlFromSmellingFloor
    private IEnumerator GainControl() {
        yield return new WaitForSeconds(secsToRegainControlFromSmellingFloor);
        speed = origSpeed;
        isLosingCtrl = false;
        hasControl = true;   
    }

    // Disable movement for secsToRegainControlFromSmellingFloor secs
    private void FreezeControl() {
        //hasControl = false;
        isLosingCtrl = true;
        StartCoroutine("GainControl");
    }


}
