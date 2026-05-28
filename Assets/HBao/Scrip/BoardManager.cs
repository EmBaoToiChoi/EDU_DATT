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
               if (CheckPath(p1, p2))
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

    // --- THUẬT TOÁN TÌM ĐƯỜNG PIKACHU CHUẨN (100% KHÔNG LỖI) ---

    // Đổi tên hàm gọi ở phần Check Logic trong SelectTile thành hàm này
    private bool CheckPath(Vector2Int p1, Vector2Int p2)
    {
        if (CheckLine(p1, p2)) return true;       // Nối đường thẳng
        if (CheckRect(p1, p2)) return true;       // Nối chữ L (1 lần rẽ)
        if (CheckMoreLine(p1, p2)) return true;   // Nối chữ U, Z (2 lần rẽ)
        return false;
    }

    // 1. Kiểm tra đường thẳng
    private bool CheckLine(Vector2Int p1, Vector2Int p2)
    {
        if (p1.x == p2.x) // Cùng hàng
        {
            int y1 = Mathf.Min(p1.y, p2.y);
            int y2 = Mathf.Max(p1.y, p2.y);
            for (int y = y1 + 1; y < y2; y++)
                if (matrix[p1.x, y] > 0) return false; // Vướng vật cản
            return true;
        }
        if (p1.y == p2.y) // Cùng cột
        {
            int x1 = Mathf.Min(p1.x, p2.x);
            int x2 = Mathf.Max(p1.x, p2.x);
            for (int x = x1 + 1; x < x2; x++)
                if (matrix[x, p1.y] > 0) return false; // Vướng vật cản
            return true;
        }
        return false;
    }

    // 2. Kiểm tra chữ L (1 lần rẽ)
    private bool CheckRect(Vector2Int p1, Vector2Int p2)
    {
        Vector2Int p3 = new Vector2Int(p1.x, p2.y); // Góc vuông 1
        if (matrix[p3.x, p3.y] == 0 && CheckLine(p1, p3) && CheckLine(p2, p3)) return true;

        Vector2Int p4 = new Vector2Int(p2.x, p1.y); // Góc vuông 2
        if (matrix[p4.x, p4.y] == 0 && CheckLine(p1, p4) && CheckLine(p2, p4)) return true;

        return false;
    }

    // 3. Kiểm tra chữ U, Z (2 lần rẽ)
    private bool CheckMoreLine(Vector2Int p1, Vector2Int p2)
    {
        // Quét dọc theo tất cả các cột
        for (int y = 0; y < cols + 2; y++)
        {
            Vector2Int p3 = new Vector2Int(p1.x, y);
            Vector2Int p4 = new Vector2Int(p2.x, y);
            if (matrix[p3.x, p3.y] == 0 && matrix[p4.x, p4.y] == 0)
            {
                if (CheckLine(p1, p3) && CheckLine(p3, p4) && CheckLine(p4, p2)) return true;
            }
        }

        // Quét ngang theo tất cả các hàng
        for (int x = 0; x < rows + 2; x++)
        {
            Vector2Int p3 = new Vector2Int(x, p1.y);
            Vector2Int p4 = new Vector2Int(x, p2.y);
            if (matrix[p3.x, p3.y] == 0 && matrix[p4.x, p4.y] == 0)
            {
                if (CheckLine(p1, p3) && CheckLine(p3, p4) && CheckLine(p4, p2)) return true;
            }
        }
        return false;
    }
}