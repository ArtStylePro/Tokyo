﻿using Assets.Scripts.Helpers;
using UnityEngine;

namespace Assets.Scripts.Arena
{
    public class GridCell : MonoBehaviour
    {
        [SerializeField] public IntVector2 GridPostion;
        public Deployable InCellObject;
        public bool IsEmpty = true;

        public void GotHit()
        {
            
        }
    }
}