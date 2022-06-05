using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

using Unity.Mathematics;

public class FlowFieldManager : MonoBehaviour
{

    private readonly Vector3[] dirLUT =
    {
        Vector3.down, Vector3.back, (Vector3.back + Vector3.right).normalized, Vector3.right, (Vector3.forward + Vector3.right).normalized, Vector3.forward, (Vector3.forward + Vector3.left).normalized, Vector3.left, (Vector3.back + Vector3.left).normalized
    };

    public static readonly int sectorSize = 32;

    public int targetX, targetY;

    JobHandle debugHandle;

    private bool scheduled = false;
    private bool isDone = false;

    public NativeFlowField currentFlowField;

    private Camera mainCam;

    // Start is called before the first frame update
    void Start()
    {
        mainCam = Camera.main;
        currentFlowField = new NativeFlowField();
        currentFlowField.SetTarget(new int2(targetX, targetY));

        currentFlowField.Calculate();

        debugHandle = currentFlowField.handle;
        scheduled = true;
    }

    public Vector2 getDirection(int x, int y)
    {
        var dir = dirLUT[(int)currentFlowField.direction[y*sectorSize + x]];
        return new Vector2(dir.x,dir.z);
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
                    Gizmos.DrawRay(new Vector3(x + 0.5f, 1, y + 0.5f), dirLUT[(int) currentFlowField.direction[i]]*0.5f);
                    Gizmos.color = Color.red;
                    Gizmos.DrawRay(new Vector3(x + 0.5f, 1, y + 0.5f), new Vector3(0,10*currentFlowField.integ[i]/255f,0));
                }
            }
        }
    }


    public void OnDestroy()
    {
        currentFlowField.Dispose();
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

        if(Input.GetMouseButtonDown(1))
        {
            var mPos = Input.mousePosition;
            var screenRay = mainCam.ScreenPointToRay(mPos);
            Debug.Log(screenRay);
            float denom = Vector3.Dot(Vector3.down, screenRay.direction);
            Debug.Log(denom);
            if(denom > 0.0000001)
            {
                Vector3 diff = Vector3.zero - screenRay.origin;
                float t = Vector3.Dot(diff, Vector3.down) / denom;

                var groundPos = screenRay.GetPoint(t);
                Debug.Log(groundPos);
                targetX = (int) groundPos.x;
                targetY = (int) groundPos.z;
            } else
            {
                targetX = 7;
                targetY = 7;
            }
            currentFlowField.Dispose();
            currentFlowField = new NativeFlowField();
            currentFlowField.SetTarget(new int2(targetX, targetY));

            currentFlowField.Calculate();

            debugHandle = currentFlowField.handle;
        }
    }

    public class NativeFlowField : INativeDisposable
    {
        public NativeArray<Direction> direction;
        public NativeArray<int> cost, integ;
        public NativeList<int2> stack;
        public JobHandle handle;

        public NativeFlowField()
        {
            this.direction = new NativeArray<Direction>(sectorSize*sectorSize, Allocator.Persistent);
            this.cost = new NativeArray<int>(sectorSize*sectorSize, Allocator.Persistent);
            this.stack = new NativeList<int2>(sectorSize*sectorSize, Allocator.Persistent);
            this.integ = new NativeArray<int>(sectorSize*sectorSize, Allocator.Persistent);

            for (int x = 0; x < sectorSize; x++)
            {
                for (int y = 0; y < sectorSize; y++)
                {
                    integ[y*sectorSize + x] = 255;
                    cost[y*sectorSize + x] = 1;
                }
            }

            foreach (Building b in FindObjectsOfType<Building>())
            {
                for (int x = 0; x < b.dimensions.x; x++)
                {
                    for (int y = 0; y < b.dimensions.y; y++)
                    {
                        int px = (int)b.transform.position.x;
                        int py = (int)b.transform.position.z;
                        cost[(py-y)*sectorSize + (px-x)] = 255;
                    }
                }


            }
        }

        public void SetTarget(int2 target, bool overrideTarget = true)
        {
            if (overrideTarget && stack.Length != 0)
            {
                int2 oldTarget = stack[0];
                integ[oldTarget.y*sectorSize + oldTarget.x] = 255;
            }
            if (stack.Length == 0 || !overrideTarget)
            {
                stack.Add(target);
            } else
            {
                stack[0] = target;
            }
            integ[target.y*sectorSize + target.x] = 0;
        }

        public void Calculate()
        {
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
            var directionHandle = directionJob.Schedule(sectorSize*sectorSize, 1, integHandle);

            handle = directionHandle;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var disposeJob = new DisposeJob
            {
                direction = direction,
                cost = cost,
                integ = integ,
                stack = stack
            };

            return disposeJob.Schedule(inputDeps);
        }

        private struct DisposeJob : IJob
        {
            public NativeArray<Direction> direction;
            public NativeArray<int> cost, integ;
            public NativeList<int2> stack;
            public void Execute()
            {
                stack.Dispose();
                cost.Dispose();
                integ.Dispose();
                direction.Dispose();
            }
        }


        public void Dispose()
        {
            direction.Dispose();
            cost.Dispose();
            stack.Dispose();
            integ.Dispose();
        }

        ~NativeFlowField()
        {
            direction.Dispose();
            cost.Dispose();
            stack.Dispose();
            integ.Dispose();
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

            

            if (!top && !right)
            {
                int ne = integrationGrid[I(x+1, y-1)];
                if (ne < min)
                {
                    min = ne;
                    dir = Direction.NORTHEAST;
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

            

            if (!bottom & !left)
            {
                int sw = integrationGrid[I(x-1, y+1)];
                if (sw < min)
                {
                    min = sw;
                    dir = Direction.SOUTHWEST;
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

            if (!top)
            {
                int n = integrationGrid[I(x, y-1)];
                if (n < min)
                {
                    min = n;
                    dir = Direction.NORTH;
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

            if (!bottom)
            {
                int s = integrationGrid[I(x, y+1)];
                if (s < min)
                {
                    min = s;
                    dir = Direction.SOUTH;
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

            directionGrid[i] = dir;
            
        }

        public int I(int x, int y)
        {
            return y*sectorSize + x;
        }
    }

    public enum Direction : byte { ZERO = 0, NORTH, NORTHEAST, EAST, SOUTHEAST, SOUTH, SOUTHWEST, WEST, NORTHWEST} 
}
