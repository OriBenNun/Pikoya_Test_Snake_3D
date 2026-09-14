using System.Collections.Generic;
using UnityEngine;

namespace GardenSnake
{
    /// <summary>Articulates the existing face and carries the picked apple into its mouth.</summary>
    public sealed class SnakeMouth : MonoBehaviour
    {
        /// <summary>A piece of the upper face, and where it sits when the mouth is shut.</summary>
        private readonly struct FacePart
        {
            public readonly Transform Transform;
            public readonly Vector3 RestPosition;
            public readonly Quaternion RestRotation;

            public FacePart(Transform transform, Vector3 restPosition, Quaternion restRotation)
            {
                Transform = transform;
                RestPosition = restPosition;
                RestRotation = restRotation;
            }
        }

        /// <summary>One eye and everything on that side of the face, parented so they move together.</summary>
        private readonly struct EyeRig
        {
            public readonly Transform Transform;
            public readonly Vector3 RestPosition;

            public EyeRig(Transform transform, Vector3 restPosition)
            {
                Transform = transform;
                RestPosition = restPosition;
            }
        }

        /// <summary>Age given to a swallow that is over, so it never replays on its own.</summary>
        private const float Finished = 99f;

        [SerializeField] private SnakeMouthSettings settings;
        private bool ownsSettings;
        private readonly List<FacePart> upperFace = new();
        private readonly List<EyeRig> eyes = new();
        private Transform cavity;
        private Transform lip;
        private Transform jaw;
        private Transform tongue;
        private Transform muzzle;
        private Transform swallowedApple;
        private Vector3 muzzleScale;
        private Vector3 appleStart;
        private Vector3 appleSize;
        private Vector3 facePivot;
        private float faceLever;
        private float swallowAge = Finished;
        private float swallowDuration = .2f;
        private float excitementAge;
        public float Openness { get; private set; }

        public void Initialize(GameObject applePrefab, SnakeMouthSettings tuning = null)
        {
            if (tuning != null) settings = tuning;
            if (settings == null) { settings = ScriptableObject.CreateInstance<SnakeMouthSettings>(); ownsSettings = true; }
            MeshRenderer[] parts = GetComponentsInChildren<MeshRenderer>();
            Material cream = null, ink = null, blush = null;
            foreach (var renderer in parts)
            {
                string part = renderer.name;
                if (part == "Muzzle") { muzzle = renderer.transform; cream = renderer.sharedMaterial; }
                if (part.StartsWith("Pupil")) ink = renderer.sharedMaterial;
                if (part.StartsWith("Cheek")) blush = renderer.sharedMaterial;
                // The authored smile and tongue give way to the articulated pieces built below.
                if (part.StartsWith("Smile") || part == "Tongue") { renderer.enabled = false; continue; }
                if (IsEyePart(part)) continue;
                var face = new FacePart(renderer.transform,
                    transform.InverseTransformPoint(renderer.transform.position),
                    Quaternion.Inverse(transform.rotation) * renderer.transform.rotation);
                upperFace.Add(face);
                if (part == "Head") facePivot = face.RestPosition;
            }
            CreateEyeRigs(parts);
            faceLever = Mathf.Max(.01f, Mathf.Abs(transform.InverseTransformPoint(muzzle.position).z - facePivot.z));
            muzzleScale = muzzle.localScale;
            cavity = CreateBlob("Mouth interior", ink);
            lip = CreateBlob("Stretchy mouth rim", cream);
            jaw = CreateBlob("Lower jaw", cream);
            tongue = CreateBlob("Mouth tongue", blush);
            swallowedApple = Instantiate(applePrefab, transform.parent).transform;
            swallowedApple.name = "Swallowed apple";
            ResetPose();
        }

        private static bool IsEyePart(string part) => part.StartsWith("Eye white") ||
            part.StartsWith("Pupil") || part.StartsWith("Eye glint") || part.StartsWith("Eyebrow");

        /// <summary>Re-parents each eye's pieces under one rig, so an excited eye moves as a unit.</summary>
        private void CreateEyeRigs(MeshRenderer[] parts)
        {
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
                eyes.Add(new EyeRig(rig, rest));
            }
        }

        /// <summary>The mouth's soft parts are all the same thing: a tinted sphere it stretches.</summary>
        private Transform CreateBlob(string label, Material material)
        {
            var blob = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            blob.name = label;
            blob.transform.SetParent(transform, false);
            Destroy(blob.GetComponent<Collider>());
            blob.GetComponent<MeshRenderer>().sharedMaterial = material;
            return blob.transform;
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
            swallowAge = Finished;
            excitementAge = 0;
            Openness = 0;
            swallowedApple.gameObject.SetActive(false);
            Pose();
        }

        public void Animate(GameLoopManager loop, Vector3 foodPosition, float delta)
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
            float target = loop.State == RunState.Playing && loop.HasFood && ahead > 0 && sideways < settings.AnticipationHalfWidth
                ? Mathf.SmoothStep(0, 1, (settings.AnticipationDistance - ahead) / Mathf.Max(.01f, settings.AnticipationRamp)) : 0;
            if (swallowing && loop.State != RunState.Lost) target = Mathf.Max(target, 1 - Mathf.Clamp01(swallowAge / swallowDuration));
            Openness = Mathf.MoveTowards(Openness, target, delta * settings.JawSpeed);
            Pose();
        }

        private void Pose()
        {
            // Hinge at the head's neck-height center instead of lifting the neck off the body.
            Quaternion opening = Quaternion.Euler(-Mathf.Atan2(settings.UpperFaceLift, faceLever)
                * Mathf.Rad2Deg * Openness, 0, 0);
            for (int i = 0; i < upperFace.Count; i++)
            {
                FacePart face = upperFace[i];
                face.Transform.SetPositionAndRotation(
                    transform.TransformPoint(facePivot + opening * (face.RestPosition - facePivot)),
                    transform.rotation * opening * face.RestRotation);
            }
            // Imported facial meshes have local Y along the snout and local Z pointing upward.
            muzzle.localScale = Vector3.Scale(muzzleScale, new Vector3(1 + settings.MuzzleWidening * Openness,
                1 - settings.MuzzleRetraction * Openness, 1));
            for (int i = 0; i < eyes.Count; i++)
            {
                EyeRig eye = eyes[i];
                Vector3 rest = eye.RestPosition;
                float bounce = Mathf.Sin(excitementAge * settings.EyeBounceFrequency * Mathf.PI * 2 + i * .8f)
                    * settings.EyeBounce * Openness;
                eye.Transform.localPosition = facePivot + opening * (rest - facePivot) +
                    new Vector3(Mathf.Sign(rest.x) * settings.EyeSpread * Openness, settings.EyeLift * Openness + bounce, 0);
                eye.Transform.localRotation = opening;
                eye.Transform.localScale = Vector3.Lerp(Vector3.one, settings.ExcitedEyeScale, Openness);
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
