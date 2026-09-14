using MoreMountains.Feedbacks;
using UnityEngine;
using UnityEngine.Rendering;

namespace GardenSnake
{
    /// <summary>
    /// The reaction layer. It listens to the game loop and answers every beat a player can feel:
    /// the sound, the Feel players, the ripple through the board, the ring a picked apple throws,
    /// the sparkle behind the head, the vignette that closes in with the pace, and the words the
    /// HUD throws up.
    /// <para>
    /// Nothing holds a reference to it. It only ever observes, so the game can run with this
    /// object switched off and still be a correct, silent game.
    /// </para>
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public sealed class FeedbackManager : MonoBehaviour
    {
        /// <summary>The pitch and level of every one-shot, kept together so the mix reads as a mix.</summary>
        [System.Serializable]
        public sealed class SoundMix
        {
            [Range(.1f, 3)] public float startPitch = 1;
            [Range(0, 1)] public float startVolume = .5f;
            public Vector2 turnPitch = new(.96f, 1.06f);
            [Range(0, 1)] public float turnVolume = .16f;
            [Range(.1f, 3)] public float clickPitch = 1;
            [Range(0, 1)] public float clickVolume = .35f;
            [Range(.1f, 3)] public float pickupPitch = 1;
            [Tooltip("Pickups climb a short scale, then start it again.")]
            [Min(1)] public int pickupPitchCycle = 6;
            [Min(0)] public float pickupPitchIncrement = .045f;
            [Range(0, 1)] public float pickupVolume = .6f;
            [Range(.1f, 3)] public float bestPitch = 1;
            [Range(0, 1)] public float bestVolume = .45f;
            [Range(.1f, 3)] public float recordPitch = 1.35f;
            [Range(0, 1)] public float recordVolume = .1f;
            [Range(.1f, 3)] public float losePitch = 1;
            [Range(0, 1)] public float loseVolume = .55f;
        }

        /// <summary>The Feel players, and how hard each beat is allowed to land.</summary>
        [System.Serializable]
        public sealed class FeelPlayers
        {
            public MMF_Player pickup;
            public MMF_Player death;
            public MMF_Player runStart;
            public MMF_Player newBest;
            public MMF_Player recordApple;
            [Tooltip("Intensity of the first pickup and of a fully escalated pickup.")]
            public Vector2 pickupIntensity = new(.75f, 1.5f);
            [Min(1)] public int fullPickupIntensityScore = 14;
            [Min(0)] public float deathIntensity = 1;
            [Min(0)] public float runStartIntensity = 1;
            [Min(0)] public float newBestIntensity = 1;
            [Min(0)] public float recordAppleIntensity = 1;
        }

        /// <summary>Which wave the board runs for each moment, and how far it carries.</summary>
        [System.Serializable]
        public sealed class WaveCues
        {
            public GridCellWaves.Pattern start = GridCellWaves.Pattern.Sweep;
            public GridCellWaves.Pattern death = GridCellWaves.Pattern.Ripple;
            public GridCellWaves.Pattern best = GridCellWaves.Pattern.Bloom;
            public GridCellWaves.Pattern milestone = GridCellWaves.Pattern.CheckerHop;
            public GridCellWaves.Pattern victory = GridCellWaves.Pattern.Bloom;
            [Min(0)] public float startIntensity = 1;
            [Min(0)] public float deathIntensity = 1;
            [Min(0)] public float bestIntensity = 1;
            [Min(0)] public float milestoneIntensity = 1;
            [Min(0)] public float victoryIntensity = 1.5f;
            [Min(1)] public int milestoneAppleInterval = 10;
        }

        [Header("Observed")]
        [SerializeField] private GameLoopManager loop;
        [SerializeField] private SnakeManager snake;
        [Header("Stage")]
        [SerializeField] private SnakeHud hud;
        [SerializeField] private GridCellWaves cellWaves;
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
        [Header("Tuning")]
        [SerializeField] private SoundMix sound = new();
        [SerializeField] private FeelPlayers feel = new();
        [SerializeField] private WaveCues waves = new();

        // The shape of each reaction, not settings.
        private const float BurstDuration = .42f;
        private const float BurstEasePower = 2.6f;
        private const float BurstStartScale = .6f;
        private const float BurstEndScale = 2.5f;
        private const float BurstOpacity = .55f;
        private const float BurstHeight = .05f;
        private const float TrailEmissionSlow = 10f;
        private const float TrailEmissionFast = 30f;
        private const float TrailHeight = .12f;
        private const float PaceVolumeBlendSpeed = 1.2f;
        private const int DeathParticleCount = 18;
        private const float DeathParticleHeight = .3f;
        /// <summary>Age given to a burst that is over, so it never replays on its own.</summary>
        private const float Finished = 99f;

        private static readonly int AlphaProperty = Shader.PropertyToID("_Alpha");

        private Material burstMaterial;
        private float burstAge = Finished;
        private bool burstShown;

        private void Awake()
        {
            burstMaterial = burstRing.GetComponent<Renderer>().material;
            burstShown = burstRing.gameObject.activeSelf;
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
            Strike(Beat.RunStart, feel.runStart, head, feel.runStartIntensity);
            cellWaves.Play(waves.start, loop.Body[0], waves.startIntensity);
        }

        private void OnAppleEaten(AppleBeat apple)
        {
            Strike(Beat.Pickup, feel.pickup, apple.At, Mathf.Lerp(feel.pickupIntensity.x, feel.pickupIntensity.y,
                Mathf.Clamp01(apple.Score / (float)feel.fullPickupIntensityScore)));
            burstAge = 0;
            burstRing.position = apple.At + Vector3.up * BurstHeight;
            Play(pickupSound, sound.pickupPitch + apple.Score % sound.pickupPitchCycle * sound.pickupPitchIncrement,
                sound.pickupVolume);
            bool extended = apple.Record == RecordBeat.Extended;
            hud.ShowPickup(extended ? "+1 <size=55%>best</size>" : "+1", apple.At, extended);
            if (apple.Record == RecordBeat.Broken)
            {
                cellWaves.Play(waves.best, loop.Body[0], waves.bestIntensity);
                Strike(Beat.NewBest, feel.newBest, apple.At, feel.newBestIntensity);
                Play(bestSound, sound.bestPitch, sound.bestVolume);
                hud.ShowBanner("NEW BEST");
            }
            else if (extended)
            {
                Strike(Beat.RecordApple, feel.recordApple, apple.At, feel.recordAppleIntensity);
                Play(bestSound, sound.recordPitch, sound.recordVolume);
                hud.WhisperBest();
            }
            else if (apple.Score % waves.milestoneAppleInterval == 0)
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
                Strike(Beat.Death, feel.death, at, feel.deathIntensity);
                cellWaves.Play(waves.death, head, waves.deathIntensity);
                pickupParticles.transform.position = at + Vector3.up * DeathParticleHeight;
                pickupParticles.Emit(DeathParticleCount);
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

        private void OnChanged() => cellWaves.Frozen = loop.State == RunState.Paused;

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

        // The frame

        private void Update()
        {
            float delta = Time.unscaledDeltaTime;
            bool moving = loop.State == RunState.Playing;
            AnimateBurst(delta);
            AnimateTrail(moving);
            float wanted = moving ? loop.CurrentPace : 0;
            paceVolume.weight = Mathf.MoveTowards(paceVolume.weight, wanted, delta * PaceVolumeBlendSpeed);
        }

        /// <summary>One expanding ring per apple: the pickup gets a shape, not just particles.</summary>
        private void AnimateBurst(float delta)
        {
            if (burstAge >= BurstDuration)
            {
                ShowBurst(false);
                return;
            }
            burstAge += delta;
            ShowBurst(true);
            float t = Mathf.Clamp01(burstAge / BurstDuration);
            float eased = 1 - Mathf.Pow(1 - t, BurstEasePower);
            burstRing.localScale = Vector3.one * Mathf.Lerp(BurstStartScale, BurstEndScale, eased);
            burstMaterial.SetFloat(AlphaProperty, (1 - t) * (1 - t) * BurstOpacity);
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
            emission.rateOverTime = Mathf.Lerp(TrailEmissionSlow, TrailEmissionFast, loop.CurrentPace);
            if (moving && !trailParticles.isPlaying) trailParticles.Play();
            if (!moving && trailParticles.isPlaying) trailParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            trailParticles.transform.position = snake.HeadPosition + Vector3.up * TrailHeight;
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
