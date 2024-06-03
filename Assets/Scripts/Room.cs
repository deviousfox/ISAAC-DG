using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class Room : MonoBehaviour
    {
        public Vector2Int ArrayPos { get; set; }
        [field: SerializeField] public RoomType RoomType { get; private set; }
        [field: SerializeField] public RoomSize RoomSize { get; private set; }
        [field: SerializeField] public Door[] Doors { get; private set; }
        [field: SerializeField] public Door[] DoorsForConnection { get; private set; }

        [Header("ONLY FOR TEST")] [SerializeField]
        private MeshRenderer[] renderer;

        private void OnEnable()
        {
            var color = new Color(Random.value, Random.value, Random.value, 1);
            for (int i = 0; i < renderer.Length; i++)
            {
                renderer[i].material.SetColor("_Color", color);
            }
        }
        
        public bool AvailableConnection(Door door)
        {
            Direction target;
            if (door.Direction == Direction.BackToFront)
                target = Direction.FrontToBack;
            else if (door.Direction == Direction.FrontToBack)
                target = Direction.BackToFront;
            else if (door.Direction == Direction.LeftToRight)
                target = Direction.RightToLeft;
            else
                target = Direction.LeftToRight;

            foreach (var innerDoor in DoorsForConnection)
            {
                if (innerDoor.Direction == target && !innerDoor.IsConnected && !innerDoor.IsDisable)
                    return true;
            }

            return false;
        }

        private bool GetConnectedDoor(Door inDoor, out Door door)
        {
            door = null;
            Direction target;
            if (inDoor.Direction == Direction.BackToFront)
                target = Direction.FrontToBack;
            else if (inDoor.Direction == Direction.FrontToBack)
                target = Direction.BackToFront;
            else if (inDoor.Direction == Direction.LeftToRight)
                target = Direction.RightToLeft;
            else
                target = Direction.LeftToRight;

            foreach (var innerDoor in DoorsForConnection)
            {
                if (innerDoor.Direction == target && !innerDoor.IsConnected && !innerDoor.IsDisable)

                {
                    door = innerDoor;
                    return true;
                }
            }

            return false;
        }

        public Door GetAvailableDoor()
        {
            var door = Doors[Random.Range(0, Doors.Length)];
            if (door.IsDisable)
                return GetAvailableDoor();

            return door;
        }

        public void ConnectToDoor(Door door)
        {
            if (GetConnectedDoor(door, out Door oDoor))
            {
                door.Connect(oDoor);
                oDoor.Connect(door);
            }
        }
    }
}