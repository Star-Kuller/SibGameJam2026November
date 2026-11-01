using UnityEngine;

namespace _Game.Code.Features.Presentation.View
{
    public static class CharacterDeathPose
    {
        public static bool Render(
            Animator animator,
            Transform visual,
            SkinnedMeshRenderer renderer,
            Vector3 originalPosition,
            float progress,
            float groundHeight)
        {
            if (animator == null || !animator.HasState(0, Animator.StringToHash("Death")))
            {
                return false;
            }

            visual.localPosition = originalPosition;
            animator.Play("Death", 0, Mathf.Min(0.9999f, progress));
            animator.speed = 0;
            animator.Update(0);

            if (renderer != null && progress > 0.6f)
            {
                var lowestBone = float.PositiveInfinity;

                foreach (var bone in renderer.bones)
                {
                    if (bone != null)
                    {
                        lowestBone = Mathf.Min(lowestBone, bone.position.y);
                    }
                }

                if (!float.IsInfinity(lowestBone))
                {
                    var blend = Mathf.SmoothStep(0, 1, (progress - 0.6f) / 0.3f);
                    var offset = lowestBone - groundHeight - 0.08f;
                    visual.position -= Vector3.up * offset * blend;
                }
            }

            return true;
        }
    }
}
