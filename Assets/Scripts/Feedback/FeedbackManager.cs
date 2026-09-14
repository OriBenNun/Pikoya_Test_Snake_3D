using System.Collections;
using GardenSnake.Gameplay;
using GardenSnake.Presentation;
using GardenSnake.Presentation.Hud;
using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>
    /// The reaction layer. It listens to the game loop and answers every beat a player can feel:
    /// the sound, the Feel players, the ripple through the board, the ring a picked apple throws,
    /// the sparkle behind the head, the vignette that closes in with the pace, the camera pushing
    /// in on a run, and the words the HUD throws up.
    /// <para>
    /// Nothing holds a reference to it. It only ever observes, so the game can run with this
    /// object switched off and still be a correct, silent game.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class FeedbackManager : MonoBehaviour
    {
        /// <summary>The Feel players, one per beat. Wiring, not tuning.</summary>
        [System.Serializable]
        public sealed class FeelPlayers
        {
            public MMF_Player pickup;
            public MMF_Player death;
            public MMF_Player runStart;
            public MMF_Player newBest;
            public MMF_Player recordApple;
        }

        [Header("Observed")]
        [SerializeField] private GameLoopManager loop;
        [SerializeField] private SnakeManager snake;
        [Header("Stage")]
        [SerializeField] private SnakeHud hud;
        [SerializeField] private GridCellWaves cellWaves;
        [SerializeField] private Camera view;
        [SerializeField] private Volume paceVolume;
        [SerializeField] private Transform burstRing;
        [SerializeField] private ParticleSystem pickupParticles;
        [SerializeField] private ParticleSystem trailParticles;
        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioClip pickupSound;
        [SerializeField] private AudioClip turnSound;
        [SerializeField] private AudioClip loseSound;
        [SerializeField] private AudioClip startSound;
        [SerializeField] private AudioClip bestSound;
        [SerializeField] private AudioClip clickSound;
        [Header("Feel")]
        [SerializeField] private FeelPlayers feel = new();
        [Header("Tuning")]
        [SerializeField] private SoundMixSettings sound;
        [SerializeField] private FeelBeatSettings beats;
        [SerializeField] private WaveCueSettings waves;
        [SerializeField] private ReactionSettings reactions;
        [Header("Camera")]
        [SerializeField, Range(1f, 1.4f), Tooltip("How far the camera sits back on the menus, against the framing it plays at.")]
        private float restingZoom = 1.13f;
        [SerializeField, Range(.05f, 2f), Tooltip("Seconds the camera takes to push in on a run, and to settle back after it.")]
        private float zoomSeconds = .45f;

        /// <summary>Age given to a burst that is over, so it never replays on its own.</summary>
        private const float Finished = 99f;

        private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

        private Material burstMaterial;
        /// <summary>The ring's authored colour; only its alpha moves.</summary>
        private Color burstTint;
        private float burstAge = Finished;
        private bool burstShown;
        /// <summary>The framing the scene was authored at; the player settings decide the window.</summary>
        private float playingSize;
        private float zoomTarget;
        private Coroutine zooming;

        private void Awake()
        {
            sound = Tuning.Or(sound);
            beats = Tuning.Or(beats);
            waves = Tuning.Or(waves);
            reactions = Tuning.Or(reactions);
            burstMaterial = burstRing.GetComponent<Renderer>().material;
            burstTint = burstMaterial.GetColor(BaseColorProperty);
            burstShown = burstRing.gameObject.activeSelf;
            if (view == null) return;
            playingSize = view.orthographicSize;
            zoomTarget = playingSize * restingZoom;
            view.orthographicSize = zoomTarget;
        }

        private void OnEnable()
        {
            loop.RunStarted += OnRunStarted;
            loop.AppleEaten += OnAppleEaten;
            loop.RunEnded += OnRunEnded;
            loop.TurnAccepted += OnTurnAccepted;
            loop.MuteChanged += ApplyMute;
            loop.Clicked += OnClicked;
            loop.Changed += OnChanged;
            snake.VisualsReset += OnVisualsReset;
        }

        private void OnDisable()
        {
            loop.RunStarted -= OnRunStarted;
            loop.AppleEaten -= OnAppleEaten;
            loop.RunEnded -= OnRunEnded;
            loop.TurnAccepted -= OnTurnAccepted;
            loop.MuteChanged -= ApplyMute;
            loop.Clicked -= OnClicked;
            loop.Changed -= OnChanged;
            snake.VisualsReset -= OnVisualsReset;
        }

        private void Start() => musicSource.Play();

        // Beats

        private void OnRunStarted(Vector3 head)
        {
            cellWaves.Clear();
            Play(startSound, sound.startPitch, sound.startVolume);
            Strike(Beat.RunStart, feel.runStart, head, beats.runStartIntensity);
            cellWaves.Play(waves.start, loop.Body[0], waves.startIntensity);
        }

        private void OnAppleEaten(AppleBeat apple)
        {
            Strike(Beat.Pickup, feel.pickup, apple.At,
                Mathf.Lerp(beats.pickupIntensity.x, beats.pickupIntensity.y,
                    Mathf.Clamp01(apple.Score / (float)Mathf.Max(1, beats.fullPickupIntensityScore))));
            burstAge = 0;
            burstRing.position = apple.At + Vector3.up * reactions.burstHeight;
            Play(pickupSound,
                sound.pickupPitch + apple.Score % Mathf.Max(1, sound.pickupPitchCycle) * sound.pickupPitchIncrement,
                sound.pickupVolume);
            bool extended = apple.Record == RecordBeat.Extended;
            hud.ShowPickup(extended ? "+1 <size=55%>best</size>" : "+1", apple.At, extended);
            if (apple.Record == RecordBeat.Broken)
            {
                cellWaves.Play(waves.best, loop.Body[0], waves.bestIntensity);
                Strike(Beat.NewBest, feel.newBest, apple.At, beats.newBestIntensity);
                Play(bestSound, sound.bestPitch, sound.bestVolume);
                hud.ShowBanner("NEW BEST");
            }
            else if (extended)
            {
                Strike(Beat.RecordApple, feel.recordApple, apple.At, beats.recordAppleIntensity);
                Play(bestSound, sound.recordPitch, sound.recordVolume);
                hud.WhisperBest();
            }
            else if (apple.Score % Mathf.Max(1, waves.appleInterval) == 0)
            {
                cellWaves.Play(waves.milestone, loop.Body[0], waves.milestoneIntensity);
                hud.ShowBanner(apple.Score + " APPLES");
            }
        }

        private void OnRunEnded(StepResult result)
        {
            Cell head = loop.Body[0];
            Vector3 at = loop.World(head);
            if (result == StepResult.Lost)
            {
                Play(loseSound, sound.losePitch, sound.loseVolume);
                Strike(Beat.Death, feel.death, at, beats.deathIntensity);
                cellWaves.Play(waves.death, head, waves.deathIntensity);
                pickupParticles.transform.position = at + Vector3.up * reactions.deathParticleHeight;
                pickupParticles.Emit(reactions.deathParticleCount);
            }
            else
            {
                cellWaves.Play(waves.victory, head, waves.victoryIntensity);
                hud.ShowBanner("GARDEN COMPLETE");
            }
        }

        private void OnTurnAccepted(int turnSign) =>
            Play(turnSound, Random.Range(sound.turnPitch.x, sound.turnPitch.y), sound.turnVolume);

        private void OnClicked() => Play(clickSound, sound.clickPitch, sound.clickVolume);

        private void OnChanged()
        {
            cellWaves.Frozen = loop.State == RunState.Paused;
            ZoomTo(loop.State == RunState.Playing ? playingSize : playingSize * restingZoom);
        }

        private void OnVisualsReset()
        {
            burstAge = Finished;
            pickupParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        private void ApplyMute(bool muted)
        {
            audioSource.mute = muted;
            musicSource.mute = muted;
        }

        // The camera

        /// <summary>
        /// The garden is framed by the player settings' resolution, so the camera only ever eases
        /// between two sizes: pushed in while a run is going, sitting back on the menus.
        /// </summary>
        private void ZoomTo(float size)
        {
            if (view == null || Mathf.Approximately(zoomTarget, size)) return;
            zoomTarget = size;
            if (zooming != null) StopCoroutine(zooming);
            zooming = StartCoroutine(Zooming(size));
        }

        private IEnumerator Zooming(float size)
        {
            float from = view.orthographicSize;
            for (float age = 0; age < zoomSeconds; age += Time.unscaledDeltaTime)
            {
                view.orthographicSize = Mathf.Lerp(from, size, Mathf.SmoothStep(0, 1, age / zoomSeconds));
                yield return null;
            }
            view.orthographicSize = size;
            zooming = null;
        }

        // The frame

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            bool moving = loop.State == RunState.Playing;
            AnimateBurst(delta);
            AnimateTrail(moving);
            float wanted = moving ? loop.CurrentPace : 0;
            paceVolume.weight = Mathf.MoveTowards(paceVolume.weight, wanted,
                delta * reactions.paceVolumeBlendSpeed);
        }

        /// <summary>One expanding ring per apple: the pickup gets a shape, not just particles.</summary>
        private void AnimateBurst(float delta)
        {
            if (burstAge >= reactions.burstDuration)
            {
                ShowBurst(false);
                return;
            }
            burstAge += delta;
            ShowBurst(true);
            float t = Mathf.Clamp01(burstAge / reactions.burstDuration);
            float eased = 1 - Mathf.Pow(1 - t, reactions.burstEasePower);
            burstRing.localScale = Vector3.one
                * Mathf.Lerp(reactions.burstStartScale, reactions.burstEndScale, eased);
            burstTint.a = (1 - t) * (1 - t) * reactions.burstOpacity;
            burstMaterial.SetColor(BaseColorProperty, burstTint);
        }

        private void ShowBurst(bool visible)
        {
            if (burstShown == visible) return;
            burstShown = visible;
            burstRing.gameObject.SetActive(visible);
        }

        private void AnimateTrail(bool moving)
        {
            var emission = trailParticles.emission;
            // The faster the run gets, the more the snake leaves behind it.
            emission.rateOverTime = Mathf.Lerp(reactions.trailEmissionSlow, reactions.trailEmissionFast,
                loop.CurrentPace);
            if (moving && !trailParticles.isPlaying) trailParticles.Play();
            if (!moving && trailParticles.isPlaying) trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailParticles.transform.position = snake.HeadPosition + Vector3.up * reactions.trailHeight;
        }

        // Striking a beat

        /// <summary>
        /// Plays the Feel player for a beat and puts the same beat on the garden's channel, so
        /// the scenery can lean with it without anything knowing the scenery is there.
        /// </summary>
        private void Strike(Beat beat, MMF_Player player, Vector3 at, float intensity)
        {
            GardenBeats.Strike(beat, at, intensity);
            if (player == null) return;
            player.PlayFeedbacks(at, intensity);
        }

        private void Play(AudioClip clip, float pitch, float volume)
        {
            audioSource.pitch = pitch;
            audioSource.PlayOneShot(clip, volume);
        }
    }
}
