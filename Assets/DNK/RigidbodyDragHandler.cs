using System.Collections;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
//using Utility.PlatformUtils;

namespace Utility.PhysicsUtils
{
    [RequireComponent(typeof(Collider))]
    public class RigidbodyDragHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IBeginDragHandler, IEndDragHandler, IDragHandler
    {
        public MeshRenderer helperMeshRenderer;

        public LayerMask raycastCollisionLayers = 1;

        public bool rotateToRay;

        public bool returnToOrigin;

        public bool dragIntoAir;

        public float maxVelocity = 5f;

        public bool limitDistance;

        public float maxRayDistance = 2f;

        public float amplifyZMotion = 1f;

        float beginDragRayDistance;

        public bool invertRotation;

        public float minDragDistance = 0.5f;

        public float maxAcceleration = 5f;

        public float lerpRotationSpeed = 5f;

        public float massModifier = 1f;

        [ReadOnly]
        public bool isDragging;

        public Vector3 planeNormal = Vector3.up;

        Vector3 beginDragPos;

        Collider _collider;

        public UnityEvent onBeginDrag;

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

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!isDragging && eventData.pointerCurrentRaycast.isValid)
            {
                if (helperMeshRenderer != null)
                    helperMeshRenderer.enabled = false;

                if (rb != null)
                {
                    useGravity = rb.useGravity;
                    rb.useGravity = false;
                }
                targetPosition = transform.position;

                beginDragPos = eventData.pointerCurrentRaycast.worldPosition;

                beginDragRayDistance = (eventData.pointerCurrentRaycast.worldPosition - eventData.pressEventCamera.transform.position).magnitude;

                if (limitDistance && beginDragRayDistance > maxRayDistance)
                    return;

                if (dragIntoAir)
                    beginDragRayDistance = Mathf.Min(maxRayDistance, beginDragRayDistance);
                //timestamp = Time.time;
                Debug.Log("OnBeginDrag");

                onBeginDrag.Invoke();

                isDragging = true;

                screenClickPosition = eventData.pointerCurrentRaycast.screenPosition;

                StartCoroutine(DragUpdateRoutine(eventData.pressEventCamera));
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (helperMeshRenderer != null)
                helperMeshRenderer.enabled = true;

            if (rb != null)
                rb.useGravity = useGravity;

            Debug.Log("OnEndDrag");

            targetPosition = Vector3.zero;

            isDragging = false;

            StopAllCoroutines();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            //Debug.Log("OnPointerClick");
        }

        public void OnBeginDrag(PointerEventData eventData)
        {

        }

        //float timestamp;

        Rigidbody rb;

        private void Start()
        {
            rb = transform.GetComponent<Rigidbody>();
            startPosition = transform.position;
            mr = GetComponent<MeshRenderer>();

            //if (helperMeshRenderer != null)
            // helperMeshRenderer.enabled = !helperMeshRenderer.enabled;
        }

        Vector3 startPosition;

