using UnityEngine;

using UnityEngine.EventSystems;

namespace Utility.PhysicsUtils
{
    [RequireComponent(typeof(Collider))]
    public class RigidbodyDragController : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {

        public bool returnToOrigin;

        public bool dragIntoAir;

        public float maxVelocity = 5f;

        public bool limitDistance;

        public float maxRayDistance = 2f;

        float beginDragRayDistance;

        public bool invertRotation;

        public float minDragDistance = 0.5f;

        public float maxAcceleration = 5f;

        public float lerpRotationSpeed = 5f;

        [SerializeField]
        bool isDragging;

        Vector3 beginDragPos;

        Collider _collider;

        new Collider collider
        {
            get
            {
                if (_collider == null)
                    _collider = GetComponent<Collider>();
                return _collider;
            }
        }

        MeshRenderer mr;

        bool useGravity;

        public void OnBeginDrag(PointerEventData eventData)
        {
            useGravity = rb.useGravity;
            rb.useGravity = false;

            if (eventData.pointerCurrentRaycast.isValid)
            {
                beginDragPos = eventData.pointerCurrentRaycast.worldPosition;

                float beginDragDistance = (eventData.pointerCurrentRaycast.worldPosition - eventData.pressEventCamera.transform.position).magnitude;

                if (limitDistance && beginDragDistance > maxRayDistance)
                    return;

                if (dragIntoAir)
                    beginDragRayDistance = Mathf.Min(maxRayDistance, beginDragDistance);
                //timestamp = Time.time;
                Debug.Log("OnBeginDrag");
                isDragging = true;
            }

        }

        //float timestamp;

        Rigidbody rb;

        private void Start()
        {
            rb = transform.parent.GetComponent<Rigidbody>();
            startPosition = transform.parent.position;
            mr = GetComponent<MeshRenderer>();
            if (mr != null)
                mr.enabled = false;
        }

        Vector3 startPosition;

        private void OnEnable()
        {
            //transform.parent.localPosition = startPosition;
        }

        void Update()
        {
            if (isDragging)
            {
                //if (mr!=null)
                //    mr.enabled = true;
            }
            else
            {
                if (mr != null)
                    mr.enabled = false;
            }
        }

        public void SetMaxVelocity(float value)
        {
            maxVelocity = value;
        }

        Vector3 targetPosition;

