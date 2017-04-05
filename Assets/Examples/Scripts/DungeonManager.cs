using UnityEngine;

[DefaultExecutionOrder(-200)]
public class DungeonManager : MonoBehaviour
{
    public int m_Width = 10;
    public int m_Height = 10;
    public float m_Spacing = 4.0f;
    public GameObject[] m_Tiles = new GameObject[16];

    void Awake()
    {
        Random.InitState(23431);
        var map = new int[m_Width * m_Height];
        for (int y = 0; y < m_Height; y++)
        {
            for (int x = 0; x < m_Width; x++)
            {
                bool px = false;
                bool py = false;
                if (x > 0)
                    px = (map[(x - 1) + y * m_Width] & 1) != 0;
                if (y > 0)
                    py = (map[x + (y - 1) * m_Width] & 2) != 0;

                int tile = 0;
                if (px)
                    tile |= 4;
                if (py)
                    tile |= 8;
                if (x + 1 < m_Width && Random.value > 0.5f)
                    tile |= 1;
                if (y + 1 < m_Height && Random.value > 0.5f)
                    tile |= 2;

                map[x + y * m_Width] = tile;
            }
        }

        for (int y = 0; y < m_Height; y++)
        {
            for (int x = 0; x < m_Width; x++)
            {
                var pos = new Vector3(x * m_Spacing, 0, y * m_Spacing);
                if (m_Tiles[map[x + y * m_Width]] != null)
                    Instantiate(m_Tiles[map[x + y * m_Width]], pos, Quaternion.identity);
            }
        }
    }
}
