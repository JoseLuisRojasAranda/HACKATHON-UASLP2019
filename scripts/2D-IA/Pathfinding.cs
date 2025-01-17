﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;
using UnityEngine;

public class Pathfinding : MonoBehaviour {

    public bool debug;                          // PAra debugging del tiempo

    private PathRequestManager requestManager;  // Manager para encontrar los caminos
    private Grid grid;                          // Malla con los nodos del A*
    private Stopwatch sw;                       // Reloj para medir el tiempo cuando esta en modo debug

    void Awake() {
        grid = this.GetComponent<Grid>();
        requestManager = this.GetComponent<PathRequestManager>();
    }

    // Empieza una que encuentra el camino mas corto
    public void StartFindPath(Vector2 startPos, Vector2 targetPos) {
        StartCoroutine(FindPath(startPos, targetPos));
    }

    // Corutina para encontrar el camino
    // Este codigo es la implementacion del algoritmo A*
    IEnumerator FindPath(Vector2 startPos, Vector2 targetPos) {
        if(debug) {
            sw = new Stopwatch();
            sw.Start();
        }

        Vector2[] waypoints = new Vector2[0];
        bool pathSuccess = false;

        Node startNode = grid.NodeFromWorldPoint(startPos);
        Node targetNode = grid.NodeFromWorldPoint(targetPos);

        if(startNode.walkable && targetNode.walkable) {
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);
            HashSet<Node> closedSet = new HashSet<Node>();
            openSet.Add(startNode);

            while(openSet.Count > 0) {
                Node currentNode = openSet.RemoveFirst();
                closedSet.Add(currentNode);

                if(currentNode == targetNode) {
                    if(debug) {
                        sw.Stop();
                        print("Path found: " + sw.ElapsedMilliseconds + " ms");
                    }
                    pathSuccess = true;
                    break;
                }

                foreach(Node neighbour in grid.GetNeighbours(currentNode)) {
                    if(!neighbour.walkable || closedSet.Contains(neighbour))
                        continue;

                    int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour) + neighbour.movementPenalty;
                    if(newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour)) {
                        neighbour.gCost = newMovementCostToNeighbour;
                        neighbour.hCost = GetDistance(neighbour, targetNode);
                        neighbour.parent = currentNode;

                        if(!openSet.Contains(neighbour))
                            openSet.Add(neighbour);
                        else
                            openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        yield return null;
        if(pathSuccess) {
            waypoints = RetracePath(startNode, targetNode);
        }

        requestManager.FinishProcessPath(waypoints, pathSuccess);
    }

    Vector2[] RetracePath(Node startNode,  Node endNode) {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while(currentNode != startNode) {
            path.Add(currentNode);
            currentNode = currentNode.parent;
        }

        Vector2[] waypoints = SimplifyPath(path);
        Array.Reverse(waypoints);

        return waypoints;

    }

    Vector2[] SimplifyPath(List<Node> path) {
        List<Vector2> waypoints = new List<Vector2>();
        Vector2 directionOld = Vector2.zero;

        for(int i = 1; i < path.Count; i++) {
            Vector2 directionNew  = new Vector2(path[i-1].gridX - path[i].gridX, path[i-1].gridY - path[i].gridY);
            //if(directionNew != directionOld) {
                waypoints.Add(path[i].worldPosition);
            //}

            directionOld = directionNew;
        }

        return waypoints.ToArray();
    }

    int GetDistance(Node nodeA, Node nodeB) {
        int distX = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int distY = Mathf.Abs(nodeA.gridY - nodeB.gridY);

        if(distX > distY) {
            return 14*distY + 10*(distX-distY);
        } else {
            return 14*distX + 10*(distY-distX);
        }
    }
}
