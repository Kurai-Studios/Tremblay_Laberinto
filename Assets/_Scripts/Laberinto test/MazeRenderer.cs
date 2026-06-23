using UnityEngine;

public class MazeRenderer : MonoBehaviour
{
    [SerializeField] MazeGenerator mazeGenerator;
    [SerializeField] GameObject MazeCellPrefab;

    public float cellSize = 1f;

    private void Start()
    {
        MazeCell[,] maze = mazeGenerator.GetMaze();

        // Loop through every cell in the maze
        for (int x = 0; x < mazeGenerator.mazeWidth; x++)
        {
            for (int y = 0; y < mazeGenerator.mazeHeight; y++)
            {
                // Instantiate a new cell prefab as a child of the mazerenderer object
                GameObject newCell = Instantiate(MazeCellPrefab, new Vector3((float)x * cellSize, 0f,
                                                 (float)y * cellSize), Quaternion.identity, transform);

                // Get a reference to the cells mazeCellprefab script
                MazeCellObj mazeCell = newCell.GetComponent<MazeCellObj>();

                // Determine which walls need to be active
                bool top = maze[x, y].topWall;
                bool left = maze[x, y].leftWall;

                // bottom and right walls are deactivated by default unless we are at the
                // bottom or right edge of the maze
                bool right = false;
                bool bottom = false;

                if (x == mazeGenerator.mazeWidth - 1) right = true;
                if (y == 0) bottom = true;

                mazeCell.Init(top, bottom, right, left);
            }
        }
    }
}
