using DelaunayVoronoi;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public static class GeneratorDebugSettings {
    public static bool DebugStartingRoom = false;
    public static bool DebugGraph = false;
    public static bool DebugOverlays = false;
    public static bool DebugHallways = false;
    public static bool DebugEntranceExits = false;
}

public class DungeonGenerator : ScriptableObject {
    /*
     * Find how big it is by querying the object dimensions, organize that information into an Object.
     * Use the set of information to randomly generate a map with random seeding.
     * 
     * Types of nodes:
     *  Rooms
     *   1 Entrance
     *   1 Exit
     *  Hallways
     *   Almost like a voxel grid.
     *   Elbow piece.
     *   Hallway piece.
     *   End piece.
     *   Three way intersection.
     *   Four way intersection.
     *  
     *  Algorithm:
     *   First generate the rooms far enough away from each other.
     *   Delaunay Triangulation.
     *   Build a graph.
     *   Generate voxel hallways in between exits and entrances.
     *   Align things to a grid - entrance / exit values need to be divisible by some number (matching our TileSize).
     */

    public float minX;
    public float minY;
    public float maxX;
    public float maxY;

    public int hallwaySize;

    public void Generate() {
        hallwaySize = 10;

        var rooms = GenerateRooms();
        var startingRoom = GenerateConnections(rooms);
        var hallwayMap = GenerateHallwayMap(startingRoom, rooms);
        if (GeneratorDebugSettings.DebugHallways) {
            DebugHallwayMap(hallwayMap);
        }

        SetHallwayTypes(hallwayMap);
        GenerateHallways(hallwayMap);
    }

    public List<Room> GenerateRooms() {
        var roomTypes = GetRooms();

        var rooms = new List<Room>();

        var roomCount = 20;

        var mapWidth = 10;
        var mapHeight = 10;

        // Generate the rooms.
        for (var i = 0; i < roomCount; i++) {
            var roomType = roomTypes[Random.Range(0, roomTypes.Count - 1)];
            var room = new Room(roomType.resource, roomType.width, roomType.height);
            room.position.x = Random.Range(-mapWidth, mapWidth);
            room.position.y = Random.Range(-mapHeight, mapHeight);
            rooms.Add(room);
        }

        // Spread the rooms out.
        var spreading = true;
        var spreadSpeed = 1f;
        var minimumDistance = 150;
        var movements = new List<System.Tuple<Vector2, Room>>();
        while (spreading) {
            spreading = false;
            for (var i = 0; i < rooms.Count; i++) {
                var movingRoom = rooms[i];
                var vector = new Vector2();
                for (var j = 0; j < rooms.Count; j++) {
                    if (i == j) {
                        continue;
                    }
                    var comparisonRoom = rooms[j];
                    if (Vector2.Distance(comparisonRoom.position, movingRoom.position) < minimumDistance) {
                        // Get the vector we'd be at moving away from the room.
                        var endPosition = Vector2.MoveTowards(
                            movingRoom.position, comparisonRoom.position, -spreadSpeed);
                        // Subtract it from our current position to get the movement vector to get there.
                        var movement = endPosition - movingRoom.position;
                        // Sum up all the movement vectors.
                        vector += movement;
                    }
                }

                if (vector.x != 0 || vector.y != 0) {
                    movements.Add(new System.Tuple<Vector2, Room>(vector, movingRoom));
                }
            }

            // Apply all the movements.
            foreach (var movement in movements) {
                var vector = movement.Item1;
                var room = movement.Item2;
                room.position += vector;
            }

            // If no movements were necessary, we're done.
            spreading = movements.Count > 0;

            movements.Clear();
        }

        foreach (var room in rooms) {
            var instance = Instantiate(room.resource,
                new Vector3(
                    room.position.x,
                    0,
                    room.position.y),
                Quaternion.identity);
            room.instance = instance;
        }

        return rooms;
    }

