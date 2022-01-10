using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MergeMeals
{
    public class TouchManager : MonoBehaviour
    {
        Vector3 touchPos;
        [SerializeField] private Camera theCam;
        [SerializeField] private GameObject mergeManager;
        MergeManager mergeManagerScript;
        [SerializeField] private Vector3 touchTo3DWorldPos;
        [SerializeField] private LayerMask whatIsIngredient;
        [SerializeField] private PickUpScript grabbedObj;

        void Awake()
        {
            mergeManagerScript = mergeManager.GetComponent<MergeManager>();
        }
        void Start()
        {
            touchTo3DWorldPos = Vector3.one*(-100);
        }
        void Update()
        {
            DetermineTouch();
        }

        private void DetermineTouch()
        {
            if (Input.touchCount > 0)
            {
                Touch screenTouch = Input.GetTouch(0);
                touchTo3DWorldPos = ConvertScreenTo3DWorldPoint(screenTouch);
                if (touchTo3DWorldPos != Vector3.one * (-100))
                {   //(-100,-100,-100) means no item at location
                    if (screenTouch.phase == TouchPhase.Began)
                    {
                        grabbedObj = GetObjectAt3DWorldPoint(touchTo3DWorldPos);
                        //Initializes obj's placement values
                        if (grabbedObj != null)
                        {
                            if (grabbedObj.CanBeLifted())
                            {
                                mergeManagerScript.lastGrabbedObj = grabbedObj.gameObject;
                                grabbedObj.gameObject.layer = 8;    //This allows us to look for vacant locations
                                grabbedObj.SetBeingCarriedVariables();
                                grabbedObj.SetStartPos();
                            }
                        }
                        mergeManagerScript.FingerPressed();
                    }
                    else if (screenTouch.phase == TouchPhase.Ended)
                    {
                        if (grabbedObj != null)
                        {
                            if (IsObjectAt3DWorldPoint(touchTo3DWorldPos, grabbedObj.gameObject))
                            {
                                grabbedObj.ReturnHome();
                            }
                            mergeManagerScript.lastGrabbedObj = grabbedObj.gameObject;
                            grabbedObj.gameObject.layer = 6;    //This allows us to look for vacant locations (6 == "ingredients")
                            grabbedObj.BeingDropped();
                            grabbedObj.fingerPressed = false;
                        }
                        mergeManagerScript.FingerReleased();        //This is for interrupting the obj moving animation
                    }
                    else if (screenTouch.phase == TouchPhase.Moved || screenTouch.phase == TouchPhase.Stationary)
                    {
                        if (grabbedObj != null && grabbedObj.CanBeLifted())
                        {
                            if (Vector3.Distance(grabbedObj.transform.position, touchTo3DWorldPos) > 0.01f)
                            {
                                if (mergeManagerScript.IsWithinBounds(touchTo3DWorldPos))
                                {
                                    grabbedObj.MoveToLocation(touchTo3DWorldPos);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void SnapObjectToGrid(GameObject obj)
        {
            int newX = (int) Mathf.Round(obj.transform.position.x);
            int newZ = (int) Mathf.Round(obj.transform.position.z);
            obj.transform.position = new Vector3(newX, 0.4f, newZ);
        }
        Vector3 ConvertScreenTo3DWorldPoint(Touch touch)
        {
            Ray ray = theCam.ScreenPointToRay(touch.position);
            RaycastHit rayCastHit;
            if (Physics.Raycast(ray, out rayCastHit, Mathf.Infinity)) {
                Vector3 touch_ScreenToWorld = theCam.ScreenToWorldPoint(touch.position);
                float y_total = rayCastHit.point.y - touch_ScreenToWorld.y;                          // calculating the total y difference
                float newY = (y_total - rayCastHit.point.y);                // calculating the difference between hit.y and player's y position ...
                                                                                        // ... and subtracting it from the total y difference
                float factor = newY / y_total;                                          // multiplier in order to adjust the original length and reach the target position
                Vector3 pos = touch_ScreenToWorld + ((rayCastHit.point - touch_ScreenToWorld) * factor); // start of at the starting point and add the adjusted directional vector
                return new Vector3(Mathf.Round(pos.x), 0.4f, Mathf.Round(pos.z));
            }
            return Vector3.one*(-100);
        }
        PickUpScript GetObjectAt3DWorldPoint(Vector3 center)
        {
            Collider[] hitColliders = Physics.OverlapSphere(center, 0.2f, whatIsIngredient);
            for (int k = 0; k < hitColliders.Length; k++) {
                if (hitColliders[k].gameObject != null) {
                    return hitColliders[k].gameObject.GetComponent<PickUpScript>();
                }
            }
            return null;
        }
        //Overloaded function allows to check if not the same object that's being carried
        bool IsObjectAt3DWorldPoint(Vector3 center, GameObject myObj)
        {
            Collider[] hitColliders = Physics.OverlapSphere(center, 0.2f, whatIsIngredient);
            for (int k = 0; k < hitColliders.Length; k++) {
                if (hitColliders[k].gameObject != null) {
                    if (GameObject.ReferenceEquals(hitColliders[k].gameObject,myObj) == false) {
                        //return hitColliders[k].gameObject.GetComponent<PickUpScript>();
                        return true;
                    }
                }
            }
            return false;
        }
        void OnDrawGizmos()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(touchTo3DWorldPos, 0.2f);
        }
    }
}
