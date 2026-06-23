using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MazeCell
{
    public bool visited;
    public int x, y;

    public bool topWall;
    public bool leftWall;

    public Vector2Int position
    {
        get { return new Vector2Int(x, y); }
    }

    public MazeCell(int x, int y)
    {
        this.x = x;
        this.y = y;

        visited = false;

        topWall = leftWall = true;
    }
}
public enum Direction
{
    Up,
    Down,
    Left,
    Right
}

public class MazeGenerator : MonoBehaviour
{
    [Range(5, 50)]
    public int mazeWidth = 5, mazeHeight = 5;  // Dimensions of the maze
    public int startX, startY;                 // Starting pos for the algorithm
    MazeCell[,] maze;

    Vector2Int currentCell;

    public MazeCell[,] GetMaze()
    {
        maze = new MazeCell[mazeWidth, mazeHeight];

        for (int x = 0; x < mazeWidth; x++)
        {
            for (int y = 0; y < mazeHeight; y++)
            {
                maze[x, y] = new MazeCell(x, y);
            }
        }

        CarvePath(startX, startY);

        return maze;
    }

    List<Direction> directions = new List<Direction>
    {
        Direction.Up, Direction.Down, Direction.Left, Direction.Right,
    };

    List<Direction> GetRandomDirections()
    {
        // Make a copy of the directions list
        List<Direction> dir = new List<Direction>(directions);

        // Make a directions list to store randomised directions into
        List<Direction> rndDir = new List<Direction>();

        while (dir.Count > 0) // Loop until rndDir list is empty
        {
            int rnd = Random.Range(0, dir.Count);   // Get random index in list
            rndDir.Add(dir[rnd]);                   // Add the random direction to the list
            dir.RemoveAt(rnd);                      // Remove that direction so we cant choose it again
        }

        // When we get all 4 directions in a random order, return the queue
        return rndDir;
    }

    bool isCellValid (int x, int y)
    {
        if (x < 0 || y < 0 || x > mazeWidth - 1 || y > mazeHeight - 1 || maze[x, y].visited) return false;
        else return true;
    }

    Vector2Int CheckNeighbours()
    {
        List<Direction> rndDir = GetRandomDirections();

        for (int i = 0; i < rndDir.Count; i++)
        {
            // Set neighbour coordinates to current cell for now
            Vector2Int neighbour = currentCell;

            // Modify neighbour coordinates based on the random directions we are trying
            switch (rndDir[i])
            {
                case Direction.Up:
                    neighbour.y++;
                    break;
                case Direction.Down:
                    neighbour.y--;
                    break;
                case Direction.Right:
                    neighbour.x++;
                    break;
                case Direction.Left:
                    neighbour.x--;
                    break;
            }

            // If the neigbour we tried is valid, we can return that neighbour, if not, try again
            if (isCellValid(neighbour.x, neighbour.y)) return neighbour;
        }

        return currentCell;
    }

    void BreakWalls(Vector2Int primaryCell, Vector2Int secondaryCell)
    {
        if (primaryCell.x > secondaryCell.x)
            maze[primaryCell.x, primaryCell.y].leftWall = false;

        else if (primaryCell.x < secondaryCell.x)
            maze[secondaryCell.x, secondaryCell.y].leftWall = false;

        else if (primaryCell.y < secondaryCell.y)
            maze[primaryCell.x, primaryCell.y].topWall = false;

        else if (primaryCell.y > secondaryCell.y)
            maze[secondaryCell.x, secondaryCell.y].topWall = false;
    }

    void CarvePath (int x, int y)
    {
        if (x < 0 || y < 0 || x > mazeWidth - 1 || y > mazeHeight - 1)
        {
            x = y = 0;
            Debug.LogWarning("Starting POS out of bounds, defaulting to 0, 0");
        }

        // Set current cell to the starting POS we got
        currentCell = new Vector2Int(x, y);

        // A list to keep track of the current path
        List<Vector2Int> path = new List<Vector2Int>();

        // Loop until we encounter a dead end
        bool deadEnd = false;

        while (!deadEnd)
        {
            // Get the next cell we're going to try
            Vector2Int nextCell = CheckNeighbours();

            // if that cell has no valid neighbours, set deadend to true so we break out of the loop
            if (nextCell == currentCell)
            {
                // If that cell has no valid neighbours, set deadend true so we break out of the loop
                for (int i = path.Count - 1; i >= 0; i--)
                {
                    currentCell = path[i];
                    path.RemoveAt(i);
                    nextCell = CheckNeighbours();

                    // If we find a valid neighbour, break out of the loop
                    if (nextCell != currentCell) break;
                }

                if (nextCell == currentCell) 
                    deadEnd = true;
            }
            else
            {
                BreakWalls(currentCell, nextCell);                      // Set wall flags on those 2 cells
                maze[currentCell.x, currentCell.y].visited = true;      // Set cell to visited before moving
                currentCell = nextCell;                                 // Set current cell to the valid neigbhour we found
                path.Add(currentCell);                                  // Add this cell to our path
            }
        }

    }
}
