using MoreMountains.Feedbacks;
using UnityEditor;
using UnityEngine;
using static GardenSnake.Editor.GardenPalette;

namespace GardenSnake.Editor
{
    /// <summary>
    /// Authors the Feel players. Each one is tuned to be felt rather than noticed: shakes are short,
    /// the freeze frames are a couple of frames long, and only death is allowed to slow time down.
    /// </summary>
    public static class GardenFeel
    {
        public static void Create(SnakeController game, SnakeHud hud, Transform board, ParticleSystem burst)
        {
            var root = new GameObject("Feel").transform;
            root.SetParent(game.transform, false);
            var feel = root.gameObject.AddComponent<SnakeFeel>();
            var flash = UnityEngine.Object.FindAnyObjectByType<MMFlash>();
            Transform scoreTransform = hud.ScoreTransform;

            MMF_Player pickup = Player("Pickup", root);
            Particles(pickup, burst, 26);
            Shake(pickup, .16f, .13f, 34f);
            Freeze(pickup, .028f);
            Bump(pickup, scoreTransform, 8f, .32f, new Vector3(26, 26, 0));

            MMF_Player death = Player("Death", root);
            Shake(death, .5f, .42f, 26f);
            Bump(death, board, 4.5f, .5f, new Vector3(1.1f, 1.1f, 1.1f));
            Freeze(death, .09f);
            Flash(death, flash, Danger.With(.55f), .22f);
            Slow(death, .28f, .45f);

            MMF_Player start = Player("Run start", root);
            Shake(start, .18f, .08f, 22f);
            Bump(start, scoreTransform, 7f, .4f, new Vector3(18, 18, 0));
            // The whole garden settles into place, which makes a restart feel like a fresh deal.
            Bump(start, board, 5.5f, .45f, new Vector3(1.6f, 1.6f, 1.6f));

            MMF_Player best = Player("New best", root);
            Particles(best, burst, 44);
            Shake(best, .3f, .2f, 30f);
            Flash(best, flash, Paper.With(.55f), .28f);
            Bump(best, scoreTransform, 6f, .3f, new Vector3(40, 40, 0));

            MMF_Player turn = Player("Turn", root);
            Shake(turn, .08f, .035f, 40f);

            var bound = new SerializedObject(feel);
            GardenBuilder.Set(bound, "pickup", pickup);
            GardenBuilder.Set(bound, "death", death);
            GardenBuilder.Set(bound, "runStart", start);
            GardenBuilder.Set(bound, "newBest", best);
            GardenBuilder.Set(bound, "turn", turn);
            bound.ApplyModifiedPropertiesWithoutUndo();

            var controller = new SerializedObject(game);
            GardenBuilder.Set(controller, "feel", feel);
            controller.ApplyModifiedPropertiesWithoutUndo();
        }

        private static MMF_Player Player(string name, Transform parent)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            var player = holder.AddComponent<MMF_Player>();
            player.AutoPlayOnEnable = false;
            player.AutoPlayOnStart = false;
            player.CanPlayWhileAlreadyPlaying = true;
            return player;
        }

        private static void Shake(MMF_Player player, float duration, float amplitude, float frequency)
        {
            var shake = (MMF_CameraShake)player.AddFeedback(typeof(MMF_CameraShake));
            shake.Label = "Camera shake";
            shake.CameraShakeProperties = new MMCameraShakeProperties(duration, amplitude, frequency);
        }

        private static void Freeze(MMF_Player player, float duration)
        {
            var freeze = (MMF_FreezeFrame)player.AddFeedback(typeof(MMF_FreezeFrame));
            freeze.Label = "Freeze frame";
            freeze.FreezeFrameDuration = duration;
        }

        private static void Slow(MMF_Player player, float scale, float duration)
        {
            var slow = (MMF_TimescaleModifier)player.AddFeedback(typeof(MMF_TimescaleModifier));
            slow.Label = "Slow motion";
            slow.TimeScale = scale;
            slow.TimeScaleDuration = duration;
            slow.TimeScaleLerp = true;
            slow.TimeScaleLerpSpeed = 6f;
        }

        private static void Flash(MMF_Player player, MMFlash target, Color color, float duration)
        {
            if (target == null) return;
            var flash = (MMF_Flash)player.AddFeedback(typeof(MMF_Flash));
            flash.Label = "Screen flash";
            flash.TargetFlash = target;
            flash.FlashColor = color;
            flash.FlashDuration = duration;
            flash.FlashAlpha = color.a;
        }

        private static void Particles(MMF_Player player, ParticleSystem system, int count)
        {
            var particles = (MMF_Particles)player.AddFeedback(typeof(MMF_Particles));
            particles.Label = "Confetti";
            particles.BoundParticleSystem = system;
            particles.EmitCount = count;
            particles.MoveToPosition = true;
            particles.Mode = MMF_Particles.Modes.Emit;
        }

        private static void Bump(MMF_Player player, Transform target, float frequency, float damping, Vector3 force)
        {
            if (target == null) return;
            var spring = (MMF_ScaleSpring)player.AddFeedback(typeof(MMF_ScaleSpring));
            spring.Label = "Score bump";
            spring.AnimateScaleTarget = target;
            spring.Mode = MMF_ScaleSpring.Modes.Bump;
            spring.FrequencyX = spring.FrequencyY = spring.FrequencyZ = frequency;
            spring.DampingX = spring.DampingY = spring.DampingZ = damping;
            spring.BumpScaleMin = force;
            spring.BumpScaleMax = force;
        }
    }
}
