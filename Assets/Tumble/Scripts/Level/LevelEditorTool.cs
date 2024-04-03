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

        public TumbleLevel level;
        public float       maxPlaceDistance = 20f;
        public int         elementId        = 0;
        public Transform   previewHolder;
        
        public Material selectOutlineMaterial;
        public Material destroyOutlineMaterial;
        public Material placeOutlineMaterial;

        private Universe            _universe;
        private DualLaser           _laser;
        private TumbleLevelLoader64 _levelLoader;
        private GameObject          _placePreview;

        private RaycastHit[] _hits = new RaycastHit[20];

        // Inputs
        private bool _use;
        private bool _useDown;
        private bool _useHold;
        private bool _useUp;

        private void Start() {
            _universe    = GetComponentInParent<Universe>();
            _laser       = _universe.dualLaser;
            _levelLoader = _universe.levelLoader;
            SetElement(0);
            SetMode(1);
        }

        private void LateUpdate() {
            if (Input.GetKeyDown(KeyCode.Alpha1)) mode = LevelEditorToolMode.Select;
            if (Input.GetKeyDown(KeyCode.Alpha2)) mode = LevelEditorToolMode.Place;
            if (Input.GetKeyDown(KeyCode.Alpha3)) mode = LevelEditorToolMode.Destroy;
            if (Input.GetKeyDown(KeyCode.Alpha4)) mode = LevelEditorToolMode.Paint;

            switch (mode) {
                case LevelEditorToolMode.Select:
                    SelectMode();
                    break;
                case LevelEditorToolMode.Place:
                    PlaceMode();
                    break;
                case LevelEditorToolMode.Paint: break;
            }

            UpdateInputs();
        }

        private void SelectMode() { }

        private void PlaceMode() {
            var rightRay   = _laser.GetPointerRay(HandType.RIGHT, false);
            var hitElement = GetRayElement(rightRay, out var position, out var normal);
            position += normal * 0.1f;
            var levelCell = level.GetCell(position);
            previewHolder.position = level.GetWorldPosition(levelCell);

            if (_useDown) {
                var element  = _levelLoader.GetElementPrefab(elementId);
                var instance = Instantiate(element, level.GetElementHolder(elementId));
                instance.transform.localPosition = levelCell;
                instance.transform.rotation      = Quaternion.identity;
            }
        }

        private void BreakMode() {
            selection.Clear();
            var rightRay = _laser.GetPointerRay(HandType.RIGHT, false);
            var element  = GetRayElement(rightRay, out var point, out var normal);
            selection.Add(new DataToken(element));
            
            DrawOutline(destroyOutlineMaterial);

            if (_useDown && element != null) Destroy(element);
        }

        private void DrawOutline(Material material) {
            var meshes = new DataDictionary();

            foreach (var entry in selection.ToArray()) {
                var element = (GameObject)entry.Reference;
                if (element == null) continue;

                var mesh = element.GetComponentInChildren<MeshFilter>();
                if (mesh == null) continue;

                var meshToken = new DataToken(mesh.sharedMesh);
                if (!meshes.ContainsKey(meshToken)) meshes.Add(meshToken, new DataList());
                meshes[meshToken].DataList.Add(new DataToken(mesh.transform));
            }
            
            foreach (var meshToken in meshes.GetKeys().ToArray()) {
                var values = meshes[meshToken].DataList;
                var matrices = new Matrix4x4[values.Count];
                for (var i = 0; i < values.Count; i++) {
                    var element = (GameObject)values[i].Reference;
                    matrices[i] = element.transform.localToWorldMatrix;
                }
                
                VRCGraphics.DrawMeshInstanced((Mesh)meshToken.Reference, 0, material, matrices, matrices.Length);
            }
        }

        public override void InputUse(bool value, UdonInputEventArgs args) {
            _use     = value;
            _useDown = value;
        }

        private void UpdateInputs() {
            _useUp   = !_use && _useHold;
            _useHold = _use;
            _useDown = false;
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

                    if (level.TryGetHitElement(h, out var element)) {
                        normal         = h.normal;
                        position       = h.point;
                        nearest        = h.distance;
                        passed         = true;
                        nearestElement = element;
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
            if (_placePreview != null) Destroy(_placePreview);
            _placePreview                         = Instantiate(_levelLoader.GetElementPrefab(elementId), previewHolder);
            _placePreview.transform.localPosition = Vector3.zero;
            _placePreview.transform.localRotation = Quaternion.identity;
            _placePreview.transform.localScale    = Vector3.one;
        }

        public void SetMode(int mode) {
            this.mode = (LevelEditorToolMode)mode;

            previewHolder.gameObject.SetActive(this.mode == LevelEditorToolMode.Place);
        }
    }

    public enum LevelEditorToolMode {
        Select,
        Place,
        Destroy,
        Paint,
    }
}