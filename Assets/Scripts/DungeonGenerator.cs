using DelaunayVoronoi;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


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
        hallwaySize = 1;

        var rooms = GenerateRooms();
        var startingRoom = GenerateConnections(rooms);
        var hallwayMap = GenerateHallwayMap(startingRoom, rooms);
        DrawHallwayMap(hallwayMap);
        SetHallwayTypes(hallwayMap);
        GenerateHallways(hallwayMap);
    }

    public List<Room> GenerateRooms() {
        var roomTypes = GetRooms();

        var rooms = new List<Room>();

        var roomCount = 50;

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
        var spreadSpeed = 0.1f;
        var minimumDistance = 20;
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
        minX -= 20;
        minY -= 20;
        maxX += 20;
        maxY += 20;
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

        Debug.DrawLine(new Vector3(startingRoom.position.x, 0, startingRoom.position.y),
            new Vector3(startingRoom.position.x, 5, startingRoom.position.y),
            Color.yellow, 100000f);

        // Center room has no entrance; you spawn there.
        startingRoom.DeleteConnectionsIntoSelf();

        // Remove as many connections as possible while preventing islands.
        startingRoom.DeleteAsManyConnectionsAsPossible();

        // Remove any loops while preventing islands.
        startingRoom.DeleteLoops();

        // TODO: Add some more connections randomly.

        startingRoom.DrawEntireGraph();

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
                    // Debug.DrawLine(
                    //     new Vector3(worldX - hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                    //     new Vector3(worldX + hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                    //     Color.black, 1000000f);
                    // Debug.DrawLine(
                    //     new Vector3(worldX - hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                    //     new Vector3(worldX + hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                    //     Color.black, 1000000f);
                } else {
                    map[x, y] = HallwayType.None;
                    // Debug.DrawLine(
                    //     new Vector3(worldX - hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                    //     new Vector3(worldX + hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                    //     Color.white, 1000000f);
                    // Debug.DrawLine(
                    //     new Vector3(worldX - hallwaySize / 2f, 0, worldY + hallwaySize / 2f),
                    //     new Vector3(worldX + hallwaySize / 2f, 0, worldY - hallwaySize / 2f),
                    //     Color.white, 1000000f);
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
                hallwayGenerator.FillShortestHallwayPath(
                    Mathf.CeilToInt((room.exit.x - minX - hallwaySize / 2f) / hallwaySize),
                    Mathf.CeilToInt((room.exit.y - minY - hallwaySize / 2f) / hallwaySize),
                    Mathf.CeilToInt((connectedRoom.entrance.x - minX - hallwaySize / 2f) / hallwaySize),
                    Mathf.CeilToInt((connectedRoom.entrance.y - minY - hallwaySize / 2f) / hallwaySize));

                if (!seen.Contains(connectedRoom)) {
                    queue.Enqueue(connectedRoom);
                    seen.Add(connectedRoom);
                }
            }
        }

        return map;
    }

    public void SetHallwayTypes(HallwayType[,] hallwayMap) {

    }

    public void GenerateHallways(HallwayType[,] hallwayMap) {
        var hallwayFourWay = Resources.Load("Prefabs\\Hallways\\HallwayFourWay");
        for (int x = 0; x < hallwayMap.GetLength(0); x++) {
            for (int y = 0; y < hallwayMap.GetLength(1); y++) {
                var hallway = hallwayMap[x, y];
                switch (hallway) {
                    case HallwayType.Invalid:
                        break;
                    case HallwayType.None:
                        break;
                    case HallwayType.Hallway:
                        Instantiate(hallwayFourWay,
                            new Vector3(x * hallwaySize + minX, 0, y * hallwaySize + minY),
                            Quaternion.identity);
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

    public void DrawHallwayMap(HallwayType[,] hallwayMap) {
        for (int x = 0; x < hallwayMap.GetLength(0); x++) {
            for (int y = 0; y < hallwayMap.GetLength(1); y++) {
                var hallway = hallwayMap[x, y];
                switch (hallway) {
                    case HallwayType.Invalid:
                        break;
                    case HallwayType.None:
                        break;
                    case HallwayType.Hallway:
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

        entranceRelative = new Point(-width / 2f, 0);
        exitRelative = new Point(width / 2f, 0);
    }

    public void FinalizePosition() {
        // TODO: Autodiscover these based on GameObject's subobjects (exit / entrance GameObjects).
        entrance = new Point(position.x + entranceRelative.x, position.y);
        exit = new Point(position.x + exitRelative.x, position.y);
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

    public void DrawEntireGraph() {
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

        aligned.x = Mathf.CeilToInt(aligned.x);
        aligned.y = Mathf.CeilToInt(aligned.y);

        aligned.x += (aligned.x + gridStartX) % hallwaySize;
        aligned.y += (aligned.y + gridStartY) % hallwaySize;
        aligned.x += hallwaySize / 2f;
        aligned.y += hallwaySize / 2f;

        position.x += aligned.x - start.x;
        position.y += aligned.y - start.y;

        // Check if exit is unaligned? Exit and entrance must at least be aligned...
        Debug.DrawLine(
            new Vector3(position.x + entranceRelative.x, 0, position.y + entranceRelative.y),
            new Vector3(position.x + entranceRelative.x, 5, position.y + entranceRelative.y),
            Color.cyan, 1000000f);
        Debug.DrawLine(
            new Vector3(position.x + exitRelative.x, 0, position.y + exitRelative.y),
            new Vector3(position.x + exitRelative.x, 5, position.y + exitRelative.y),
            Color.magenta, 1000000f);

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

public enum HallwayType { None, Invalid, Hallway };

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
        Debug.Log(shortestPathHallway.Count);

        foreach (var hallwayPoint in shortestPathHallway) {
            map[hallwayPoint.x, hallwayPoint.y] = HallwayType.Hallway;
        }
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
