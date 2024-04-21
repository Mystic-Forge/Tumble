
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.SDK3.Data;
using VRC.Udon;
using VRC.Udon.Common;

#if UNITY_EDITOR && !COMPILER_UDONSHARP
using UnityEditor; // For custom inspector
#endif

namespace BobyStar.DualLaser
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DualLaser : UdonSharpBehaviour
    {
        [SerializeField] private Collider[] onlyHitObjects;
        [SerializeField] private LineRenderer laserLine;

        #region Public Variables
        /// <summary>
        /// The hand that was last used to interact with UI. 
        /// Activates when the Trigger or Grip is pressed or when the opposite hand presses the VRChat Menu button. 
        /// The active hand usually has the main laser pointer.
        /// </summary>
        public HandType ActiveHand { get => _isLeftHandActive ? HandType.LEFT : HandType.RIGHT; }
        
        /// <summary>
        /// True if the active hand is the left hand, False for the right hand. 
        /// The active hand usually has the main laser pointer.
        /// </summary>
        public bool IsLeftHandActive { get => _isLeftHandActive; }
        
        /// <summary>
        /// True if the active hand swapped to another hand on this frame.
        /// </summary>
        public bool ActiveHandSwapped { get => _activeHandSwapped; }
        #endregion

        #region Setup Parameters (Measured with PCVR + Quest 2 and Adjusted to Avatar Scale)
        #region PCVR
#if !UNITY_ANDROID
        private readonly Vector3 handPosOffset = new Vector3(0.0185f, 0, .0506f);
        private readonly Vector3 handRotOffset = Vector3.up * 40;
        private readonly float hitOffset = .001f;
        private readonly float zoom = 27f;
        private readonly float laserWidth = .00175f;
        private readonly float minimumSize = .004f;
        private readonly float lerpLocationSpeed = 30;
        private readonly float slerpRotationSpeed = 20;
        private readonly float normalizedPlayerVelocityMultiplier = -.005f;
        private readonly float fadeDistance = .05f;
        private readonly float inputGripThreshold = .075f;
#endif
        #endregion

        #region Quest
#if UNITY_ANDROID
        private readonly Vector3 handPosOffset = new Vector3(.0079f, 0, .02125f);
        private readonly Vector3 handRotOffset = Vector3.up * 45;
        private readonly float hitOffset = .001f;
        private readonly float zoom = 29f;
        private readonly float laserWidth = .00175f;
        private readonly float minimumSize = .00675f;
        private readonly float lerpLocationSpeed = 30;
        private readonly float slerpRotationSpeed = 20;
        private readonly float normalizedPlayerVelocityMultiplier = -.001f;
        private readonly float fadeDistance = .05f;
        private readonly float inputGripThreshold = .3825f;
#endif
        #endregion
        [SerializeField] private LayerMask hitLayers = ~((1 << 5) | (1 << 10) | (1 << 12) | (1 << 18));
        public float menuTime = .25f;
        #endregion

        #region Player Properties
        private VRCPlayerApi localPlayer;
        private float playerScaleMultiplier = 1;
        public bool _isLeftHandActive = false;
        private bool _activeHandSwapped;
        private bool inputLeftTriggerPressed;
        private bool inputRightTriggerPressed;
        private float inputLeftMenuTime;
        private float inputRightMenuTime;
        private bool inputLeftGrip;
        private bool inputRightGrip;
        #endregion

        #region Ray Properties
        private Ray pointerRay = new Ray(Vector3.zero, Vector3.forward);

        private Ray pointerRightRay = new Ray(Vector3.zero, Vector3.forward);
        private Ray pointerRightRaySmooth = new Ray(Vector3.zero, Vector3.forward);
        private Ray pointerRightRayLocal = new Ray(Vector3.zero, Vector3.forward);

        private Ray pointerLeftRay = new Ray(Vector3.zero, Vector3.forward);
        private Ray pointerLeftRaySmooth = new Ray(Vector3.zero, Vector3.forward);
        private Ray pointerLeftRayLocal = new Ray(Vector3.zero, Vector3.forward);

        private RaycastHit pointerHit;
        private bool       _isPointerOn;

        private bool isPointerOn {
            get => false; set => _isPointerOn = value; 
        }
        #endregion

        private DataDictionary hitCache;

        #region Public Methods
        /// <summary>
        /// Gets the ray for the currently active hand. The active hand usually has the main laser pointer.
        /// </summary>
        /// <param name="isSmoothed">True returns smoothed data for the ray. The VRChat pointer is smoothed.</param>
        /// <returns>A ray originating at the tracked active hand position and pointing in the direction of the active hand. Akin to the VRChat laser pointer for UI.</returns>
        public Ray GetPointerRay(bool isSmoothed = true)
        {
            return GetPointerRay(ActiveHand, isSmoothed);
        }

        /// <summary>
        /// Gets the ray for the selected hand. Use the other overload for defaulting to the active hand.
        /// </summary>
        /// <param name="handType">The hand wher the ray originates and moves from.</param>
        /// <param name="isSmoothed">True returns smoothed data for the ray. The VRChat pointer is smoothed.</param>
        /// <returns>A ray originating at the tracked chosen hand position and pointing in the direction of the chosen hand. Akin to the VRChat laser pointer for UI.</returns>
        public Ray GetPointerRay(HandType handType, bool isSmoothed = true)
        {
            #region Void
            if (!Utilities.IsValid(localPlayer)) return new Ray();
            if (!localPlayer.IsUserInVR())
            {
                VRCPlayerApi.TrackingData headTrack = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
                return new Ray(headTrack.position, headTrack.rotation * Vector3.forward);
            }
            #endregion

            if (isSmoothed) return handType == HandType.RIGHT ? pointerRightRaySmooth : pointerLeftRaySmooth;
            else return handType == HandType.RIGHT ? pointerRightRay : pointerLeftRay;
        }

        /// <summary>
        /// Refreshes the Pointer data for GetPointerRay().
        /// Only use this if you need to forcibly update the pointer, like after moving the local player.
        /// </summary>
        public void ForceRefreshPointerData() => RefreshPointer();
        #endregion

        void Start()
        {
            #region Validate and Get Local Player
            localPlayer = Networking.LocalPlayer;
            if (!Utilities.IsValid(localPlayer)) gameObject.SetActive(false);
            if (!localPlayer.IsUserInVR()) gameObject.SetActive(false);
            #endregion

            hitCache = new DataDictionary();

            #region Check and Clean Whitelist
            if (Utilities.IsValid(onlyHitObjects))
            {
                #region Check if Needs Clean Up
                int blankEntries = 0;
                for (int oho = 0; oho < onlyHitObjects.Length; oho++)
                {
                    if (onlyHitObjects[oho] == null) blankEntries++;
                }
                #endregion

                #region Clean Up
                if (blankEntries == onlyHitObjects.Length) onlyHitObjects = null;
                else if (blankEntries > 0)
                {
                    Collider[] newRTArray = new Collider[onlyHitObjects.Length - blankEntries];

                    blankEntries = 0;
                    for (int oho2 = 0; oho2 < onlyHitObjects.Length; oho2++)
                    {
                        if (onlyHitObjects[oho2] == null) continue;

                        newRTArray[blankEntries] = onlyHitObjects[oho2];
                        blankEntries++;
                    }

                    onlyHitObjects = (Collider[])newRTArray.Clone();
                }
                #endregion
            }
            #endregion

            CheckPointer();
        }

        #region Setup Pointer Ray Data for Raycasting
        private void RefreshPointer()
        {
            #region Vaidate
            if (!Utilities.IsValid(localPlayer)) return;
            if (!localPlayer.IsUserInVR()) return;
            #endregion

            #region Setup Transform for Local Interpolation
            VRCPlayerApi.TrackingData trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Origin);
            transform.SetPositionAndRotation(trackingData.position, trackingData.rotation);
            transform.localScale = Vector3.one * playerScaleMultiplier;
            #endregion

            #region Get Right Hand Pointer
            trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.RightHand);

            #region Get New Ray
            pointerRightRay = new Ray(
                trackingData.position + trackingData.rotation * handPosOffset * playerScaleMultiplier,
                trackingData.rotation * Quaternion.Euler(handRotOffset) * Vector3.forward);
            #endregion

            #region Transform to Local Tracking Space
            Ray localRay = new Ray(
                transform.InverseTransformPoint(pointerRightRay.origin + normalizedPlayerVelocityMultiplier * playerScaleMultiplier * localPlayer.GetVelocity().normalized),
                transform.InverseTransformDirection(pointerRightRay.direction));
            #endregion

            #region Interpolate Locally in Tracking Space
            pointerRightRaySmooth = new Ray(
                Vector3.Lerp(pointerRightRayLocal.origin, localRay.origin, Time.deltaTime * lerpLocationSpeed),
                Vector3.Slerp(pointerRightRayLocal.direction, localRay.direction, Time.deltaTime * slerpRotationSpeed));
            #endregion

            pointerRightRayLocal = pointerRightRaySmooth;

            pointerRightRaySmooth = new Ray(transform.TransformPoint(pointerRightRaySmooth.origin), transform.TransformDirection(pointerRightRaySmooth.direction));
            #endregion

            #region Get Left Hand Pointer
            trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.LeftHand);

            #region Get New Ray
            pointerLeftRay = new Ray(
                trackingData.position + trackingData.rotation * handPosOffset * playerScaleMultiplier,
                trackingData.rotation * Quaternion.Euler(handRotOffset) * Vector3.forward);
            #endregion

            #region Transform to Local Tracking Space
            localRay = new Ray(
                transform.InverseTransformPoint(pointerLeftRay.origin + normalizedPlayerVelocityMultiplier * playerScaleMultiplier * localPlayer.GetVelocity().normalized),
                transform.InverseTransformDirection(pointerLeftRay.direction));
            #endregion

            #region Interpolate Locally in Tracking Space
            pointerLeftRaySmooth = new Ray(
                Vector3.Lerp(pointerLeftRayLocal.origin, localRay.origin, Time.deltaTime * lerpLocationSpeed),
                Vector3.Slerp(pointerLeftRayLocal.direction, localRay.direction, Time.deltaTime * slerpRotationSpeed));
            #endregion

            pointerLeftRayLocal = pointerLeftRaySmooth;

            pointerLeftRaySmooth = new Ray(transform.TransformPoint(pointerLeftRaySmooth.origin), transform.TransformDirection(pointerLeftRaySmooth.direction));
            #endregion

            pointerRay = IsLeftHandActive ? pointerRightRaySmooth : pointerLeftRaySmooth;
        }
        #endregion

        #region Raycasts and checks if there is clickable UI
        /// <summary>
        /// INTERNAL USE ONLY! Main loop of this script.
        /// </summary>
        public void CheckPointer()
        {
            #region Validate
            if (!Utilities.IsValid(localPlayer)) return;
            if (!localPlayer.IsUserInVR()) return;
            #endregion

            SendCustomEventDelayedFrames(nameof(CheckPointer), 0, VRC.Udon.Common.Enums.EventTiming.LateUpdate);

            RefreshInputs();
            RefreshPointer();

            if (Physics.Raycast(pointerRay, out pointerHit, Mathf.Infinity, hitLayers, QueryTriggerInteraction.Collide))
            {
                Transform target = pointerHit.transform;

                #region Target Valid
                if (Utilities.IsValid(target))
                {
                    #region Whitelist control
                    bool isInWhitelist = true;
                    if (onlyHitObjects != null)
                    {
                        isInWhitelist = false;
                        for (int i = 0; i < onlyHitObjects.Length; i++)
                        {
                            if (onlyHitObjects[i].transform == target)
                            {
                                isInWhitelist = true;
                                break;
                            }
                        }
                    }
                    #endregion

                    if (isInWhitelist)
                    {
                        DataToken targetToken = target;

                        #region Is In Cache
                        if (hitCache.TryGetValue(targetToken, out DataToken result))
                            isPointerOn = result.Boolean;
                        #endregion
                        #region Find and Add to cache
                        else
                        {
                            #region Find VRC UI Shape
                            int tries = 0;
                            bool success = false;
                            while (tries < 100)
                            {
                                tries++;

                                #region Transform not Valid
                                if (!Utilities.IsValid(target))
                                { success = false; break; }
                                #endregion

                                #region Has VRC UI Shape
                                if (Utilities.IsValid(target.GetComponent(typeof(VRC.SDKBase.VRC_UiShape))))
                                {
                                    #region Has RectTransform
                                    if (Utilities.IsValid(target.GetComponent<RectTransform>()))
                                    { success = true; break; }
                                    #endregion
                                }
                                #endregion
                                #region Has Parent Transform
                                else if (Utilities.IsValid(target.parent))
                                { target = target.parent; }
                                #endregion
                                #region Top of Hierarchy, failed to find valid RectTransform with VRC UI Shape
                                else
                                { success = false; break; }
                                #endregion
                            }
                            #endregion

                            if (tries < 100)
                            {
                                hitCache.SetValue(targetToken, success);

                                // Limit Size of Cache?
                            }
                            isPointerOn = success;
                        }
                        #endregion
                    }
                }
                #endregion
                else isPointerOn = false;
            }
            else isPointerOn = false;

            DisplayPointer();
        }
        #endregion

        #region Show/Hide the Pointer Object and Ray and Set the positions of the laser
        private void DisplayPointer()
        {
            #region Validate
            if (!Utilities.IsValid(localPlayer)) return;
            if (!localPlayer.IsUserInVR()) return;
            #endregion

            #region Show/Hide Pointer
            gameObject.SetActive(isPointerOn);
            if (!isPointerOn) return;
            #endregion

            #region Move to Ray Hit and Point along Normal
            transform.SetPositionAndRotation(pointerHit.point + pointerHit.normal * hitOffset, Quaternion.LookRotation(pointerHit.normal));
            #endregion

            #region Keep constant size in screenspace (approx 60 FOV)
            VRCPlayerApi.TrackingData trackingData = localPlayer.GetTrackingData(VRCPlayerApi.TrackingDataType.Head);
            transform.localScale = Vector3.one *
                Mathf.Max(Mathf.Tan(60 * Mathf.Deg2Rad * .5f) * Vector3.Distance(trackingData.position, transform.position) / zoom, minimumSize);
            #endregion

            #region Set Positions and Size for Laser
            if (Utilities.IsValid(laserLine))
            {
                Vector3 laserStart = pointerRay.origin;
                Vector3 laserDirection = pointerHit.point - pointerRay.origin;

                laserLine.positionCount = 4;

                #region Set Positions
                laserLine.SetPosition(0, laserStart); // Start
                #region Fade Points
                #region When long enough, fade points are offsets from start and end
                if (laserDirection.magnitude > fadeDistance * 2)
                {
                    laserLine.SetPosition(1, laserStart + laserDirection.normalized * fadeDistance);
                    laserLine.SetPosition(2, laserStart + laserDirection - laserDirection.normalized * fadeDistance);
                }
                #endregion
                #region Fade takes 50% if not long enough for offsets
                else
                {
                    laserLine.SetPosition(1, laserStart + laserDirection * .5f);
                    laserLine.SetPosition(2, laserStart + laserDirection * .5f);
                }
                #endregion
                #endregion
                laserLine.SetPosition(3, laserStart + laserDirection); // End

                #region Transform to World Space if the Line Renderer is not using World Space
                if (!laserLine.useWorldSpace)
                {
                    laserLine.SetPosition(0, transform.InverseTransformPoint(laserLine.GetPosition(0)));
                    laserLine.SetPosition(1, transform.InverseTransformPoint(laserLine.GetPosition(1)));
                    laserLine.SetPosition(2, transform.InverseTransformPoint(laserLine.GetPosition(2)));
                    laserLine.SetPosition(3, transform.InverseTransformPoint(laserLine.GetPosition(3)));
                }
                #endregion
                #endregion

                laserLine.widthMultiplier = laserWidth * playerScaleMultiplier;
            }
            #endregion
        }
        #endregion

        #region Inputs
        #region Manage Inputs
        private void RefreshInputs()
        {
            inputLeftTriggerPressed = false;
            inputRightTriggerPressed = false;

            if (!InputManager.IsUsingHandController()) return;
            _activeHandSwapped = false;

            bool leftActive = _isLeftHandActive;
            #region Detect Grips and Menu Buttons (PC Oculus Native Will be Wrong ¯\_(ツ)_/¯)
            #region Grips (Activates Hand That Was Pressed)
            if (!inputRightGrip && Input.GetAxis("Oculus_CrossPlatform_SecondaryHandTrigger") > inputGripThreshold) leftActive = false;
            if (!inputLeftGrip && Input.GetAxis("Oculus_CrossPlatform_PrimaryHandTrigger") > inputGripThreshold) leftActive = true;

            inputRightGrip = Input.GetAxis("Oculus_CrossPlatform_SecondaryHandTrigger") > inputGripThreshold;
            inputLeftGrip = Input.GetAxis("Oculus_CrossPlatform_PrimaryHandTrigger") > inputGripThreshold;
            #endregion

            #region Menu Buttons (Button Activates Opposite Hand)
#if !UNITY_ANDROID
            string leftMenuButtonName = "Oculus_CrossPlatform_Button4";
            string rightMenuButtonName = "Oculus_CrossPlatform_Button2";
#endif
#if UNITY_ANDROID
            string leftMenuButtonName = "Jump"; // The best button name for opening a Menu :)
            string rightMenuButtonName = "Fire2";
#endif
            if (Input.GetButtonUp(rightMenuButtonName))
            {
                if (inputLeftMenuTime < 0) inputLeftMenuTime = 0;

                if (Time.timeSinceLevelLoad - inputRightMenuTime <= menuTime) leftActive = true;
                else if (inputRightMenuTime > 0) inputRightMenuTime = -1;
            }
            if (Input.GetButtonUp(leftMenuButtonName))
            {
                if (inputRightMenuTime < 0) inputRightMenuTime = 0;

                if (Time.timeSinceLevelLoad - inputLeftMenuTime <= menuTime) leftActive = false;
                else if (inputLeftMenuTime > 0) inputLeftMenuTime = -1;
            }

            if (Input.GetButtonDown(rightMenuButtonName))
            {
                if (inputRightMenuTime < 0) inputRightMenuTime = 0;
                else inputRightMenuTime = Time.timeSinceLevelLoad;
            }
            if (Input.GetButtonDown(leftMenuButtonName))
            {
                if (inputLeftMenuTime < 0) inputLeftMenuTime = 0;
                else inputLeftMenuTime = Time.timeSinceLevelLoad;
            }
            #endregion
            #endregion

            #region Detect if Changed
            if (_isLeftHandActive != leftActive) _activeHandSwapped = true;
            #endregion
            _isLeftHandActive = leftActive;
        }
        #endregion

        #region VRChat Trigger Input
        public override void InputUse(bool value, UdonInputEventArgs args)
        {
            if (value)
            {
                #region Track the Triggers for the current Frame
                if (args.handType == HandType.LEFT) inputLeftTriggerPressed = true;
                else if (args.handType == HandType.RIGHT) inputRightTriggerPressed = true;
                #endregion

                #region Default to Left Hand if both triggers were pressed on the same frame
                if (inputLeftTriggerPressed && inputRightTriggerPressed)
                    _isLeftHandActive = _isLeftHandActive || (args.handType == HandType.LEFT);
                #endregion

                #region Active Hand Swapped
                _activeHandSwapped = _isLeftHandActive != (args.handType == HandType.LEFT);
                #endregion

                if (!inputLeftTriggerPressed || !inputRightTriggerPressed) _isLeftHandActive = args.handType == HandType.LEFT;
            }
        }
        #endregion
        #endregion

        #region VRChat Avatar Height Change
        public override void OnAvatarEyeHeightChanged(VRCPlayerApi player, float prevEyeHeightAsMeters)
        {
            if (player.isLocal) playerScaleMultiplier = player.GetAvatarEyeHeightAsMeters();
        }
        #endregion
    }

    #region Custom Inspector
