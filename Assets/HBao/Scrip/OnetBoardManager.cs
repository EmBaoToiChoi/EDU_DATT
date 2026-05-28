using System.Collections.Generic;
using UnityEngine;

public class OnetBoardManager : MonoBehaviour
{
    public int rows = 6;
    public int cols = 10;
    
    // board chứa ID của nguyên tố (0 = ô trống, > 0 = ID nguyên tố)
    private int[,] board;

    void Start()
    {
        // Cộng thêm 2 để tạo lớp viền rỗng bên ngoài cho phép nối vòng
        board = new int[rows + 2, cols + 2];
        GenerateBoard();
    }

    void GenerateBoard()
    {
        // Todo: Viết logic sinh ngẫu nhiên các cặp ID nguyên tố và gán vào board
        // Đảm bảo số lượng mỗi loại ID phải là số chẵn
    }

    // Cấu trúc hỗ trợ cho thuật toán tìm đường
    struct Node
    {
        public int x, y, dir, turns;
        public Node(int x, int y, int dir, int turns)
        {
            this.x = x; this.y = y; this.dir = dir; this.turns = turns;
        }
    }

    // Trả về true nếu có thể nối 2 điểm (p1, p2 là tọa độ trên ma trận)
    public bool CanConnect(Vector2Int p1, Vector2Int p2)
    {
        // Khác loại không thể nối
        if (board[p1.x, p1.y] != board[p2.x, p2.y]) return false;
        
        // Trùng vị trí
        if (p1 == p2) return false;

        return CheckPathBFS(p1, p2);
    }

    private bool CheckPathBFS(Vector2Int start, Vector2Int target)
    {
        Queue<Node> queue = new Queue<Node>();
        // int[x, y, hướng] để lưu số lần rẽ nhỏ nhất, tránh lặp lại vòng lặp vô tận
        int[,,] minTurns = new int[rows + 2, cols + 2, 4]; 
        
        for (int i = 0; i < rows + 2; i++)
            for (int j = 0; j < cols + 2; j++)
                for (int d = 0; d < 4; d++)
                    minTurns[i, j, d] = int.MaxValue;

        // Các hướng: 0=Lên, 1=Xuống, 2=Trái, 3=Phải
        int[] dx = { 0, 0, -1, 1 };
        int[] dy = { 1, -1, 0, 0 };

        // Khởi tạo 4 hướng từ điểm bắt đầu
        for (int d = 0; d < 4; d++)
        {
            queue.Enqueue(new Node(start.x, start.y, d, 0));
            minTurns[start.x, start.y, d] = 0;
        }

        while (queue.Count > 0)
        {
            Node current = queue.Dequeue();

            // Nếu đến đích và số lần rẽ <= 2 (tương đương 3 đoạn thẳng)
            if (current.x == target.x && current.y == target.y && current.turns <= 2)
                return true;

            for (int d = 0; d < 4; d++)
            {
                int nx = current.x + dx[d];
                int ny = current.y + dy[d];

                // Kiểm tra giới hạn mảng
                if (nx < 0 || nx >= rows + 2 || ny < 0 || ny >= cols + 2) continue;

                // Tính số lần rẽ mới (nếu đổi hướng thì cộng 1)
                int newTurns = current.turns + (current.dir != d ? 1 : 0);

                // Nếu số lần rẽ > 2, bỏ qua nhánh này
                if (newTurns > 2) continue;

                // Chỉ đi tiếp nếu ô đó là ô trống HOẶC là ô đích
                if (board[nx, ny] == 0 || (nx == target.x && ny == target.y))
                {
                    if (newTurns < minTurns[nx, ny, d])
                    {
                        minTurns[nx, ny, d] = newTurns;
                        queue.Enqueue(new Node(nx, ny, d, newTurns));
                    }
                }
            }
        }
        return false;
    }
}