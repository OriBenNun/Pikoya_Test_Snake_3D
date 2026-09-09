using System.Collections.Generic;
using GardenSnake.Core;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>Articulates the existing face and carries the picked apple into its mouth.</summary>
    public sealed class SnakeMouth : MonoBehaviour
    {
        [SerializeField] private SnakeMouthSettings settings;
        private bool ownsSettings;
        private readonly List<Transform> upperFace = new List<Transform>();
        private readonly List<Vector3> upperRest = new List<Vector3>();
        private Transform cavity, jaw, tongue, muzzle, swallowedApple;
        private Vector3 muzzleScale, appleStart, appleSize;
        private float swallowAge = 99, swallowDuration = .2f;
        public float Openness { get; private set; }

        public void Initialize(GameObject applePrefab, SnakeMouthSettings tuning = null)
        {
            if (tuning != null) settings = tuning;
            if (settings == null) { settings = ScriptableObject.CreateInstance<SnakeMouthSettings>(); ownsSettings = true; }
            Material cream = null, ink = null, blush = null;
            foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                string part = renderer.name;
                if (part == "Muzzle") { muzzle = renderer.transform; cream = renderer.sharedMaterial; }
                if (part.StartsWith("Pupil")) ink = renderer.sharedMaterial;
                if (part.StartsWith("Cheek")) blush = renderer.sharedMaterial;
                if (part.StartsWith("Smile") || part == "Tongue")
                {
                    renderer.enabled = false;
                    continue;
                }
                upperFace.Add(renderer.transform);
                upperRest.Add(transform.InverseTransformPoint(renderer.transform.position));
            }
            muzzleScale = muzzle.localScale;
            cavity = Piece("Mouth interior", ink);
            jaw = Piece("Lower jaw", cream);
            tongue = Piece("Mouth tongue", blush);
            swallowedApple = Instantiate(applePrefab, transform.parent).transform;
            swallowedApple.name = "Swallowed apple";
            ResetPose();
        }

        private Transform Piece(string label, Material material)
        {
            var piece = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            piece.name = label;
            piece.transform.SetParent(transform, false);
            Destroy(piece.GetComponent<Collider>());
            piece.GetComponent<MeshRenderer>().sharedMaterial = material;
            return piece.transform;
        }

        public void Swallow(Transform apple, float stepSeconds)
        {
            appleStart = apple.position;
            appleSize = apple.localScale;
            swallowedApple.SetPositionAndRotation(appleStart, apple.rotation);
            swallowedApple.localScale = appleSize;
            swallowedApple.gameObject.SetActive(true);
            swallowAge = 0;
            // Complete before the next possible pickup, even at maximum speed.
            swallowDuration = Mathf.Max(.001f, stepSeconds * Mathf.Clamp(settings.SwallowStepFraction, .05f, 1f));
            Openness = 1;
        }

        public void ResetPose()
        {
            swallowAge = 99;
            Openness = 0;
            swallowedApple.gameObject.SetActive(false);
            Pose();
        }

        public void Animate(SnakeGame game, Vector3 foodPosition, float delta)
        {
            if (delta <= 0) return;
            bool swallowing = swallowAge < swallowDuration;
            if (swallowing)
            {
                swallowAge += delta;
                float t = Mathf.Clamp01(swallowAge / swallowDuration);
                Vector3 destination = transform.TransformPoint(settings.SwallowDestination);
                swallowedApple.position = Vector3.Lerp(appleStart, destination, Mathf.SmoothStep(0, 1, t));
                swallowedApple.localScale = appleSize * (1 - Mathf.Pow(t, Mathf.Max(.1f, settings.AppleShrinkPower)));
                swallowedApple.Rotate(Vector3.right, delta * settings.AppleSpin, Space.Self);
                if (t >= 1) swallowedApple.gameObject.SetActive(false);
            }
            Vector3 offset = foodPosition - transform.position;
            offset.y = 0;
            float ahead = Vector3.Dot(offset, transform.forward);
            float sideways = Mathf.Abs(Vector3.Dot(offset, transform.right));
            float target = game.State == RunState.Playing && game.HasFood && ahead > 0 && sideways < settings.AnticipationHalfWidth
                ? Mathf.SmoothStep(0, 1, (settings.AnticipationDistance - ahead) / Mathf.Max(.01f, settings.AnticipationRamp)) : 0;
            if (swallowing && game.State != RunState.Lost) target = Mathf.Max(target, 1 - Mathf.Clamp01(swallowAge / swallowDuration));
            Openness = Mathf.MoveTowards(Openness, target, delta * settings.JawSpeed);
            Pose();
        }

        private void Pose()
        {
            for (int i = 0; i < upperFace.Count; i++)
                upperFace[i].position = transform.TransformPoint(upperRest[i] + Vector3.up * (settings.UpperFaceLift * Openness));
            muzzle.localScale = Vector3.Scale(muzzleScale, new Vector3(1, 1, 1 - settings.MuzzleRetraction * Openness));
            cavity.localPosition = settings.CavityPosition + settings.CavityOpeningOffset * Openness;
            cavity.localScale = settings.CavityScale + settings.CavityOpeningScale * Openness;
            cavity.localRotation = Quaternion.Euler(settings.CavityRotation);
            jaw.localPosition = settings.JawPosition + settings.JawOpeningOffset * Openness;
            jaw.localScale = settings.JawScale + settings.JawOpeningScale * Openness;
            tongue.localPosition = settings.TonguePosition + settings.TongueOpeningOffset * Openness;
            tongue.localScale = settings.TongueScale;
            tongue.gameObject.SetActive(Openness > settings.TongueThreshold);
        }
        private void OnDestroy()
        {
            if (ownsSettings && settings != null) Destroy(settings);
        }
    }
}
