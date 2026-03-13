using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System;

// Успешный успех. Просто тестовый проектик для того чтобы посмотреть как сохраняются файлы с использованием стандартных библиотек C#. В целом не слишком сложно. 
// Это поле 10х10 размером, с игроком. Каждая клеточка имеет позицию и цвет. Для передвижения стрелочки, чтобы изменить цвет под собой то пробел.
// Одновременно видны только чанки в пределах одной клетки, при перемещении чанки, оказавшиеся за пределеами сохраняются в виде файликов и удаляются из игры.
// Вошедшие в радиус обзора чанки либо загружаются с диска, либо же генерируются с нуля.
// Если еще в потоки уйти то загрузку и выгрузку можно перенести на отдельное ядрыко процессора чтобы другие не забивать. Но это важно будет только если игра РТСкой выйжет

public class SeamlessWorldTest : MonoBehaviour
{
    public static Vector2Int world_size = new Vector2Int(10, 10);

    public Vector2Int player_pos = new Vector2Int(5, 5);
    public Color player_color = Color.black;
    public int loading_range = 1;
    public static int palette = 8;

    enum load_state { generated, kept, failure, loaded, unloaded }

    List<TestToySeamlessChunk> chunks = new List<TestToySeamlessChunk>();

    public void Start()
    {
        Debug.Log(Application.persistentDataPath);
    }


    public void Update()
    {
        MovePlayer();
        DrawSelf();
    }

    public void DrawSelf()
    {
        for (int c = 0; c < chunks.Count; c++)
        {
            DebugUtilities.DebugDrawCross(chunks[c].pos, pickColor(chunks[c].color));
        }
        DrawSquare(player_pos, player_color);
        DebugUtilities.DrawRectangle(player_pos - new Vector2(0.5f, 0.5f), player_pos + new Vector2(0.5f, 0.5f), player_color);
    }
    public void MovePlayer()
    {
        Vector2Int prev_pos = player_pos;
        if (Input.GetKeyUp(KeyCode.UpArrow)) player_pos += Vector2Int.up;
        if (Input.GetKeyUp(KeyCode.DownArrow)) player_pos += Vector2Int.down;
        if (Input.GetKeyUp(KeyCode.LeftArrow)) player_pos += Vector2Int.left;
        if (Input.GetKeyUp(KeyCode.RightArrow)) player_pos += Vector2Int.right;
        if (Input.GetKeyUp(KeyCode.Space))
        {
            int index = FindChunk(player_pos);
            if (index >= 0 | index < chunks.Count) chunks[index].color = (chunks[index].color + 1) % palette;
        }
        PositionClamp();
        if (prev_pos != player_pos) OnMoveScenario(player_pos);
    }

    public int FindChunk(Vector2 pos)
    {
        for (int i = 0; i < chunks.Count; i++)
            if (chunks[i].pos == pos) return i;
        return -1;
    }

    public void DrawSquare(Vector2 xy, Color color)
    {
        DebugUtilities.DrawRectangle(xy - new Vector2(0.5f, 0.5f), xy + new Vector2(0.5f, 0.5f), color);
    }
    static Vector2 off04 = new Vector2(0.4f, 0.4f);
    static Vector2 off03 = new Vector2(0.3f, 0.3f);
    
    void DrawChunkState(Vector2 xy, load_state generated)
    {
        Color color = Color.white;
        switch  (generated)
        {
            case load_state.generated: color = Color.purple; break;
            case load_state.failure: color = Color.red; break;
            case load_state.loaded: color = Color.green; break;
            case load_state.unloaded: color = Color.yellow; break;
            default: color = Color.white; break;
        }
        DebugUtilities.DrawRectangle(xy - off04, xy + off04, color, 1f);
        DebugUtilities.DrawRectangle(xy - off03, xy + off03, color, 1f);
    }
    public void PositionClamp()
    {
        player_pos.x = (int)Mathf.Clamp(player_pos.x, 0, world_size.x);
        player_pos.y = (int)Mathf.Clamp(player_pos.y, 0, world_size.y);
    }
    public Color pickColor(int v)
    {
        if (v < 0 | v > palette) return Color.black;
        return DebugUtilities.RainbowGradient_Red2Violet(v, palette - 1);
    }
    
