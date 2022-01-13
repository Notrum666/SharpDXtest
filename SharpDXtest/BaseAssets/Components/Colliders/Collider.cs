using SharpDXtest.BaseAssets.Components.Colliders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpDXtest.BaseAssets.Components
{
    public abstract class Collider : Component
    {
        public Vector3 InertiaTensor { get; protected set; }

        public abstract Vector3 Offset { get; set; }
        private double massPart = 1.0;
        public double MassPart
        {
            get
            {
                return massPart;
            }
            set
            {
                if (value < 0.0)
                    throw new ArgumentException("Mass part can't be negative.");
                massPart = value;
            }
        }

        protected List<Vector3> vertices = new List<Vector3>();
        protected List<int[]> polygons = new List<int[]>();
        protected List<Vector3> globalSpaceVertices = new List<Vector3>();
        protected List<Vector3> globalSpaceNormals = new List<Vector3>();

        protected double squaredOuterSphereRadius;
        protected double outerSphereRadius;

        protected void calculateOuterSphereRadius()
        {
            double currentLenght;

            foreach(Vector3 vertex in vertices)
            {
                currentLenght = vertex.squaredLength();
                if(currentLenght > squaredOuterSphereRadius)
                {
                    squaredOuterSphereRadius = currentLenght;
                    outerSphereRadius = Math.Sqrt(squaredOuterSphereRadius);
                }
            }
        }
        public virtual void calculateGlobalVertices()
        {
            globalSpaceVertices.Clear();

            Matrix4x4 worldMatrix = gameObject.transform.model;
            foreach(Vector3 localVertex in vertices)
            {
                globalSpaceVertices.Add((worldMatrix * new Vector4(localVertex, 1)).xyz);
            }
        }
        protected void calculateNormalsInGlobal()
        {
            globalSpaceNormals.Clear();

            Vector3 currentNormal;
            Vector3 edge1;
            Vector3 edge2;
            foreach(int[] poly in polygons)
            {
                edge1 = globalSpaceVertices[poly[1]] - globalSpaceVertices[poly[0]];
                edge2 = globalSpaceVertices[poly[2]] - globalSpaceVertices[poly[1]];
                currentNormal = edge1.cross(edge2);

                if (!globalSpaceNormals.Exists(n => n.isCollinearTo(currentNormal)))
                    globalSpaceNormals.Add(currentNormal);
            }
        }
        protected virtual void projectOnVector(Vector3 vector, Vector3[] projection)
        {
            for(int i = 0; i < globalSpaceVertices.Count; i++)
            {
                projection[i] = globalSpaceVertices[i].projectOnVector(vector);
            }
        }
        protected Vector3 getCenterInGlobal()
        {
            return (gameObject.transform.model * new Vector4(this.Offset, 1)).xyz;
        }
        private static void getSegmentFromProjection(Vector3[] projection, Vector3[] segment)
        {
            int i;
            for (i = 1; i < projection.Length; i++)
            {
                if (!(projection[i] - projection[0]).isZero())
                    break;
            }

            if (i == projection.Length)
                throw new ArgumentException("Projection of collider is the point.");

            double maxK = 0, minK = 0, k;
            Vector3 baseVector = projection[i] - projection[0];

            for (i = 1; i < projection.Length; i++)
            {
                k = (projection[i] - projection[0]) * baseVector;

                if (k > maxK)
                {
                    maxK = k;
                    segment[1] = projection[i];
                }
                else if (k < minK)
                {
                    minK = k;
                    segment[0] = projection[i];
                }
            }

            if (minK == 0)
                segment[0] = projection[0];

            if (maxK == 0)
                segment[1] = projection[0];
        }

        protected bool IsOuterSphereIntersectWith(Collider collider, out Vector3 centersVector)
        {
            centersVector = collider.getCenterInGlobal() - getCenterInGlobal();
            return centersVector.squaredLength() <= squaredOuterSphereRadius + 2 * outerSphereRadius * collider.outerSphereRadius + collider.squaredOuterSphereRadius;
        }

        public virtual bool getCollisionExitVector(Collider collider, out Vector3? collisionExitVector, out Vector3? exitDirectionVector, out Vector3? colliderEndPoint)
        {
            collisionExitVector = null;
            exitDirectionVector = null;
            colliderEndPoint = null;

            if(!IsOuterSphereIntersectWith(collider, out _))
            {
                return false;
            }

            List<Vector3> normals = new List<Vector3>();
            normals.AddRange(this.globalSpaceNormals);
            foreach(var normal in collider.globalSpaceNormals)
            {
                if(!normals.Exists(n => n.isCollinearTo(normal)))
                {
                    normals.Add(normal);
                }
            }

            Vector3 edge1, edge2, currentNormal;
            if (collider is SphereCollider)
            {
                Vector3 sphereCenter = collider.getCenterInGlobal();
                
                foreach(Vector3 vertex in globalSpaceVertices)
                {
                    currentNormal = vertex - sphereCenter;
                    if (!normals.Exists(n => n.isCollinearTo(currentNormal)))
                        normals.Add(currentNormal);
                }
                
                foreach(int[] poly in polygons)
                {
                    for(int i = 0; i < poly.Length; i++)
                    {
                        edge1 = globalSpaceVertices[poly[(i + 1) % poly.Length]] - globalSpaceVertices[poly[i]];
                        edge2 = sphereCenter - globalSpaceVertices[poly[i]];
                
                        currentNormal = edge2 % edge1 % edge1;
                        if (!normals.Exists(n => n.isCollinearTo(currentNormal)))
                            normals.Add(currentNormal);
                    }
                }
            }
            else
            {
                foreach (int[] poly1 in this.polygons)
                {
                    foreach (int[] poly2 in collider.polygons)
                    {
                        for (int i = 0; i < poly1.Length; i++)
                        {
                            edge1 = globalSpaceVertices[poly1[(i + 1) % poly1.Length]] - globalSpaceVertices[poly1[i]];

                            for (int j = 0; j < poly2.Length; j++)
                            {
                                edge2 = collider.globalSpaceVertices[poly2[(j + 1) % poly2.Length]] - collider.globalSpaceVertices[poly2[j]];
                                currentNormal = edge1.cross(edge2);

                                if (!normals.Exists(n => n.isCollinearTo(currentNormal)))
                                {
                                    normals.Add(currentNormal);
                                }
                            }
                        }
                    }
                }
            }

            Vector3[] segment1 = new Vector3[2];
            Vector3[] segment2 = new Vector3[2];
            Vector3[] projection1 = new Vector3[globalSpaceVertices.Count];
            Vector3[] projection2 = new Vector3[collider.globalSpaceVertices.Count];
            foreach(Vector3 normal in normals)
            {
                projectOnVector(normal, projection1);
                collider.projectOnVector(normal, projection2);

                getSegmentFromProjection(projection1, segment1);
                getSegmentFromProjection(projection2, segment2);

                if ((segment2[1] - segment2[0]) * (segment1[1] - segment1[0]) < 0)
                {
                    Vector3 tmp = segment2[1];
                    segment2[1] = segment2[0];
                    segment2[0] = tmp;
                }

                Vector3 segmentVector = segment1[1] - segment1[0];
                double segmentVectorSqrLength = segmentVector.squaredLength();
                double t1 = (segment2[0] - segment1[0]) * segmentVector;
                double t2 = (segment2[1] - segment1[0]) * segmentVector;

                if (t2 < 0 || t1 > segmentVectorSqrLength)
                    return false;

                Vector3 outVec1 = segment2[0] - segment1[1];
                Vector3 outVec2 = segment2[1] - segment1[0];
                Vector3 outVec;
                Vector3 newEndPoint;
                if (outVec1.squaredLength() > outVec2.squaredLength())
                {
                    newEndPoint = segment1[0];
                    outVec = outVec2;
                }
                else
                {
                    newEndPoint = segment1[1];
                    outVec = outVec1;
                }

                if (!collisionExitVector.HasValue || outVec.squaredLength() < collisionExitVector.Value.squaredLength())
                {
                    colliderEndPoint = newEndPoint;
                    collisionExitVector = outVec;
                    exitDirectionVector = segmentVector;
                }
            }

            return true;
        }

        protected virtual List<int> getVertexOnPlaneIndices(Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon)
        {
            List<int> vertexIndices = new List<int>();
            for (int i = 0; i < globalSpaceVertices.Count; i++)
            {
                if (Math.Abs((globalSpaceVertices[i] - collisionPlanePoint).dotMul(collisionPlaneNormal)) < epsilon)
                    vertexIndices.Add(i);
            }

            return vertexIndices;
        }

        private static Vector3 _GetAverageCollisionPointWithEpsilon(Collider collider1, Collider collider2, Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal, double epsilon = 1E-7)
        {
            double sqrEpsilon = epsilon * epsilon;

            List<int[]> getEdgesToCheckIntersections(Collider collider, List<int> vertexOnPlaneIndices, List<int> insideOtherColliderVertexIndices)
            {
                List<int[]> edges = new List<int[]>();
                int startIndex, endIndex;
                int i;
                foreach (int[] poly in collider.polygons)
                {
                    for (i = 0; i < poly.Length; i++)
                    {
                        startIndex = poly[i];
                        endIndex = poly[(i + 1) % poly.Length];

                        if (vertexOnPlaneIndices.Contains(startIndex) && vertexOnPlaneIndices.Contains(endIndex) &&
                            !edges.Exists(edge => edge[0] == startIndex && edge[1] == endIndex || edge[0] == endIndex && edge[1] == startIndex))
                        {
                            edges.Add(new int[] { startIndex, endIndex });
                        }
                    }
                }

                return edges;
            }
            
            Vector3[] getFigureFromVertexIndices(Collider collider, List<int> vertexIndices)
            {
                List<Vector3> vertices = vertexIndices.Select(index => collider.globalSpaceVertices[index]).ToList();

                int count = vertices.Count;
                Vector3[] figure = new Vector3[count];
                figure[0] = vertices[0];
                vertices.RemoveAt(0);

                Vector3 start = figure[0];
                Vector3 current;
                for(int k = 1; k < count - 1; k++)
                {
                    current = vertices[0] - start;
                    int currentIndex = 0;
                    for (int i = 1; i < vertices.Count; i++)
                    {
                        if ((vertices[i] - start).cross(current) * collisionPlaneNormal > 0)
                        {
                            currentIndex = i;
                            current = vertices[i] - start;
                        }
                    }

                    figure[k] = vertices[currentIndex];
                    vertices.RemoveAt(currentIndex);
                }

                if (vertices.Count > 0)
                    figure[figure.Length - 1] = vertices[0];

                //while (vertices.Count > 1)
                //{
                //    Vector3 current = vertices[0] - start;
                //    int currentIndex = 0;
                //    for (int i = 1; i < vertices.Count; i++)
                //    {
                //        if ((vertices[i] - start).cross(current) * collisionPlaneNormal > 0)
                //        {
                //            currentIndex = i;
                //            current = vertices[i] - start;
                //        }
                //    }
                //
                //    figure.Add(vertices[currentIndex]);
                //    vertices.RemoveAt(currentIndex);
                //}
                

                return figure;
            }

            bool IsPointInsideFigure(Vector3[] figure, Vector3 point)
            {
                Vector3 start, end, edge;
                Vector3 vec, vecMult, prevVecMult;
                double edgeDotMult, currentDotMult;
                int count = figure.Length;

                prevVecMult = (figure[1] - figure[0]).cross(point - figure[0]);
                for (int i = 0; i < count; i++)
                {
                    start = figure[i];
                    end = figure[(i + 1) % count];
                    edge = end - start;
                    vec = point - start;

                    vecMult = edge.cross(vec);
                    if (vecMult.squaredLength() < sqrEpsilon)
                    {
                        edgeDotMult = edge.dotMul(edge);
                        currentDotMult = vec.dotMul(edge);

                        if (currentDotMult >= 0 && currentDotMult <= edgeDotMult)
                            return true;
                        else continue;
                    }

                    if (vecMult.dotMul(prevVecMult) < 0)
                    {
                        return false;
                    }

                    prevVecMult = vecMult;
                }

                return true;
            }

            List<int> vertexOnPlaneIndices_1 = collider1.getVertexOnPlaneIndices(collisionPlanePoint, collisionPlaneNormal, epsilon);
            List<int> vertexOnPlaneIndices_2 = collider2.getVertexOnPlaneIndices(collisionPlanePoint, collisionPlaneNormal, epsilon);

            if (vertexOnPlaneIndices_1.Count == 0 || vertexOnPlaneIndices_2.Count == 0)
                throw new ArgumentException("Colliders don't intersect in the given plane.");

            if (vertexOnPlaneIndices_1.Count == 1)
                return collider1.globalSpaceVertices[vertexOnPlaneIndices_1[0]];
            if (vertexOnPlaneIndices_2.Count == 1)
                return collider2.globalSpaceVertices[vertexOnPlaneIndices_2[0]];

            Vector3[] figure_1 = getFigureFromVertexIndices(collider1, vertexOnPlaneIndices_1);
            Vector3[] figure_2 = getFigureFromVertexIndices(collider2, vertexOnPlaneIndices_2);

            List<int> insideVertexIndices_1 = vertexOnPlaneIndices_1.Where(index => IsPointInsideFigure(figure_2, collider1.globalSpaceVertices[index])).ToList();
            List<int> insideVertexIndices_2 = vertexOnPlaneIndices_2.Where(index => IsPointInsideFigure(figure_1, collider2.globalSpaceVertices[index])).ToList();

            List<int[]> edges_1 = getEdgesToCheckIntersections(collider1, vertexOnPlaneIndices_1, insideVertexIndices_1);
            List<int[]> edges_2 = getEdgesToCheckIntersections(collider2, vertexOnPlaneIndices_2, insideVertexIndices_2);

            Vector3[] firstColliderVertices = new Vector3[insideVertexIndices_1.Count];
            for(int index = 0; index < firstColliderVertices.Length; index++)
            {
                firstColliderVertices[index] = collider1.globalSpaceVertices[insideVertexIndices_1[index]];
            }

            LinkedList<Vector3> points = new LinkedList<Vector3>(firstColliderVertices);
            if (firstColliderVertices.Length == 0)
            {
                foreach(int index in insideVertexIndices_2)
                {
                    points.AddLast(collider2.globalSpaceVertices[index]);
                }
            }
            else
            {
                Vector3 vertex;

                foreach (int index in insideVertexIndices_2)
                {
                    vertex = collider2.globalSpaceVertices[index];

                    if (!firstColliderVertices.Any(v => (v - vertex).isZero()))
                        points.AddLast(vertex);
                }
            }

            Vector3 start1, end1, edge1;
            Vector3 start2, end2, edge2;
            foreach (int[] edgeIndices_1 in edges_1)
            {
                start1 = collider1.globalSpaceVertices[edgeIndices_1[0]];
                end1 = collider1.globalSpaceVertices[edgeIndices_1[1]];
                edge1 = end1 - start1;

                foreach (int[] edgeIndices_2 in edges_2)
                {
                    start2 = collider2.globalSpaceVertices[edgeIndices_2[0]];
                    end2 = collider2.globalSpaceVertices[edgeIndices_2[1]];
                    edge2 = end2 - start2;

                    Vector3 start1ToStart2 = start2 - start1;
                    Vector3 start1ToEnd2 = end2 - start1;

                    if (!edge1.isCollinearTo(edge2))
                    {
                        Vector3 start2Proj = start1 + start1ToStart2.projectOnVector(edge1);
                        Vector3 end2Proj = start1 + start1ToEnd2.projectOnVector(edge1);
                        Vector3 start2ToProj = start2Proj - start2;
                        Vector3 end2ToProj = end2Proj - end2;

                        if (!start2ToProj.isZero() && !end2ToProj.isZero() && start2ToProj.dotMul(end2ToProj) <= 0)
                        {
                            double k = Math.Sqrt(end2ToProj.squaredLength() / start2ToProj.squaredLength());
                            Vector3 intersectionPoint = start2Proj + 1.0 / (k + 1.0) * (end2Proj - start2Proj);

                            if (!points.Any(v => (v - intersectionPoint).isZero()))
                            {
                                points.AddLast(intersectionPoint);
                            }
                        }
                    }
                }
            }

            double x = 0, y = 0, z = 0;
            int pointCount = 0;
            LinkedList<Vector3>.Enumerator enumerator = points.GetEnumerator();
            while (enumerator.MoveNext())
            {
                start1 = enumerator.Current;
                pointCount++;
                x += start1.x;
                y += start1.y;
                z += start1.z;
            }

            return new Vector3(x / pointCount, y / pointCount, z / pointCount);
        }

        public static Vector3 GetAverageCollisionPoint(Collider collider1, Collider collider2, Vector3 collisionPlanePoint, Vector3 collisionPlaneNormal)
        {
            Vector3 point = _GetAverageCollisionPointWithEpsilon(collider1, collider2, collisionPlanePoint, collisionPlaneNormal, Constants.Epsilon);
            return double.IsNaN(point.x) || double.IsNaN(point.y) || double.IsNaN(point.z) ?
                _GetAverageCollisionPointWithEpsilon(collider1, collider2, collisionPlanePoint, collisionPlaneNormal, Constants.FloatEpsilon) :
                point;
        }

        public override void fixedUpdate()
        {
            calculateGlobalVertices();
            calculateNormalsInGlobal();
        }
    }
}