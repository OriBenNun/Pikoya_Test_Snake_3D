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
        private readonly List<Transform> eyes = new List<Transform>();
        private readonly List<Vector3> eyeRest = new List<Vector3>();
        private Transform cavity, lip, jaw, tongue, muzzle, swallowedApple;
        private Vector3 muzzleScale, appleStart, appleSize;
        private float swallowAge = 99, swallowDuration = .2f;
        private float excitementAge;
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
                if (!IsEyePart(part))
                {
                    upperFace.Add(renderer.transform);
                    upperRest.Add(transform.InverseTransformPoint(renderer.transform.position));
                }
            }
            CreateEyeRigs();
            muzzleScale = muzzle.localScale;
            cavity = Piece("Mouth interior", ink);
            lip = Piece("Stretchy mouth rim", cream);
            jaw = Piece("Lower jaw", cream);
            tongue = Piece("Mouth tongue", blush);
            swallowedApple = Instantiate(applePrefab, transform.parent).transform;
            swallowedApple.name = "Swallowed apple";
            ResetPose();
        }

        private static bool IsEyePart(string part) => part.StartsWith("Eye white") ||
            part.StartsWith("Pupil") || part.StartsWith("Eye glint") || part.StartsWith("Eyebrow");

        private void CreateEyeRigs()
        {
            var parts = GetComponentsInChildren<MeshRenderer>();
            foreach (var white in parts)
            {
                if (!white.name.StartsWith("Eye white")) continue;
                Vector3 rest = transform.InverseTransformPoint(white.transform.position);
                var rig = new GameObject("Excited eye").transform;
                rig.SetParent(transform, false);
                rig.localPosition = rest;
                foreach (var part in parts)
                {
                    if (!IsEyePart(part.name)) continue;
                    float side = transform.InverseTransformPoint(part.transform.position).x;
                    if (Mathf.Sign(side) == Mathf.Sign(rest.x)) part.transform.SetParent(rig, true);
                }
                eyes.Add(rig);
                eyeRest.Add(rest);
            }
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
            excitementAge = 0;
            Openness = 0;
            swallowedApple.gameObject.SetActive(false);
            Pose();
        }

        public void Animate(SnakeGame game, Vector3 foodPosition, float delta)
        {
            if (delta <= 0) return;
            excitementAge += delta;
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
            // Imported facial meshes have local Y along the snout and local Z pointing upward.
            muzzle.localScale = Vector3.Scale(muzzleScale, new Vector3(1 + settings.MuzzleWidening * Openness,
                1 - settings.MuzzleRetraction * Openness, 1));
            for (int i = 0; i < eyes.Count; i++)
            {
                Vector3 rest = eyeRest[i];
                float bounce = Mathf.Sin(excitementAge * settings.EyeBounceFrequency * Mathf.PI * 2 + i * .8f)
                    * settings.EyeBounce * Openness;
                eyes[i].localPosition = rest + new Vector3(Mathf.Sign(rest.x) * settings.EyeSpread * Openness,
                    (settings.UpperFaceLift + settings.EyeLift) * Openness + bounce, 0);
                eyes[i].localScale = Vector3.Lerp(Vector3.one, settings.ExcitedEyeScale, Openness);
            }
            cavity.localPosition = settings.CavityPosition + settings.CavityOpeningOffset * Openness;
            cavity.localScale = settings.CavityScale + settings.CavityOpeningScale * Openness;
            cavity.localRotation = Quaternion.Euler(settings.CavityRotation);
            lip.localPosition = cavity.localPosition - cavity.localRotation * Vector3.forward * settings.LipInset;
            lip.localRotation = cavity.localRotation;
            lip.localScale = cavity.localScale + settings.LipThickness;
            lip.gameObject.SetActive(Openness > .05f);
            jaw.localPosition = settings.JawPosition + settings.JawOpeningOffset * Openness;
            jaw.localScale = settings.JawScale + settings.JawOpeningScale * Openness;
            tongue.localPosition = settings.TonguePosition + settings.TongueOpeningOffset * Openness;
            tongue.localScale = settings.TongueScale + settings.TongueOpeningScale * Openness;
            tongue.gameObject.SetActive(Openness > settings.TongueThreshold);
        }
        private void OnDestroy()
        {
            if (ownsSettings && settings != null) Destroy(settings);
        }
    }
}
