using System;

using BobyStar.DualLaser;

using UdonSharp;

using UnityEngine;
using UnityEngine.Serialization;

using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common;

using Mesh = UnityEngine.Mesh;


namespace Tumble.Scripts.Level {
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class LevelEditorTool : TumbleBehaviour {
        private const int MaxBlockSize = 3;

        public DataList selection;

        public LevelEditorToolMode mode = LevelEditorToolMode.Place;

        public LevelEditor editor;
        public float       maxPlaceDistance = 20f;
        public int         elementId        = 0;

        public float LaserLength => mode == LevelEditorToolMode.Place ? maxPlaceDistance : 64f;

        public GameObject laserPointerGhost;
        public GameObject laserPointer;
        public GameObject cursor;
        public GameObject cursorGhost;
        public Transform  gizmoHolder;

        public Material outlinePrepareMaterial;

        public int paintColor;

        public Material selectOutlineMaterial;
        public Material destroyOutlineMaterial;
        public Material placeOutlineMaterial;

        public bool debugBlockPermutations;
        public bool generatePermutations;

        private DualLaser           _laser;
        private TumbleLevelLoader64 _levelLoader;

        private Vector3Int _dragStart;
        private Vector3Int _dragEnd;
        private Plane      _dragPlane;

        private GameObject  _placeElement;
        private Mesh        _placeMesh;
        private Matrix4x4[] _placeMatrices = new Matrix4x4[256];
        private int         _placeMatrixCount;

        private RaycastHit[] _hits = new RaycastHit[20];

        // Inputs
        private bool _useDown;
        private bool _useHold;
        private bool _useUp;
        private bool _grabDown;
        private bool _grabHold;
        private bool _grabUp;

        private bool _enabled;

        private void Start() {
            _laser       = Universe.dualLaser;
            _levelLoader = Universe.levelLoader;
            SetElement(0);
            SetMode(0);

            var x = 0;

            if (debugBlockPermutations) {
                for (var i = 0; i < _blockPermutationIndices.Length; i++) {
                    var size  = new Vector3Int(i / 9, (i / 3) % 3, i % 3);
                    var index = _blockPermutationIndices[i];
                    var rot   = _blockPermutationRotations[i];
                    var obj   = GameObject.Instantiate(_levelLoader.levelElements[index], new Vector3(x, 0, 0), rot);
                    obj.transform.SetParent(transform);
                    obj.name =  $"{size.x}x{size.y}x{size.z}";
                    x        += size.x + 3;
                }
            }
        }

        private void Update() {
            if (Universe.BlockInputs) return;

            if (debugBlockPermutations) {
                var x = 0;

                for (var i = 0; i < _blockPermutationIndices.Length; i++) {
                    var size = new Vector3Int(i / 9, (i / 3) % 3, i % 3);
                    size += Vector3Int.one;
                    DrawDebugBox(new Vector3(x, 0, 0) - (Vector3.one * 0.5f), size);
                    x += size.x + 2;
                }
            }

            if (generatePermutations) {
                generatePermutations = false;

                var s = "";

                for (var i = 0; i < transform.childCount; i++) {
                    var c = transform.GetChild(i);
                    s += $"Quaternion.Euler({c.localEulerAngles.x}, {c.localEulerAngles.y}, {c.localEulerAngles.z}), // {c.name}\n";
                }

                Debug.Log(s);
            }
        }

        private void DrawDebugBox(Vector3 position, Vector3Int size) {
            var v000 = position;
            var v100 = position + new Vector3(size.x, 0,      0);
            var v101 = position + new Vector3(size.x, 0,      size.z);
            var v001 = position + new Vector3(0,      0,      size.z);
            var v010 = position + new Vector3(0,      size.y, 0);
            var v110 = position + new Vector3(size.x, size.y, 0);
            var v111 = position + new Vector3(size.x, size.y, size.z);
            var v011 = position + new Vector3(0,      size.y, size.z);

            Debug.DrawLine(v000, v100, Color.red);
            Debug.DrawLine(v100, v101, Color.red);
            Debug.DrawLine(v101, v001, Color.red);
            Debug.DrawLine(v001, v000, Color.red);
            Debug.DrawLine(v000, v010, Color.red);
            Debug.DrawLine(v100, v110, Color.red);
            Debug.DrawLine(v101, v111, Color.red);
            Debug.DrawLine(v001, v011, Color.red);
            Debug.DrawLine(v010, v110, Color.red);
            Debug.DrawLine(v110, v111, Color.red);
            Debug.DrawLine(v111, v011, Color.red);
            Debug.DrawLine(v011, v010, Color.red);
        }

        private bool  _showLaser;
        private float _laserLength;

        public override void PostLateUpdate() {
            laserPointer.SetActive(false);
            laserPointerGhost.SetActive(false);
            cursor.SetActive(false);
            cursorGhost.SetActive(false);

            if (!_enabled) return;

            _showLaser = Networking.LocalPlayer.IsUserInVR();
            var ray = _laser.GetPointerRay(HandType.RIGHT, false);
            var hit = GetRayElement(LevelEditorToolMode.Select, ray, out var point, out var normal, out var elementId, out var cancel);

            _showLaser &= !cancel;
            laserPointer.SetActive(_showLaser);
            laserPointerGhost.SetActive(_showLaser);
            cursor.SetActive(_showLaser);
            cursorGhost.SetActive(_showLaser);

            if (_showLaser) {
                laserPointer.transform.position = ray.origin;
                laserPointer.transform.rotation = Quaternion.LookRotation(ray.direction);
                var dist = Vector3.Distance(ray.origin, point);
                laserPointer.transform.localScale      = new Vector3(1f, 1f, dist);
                laserPointerGhost.transform.localScale = new Vector3(1f, 1f, maxPlaceDistance);
            }
        }

        private void LateUpdate() {
            if (Input.GetKeyDown(KeyCode.Tab))
                SetMode(
                    (int)(mode == LevelEditorToolMode.Play ? LevelEditorToolMode.Place : LevelEditorToolMode.Play)
                );

            for (var i = 0; i < gizmoHolder.childCount; i++) gizmoHolder.GetChild(i).gameObject.SetActive(false);

            var newEnabled = editor.level != null
                && !Universe.BlockInputs
                && mode != LevelEditorToolMode.Play;

            if (newEnabled != _enabled) {
                _enabled = newEnabled;
                Universe.flyMovement.SetFlyActive(_enabled);
            }

            if (!_enabled
                || editor.level == null
                || Input.GetKey(KeyCode.Tab) // This unlocks the cursor in desktop mode so they are likely trying to use UI.
               ) {
                UpdateInputs(); // Stops phantom clicks when entering edit mode
                return;
            }

            // R-Click for grab
            _grabDown |= Input.GetMouseButtonDown(1);
            _grabUp   |= Input.GetMouseButtonUp(1);

            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(1);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(2);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(3);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetMode(4);

            var scroll = Input.GetAxis("Mouse ScrollWheel");
            maxPlaceDistance = Mathf.Clamp(maxPlaceDistance + scroll * 5f, 1, 100);

            switch (mode) {
                case LevelEditorToolMode.Select:
                    SelectMode();
                    break;
                case LevelEditorToolMode.Place:
                    PlaceMode();
                    break;
                case LevelEditorToolMode.Break:
                    BreakMode();
                    break;
                case LevelEditorToolMode.Paint:
                    PaintMode();
                    break;
            }

            UpdateInputs();
        }

        private LevelEditorGizmo _currentGizmo;

        private bool SelectMode() {
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);

            var element = GetRayElement(LevelEditorToolMode.Select,
                rightRay,
                out var point,
                out var normal,
                out var hitElementId,
                out var cancel,
                hitTriggers: true);

            if (cancel) return false;

            // Remove null selection elements
            for (var i = selection.Count - 1; i >= 0; i--)
                if (selection[i].Reference == null) selection.RemoveAt(i);
            
            if (selection.Count == 1) DoGizmo(rightRay, point, (GameObject)selection[0].Reference); // Do this before getting gizmo because it wont be active yet otherwise

            if (_grabHold || Input.GetMouseButton(1)) {
                if (_grabDown) StartGrab();

                DoGrab(rightRay);

                if (_grabUp) EndGrab();
            }
            else {
                if (_useDown) {
                    _currentGizmo = GetGizmo(rightRay, out var gizmoHitPoint);

                    if (_currentGizmo != null) _currentGizmo.OnClickGizmo(rightRay, gizmoHitPoint);
                    else
                        if (element != null) {
                            var data      = new DataToken(element);
                            var contained = selection.Contains(data);

                            if (!contained)
                                selection.Add(data);
                            else
                                selection.Remove(data);
                        }
                }

                if (_currentGizmo != null && _useHold) _currentGizmo.OnDragGizmo(rightRay);

                if (_currentGizmo != null && _useUp) _currentGizmo.OnReleaseGizmo(rightRay);
            }

            DrawOutline(selectOutlineMaterial);
            return true;
        }