    public class TestToySeamlessChunk
    {
        [SerializeField] public int color;
        [SerializeField] public Vector2Int pos;
        public static TestToySeamlessChunk GenerateNewChunk(Vector2Int xy, int x_size, int strip_size)
        {
            TestToySeamlessChunk new_chunk = new TestToySeamlessChunk();

            new_chunk.color = (xy.x * x_size + xy.y + strip_size * 7) % strip_size;
            new_chunk.pos = xy;
            return new_chunk;
        }
        public override string ToString()
        {
            return pos.ToString() + " " + color;
        }
    }
    [Serializable]
    public class TestToyChunkData {
        [SerializeField] public int color;
        [SerializeField] public int x;
        [SerializeField] public int y;
        public TestToyChunkData(TestToySeamlessChunk chunk)
        {
            this.color = chunk.color; this.x = chunk.pos.x; this.y = chunk.pos.y;
        }
        public TestToySeamlessChunk GetChunk()
        {
            TestToySeamlessChunk chunk = new TestToySeamlessChunk();
            chunk.pos = new Vector2Int(this.x, this.y);
            chunk.color = this.color;
            return chunk;
        }
    }

    public void DumpChunkData()
    {
        string n = "";
        for (int i = 0; i < chunks.Count; i++)
        {
            n += chunks[i].ToString() + " \n";
        }
        Debug.Log(n);
        Debug.Log(player_pos + " " + player_color);

    }

    public void PurgeChunkData()
    {
        this.chunks.Clear();
    }

