using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class GeneratorV2 : MonoBehaviour
    {
        [SerializeField] private string seed;
        [SerializeField] private int level = 6;
        [SerializeField] private Vector2Int mapSize = new Vector2Int(27, 27);
        [SerializeField] private Vector3 roomSize = new Vector3(15, 0, 9);
        [SerializeField] private Room startRoom;
        [SerializeField] private RoomPool roomPool;
        private List<Room> _spawnedRooms;
        private List<Room> _specialRooms;
        private Room[,] _rooms;
        private int _spawnedRoomsCount;

        private int _iterations;

        private bool _bsR, _trR, _mgR, _ssR, _sR;

        [ContextMenu("New Seed")]
        private string CreateNewSeed()
        {
            var random = new System.Random();
            string[] letters =
            {
                "A", "B", "C", "D", "E", "F", "G", "H", "1", "J", "K", "L", "M",
                "N", "0", "P", "Q", "R", "S", "T", "U", "V", "W", "X", "Y", "Z",
                "0", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"
            };
            string result = String.Empty;
            for (int i = 0; i < 9; i++)
            {
                if (i == 4)
                    result += " ";
                else
                    result += letters[random.Next(0, letters.Length - 1)];
            }

            return result;
        }

        private void Start()
        {
            
            if (string.IsNullOrEmpty(seed))
                seed = CreateNewSeed();
            Generate(seed);
        }

        private void Clear()
        {
            foreach (var room in _spawnedRooms)
            {
                Destroy(room.gameObject);
            }

            foreach (var room in _specialRooms)
            {
                Destroy(room.gameObject);
            }

            _specialRooms.Clear();
            _spawnedRooms.Clear();
            _spawnedRoomsCount = 0;
            _rooms = new Room[mapSize.x, mapSize.y];
            _bsR = false;
            _trR = false;
            _mgR = false;
            _ssR = false;
            _sR = false;
        }

        private void Generate(string rawSeed)
        {
            _iterations++;
            Random.InitState(rawSeed.GetHashCode());

            _rooms = new Room[mapSize.x, mapSize.y];
            _specialRooms = new List<Room>();
            _spawnedRooms = new List<Room>();

            AddStartRoom();
            var roomCount = Random.Range(0, 3) + 5 + Mathf.FloorToInt(level * 2.6f);
            var iterations = 0;
            while (_spawnedRoomsCount < roomCount && iterations < 10000)
            {
                DoStep();
                iterations++;
            }

            SpawnSpecialRooms();
            RemoveUnconnectedDoors();
            if (iterations >= 10)
                return;
            if (!_trR || !_bsR || !_mgR)
            {
                Clear();
                seed = CreateNewSeed();
                Generate(seed);
            }

            RemoveUnconnectedDoors();
        }


        private void SpawnSpecialRooms()
        {
            SpawnBossRoom(roomPool.GetBossRoom(), new[] { RoomType.Secret, RoomType.SuperSecret },
                1,
                1);
            SpawnSpecialRoomV2(roomPool.GetMagazineRoom(), new[] { RoomType.Boss, RoomType.Treasury },
                4,
                1, RoomType.Magazine);
            SpawnSpecialRoomV2(roomPool.GetTreasuryRoom(), new[] { RoomType.Boss, RoomType.Magazine },
                4,
                1, RoomType.Treasury);
        }

        private void SpawnBossRoom(Room room, RoomType[] wrongNeighbour, int maxNeighbour = 1,
            int minNeighbour = 1)
        {
            float range = 0;
            Room farthestRoom = null;
            for (int i = 0; i < _spawnedRooms.Count; i++)
            {
                float dist = Vector2Int.Distance(startRoom.ArrayPos, _spawnedRooms[i].ArrayPos);
                if (dist > range)
                {
                    for (int j = 0; j < _spawnedRooms[i].DoorsForConnection.Length; j++)
                    {
                        var pos = _spawnedRooms[i].ArrayPos + _spawnedRooms[i].DoorsForConnection[j].Offset;
                        if (CellIsValid(pos, maxNeighbour, minNeighbour))
                        {
                            range = dist;
                            farthestRoom = _spawnedRooms[i];
                        }
                    }
                }
            }

            if (farthestRoom != null)
                for (int i = 0; i < farthestRoom.DoorsForConnection.Length; i++)
                {
                    var pos = farthestRoom.ArrayPos + farthestRoom.DoorsForConnection[i].Offset;
                    for (int j = 0; j < room.DoorsForConnection.Length; j++)
                    {
                        if (CellValidConnection(pos, room.DoorsForConnection[j]))
                        {
                            var spawnedRoom = SpawnRoom(room, pos);
                            spawnedRoom.ConnectToDoor(farthestRoom.DoorsForConnection[i]);
                            _spawnedRooms.Add(spawnedRoom);
                            _bsR = true;
                            _specialRooms.Add(spawnedRoom);
                            return;
                        }
                    }
                }
            else
            {
                SpawnBossRoom(room, wrongNeighbour, maxNeighbour + 1, minNeighbour);
            }
        }

        private void SpawnSpecialRoomV2(Room room, RoomType[] wrongNeighbour, int maxNeighbour = 1,
            int minNeighbour = 1, RoomType roomType = RoomType.Common)
        {
            for (int i = 0; i < _spawnedRooms.Count; i++)
            {
                for (int j = 0; j < _spawnedRooms[i].DoorsForConnection.Length; j++)
                {
                    var pos = _spawnedRooms[i].ArrayPos + _spawnedRooms[i].DoorsForConnection[j].Offset;
                    if (CellIsValid(pos, maxNeighbour, minNeighbour))
                    {
                        bool canSpawn = true;
                        for (int k = 0; k < wrongNeighbour.Length; k++)
                        {
                            if (!NeighbourIsNot(pos, wrongNeighbour[k]))
                            {
                                canSpawn = false;
                                break;
                            }
                        }

                        if (!canSpawn)
                            continue;
                        for (int k = 0; k < room.DoorsForConnection.Length; k++)
                        {
                            if (CellValidConnection(pos, room.DoorsForConnection[k]))
                            {
                                var spawnedRoom = SpawnRoom(room, pos);
                                spawnedRoom.ConnectToDoor(_spawnedRooms[i].DoorsForConnection[j]);
                                _spawnedRooms.Add(spawnedRoom);
                                if (roomType == RoomType.Magazine)
                                    _mgR = true;
                                if (roomType == RoomType.Treasury)
                                    _trR = true;
                                if (roomType == RoomType.Secret)
                                    _sR = true;
                                if (roomType == RoomType.SuperSecret)
                                    _ssR = true;
                                return;
                            }
                        }
                    }
                }
            }
        }

        private void AddStartRoom()
        {
            var spawnRoom = Instantiate(startRoom, Vector3.zero, Quaternion.identity);
            spawnRoom.gameObject.name = "SPAWN_ROOM";
            spawnRoom.ArrayPos = new Vector2Int(mapSize.x / 2, mapSize.y / 2);
            _rooms[mapSize.x / 2, mapSize.y / 2] = spawnRoom;
            _spawnedRooms.Add(spawnRoom);
        }


        private void RemoveUnconnectedDoors()
        {
            var doors = FindObjectsByType<Door>(FindObjectsInactive.Include, 0);
            for (int i = 0; i < doors.Length; i++)
            {
                if (doors[i] != null)
                {
                    if (doors[i].IsDisable || !doors[i].IsConnected)
                        doors[i].Disable();
                }
            }
        }

        private void DoStep()
        {
            List<Room> cached = new List<Room>();
            for (int i = 0; i < _spawnedRooms.Count; i++)
            {
                var door = _spawnedRooms[i].GetAvailableDoor();
                if (CellIsValid(_spawnedRooms[i].ArrayPos + door.Offset, 1))
                {
                    var room = roomPool.GetAvailableRoom(door);
                    if (CellsIsEmpty(_spawnedRooms[i].ArrayPos + door.Offset, room.RoomSize))
                    {
                        var pos = _spawnedRooms[i].ArrayPos + door.Offset;
                        var spawnedRoom = SpawnRoom(room, pos);
                        spawnedRoom.ConnectToDoor(door);
                        cached.Add(spawnedRoom);
                        _spawnedRoomsCount++;
                    }
                }
            }

            for (int i = 0; i < cached.Count; i++)
            {
                _spawnedRooms.Add(cached[i]);
            }
        }

        private Room SpawnRoom(Room room, Vector2Int pos)
        {
            var spawnedRoom = Instantiate(room);
            spawnedRoom.ArrayPos = pos;
            FillArrayAtRoom(spawnedRoom, spawnedRoom.RoomSize);
            pos.x -= mapSize.x / 2;
            pos.y -= mapSize.y / 2;
            spawnedRoom.transform.position = new Vector3(pos.x * roomSize.x, 0, pos.y * roomSize.z);
            return spawnedRoom;
        }

        private bool NeighbourIsNot(Vector2Int pos, RoomType roomType)
        {
            if (!IncludeArray(pos))
                return false;
            if (_rooms[pos.x, pos.y] != null)
                return false;
            bool connect10 = false;
            bool connect01 = false;
            bool connect_10 = false;
            bool connect0_1 = false;
            if (_rooms[pos.x + 1, pos.y] == null)
                connect10 = true;
            else if (_rooms[pos.x + 1, pos.y].RoomType != roomType)
                connect10 = true;
            if (_rooms[pos.x, pos.y + 1] == null)
                connect01 = true;
            else if (_rooms[pos.x, pos.y + 1].RoomType != roomType)
                connect01 = true;
            if (_rooms[pos.x - 1, pos.y] == null)
                connect_10 = true;
            else if (_rooms[pos.x - 1, pos.y].RoomType != roomType)
                connect_10 = true;
            if (_rooms[pos.x, pos.y - 1] == null)
                connect0_1 = true;
            else if (_rooms[pos.x, pos.y - 1].RoomType != roomType)
                connect0_1 = true;
            return connect10 && connect0_1 && connect_10 && connect01;
        }

        private bool IncludeArray(Vector2Int pos)
        {
            if (pos.x < 0 || pos.x > _rooms.GetLength(0) || pos.y < 0 || pos.y > _rooms.GetLength(1))
                return false;
            if (pos.x - 1 < 0 || pos.x + 1 > _rooms.GetLength(0) || pos.y - 1 < 0 || pos.y + 1 > _rooms.GetLength(1))
                return false;
            if (pos.x - 2 < 0 || pos.x + 2 > _rooms.GetLength(0) || pos.y - 2 < 0 || pos.y + 2 > _rooms.GetLength(1))
                return false;

            return true;
        }

        private bool CellValidConnection(Vector2Int pos, Door door)
        {
            bool connect10 = false;
            bool connect01 = false;
            bool connect_10 = false;
            bool connect0_1 = false;
            if (!IncludeArray(pos))
                return false;
            if (_rooms[pos.x, pos.y])
                return false;
            if (_rooms[pos.x + 1, pos.y])
                connect10 = _rooms[pos.x + 1, pos.y].AvailableConnection(door);
            if (_rooms[pos.x - 1, pos.y])
                connect_10 = _rooms[pos.x - 1, pos.y].AvailableConnection(door);
            if (_rooms[pos.x, pos.y + 1])
                connect01 = _rooms[pos.x, pos.y + 1].AvailableConnection(door);
            if (_rooms[pos.x, pos.y - 1])
                connect0_1 = _rooms[pos.x, pos.y - 1].AvailableConnection(door);


            return connect10 || connect0_1 || connect01 || connect_10;
        }

        private bool CellIsValid(Vector2Int pos, int maxNeighbour, int minNeighbour = 1)
        {
            int neighbour = 0;

            if (!IncludeArray(pos))
                return false;

            if (_rooms[pos.x, pos.y] != null)
                return false;
            if (_rooms[pos.x + 1, pos.y] != null)
                neighbour++;
            if (_rooms[pos.x - 1, pos.y] != null)
                neighbour++;
            if (_rooms[pos.x, pos.y + 1] != null)
                neighbour++;
            if (_rooms[pos.x, pos.y - 1] != null)
                neighbour++;
            if (_rooms[pos.x, pos.x] == false && neighbour <= maxNeighbour && neighbour >= minNeighbour)
                return true;
            return false;
        }

        private bool CellsIsEmpty(Vector2Int pos, RoomSize size)
        {
            var arrayPos = pos;

            switch (size)
            {
                case RoomSize.Room1x1:
                    if (_rooms[arrayPos.x, arrayPos.y] == false)
                        return true;
                    return false;

                case RoomSize.Room2x2:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x, arrayPos.y + 1] == false &&
                        _rooms[arrayPos.x + 1, arrayPos.y] == false &&
                        _rooms[arrayPos.x + 1, arrayPos.y + 1] == false)
                        return true;
                    return false;

                case RoomSize.Room2x1:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x + 1, arrayPos.y] == false
                       ) return true;
                    return false;

                case RoomSize.Room1x2:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x, arrayPos.y + 1] == false)
                        return true;
                    return false;

                case RoomSize.RoomL:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x, arrayPos.y + 1] == false &&
                        _rooms[arrayPos.x + 1, arrayPos.y] == false)
                        return true;
                    return false;

                case RoomSize.RoomL90:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x + 1, arrayPos.y] == false &&
                        _rooms[arrayPos.x, arrayPos.y - 1] == false)
                        return true;
                    return false;

                case RoomSize.RoomL180:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x - 1, arrayPos.y] == false &&
                        _rooms[arrayPos.x, arrayPos.y - 1] == false)
                        return true;
                    return false;

                case RoomSize.RoomL270:
                    if (_rooms[arrayPos.x, arrayPos.y] == false &&
                        _rooms[arrayPos.x, arrayPos.y + 1] == false &&
                        _rooms[arrayPos.x - 1, arrayPos.y] == false
                       ) return true;
                    return false;
            }

            return false;
        }

        private void FillArrayAtRoom(Room room, RoomSize size)
        {
            var roomArrayPos = room.ArrayPos;
            switch (size)
            {
                case RoomSize.Room1x1:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    break;
                case RoomSize.Room2x2:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x, roomArrayPos.y + 1] = room;
                    _rooms[roomArrayPos.x + 1, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x + 1, roomArrayPos.y + 1] = room;
                    break;
                case RoomSize.Room2x1:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x + 1, roomArrayPos.y] = room;
                    break;
                case RoomSize.Room1x2:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x, roomArrayPos.y + 1] = room;
                    break;
                case RoomSize.RoomL:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x + 1, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x, roomArrayPos.y + 1] = room;
                    break;
                case RoomSize.RoomL90:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x + 1, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x, roomArrayPos.y - 1] = room;
                    break;
                case RoomSize.RoomL180:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x - 1, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x, roomArrayPos.y - 1] = room;
                    break;
                case RoomSize.RoomL270:
                    _rooms[roomArrayPos.x, roomArrayPos.y] = room;
                    _rooms[roomArrayPos.x, roomArrayPos.y + 1] = room;
                    _rooms[roomArrayPos.x - 1, roomArrayPos.y] = room;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(size), size, null);
            }
        }

        private void OnDrawGizmos()
        {
            if (_rooms == null)
                return;
            for (int i = 0; i < mapSize.x; i++)
            {
                for (int j = 0; j < mapSize.y; j++)
                {
                    Gizmos.color = _rooms[i, j] == false ? new Color(0f, 1f, 0f, 0.6f) : new Color(1f, 0f, 0f, 0.6f);
                    Gizmos.DrawCube(new Vector3((i - mapSize.x / 2) * roomSize.x, 2, (j - mapSize.y / 2) * roomSize.z),
                        roomSize);
                }
            }
        }
    }
}