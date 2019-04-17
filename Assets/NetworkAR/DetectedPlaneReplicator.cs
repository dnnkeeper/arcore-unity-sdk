using GoogleARCore;
using GoogleARCore.Examples.Common;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using ZeroFormatter;



//[RequireComponent(typeof(DetectedPlaneVisualizer))]
public class DetectedPlaneReplicator : MonoBehaviour
{
    private DetectedPlane m_DetectedPlane;

    MeshFilter meshFilter;

    DetectedPlaneVisualizer vis;

    /// <summary>
    /// The Unity Awake() method.
    /// </summary>
    public void Awake()
    {
        m_Mesh = GetComponent<MeshFilter>().mesh;
        m_MeshRenderer = GetComponent<UnityEngine.MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        vis = GetComponent<DetectedPlaneVisualizer>();
    }

    
    // Start is called before the first frame update
    void Start()
    {

        Debug.Log("DetectedPlaneReplicator start()");
       
    }

    private bool _AreVerticesListsEqual(List<Vector3> firstList, List<Vector3> secondList)
    {
        if (firstList.Count != secondList.Count)
        {
            return false;
        }

        for (int i = 0; i < firstList.Count; i++)
        {
            if (firstList[i] != secondList[i])
            {
                return false;
            }
        }

        return true;
    }

    private List<Vector3> m_PreviousFrameMeshVertices = new List<Vector3>();
    public List<Vector3> m_MeshVertices = new List<Vector3>();
    private List<Color> m_MeshColors = new List<Color>();

    private List<int> m_MeshIndices = new List<int>();

    private Mesh m_Mesh;

    private MeshRenderer m_MeshRenderer;

    // Update is called once per frame
    void Update()
    {
        if (!NetworkClient.active || NetworkManager.singleton == null || NetworkServer.active)
            return;

        if (vis == null)
            vis = GetComponent<DetectedPlaneVisualizer>();

        m_DetectedPlane = vis.GetDetectedPlane();

        if (m_DetectedPlane == null)
        {
            return;

        }

        m_MeshVertices = new List<Vector3>(meshFilter.mesh.vertices);
        if (_AreVerticesListsEqual(m_PreviousFrameMeshVertices, m_MeshVertices))
        {
            return;
        }
        else {
            
            m_PreviousFrameMeshVertices.Clear();

            m_PreviousFrameMeshVertices.AddRange(m_MeshVertices);
            
            NetworkPlayerAR player = transform.GetComponentInParent<NetworkPlayerAR>();
            if (player != null)
            {
                
                Debug.Log("Send Message with "+ m_DetectedPlane);
                var newMessage = new PlaneInfoMessage { info = new PlaneInfo { hashcode = m_DetectedPlane.GetHashCode(), pos = m_DetectedPlane.CenterPose.position, rot =  (m_DetectedPlane.CenterPose.rotation), vertices = ( m_MeshVertices.ToArray()) } };
                player.CmdSendPlane(newMessage);
                    //m_DetectedPlane.CenterPose.position, m_DetectedPlane.CenterPose.rotation, m_MeshVertices);
            }   
        }
    }
    
    public void CreateMesh(Vector3 planeCenter, Quaternion planeRotation, List<Vector3> newVertices)
    {
        Debug.Log("Create mesh with "+ newVertices.Count+" verts");
        
        Vector3 planeNormal = planeRotation * Vector3.up;
        
        int planePolygonCount = newVertices.Count;

        // The following code converts a polygon to a mesh with two polygons, inner
        // polygon renders with 100% opacity and fade out to outter polygon with opacity 0%, as shown below.
        // The indices shown in the diagram are used in comments below.
        // _______________     0_______________1
        // |             |      |4___________5|
        // |             |      | |         | |
        // |             | =>   | |         | |
        // |             |      | |         | |
        // |             |      |7-----------6|
        // ---------------     3---------------2
        m_MeshColors.Clear();

        // Fill transparent color to vertices 0 to 3.
        for (int i = 0; i < planePolygonCount; ++i)
        {
            m_MeshColors.Add(Color.clear);
        }

        // Feather distance 0.2 meters.
        const float featherLength = 0.2f;

        // Feather scale over the distance between plane center and vertices.
        const float featherScale = 0.2f;

        // Add vertex 4 to 7.
        for (int i = 0; i < planePolygonCount; ++i)
        {
            Vector3 v = newVertices[i];

            // Vector from plane center to current point
            Vector3 d = v - planeCenter;

            float scale = 1.0f - Mathf.Min(featherLength / d.magnitude, featherScale);
            newVertices.Add((scale * d) + planeCenter);

            m_MeshColors.Add(Color.white);
        }

        m_MeshIndices.Clear();
        int firstOuterVertex = 0;
        int firstInnerVertex = planePolygonCount;

        // Generate triangle (4, 5, 6) and (4, 6, 7).
        for (int i = 0; i < planePolygonCount - 2; ++i)
        {
            m_MeshIndices.Add(firstInnerVertex);
            m_MeshIndices.Add(firstInnerVertex + i + 1);
            m_MeshIndices.Add(firstInnerVertex + i + 2);
        }

        // Generate triangle (0, 1, 4), (4, 1, 5), (5, 1, 2), (5, 2, 6), (6, 2, 3), (6, 3, 7)
        // (7, 3, 0), (7, 0, 4)
        for (int i = 0; i < planePolygonCount; ++i)
        {
            int outerVertex1 = firstOuterVertex + i;
            int outerVertex2 = firstOuterVertex + ((i + 1) % planePolygonCount);
            int innerVertex1 = firstInnerVertex + i;
            int innerVertex2 = firstInnerVertex + ((i + 1) % planePolygonCount);

            m_MeshIndices.Add(outerVertex1);
            m_MeshIndices.Add(outerVertex2);
            m_MeshIndices.Add(innerVertex1);

            m_MeshIndices.Add(innerVertex1);
            m_MeshIndices.Add(outerVertex2);
            m_MeshIndices.Add(innerVertex2);
        }

        m_Mesh.Clear();
        m_Mesh.SetVertices(newVertices);
        m_Mesh.SetTriangles(m_MeshIndices, 0);
        m_Mesh.SetColors(m_MeshColors);

        GetComponent<MeshRenderer>().material.SetVector("_PlaneNormal", planeNormal);
    }
}
