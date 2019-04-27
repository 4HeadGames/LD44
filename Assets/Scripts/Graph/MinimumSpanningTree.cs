// From https://www.geeksforgeeks.org/prims-minimum-spanning-tree-mst-greedy-algo-5/

public static class MinimumSpanningTree {
    // A utility function to find  
    // the vertex with minimum key 
    // value, from the set of vertices  
    // not yet included in MST 
    public static int MinKey(int[] key, bool[] mstSet, int size) {

        // Initialize min value 
        int min = int.MaxValue, min_index = -1;

        for (int v = 0; v < size; v++) {
            if (mstSet[v] == false && key[v] < min) {
                min = key[v];
                min_index = v;
            }
        }
        return min_index;
    }

    // Function to construct and  
    // print MST for a graph represented 
    // using adjacency matrix representation 
    public static int[] Calculate(int[,] graph, int size) {

        // Array to store constructed MST 
        int[] parent = new int[size];

        // Key values used to pick 
        // minimum weight edge in cut 
        int[] key = new int[size];

        // To represent set of vertices 
        // not yet included in MST 
        bool[] mstSet = new bool[size];

        // Initialize all keys 
        // as INFINITE 
        for (int i = 0; i < size; i++) {
            key[i] = int.MaxValue;
            mstSet[i] = false;
        }

        // Always include first 1st vertex in MST. 
        // Make key 0 so that this vertex is 
        // picked as first vertex 
        // First node is always root of MST 
        key[0] = 0;
        parent[0] = -1;

        // The MST will have V vertices 
        for (int count = 0; count < size - 1; count++) {
            // Pick thd minimum key vertex 
            // from the set of vertices 
            // not yet included in MST 
            int u = MinKey(key, mstSet, size);

            // Add the picked vertex 
            // to the MST Set 
            mstSet[u] = true;

            // Update key value and parent  
            // index of the adjacent vertices 
            // of the picked vertex. Consider 
            // only those vertices which are  
            // not yet included in MST 
            for (int v = 0; v < size; v++) {
                // graph[u][v] is non zero only  
                // for adjacent vertices of m 
                // mstSet[v] is false for vertices 
                // not yet included in MST Update  
                // the key only if graph[u][v] is 
                // smaller than key[v] 
                if (graph[u, v] != 0 && mstSet[v] == false &&
                                        graph[u, v] < key[v]) {
                    parent[v] = u;
                    key[v] = graph[u, v];
                }
            }
        }

        return parent;
    }
}