        private LevelEditorGizmo GetGizmo(Ray ray, out Vector3 point) {
            var hits = Physics.RaycastAll(ray, LaserLength);

            foreach (var hit in hits) {
                if (hit.collider == null) continue;

                var gizmo = hit.collider.GetComponentInParent<LevelEditorGizmo>();

                if (gizmo != null) {
                    point = hit.point;
                    return gizmo;
                }
            }

            point = Vector3.zero;
            return null;
        }

        private void DoGizmo(Ray ray, Vector3 hitPoint, GameObject element) {
            var id = TumbleLevel.GetElementId(element);
            for (var i = 0; i < gizmoHolder.childCount; i++) {
                var gizmoTransform = gizmoHolder.GetChild(i);
                var gizmoComponent = gizmoTransform.GetComponent<LevelEditorGizmo>();
                if (!gizmoComponent.IsWhitelisted(element, id)) continue;
                if(!gizmoComponent.ShouldShowGizmo(this)) continue; 

                gizmoTransform.gameObject.SetActive(true);
                if (gizmoComponent != null) gizmoComponent.UpdateGizmo(ray, hitPoint, editor.level.GetCell(hitPoint), element);
            }
        }

        private Vector3    _grabOffset;
        private GameObject _grabElement;
        private float      _grabDistance;

