using System.Collections.Generic;
using UnityEngine;
//using System;

public struct PathFindBlock {
    public int currentNavBoxRoom; // текущее положение
    public int targetNavBoxRoom; // расположение цели
    public int nextNavBoxRoom; // следующий шаг, к оторому нужно двигаться
    public Bounds? intermediateStep; // Область пересечения текущей комнаты и следующей комнаты
    public PathFindBlock(int currBox, int targetBox, int nextBox, Bounds? intermediate)
    {
        currentNavBoxRoom = currBox;
        targetNavBoxRoom = targetBox;
        nextNavBoxRoom = nextBox;
        intermediateStep = intermediate;
    }

    static public PathFindBlock empty()
    {
        return new PathFindBlock(-1,-1,-1,null);
    }

}

public class AIAgent : MonoBehaviour
{
    public NavigationSpace encapsulator;
    public Transform target;
    public float speed;
    public float epsilon = 0.1f;

    PathFindBlock pathFindBlock = new PathFindBlock(-1,-1,-1,null);
    bool compiled = false;

    // Update is called once per frame

    private void Start()
    {
    }

    void Update()
    {
        
        if (compiled == false)
        {
            this.pathFindBlock.currentNavBoxRoom = encapsulator.NaiveBoxFinder(this.transform);
            this.pathFindBlock.targetNavBoxRoom = encapsulator.NaiveBoxFinder(this.target);
            compiled = true;
        }

        if (pathFindBlock.currentNavBoxRoom == -1 & compiled)
        {
            this.gameObject.SetActive(false);
        }

        if (pathFindBlock.nextNavBoxRoom == -1)
        {
            this.pathFindBlock = this.PathFind();
        } 
        else
        {
            this.MoveAlongPath();
        }

        

        
    }   

    public void MoveAlongPath()
    {
        Vector3 current_pos = this.transform.position;
        //Debug.Log(pathFindBlock.nextNavBoxRoom);
        if (pathFindBlock.intermediateStep.HasValue == false) return;

        Bounds bounds = pathFindBlock.intermediateStep.Value;

        Debug.Log(this.pathFindBlock.currentNavBoxRoom.ToString() + " " + this.pathFindBlock.targetNavBoxRoom);
        if (this.pathFindBlock.currentNavBoxRoom == this.pathFindBlock.targetNavBoxRoom)
        {
            Debug.Log(this.pathFindBlock.currentNavBoxRoom);
            Vector3 targetPos = this.target.position;
            Vector3 dir = (targetPos - current_pos).normalized;
            this.transform.position += dir * this.speed * Time.deltaTime;
            return;
        }


        {
            if (bounds.Contains(this.transform.position) && (this.transform.position - bounds.center).magnitude < epsilon)
            {
                this.pathFindBlock.currentNavBoxRoom = this.pathFindBlock.nextNavBoxRoom;
                this.pathFindBlock.nextNavBoxRoom = -1;
                return;
            }
        }
        
        Vector3 interTarget = bounds.center;
        Vector3 direction = (interTarget - current_pos).normalized;

        //Debug.Log(direction * this.speed * Time.deltaTime);

        this.transform.position += direction * this.speed * Time.deltaTime;
    }


