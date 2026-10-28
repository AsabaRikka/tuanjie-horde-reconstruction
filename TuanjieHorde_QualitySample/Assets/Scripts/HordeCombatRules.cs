using UnityEngine;

public static class HordeCombatRules
{
    public const float ShotRange=20.2f;
    public static Vector3 Pursuit(Vector3 position,Vector3 target,float speed)
    {
        Vector3 delta=target-position;delta.y=0;
        return delta.normalized*Mathf.Min(speed,Mathf.Max(0,delta.magnitude-.65f)*3);
    }
}
