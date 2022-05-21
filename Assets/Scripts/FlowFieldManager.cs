using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

public class FlowFieldManager : MonoBehaviour
{

    public static readonly int sectorSize = 64;

    // Start is called before the first frame update
    void Start()
    {
        var input = new NativeArray<int>(10, Allocator.Persistent);
        var output = new NativeArray<Direction>(1, Allocator.Persistent);
        for (int i = 0; i < input.Length; i++)
            input[i] = i;

        var job = new DirectionJob
        {
            integrationGrid = input,
            directionGrid = output
        };
        job.Schedule(output.Length, 1).Complete();

        Debug.Log("The result of the sum is: " + output[0]);
        input.Dispose();
        output.Dispose();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [BurstCompile(CompileSynchronously = true)]
    private struct IntegrateJob : IJob
    {
        [ReadOnly]
        public NativeArray<int> costGrid;

        public NativeArray<int> stack;

        //must be filled with high default value except for the goal gridspaces.
        public NativeArray<int> integrationGrid;

        //TODO: Add in walls and shit
        public void Execute()
        {
            int stackPointer = 0;
            while (stackPointer >= 0)
            {
                int i = stack[stackPointer--];

                int x = i % sectorSize;
                int y = i / sectorSize;

                int integCenter = integrationGrid[I(x, y)];

                if(y != 0)
                {
                    int costNorth = costGrid[I(x, y-1)];
                    int integNorth = integrationGrid[I(x, y-1)];

                    if (integCenter + costNorth < integNorth)
                    {
                        integrationGrid[I(x, y-1)] = integCenter + costNorth;

                        stack[++stackPointer] = I(x, y-1);
                    }
                }

                if(x != sectorSize - 1)
                {
                    int costEast = costGrid[I(x+1, y)];
                    int integEast = integrationGrid[I(x+1, y)];

                    if (integCenter + costEast < integEast)
                    {
                        integrationGrid[I(x+1, y)] = integCenter + costEast;

                        stack[++stackPointer] = I(x+1, y);
                    }
                }

                if (y != sectorSize - 1)
                {
                    int costSouth = costGrid[I(x, y+1)];
                    int integSouth = integrationGrid[I(x, y+1)];

                    if (integCenter + costSouth < integSouth)
                    {
                        integrationGrid[I(x, y+1)] = integCenter + costSouth;

                        stack[++stackPointer] = I(x, y+1);
                    }
                }

                if(x != 0)
                {
                    int costWest = costGrid[I(x-1, y)];
                    int integWest = integrationGrid[I(x-1, y)];

                    if (integCenter + costWest < integWest)
                    {
                        integrationGrid[I(x-1, y)] = integCenter + costWest;

                        stack[++stackPointer] = I(x-1, y);
                    }
                }
            }
        }

        public static int I(int x, int y)
        {
            return y*sectorSize + x;
        }
    }

    [BurstCompile(CompileSynchronously = true)]
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
