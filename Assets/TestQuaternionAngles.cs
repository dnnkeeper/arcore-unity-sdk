using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class TestQuaternionAngles : MonoBehaviour
{
    public Transform target;

    public Vector3 calculatedDifference;

    Quaternion diff;
    // Update is called once per frame
    void Update()
    {
        if (target != null)
        {
            diff = Quaternion.Inverse(transform.rotation) * target.rotation;
            calculatedDifference = diff.eulerAngles;
        }
    }

    [ContextMenu("Compensate")]
    public void Compensate()
    {

        //Quaternion fromTargetToUP = Quaternion.Inverse(target.rotation);
        //Quaternion fromMyToUP = Quaternion.Inverse(transform.rotation);

        //Quaternion upDiff = Quaternion.Inverse(fromMyToUP) * fromTargetToUP;


        //Vector3 eulerAngles = transform.rotation.eulerAngles;
        //Vector3 targetEulerAngles = target.rotation.eulerAngles;
        //transform.rotation = Quaternion.Euler(Vector3.ProjectOnPlane(eulerAngles, transform.up));//  * Quaternion.Euler( Vector3.ProjectOnPlane(targetEulerAngles, Vector3.up) );
        //transform.rotation * upDiff;
        //Quaternion.Euler(targetEulerAngles.x, eulerAngles.y, targetEulerAngles.z);
        //transform.rotation * Quaternion.Euler( Vector3.ProjectOnPlane(calculatedDifference, Vector3.up) );

        Quaternion rotatedTargetOrigin = Quaternion.LookRotation(Vector3.Cross(Vector3.up, Vector3.ProjectOnPlane(target.right, Vector3.up) ), Vector3.up);
        Quaternion fromOriginToMarker = Quaternion.Inverse(rotatedTargetOrigin) * target.rotation;

        Quaternion rotatedOrigin = Quaternion.LookRotation(Vector3.Cross(Vector3.up, transform.right), Vector3.up);
        Quaternion fromOriginToAnchor = Quaternion.Inverse(rotatedOrigin) * transform.rotation;

        transform.rotation = rotatedOrigin * fromOriginToMarker;

        //Quaternion diff = Quaternion.Inverse(transform.rotation) * target.rotation;
        //Vector3 calculatedDifference = diff.eulerAngles;
        //transform.rotation = transform.rotation * Quaternion.Euler(Vector3.ProjectOnPlane(calculatedDifference, Vector3.up)) ;
    }
}
