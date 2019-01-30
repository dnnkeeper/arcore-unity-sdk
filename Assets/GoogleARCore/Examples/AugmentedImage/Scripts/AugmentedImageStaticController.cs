namespace GoogleARCore.Examples.AugmentedImage
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using GoogleARCore;
    using GoogleARCore.Examples.CloudAnchors;
    using GoogleARCore.Examples.Common;
    using UnityEngine;
    using UnityEngine.Events;
    using UnityEngine.SpatialTracking;
    using UnityEngine.UI;

    /// <summary>
    /// Controller for AugmentedImage example.
    /// </summary>
    public class AugmentedImageStaticController : MonoBehaviour
    {
        public bool usePlaneAnchors;

        public bool usePlaneHorizonCorrection;

        public Transform offsetOrigin;

        public Transform lastAnchorTransform;

        public Transform recognizedImageOriginTransform;

        public Transform deviceTrackerTransform;

        public Transform cameraTransform;

        public TrackedPoseDriver tracker;

        private Dictionary<AugmentedImage, Anchor> imageAnchors
            = new Dictionary<AugmentedImage, Anchor>();

        public Dictionary<DetectedPlane, Anchor> detectedPlaneAnchors
           = new Dictionary<DetectedPlane, Anchor>();

        public UnityEvent onHasMarkers;

        public UnityEvent onZeroMarkers;

        /// <summary>
        /// The overlay containing the fit to scan user guide.
        /// </summary>
        public GameObject FitToScanOverlay;

        private List<AugmentedImage> updatedAugmentedImages = new List<AugmentedImage>();

        public Dictionary<int, VirtualMarker> virtualMarkersDict 
            = new Dictionary<int, VirtualMarker>();

        ARCoreSession session;

        IEnumerator resetSession()
        {
            session.enabled = false;

            Debug.Log("reset ar core session");

            yield return null;

            session.enabled = true;
        }

        GameObject[] virtualScenes;

        private void Start()
        {
            virtualScenes = GameObject.FindGameObjectsWithTag("VirtualScene");

            if (!Application.isEditor)
            {
                SetVirtualSceneActive(false);
                onZeroMarkers.Invoke();
            }

            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            if (tracker == null)
                tracker = FindObjectOfType<TrackedPoseDriver>();

            session = FindObjectOfType<ARCoreSession>();
            
            var virtualMarkers = GameObject.FindObjectsOfType<VirtualMarker>();
            foreach (var virtualMarker in virtualMarkers)
            {
                if (session != null)// && session.SessionConfig.AugmentedImageDatabase == virtualMarker.imageDatabase)
                {
                    if (!virtualMarkersDict.ContainsKey(virtualMarker.databaseIndex))
                    {
                        virtualMarkersDict.Add(virtualMarker.databaseIndex, virtualMarker);
                    }
                    else
                    {
                        Debug.LogError("virtualMarkersDict already contains marker with databaseIndex "+ virtualMarker.databaseIndex);
                    }

                    virtualMarker.gameObject.SetActive(false);
                }
            }
        }

        VirtualMarker lastTrackedMarker;

        //public int lastTrackedIdx;

        public int markersCount = -1;

        Anchor originAnchor;


        /// <summary>
        /// A prefab for tracking and visualizing detected planes.
        /// </summary>
        public GameObject DetectedPlanePrefab;

        /// <summary>
        /// A list to hold new planes ARCore began tracking in the current frame. This object is used across
        /// the application to avoid per-frame allocations.
        /// </summary>
        private List<DetectedPlane> m_NewPlanes = new List<DetectedPlane>();


        //public Text planeInfoText;

        Anchor lastTrackedAnchor = null;

        Anchor globalAnchor = null;

        bool hasAnyTrackedImages;

        /// <summary>
        /// The Unity Update method.
        /// </summary>
        public void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Application.Quit();

            hasAnyTrackedImages = false;

            // Check that motion tracking is tracking.
            if (Session.Status == SessionStatus.Tracking)
            {
                // Get updated augmented images for this frame.
                Session.GetTrackables<AugmentedImage>(updatedAugmentedImages, TrackableQueryFilter.Updated);

                // Create visualizers and anchors for updated augmented images that are tracking and do not previously
                // have a visualizer. Remove visualizers for stopped images.
                foreach (var image in updatedAugmentedImages)
                {
                    imageAnchors.TryGetValue(image, out Anchor anchor);

                    if (image.TrackingState == TrackingState.Tracking)
                    {
                        if (anchor == null)
                        {
                            var centerPosePosition = image.CenterPose.position;//deviceTrackerTransform.TransformPoint(image.CenterPose.position);

                            var centerPoseRotation = image.CenterPose.rotation;//deviceTrackerTransform.rotation * image.CenterPose.rotation;

                            var pose = new Pose(centerPosePosition, centerPoseRotation);

                            anchor = image.CreateAnchor(pose);

                            if (virtualMarkersDict.TryGetValue(image.DatabaseIndex, out VirtualMarker virtualMarker))
                            {
                                lastTrackedAnchor = anchor;

                                lastTrackedMarker = virtualMarker;

                                

                                var visualizer = virtualMarker.GetComponentInChildren<AugmentedImageVisualizer>();
                                if (visualizer != null)
                                {
                                    visualizer.Image = image;
                                }

                                if (recognizedImageOriginTransform != null)
                                {
                                    if (virtualMarker == null)
                                        Debug.LogWarning("virtualMarker == null");
                                    else
                                    {
                                        recognizedImageOriginTransform.parent = anchor.transform;
                                        recognizedImageOriginTransform.transform.localPosition = -virtualMarker.transform.position;//image.CenterPose.position;
                                        recognizedImageOriginTransform.transform.localRotation = Quaternion.Inverse(virtualMarker.transform.rotation);
                                    }
                                }
                            }

                            imageAnchors.Add(image, anchor);

                            SetMarkerActive(image.DatabaseIndex, true);
                        }
                        else
                        {
                            anchor.transform.SetPositionAndRotation(image.CenterPose.position, image.CenterPose.rotation);
                            if (virtualMarkersDict.TryGetValue(image.DatabaseIndex, out VirtualMarker virtualSceneMarker))
                            {
                                lastTrackedAnchor = anchor;

                                lastTrackedMarker = virtualSceneMarker;
                            }

                        }
                    }
                    else if (image.TrackingState == TrackingState.Stopped && anchor != null)
                    {
                        imageAnchors.Remove(image);

                        SetMarkerActive(image.DatabaseIndex, false);

                        GameObject.Destroy(anchor.gameObject);
                    }
                }

                // Show the fit-to-scan overlay if there are no images that are Tracking.
                foreach (var image in imageAnchors.Keys)
                {
                    var anchor = imageAnchors[image];

                    bool isTracked = image.TrackingState == TrackingState.Tracking;

                    SetMarkerActive(image.DatabaseIndex, isTracked, out VirtualMarker vm);

                    if (vm == null)
                    {
                        imageAnchors.Remove(image);

                        SetMarkerActive(image.DatabaseIndex, false);

                        GameObject.Destroy(anchor.gameObject);
                    }

                    hasAnyTrackedImages |= isTracked;
                }
               
                // Iterate over planes found in this frame and instantiate corresponding GameObjects to visualize them.
                Session.GetTrackables<DetectedPlane>(m_NewPlanes, TrackableQueryFilter.Updated);
                for (int i = 0; i < m_NewPlanes.Count; i++)
                {
                    DetectedPlane detectedPlane = m_NewPlanes[i];

                    detectedPlaneAnchors.TryGetValue(detectedPlane, out Anchor anchor);

                    if (detectedPlane.TrackingState == TrackingState.Tracking && anchor == null)
                    {
                        if (lastTrackedMarker != null && lastTrackedAnchor != null)
                        {
                            Vector3 eulerAnchorRotation = lastTrackedAnchor.transform.rotation.eulerAngles;
                            Vector3 virtualMarkerRotation = lastTrackedMarker.transform.rotation.eulerAngles;
                            if (usePlaneHorizonCorrection && detectedPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing && (Mathf.Abs(virtualMarkerRotation.x) < 1f && Mathf.Abs(virtualMarkerRotation.z) < 1f))
                            {
                                eulerAnchorRotation.x = detectedPlane.CenterPose.rotation.eulerAngles.x;

                                eulerAnchorRotation.z = detectedPlane.CenterPose.rotation.eulerAngles.z;

                                Debug.LogWarning("detectedPlane Angle diff: " + (detectedPlane.CenterPose.rotation.eulerAngles - lastTrackedAnchor.transform.rotation.eulerAngles));

                                lastTrackedAnchor.transform.rotation = Quaternion.LookRotation( Vector3.Cross(detectedPlane.CenterPose.rotation * Vector3.up, lastTrackedAnchor.transform.right), detectedPlane.CenterPose.rotation * Vector3.up);

                                Debug.LogWarning("corrected Angle diff: " + (detectedPlane.CenterPose.rotation.eulerAngles - lastTrackedAnchor.transform.rotation.eulerAngles));
                            }

                            anchor = detectedPlane.CreateAnchor(new Pose(lastTrackedAnchor.transform.position, Quaternion.Euler(eulerAnchorRotation)));
                            //(new Pose(lastTrackedAnchor.transform.position, lastTrackedAnchor.transform.rotation));
                            //(detectedPlane.CenterPose);

                            anchor.gameObject.name = detectedPlane.ToString()+" anchor";

                            //anchor.transform.parent = deviceTrackerTransform;

                            detectedPlaneAnchors.Add(detectedPlane, anchor);

                            // Instantiate a plane visualization prefab and set it to track the new plane. The transform is set to
                            // the origin with an identity rotation since the mesh for our prefab is updated in Unity World
                            // coordinates.
                            GameObject planeObject = Instantiate(DetectedPlanePrefab, offsetOrigin.transform);

                            planeObject.GetComponent<DetectedPlaneVisualizer>().Initialize(detectedPlane);

                            planeObject.transform.SetPositionAndRotation(offsetOrigin.transform.position, offsetOrigin.transform.rotation);

                            planeObject.transform.parent = offsetOrigin;
                            //var newInfo = GameObject.Instantiate(planeInfoText.gameObject, planeInfoText.gameObject.transform.parent);
                            //newInfo.GetComponent<Text>().text = planeObject.name+" "+planeObject.transform.rotation.eulerAngles;
                            //newInfo.SetActive(true);

                            if (usePlaneAnchors)
                                lastTrackedAnchor = anchor;
                        }

                    }
                    else if (detectedPlane.TrackingState == TrackingState.Stopped && anchor != null)
                    {
                        Debug.LogWarning(detectedPlane + " tracking STOPPED");

                        detectedPlaneAnchors.Remove(detectedPlane);

                        GameObject.Destroy(anchor.gameObject);
                    }
                }
                

                if (globalAnchor != null)
                    lastTrackedAnchor = globalAnchor;

                if (lastTrackedAnchor != null)
                {
                    lastAnchorTransform.localPosition = lastTrackedAnchor.transform.position;

                    lastAnchorTransform.localRotation = lastTrackedAnchor.transform.rotation;

                    //Quaternion localRot = Quaternion.Inverse(lastTrackedAnchor.transform.rotation) * tracker.transform.rotation;

                    //Vector3 localPos = lastTrackedAnchor.transform.InverseTransformPoint(tracker.transform.position);

                    //if (virtualMarkersDict.TryGetValue(lastTrackedIdx, out VirtualMarker virtualSceneMarker))
                   
                }
            }

            if (globalAnchor != null)
                GameObject.Destroy(globalAnchor.gameObject);

            if (!Application.isEditor)
            {

                if (hasAnyTrackedImages || globalAnchor != null)
                {
                    FitToScanOverlay.SetActive(false);

                    if (markersCount == 0)
                    {
                        markersCount = imageAnchors.Count + detectedPlaneAnchors.Count;

                        SetVirtualSceneActive(true);

                        onHasMarkers.Invoke();
                    }
                }
                else
                {
                    FitToScanOverlay.SetActive(true);

                    //imageAnchors.Clear();
                    //detectedPlaneAnchors.Clear();

                    if (markersCount != 0)
                    {
                        markersCount = 0;
                        SetVirtualSceneActive(false);
                        onZeroMarkers.Invoke();
                    }
                }
            }
        }

        // callback to be called before any camera starts rendering
        public void MyPreRender(Camera cam)
        {
#if !UNITY_EDITOR
            SyncCameraWithTracker();
#endif
        }

        public void OnEnable()
        {
            // register the callback when enabling object
            Camera.onPreRender += MyPreRender;
        }

        public void OnDisable()
        {
            // remove the callback when disabling object
            Camera.onPreRender -= MyPreRender;
        }
        
        void SyncCameraWithTracker()
        {
            if (lastTrackedMarker != null && lastTrackedAnchor != null)
            {
                offsetOrigin.SetPositionAndRotation(
                                    lastTrackedMarker.transform.TransformPoint(lastTrackedAnchor.transform.InverseTransformPoint(deviceTrackerTransform.position)),
                                    lastTrackedMarker.transform.rotation * (Quaternion.Inverse(lastTrackedAnchor.transform.rotation) * deviceTrackerTransform.rotation));
            }

            //cameraTransform.position = lastTrackedMarker.transform.TransformPoint(localPos);
            cameraTransform.localPosition = tracker.transform.localPosition;
            //Vector3.Lerp(cameraTransform.localPosition, tracker.transform.localPosition, Time.deltaTime * 10f);

            //cameraTransform.rotation = lastTrackedMarker.transform.rotation * localRot;
            cameraTransform.localRotation = tracker.transform.localRotation;
            //Quaternion.Lerp(cameraTransform.localRotation, tracker.transform.localRotation, Time.deltaTime * 10f);
        }

        public void SetVirtualSceneActive(bool b)
        {
            foreach(var virtualScene in virtualScenes) //GameObject.FindGameObjectsWithTag("VirtualScene"))
            {
                virtualScene.SetActive(b);
            }
        }

        //public void EnableMarker(int databaseIndex)
        //{
        //    foreach(var kvp in virtualMarkersDict)
        //    {
        //        if (kvp.Key == databaseIndex)
        //        {
        //            kvp.Value.gameObject.SetActive(true);
        //        }
        //        else
        //        {
        //            kvp.Value.gameObject.SetActive(false);
        //        }
        //    }
        //}

        void SetMarkerActive(int databaseIndex, bool value)
        {
            VirtualMarker virtualSceneMarker = null;

            if (virtualMarkersDict.TryGetValue(databaseIndex, out virtualSceneMarker))
            {
                virtualSceneMarker.gameObject.SetActive(value);
            }
        }

        void SetMarkerActive(int databaseIndex, bool value, out VirtualMarker virtualSceneMarker)
        {
            //VirtualMarker virtualSceneMarker = null;

            if (virtualMarkersDict.TryGetValue(databaseIndex, out virtualSceneMarker))
            {
                virtualSceneMarker.gameObject.SetActive(value);
            }
        }

#if UNITY_EDITOR
        private void OnGUI()
        {
            GUILayout.Space(Screen.height*0.5f);

            GUILayout.Label(Session.Status.ToString());
            
            GUILayout.Label("hasAnyTrackedImages " + hasAnyTrackedImages);

            if (lastTrackedMarker != null)
                GUILayout.Label("lastTrackedMarker " + lastTrackedMarker.transform.position + " " + lastTrackedMarker.transform.rotation.eulerAngles);

            if (lastTrackedAnchor != null)
            {
                GUILayout.Label(lastTrackedAnchor.gameObject.name+ " TrackingState: " + lastTrackedAnchor.TrackingState);
                GUILayout.Label("lastTrackedAnchor " + lastTrackedAnchor.transform.position + " " + lastTrackedAnchor.transform.rotation.eulerAngles);
            }
            /*foreach(var VM in virtualMarkersDict.Values)
            {
                GUILayout.Label(VM.name);
            }*/
        }
#endif
    }
}