        private void StartGrab() {
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);

            var hitElement = GetRayElement(LevelEditorToolMode.Grab,
                rightRay,
                out var hitPoint,
                out var normal,
                out var hitElementId,
                out var cancel,
                hitTriggers: true);

            _grabElement = hitElement;
            if (_grabElement == null) return;

            _grabOffset   = _grabElement.transform.position - hitPoint;
            _grabDistance = Vector3.Distance(rightRay.origin, hitPoint);
        }

        private void DoGrab(Ray ray) {
            if (_grabElement == null) return;

            // Duplication when pressing action button while holding grab
            if (_useDown) {
                foreach (var entry in selection.ToArray()) {
                    var element   = (GameObject)entry.Reference;
                    var elementId = TumbleLevel.GetElementId(element);
                    editor.AddElement(elementId, element.transform.localPosition, TumbleLevel.GetElementState(element));
                }

                if (!selection.Contains(_grabElement)) {
                    var elementId = TumbleLevel.GetElementId(_grabElement);
                    editor.AddElement(elementId, _grabElement.transform.localPosition, TumbleLevel.GetElementState(_grabElement));
                }
            }

            var oldCell     = TumbleLevel.GetLocalCell(_grabElement.transform.localPosition);
            var newPosition = ray.GetPoint(_grabDistance) + _grabOffset;
            var newCell     = editor.level.GetCell(newPosition);
            if (newCell == oldCell) return;

            var offset = newCell - oldCell;

            foreach (var entry in selection.ToArray()) {
                var element = (GameObject)entry.Reference;
                editor.MoveElement(element, element.transform.localPosition + offset);
            }

            if (!selection.Contains(_grabElement)) editor.MoveElement(_grabElement, newCell);
        }

        private void EndGrab() { _grabElement = null; }

