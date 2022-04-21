using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FitnessTrigger : MonoBehaviour
{
    public int fitnessValue = 100;
    [HideInInspector] public List<Collider> alreadyTriggered = new List<Collider>();


    private void OnTriggerEnter(Collider other)
    {
        if (alreadyTriggered.Contains(other))
        {
            return;
        }

        if (other.gameObject.name.Contains("NetLearner"))
        {
            other.GetComponent<NetLearner>().fitness += fitnessValue;
        }

        alreadyTriggered.Add(other);
    }
}
