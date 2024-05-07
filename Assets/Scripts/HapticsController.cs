﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    /// <summary>
    /// Wrapper around OVR's controller haptics that takes in an extra parameter of duration. 
    /// Can be called in other scripts as long as this script is on a gameobject in the scene.
    /// </summary>
    public class HapticsController : MonoBehaviour
    {
        private static HapticsController _hapticsController;
        //singleton instance
        public static HapticsController Instance
        {
            get
            {
                if (!_hapticsController)
                {
                    _hapticsController = FindObjectOfType(typeof(HapticsController)) as HapticsController;
                    if (!_hapticsController)
                    {
                        UnityEngine.Debug.LogError("There needs to be one active HapticsController script on a GameObject in your scene.");
                    }
                }
                return _hapticsController;
            }
        }

        public void Haptics(float frequency, float amplitude, float duration, OVRInput.Controller controller)
        {
            OVRInput.SetControllerVibration(frequency, amplitude, controller);
            StartCoroutine(Duration(duration, controller));
        }
        IEnumerator Duration(float duration, OVRInput.Controller controller)
        {
            yield return new WaitForSecondsRealtime(duration);
            OVRInput.SetControllerVibration(0, 0, controller);
        }

        public void ContinuousHaptics(float frequency, float amplitude, OVRInput.Controller controller)
        {
            OVRInput.SetControllerVibration(frequency, amplitude, controller);
        }

    public void StopHaptics(OVRInput.Controller controller)
        {
            OVRInput.SetControllerVibration(0, 0, controller);
        }
    }