#region Place Mode
        private bool PlaceMode() {
            var rightRay   = _laser.GetPointerRay(HandType.RIGHT, false);
            var hitElement = GetRayElement(LevelEditorToolMode.Place, rightRay, out var hitPoint, out var normal, out var hitElementId, out var cancel);
            if (cancel) return false;

            if (_grabHold || Input.GetMouseButton(1)) {
                if (_grabDown) StartGrab();

                DoGrab(rightRay);

                if (_grabUp) EndGrab();
                return true;
            }

            hitPoint += normal * 0.1f;
            var levelCell = editor.level.GetCell(hitPoint);

            var elementToPlace = _levelLoader.levelElements[elementId];

            if(elementToPlace == null) return false;
            DoGizmo(rightRay, hitPoint, elementToPlace);
            
            if (elementId < 30) return BlockPlaceMode(levelCell);

            if (_useUp) {
                var elements = editor.level.GetElementsInCell(levelCell);

                var passed = true;

                foreach (var elementData in elements.ToArray()) {
                    var element = ((GameObject)elementData.Reference).GetComponent<LevelElement>();
                    if (element == null) continue;

                    passed &= element.TryOverlapElement(rightRay, hitPoint);
                }

                if (passed) {
                    var newElementObject = editor.AddElement(elementId, levelCell, 0);

                    if (newElementObject != null) {
                        var newElement = newElementObject.GetComponent<LevelElement>();
                        if (newElement != null) newElement.OnElementPlaced(rightRay, hitPoint);
                    }
                }
            }

            var rootElement = _levelLoader.levelElements[elementId];
            var meshes      = rootElement.GetComponentsInChildren<MeshFilter>();

            var matrices = new Matrix4x4[meshes.Length];

            for (var i = 0; i < meshes.Length; i++) {
                var m = meshes[i];
                // Matrix relative to rootElement
                var localPos = m.transform.position - rootElement.transform.position;
                var localRot = m.transform.rotation * Quaternion.Inverse(rootElement.transform.rotation);
                var localScl = m.transform.lossyScale;
                matrices[i] = Matrix4x4.TRS(localPos + editor.level.GetWorldPosition(levelCell), localRot, localScl);
            }

            for (var index = 0; index < meshes.Length; index++) {
                var m = meshes[index].sharedMesh;
                if (m == null) continue;

                for (var submesh = 0; submesh < m.subMeshCount; submesh++)
                    VRCGraphics.DrawMeshInstanced(meshes[index].sharedMesh, submesh, outlinePrepareMaterial, matrices, matrices.Length);
            }

            for (var index = 0; index < meshes.Length; index++) {
                var m = meshes[index].sharedMesh;

                for (var submesh = 0; submesh < m.subMeshCount; submesh++)
                    VRCGraphics.DrawMeshInstanced(meshes[index].sharedMesh, submesh, selectOutlineMaterial, matrices, matrices.Length);
            }

            return true;
        }

        private bool BlockPlaceMode(Vector3Int levelCell) {
            var clampedDragEnd = new Vector3Int(
                Mathf.Clamp(_dragEnd.x, _dragStart.x - MaxBlockSize + 1, _dragStart.x + MaxBlockSize - 1),
                Mathf.Clamp(_dragEnd.y, _dragStart.y - MaxBlockSize + 1, _dragStart.y + MaxBlockSize - 1),
                Mathf.Clamp(_dragEnd.z, _dragStart.z - MaxBlockSize + 1, _dragStart.z + MaxBlockSize - 1)
            );

            var start = new Vector3Int(Mathf.Min(_dragStart.x, clampedDragEnd.x),
                Mathf.Min(_dragStart.y,                        clampedDragEnd.y),
                Mathf.Min(_dragStart.z,                        clampedDragEnd.z));

            var end = new Vector3Int(Mathf.Max(_dragStart.x, clampedDragEnd.x),
                Mathf.Max(_dragStart.y,                      clampedDragEnd.y),
                Mathf.Max(_dragStart.z,                      clampedDragEnd.z));

            var size  = end - start;
            var perm  = size.x * 9 + size.y * 3 + size.z;
            var index = _blockPermutationIndices[perm];
            var rot   = _blockPermutationRotations[perm];

            if (_useUp) editor.AddElement(index + paintColor * 10, start, TumbleLevelLoader64.EncodeRotation(rot));

            if (_useDown || !_useHold) {
                _dragStart = levelCell;
                _dragEnd   = levelCell;
            }

            if (_useHold)
                if (levelCell != _dragEnd)
                    _dragEnd = levelCell;

            var mesh = _levelLoader.levelElements[index].GetComponentInChildren<MeshFilter>().sharedMesh;

            var matrix = new Matrix4x4[] {
                Matrix4x4.TRS(start + editor.level.transform.position, rot, Vector3.one),
            };

            VRCGraphics.DrawMeshInstanced(mesh, 0, outlinePrepareMaterial, matrix, 1);
            VRCGraphics.DrawMeshInstanced(mesh, 0, placeOutlineMaterial,   matrix, 1);

            return true;
        }

        private int[] _blockPermutationIndices = new int[] {
            0, // 1x1x1
            1, // 1x1x2
            2, // 1x1x3
            1, // 1x2x1
            3, // 1x2x2
            4, // 1x2x3
            2, // 1x3x1
            4, // 1x3x2
            5, // 1x3x3
            1, // 2x1x1
            3, // 2x1x2
            4, // 2x1x3
            3, // 2x2x1
            6, // 2x2x2
            7, // 2x2x3
            4, // 2x3x1
            7, // 2x3x2
            8, // 2x3x3
            2, // 3x1x1
            4, // 3x1x2
            5, // 3x1x3
            4, // 3x2x1
            7, // 3x2x2
            8, // 3x2x3
            5, // 3x3x1
            8, // 3x3x2
            9, // 3x3x3
        };

        private Quaternion[] _blockPermutationRotations = new Quaternion[] {
            Quaternion.Euler(0,   0,   0),   // 0x0x0
            Quaternion.Euler(0,   0,   0),   // 0x0x1
            Quaternion.Euler(0,   0,   0),   // 0x0x2
            Quaternion.Euler(270, 0,   0),   // 0x1x0
            Quaternion.Euler(0,   0,   0),   // 0x1x1
            Quaternion.Euler(0,   0,   0),   // 0x1x2
            Quaternion.Euler(270, 0,   0),   // 0x2x0
            Quaternion.Euler(270, 180, 0),   // 0x2x1
            Quaternion.Euler(0,   0,   0),   // 0x2x2
            Quaternion.Euler(0,   90,  0),   // 1x0x0
            Quaternion.Euler(0,   0,   270), // 1x0x1
            Quaternion.Euler(0,   0,   270), // 1x0x2
            Quaternion.Euler(0,   90,  0),   // 1x1x0
            Quaternion.Euler(0,   0,   0),   // 1x1x1
            Quaternion.Euler(0,   0,   0),   // 1x1x2
            Quaternion.Euler(270, 270, 0),   // 1x2x0
            Quaternion.Euler(270, 270, 0),   // 1x2x1
            Quaternion.Euler(0,   0,   0),   // 1x2x2
            Quaternion.Euler(0,   90,  0),   // 2x0x0
            Quaternion.Euler(0,   90,  90),  // 2x0x1
            Quaternion.Euler(0,   0,   270), // 2x0x2
            Quaternion.Euler(0,   90,  0),   // 2x1x0
            Quaternion.Euler(0,   90,  90),  // 2x1x1
            Quaternion.Euler(0,   90,  90),  // 2x1x2
            Quaternion.Euler(0,   90,  0),   // 2x2x0
            Quaternion.Euler(270, 270, 0),   // 2x2x1
            Quaternion.Euler(0,   0,   0),   // 2x2x2
        };
