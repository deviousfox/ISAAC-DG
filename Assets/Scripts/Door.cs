using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class Door : MonoBehaviour
    {
        [field: SerializeField] public Direction Direction { get; private set; }
        [field: SerializeField] public Vector2Int Offset { get; private set; }

        [field: SerializeField] public bool IsDisable { get; private set; }
        [field: SerializeField] public Door ConnectedDoor { get; private set; }
        public bool IsConnected
        {
            get { return ConnectedDoor != null; }
            private set { }
        }

        public void Disable()
        {
            IsDisable = true;
            gameObject.SetActive(false);
        }

        public void Enable()
        {
            IsDisable = false;
            gameObject.SetActive(true);
        }

        public void Connect(Door p)
        {
            IsConnected = true;
            ConnectedDoor = p;
            Enable();
        }
        
        private void OnDrawGizmos()
        {
            Gizmos.color = IsConnected ? Color.green : Color.red;

            Gizmos.DrawSphere(transform.position + new Vector3(0, 5), 0.25f);
        }
    }
}