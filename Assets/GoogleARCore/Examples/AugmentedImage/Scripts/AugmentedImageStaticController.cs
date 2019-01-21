namespace GoogleARCore.Examples.AugmentedImage
{
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
        public bool usePlanes;

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

        private void Start()
        {
            if (!Application.isEditor)
                onZeroMarkers.Invoke();

            if (cameraTransform == null)
                cameraTransform = Camera.main.transform;

            if (tracker == null)
                tracker = FindObjectOfType<TrackedPoseDriver>();

            var session = FindObjectOfType<ARCoreSession>();
            
            var virtualMarkers = GameObject.FindObjectsOfType<VirtualMarker>();
            foreach (var virtualMarker in virtualMarkers)
            {
                if (session != null && session.SessionConfig.AugmentedImageDatabase == virtualMarker.imageDatabase)
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
                            var centerPosePosition = deviceTrackerTransform.TransformPoint(image.CenterPose.position);

                            var centerPoseRotation = deviceTrackerTransform.rotation * image.CenterPose.rotation;

                            var pose = new Pose(centerPosePosition, centerPoseRotation);

                            anchor = image.CreateAnchor(pose);

                            anchor.transform.parent = deviceTrackerTransform;

                            //if (globalAnchor != null)
                            //    GameObject.Destroy(globalAnchor.gameObject);

                            //globalAnchor = Session.CreateAnchor(pose);

                            if (virtualMarkersDict.TryGetValue(image.DatabaseIndex, out VirtualMarker virtualMarker))
                            {
                                //centerPosePosition = image.CenterPose.position - virtualMarker.transform.position; // TransformPoint((image.ExtentX/2f * Vector3.right) + (image.ExtentZ/2f * Vector3.forward));

                                //centerPoseRotation = image.CenterPose.rotation * Quaternion.Inverse(virtualMarker.transform.rotation);

                                if (recognizedImageOriginTransform != null)
                                {
                                    if (virtualMarker == null)
                                        Debug.LogWarning("virtualMarker == null");
                                    else
                                    {
                                        recognizedImageOriginTransform.parent = anchor.transform;
                                        recognizedImageOriginTransform.transform.localPosition = -virtualMarker.transform.position;//image.CenterPose.position;
                                        recognizedImageOriginTransform.transform.localRotation = Quaternion.Inverse(virtualMarker.transform.rotation);
                                        //image.CenterPose.rotation;
                                        //cameraTransform.parent = tracker.transform;
                                        //cameraTransform.localPosition = Vector3.zero;
                                        //cameraTransform.localRotation = Quaternion.identity;
                                        //deviceTrackerTransform.SetPositionAndRotation(recognizedImageOriginTransform.InverseTransformPoint(deviceTrackerTransform.position), Quaternion.Inverse(recognizedImageOriginTransform.rotation) * deviceTrackerTransform.rotation);


                                    }
                                }

                            }

                            imageAnchors.Add(image, anchor);

                            //lastTrackedIdx = image.DatabaseIndex;
                            if (virtualMarkersDict.TryGetValue(image.DatabaseIndex, out VirtualMarker virtualSceneMarker))
                            {
                                lastTrackedAnchor = anchor;

                                lastTrackedMarker = virtualSceneMarker;
                            }

                            SetMarkerActive(image.DatabaseIndex, true);

                            //Quaternion localRot = Quaternion.Inverse(anchor.transform.rotation) * deviceTrackerTransform.rotation;

                            //Vector3 localPos = anchor.transform.InverseTransformPoint(deviceTrackerTransform.position);

                            //deviceTrackerTransform.SetPositionAndRotation(localPos, localRot);
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

                    //if (anchor != null)
                    //{
                    //    var localRot = Quaternion.Inverse(tracker.originPose.rotation) * anchor.transform.rotation;

                    //    var rot = localRot.eulerAngles;

                    //    rot.x = virtualSceneMarker.transform.rotation.eulerAngles.x;

                    //    rot.y = virtualSceneMarker.transform.rotation.eulerAngles.y;

                    //    rot.z = virtualSceneMarker.transform.rotation.eulerAngles.z;

                    //    anchor.transform.localRotation = Quaternion.Euler(rot);
                    //}

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
                            if (detectedPlane.PlaneType == DetectedPlaneType.HorizontalUpwardFacing)
                            {
                                eulerAnchorRotation.x = detectedPlane.CenterPose.rotation.eulerAngles.x;

                                eulerAnchorRotation.z = detectedPlane.CenterPose.rotation.eulerAngles.z;

                                Debug.LogWarning("detectedPlane Angle diff: " + (detectedPlane.CenterPose.rotation.eulerAngles - lastTrackedAnchor.transform.rotation.eulerAngles));
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

                            if (usePlanes)
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
                    if (lastTrackedMarker != null)
                    {
                        offsetOrigin.SetPositionAndRotation(
                                            lastTrackedMarker.transform.TransformPoint(lastTrackedAnchor.transform.InverseTransformPoint(deviceTrackerTransform.position)),
                                            lastTrackedMarker.transform.rotation * (Quaternion.Inverse(lastTrackedAnchor.transform.rotation) * deviceTrackerTransform.rotation));
                        
                        //cameraTransform.position = lastTrackedMarker.transform.TransformPoint(localPos);
                        cameraTransform.localPosition = tracker.transform.localPosition;

                        //cameraTransform.rotation = lastTrackedMarker.transform.rotation * localRot;
                        cameraTransform.localRotation = tracker.transform.localRotation;
                    }
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
                        onZeroMarkers.Invoke();
                    }
                }
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

        private void OnGUI()
        {

            GUILayout.Label(Session.Status.ToString());
            //GUILayout.Label("OriginPose = "+tracker.originPose.position.ToString());
            //GUILayout.Label("tpd.transform.position  = "+tracker.transform.parent.position.ToString());

            //if (originPoseTransform != null)
            //{
            //    originPoseTransform.position = tpd.originPose.position;
            //    originPoseTransform.rotation = tpd.originPose.rotation;
            //}

            //GUILayout.Label("FOV " + Camera.main.fieldOfView);

            //GUILayout.Label("markersCount: " + markersCount);

            //GUILayout.Label("lastTrackedIdx: " + lastTrackedIdx);

            //GUILayout.Label("AnchoredImages:" + imageAnchors.Count);

            //GUILayout.Label("virtualMarkersDict:" + virtualMarkersDict.Count);

            //GUILayout.Label("Origin rotation : " + tracker.originPose.rotation.eulerAngles);

            //GUILayout.Label("parent Pos : " + deviceTrackerTransform.position);

            //GUILayout.Label("tracker Pos : " + tracker.transform.position);


            //GUILayout.Label("parent Rot : " + deviceTrackerTransform.rotation.eulerAngles);

            //GUILayout.Label("tracker Rot : " + tracker.transform.rotation.eulerAngles);

            GUILayout.Label("hasAnyTrackedImages " + hasAnyTrackedImages);

            if (lastTrackedMarker != null)
                GUILayout.Label("lastTrackedMarker " + lastTrackedMarker.transform.position + " " + lastTrackedMarker.transform.rotation.eulerAngles);

            if (lastTrackedAnchor != null)
            {
                GUILayout.Label(lastTrackedAnchor.gameObject.name);
                GUILayout.Label("lastTrackedAnchor " + lastTrackedAnchor.transform.position + " " + lastTrackedAnchor.transform.rotation.eulerAngles);
            }
            foreach(var VM in virtualMarkersDict.Values)
            {
                GUILayout.Label(VM.name);
            }

            /*foreach (var anchoredImage in imageAnchors)
            {
                var anchor = anchoredImage.Value;

                GUILayout.Label( anchoredImage.Key.ToString()+" rotation : " + anchor.transform.rotation.eulerAngles);

            }*/
        }
    }

}
