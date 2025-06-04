using RedLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace AllowBuildInCaves.Components
{
    internal class KeepObjectDeactivated
    {
    }
}

[RegisterTypeInIl2Cpp]
public class KeepObjectDeactivated : MonoBehaviour
{
    private void Start()
    {
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        gameObject.SetActive(false);
    }
}