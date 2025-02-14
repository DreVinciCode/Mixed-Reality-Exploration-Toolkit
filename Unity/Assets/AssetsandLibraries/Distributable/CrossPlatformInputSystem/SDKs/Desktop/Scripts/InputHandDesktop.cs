﻿// Copyright © 2018-2021 United States Government as represented by the Administrator
// of the National Aeronautics and Space Administration. All Rights Reserved.

using GSFC.ARVR.MRET.Infrastructure.CrossPlatformInputSystem.SDK.Base;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GSFC.ARVR.MRET.Infrastructure.CrossPlatformInputSystem.SDK.Desktop
{
    /// <remarks>
    /// History:
    /// 27 October 2020: Created
    /// 17 March 2021: Updated the navigation axis to return just the local X/Y without rotation
    ///     to make the result consistent with the other hand implementations and to defer orientation
    ///     to the navigation controller. Additionally, the desktop rig reference is no longer needed
    ///     since the motion constraints are now contained within the rig SDK. Lastly, the shift key press
    ///     is now exposed as a navigation press event so that the locomotion manager can attach an event
    ///     handler to use the event to trigger motion constraint changes. (J. Hosler)
    /// 17 August 2021: Added pointer functions.
    /// </remarks>
    /// <summary>
    /// Desktop wrapper for the input hand.
    /// Author: Dylan Z. Baker
    /// </summary>
    public class InputHandDesktop : InputHandSDK
    {
        /// <summary>
        /// Reference to the desktop input head.
        /// </summary>
        [Tooltip("Reference to desktop input head.")]
        public InputHeadDesktop inputHeadDesktop;

        /// <summary>
        /// Whether or not the hand is dynamic.
        /// </summary>
        [Tooltip("Whether or not the hand is dynamic.")]
        public bool dynamic;

        /// <summary>
        /// Used to poll navigating.
        /// </summary>
        public override bool navigating
        {
            get
            {
                return _navigateValue != Vector2.zero;
            }
        }

        /// <summary>
        /// Used to poll navigate pressing.
        /// </summary>
        public override bool navigatePressing
        {
            get
            {
                return _navigatePressing;
            }
        }
        private bool _navigatePressing = false;

        /// <summary>
        /// Used to poll navigate value.
        /// </summary>
        public override Vector2 navigateValue
        {
            get
            {
                return _navigateValue;
            }
        }
        private Vector2 _navigateValue = Vector2.zero;

        /// <summary>
        /// Whether or not the pointer is currently on.
        /// </summary>
        public override bool pointerOn
        {
            get
            {
                if (uiPointerController != null)
                {
                    if (uiPointerController.raycastLaser != null)
                    {
                        if (uiPointerController.raycastLaser.active)
                        {
                            return true;
                        }
                    }
                }

                if (teleportController != null)
                {
                    if (teleportController.raycastLaser != null)
                    {
                        if (teleportController.raycastLaser.active)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        /// <summary>
        /// The current endpoint of the pointer.
        /// </summary>
        public override Vector3 pointerEnd
        {
            get
            {
                if (uiPointerController != null)
                {
                    if (uiPointerController.raycastLaser != null)
                    {
                        if (uiPointerController.raycastLaser.active)
                        {
                            return uiPointerController.raycastLaser.hitPos;
                        }
                    }
                }

                if (teleportController != null)
                {
                    if (teleportController.raycastLaser != null)
                    {
                        if (teleportController.raycastLaser.active)
                        {
                            return teleportController.raycastLaser.hitPos;
                        }
                    }
                }

                return Vector3.zero;
            }
        }

        /// <summary>
        /// Handler for menu press event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void MenuPressEvent(InputAction.CallbackContext callbackContext)
        {
            inputHand.MenuPressed(transform);
        }

        /// <summary>
        /// Handler for shift press event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void ShiftPressEvent(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.performed)
            {
                _navigatePressing = true;
                inputHand.NavigatePressBegin();
            }
            else
            {
                inputHand.NavigatePressComplete();
                _navigatePressing = false;
            }
        }

        /// <summary>
        /// Handler for rotation lock press event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void RotationLockPressEvent(InputAction.CallbackContext callbackContext)
        {
            inputHeadDesktop.rotationLocked = !callbackContext.performed;
        }

        /// <summary>
        /// Handler for primary press event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void PrimaryPressEvent(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.phase == InputActionPhase.Started)
            {
                inputHand.SelectBegin();
            }
            else if (callbackContext.phase == InputActionPhase.Canceled)
            {
                inputHand.SelectComplete();
            }
        }

        /// <summary>
        /// Handler for secondary press event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void SecondaryPressEvent(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.phase == InputActionPhase.Started)
            {
                inputHand.GrabBegin();
            }
            else if (callbackContext.phase == InputActionPhase.Canceled)
            {
                inputHand.GrabComplete();
            }
        }

        /// <summary>
        /// Handler for tertiary press event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void TertiaryPressEvent(InputAction.CallbackContext callbackContext)
        {
            if (callbackContext.phase == InputActionPhase.Started)
            {
                inputHand.NavigatePressBegin();
            }
            else if (callbackContext.phase == InputActionPhase.Canceled)
            {
                inputHand.NavigatePressComplete();
            }
        }

        /// <summary>
        /// Handler for mouse position event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void MousePositionEvent(InputAction.CallbackContext callbackContext)
        {
            // If rotation isn't locked, calculate pitch and yaw and set controlled to that.
            Vector2 look = callbackContext.ReadValue<Vector2>();
            if (look != null)
            {
                Camera cam = inputHand.inputRig.activeCamera;

                // Try raycast.
                RaycastHit controllerLoc;
                if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out controllerLoc, Mathf.Infinity))
                {
                    // If successful, set icon to active and position it.
                    if (inputHand.activeHandModel)
                    {
                        inputHand.activeHandModel.SetActive(true);
                    }
                    transform.position = Vector3.Lerp(cam.transform.position, controllerLoc.point, 0.9f);
                    transform.rotation = cam.transform.rotation;
                }
                else
                {
                    // If not, set icon to inactive.
                    if (inputHand.activeHandModel)
                    {
                        inputHand.activeHandModel.SetActive(false);
                    }
                }

                /*Camera cam = inputHand.inputRig.activeCamera;
                Debug.Log(cam.ScreenToWorldPoint(new Vector3(look.x, look.y, cam.nearClipPlane + 1)));
                if (cam != null)
                {
                    transform.position = cam.ScreenToWorldPoint(new Vector3(look.x, look.y, cam.nearClipPlane + 1));
                }*/
            }
        }

        /// <summary>
        /// The amount to move on the next update.
        /// </summary>
        private Vector3 amountToMove = Vector3.zero;

        /// <summary>
        /// Handler for move event.
        /// </summary>
        /// <param name="callbackContext">InputSystem callback context.</param>
        public void MoveAction(InputAction.CallbackContext callbackContext)
        {
            // Set the amount to move on a 2d plane.
            Vector2 move = callbackContext.ReadValue<Vector2>();
            if (move != null)
            {
                amountToMove = new Vector3(move.x, 0, move.y);
                if (amountToMove == Vector3.zero)
                {
                    inputHand.NavigateComplete();
                }
                else
                {
                    inputHand.NavigateBegin();
                }
            }
        }

        private void Update()
        {
            _navigateValue = new Vector2(amountToMove.x, amountToMove.z);
        }

#region Locomotion

#region Locomotion [Teleport]

        public override void EnableTeleportation()
        {
            
        }

        public override void DisableTeleportation()
        {
            
        }

        public override void ToggleTeleportOn()
        {
            
        }

        public override void ToggleTeleportOff()
        {
            
        }

        public override void CompleteTeleport()
        {
            
        }

        public override void BlockTeleport()
        {
            
        }

        public override void UnblockTeleport()
        {
            
        }

#endregion // Locomotion [Teleport]

#region Locomotion [Armswing]

        /// <seealso cref="InputHandSDK.EnableArmswing"/>
        public override void EnableArmswing()
        {
            
        }

        /// <seealso cref="InputHandSDK.DisableArmswing"/>
        public override void DisableArmswing()
        {
            
        }

#endregion // Locomotion [Armswing]

#region Locomotion [Fly]

        /// <seealso cref="InputHandSDK.EnableFly"/>
        public override void EnableFly()
        {
            
        }

        /// <seealso cref="InputHandSDK.DisableFly"/>
        public override void DisableFly()
        {
            
        }

        #endregion // Locomotion [Fly]

#region Locomotion [Navigate]

        /// <seealso cref="InputHandSDK.EnableNavigate"/>
        public override void EnableNavigate()
        {
            // Notify the locomotion controller that we are enabled
            if (_navigationController != null) _navigationController.SetHandActiveState(this.inputHand, true);
        }

        /// <seealso cref="InputHandSDK.DisableNavigate"/>
        public override void DisableNavigate()
        {
            // Notify the locomotion controller that we are disabled
            if (_navigationController != null) _navigationController.SetHandActiveState(this.inputHand, false);
        }

#endregion // Locomotion [Navigate]

#endregion // Locomotion

#region UI Handling

        public override void ToggleUIPointerOn(bool soft, bool showWhenInvalid)
        {
            if (uiPointerController == null)
            {
                return;
            }

            uiPointerController.EnterMode();
            uiPointerController.ToggleUIPointingOn(soft);
            uiPointerController.showInvalid = showWhenInvalid;
        }

        public override void ToggleUIPointerOff(bool soft)
        {
            if (uiPointerController == null)
            {
                return;
            }

            uiPointerController.ToggleUIPointingOff(soft);
            uiPointerController.ExitMode();
        }

        public override void UIPointerSelect()
        {
            if (uiPointerController == null)
            {
                return;
            }

            uiPointerController.Select();
        }

#endregion

    }
}