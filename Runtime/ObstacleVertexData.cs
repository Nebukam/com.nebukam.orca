using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace Nebukam.ORCA
{
    public struct ObstacleVertexData
    {
        public int infos;
        public int index;
        public int localIndex;
        public int next;
        public int prev;
        public bool convex;
        public float2 pos;
        public float2 dir;
        public float2 normal;
        public ObstacleSegment segment;
        //public float2 dir;
        //public float2 normal;
    }

    public struct ObstacleSegment
    {

        public float3 A, B;
        public float2 normal;
        public float length;

        public ObstacleSegment(float2 a, float2 b)
        {

            A = float3(a, 0f);
            B = float3(b, 0f);

            length = distance(A, B);

            float3 n = cross((A + float3(0f, 0f, 1f)) - A, A - B);
            n = normalize(n);
            normal = float2(n.x, n.y);

        }
        
        
        /// <summary>
        /// Find the closest point on the segment to a given point
        /// </summary>
        /// <param name="pt"></param>
        /// <returns></returns>
        public float2 ClosestPoint(float2 p)
        {

            float3 pt = float3(p, 0f);
            float3 C = float3(pt.x - A.x, pt.y - A.y, pt.z - A.z),//pt - A
                D = float3(B.x - A.x, B.y - A.y, B.z - A.z);//B - A

            float d = D.x * D.x + D.y * D.y + D.z * D.z; //Square distance

            D = normalize(D);
            float t = D.x * C.x + D.y * C.y + D.z * C.z; //Dot

            if (t <= 0)
                return float2(A.x, A.y);

            if ((t * t) >= d) //Distance check
                return float2(B.x, B.y);

            C.x = A.x + D.x * t; C.y = A.y + D.y * t; C.z = A.z + D.z * t;
            return float2(C.x, C.y);

        }
        

    }

}
