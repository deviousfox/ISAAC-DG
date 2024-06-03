using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace
{
    public class RoomPool : MonoBehaviour
    {
        [SerializeField] private Room[] rooms;
        [SerializeField] private Room[] secretRoom;
        [SerializeField] private Room[] superSecretRoom;
        [SerializeField] private Room[] bossRooms;
        [SerializeField] private Room[] treasuryRooms;
        [SerializeField] private Room[] magazineRooms;


        public Room GetSecretRoom() => secretRoom[Random.Range(0, secretRoom.Length)];
        public Room GetSuperSecretRoom() => superSecretRoom[Random.Range(0, superSecretRoom.Length)];
        public Room GetBossRoom() => bossRooms[Random.Range(0, bossRooms.Length)];
        public Room GetTreasuryRoom() => treasuryRooms[Random.Range(0, treasuryRooms.Length)];
        public Room GetMagazineRoom() => magazineRooms[Random.Range(0, magazineRooms.Length)];
        
        public Room GetAvailableRoom(Door door)
        {
            var current = rooms[Random.Range(0, rooms.Length)];
            if (current.RoomSize != RoomSize.Room1x1)
                if ((current.RoomSize != RoomSize.Room1x2 || current.RoomSize != RoomSize.Room2x1) &&
                    Random.value < 0.15f)
                    if (Random.value < 0.15)
                        return GetAvailableRoom(door);
            if (current.AvailableConnection(door))
                return current;

            return GetAvailableRoom(door);
        }
    }
}