#if UNITY_EDITOR && !COMPILER_UDONSHARP
    [CustomEditor(typeof(DualLaser))]
    public class DualLaser_Editor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("Allows players to have 2 lasers for UI! Made by BobyStar!", MessageType.None);

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("onlyHitObjects"), 
                new GUIContent("Only Hit Objects (Optional Whitelist)",
                "Only allow the second laser to hit these Colliders (Whitelist).\n" +
                "Useful if you only want a specific canvas to show both lasers.\n\n" +
                "Leave blank to allow any Colliders to be hit."), 
                true, null);

            EditorGUILayout.PropertyField(serializedObject.FindProperty("laserLine"), 
                new GUIContent("Laser (Optional)", "The Line Renderer to use for the Laser.\n" +
                "For the fade effect:\n" +
                "Use 4 Alpha Keys at 0%, 0.1%, 99.9%, & 100%\n" +
                "With the alpha values 0, 255, 255, & 0\n" +
                "Use the Prefab as reference!"), 
                true, null);

            serializedObject.ApplyModifiedProperties();
        }

        [UnityEditor.Callbacks.PostProcessScene(-1)]
        public static void PrePostProcess()
        {
            VRC_SceneDescriptor sceneDescriptor = FindObjectOfType<VRC_SceneDescriptor>();

            if (sceneDescriptor == null) return;

            foreach (DualLaser laser2 in FindObjectsOfType<DualLaser>())
            {
                SerializedObject laser2Obj = new SerializedObject(laser2);

                laser2Obj.Update();
                laser2Obj.FindProperty("hitLayers").intValue &= ~sceneDescriptor.interactThruLayers;

                laser2Obj.ApplyModifiedProperties();
            }
        }
    }
#endif
#endregion
}