﻿/* 
 * This file is part of the Virtual Data Explorer distribution (https://coda.ee/vde).
 * Copyright (c) 2020 Kaur Kullman.
 */
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Assets.VDE.UI.Input
{
    internal class CameraObserver : Observer
    {
        private List<InputDevice>
            eyes = new List<InputDevice>(),
            cameras = new List<InputDevice>();
        Log log;
        internal Camera usableCamera;
        internal Data data;

        private void Awake()
        {
            log = new Log("CameraObserver");
        }
        private void Start()
        {
            log = new Log("CameraObserver", data.messenger);
        }
        void OnEnable()
        {            
            List<InputDevice> foundDevices = new List<InputDevice>();

            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.EyeTracking, foundDevices);
            foreach (InputDevice device in foundDevices)
                InputDevices_deviceConnected(device);

            InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.HeadMounted, foundDevices);
            foreach (InputDevice device in foundDevices)
                InputDevices_deviceConnected(device);

            InputDevices.deviceConnected += InputDevices_deviceConnected;
            InputDevices.deviceDisconnected += InputDevices_deviceDisconnected;
        }

        private void InputDevices_deviceConnected(InputDevice device)
        {
            if (device != null)
            {
                log.Entry("incoming camera: " + device.name, Log.Event.ToServer);
                List<InputFeatureUsage> dafu = new List<InputFeatureUsage> { };
                device.TryGetFeatureUsages(dafu);
                foreach (InputFeatureUsage item in dafu)
                {
                    if (item.name == "CenterEyeRotation")
                    {
                        cameras.Add(device);
                    }
                }
                if (device.TryGetFeatureValue(CommonUsages.eyesData, out _))
                {
                    eyes.Add(device);
                }
                if (device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out _))
                {
                    cameras.Add(device);
                }
            }
        }

        private void InputDevices_deviceDisconnected(InputDevice device)
        {
            if (cameras.Contains(device))
                cameras.Remove(device);
            if (eyes.Contains(device))
                eyes.Remove(device);
        }
        private void Update()
        {
            foreach (Shape item in data.UI.shapesInFocus.Values)
            {
                Vector3 paul = usableCamera.WorldToViewportPoint(item.gameObject.transform.position);
                if (
                    -1.2    < paul.x &&
                     1.2    > paul.x &&
                    -1.3    < paul.y &&
                     1.3    > paul.y &&
                     0.0    < paul.z &&
                     1.5    > paul.z // minimum distance to render the expensive material 
                    )
                {
                    item.BePresentable();
                }
                else
                {
                    item.Relax();
                }
            }
            foreach (InputDevice input in eyes)
            {
                if (input.TryGetFeatureValue(CommonUsages.eyesData, out Eyes measure))
                {
                    if (measure.TryGetFixationPoint(out Vector3 gazePoint))
                    {
                        inputEvent.Invoke(new Assets.VDE.UI.Input.Event
                        {
                            function = Assets.VDE.UI.Input.Event.Function.GazePoint,
                            type = Assets.VDE.UI.Input.Event.Type.Vector3,
                            Vector3 = gazePoint
                        });
                        if(CastSphere(gazePoint, out RaycastHit hit, gazeMask))
                        {
                            inputEvent.Invoke(new Assets.VDE.UI.Input.Event
                            {
                                function = Assets.VDE.UI.Input.Event.Function.GazingAt,
                                type = Assets.VDE.UI.Input.Event.Type.GameObject,
                                GameObject = hit.transform.gameObject
                            });
                        }
                    }
                    else if (measure.TryGetRightEyeRotation(out Quaternion rot) && measure.TryGetRightEyePosition(out Vector3 pos))
                    {
                        if (CastRay(rot, pos, out RaycastHit hit, gazeMask))
                        {
                            inputEvent.Invoke(new Assets.VDE.UI.Input.Event
                            {
                                function = Assets.VDE.UI.Input.Event.Function.GazeDirection,
                                type = Assets.VDE.UI.Input.Event.Type.Quaternion,
                                Quaternion = rot
                            });
                            inputEvent.Invoke(new Assets.VDE.UI.Input.Event
                            {
                                function = Assets.VDE.UI.Input.Event.Function.GazingAt,
                                type = Assets.VDE.UI.Input.Event.Type.GameObject,
                                GameObject = hit.transform.gameObject
                            });
                        }
                    }
                }
            }
            
            if (!(usableCamera is null) && usableCamera != null && !(usableCamera.transform is null))
            {
                if (CastRay(usableCamera.transform.rotation, usableCamera.transform.position, out RaycastHit hit, gazeMask))
                {
                    inputEvent.Invoke(new Assets.VDE.UI.Input.Event
                    {
                        function = Assets.VDE.UI.Input.Event.Function.GazeDirection,
                        type = Assets.VDE.UI.Input.Event.Type.Quaternion,
                        Quaternion = usableCamera.transform.rotation
                    });
                    Entity gazeTarget = null;
                    if (hit.transform.gameObject.TryGetComponent(out Container entityContainer))
                    {
                        gazeTarget = entityContainer.entity;
                    }
                    else if (hit.transform.gameObject.TryGetComponent(out Shape entityShape))
                    {
                        gazeTarget = entityShape.entity;
                    }
                    if (!(gazeTarget is null))
                    {
                        inputEvent.Invoke(new Assets.VDE.UI.Input.Event
                        {
                            function = Assets.VDE.UI.Input.Event.Function.GazingAt,
                            type = Assets.VDE.UI.Input.Event.Type.GameObject,
                            GameObject = hit.transform.gameObject,
                            Entity = gazeTarget
                        });
                    }
                }
            }
        }
    }
}
