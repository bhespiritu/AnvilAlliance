using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using System;

public class MapGrid : MonoBehaviour
{
    public int width = 100, height = 100;
    public float scale = 1;

    public Grid<bool> occupied { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        occupied = new Grid<bool>(width, height);
    }

    

}

public interface IGrid<T>
{
    public int width { get; set; }
    public int height { get; set; }

    public void Set(T value, int minX, int minY, int maxX, int maxY);
    public void Set(T value, Vector2Int min, Vector2Int max);

    public void Set(T value, int x, int y);

    public void Set(IGrid<T> value, int x, int y);

    public SubGrid<T> subGrid(int minX, int minY, int maxX, int maxY);

    public T this[int x, int y]
    {
        get; set;
    }

    public SubGrid<T> this[int minX, int minY, int maxX, int maxY]
    {
        get;
    }

    public SubGrid<T> this[Vector2Int min, Vector2Int max]
    {
        get;
    }
}
 
public class Grid<T> : IGrid<T>
{
    public int width, height;
    public T[,] occupied;

    int IGrid<T>.width { get => width; set => width = value; }
    int IGrid<T>.height { get => height; set => height = value; }

    

    public Grid(int w, int h)
    {
        width = w;
        height = h;
        occupied = new T[w, h];
    }

    public void Set(T value, int minX, int minY, int maxX, int maxY)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                occupied[x, y] = value;
            }
        }
    }

    public SubGrid<T> subGrid(int minX, int minY, int maxX, int maxY)
    {
        int dx = maxX - minX;
        int dy = maxY - maxY;
        SubGrid<T> newGrid = new SubGrid<T>
        {
            offsetX = minX,
            offsetY = minY,
            m_width = dx,
            m_height = dy
        };
        for (int x = 0; x < dx; x++)
        {
            for (int y = 0; y < dy; y++)
            {
                newGrid[x, y] = this[x + minX, y + minY];
            }
        }
        return newGrid;
    }


    public void Set(T value, int x, int y) => occupied[x, y] = value;

    public void Set(IGrid<T> value, int offsetX, int offsetY)
    {

        for (int x = 0; x <= value.width; x++)
        {
            for (int y = 0; y <= value.height; y++)
            {
                this[x + offsetX, y + offsetY] = value[x, y];
            }
        }
    }

    

    public void Set(T value, Vector2Int min, Vector2Int max) => Set(value, min.x, min.y, max.x, max.y);


    public T this[int x, int y]
    {
        get
        {
            return occupied[x, y];
        }
        set
        {
            occupied[x, y] = value;
        }
        
    }

    public SubGrid<T> this[int minX, int minY, int maxX, int maxY]
    {
        get
        {
            return subGrid(minX,minY,maxX,maxY);
        }
    }

    public SubGrid<T> this[Vector2Int min, Vector2Int max] { get => subGrid(min.x,min.y,max.x,max.y); }
}

/*
 * SubGrid is a reference to a subsection of a Grid instance. Know that modifying this will modify the parent. If you want to modify it without affecting the original, use toGrid() to copy it to a new Grid instance.
 */
public struct SubGrid<T> : IGrid<T>
{
    public Grid<T> parent;
    public int offsetX, offsetY;
    public int m_width, m_height;

    public T this[int x, int y] { get => parent[x + offsetX, y + offsetY]; set => parent[x + offsetX, y + offsetY] = value; }

    public SubGrid<T> this[Vector2Int min, Vector2Int max] => this[min.x,min.y,max.x,max.y];

    public SubGrid<T> this[int minX, int minY, int maxX, int maxY] => subGrid(minX,minY,maxX,maxY);

    public int width { get => m_width; set => m_width = value; }
    public int height { get => m_height; set => m_height = value; }

    public void Set(T value, int minX, int minY, int maxX, int maxY)
    {
        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                parent[x + offsetX, y + offsetY] = value;
            }
        }
    }

    public void Set(T value, Vector2Int min, Vector2Int max) => Set(value, min.x, min.y, max.x, max.y);

    public void Set(T value, int x, int y)
    {
        parent[x + offsetX, y + offsetY] = value;
    }

    public void Set(IGrid<T> value, int offsetX, int offsetY)
    {
        for (int x = 0; x <= value.width; x++)
        {
            for (int y = 0; y <= value.height; y++)
            {
                this[x + offsetX, y + offsetY] = value[x, y];
            }
        }
    }

    public SubGrid<T> subGrid(int minX, int minY, int maxX, int maxY)
    {
        int dx = maxX - minX;
        int dy = maxY - maxY;
        return new SubGrid<T>
        {
            parent = this.parent,
            offsetX = this.offsetX + minX,
            offsetY = this.offsetY + minY,
            m_width = dx,
            m_height = dy
        };
    }

    public Grid<T> toGrid()
    {
        var newGrid = new Grid<T>(m_width, m_height);
        newGrid.Set(this, 0, 0);
        return newGrid;
    }
}