        public void OnDrag(PointerEventData eventData)
        {
            //Debug.Log("OnDrag");

            if (!isDragging)
                return;

            if (eventData.pointerCurrentRaycast.isValid)
            {
                float deltaTime = Time.deltaTime;//(Time.time - timestamp);

                //timestamp = Time.time;

                Vector3 rayHitPos = eventData.pointerCurrentRaycast.worldPosition;// + eventData.pointerCurrentRaycast.worldNormal * collider.bounds.extents.magnitude;

                Vector3 rayOrigin = eventData.pressEventCamera.ScreenToWorldPoint(new Vector3(eventData.pointerCurrentRaycast.screenPosition.x, eventData.pointerCurrentRaycast.screenPosition.y, 0.3f));

                Vector3 rayDir = (rayOrigin - eventData.pressEventCamera.transform.position).normalized;

                if (eventData.pointerCurrentRaycast.distance > maxRayDistance || eventData.pointerCurrentRaycast.gameObject == transform.parent.gameObject || eventData.pointerCurrentRaycast.gameObject == gameObject)
                    rayHitPos = rayOrigin + rayDir * maxRayDistance;

                RaycastHit closestHit = new RaycastHit();

                closestHit.distance = maxRayDistance;

                foreach (var hit in Physics.RaycastAll(rayOrigin, rayDir, maxRayDistance, collider.gameObject.layer ))
                {
                    if (hit.collider.gameObject != transform.parent.gameObject && hit.collider.gameObject != gameObject)
                    {
                        if (hit.distance <= closestHit.distance)
                        {
                            //closestHitDistance = hit;
                            closestHit = hit;
                        }
                    }
                }

                if (closestHit.collider != null)
                    rayHitPos = closestHit.point + closestHit.normal * collider.bounds.extents.y;

                Debug.DrawLine(rayOrigin, closestHit.point, Color.green);

                Debug.DrawLine(closestHit.point, rayHitPos, Color.yellow);

                if (dragIntoAir)
                {
                    //Vector3 rayOrigin = eventData.pressEventCamera.ScreenToWorldPoint(new Vector3(eventData.pointerCurrentRaycast.screenPosition.x, eventData.pointerCurrentRaycast.screenPosition.y, 0.3f));
                    //Vector3 rayDir = (rayOrigin - eventData.pressEventCamera.transform.position).normalized;

                    //rayHitPos = rayOrigin + rayDir * rayDistance;

                    //dragDeltaVector = rayHitPos - beginDragPos;

                    targetPosition = rayHitPos; //beginDragPos + dragDeltaVector;//Vector3.Lerp(transform.parent.position, transform.parent.position + dragDeltaVector, Time.deltaTime * lerpPositionSpeed);

                    beginDragPos = targetPosition;
                }
                else
                {
                    Vector3 dragDeltaVector = rayHitPos - beginDragPos;

                    dragDeltaVector = Vector3.ProjectOnPlane(dragDeltaVector, transform.parent.up);

                    targetPosition = beginDragPos + dragDeltaVector;//Vector3.Lerp(transform.parent.position, transform.parent.position + dragDeltaVector, Time.deltaTime * lerpPositionSpeed);

                    beginDragPos = targetPosition;
                }

                //if (dragDeltaVector.magnitude > minDragDistance * collider.bounds.size.magnitude)
                //{


                //}
            }
            else
            {
                if (dragIntoAir)
                {
                    Vector3 rayOrigin = eventData.pressEventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 0.3f));
                    Vector3 rayDir = (rayOrigin - eventData.pressEventCamera.transform.position).normalized;

                    Vector3 rayHitPos = rayOrigin + rayDir * beginDragRayDistance;

                    //Debug.DrawLine(rayOrigin, rayHitPos, Color.cyan, 1f);
                    //if ((rayHitPos- beginDragPos).magnitude > minDragDistance * collider.bounds.size.magnitude)
                    targetPosition = rayHitPos;
                }
            }
        }

        private void FixedUpdate()
        {
            if (targetPosition != Vector3.zero)
            {
                Vector3 fromParentToTarget = (targetPosition - transform.parent.position);
                if (!dragIntoAir)
                    fromParentToTarget = Vector3.ProjectOnPlane(fromParentToTarget, transform.parent.up);

                if (fromParentToTarget.magnitude > 0)
                {
                    Quaternion targetRotation = transform.parent.rotation;

                    if (fromParentToTarget.magnitude > minDragDistance * collider.bounds.extents.magnitude)
                    {
                        targetRotation = Quaternion.LookRotation(invertRotation ? -fromParentToTarget : fromParentToTarget, Vector3.up);
                    }

                    if (rb != null)
                    {
                        rb.rotation = Quaternion.Lerp(rb.rotation, targetRotation, Time.fixedDeltaTime * lerpRotationSpeed);

                        Vector3 targetVelocity = fromParentToTarget / Time.fixedDeltaTime;
                        targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);
                        //rb.transform.forward * maxVelocity;

                        rb.velocity = Vector3.MoveTowards(rb.velocity, targetVelocity, Time.fixedDeltaTime * maxAcceleration);
                        //rb.inertiaTensorRotation = Quaternion.identity;
                        rb.angularVelocity = Vector3.zero;

                    }
                    else
                    {
                        targetPosition = transform.parent.position + fromParentToTarget;
                        transform.parent.rotation = Quaternion.Lerp(transform.parent.rotation, targetRotation, Time.fixedDeltaTime * lerpRotationSpeed);
                        transform.parent.position = targetPosition;
                    }
                }
                else
                {
                    if (rb != null)
                    {
                        //targetPosition = rb.transform.position;
                        rb.velocity = Vector3.MoveTowards(rb.velocity, Vector3.zero, Time.fixedDeltaTime * maxAcceleration);
                        //rb.inertiaTensorRotation = Quaternion.identity;
                        rb.angularVelocity = Vector3.zero;

                        if (rb.velocity.sqrMagnitude > 1.0f)
                        {

                            //rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(rb.velocity, rb.transform.up), Time.fixedDeltaTime * lerpRotationSpeed);
                        }
                        else
                        {
                            if (!isDragging && returnToOrigin)
                                targetPosition = startPosition;
                        }
                    }
                }

                Debug.DrawLine(transform.parent.position, targetPosition, Color.red);
            }
            /*else
            {
                if (rb != null)
                {
                    rb.velocity = Vector3.Lerp(rb.velocity, Vector3.zero, Time.fixedDeltaTime * lerpPositionSpeed);
                    if (rb.velocity.sqrMagnitude > 0f)
                        rb.rotation = Quaternion.Lerp(rb.rotation, Quaternion.LookRotation(rb.velocity, rb.transform.up), Time.fixedDeltaTime * lerpRotationSpeed);
                }
            }*/


        }

        public void OnEndDrag(PointerEventData eventData)
        {
            rb.useGravity = useGravity;

            Debug.Log("OnEndDrag");

            targetPosition = Vector3.zero;

            isDragging = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("OnPointerClick");
        }

        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(transform.position, minDragDistance * collider.bounds.extents.magnitude);
            Gizmos.color = Color.red;
            if (Application.isPlaying)
                Gizmos.DrawWireSphere(targetPosition, minDragDistance * collider.bounds.extents.magnitude);
        }
    }
}