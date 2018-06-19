// based on https://github.com/ms-iot/pid-controller
public class PIDController {
    public float PFactor;
    public float IFactor;
    public float DFactor;
    public float TargetValue;
    public float Value;
    public float LastValue;
    public float ITerm;

    public PIDController(float pFactor, float iFactor, float dFactor) {
        PFactor = pFactor;
        IFactor = iFactor;
        DFactor = dFactor;
    }

    public float UpdateValue(float dt) {
        float error = TargetValue - Value;

        // compute P
        float pTerm = PFactor * error;

        // compute I
        ITerm += IFactor * error * dt;

        // compute D
        float dTerm = DFactor * ((Value - LastValue) / dt);

        LastValue = Value;
        Value = pTerm + ITerm - dTerm;

        return Value;
    }
}
