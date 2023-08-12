using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityCopilot.Models
{
    [System.Serializable]
    public class User
    {
        public string username;
        public string email;
        public string full_name;
        public bool? disabled;
        public int credits;
    }
}
