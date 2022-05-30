using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using Unity.Mathematics;

public class FlowFieldManager : MonoBehaviour
{

    public Vector3[] dirLUT =
    {
        Vector3.down, Vector3.forward, (Vector3.forward + Vector3.right).normalized, Vector3.right, (Vector3.back + Vector3.right).normalized, Vector3.back, (Vector3.back + Vector3.left).normalized, Vector3.left, (Vector3.forward + Vector3.left).normalized
    };

    public static readonly int sectorSize = 64;

    public int targetX, targetY;

    JobHandle debugHandle;

    NativeArray<Direction> direction;
    NativeArray<int> cost, integ;
    NativeList<int2> stack;

    private bool scheduled = false;
    private bool isDone = false;

    // Start is called before the first frame update
    void Start()
    {
        

        cost = new NativeArray<int>(64*64, Allocator.Persistent);
        integ = new NativeArray<int>(64*64, Allocator.Persistent);
        stack = new NativeList<int2>(10, Allocator.Persistent);

        direction = new NativeArray<Direction>(64*64, Allocator.Persistent);

        stack.Add(new int2(targetX,targetY));

        for(int x = 0; x < sectorSize; x++)
        {
            for(int y = 0; y < sectorSize; y++)
            {
                if(!(x == targetX && y == targetY))
                {
                    integ[y*sectorSize + x] = 255;
                }
                cost[y*sectorSize + x] = 1;
            }
        }

        cost[5*sectorSize + 18] = 40;
        cost[6*sectorSize + 18] = 40;
        cost[5*sectorSize + 19] = 40;
        cost[6*sectorSize + 19] = 40;

        var integrationJob = new IntegrateJob
        {
            costGrid = cost,
            stack = stack,
            integrationGrid = integ
        };

        var directionJob = new DirectionJob
        {
            integrationGrid = integ,
            directionGrid = direction
        };

        var integHandle = integrationJob.Schedule();
        var directionHandle = directionJob.Schedule(64*64, 1,integHandle);

        debugHandle = directionHandle;

        scheduled = true;
    }

    private void OnDrawGizmosSelected()
    {
        
        if(isDone)
        {
            for(int x = 0; x < sectorSize; x++)
            {
                for(int y = 0; y < sectorSize; y++)
                {
                    Gizmos.color = Color.white;
                    int i = y*sectorSize + x;
                    if (x == targetX && y == targetY)
                        Gizmos.color = Color.blue;
                    Gizmos.DrawRay(new Vector3(x, 1, y), dirLUT[(int) direction[i]]*0.5f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(new Vector3(x, 1, y), new Vector3(0,10*integ[i]/255f,0));
                }
            }
        }
    }

    private void OnDestroy()
    {
        direction.Dispose();
        cost.Dispose();
        stack.Dispose();
        integ.Dispose();
    }


    void Update()
    {
        if (scheduled)
        {
            if (debugHandle.IsCompleted)
            {
                debugHandle.Complete();
                isDone = true;
                scheduled = false;
            }
        }
    }

    [BurstCompile]
    private struct IntegrateJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> costGrid;

        public NativeList<int2> stack;

        //must be filled with high default value except for the goal gridspaces.
        public NativeArray<int> integrationGrid;

        //TODO: Add in walls and shit
        public void Execute()
        {
            while (stack.Length > 0)
            {
                int2 i = stack[0];
                stack.RemoveAt(0);

                int x = i.x;
                int y = i.y;

                int integCenter = integrationGrid[I(x, y)];

                if(y != 0)
                {
                    int costNorth = costGrid[I(x, y-1)];
                    int integNorth = integrationGrid[I(x, y-1)];

                    if (integCenter + costNorth < integNorth)
                    {
                        integrationGrid[I(x, y-1)] = integCenter + costNorth;

                        stack.Add(new int2(x, y-1));
                    }
                }

                if(x != sectorSize - 1)
                {
                    int costEast = costGrid[I(x+1, y)];
                    int integEast = integrationGrid[I(x+1, y)];

                    if (integCenter + costEast < integEast)
                    {
                        integrationGrid[I(x+1, y)] = integCenter + costEast;

                        stack.Add(new int2(x+1, y));
                    }
                }

                if (y != sectorSize - 1)
                {
                    int costSouth = costGrid[I(x, y+1)];
                    int integSouth = integrationGrid[I(x, y+1)];

                    if (integCenter + costSouth < integSouth)
                    {
                        integrationGrid[I(x, y+1)] = integCenter + costSouth;

                        stack.Add(new int2(x, y+1));
                    }
                }

                if(x != 0)
                {
                    int costWest = costGrid[I(x-1, y)];
                    int integWest = integrationGrid[I(x-1, y)];

                    if (integCenter + costWest < integWest)
                    {
                        integrationGrid[I(x-1, y)] = integCenter + costWest;
                        stack.Add(new int2(x-1, y));
                    }
                }
            }
        }

        public static int I(int x, int y)
        {
            return y*sectorSize + x;
        }
    }

    [BurstCompile]
    private struct DirectionJob : IJobParallelFor
    {
        [ReadOnly]
        public NativeArray<int> integrationGrid;

        [WriteOnly]
        public NativeArray<Direction> directionGrid;

        

        public void Execute(int i) 
        {
            Direction dir = Direction.ZERO;

            int x = i % sectorSize;
            int y = i / sectorSize;

            int min = integrationGrid[i];

            bool top, bottom, left, right;

            top = (y == 0);
            bottom = (y == sectorSize-1);
            left = (x == 0);
            right = (x == sectorSize-1);

            if(!top)
            {
                int n = integrationGrid[I(x, y-1)];
                if(n < min)
                {
                    min = n;
                    dir = Direction.NORTH;
                }
            }

            if (!top && !right)
            {
                int ne = integrationGrid[I(x+1, y-1)];
                if (ne < min)
                {
                    min = ne;
                    dir = Direction.NORTHEAST;
                }
            }

            if (!right)
            {
                int e = integrationGrid[I(x+1, y)];
                if (e < min)
                {
                    min = e;
                    dir = Direction.EAST;
                }
            }

            if (!bottom & !right)
            {
                int se = integrationGrid[I(x+1, y+1)];
                if (se < min)
                {
                    min = se;
                    dir = Direction.SOUTHEAST;
                }
            }

            if (!bottom)
            {
                int s = integrationGrid[I(x, y+1)];
                if (s < min)
                {
                    min = s;
                    dir = Direction.SOUTH;
                }
            }

            if (!bottom & !left)
            {
                int sw = integrationGrid[I(x-1, y+1)];
                if (sw < min)
                {
                    min = sw;
                    dir = Direction.SOUTHWEST;
                }
            }

            if (!left)
            {
                int w = integrationGrid[I(x-1, y)];
                if (w < min)
                {
                    min = w;
                    dir = Direction.WEST;
                }
            }

            if (!top & !left)
            {
                int nw = integrationGrid[I(x-1, y-1)];
                if (nw < min)
                {
                    min = nw;
                    dir = Direction.NORTHWEST;
                }
            }

            directionGrid[i] = dir;
            
        }

        public int I(int x, int y)
        {
            return y*sectorSize + x;
        }
    }

    public enum Direction : byte { ZERO = 0, NORTH, NORTHEAST, EAST, SOUTHEAST, SOUTH, SOUTHWEST, WEST, NORTHWEST} 
}
