using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Cấu trúc dữ liệu để gom Tên và Ảnh của 1 chất hóa học
[System.Serializable]
public class ElementData
{
    public string elementName;
    public Sprite elementSprite;
}

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int rows = 6;
    public int cols = 10;

    [Header("Element Database")]
    // Danh sách Tên + Ảnh của các chất (nhập từ Inspector)
    public ElementData[] elementDatabase;

    [Header("References")]
    public GameObject tilePrefab;
    public Transform boardParent;
    public Slider expSlider; // Thanh Kinh Nghiệm

    private int[,] matrix;
    private TileController firstSelected = null;
    private int currentExp = 0;
    private int maxExp = 0;
    private bool isGameOver = false;

    void Start()
    {
        Application.targetFrameRate = 60;
        matrix = new int[rows + 2, cols + 2];
        
        GenerateBoardLogic();

        // 1 Cặp nối đúng = 1 EXP. Max EXP = Tống số ô chia 2.
        maxExp = (rows * cols) / 2;
        if (expSlider != null)
        {
            expSlider.minValue = 0;
            expSlider.maxValue = maxExp;
            expSlider.value = 0;
        }
    }

    void GenerateBoardLogic()
    {
        int totalPlayableTiles = rows * cols;
        List<int> elementIDs = new List<int>();

        int numPairs = totalPlayableTiles / 2;
        for (int i = 0; i < numPairs; i++)
        {
            // Lấy ngẫu nhiên ID từ danh sách Database bạn nạp vào
            int randomID = Random.Range(0, elementDatabase.Length);
            elementIDs.Add(randomID);
            elementIDs.Add(randomID);
        }

        // Xáo trộn (Shuffle)
        for (int i = elementIDs.Count - 1; i > 0; i--)
        {
            int r = Random.Range(0, i + 1);
            int temp = elementIDs[i];
            elementIDs[i] = elementIDs[r];
            elementIDs[r] = temp;
        }

        GridLayoutGroup gridLayout = boardParent.GetComponent<GridLayoutGroup>();
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = cols;

        int index = 0;
        for (int r = 1; r <= rows; r++)
        {
            for (int c = 1; c <= cols; c++)
            {
                int id = elementIDs[index];
                matrix[r, c] = id + 1; // +1 để phân biệt với 0 (ô trống)

                GameObject go = Instantiate(tilePrefab, boardParent);
                TileController tile = go.GetComponent<TileController>();
                // Gửi dữ liệu Tên + Ảnh vào ô
                tile.SetupTile(r, c, id + 1, elementDatabase[id], this);

                index++;
            }
        }
    }

    public void SelectTile(TileController clickedTile)
    {
        if (isGameOver) return;

        if (firstSelected == clickedTile)
        {
            firstSelected.SetSelectedVisual(false);
            firstSelected = null;
            return;
        }

        if (firstSelected == null)
        {
            firstSelected = clickedTile;
            firstSelected.SetSelectedVisual(true);
        }
        else
        {
            if (firstSelected.elementID == clickedTile.elementID)
            {
                Vector2Int p1 = new Vector2Int(firstSelected.x, firstSelected.y);
                Vector2Int p2 = new Vector2Int(clickedTile.x, clickedTile.y);

                // Sử dụng thuật toán quét thẳng dòng (Line-Search BFS) mới
                if (CheckPathLineBFS(p1, p2))
                {
                    matrix[p1.x, p1.y] = 0;
                    matrix[p2.x, p2.y] = 0;

                    // Gọi hiệu ứng thay vì ẩn đi lập tức
                    firstSelected.PlayMatchEffect();
                    clickedTile.PlayMatchEffect();

                    // Tăng cấp độ
                    GainExp();
                }
                else
                {
                    firstSelected.SetSelectedVisual(false);
                }
            }
            else
            {
                firstSelected.SetSelectedVisual(false);
            }
            
            firstSelected = null;
        }
    }

    private void GainExp()
    {
        currentExp++;
        if (expSlider != null) expSlider.value = currentExp;

        if (currentExp >= maxExp)
        {
            isGameOver = true;
            Debug.Log("LEVEL UP! Chúc mừng hoàn thành bài học!");
        }
    }

    // --- THUẬT TOÁN TÌM ĐƯỜNG KIỂU MỚI (CHỐNG KẸT LỖI 100%) ---
    struct Node
    {
        public int x, y, segments; // Đếm số đoạn thẳng (tối đa 3 đoạn = 2 lần rẽ)
        public Node(int x, int y, int segments) { this.x = x; this.y = y; this.segments = segments; }
    }

    private bool CheckPathLineBFS(Vector2Int start, Vector2Int target)
    {
        Queue<Node> queue = new Queue<Node>();
        int[,] minSegments = new int[rows + 2, cols + 2];
        for (int i = 0; i < rows + 2; i++)
            for (int j = 0; j < cols + 2; j++)
                minSegments[i, j] = int.MaxValue;

        queue.Enqueue(new Node(start.x, start.y, 0));
        minSegments[start.x, start.y] = 0;

        int[] dx = { -1, 1, 0, 0 }; // Lên, Xuống
        int[] dy = { 0, 0, -1, 1 }; // Trái, Phải

        while (queue.Count > 0)
        {
            Node curr = queue.Dequeue();

            for (int d = 0; d < 4; d++) // Quét 4 hướng
            {
                int nx = curr.x;
                int ny = curr.y;

                // Thay vì đi 1 ô, nó quét chạy thẳng dài 1 mạch cho tới khi đụng tường/chướng ngại vật
                while (true)
                {
                    nx += dx[d];
                    ny += dy[d];

                    // Văng ra khỏi bản đồ -> Dừng
                    if (nx < 0 || nx >= rows + 2 || ny < 0 || ny >= cols + 2) break;

                    int newSegments = curr.segments + 1;
                    
                    // Nếu quá 3 đoạn thẳng (gấp khúc > 2 lần) -> Dừng
                    if (newSegments > 3) break;

                    // Chạm đúng ô đích! -> Thành công
                    if (nx == target.x && ny == target.y) return true;

                    // Đụng trúng một ô hóa học khác cản đường -> Dừng
                    if (matrix[nx, ny] > 0) break;

                    // Lưu lại và tiếp tục từ điểm ngã 3 này
                    if (newSegments < minSegments[nx, ny])
                    {
                        minSegments[nx, ny] = newSegments;
                        queue.Enqueue(new Node(nx, ny, newSegments));
                    }
                }
            }
        }
        return false;
    }
}