using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using PawnSystem.Network;
using ZeroFormatter;
using System;
using System.IO;

public struct PlaneInfo
{
    public Vector3 pos;
    public Quaternion rot;
    public Vector3[] vertices;
}

public static class UShortProxy
{
    public static void Serialize(Stream bytes, ushort instance)
    {
        byte[] bytes2 = BitConverter.GetBytes(instance);
        bytes.Write(bytes2, 0, bytes2.Length);
    }

    public static ushort Deserialize(Stream bytes)
    {
        byte[] array = new byte[2];
        bytes.Read(array, 0, 2);
        return BitConverter.ToUInt16(array, 0);
    }
}


public static class ArrayProxy<T>
{
    public static void Serialize(Stream bytes, T[] instance, Action<Stream, T> serialization)
    {
        UShortProxy.Serialize(bytes, (ushort)instance.Length);
        foreach (T arg in instance)
        {
            serialization(bytes, arg);
        }
    }

    public static T[] Deserialize(Stream bytes, ArrayProxy<T>.Deserializer<T> serialization)
    {
        ushort num = UShortProxy.Deserialize(bytes);
        T[] array = new T[(int)num];
        for (int i = 0; i < (int)num; i++)
        {
            array[i] = serialization(bytes);
        }
        return array;
    }

    public delegate void Serializer<U>(Stream stream, U instance);

    public delegate U Deserializer<U>(Stream stream);
}

public static class Vector3Proxy
{
    public static void Serialize(Stream bytes, Vector3 instance)
    {
        bytes.Write(BitConverter.GetBytes(instance.x), 0, 4);
        bytes.Write(BitConverter.GetBytes(instance.y), 0, 4);
        bytes.Write(BitConverter.GetBytes(instance.z), 0, 4);
    }

    public static Vector3 Deserialize(Stream bytes)
    {
        byte[] array = new byte[12];
        bytes.Read(array, 0, 12);
        return new Vector3(BitConverter.ToSingle(array, 0), BitConverter.ToSingle(array, 4),
            BitConverter.ToSingle(array, 8));
    }
}


public class PlaneInfoMessage : MessageBase
{
    public PlaneInfo info;

    public override void Serialize(NetworkWriter writer)
    {
        writer.Write(info.pos);
        writer.Write(info.rot);

        using (MemoryStream memoryStream = new MemoryStream())
        {
            ArrayProxy<Vector3>.Serialize(
                       memoryStream,
                       info.vertices,
                       Vector3Proxy.Serialize);

            //Here is the result
            byte[] conversionArray = memoryStream.ToArray();

            writer.WriteBytesFull(conversionArray);
        }
    }

    public override void Deserialize(NetworkReader reader)
    {
        info.pos = reader.ReadVector3();//ZeroFormatterSerializer.Deserialize<Vector3>(reader.ReadBytesAndSize());
        info.rot = reader.ReadQuaternion();//ZeroFormatterSerializer.Deserialize<Quaternion>(reader.ReadBytesAndSize());
        byte[] conversionArray = reader.ReadBytesAndSize();//ZeroFormatterSerializer.Deserialize<List<Vector3>>(reader.ReadBytesAndSize());

        using (MemoryStream memoryStream = new MemoryStream(conversionArray))
        {
            //Here is the result
            info.vertices = ArrayProxy<Vector3>.Deserialize(memoryStream, Vector3Proxy.Deserialize);
        }
        
    }



    //public PlaneStruct(Vector3 p, Quaternion r, List<Vector3> v)
    //{
    //    pos = p;
    //    rot = r;
    //    vertices = v;
    //}

    
}

public class NetworkPlayerAR : NetworkPlayerReplicator
{
    public Transform offsetTransform;
    public Transform cameraTransform;

    [SyncVar]
    Vector3 camPos;

    [SyncVar]
    Quaternion camRot;

    [SyncVar]
    Vector3 offsetPos;

    [SyncVar]
    Quaternion offsetRot;


    public override void OnLocalPlayerNetUpdate()
    {
        CmdSendOffsetState(offsetTransform.position, offsetTransform.rotation);
        CmdSendCameraState(cameraTransform.position, cameraTransform.rotation);
        
    }

    [Command]
    void CmdSendCameraState(Vector3 pos, Quaternion rot)
    {
        cameraTransform.position = pos;
        cameraTransform.rotation = rot;
        camPos = pos;
        camRot = rot;
    }

    [Command]
    void CmdSendOffsetState(Vector3 pos, Quaternion rot)
    {
        offsetTransform.position = pos;
        offsetTransform.rotation = rot;
        offsetPos = pos;
        offsetRot = rot;
    }

    protected override void UpdateRemotePlayer()
    {
        base.UpdateRemotePlayer();
    
        cameraTransform.position = camPos;
        cameraTransform.rotation = camRot;
        offsetTransform.position = offsetPos;
        offsetTransform.rotation = offsetRot;
    }

    public Material planeMaterial;

    Dictionary<Vector3, DetectedPlaneReplicator> replicatedPlanes = new Dictionary<Vector3, DetectedPlaneReplicator>();

    [Command]
    public void CmdSendPlane(PlaneInfoMessage planeInfo)
        //Vector3 pos, Quaternion rot, List<Vector3> newVerts)
    {
        if (replicatedPlanes.TryGetValue( (planeInfo.info.pos), out DetectedPlaneReplicator plane))
        {
            Debug.Log("Updating existing plane");
            //plane.CreateMesh(pos, rot, newVerts);
        }
        else
        {
            Debug.Log("Creating new plane with "+ planeInfo.info.vertices.Length);
            var newGO = new GameObject("NewPlane", typeof(MeshFilter), typeof(DetectedPlaneReplicator), typeof(MeshRenderer));
            newGO.transform.parent = offsetTransform;
            plane = newGO.GetComponent<DetectedPlaneReplicator>();
            var meshRend = newGO.GetComponent<MeshRenderer>();
            meshRend.material = planeMaterial;
        }
        plane.CreateMesh((planeInfo.info.pos),  (planeInfo.info.rot),  new List<Vector3>(planeInfo.info.vertices));
    }

}