#endregion

        private bool BreakMode() {
            selection.Clear();
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);

            var element = GetRayElement(LevelEditorToolMode.Break,
                rightRay,
                out var point,
                out var normal,
                out var elementId,
                out var cancel,
                hitTriggers: true);

            if (cancel) return false;

            if (element != null) selection.Add(new DataToken(element));

            DrawOutline(destroyOutlineMaterial);

            if (_useDown && element != null) editor.RemoveElement(element);
            return true;
        }

        private bool PaintMode() {
            selection.Clear();
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);
            var element  = GetRayElement(LevelEditorToolMode.Paint, rightRay, out var point, out var normal, out var elementId, out var cancel);
            if (cancel) return false;

            if (element != null) selection.Add(new DataToken(element));
            DrawOutline(selectOutlineMaterial);

            if (_useHold && element != null) {
                if (elementId < 30) // In block range
                {
                    var shape = elementId % 10;
                    var color = elementId / 10;
                    if (paintColor == color) return true;

                    var newElementId = paintColor * 10 + shape;
                    var position     = element.transform.localPosition;
                    var rotation     = element.transform.localRotation;

                    editor.RemoveElement(element);
                    editor.AddElement(newElementId, position, TumbleLevelLoader64.EncodeRotation(rotation));
                }
            }

            return true;
        }

        private void DrawOutline(Material material) {
            var meshes = new DataDictionary();

            foreach (var entry in selection.ToArray()) {
                var element = (GameObject)entry.Reference;
                if (element == null) continue;

                var mesh = element.GetComponentInChildren<MeshFilter>();
                if (mesh == null || mesh.mesh == null) continue;

                var meshToken = new DataToken(mesh.mesh);
                if (!meshes.ContainsKey(meshToken)) meshes.Add(meshToken, new DataList());
                meshes[meshToken].DataList.Add(new DataToken(mesh.transform));
            }

            foreach (var meshToken in meshes.GetKeys().ToArray()) {
                var values   = meshes[meshToken].DataList;
                var matrices = new Matrix4x4[values.Count];

                for (var i = 0; i < values.Count; i++) {
                    var element = (Transform)values[i].Reference;
                    matrices[i] = element.localToWorldMatrix;
                }

                VRCGraphics.DrawMeshInstanced((Mesh)meshToken.Reference, 0, outlinePrepareMaterial, matrices, matrices.Length);
                VRCGraphics.DrawMeshInstanced((Mesh)meshToken.Reference, 0, material,               matrices, matrices.Length);
            }
        }

        public override void InputUse(bool value, UdonInputEventArgs args) {
            _useHold = value;
            _useDown = value;
            _useUp   = !value;
        }

        public override void InputGrab(bool value, UdonInputEventArgs args) {
            if (!Networking.LocalPlayer.IsUserInVR()) return; // This is to avoid immediate duplication

            _grabHold = value;
            _grabDown = value;
            _grabUp   = !value;
        }

        private void UpdateInputs() {
            _useDown  = false;
            _useUp    = false;
            _grabDown = false;
            _grabUp   = false;
        }

        private GameObject GetRayElement(
            LevelEditorToolMode mode, Ray ray, out Vector3 point, out Vector3 normal, out int elementId, out bool cancel, bool hitTriggers = false
        ) {
            normal    = -ray.direction;
            elementId = -1;
            cancel    = false;

            var hitCount = Physics.RaycastNonAlloc(ray, _hits, LaserLength);

            if (hitCount > 0) {
                var        passed         = false;
                var        position       = Vector3.zero;
                var        nearest        = float.MaxValue;
                GameObject nearestElement = null;

                for (var i = 0; i < Mathf.Min(hitCount, _hits.Length); i++) {
                    var h = _hits[i];
                    if (h.collider == null) continue;

                    if (h.collider.GetComponentInParent<Canvas>() != null) {
                        point     = h.point;
                        cancel    = true;
                        elementId = -1;
                        return null;
                    }

                    if (h.collider.isTrigger && !hitTriggers) continue;

                    if (nearest < h.distance) continue;

                    if (editor.level.TryGetHitElement(h, out var element, out var eid)) {
                        if (mode == LevelEditorToolMode.Place) {
                            var elementComponent = element.GetComponent<LevelElement>();
                            if (elementComponent != null && elementComponent.PlaceModeIgnoresColliders) continue;
                        }

                        passed         = true;
                        normal         = h.normal;
                        position       = h.point;
                        nearest        = h.distance;
                        nearestElement = element;
                        elementId      = eid;
                    }
                }

                if (passed) {
                    point = position;
                    return nearestElement;
                }
            }

            point = ray.origin + ray.direction * LaserLength;
            return null;
        }

        private void SetElement(int elementId) {
            this.elementId = elementId;
            _placeElement  = _levelLoader.GetElementPrefab(elementId);
            _placeMesh     = _placeElement.GetComponentInChildren<MeshFilter>().mesh;
        }

        public void SetMode(int mode) {
            this.mode = (LevelEditorToolMode)mode;
            selection.Clear();
            Universe.BroadcastCustomEvent("EventLevelEditorToolModeChanged");
        }
    }

    public enum LevelEditorToolMode {
        Play,
        Select,
        Place,
        Break,
        Paint,
        Grab,
    }
}