        private void OnEnable()
        {
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.inertiaTensorRotation = Quaternion.identity;
            }
            //transform.parent.localPosition = startPosition;
        }

        Vector2 screenClickPosition;

        IEnumerator DragUpdateRoutine(Camera pressEventCamera)
        {
            while (isDragging)
            {
                if (rb != null && rb.isKinematic)
                    yield return null;
                //screenClickPosition = Input.mousePosition;

                if (screenClickPosition == Vector2.zero)
                    screenClickPosition = Input.mousePosition;//yield return null;

                Vector3 rayOrigin = pressEventCamera.ScreenToWorldPoint(new Vector3(screenClickPosition.x, screenClickPosition.y, pressEventCamera.nearClipPlane));

                Vector3 rayDir = (rayOrigin - pressEventCamera.transform.position).normalized;

                if (closestHit.collider != null)
                {
                    float deltaTime = Time.deltaTime;

                    Vector3 rayHitPos = closestHit.point;

                    if (closestHit.distance > maxRayDistance || closestHit.collider.gameObject == transform.gameObject)
                        rayHitPos = rayOrigin + rayDir * beginDragRayDistance;
                    else
                        rayHitPos = closestHit.point + closestHit.normal * collider.bounds.extents.y;

                    Debug.DrawLine(rayOrigin, closestHit.point, Color.green);

                    Debug.DrawLine(closestHit.point, rayHitPos, Color.yellow);

                    if (dragIntoAir)
                    {
                        targetPosition = rayHitPos;

                        beginDragPos = targetPosition;
                    }
                    else
                    {
                        Vector3 dragDeltaVector = rayHitPos - beginDragPos;

                        dragDeltaVector = Vector3.ProjectOnPlane(dragDeltaVector, planeNormal);

                        targetPosition = beginDragPos + dragDeltaVector;//Vector3.Lerp(transform.parent.position, transform.parent.position + dragDeltaVector, Time.deltaTime * lerpPositionSpeed);

                        beginDragPos = targetPosition;
                    }
                }
                else
                {
                    if (dragIntoAir)
                    {
                        //Vector3 rayOrigin = eventData.pressEventCamera.ScreenToWorldPoint(new Vector3(eventData.position.x, eventData.position.y, 0.3f));
                        //Vector3 rayDir = (rayOrigin - eventData.pressEventCamera.transform.position).normalized;

                        Vector3 rayHitPos = rayOrigin + rayDir * beginDragRayDistance;

                        //dragDeltaVector = pressEventCamera.transform.InverseTransformVector(dragDeltaVector);

                        //dragDeltaVector.z *= amplifyZMotion;

                        //dragDeltaVector = pressEventCamera.transform.TransformVector(dragDeltaVector);

                        Debug.DrawLine(rayOrigin, rayHitPos, Color.magenta, Time.deltaTime);
                        //if ((rayHitPos- beginDragPos).magnitude > minDragDistance * collider.bounds.size.magnitude)
                        targetPosition = rayHitPos;
                    }
                    else
                    {
                        Vector3 rayHitPos = rayOrigin + rayDir * beginDragRayDistance;

                        Vector3 dragDeltaVector = rayHitPos - beginDragPos;

                        dragDeltaVector = Vector3.ProjectOnPlane(dragDeltaVector, planeNormal);

                        targetPosition = beginDragPos + dragDeltaVector;//Vector3.Lerp(transform.parent.position, transform.parent.position + dragDeltaVector, Time.deltaTime * lerpPositionSpeed);

                        beginDragPos = targetPosition;
                    }
                }

                yield return null;
            }
        }

        public void SetTargetPositon(Vector3 pos)
        {
            targetPosition = pos;
        }

        Vector3 targetPosition;

        RaycastHit closestHit;

        Vector3 rayDir;

        public void OnDrag(PointerEventData eventData)
        {
            //Debug.Log("OnDrag");

            if (!isDragging)
                return;

            screenClickPosition = eventData.pointerCurrentRaycast.screenPosition;
            if (screenClickPosition == Vector2.zero)
                screenClickPosition = Input.mousePosition;

            Vector3 rayOrigin = eventData.pressEventCamera.ScreenToWorldPoint(new Vector3(screenClickPosition.x, screenClickPosition.y, eventData.pressEventCamera.nearClipPlane));

            rayDir = (rayOrigin - eventData.pressEventCamera.transform.position).normalized;

            Debug.DrawRay(rayOrigin, rayDir, Color.yellow, Time.deltaTime);

            closestHit = new RaycastHit();

            closestHit.distance = maxRayDistance;

            foreach (var hit in Physics.RaycastAll(rayOrigin, rayDir, maxRayDistance, raycastCollisionLayers))
            {
                if (hit.collider.gameObject != transform.gameObject)
                {
                    if (hit.distance <= closestHit.distance)
                    {
                        //closestHitDistance = hit;
                        closestHit = hit;
                    }
                }
            }

        }

        void Update()
        {
            
        }

        private void FixedUpdate()
        {
            if (targetPosition != Vector3.zero)
            {
                Vector3 fromTransformToTarget = (targetPosition - transform.position);
                if (!dragIntoAir)
                    fromTransformToTarget = Vector3.ProjectOnPlane(fromTransformToTarget, planeNormal);

                if (fromTransformToTarget.magnitude > 0)
                {
                    Quaternion targetRotation = transform.rotation;

                    if (rotateToRay)
                    {
                        targetRotation = Quaternion.LookRotation(rayDir, Vector3.up);
                    }
                    else if (fromTransformToTarget.magnitude > minDragDistance * collider.bounds.extents.magnitude)
                    {
                        targetRotation = Quaternion.LookRotation(invertRotation ? -fromTransformToTarget : fromTransformToTarget, planeNormal);
                    }

                    if (rb != null)
                    {
                        if (!rb.isKinematic)
                        {
                            rb.rotation = Quaternion.Lerp(rb.rotation, targetRotation, Time.fixedDeltaTime * lerpRotationSpeed);

                            Vector3 targetVelocity = fromTransformToTarget / Time.fixedDeltaTime;
                            targetVelocity = Vector3.ClampMagnitude(targetVelocity, maxVelocity);
                            //rb.transform.forward * maxVelocity;

                            rb.velocity = Vector3.MoveTowards(rb.velocity, targetVelocity * massModifier, Time.fixedDeltaTime * maxAcceleration);

                            //rb.inertiaTensorRotation = Quaternion.identity;
                            rb.angularVelocity = Vector3.zero;
                        }
                    }
                    else
                    {
                        targetPosition = transform.position + fromTransformToTarget;
                        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * lerpRotationSpeed);
                        transform.position = targetPosition;
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

                        if (rb.velocity.sqrMagnitude > 0.1f)
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

                Debug.DrawLine(transform.position, targetPosition, Color.red);
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
            OnPointerUp(eventData);
        }

        public void SetMaxVelocity(float value)
        {
            maxVelocity = value;
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