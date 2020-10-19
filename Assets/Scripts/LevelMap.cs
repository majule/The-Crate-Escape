using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelMap : MonoBehaviour
{
    Interactable[,] _interactableArray;
    Dictionary<Vector3Int, Interactable> interactableDict;

    public Interactable[,] map {get; private set;}
    public int MaxX {get; private set;}
    public int MaxY {get; private set;}

    // Start is called before the first frame update
    void Start()
    {
        ReadLevelMap();
    }

    void ReadLevelMap()
    {
        var interactables = GetComponentsInChildren<Interactable>();
        MaxX = interactables.Max(a => a.GridPos.x);
        MaxY = interactables.Max(a => a.GridPos.y);
        //print("LevelMap found size of " + MaxX + " : " + MaxY);

        _interactableArray = new Interactable[MaxX + 1, MaxY + 1];
        foreach (var i in interactables)
        {
            _interactableArray[i.GridPos.x, i.GridPos.y] = i;
        }
        map = _interactableArray;
    }

    public void UpdateMap(int xPos, int yPos, Interactable interactable)
    {
        _interactableArray[xPos, yPos] = interactable;
        map = _interactableArray;
    }

    internal bool CanClimb(Vector3Int playerPos, Vector3Int gridPos)
    {
        int playerX = playerPos.x, playerY = playerPos.y, targetX = gridPos.x, targetY = gridPos.y;

        if (!map[targetX, targetY].canClimb) return false;  // target not climbable

        // First check relative position
        if (Math.Abs(playerX - targetX) == 1 && playerY == targetY && playerY < MaxY)
        {
            // Then check for empty spaces above
            if (map[playerX, playerY + 1].spaceType == SpaceType.Empty && map[targetX, targetY + 1].spaceType == SpaceType.Empty)
            {
                return true;
            }
        }
        return false;
    }
}