    public void RegenerateChunkData()
    {
        if (chunks.Count != 0) PurgeChunkData();
        for (int x = 0 - loading_range; x < 0 + loading_range + 1; x++)
        {
            for (int y = 0 - loading_range; y < 0 + loading_range + 1; y++)
            {
                var new_chunk = TestToySeamlessChunk.GenerateNewChunk(player_pos + new Vector2Int(x, y), world_size.x, palette);
                this.chunks.Add(new_chunk);
            }
        }
    }
    // Saving loading
    public void SaveChunkData(Vector2Int pos)
    {
        int index = FindChunk(pos);
        if (index < 0 | index >= chunks.Count) { Debug.Log("Failed to find chunk, it must have been unloaded or outside boundaries"); return; }
        SaveChunkData(chunks[index]);
    }
    public void SaveChunkData(TestToySeamlessChunk chunk)
    {
        //JsonReaderWriterFactory.CreateJsonWriter()
        string path = ChunkLocation(chunk.pos);
        Debug.Log("chunk saving here hope goes well \n" + path);
        if (File.Exists(path))
        {
            Debug.Log("this file exists, doinjg nothing");
        } //else
        {
            FileStream stream = new FileStream(path, FileMode.Create);
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, new TestToyChunkData(chunk));
            stream.Close();
            Debug.Log("new file created: \n" + path);
        }
    }
    public void LoadChunkDataAndAdd(Vector2Int pos)
    {
        int index = FindChunk(pos);
        if (index >= 0 & index < chunks.Count) { 
            Debug.Log("this chunk already exists, removing it and loading it from memory");
            chunks.RemoveAt(index);
        }
        Debug.Log("Chunk does not exist, attempting to load it from memory");
        TestToySeamlessChunk chunk = LoadChunkData(pos);
        this.chunks.Add(chunk);
    }
    public TestToySeamlessChunk LoadChunkData(Vector2Int pos)
    {
        string path = ChunkLocation(pos);
        if (File.Exists(path))
        {
            Debug.Log("Found existing chunk, loading it from memory ");
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path, FileMode.Open);
            TestToyChunkData chunk_data = formatter.Deserialize(stream) as TestToyChunkData;
            stream.Close();
            return chunk_data.GetChunk();
        } else
        {
            Debug.Log("Did not found existing chunk, generating new one from scratch");
            return TestToySeamlessChunk.GenerateNewChunk(pos, world_size.x, palette);
        }
    }

    public string ChunkLocation(Vector2Int pos)
    {
        return Application.persistentDataPath + "/chunk" + pos.x + "_" + pos.y + ".chu";
    }

    public void OnMoveScenario(Vector2Int new_pos)
    {
        // Найти разницу в текущем и новом диапазоне.
        // Надо загрузить/сгенерировать новые чанки
        // Сохранить и удалить старые чанки
        (var keep, var kill, var load) = UpdateLoadingRange(new_pos);
        Debug.Log(DebugUtilities.DebugListString(kill.ToArray()));
        for (int i = 0; i < kill.Count; i++)
        {
            int to_kill = kill[kill.Count - 1 - i];
            SaveChunkData(chunks[to_kill]);
            DrawChunkState(chunks[to_kill].pos, load_state.unloaded);
            chunks.RemoveAt(to_kill);
        }

        for (int i = 0; i < keep.Count; i++)
            DrawChunkState(keep[i], load_state.kept);

        for (int i = 0; i < load.Count; i++)
        {
            DrawChunkState(load[i], load_state.loaded);
            LoadChunkDataAndAdd(load[i]);
        }

    }
    
    public (List<Vector2Int> to_keep, List<int> to_kill, List<Vector2Int> to_load) UpdateLoadingRange(Vector2Int new_pos)
    {
        List<int> to_kill = new List<int>();
        List<Vector2Int> to_keep = new List<Vector2Int>();
        for (int i = 0; i < chunks.Count; i++)
            if (OutsideLoadingRange(chunks[i].pos, new_pos)) to_kill.Add(i);
            else to_keep.Add(chunks[i].pos);
        List<Vector2Int> to_load = GetRangeToLoad(new_pos);
        for (int i = 0; i < to_keep.Count; i++)
            for (int j = 0; j < to_load.Count; j++)
                if (to_load[j] == to_keep[i]) { to_load.RemoveAt(j); continue; }

        Debug.Log(to_keep.Count + " " + to_kill.Count + " " + to_load.Count);

        return (to_keep, to_kill, to_load);
    }
    public bool OutsideLoadingRange(Vector2Int chunk_pos, Vector2Int new_pos)
    {
        int d_x = Mathf.Abs(chunk_pos.x - new_pos.x);
        int d_y = Mathf.Abs(chunk_pos.y - new_pos.y);
        return d_x > loading_range | d_y > loading_range;
    }
    public List<Vector2Int> GetRangeToLoad(Vector2Int new_pos)
    {
        int quantity = (loading_range * 2 + 1) * (loading_range * 2 + 1);
        List<Vector2Int> to_load = new List<Vector2Int>(quantity);
        for (int x = 0 - loading_range; x < 1 + loading_range; x++)
            for (int y = 0 - loading_range; y < 1 + loading_range; y++)
                to_load.Add(new_pos + new Vector2Int(x, y));
        return to_load;
    }

    public void TryLoadAllChunks()
    {
        for (int x = 0; x < world_size.x; x++)
        {
            for (int y = 0; y < world_size.y; y++)
            {
                LoadChunkDataAndAdd(new Vector2Int(x, y));
            }
        }
    }

}
[CustomEditor(typeof(SeamlessWorldTest))]
public class SWTEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SeamlessWorldTest swt = (SeamlessWorldTest)target;
        if (GUILayout.Button("Dump chunk data")) swt.DumpChunkData();
        if (GUILayout.Button("Purge chunk data")) swt.PurgeChunkData();
        if (GUILayout.Button("Regenerate chunk data")) swt.RegenerateChunkData();
        if (GUILayout.Button("Save chunk under player")) swt.SaveChunkData(swt.player_pos);
        if (GUILayout.Button("Load chunk under player")) swt.LoadChunkDataAndAdd(swt.player_pos);
        if (GUILayout.Button("Load all chunks for funzies")) swt.TryLoadAllChunks();
        base.OnInspectorGUI();
    }
}