using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MergeMeals
{
    public class PickUpScript : MonoBehaviour
    {
        [Header("GRAPHICS")]
        [SerializeField] public RawImage mySquare;  //Called from MergeManager

        [Header("GROUND DETECTION")]
        [SerializeField] private Vector3 startPos;
        [SerializeField] private LayerMask whatIsGround;
        [SerializeField] private float groundLength = 1.4f;
        [SerializeField] private bool isGrounded = false;
        [SerializeField] public DateTime timeDropped = DateTime.Now;    //Checked by MergeManager
        private Rigidbody myRigidbody;

        [Header("USER INPUT")]
        [SerializeField] private float movementSpeed = 0.2f;    //1.0 is slow, 0.001 is stupid fast
        [SerializeField] private bool canBeLifted = true;
        [SerializeField] private bool currentlyBeingCarried = false;
        [SerializeField] private bool isMoving = false;
        [SerializeField] public bool fingerPressed = false;    //Altered by MergeManager
    	public InterpType interpolation = InterpType.SmootherStep;
    	public enum InterpType
    	{
    		Linear,
    		EaseOut,
    		EaseIn,
    		SmoothStep,
    		SmootherStep
    	};

        [Header("MATCHING")]
        public bool canBeMatched = true;    //Altered by MergeManager
        public bool aboutToBeMatch = false; //Altered by MergeManager

        [Header("POTENTIAL MATCH CHECKING")]
        [SerializeField] private bool IAmADudPlacementMarker = false;
        public bool IAmADudPlacementMarkerAndIMadeAMatch = false;    //Altered by MergeManager

        [Header("OCCUPIES INITIAL POSITION")]
        [SerializeField] private GameObject blockerPrefab;
        [SerializeField] private GameObject myBlocker;  //TODO Serializing this for a test, please remove!!!

        void Awake()
        {
            startPos = new Vector3(Mathf.Round(this.transform.position.x), 0.2f, Mathf.Round(this.transform.position.z));
            myRigidbody = gameObject.GetComponent<Rigidbody>();
            mySquare = gameObject.GetComponentInChildren<RawImage>();
            mySquare.enabled = false;
        }
        void Update()
        {
            Gravity();
        }
        public void Gravity()
        {
            if (!currentlyBeingCarried)
            {
                isGrounded = IsGrounded();
                //If initially falling, stop falling
                if (isGrounded && Mathf.Abs(myRigidbody.velocity.y) > 0) {
                    myRigidbody.velocity = Vector3.zero;
                    this.transform.position = SnapToGrid();
                }
                if (isGrounded) {
                    //OrientObject();
                }
                FloatDownWard();
            }
        }
        public Vector3 SnapToGrid()
        {
            int newX = (int) Mathf.Round(this.transform.position.x);
            float newY = this.transform.position.y;
            int newZ = (int) Mathf.Round(this.transform.position.z);
            return new Vector3(newX, newY, newZ);
        }
        public bool IsGrounded()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position+Vector3.up, transform.TransformDirection(Vector3.down), out hit, groundLength, whatIsGround)) {
                return true;
            }
            return false;
        }
        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position+Vector3.up, transform.TransformDirection(Vector3.down)*groundLength);

        }
        public void OrientObject()
        {
            this.transform.rotation = Quaternion.identity;
            Vector3 myRot = Vector3.up * 180;
            this.transform.localRotation = Quaternion.Euler(myRot);
        }
        public void FloatDownWard()
        {
            if (myRigidbody.velocity.y < -5f) {
                myRigidbody.velocity = Vector3.down *4f;
            }
        }
        //Wrapper Function for MoveToLocation() below it
        public void MoveToLocation(Vector3 desPos, bool movingBack = false) {
            StartCoroutine(MoveToLocation(desPos, movementSpeed, movingBack));
        }
        /*
        IEnumerator MoveToLocation(Vector3 initPos, Vector3 desPos)
        {
            if (!isMoving) {
                isMoving = true;
                SetBeingCarriedVariables();
                float timeOfTravel = 1f;
                float currentTime = 0;
                float normalizedValue;
                while (currentTime <= timeOfTravel) {
                    currentTime += Time.deltaTime * 7f;
                    normalizedValue = currentTime / timeOfTravel; // we normalize our time
                    if (!fingerPressed && !aboutToBeMatch) {
                        break;  //If player immediately released their finger, break from motion
                    }
                    this.transform.position = Vector3.Slerp(initPos, desPos, normalizedValue);
                    yield return null;
                }
                if (!fingerPressed && !aboutToBeMatch) {
                    this.transform.position = startPos;
                } else {
                    this.transform.position = desPos;
                }
                //OrientObject();
                isMoving = false;
            }
            yield return null;
        }
        */
    	IEnumerator MoveToLocation(Vector3 destination, float timeToMove, bool movingBack = false)
    	{
            if (!isMoving)
            {
                isMoving = true;
        		Vector3 startPosition = transform.position;
        		bool reachedDestination = false;
        		float elapsedTime = 0f;
                SetBeingCarriedVariables();
        		while (!reachedDestination)
        		{
        			// if we are close enough to destination
        			if (Vector3.Distance(transform.position, destination) < 0.01f)
        			{
        				reachedDestination = true;
        				// round our position to the final destination on integer values
        				transform.position = destination;
        				break;
        			}
        			// track the total running time
        			elapsedTime += Time.deltaTime;
        			// calculate the Lerp value
        			float t = Mathf.Clamp(elapsedTime / timeToMove, 0f, 1f);
        			switch (interpolation)
        			{
        				case InterpType.Linear:
        					break;
        				case InterpType.EaseOut:
        					t = Mathf.Sin(t * Mathf.PI * 0.5f);
        					break;
        				case InterpType.EaseIn:
        					t = 1 - Mathf.Cos(t * Mathf.PI * 0.5f);
        					break;
        				case InterpType.SmoothStep:
        					t = t*t*(3 - 2*t);
        					break;
        				case InterpType.SmootherStep:
        					t =  t*t*t*(t*(t*6 - 15) + 10);
        					break;
        			}
                    if (!movingBack) {
                        if (!fingerPressed && !aboutToBeMatch) {
                            break;  //If player immediately released their finger, break from motion
                        }
                    }
        			// move the game piece
        			transform.position = Vector3.Lerp(startPosition, destination, t);
        			// wait until next frame
        			yield return null;
        		}
                if (!movingBack) {
                    if (!fingerPressed && !aboutToBeMatch) {
                        this.transform.position = startPos;
                    } else {
                        this.transform.position = destination;
                    }
                }
        		isMoving = false;
            }
            yield return null;
    	}
        public void SetBeingCarriedVariables()
        {
            this.gameObject.GetComponent<BoxCollider>().isTrigger = true;
            currentlyBeingCarried = true;
            if (myRigidbody != null) {
                myRigidbody.useGravity = false;
            }
            Vector3 myPos = this.transform.position;
            if (myBlocker == null)
            {
                CreateDudPlacementMarker((int)myPos.x,(int)myPos.z);
            }
        }
        public void SetStartPos()
        {
            startPos = SnapToGrid();
        }
        public void BeingDropped()
        {
            this.gameObject.GetComponent<BoxCollider>().isTrigger = false;
            currentlyBeingCarried = false;
            timeDropped = DateTime.Now;
            if (myRigidbody != null) {
                myRigidbody.useGravity = true;
            }
            if (myBlocker != null)
            {
                Destroy(myBlocker);
                myBlocker = null;
            }
        }
        public void ReturnHome()
        {
            //Debug.Log("I'm being told to go back");
            //TODO THIS AINT WORKIN'
            MoveToLocation(startPos, true);
            //this.transform.position = startPos;
        }
        public void KillTimer(float time)
        {
            StartCoroutine(KillTimerRT(time));
        }
        IEnumerator KillTimerRT(float time)
        {
            if (myBlocker != null)
            {
                Destroy(myBlocker);
                myBlocker = null;
            }
            yield return new WaitForSeconds(time);
            Destroy(this.gameObject);
            yield return null;
        }
        public void CreateDudPlacementMarker(int myX, int myZ)
        {
            Vector3 pos = new Vector3(myX, 0.2f, myZ);
            myBlocker = Instantiate(blockerPrefab, pos, Quaternion.identity);

        }
        public bool IsMoving()  //Called by MergeManager
        {
            return isMoving;
        }
        public bool CurrentlyBeingCarried() //Called by MergeManager
        {
            return currentlyBeingCarried;
        }
        public bool AmIADudPlacementMarker()   //Called by MergeManager
        {
            return IAmADudPlacementMarker;
        }
        public bool CanBeLifted()   //Called by TouchManager
        {
            return canBeLifted;
        }
        public void MakeIntoADudPlacementMarkerFoodItem()
        {
            IAmADudPlacementMarker = true;

        }
        public int GetPlaceHolderID()
        {
            if (myBlocker != null)
            {
                return myBlocker.GetInstanceID();
            }
            return 0;
        }
    }
}
