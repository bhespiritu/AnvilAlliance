using System.Collections.Generic;
using UnityEngine;

public class KDTree<T> where T : IKDTreeElement
{
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    private class KDTreeNode
    {
        KDTreeNode left;
        KDTreeNode right;

        int depth;

        T value;

        public KDTreeNode(List<T> points, int depth)
        {
            this.depth = depth;

            List<T> leftPoints = new List<T>(points.Count/2);
            List<T> rightPoints = new List<T>(points.Count/2);

            bool axis = (depth % 2 == 0);

            if (points.Count == 1)
            {
                value = points[0];
            } else if (points.Count == 2)
            {
                value = points[0];
                var a = (axis ? points[0].getPosition().x : points[0].getPosition().y);
                var b = (axis ? points[1].getPosition().x : points[1].getPosition().y);
                if (a <= b)
                {
                    right = new KDTreeNode(points[1], depth + 1);
                } else
                {
                    left= new KDTreeNode(points[1], depth + 1);
                }
                
            } else
            {
                points.Sort((a, b) => { return (axis ? a.getPosition().x : a.getPosition().y).CompareTo((axis ? b.getPosition().x : b.getPosition().y)); });//TODO: replace with better function

                leftPoints = points.GetRange(0, points.Count/2);
                rightPoints = points.GetRange(points.Count/2+1, points.Count - (points.Count/2 + 1));

                value = points[points.Count/2];

                left = new KDTreeNode(leftPoints, depth+1);
                right = new KDTreeNode(rightPoints, depth+1);
            }
        }

        public KDTreeNode(T value, int depth)
        {
            this.depth = depth;
            this.value = value;
            
        }
    }
}

public interface IKDTreeElement
{
    public Vector2 getPosition();
}