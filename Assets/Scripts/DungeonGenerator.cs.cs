using DelaunayVoronoi;
using System.Collections.Generic;
using UnityEngine;


public class DungeonGenerator : MonoBehaviour {
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
    public static void Generate() {
        var rooms = GenerateRooms();
        GenerateHallways(rooms);
    }

    public static List<Room> GenerateRooms() {
        var roomTypes = GetRooms();

        var rooms = new List<Room>();

        var roomCount = 100;

        var mapWidth = 100;
        var mapHeight = 100;

        // Generate the rooms.
        for (var i = 0; i < roomCount; i++) {
            var roomType = roomTypes[Random.Range(0, roomTypes.Count - 1)];
            var room = new Room(roomType.resource, roomType.width, roomType.height);
            room.position.x = Random.Range(0, mapWidth);
            room.position.y = Random.Range(0, mapHeight);
            rooms.Add(room);
        }

        // Spread the rooms out.
        var spreading = true;
        var spreadSpeed = 5;
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
            Instantiate(room.resource,
                new Vector3(
                    room.position.x,
                    0,
                    room.position.y),
                Quaternion.identity);
        }

        return rooms;
    }

    public static void GenerateHallways(List<Room> rooms) {
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

        minX -= 10000;
        minY -= 10000;
        maxX += 10000;
        maxY += 10000;

        var point0 = new DelaunayVoronoi.Point(minX, minY);
        var point1 = new DelaunayVoronoi.Point(minX, maxY);
        var point2 = new DelaunayVoronoi.Point(maxX, maxY);
        var point3 = new DelaunayVoronoi.Point(maxX, minY);
        var borderTriangle1 = new Triangle(point0, point1, point2);
        var borderTriangle2 = new Triangle(point0, point2, point3);
        triangulator.border = new List<Triangle>() { borderTriangle1, borderTriangle2 };

        var delaunayTriangles = new List<Triangle>(triangulator.BowyerWatson(roomMidpoints));

        var testQuad = Resources.Load("Prefabs/TestQuad");
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

            // Each room has an entrance and an exit. Is Minimum Spanning Tree what we want?
            // TODO: Think more on this.

            foreach (var edge in edges) {
                var p1 = new Vector2((float)edge.Item1.X, (float)edge.Item1.Y);
                var p2 = new Vector2((float)edge.Item2.X, (float)edge.Item2.Y);

                var r1 = GetRoomByMidpoint(rooms, p1);
                var r2 = GetRoomByMidpoint(rooms, p2);

                var distance = Vector2.Distance(p1, p2);
                var center = (p1 + p2) / 2;
                Debug.DrawLine(new Vector3(p1.x, 0, p1.y), new Vector3(p2.x, 0, p2.y), Color.red, 1000000f);
                var rotation = Quaternion.FromToRotation(
                        new Vector3(p1.x, 0, p1.y),
                        new Vector3(p2.x, 0, p2.y));
            }
        }

        // Weights are the edge distance.
        int[,] graph = new int[5, 5]{
            {0, 2, 0, 6, 0},
            {2, 0, 3, 8, 5},
            {0, 3, 0, 0, 7},
            {6, 8, 0, 0, 9},
            {0, 5, 7, 9, 0},
        };

        var result = MinimumSpanningTree.Calculate(graph, 5);
        // Debug.Log(result);
    }

    public static List<Room> GetRooms() {
        var root = "Prefabs\\Rooms\\";
        var rooms = new List<Room>();

        foreach (var resource in Resources.LoadAll(root)) {
            var room = (GameObject)resource;
            rooms.Add(new Room(room,
                room.transform.lossyScale.x,
                room.transform.lossyScale.z));
        }

        return rooms;
    }

    public static Room GetRoomByMidpoint(List<Room> rooms, Vector2 position) {
        foreach (var room in rooms) {
            if (room.position.x == position.x && room.position.y == position.y) {
                return room;
            }
        }
        return null;
    }
}

public class Room {
    public GameObject resource;
    public float width;
    public float height;
    public Vector2 position;
    public HashSet<Room> connectedNodes;

    public Point entrance;
    public Point exit;

    public Room(GameObject resource, float width, float height) {
        this.resource = resource;
        this.width = width;
        this.height = height;
        connectedNodes = new HashSet<Room>();
    }

    public void FinalizePosition(Vector2 position) {
        this.position = position;

        // TODO: Autodiscover these based on GameObject's subobjects (exit / entrance GameObjects).
        entrance = new Point(position.x - width / 2, position.y);
        exit = new Point(position.x + width / 2, position.y);
    }
}

public class Point {
    public float x;
    public float z;

    public Point(float x, float z) {
        this.x = x;
        this.z = z;
    }
}
