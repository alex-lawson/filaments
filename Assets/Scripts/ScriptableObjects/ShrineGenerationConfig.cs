using UnityEngine;

[CreateAssetMenu(fileName = "ShrineGenerationConfig", menuName = "Shrine Generation Config")]
public class ShrineGenerationConfig : ScriptableObject {
    public float FinalScale = 1;
    public float AccentChanceMid;
    public float AccentChanceEnd;
    public IntRange RotationalSymmetry;
    public FloatRange Radius;
    public IntRange TierCount;
    public FloatRange TierHeight;
    public float OffsetTierChance;
    public FloatRange OffsetTierHeight;
    public float GlobalTwistChance;
    public float SegmentTwistChance;
    public FloatRange TwistAngle;
    public float ExtrudeChance;
    public IntRange ExtrudeSteps;
    public FloatRange BranchSegmentLength;
    public FloatRange BranchSegmentScale;
    public FloatRange BranchVerticalBias;
    public float BranchDirectionNoise;
    public float PeakChance;
    public FloatRange PeakSize;
    public FloatRange BranchPeakSize;
}