    public Room GenerateConnections(List<Room> rooms) {
        var triangulator = new DelaunayTriangulator();
        var roomMidpoints = new List<DelaunayVoronoi.Point>();
        var minX = float.MaxValue;
        var minY = float.MaxValue;
        var maxX = float.MinValue;
        var maxY = float.MinValue;
        foreach (var room in rooms) {
            var x = room.position.x;
            var y = room.position.y;

            minX = Mathf.Min(x, minX);
            minY = Mathf.Min(y, minY);
            maxX = Mathf.Max(x, maxX);
            maxY = Mathf.Max(y, maxY);

            roomMidpoints.Add(new DelaunayVoronoi.Point(room.position.x, room.position.y));
        }

        // Add buffer to the sides.
        minX -= 100;
        minY -= 100;
        maxX += 100;
        maxY += 100;
        minX = Mathf.CeilToInt(minX);
        minY = Mathf.CeilToInt(minY);
        maxX = Mathf.CeilToInt(maxX);
        maxY = Mathf.CeilToInt(maxY);

        this.minX = minX;
        this.minY = minY;
        this.maxX = maxX;
        this.maxY = maxY;

        var point0 = new DelaunayVoronoi.Point(minX, minY);
        var point1 = new DelaunayVoronoi.Point(minX, maxY);
        var point2 = new DelaunayVoronoi.Point(maxX, maxY);
        var point3 = new DelaunayVoronoi.Point(maxX, minY);
        var borderTriangle1 = new Triangle(point0, point1, point2);
        var borderTriangle2 = new Triangle(point0, point2, point3);
        triangulator.border = new List<Triangle>() { borderTriangle1, borderTriangle2 };

        var delaunayTriangles = new List<Triangle>(triangulator.BowyerWatson(roomMidpoints));

        foreach (var triangle in delaunayTriangles) {
            // Check if this is actually part of the delaunay triangulation, or if it's a bordering region.
            var valid = true;
            foreach (var vertex in triangle.Vertices) {
                if (vertex.X == minX || vertex.X == maxX || vertex.Y == minY || vertex.Y == maxY) {
                    valid = false;
                    break;
                }
            }

            if (!valid) {
                continue;
            }

            var edges = new List<System.Tuple<DelaunayVoronoi.Point, DelaunayVoronoi.Point>>() {
                new System.Tuple<DelaunayVoronoi.Point, DelaunayVoronoi.Point>(triangle.Vertices[0], triangle.Vertices[1]),
                new System.Tuple<DelaunayVoronoi.Point, DelaunayVoronoi.Point>(triangle.Vertices[1], triangle.Vertices[2]),
                new System.Tuple<DelaunayVoronoi.Point, DelaunayVoronoi.Point>(triangle.Vertices[2], triangle.Vertices[0]),
            };

            foreach (var edge in edges) {
                var p1 = new Vector2((float)edge.Item1.X, (float)edge.Item1.Y);
                var p2 = new Vector2((float)edge.Item2.X, (float)edge.Item2.Y);

                var r1 = GetRoomByMidpoint(rooms, p1);
                var r2 = GetRoomByMidpoint(rooms, p2);
                if (r1 == null || r2 == null) {
                    continue;
                }
                r1.connectedRooms.Add(r2);
                r2.connectedRooms.Add(r1);
            }
        }

        Room startingRoom = null;
        var startingRoomDistanceSq = float.MaxValue;
        foreach (var room in rooms) {
            var distanceSq = room.position.x * room.position.x + room.position.y * room.position.y;
            if (distanceSq < startingRoomDistanceSq) {
                startingRoomDistanceSq = distanceSq;
                startingRoom = room;
            }
        }

        if (GeneratorDebugSettings.DebugStartingRoom) {
            Debug.DrawLine(new Vector3(startingRoom.position.x, 0, startingRoom.position.y),
                new Vector3(startingRoom.position.x, 5, startingRoom.position.y),
                Color.yellow, 100000f);
        }

        // Center room has no entrance; you spawn there.
        startingRoom.DeleteConnectionsIntoSelf();

        // Remove as many connections as possible while preventing islands.
        startingRoom.DeleteAsManyConnectionsAsPossible();

        // Remove any loops while preventing islands.
        startingRoom.DeleteLoops();

        // TODO: Add some more connections randomly.

        if (GeneratorDebugSettings.DebugGraph) {
            startingRoom.DebugGraph();
        }

        foreach (var room in rooms) {
            room.AlignToHallwayGrid(hallwaySize, minX, minY);
            room.FinalizePosition();
        }

        return startingRoom;
    }

