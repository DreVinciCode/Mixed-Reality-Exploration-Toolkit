﻿// Copyright © 2018-2021 United States Government as represented by the Administrator
// of the National Aeronautics and Space Administration. All Rights Reserved.

using UnityEngine;
using GSFC.ARVR.MRET.XRC;
using GSFC.ARVR.MRET.Infrastructure.CrossPlatformInputSystem;

public class SynchronizedPointer : MonoBehaviour
{
    public SynchronizationManager synchronizationManager;
    public SynchronizedController synchronizedController;
    public InputHand hand;
    public LineRenderer lineRenderer;
    public Object entityLock = new Object();
    public int throttleFactor = 10;
    public float positionResolution = 0.01f;
    public Transform transformToUse;
    public System.Guid uuid;

    private CollaborationManager collaborationManager;
    private Vector3 lastRecordedPosition;
    private int positionThrottleCounter = 0;

    void Start()
    {
        if (synchronizationManager == null)
        {
            synchronizationManager = FindObjectOfType<SynchronizationManager>();
        }

        collaborationManager = FindObjectOfType<CollaborationManager>();

        //lastRecordedPosition = pointerRenderer.GetPointerObjects()[1].transform.position;
    }

    void Update()
    {
        positionThrottleCounter++;

        if (synchronizedController.synchronizedUser.isControlled)
        {
            // Send Transform (position) changes.
            lock (entityLock)
            {
                Vector3 newPos = hand.pointerEnd;
                //Vector3 newPos = pointerRenderer.GetPointerObjects()[1].transform.localPosition;
                if (!hand.pointerOn)
                {
                    newPos = Vector3.zero;
                }

                if (newPos != lastRecordedPosition && positionThrottleCounter >= throttleFactor)
                {
                    if ((newPos - lastRecordedPosition).magnitude > positionResolution)
                    {
                        lastRecordedPosition = newPos;
                        if (collaborationManager.engineType == CollaborationManager.EngineType.XRC)
                        {
#if !HOLOLENS_BUILD
                            XRCUnity.UpdateEntityPosition(synchronizedController.synchronizedUser.userAlias,
                                synchronizedController.controllerSide == SynchronizedController.ControllerSide.Left ?
                                XRCManager.LPOINTERCATEGORY : XRCManager.RPOINTERCATEGORY
                                , lastRecordedPosition, uuid.ToString(),
                                synchronizedController.synchronizedUser.uuid.ToString(), GSFC.ARVR.XRC.UnitType.meter);
#endif
                        }
                        else
                        {
                            synchronizationManager.SendPositionChange(gameObject);
                        }
                    }
                    positionThrottleCounter = 0;
                }
                transform.hasChanged = false;
            }
        }
    }

    public void SetPosition(Vector3 endpoint)
    {
        if (!synchronizedController.synchronizedUser.isControlled)
        {
            lock (entityLock)
            {
                Vector3[] positions = new Vector3[2];
                if (endpoint == Vector3.zero)
                {
                    positions[0] = positions[1] = Vector3.zero;
                }
                else
                {
                    positions[0] = Vector3.zero;
                    positions[1] = synchronizedController.transform.InverseTransformPoint(endpoint);
                }
                lineRenderer.SetPositions(positions);
            }
        }
    }
}