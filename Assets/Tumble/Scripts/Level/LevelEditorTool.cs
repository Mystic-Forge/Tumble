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
    public class LevelEditorTool : UdonSharpBehaviour {
        public DataList selection;

        public LevelEditorToolMode mode = LevelEditorToolMode.Place;

        public LevelEditor editor;
        public float       maxPlaceDistance = 20f;
        public int         elementId        = 0;

        public Material outlinePrepareMaterial;
        public Material selectOutlineMaterial;
        public Material destroyOutlineMaterial;
        public Material placeOutlineMaterial;

        private Universe            _universe;
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

        private bool _enabled;

        private void Start() {
            _universe    = GetComponentInParent<Universe>();
            _laser       = _universe.dualLaser;
            _levelLoader = _universe.levelLoader;
            SetElement(0);
            SetMode(1);
        }

        private void LateUpdate() {
            var newEnabled = editor != null && editor.level != null;
            if (newEnabled != _enabled) {
                _enabled = newEnabled;
                _universe.dualLaser.forcePointerOn = _enabled;
            }
            if(editor.level == null) return;
            
            if (Input.GetKeyDown(KeyCode.Alpha1)) SetMode(0);
            if (Input.GetKeyDown(KeyCode.Alpha2)) SetMode(1);
            if (Input.GetKeyDown(KeyCode.Alpha3)) SetMode(2);
            if (Input.GetKeyDown(KeyCode.Alpha4)) SetMode(3);
            
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
                case LevelEditorToolMode.Paint: break;
            }

            UpdateInputs();
        }

        private void SelectMode() {
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);
            var element  = GetRayElement(rightRay, out var point, out var normal);
            if (element != null) selection.Add(new DataToken(element));

            DrawOutline(selectOutlineMaterial);
        }

        private void PlaceMode() {
            var rightRay   = _laser.GetPointerRay(HandType.RIGHT, false);
            var hitElement = GetRayElement(rightRay, out var position, out var normal);
            position += normal * 0.1f;
            var levelCell = editor.level.GetCell(position);

            var updateMatrices = false;

            if (_useUp) {
                var start = new Vector3Int(Mathf.Min(_dragStart.x, _dragEnd.x), Mathf.Min(_dragStart.y, _dragEnd.y), Mathf.Min(_dragStart.z, _dragEnd.z));
                var end   = new Vector3Int(Mathf.Max(_dragStart.x, _dragEnd.x), Mathf.Max(_dragStart.y, _dragEnd.y), Mathf.Max(_dragStart.z, _dragEnd.z));

                for (var x = start.x; x <= end.x; x++)
                for (var y = start.y; y <= end.y; y++)
                for (var z = start.z; z <= end.z; z++) {
                    var cell          = new Vector3Int(x, y, z);
                    editor.AddElement(elementId, cell, Quaternion.identity);
                }
            }
            
            if (_useDown || !_useHold) {
                _dragStart = levelCell;
                _dragEnd       = levelCell;
                updateMatrices = true;
            }

            if (_useHold) {
                if (levelCell != _dragEnd) {
                    _dragEnd       = levelCell;
                    updateMatrices = true;
                }
            }

            if (updateMatrices) {
                _placeMatrixCount = 0;
                var start = new Vector3Int(Mathf.Min(_dragStart.x, _dragEnd.x), Mathf.Min(_dragStart.y, _dragEnd.y), Mathf.Min(_dragStart.z, _dragEnd.z));
                var end   = new Vector3Int(Mathf.Max(_dragStart.x, _dragEnd.x), Mathf.Max(_dragStart.y, _dragEnd.y), Mathf.Max(_dragStart.z, _dragEnd.z));

                for (var x = start.x; x <= end.x; x++)
                for (var y = start.y; y <= end.y; y++)
                for (var z = start.z; z <= end.z; z++) {
                    if (_placeMatrixCount >= _placeMatrices.Length) break;

                    var cell          = new Vector3Int(x, y, z);
                    if(editor.level.GetElementAt(cell) != null) continue;
                    var worldPosition = editor.level.GetWorldPosition(cell);
                    _placeMatrices[_placeMatrixCount++] = Matrix4x4.TRS(worldPosition, Quaternion.identity, Vector3.one);
                }

                updateMatrices = false;
            }

            if (_placeMesh != null && _placeMatrixCount > 0) {
                VRCGraphics.DrawMeshInstanced(_placeMesh, 0, outlinePrepareMaterial, _placeMatrices, _placeMatrixCount);
                VRCGraphics.DrawMeshInstanced(_placeMesh, 0, placeOutlineMaterial,   _placeMatrices, _placeMatrixCount);
            }
        }

        private void BreakMode() {
            selection.Clear();
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);
            var element  = GetRayElement(rightRay, out var point, out var normal);
            if (element != null) selection.Add(new DataToken(element));

            DrawOutline(destroyOutlineMaterial);

            if (_useDown && element != null) Destroy(element);
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

        public override void InputLookVertical(float value, UdonInputEventArgs args) {
            if(!Networking.LocalPlayer.IsUserInVR()) return;
            if(mode == LevelEditorToolMode.Place) maxPlaceDistance = Mathf.Clamp(maxPlaceDistance + value * Time.deltaTime * 5f, 1, 100);
        }

        private void UpdateInputs() {
            _useDown = false;
            _useUp   = false;
        }

        private GameObject GetRayElement(Ray ray, out Vector3 point, out Vector3 normal) {
            normal = -ray.direction;

            var hitCount = Physics.RaycastNonAlloc(ray, _hits, maxPlaceDistance);

            if (hitCount > 0) {
                var        passed         = false;
                var        position       = Vector3.zero;
                var        nearest        = float.MaxValue;
                GameObject nearestElement = null;

                for (var i = 0; i < Mathf.Min(hitCount, _hits.Length); i++) {
                    var h = _hits[i];
                    if (h.collider == null) continue;

                    if (nearest < h.distance) continue;

                    if (editor.level.TryGetHitElement(h, out var element)) {
                        normal         = h.normal;
                        position       = h.point;
                        nearest        = h.distance;
                        passed         = true;
                        nearestElement = element;
                    } else if(h.collider.GetComponentInParent<Canvas>() != null) {
                        point = h.point;
                        return null;
                    }
                }

                if (passed) {
                    point = position;
                    return nearestElement;
                }
            }

            point = ray.origin + ray.direction * maxPlaceDistance;
            return null;
        }

        private void SetElement(int elementId) {
            this.elementId = elementId;
            _placeElement  = _levelLoader.GetElementPrefab(elementId);
            _placeMesh     = _placeElement.GetComponentInChildren<MeshFilter>().mesh;
        }

        public void SetMode(int mode) {
            this.mode                          = (LevelEditorToolMode)mode;
        }
    }

    public enum LevelEditorToolMode {
        Select,
        Place,
        Break,
        Paint,
    }
}