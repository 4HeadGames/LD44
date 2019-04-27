using System.Collections.Generic;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour {
    public static void Generate() {
        var root = "Prefabs\\Rooms\\";
        var rooms = new List<NodeMeta>();

        foreach (var resource in Resources.LoadAll(root)) {
            var room = (GameObject)resource;
            rooms.Add(new NodeMeta(room,
                room.transform.lossyScale.x,
                room.transform.lossyScale.z));
        }

        /*
         * Find how big it is by querying the object dimensions, organize that information into an Object.
         * Use the set of information to randomly generate a map with random seeding.
         * 
         * Types of items:
         *  Rooms
         *   Exits
         *   Entrances
         *  Hallways
         *  
         *  Algorithm.
         *  
         *  Starting point, center?
         *  
         *  Add a room, branch out - breadth first search with randomness
         *
         * Once generated enough rooms, find the two furthest rooms and add an entrance to one and an exit to the other.
         */

        /*
        var dungeon = new List<NodeMeta>();

        var newNodes = new Queue<NodeMeta>();
        newNodes.Enqueue(rooms[Random.Range(0, rooms.Count - 1)]);

        while (newNodes.Count > 0) {
            var newNode = newNodes.Dequeue();

            var possibleEntrances = new List<Point>(newNode.entrances);
            var possibleExits = new List<Point>(newNode.exits);

            var entrance = possibleEntrances[Random.Range(0, possibleEntrances.Count - 1)];


            for (var entrance in newNode.entrances) {

            }
        }

        Instantiate(rooms[0].resource);
        */
    }
}

public class NodeMeta {
    public GameObject resource;
    public float width;
    public float height;
    public float x;
    public float z;

    public List<Point> entrances;
    public List<Point> exits;

    public NodeMeta(GameObject resource, float width, float height) {
        this.width = width;
        this.height = height;
    }

    public void SetPosition(float x, float z) {
        this.x = x;
        this.z = z;

        // TODO: Autodiscover these based on GameObject's subobjects (exit / entrance GameObjects).
        entrances = new List<Point>() {
            new Point(x - width / 2, z),
            new Point(x + width / 2, z),
            new Point(x, z - height / 2),
            new Point(x, z + height / 2),
        };

        exits = new List<Point>() {
            new Point(x - width / 2, z),
            new Point(x + width / 2, z),
            new Point(x, z - height / 2),
            new Point(x, z + height / 2),
        };
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