    public PathFindBlock PathFind()
    {
        if (this.pathFindBlock.currentNavBoxRoom == -1) {
            return PathFindBlock.empty();
        }
        if (this.pathFindBlock.targetNavBoxRoom == -1) {
            return PathFindBlock.empty();
        }

        // 255 = выброшен
        // 0 = не исследовано
        // +n = номер комнаты в списке
        int startBox = this.pathFindBlock.currentNavBoxRoom;
        int target = this.pathFindBlock.targetNavBoxRoom;
        if (startBox == target) { return new PathFindBlock(startBox, target, target, encapsulator.boxes[target].myBounds); }
        


        byte[] boxStatus = new byte[encapsulator.boxCount];
        for (int i = 0; i < encapsulator.boxCount; i++) boxStatus[i] = 0;

        Stack<int> steps = new Stack<int>();
        steps.Push(startBox);
        boxStatus[steps.Peek()] = 1;
        bool blocked;

        
        //Debug.LogFormat("Pathfinding begun! Start Box: {0} target: {1}", steps.Peek(), target);
        int safety = 0;
        while (safety < 254) { safety += 1;
            // Проверка выполнения
            int currentBoxID = steps.Peek();
            if (currentBoxID == target)
            {
                int current_step = steps.Peek();
                for (int i = 0; i < steps.Count; i++)
                {
                    int previous_step = steps.Pop();
                    current_step = steps.Peek();
                    if (current_step == startBox) 
                    {
                        current_step = previous_step;
                        break;
                    }
                }
                Vector3[] AB = this.encapsulator.boxes[startBox].ConnectionShape(this.encapsulator.boxes[current_step]);
                //Debug.Log("Founjd it!");

                //Debug.Log(this.encapsulator.boxes[startBox].myBounds.min);
                //Debug.Log(this.encapsulator.boxes[startBox].myBounds.max);

                //Debug.Log(AB[0]);
                //Debug.Log(AB[1]);
                Bounds bounds = new Bounds();
                bounds.min = AB[0];
                bounds.max = AB[1];

                //Debug.Log(currentBoxID.ToString() + " " + target );
                return new PathFindBlock(startBox, target, current_step, bounds);
            }

            // Проверка наличия следующего шага
            List<int> roomIndexes = encapsulator.GetNeighboursList(currentBoxID);
            /*
            string foundNeighbours = "";
            foreach (var item in roomIndexes) foundNeighbours += item + " ";
            Debug.Log(foundNeighbours);*/

            for (int i = 0; i < roomIndexes.Count; i++)  {
                //Debug.LogFormat("Neighbour number: {0}, status: {1}", roomIndexes[i], boxStatus[roomIndexes[i]]);
                if (boxStatus[roomIndexes[i]] != 0) {
                    roomIndexes.RemoveAt(i);
                    i -= 1;
                }
            }
            blocked = roomIndexes.Count == 0;

            /*
            string roomStatus = "";
            for (int i = 0; i < boxStatus.Length; i++) roomStatus += "{" + i + ":" + boxStatus[i] + "} roomID : Status";
            Debug.Log(roomStatus);*/

            if (blocked)
            {
                int blockedID = steps.Pop(); // В этом случае идти больше некуда, ID вычеркиваетсчя из стека
                boxStatus[blockedID] = byte.MaxValue;
            } else
            {
                int closestToFinish = pickSmallest(roomIndexes, target);
                steps.Push(closestToFinish); // новая точка перемещения добавляется в стек
                boxStatus[steps.Peek()] = (byte)steps.Count; 
            }
        }
        //Debug.Log("Возврат дефектного блока.");
        return PathFindBlock.empty();
    }

    private int pickSmallest(float[] distanceList)
    {
        if (distanceList.Length == 0) return -1;
        //if (distanceList.Length == 1) return (distanceList[0] != float.PositiveInfinity) ? 0 : -1;

        float smallest = float.PositiveInfinity;
        int smallest_id = -1;
        for (int i = 0; i < distanceList.Length; i++)
        {
            if (distanceList[i] == float.PositiveInfinity) continue;
            if (distanceList[i] < smallest)
            {
                smallest = distanceList[i];
                smallest_id = i;
            }           
        }
        return smallest_id;

    }

    private int pickSmallest(List<int> roomID_list, int targetRoom) 
    {
        int count = roomID_list.Count;
        if (count == 0) return -1;

        float smallest_dist = float.PositiveInfinity;
        int smallest_id = -1;
        for (int i = 0; i < count; i++)
        {
            float distance = encapsulator.boxes[roomID_list[i]].CenterDistance(encapsulator.boxes[targetRoom]);
            if (distance < smallest_dist)
            {
                smallest_id = i;
                smallest_dist = distance;
            }
        }
        return roomID_list[smallest_id];       
    }
}