    public HallwayType[,] GenerateHallwayMap(Room startingRoom, List<Room> rooms) {
        var width = Mathf.CeilToInt(maxX - minX) / hallwaySize;
        var height = Mathf.CeilToInt(maxY - minY) / hallwaySize;

        HallwayType[,] map = new HallwayType[width, height];
        for (var x = 0; x < width; x++) {
            for (var y = 0; y < height; y++) {
                var worldX = x * hallwaySize + minX;
                var worldY = y * hallwaySize + minY;

                // Check if this hallway position would overlap with a room.
                var overlaps = false;
                foreach (var room in rooms) {
                    var left1 = room.position.x - room.width / 2f;
                    var left2 = worldX - hallwaySize / 2f;
                    var right1 = room.position.x + room.width / 2f;
                    var right2 = worldX + hallwaySize / 2f;
                    var top1 = room.position.y - room.height / 2f;
                    var top2 = worldY - hallwaySize / 2f;
                    var bottom1 = room.position.y + room.height / 2f;
                    var bottom2 = worldY + hallwaySize / 2f;
                    var epsilon = 0.02;
                    if (right2 - left1 > epsilon && right1 - left2 > epsilon &&
                        bottom2 - top1 > epsilon && bottom1 - top2 > epsilon) {
                        overlaps = true;
                    }

                    if (overlaps) {
                        break;
                    }
                }

                if (overlaps) {
                    map[x, y] = HallwayType.Invalid;
                    if (GeneratorDebugSettings.DebugOverlays) {
                        Debug.DrawLine(
                            new Vector3(worldX - hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                            new Vector3(worldX + hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                            Color.black, 1000000f);
                        Debug.DrawLine(
                            new Vector3(worldX - hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                            new Vector3(worldX + hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                            Color.black, 1000000f);
                    }
                } else {
                    map[x, y] = HallwayType.None;
                    if (GeneratorDebugSettings.DebugOverlays) {
                        Debug.DrawLine(
                            new Vector3(worldX - hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                            new Vector3(worldX + hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                            Color.white, 1000000f);
                        Debug.DrawLine(
                            new Vector3(worldX - hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                            new Vector3(worldX + hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                            Color.white, 1000000f);
                    }
                }
            }
        }
        var hallwayGenerator = new HallwayGenerator(map);

        var queue = new Queue<Room>();
        queue.Enqueue(startingRoom);
        var seen = new HashSet<Room>(queue);
        while (queue.Count > 0) {
            var room = queue.Dequeue();
            foreach (var connectedRoom in room.connectedRooms) {
                // TODO: This hardcodes the room rotation.
                hallwayGenerator.FillShortestHallwayPath(
                    Mathf.CeilToInt((room.exit.x - minX - hallwaySize / 2f) / hallwaySize),
                    Mathf.CeilToInt((room.exit.y - minY) / hallwaySize),
                    Mathf.CeilToInt((connectedRoom.entrance.x - minX) / hallwaySize),
                    Mathf.CeilToInt((connectedRoom.entrance.y - minY) / hallwaySize));

                if (!seen.Contains(connectedRoom)) {
                    queue.Enqueue(connectedRoom);
                    seen.Add(connectedRoom);
                }
            }
        }

        return map;
    }

    public void SetHallwayTypes(HallwayType[,] hallwayMap) {
        for (int x = 0; x < hallwayMap.GetLength(0); x++) {
            for (int y = 0; y < hallwayMap.GetLength(1); y++) {
                if (hallwayMap[x, y] != HallwayType.FourWay) {
                    continue;
                }

                bool left = x > 0 && hallwayMap[x - 1, y] != HallwayType.None && hallwayMap[x - 1, y] != HallwayType.Invalid;
                bool right = x + 1 < hallwayMap.GetLength(0) && hallwayMap[x + 1, y] != HallwayType.None && hallwayMap[x + 1, y] != HallwayType.Invalid;
                bool top = y > 0 && hallwayMap[x, y - 1] != HallwayType.None && hallwayMap[x, y - 1] != HallwayType.Invalid;
                bool bottom = y + 1 < hallwayMap.GetLength(1) && hallwayMap[x, y + 1] != HallwayType.None && hallwayMap[x, y + 1] != HallwayType.Invalid;

                if (left && right && top && bottom) {
                    hallwayMap[x, y] = HallwayType.FourWay;
                } else if (left && top && right) {
                    hallwayMap[x, y] = HallwayType.ThreeWayBottom;
                } else if (bottom && top && right) {
                    hallwayMap[x, y] = HallwayType.ThreeWayLeft;
                } else if (bottom && top && left) {
                    hallwayMap[x, y] = HallwayType.ThreeWayRight;
                } else if (bottom && right && left) {
                    hallwayMap[x, y] = HallwayType.ThreeWayTop;
                } else if (bottom && left) {
                    hallwayMap[x, y] = HallwayType.TurnBottomLeft;
                } else if (left && top) {
                    hallwayMap[x, y] = HallwayType.TurnLeftTop;
                } else if (right && bottom) {
                    hallwayMap[x, y] = HallwayType.TurnRightBottom;
                } else if (top && right) {
                    hallwayMap[x, y] = HallwayType.TurnTopRight;
                } else if (top || bottom) {
                    hallwayMap[x, y] = HallwayType.StraightVertical;
                } else if (left || right) {
                    hallwayMap[x, y] = HallwayType.StraightHorizontal;
                }
            }
        }
    }

    public void GenerateHallways(HallwayType[,] hallwayMap) {
        var hallwayTurn = Resources.Load("Prefabs\\Hallways\\HallwayTurn");
        var hallwayStraight = Resources.Load("Prefabs\\Hallways\\HallwayStraight");
        var hallwayThreeWay = Resources.Load("Prefabs\\Hallways\\HallwayThreeWay");
        var hallwayFourWay = Resources.Load("Prefabs\\Hallways\\HallwayFourWay");

        for (int x = 0; x < hallwayMap.GetLength(0); x++) {
            for (int y = 0; y < hallwayMap.GetLength(1); y++) {
                var hallway = hallwayMap[x, y];
                var position = new Vector3(x * hallwaySize + minX, 0, y * hallwaySize + minY);
                switch (hallway) {
                    case HallwayType.Invalid:
                        break;
                    case HallwayType.None:
                        break;
                    case HallwayType.FourWay:
                        Instantiate(hallwayFourWay, position,
                            Quaternion.identity);
                        break;
                    case HallwayType.ThreeWayRight:
                        Instantiate(hallwayThreeWay, position,
                            Quaternion.Euler(0, 0, 0));
                        break;
                    case HallwayType.ThreeWayBottom:
                        Instantiate(hallwayThreeWay, position,
                            Quaternion.Euler(0, 270, 0));
                        break;
                    case HallwayType.ThreeWayLeft:
                        Instantiate(hallwayThreeWay, position,
                            Quaternion.Euler(0, 180, 0));
                        break;
                    case HallwayType.ThreeWayTop:
                        Instantiate(hallwayThreeWay, position,
                            Quaternion.Euler(0, 90, 0));
                        break;
                    case HallwayType.TurnTopRight:
                        Instantiate(hallwayTurn, position,
                            Quaternion.Euler(0, 270, 0));
                        break;
                    case HallwayType.TurnRightBottom:
                        Instantiate(hallwayTurn, position,
                            Quaternion.Euler(0, 180, 0));
                        break;
                    case HallwayType.TurnBottomLeft:
                        Instantiate(hallwayTurn, position,
                            Quaternion.Euler(0, 90, 0));
                        break;
                    case HallwayType.TurnLeftTop:
                        Instantiate(hallwayTurn, position,
                            Quaternion.Euler(0, 0, 0));
                        break;
                    case HallwayType.StraightVertical:
                        Instantiate(hallwayStraight, position,
                            Quaternion.Euler(0, 0, 0));
                        break;
                    case HallwayType.StraightHorizontal:
                        Instantiate(hallwayStraight, position,
                            Quaternion.Euler(0, 90, 0));
                        break;
                }
            }
        }
    }

    public List<Room> GetRooms() {
        var root = "Prefabs\\Rooms\\";
        var rooms = new List<Room>();

        foreach (var resource in Resources.LoadAll(root)) {
            var room = (GameObject)resource;
            var mesh = room.GetComponentInChildren<MeshFilter>().sharedMesh;
            rooms.Add(new Room(room,
                room.transform.localScale.x * mesh.bounds.size.x,
                room.transform.localScale.z * mesh.bounds.size.z));
        }

        return rooms;
    }

    public Room GetRoomByMidpoint(List<Room> rooms, Vector2 position) {
        foreach (var room in rooms) {
            if (room.position.x == position.x && room.position.y == position.y) {
                return room;
            }
        }
        return null;
    }

    public void DebugHallwayMap(HallwayType[,] hallwayMap) {
        for (int x = 0; x < hallwayMap.GetLength(0); x++) {
            for (int y = 0; y < hallwayMap.GetLength(1); y++) {
                var hallway = hallwayMap[x, y];
                switch (hallway) {
                    case HallwayType.Invalid:
                        break;
                    case HallwayType.None:
                        break;
                    case HallwayType.FourWay:
                    case HallwayType.ThreeWayBottom:
                    case HallwayType.ThreeWayLeft:
                    case HallwayType.ThreeWayRight:
                    case HallwayType.ThreeWayTop:
                    case HallwayType.TurnBottomLeft:
                    case HallwayType.TurnLeftTop:
                    case HallwayType.TurnRightBottom:
                    case HallwayType.TurnTopRight:
                    case HallwayType.StraightVertical:
                    case HallwayType.StraightHorizontal:
                        Debug.DrawLine(
                            new Vector3(x * hallwaySize + minX, 0, y * hallwaySize + minY),
                            new Vector3(x * hallwaySize + minX, 5, y * hallwaySize + minY),
                            Color.green, 1000000f);
                        break;
                }
            }
        }
    }
}

public class Room {
    public GameObject resource;
    public GameObject instance;
    public float width;
    public float height;
    public Vector2 position;
    public HashSet<Room> connectedRooms;

    public Point entranceRelative;
    public Point exitRelative;

    public Point entrance;
    public Point exit;

    public Room(GameObject resource, float width, float height) {
        this.resource = resource;
        this.width = width;
        this.height = height;
        connectedRooms = new HashSet<Room>();

        var entranceChild = resource.transform.Find("Entrance");
        var exitChild = resource.transform.Find("Exit");
        entranceRelative = new Point(
            entranceChild.localPosition.x * resource.transform.localScale.x,
            entranceChild.localPosition.z * resource.transform.localScale.z);
        exitRelative = new Point(
            exitChild.localPosition.x * resource.transform.localScale.x,
            exitChild.localPosition.z * resource.transform.localScale.z);
    }

    public void FinalizePosition() {
        entrance = new Point(position.x + entranceRelative.x, position.y + entranceRelative.y);
        exit = new Point(position.x + exitRelative.x, position.y + exitRelative.y);
    }

    public void DeleteConnectionsIntoSelf() {
        foreach (var room in connectedRooms) {
            room.connectedRooms.Remove(this);
        }
    }

    public int ConnectedRoomCount() {
        int count = 0;
        var queue = new Queue<Room>();
        queue.Enqueue(this);
        var seen = new HashSet<Room>(queue);
        while (queue.Count > 0) {
            count++;

            var room = queue.Dequeue();
            foreach (var connectedRoom in room.connectedRooms) {
                if (!seen.Contains(connectedRoom)) {
                    queue.Enqueue(connectedRoom);
                    seen.Add(connectedRoom);
                }
            }
        }

        return count;
    }

    public void DeleteAsManyConnectionsAsPossible() {
        int roomCount = ConnectedRoomCount();
        var queue = new Queue<Room>();
        queue.Enqueue(this);
        var seen = new HashSet<Room>(queue);
        while (queue.Count > 0) {
            var room = queue.Dequeue();
            foreach (var connectedRoom in new List<Room>(room.connectedRooms)) {
                room.connectedRooms.Remove(connectedRoom);
                if (ConnectedRoomCount() < roomCount) {
                    // Removing this connection has led to islands in our graphs, so we can't do it.
                    room.connectedRooms.Add(connectedRoom);
                }
            }

            // Go through the remaining connections and enqueue them.
            foreach (var connectedRoom in room.connectedRooms) {
                if (!seen.Contains(connectedRoom)) {
                    queue.Enqueue(connectedRoom);
                    seen.Add(connectedRoom);
                }
            }
        }
    }

    public void DeleteLoops() {
        int roomCount = ConnectedRoomCount();
        var queue = new Queue<Room>();
        queue.Enqueue(this);
        var seen = new HashSet<Room>(queue);
        while (queue.Count > 0) {
            var room = queue.Dequeue();
            foreach (var connectedRoom in new List<Room>(room.connectedRooms)) {
                if (connectedRoom.connectedRooms.Contains(room)) {
                    connectedRoom.connectedRooms.Remove(room);
                    if (ConnectedRoomCount() < roomCount) {
                        // Removing this connection has led to islands in our graphs, so we can't do it.
                        connectedRoom.connectedRooms.Add(connectedRoom);

                        // Try removing the loop in the other direction.
                        room.connectedRooms.Remove(connectedRoom);
                        if (ConnectedRoomCount() < roomCount) {
                            Debug.Log("Could not remove loop");
                            room.connectedRooms.Add(connectedRoom);
                        }
                    }
                }
            }

            // Go through the remaining connections and enqueue them.
            foreach (var connectedRoom in room.connectedRooms) {
                if (!seen.Contains(connectedRoom)) {
                    queue.Enqueue(connectedRoom);
                    seen.Add(connectedRoom);
                }
            }
        }
    }

    public void DebugGraph() {
        var queue = new Queue<Room>();
        queue.Enqueue(this);
        var seen = new HashSet<Room>(queue);
        while (queue.Count > 0) {
            var room = queue.Dequeue();
            foreach (var connectedRoom in room.connectedRooms) {
                Debug.DrawLine(
                    new Vector3(room.position.x, 0, room.position.y),
                    new Vector3(connectedRoom.position.x, 0, connectedRoom.position.y),
                    Color.red, 1000000f);
                if (!seen.Contains(connectedRoom)) {
                    queue.Enqueue(connectedRoom);
                    seen.Add(connectedRoom);
                }
            }
        }
    }

    public void AlignToHallwayGrid(int hallwaySize, float gridStartX, float gridStartY) {
        Point offset = new Point(0, 0);

        var start = new Point(position.x + entranceRelative.x, position.y + entranceRelative.y);
        var aligned = new Point(start.x, start.y);

        var leftPoint = hallwaySize * Mathf.FloorToInt(start.x / hallwaySize) + gridStartX % hallwaySize;
        var rightPoint = leftPoint + hallwaySize;
        if (aligned.x - leftPoint < aligned.x - rightPoint) {
            aligned.x = leftPoint;
        } else {
            aligned.x = rightPoint;
        }

        var topPoint = hallwaySize * Mathf.FloorToInt(start.y / hallwaySize) + gridStartY % hallwaySize;
        var bottomPoint = topPoint + hallwaySize;
        if (aligned.y - topPoint < aligned.y - bottomPoint) {
            aligned.y = topPoint;
        } else {
            aligned.y = bottomPoint;
        }

        // TODO: This hardcodes the room rotation is where entrance + exit are horizontal.
        aligned.x -= hallwaySize / 2f;

        position.x += aligned.x - start.x;
        position.y += aligned.y - start.y;

        // Check if exit is unaligned? Exit and entrance must at least be aligned...
        if (GeneratorDebugSettings.DebugEntranceExits) {
            Debug.DrawLine(
                new Vector3(position.x + entranceRelative.x, 0, position.y + entranceRelative.y),
                new Vector3(position.x + entranceRelative.x, 5, position.y + entranceRelative.y),
                Color.cyan, 1000000f);
            Debug.DrawLine(
                new Vector3(position.x + exitRelative.x, 0, position.y + exitRelative.y),
                new Vector3(position.x + exitRelative.x, 5, position.y + exitRelative.y),
                Color.magenta, 1000000f);
        }

        instance.transform.position = new Vector3(position.x, 0, position.y);
    }
}

public class Point {
    public float x;
    public float y;

    public Point(float x, float y) {
        this.x = x;
        this.y = y;
    }
}

public enum HallwayType {
    None,
    Invalid,
    Door,
    StraightVertical,
    StraightHorizontal,
    TurnTopRight,
    TurnRightBottom,
    TurnBottomLeft,
    TurnLeftTop,
    ThreeWayTop,
    ThreeWayRight,
    ThreeWayBottom,
    ThreeWayLeft,
    FourWay,
};

public class HallwayGenerator {
    private HallwayType[,] map;
    private HallwayPoint[,] points;
    private int width;
    private int height;

    public HallwayGenerator(HallwayType[,] map) {
        this.map = map;
        width = map.GetLength(0);
        height = map.GetLength(1);

        points = new HallwayPoint[width, height];
        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                points[x, y] = new HallwayPoint(x, y);
            }
        }
    }

    private class HallwayPoint {
        public int x;
        public int y;
        public int endX;
        public int endY;
        public float? minCostToStart;
        public float straightLineDistanceToEnd;
        public HallwayPoint nearestToStart;

        public HallwayPoint(int x, int y) {
            this.x = x;
            this.y = y;
        }

        public void SetEnd(int endX, int endY) {
            float dX = endX - x;
            float dY = endY - y;

            this.endX = endX;
            this.endY = endY;

            minCostToStart = null;
            straightLineDistanceToEnd = Mathf.Sqrt(
                dX * dX + dY * dY);
            nearestToStart = null;
        }

        public List<HallwayPoint> ConnectedHallwayPoints(HallwayType[,] map, HallwayPoint[,] hallwayPoints) {
            int width = map.GetLength(0);
            int height = map.GetLength(1);

            var connectedHallwayPoints = new List<HallwayPoint>();

            if (x - 1 >= 0) {
                if (map[x - 1, y] != HallwayType.Invalid) {
                    connectedHallwayPoints.Add(hallwayPoints[x - 1, y]);
                }
            }

            if (x + 1 < width) {
                if (map[x + 1, y] != HallwayType.Invalid) {
                    connectedHallwayPoints.Add(hallwayPoints[x + 1, y]);
                }
            }

            if (y - 1 >= 0) {
                if (map[x, y - 1] != HallwayType.Invalid) {
                    connectedHallwayPoints.Add(hallwayPoints[x, y - 1]);
                }
            }

            if (y + 1 < height) {
                if (map[x, y + 1] != HallwayType.Invalid) {
                    connectedHallwayPoints.Add(hallwayPoints[x, y + 1]);
                }
            }

            return connectedHallwayPoints;
        }
    }

    public void FillShortestHallwayPath(int startX, int startY, int endX, int endY) {
        for (int x = 0; x < map.GetLength(0); x++) {
            for (int y = 0; y < map.GetLength(1); y++) {
                points[x, y].SetEnd(endX, endY);
            }
        }

        var startHallway = points[startX, startY];
        var endHallway = points[endX, endY];
        AStarSearch(startHallway, endHallway);
        var shortestPathHallway = new List<HallwayPoint> { endHallway };
        BuildShortestPath(shortestPathHallway, endHallway);

        foreach (var hallwayPoint in shortestPathHallway) {
            map[hallwayPoint.x, hallwayPoint.y] = HallwayType.FourWay;
        }
        // TODO: This hardcodes the room rotation.
        map[startHallway.x + 1, startHallway.y] = HallwayType.Door;
        map[endHallway.x - 1, endHallway.y] = HallwayType.Door;
    }

    private void AStarSearch(HallwayPoint start, HallwayPoint end) {
        bool[,] visited = new bool[width, height];

        start.minCostToStart = 0;
        var priorityQueue = new List<HallwayPoint> { start };
        do {
            priorityQueue = priorityQueue.OrderBy(x => x.minCostToStart + x.straightLineDistanceToEnd).ToList();
            var hallwayPoint = priorityQueue.First();
            priorityQueue.Remove(hallwayPoint);
            foreach (var connectedHallwayPoint in hallwayPoint.ConnectedHallwayPoints(map, points)) {
                if (visited[connectedHallwayPoint.x, connectedHallwayPoint.y]) {
                    continue;
                }
                if (connectedHallwayPoint.minCostToStart == null ||
                    hallwayPoint.minCostToStart + 1 < connectedHallwayPoint.minCostToStart) {
                    connectedHallwayPoint.minCostToStart = hallwayPoint.minCostToStart + 1;
                    connectedHallwayPoint.nearestToStart = hallwayPoint;
                    if (!priorityQueue.Contains(connectedHallwayPoint)) {
                        priorityQueue.Add(connectedHallwayPoint);
                    }
                }
            }
            visited[hallwayPoint.x, hallwayPoint.y] = true;
            if (hallwayPoint.x == end.x && hallwayPoint.y == end.y) {
                return;
            }
        } while (priorityQueue.Any());
    }

    private void BuildShortestPath(List<HallwayPoint> list, HallwayPoint node) {
        if (node.nearestToStart == null) {
            return;
        }
        list.Add(node.nearestToStart);
        BuildShortestPath(list, node.nearestToStart);
    }
}
