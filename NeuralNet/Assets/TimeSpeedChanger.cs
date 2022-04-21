using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSpeedChanger : MonoBehaviour
{
    public void AdjustSpeed()
    {
        Time.timeScale = GetComponent<Slider>().value;
    }
}
